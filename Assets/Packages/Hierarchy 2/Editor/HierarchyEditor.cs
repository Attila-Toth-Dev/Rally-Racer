using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;

using UnityEditorInternal;

using UnityEngine;
using UnityEngine.SceneManagement;

using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace Hierarchy2
{
	[InitializeOnLoad]
	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	[SuppressMessage("ReSharper", "NotAccessedField.Local")]
	[SuppressMessage("ReSharper", "UnusedParameter.Local")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public sealed class HierarchyEditor
	{
		internal const int GLOBAL_SPACE_OFFSET_LEFT = 16 * 2;

		private static HierarchyEditor instance;

		public static HierarchyEditor Instance
		{
			get { return instance ??= new HierarchyEditor(); }
		}

		public readonly Dictionary<int, Object> selectedComponents = new Dictionary<int, Object>();
		private readonly Dictionary<string, string> dicComponents = new Dictionary<string, string>(StringComparer.Ordinal);

		private readonly GUIContent tooltipContent = new();

		private HierarchySettings settings;
		private HierarchyResources resources;

		private HierarchySettings.ThemeData ThemeData => settings.UsedTheme;

		private int deepestRow = int.MinValue;
		private int previousRowIndex = int.MinValue;

		private int sceneIndex;
		private Scene currentScene;

		public static bool IsMultiScene => SceneManager.sceneCount > 1;

		private bool selectionStyleAfterInvoke;
		private bool checkingAllHierarchy;

		private Event currentEvent;

		private readonly RowItem rowItem = new();
		private RowItem previousElement;
		private WidthUse widthUse = WidthUse.Zero;

		static HierarchyEditor() => instance = new HierarchyEditor();

		public HierarchyEditor()
		{
			InternalReflection();
			EditorApplication.update += EditorAwake;
			AssetDatabase.importPackageCompleted += ImportPackageCompleted;
		}

		// ReSharper disable once NotAccessedField.Local
		private static List<Type> internalEditorType = new();
		private static Dictionary<string, Type> dicInternalEditorType = new();

		public static Type sceneHierarchyWindow;
		private static Type sceneHierarchy;
		private static Type gameObjectTreeViewGUI;

		private static FieldInfo sceneHierarchyField;
		private static FieldInfo treeViewField;
		private static PropertyInfo guiField;
		private static FieldInfo iconWidth;
		private static MethodInfo getItemAndRowIndexMethod;
		private static PropertyInfo treeViewIData;

		private static Func<SearchableEditorWindow> lastInteractedHierarchyWindowDelegate;
		private static Func<IEnumerable> getAllSceneHierarchyWindowsDelegate;
		public static Func<GameObject, Rect, bool, bool> iconSelectorShowAtPositionDelegate;
		private static Action<Rect, Object, int> displayObjectContextMenuDelegate;

		private double lastTimeSinceStartup = EditorApplication.timeSinceStartup;

		// ReSharper disable once UnassignedField.Global
		public static Action onRepaintHierarchyWindowCallback;

		// ReSharper disable once UnassignedField.Global
		public static Action onWindowsReorderedCallback;

		private bool hierarchyChangedRequireUpdating;

		private bool prefabStageChanged;

		private readonly GUIContent tmpSceneContent = new();

		private static void InternalReflection()
		{
			Type[] arrayInternalEditorType = typeof(Editor).Assembly.GetTypes();
			internalEditorType = arrayInternalEditorType.ToList();
			dicInternalEditorType = arrayInternalEditorType.ToDictionary(_type => _type.FullName);

			FieldInfo refreshHierarchy = typeof(EditorApplication).GetField(nameof(refreshHierarchy), BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo onRepaintHierarchyWindow = typeof(HierarchyEditor).GetMethod("OnRepaintHierarchyWindow", BindingFlags.NonPublic | BindingFlags.Static);
			Debug.Assert(onRepaintHierarchyWindow != null, nameof(onRepaintHierarchyWindow) + " != null");
			Delegate refreshHierarchyDelegate = Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), onRepaintHierarchyWindow);
			refreshHierarchy?.SetValue(null, refreshHierarchyDelegate);

			FieldInfo windowsReordered = typeof(EditorApplication).GetField(nameof(windowsReordered), BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo onWindowsReordered = typeof(HierarchyEditor).GetMethod("OnWindowsReordered", BindingFlags.NonPublic | BindingFlags.Static);
			Debug.Assert(onWindowsReordered != null, nameof(onWindowsReordered) + " != null");
			Delegate windowsReorderedDelegate = Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), onWindowsReordered);
			windowsReordered?.SetValue(null, windowsReorderedDelegate);

			{
				dicInternalEditorType.TryGetValue(nameof(UnityEditor) + ".SceneHierarchyWindow", out sceneHierarchyWindow);
				dicInternalEditorType.TryGetValue(nameof(UnityEditor) + ".GameObjectTreeViewGUI", out gameObjectTreeViewGUI); //GameObjectTreeViewGUI : TreeViewGUI
				dicInternalEditorType.TryGetValue(nameof(UnityEditor) + ".SceneHierarchy", out sceneHierarchy);
			}

			MethodInfo lastInteractedHierarchyWindow = sceneHierarchyWindow?.GetProperty(nameof(lastInteractedHierarchyWindow), BindingFlags.Static | BindingFlags.Public)?.GetGetMethod();
			Debug.Assert(lastInteractedHierarchyWindow != null, nameof(lastInteractedHierarchyWindow) + " != null");
			lastInteractedHierarchyWindowDelegate = Delegate.CreateDelegate(typeof(Func<SearchableEditorWindow>), lastInteractedHierarchyWindow) as Func<SearchableEditorWindow>;

			MethodInfo getAllSceneHierarchyWindows = sceneHierarchyWindow.GetMethod("GetAllSceneHierarchyWindows", BindingFlags.Static | BindingFlags.Public);
			Debug.Assert(getAllSceneHierarchyWindows != null, nameof(getAllSceneHierarchyWindows) + " != null");
			getAllSceneHierarchyWindowsDelegate = Delegate.CreateDelegate(typeof(Func<IEnumerable>), getAllSceneHierarchyWindows) as Func<IEnumerable>;

			{
				sceneHierarchyField = sceneHierarchyWindow.GetField("m_SceneHierarchy", BindingFlags.NonPublic | BindingFlags.Instance);
				treeViewField = sceneHierarchy?.GetField("m_TreeView", BindingFlags.NonPublic | BindingFlags.Instance);
				guiField = treeViewField?.FieldType.GetProperty("gui".ToLower(), BindingFlags.Public | BindingFlags.Instance);
				iconWidth = gameObjectTreeViewGUI?.GetField("k_IconWidth", BindingFlags.Public | BindingFlags.Instance);
			}

			MethodInfo displayObjectContextMenu = typeof(EditorUtility).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single
				(
				 _method => _method.Name == "DisplayObjectContextMenu" && _method.GetParameters()[1].ParameterType == typeof(Object)
				);
			displayObjectContextMenuDelegate = Delegate.CreateDelegate(typeof(Action<Rect, Object, int>), displayObjectContextMenu) as Action<Rect, Object, int>;

			Type iconSelector = typeof(EditorWindow).Assembly.GetTypes().Single(_type => _type.BaseType == typeof(EditorWindow) && _type.Name == "IconSelector");
			MethodInfo showAtPosition = iconSelector.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single
				(
				 _method => _method.Name == "ShowAtPosition" && _method.GetParameters()[0].ParameterType == typeof(Object)
				);
			iconSelectorShowAtPositionDelegate = Delegate.CreateDelegate(typeof(Func<GameObject, Rect, bool, bool>), showAtPosition) as Func<GameObject, Rect, bool, bool>;

			getItemAndRowIndexMethod = treeViewField.FieldType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(_method => _method.Name == "GetItemAndRowIndex");

			treeViewIData = treeViewField.FieldType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Single(_property => _property.Name == "data");
		}

		public static void DisplayObjectContextMenu(Rect _rect, Object _unityObject, int _value) => displayObjectContextMenuDelegate(_rect, _unityObject, _value);

		private static void OnRepaintHierarchyWindow() => onRepaintHierarchyWindowCallback?.Invoke();

		private static void OnWindowsReordered() => onWindowsReorderedCallback?.Invoke();

		private void EditorAwake()
		{
			settings = HierarchySettings.GetAssets();

			if(settings is null) return;

			OnSettingsChanged(nameof(settings.components));
			settings.onSettingsChanged += OnSettingsChanged;

			resources = HierarchyResources.GetAssets();

			if(resources is null) return;

			resources.GenerateKeyForAssets();

			EditorApplication.hierarchyWindowItemOnGUI += HierarchyOnGUI;

			if(settings.activeHierarchy)
				Invoke();
			else
				Dispose();

			EditorApplication.update -= EditorAwake;
		}

		private void ImportPackageCompleted(string _packageName) { }

		private void OnSettingsChanged(string _param)
		{
			switch(_param)
			{
				case nameof(settings.components):
					dicComponents.Clear();
					foreach(string componentType in settings.components)
					{
						if(!dicComponents.ContainsKey(componentType))
							dicComponents.Add(componentType, componentType);
					}

					break;
			}

			EditorApplication.RepaintHierarchyWindow();
		}

		public void Invoke()
		{
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			PrefabStage.prefabStageOpened += OnPrefabStageOpened;
			PrefabStage.prefabStageClosing += OnPrefabStageClosing;

			EditorApplication.update += OnEditorUpdate;

			selectionStyleAfterInvoke = false;
			EditorApplication.RepaintHierarchyWindow();
			EditorApplication.RepaintProjectWindow();
		}

		public void Dispose()
		{
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
			PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
			PrefabStage.prefabStageClosing -= OnPrefabStageClosing;

			EditorApplication.update -= OnEditorUpdate;

			foreach(EditorWindow window in getAllSceneHierarchyWindowsDelegate())
			{
				window.titleContent.text = "Hierarchy";
			}

			EditorApplication.RepaintHierarchyWindow();
			EditorApplication.RepaintProjectWindow();
		}

		private void OnEditorUpdate()
		{
			if(EditorApplication.timeSinceStartup - lastTimeSinceStartup >= 1)
			{
				DelayCall();
				lastTimeSinceStartup = EditorApplication.timeSinceStartup;
			}
		}

		private void DelayCall()
		{
			if(checkingAllHierarchy)
			{
				for(int i = 0; i < HierarchyWindow.windows.Count; ++i)
				{
					if(HierarchyWindow.windows[i].editorWindow == null)
					{
						HierarchyWindow.windows[i].Dispose();
						--i;
					}
				}

				foreach(EditorWindow window in getAllSceneHierarchyWindowsDelegate())
				{
					if(!HierarchyWindow.instances.ContainsKey(window.GetInstanceID()))
					{
						HierarchyWindow hierarchyWindow = new(window);
						hierarchyWindow.SetWindowTitle("Hierarchy 2");
					}
				}

				checkingAllHierarchy = false;
			}

			if(hierarchyChangedRequireUpdating)
				hierarchyChangedRequireUpdating = false;
		}

		private void OnHierarchyChanged() => hierarchyChangedRequireUpdating = true;

		private void OnPrefabStageOpened(PrefabStage _stage) => prefabStageChanged = true;

		private void OnPrefabStageClosing(PrefabStage _stage)
		{
			prefabStageChanged = true;

			foreach(HierarchyWindow window in HierarchyWindow.windows)
				window.Reflection();
		}

		private void HierarchyOnGUI(int _selectionID, Rect _selectionRect)
		{
			currentEvent = Event.current;

			if(currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.H && currentEvent.control)
			{
				if(!settings.activeHierarchy)
					Invoke();
				else
					Dispose();

				settings.activeHierarchy = !settings.activeHierarchy;
				currentEvent.Use();
			}

			if(!settings.activeHierarchy)
				return;

			if(currentEvent.control && currentEvent.keyCode == KeyCode.D)
				return;

			if(currentEvent.type == EventType.Layout)
			{
				if(prefabStageChanged)
				{
					prefabStageChanged = false;
				}

				return;
			}

			checkingAllHierarchy = true;

			if(selectionStyleAfterInvoke == false && currentEvent.type == EventType.MouseDown)
			{
				selectionStyleAfterInvoke = true;
			}

			rowItem.Dispose();
			rowItem.id = _selectionID;
			rowItem.gameObject = EditorUtility.InstanceIDToObject(rowItem.id) as GameObject;
			rowItem.rect = _selectionRect;
			rowItem.rowIndex = GetRowIndex(_selectionRect);
			rowItem.isSelected = InSelection(_selectionID);
			rowItem.isFirstRow = IsFirstRow(_selectionRect);
			rowItem.isFirstElement = IsFirstElement(_selectionRect);

			rowItem.isNull = rowItem.gameObject == null;

			if(!rowItem.isNull)
			{
				Debug.Assert(rowItem.gameObject != null, "rowItem.gameObject != null");
				rowItem.hierarchyFolder = rowItem.gameObject.GetComponent<HierarchyFolder>();
				if(!(rowItem.isFolder = rowItem.hierarchyFolder))
					rowItem.isSeparator = rowItem.Name.StartsWith(settings.separatorStartWith);

				rowItem.isDirty = EditorUtility.IsDirty(_selectionID);

				if(!rowItem.isSeparator && rowItem.isDirty)
				{
					rowItem.isPrefab = PrefabUtility.IsPartOfAnyPrefab(rowItem.gameObject);

					if(rowItem.isPrefab)
						rowItem.isPrefabMissing = PrefabUtility.IsPrefabAssetMissing(rowItem.gameObject);
				}
			}

			Debug.Assert(rowItem.gameObject != null, "rowItem.gameObject != null");
			rowItem.isRootObject = rowItem.isNull || rowItem.gameObject.transform.parent == null;
			rowItem.isMouseHovering = _selectionRect.Contains(currentEvent.mousePosition);

			if(rowItem.isFirstRow) //Instance always null
			{
				sceneIndex = 0;

				if(deepestRow > previousRowIndex)
					deepestRow = previousRowIndex;
			}

			if(rowItem.isNull)
			{
				if(!IsMultiScene)
					currentScene = SceneManager.GetActiveScene();
				else
				{
					if(!rowItem.isFirstRow && sceneIndex < SceneManager.sceneCount - 1)
						sceneIndex++;
					currentScene = SceneManager.GetSceneAt(sceneIndex);
				}

				RenameSceneInHierarchy();

				if(settings.displayRowBackground)
				{
					if(deepestRow != rowItem.rowIndex)
						DisplayRowBackground();
				}

				previousElement = rowItem;
				previousRowIndex = rowItem.rowIndex;

				if(previousRowIndex > deepestRow)
					deepestRow = previousRowIndex;
			}
			else
			{
				if(rowItem.isFirstElement)
				{
					if(deepestRow > previousRowIndex)
						deepestRow = previousRowIndex;
					deepestRow -= rowItem.rowIndex;

					if(IsMultiScene)
					{
						if(!previousElement.isNull)
						{
							for(int i = 0; i < SceneManager.sceneCount; ++i)
							{
								if(SceneManager.GetSceneAt(i) == rowItem.gameObject.scene)
								{
									sceneIndex = i;

									break;
								}
							}
						}
					}
				}

				if(IsMultiScene) { }

				rowItem.nameRect = rowItem.rect;
				GUIStyle nameStyle = TreeStyleFromFont(FontStyle.Normal);
				rowItem.nameRect.width = nameStyle.CalcSize(new GUIContent(rowItem.gameObject.name)).x;

				rowItem.nameRect.x += 16;

				bool isPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

				if(settings.displayRowBackground && deepestRow != rowItem.rowIndex)
				{
					if(isPrefabMode)
					{
						if(rowItem.gameObject.transform.parent == null) //Should use row index instead.
						{
							if(deepestRow != 0)
								DisplayRowBackground();
						}
					}
					else
						DisplayRowBackground();
				}

				if(rowItem.isFolder)
				{
					Texture icon = rowItem.ChildCount > 0 ? Resources.FolderIcon : Resources.EmptyFolderIcon;
					DisplayCustomObjectIcon(icon);
				}

				if(rowItem.isSeparator && rowItem.isRootObject)
				{
					ElementAsSeparator();

					goto FINISH;
				}

				if(settings.useInstantBackground)
					CustomRowBackground();

				if(settings.displayTreeView && !rowItem.isRootObject)
					DisplayTreeView();

				if(settings.displayCustomObjectIcon)
					DisplayCustomObjectIcon(null);

				widthUse = WidthUse.Zero;
				widthUse.left += GLOBAL_SPACE_OFFSET_LEFT;
				if(isPrefabMode) widthUse.left -= 2;
				widthUse.afterName = rowItem.nameRect.x + rowItem.nameRect.width;

				widthUse.afterName += settings.offSetIconAfterName;

				DisplayEditableIcon();

				// DisplayNoteIcon();

				widthUse.afterName += 8;

				if(settings.displayTag && !rowItem.gameObject.CompareTag("Untagged"))
				{
					if(!settings.onlyDisplayWhileMouseEnter ||
					   (settings.contentDisplay & HierarchySettings.ContentDisplay.Tag) !=
					   HierarchySettings.ContentDisplay.Tag ||
					   ((settings.contentDisplay & HierarchySettings.ContentDisplay.Tag) ==
					    HierarchySettings.ContentDisplay.Tag && rowItem.isMouseHovering))
					{
						DisplayTag();
					}
				}

				if(settings.displayLayer && rowItem.gameObject.layer != 0)
				{
					if(!settings.onlyDisplayWhileMouseEnter ||
					   (settings.contentDisplay & HierarchySettings.ContentDisplay.Layer) !=
					   HierarchySettings.ContentDisplay.Layer ||
					   ((settings.contentDisplay & HierarchySettings.ContentDisplay.Layer) ==
					    HierarchySettings.ContentDisplay.Layer && rowItem.isMouseHovering))
					{
						DisplayLayer();
					}
				}

				if(settings.displayComponents)
				{
					if(!settings.onlyDisplayWhileMouseEnter ||
					   (settings.contentDisplay & HierarchySettings.ContentDisplay.Component) !=
					   HierarchySettings.ContentDisplay.Component ||
					   ((settings.contentDisplay & HierarchySettings.ContentDisplay.Component) ==
					    HierarchySettings.ContentDisplay.Component && rowItem.isMouseHovering))
					{
						DisplayComponents();
					}
				}

				ElementEvent(rowItem);

				FINISH:
				if(settings.displayGrid)
					DisplayGrid();

				previousElement = rowItem;
				previousRowIndex = rowItem.rowIndex;

				if(previousRowIndex > deepestRow)
				{
					deepestRow = previousRowIndex;
				}
			}
		}

		private GUIStyle TreeStyleFromFont(FontStyle _fontStyle) => _fontStyle switch
		{
			FontStyle.Bold => new GUIStyle(Styles.TreeBoldLabel),
			FontStyle.Italic => new GUIStyle(Styles.treeLabel),
			FontStyle.BoldAndItalic => new GUIStyle(Styles.TreeBoldLabel),
			_ => new GUIStyle(Styles.treeLabel)
		};

		private void CustomRowBackground()
		{
			if(currentEvent.type != EventType.Repaint)
				return;

			HierarchySettings.InstantBackgroundColor instantBackgroundColor = new HierarchySettings.InstantBackgroundColor();
			bool contain = false;
			for(int i = 0; i < settings.instantBackgroundColors.Count; ++i)
			{
				if(!settings.instantBackgroundColors[i].active) continue;

				if
				(
					(settings.instantBackgroundColors[i].useTag && !string.IsNullOrEmpty(settings.instantBackgroundColors[i].tag) && rowItem.gameObject.CompareTag(settings.instantBackgroundColors[i].tag)) ||
					(settings.instantBackgroundColors[i].useLayer && (1 << rowItem.gameObject.layer & settings.instantBackgroundColors[i].layer) != 0) ||
					(settings.instantBackgroundColors[i].useStartWith && !string.IsNullOrEmpty(settings.instantBackgroundColors[i].startWith) && rowItem.Name.StartsWith(settings.instantBackgroundColors[i].startWith))
				)
				{
					contain = true;
					instantBackgroundColor = settings.instantBackgroundColors[i];
				}
			}

			if(!contain) return;

			Color guiColor = GUI.color;
			GUI.color = instantBackgroundColor.color;
			Texture2D texture = Resources.PixelWhite;
			Rect rect = RectFromRight(rowItem.rect, rowItem.rect.width + 16, 0);
			rect.x += 16;
			rect.xMin = 32;

			GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
			GUI.color = guiColor;
		}

		private void ElementAsSeparator()
		{
			if(currentEvent.type != EventType.Repaint)
				return;

			if(!rowItem.gameObject.CompareTag(settings.separatorDefaultTag))
				rowItem.gameObject.tag = settings.separatorDefaultTag;

			Rect rect = EditorGUIUtility.PixelsToPoints(RectFromLeft(rowItem.rect, Screen.width, 0));
			rect.y = rowItem.rect.y;
			rect.height = rowItem.rect.height;
			rect.x += GLOBAL_SPACE_OFFSET_LEFT;
			rect.width -= GLOBAL_SPACE_OFFSET_LEFT;

			Color guiColor = GUI.color;
			GUI.color = ThemeData.colorHeaderBackground;
			GUI.DrawTexture(rect, Resources.PixelWhite, ScaleMode.StretchToFill);

			GUIContent content = new GUIContent(rowItem.Name.Remove(0, settings.separatorStartWith.Length));
			rect.x += (rect.width - Styles.header.CalcSize(content).x) / 2;
			GUI.color = ThemeData.colorHeaderTitle;
			GUI.Label(rect, content, Styles.header);
			GUI.color = guiColor;
		}

		private void ElementEvent(RowItem _element)
		{
			if(currentEvent.type == EventType.KeyDown)
			{
				if(currentEvent.control && currentEvent.shift && currentEvent.alt &&
				   currentEvent.keyCode == KeyCode.C && lastInteractedHierarchyWindowDelegate() != null)
					CollapseAll();
			}

			if(currentEvent.type == EventType.KeyUp &&
			   currentEvent.keyCode == KeyCode.F2 &&
			   Selection.gameObjects.Length > 1)
			{
				SelectionsRenamePopup.ShowPopup();
				currentEvent.Use();

				return;
			}

			if(_element.rect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseUp &&
			   currentEvent.button == 2)
			{
				Undo.RegisterCompleteObjectUndo(_element.gameObject,
				                                _element.gameObject.activeSelf ? "Inactive object" : "Active object");
				_element.gameObject.SetActive(!_element.gameObject.activeSelf);
				currentEvent.Use();
			}
		}

		private void StaticIcon(RowItem _element)
		{
			if(!_element.IsStatic) return;

			Rect rect = _element.rect;
			rect = RectFromRight(rect, 3, 0);

			if(currentEvent.type == EventType.MouseUp &&
			   currentEvent.button == 1 &&
			   rect.Contains(currentEvent.mousePosition))
			{
				GenericMenu staticMenu = new GenericMenu();
				staticMenu.AddItem(new GUIContent("Apply All Children"), settings.applyStaticTargetAndChild,
				                   () => { settings.applyStaticTargetAndChild = !settings.applyStaticTargetAndChild; });
				staticMenu.AddSeparator("");
				staticMenu.AddItem(new GUIContent("True"), _element.gameObject.isStatic,
				                   () => { _element.gameObject.isStatic = !_element.gameObject.isStatic; });
				staticMenu.AddItem(new GUIContent("False"), !_element.gameObject.isStatic,
				                   () => { _element.gameObject.isStatic = !_element.gameObject.isStatic; });
				staticMenu.ShowAsContext();
				currentEvent.Use();
			}

			GUISeparator(rect, Color.magenta);
		}

		private void ApplyStaticTargetAndChild(Transform _target, bool _value)
		{
			_target.gameObject.isStatic = _value;

			for(int i = 0; i < _target.childCount; ++i)
				ApplyStaticTargetAndChild(_target.GetChild(i), _value);
		}

		private void DisplayCustomObjectIcon(Texture _icon)
		{
			Rect rect = RectFromRight(rowItem.nameRect, 16, rowItem.nameRect.width + 1);
			rect.height = 16;

			if(currentEvent.type == EventType.MouseUp && currentEvent.button == 1 &&
			   rect.Contains(currentEvent.mousePosition))
			{
				iconSelectorShowAtPositionDelegate(rowItem.gameObject, rect, true);
				currentEvent.Use();
			}

			if(currentEvent.type == EventType.Repaint)
			{
				if(rect.Contains(currentEvent.mousePosition)) { }

				if(_icon == null)
				{
					_icon = AssetPreview.GetMiniThumbnail(rowItem.gameObject);

					if(_icon.name is "GameObject Icon" or "d_GameObject Icon" or "Prefab Icon" or "d_Prefab Icon" or "PrefabModel Icon" or "d_PrefabModel Icon")
						return;
				}

				Color guiColor = GUI.color;
				GUI.color = rowItem.rowIndex % 2 != 0 ? ThemeData.colorRowEven : ThemeData.colorRowOdd;
				GUI.DrawTexture(rect, Resources.PixelWhite);
				GUI.color = guiColor;
				GUI.DrawTexture(rect, _icon, ScaleMode.ScaleToFit);
				//ReplaceObjectIcon(rowItem.ID, icon);
			}
		}

		private void DisplayEditableIcon()
		{
			if(rowItem.gameObject.hideFlags == HideFlags.NotEditable)
			{
				Rect lockRect = RectFromLeft(rowItem.nameRect, 12, ref widthUse.afterName);

				if(currentEvent.type == EventType.Repaint)
				{
					GUI.color = ThemeData.colorLockIcon;
					GUI.DrawTexture(lockRect, Resources.lockIconOn, ScaleMode.ScaleToFit);
					GUI.color = Color.white;
				}

				if(currentEvent.type == EventType.MouseUp &&
				   currentEvent.button == 1 &&
				   lockRect.Contains(currentEvent.mousePosition))
				{
					GenericMenu lockMenu = new GenericMenu();

					GameObject gameObject = rowItem.gameObject;

					lockMenu.AddItem(new GUIContent("Unlock"), false, () =>
					{
						Undo.RegisterCompleteObjectUndo(gameObject, "Unlock...");
						foreach(Component component in gameObject.GetComponents<Component>())
						{
							if(component)
							{
								Undo.RegisterCompleteObjectUndo(component, "Unlock...");
								component.hideFlags = HideFlags.None;
							}
						}

						gameObject.hideFlags = HideFlags.None;

						InternalEditorUtility.RepaintAllViews();
					});
					lockMenu.ShowAsContext();
					currentEvent.Use();
				}
			}
		}

		private void DisplayNoteIcon() { }

		private void DisplayComponents()
		{
			List<Object> components = rowItem.gameObject.GetComponents(typeof(Component)).ToList<Object>();
			Renderer rendererComponent = rowItem.gameObject.GetComponent<Renderer>();
			bool hasMaterial = rendererComponent != null && rendererComponent.sharedMaterial != null;

			if(hasMaterial)
			{
				foreach(Material sharedMat in rendererComponent.sharedMaterials)
					components.Add(sharedMat);
			}

			int length = components.Count;
			bool separator = false;
			float widthUsedCached;
			if(settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
			{
				widthUsedCached = widthUse.afterName;
				widthUse.afterName += 4;
			}
			else
			{
				widthUsedCached = widthUse.right;
				widthUse.right += 2;
			}

			for(int i = 0; i < length; ++i)
			{
				Object component = components[i];

				try
				{
					Type comType = component.GetType();

					if(comType != null)
					{
						bool isMono = comType.BaseType == typeof(MonoBehaviour);
						if(isMono)
						{
							//TODO: ???
							bool shouldIgnoreThisMono = false;

							if(shouldIgnoreThisMono) continue;
						}

						switch(settings.componentDisplayMode)
						{
							case HierarchySettings.ComponentDisplayMode.ScriptOnly:
								if(!isMono)
									continue;

								break;

							case HierarchySettings.ComponentDisplayMode.Specified:
								if(!dicComponents.ContainsKey(comType.Name))
									continue;

								break;

							case HierarchySettings.ComponentDisplayMode.Ignore:
								if(dicComponents.ContainsKey(comType.Name))
									continue;

								break;
						}

						Rect rect = settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName ? RectFromLeft(rowItem.nameRect, settings.componentSize, ref widthUse.afterName) : RectFromRight(rowItem.rect, settings.componentSize, ref widthUse.right);

						if(hasMaterial && i == length - rendererComponent.sharedMaterials.Length &&
						   settings.componentDisplayMode != HierarchySettings.ComponentDisplayMode.ScriptOnly)
						{
							foreach(Material sharedMaterial in rendererComponent.sharedMaterials)
							{
								if(sharedMaterial == null) continue;

								ComponentIcon(sharedMaterial, comType, rect, true);

								rect = settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName ? RectFromLeft(rowItem.nameRect, settings.componentSize, ref widthUse.afterName) : RectFromRight(rowItem.rect, settings.componentSize, ref widthUse.right);
							}

							separator = true;

							break;
						}

						ComponentIcon(component, comType, rect);

						if(settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
							widthUse.afterName += settings.componentSpacing;
						else
							widthUse.right += settings.componentSpacing;

						separator = true;
					}
				}
				catch(Exception)
				{
					// ignored
				}
			}

			if(separator && currentEvent.type == EventType.Repaint)
			{
				if(settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
					GUISeparator(RectFromLeft(rowItem.nameRect, 2, widthUsedCached), ThemeData.colorGrid);
			}
		}

		private void ComponentIcon(Object _component, Type _componentType, Rect _rect, bool _isMaterial = false)
		{
			int comHash = _component.GetHashCode();

			if(currentEvent.type == EventType.Repaint)
			{
				Texture image = EditorGUIUtility.ObjectContent(_component, _componentType).image;

				if(selectedComponents.ContainsKey(comHash))
				{
					Color guiColor = GUI.color;
					GUI.color = ThemeData.comSelBgColor;
					GUI.DrawTexture(_rect, Resources.PixelWhite, ScaleMode.StretchToFill);
					GUI.color = guiColor;
				}

				string tooltip = _isMaterial ? _component.name : _componentType.Name;
				tooltipContent.tooltip = tooltip;
				GUI.Box(_rect, tooltipContent, GUIStyle.none);

				GUI.DrawTexture(_rect, image, ScaleMode.ScaleToFit);
			}

			if(_rect.Contains(currentEvent.mousePosition))
			{
				if(currentEvent.type == EventType.MouseDown)
				{
					if(currentEvent.button == 0)
					{
						if(currentEvent.control)
						{
							if(!selectedComponents.ContainsKey(comHash))
							{
								selectedComponents.Add(comHash, _component);
							}
							else
							{
								selectedComponents.Remove(comHash);
							}

							currentEvent.Use();

							return;
						}

						selectedComponents.Clear();
						selectedComponents.Add(comHash, _component);
						currentEvent.Use();

						return;
					}

					if(currentEvent.button == 1)
					{
						if(currentEvent.control)
						{
							GenericMenu componentGenericMenu = new GenericMenu();

							componentGenericMenu.AddItem(new GUIContent("Remove All Component"), false, () =>
							{
								if(!selectedComponents.ContainsKey(comHash))
									selectedComponents.Add(comHash, _component);

								foreach(KeyValuePair<int, Object> selectedComponent in selectedComponents
								                                                       .ToList()
								                                                       .Where(_selectedComponent => _selectedComponent.Value is not Material))
								{
									selectedComponents.Remove(selectedComponent.Key);
									Undo.DestroyObjectImmediate(selectedComponent.Value);
								}

								selectedComponents.Clear();
							});
							componentGenericMenu.ShowAsContext();
						}
						else
						{
							displayObjectContextMenuDelegate(_rect, _component, 0);
						}

						currentEvent.Use();

						return;
					}
				}

				if(currentEvent.type == EventType.MouseUp)
				{
					if(currentEvent.button == 2)
					{
						List<Object> inspectorComponents = new List<Object>();

						foreach(KeyValuePair<int, Object> selectedComponent in selectedComponents)
							inspectorComponents.Add(selectedComponent.Value);

						if(!selectedComponents.ContainsKey(comHash))
							inspectorComponents.Add(_component);

						InstantInspector window = InstantInspector.OpenEditor();
						window.Fill(inspectorComponents,
						            currentEvent.alt ? InstantInspector.FillMode.Add : InstantInspector.FillMode.Default);
						window.Focus();

						currentEvent.Use();

						return;
					}
				}
			}

			if(selectedComponents.Count > 0 &&
			   currentEvent.type == EventType.MouseDown &&
			   currentEvent.button == 0 &&
			   !currentEvent.control &&
			   !_rect.Contains(currentEvent.mousePosition))
			{
				selectedComponents.Clear();
			}
		}

		private void BottomRightArea(Rect _rect) { }

		private void Background(Rect _rect) { }

		private void DisplayTag()
		{
			GUIContent tagContent = new(rowItem.gameObject.tag);

			GUIStyle style = Styles.tag;
			style.normal.textColor = ThemeData.tagColor;
			Rect rect;

			if(settings.tagAlignment == HierarchySettings.ElementAlignment.AfterName)
			{
				rect = RectFromLeft(rowItem.nameRect, style.CalcSize(tagContent).x, ref widthUse.afterName);

				if(currentEvent.type == EventType.Repaint)
				{
					GUISeparator(RectFromLeft(rowItem.nameRect, 1, widthUse.afterName), ThemeData.colorGrid);
					GUI.Label(rect, tagContent, style);
				}
			}
			else
			{
				rect = RectFromRight(rowItem.rect, style.CalcSize(tagContent).x, ref widthUse.right);

				if(currentEvent.type == EventType.Repaint)
				{
					GUISeparator(RectFromRight(rowItem.rect, 1, widthUse.right), ThemeData.colorGrid);
					GUI.Label(rect, tagContent, style);
				}
			}

			if(currentEvent.type == EventType.MouseUp && currentEvent.button == 1 &&
			   rect.Contains(currentEvent.mousePosition))
			{
				GenericMenu menuTags = new();
				GameObject gameObject = rowItem.gameObject;

				menuTags.AddItem(new GUIContent("Apply All Children"), settings.applyTagTargetAndChild,
				                 () => { settings.applyTagTargetAndChild = !settings.applyTagTargetAndChild; });
				menuTags.AddSeparator("");

				foreach(string tag in InternalEditorUtility.tags)
				{
					menuTags.AddItem(new GUIContent(tag), gameObject.CompareTag(tag), () =>
					{
						if(settings.applyTagTargetAndChild)
							ApplyTagTargetAndChild(gameObject.transform, tag);
						else
						{
							Undo.RegisterCompleteObjectUndo(gameObject, "Change Tag");
							gameObject.tag = tag;
						}
					});
				}

				menuTags.ShowAsContext();
				currentEvent.Use();
			}
		}

		private void ApplyTagTargetAndChild(Transform _target, string _tag)
		{
			Undo.RegisterCompleteObjectUndo(_target.gameObject, "Change Tag");
			_target.gameObject.tag = _tag;

			for(int i = 0; i < _target.childCount; ++i)
				ApplyTagTargetAndChild(_target.GetChild(i), _tag);
		}

		private void DisplayLayer()
		{
			GUIContent layerContent = new(LayerMask.LayerToName(rowItem.gameObject.layer));
			GUIStyle style = Styles.layer;
			style.normal.textColor = ThemeData.layerColor;
			Rect rect;

			if(settings.layerAlignment == HierarchySettings.ElementAlignment.AfterName)
			{
				rect = RectFromLeft(rowItem.nameRect, style.CalcSize(layerContent).x, ref widthUse.afterName);

				if(currentEvent.type == EventType.Repaint)
				{
					GUISeparator(RectFromLeft(rowItem.nameRect, 1, widthUse.afterName), ThemeData.colorGrid);
					GUI.Label(rect, layerContent, style);
				}
			}
			else
			{
				rect = RectFromRight(rowItem.rect, style.CalcSize(layerContent).x, ref widthUse.right);

				if(currentEvent.type == EventType.Repaint)
				{
					GUISeparator(RectFromRight(rowItem.rect, 1, widthUse.right), ThemeData.colorGrid);
					GUI.Label(rect, layerContent, style);
				}
			}

			if(currentEvent.type == EventType.MouseUp && currentEvent.button == 1 &&
			   rect.Contains(currentEvent.mousePosition))
			{
				GenericMenu menuLayers = new();
				GameObject gameObject = rowItem.gameObject;

				menuLayers.AddItem(new GUIContent("Apply All Children"), settings.applyLayerTargetAndChild,
				                   () => { settings.applyLayerTargetAndChild = !settings.applyLayerTargetAndChild; });
				menuLayers.AddSeparator("");

				foreach(string layer in InternalEditorUtility.layers)
				{
					menuLayers.AddItem(new GUIContent(layer),
					                   LayerMask.NameToLayer(layer) == gameObject.layer, () =>
					                   {
						                   if(settings.applyLayerTargetAndChild)
							                   ApplyLayerTargetAndChild(gameObject.transform, LayerMask.NameToLayer(layer));
						                   else
						                   {
							                   Undo.RegisterCompleteObjectUndo(gameObject, "Change Layer");
							                   gameObject.layer = LayerMask.NameToLayer(layer);
						                   }
					                   });
				}

				menuLayers.ShowAsContext();
				currentEvent.Use();
			}
		}

		private void ApplyLayerTargetAndChild(Transform _target, int _layer)
		{
			Undo.RegisterCompleteObjectUndo(_target.gameObject, "Change Layer");
			_target.gameObject.layer = _layer;

			for(int i = 0; i < _target.childCount; ++i)
				ApplyLayerTargetAndChild(_target.GetChild(i), _layer);
		}

		private void DisplayRowBackground(bool _nextRow = true)
		{
			if(currentEvent.type != EventType.Repaint)
				return;

			Rect rect = rowItem.rect;
			rect.xMin = -1;
			rect.width += 16;

			Color color = (rect.y / rect.height) % 2 == 0 ? ThemeData.colorRowEven : ThemeData.colorRowOdd;

			if(_nextRow)
				rect.y += rect.height;

			Color guiColor = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, Resources.PixelWhite, ScaleMode.StretchToFill);
			GUI.color = guiColor;
		}

		private void DisplayGrid()
		{
			if(currentEvent.type != EventType.Repaint)
				return;

			Rect rect = rowItem.rect;

			rect.xMin = GLOBAL_SPACE_OFFSET_LEFT;
			rect.y += 15;
			rect.width += 16;
			rect.height = 1;

			Color guiColor = GUI.color;
			GUI.color = ThemeData.colorGrid;
			GUI.DrawTexture(rect, Resources.PixelWhite, ScaleMode.StretchToFill);
			GUI.color = guiColor;
		}

		private void DisplayTreeView()
		{
			if(currentEvent.type != EventType.Repaint)
				return;

			Rect rect = rowItem.rect;

			rect.width = 40;
			rect.x -= 34;
			Transform t = rowItem.gameObject.transform.parent;

			Color guiColor = GUI.color;
			GUI.color = ThemeData.colorTreeView;

			if(t.childCount == 1 || t.GetChild(t.childCount - 1) == rowItem.gameObject.transform)
			{
				GUI.DrawTexture(rect, resources.GetIcon("icon_branch_L"), ScaleMode.ScaleToFit);
			}
			else
			{
				GUI.DrawTexture(rect, resources.GetIcon("icon_branch_T"), ScaleMode.ScaleToFit);
			}

			while(t != null)
			{
				if(t.parent == null)
					break;

				if(t == t.parent.GetChild(t.parent.childCount - 1))
				{
					t = t.parent;
					rect.x -= 14;

					continue;
				}

				rect.x -= 14;
				GUI.DrawTexture(rect, resources.GetIcon("icon_branch_I"), ScaleMode.ScaleToFit);
				t = t.parent;
			}

			GUI.color = guiColor;
		}

		private void RenameSceneInHierarchy()
		{
			string name = currentScene.name;

			if(name == "")
				return;

			if(!currentScene.isLoaded)
				name = $"{name} (not loaded)";

			tmpSceneContent.text = name == "" ? "Untitled" : name;
			Styles.TreeBoldLabel.CalcSize(tmpSceneContent);

			if(currentEvent.type == EventType.KeyDown &&
			   currentEvent.keyCode == KeyCode.F2 &&
			   rowItem.rect.Contains(currentEvent.mousePosition))
			{
				SceneRenamePopup.ShowPopup(currentScene);
			}
		}

		private void CollapseAll() { }

		private void DirtyScene(Scene _scene)
		{
			if(EditorApplication.isPlaying)
				return;

			EditorSceneManager.MarkSceneDirty(_scene);
		}

		private bool IsFirstElement(Rect _rect) => previousRowIndex > _rect.y / _rect.height;

		private bool IsFirstRow(Rect _rect) => _rect.y / _rect.height == 0;

		private int GetRowIndex(Rect _rect) => (int) (_rect.y / _rect.height);

		private bool InSelection(int _id) => Selection.Contains(_id);

		private bool IsElementDirty(int _id) => EditorUtility.IsDirty(_id);

		private Rect RectFromRight(Rect _rect, float _width, float _usedWidth)
		{
			_usedWidth += _width;
			_rect.x = _rect.x + _rect.width - _usedWidth;
			_rect.width = _width;

			return _rect;
		}

		private Rect RectFromRight(Rect _rect, float _width, ref float _usedWidth)
		{
			_usedWidth += _width;
			_rect.x = _rect.x + _rect.width - _usedWidth;
			_rect.width = _width;

			return _rect;
		}

		private Rect RectFromRight(Rect _rect, Vector2 _offset, float _width, ref float _usedWidth)
		{
			_usedWidth += _width;
			_rect.position += _offset;
			_rect.x = _rect.x + _rect.width - _usedWidth;
			_rect.width = _width;

			return _rect;
		}

		private Rect RectFromLeft(Rect _rect, float _width, float _usedWidth, bool _useXMin = true)
		{
			if(_useXMin)
				_rect.xMin = 0;
			_rect.x += _usedWidth;
			_rect.width = _width;

			return _rect;
		}

		private Rect RectFromLeft(Rect _rect, float _width, ref float _usedWidth, bool _useXMin = true)
		{
			if(_useXMin)
				_rect.xMin = 0;
			_rect.x += _usedWidth;
			_rect.width = _width;
			_usedWidth += _width;

			return _rect;
		}

		private Rect RectFromLeft(Rect _rect, Vector2 _offset, float _width, ref float _usedWidth, bool _useXMin = true)
		{
			if(_useXMin)
				_rect.xMin = 0;
			_rect.position += _offset;
			_rect.x += _usedWidth;
			_rect.width = _width;
			_usedWidth += _width;

			return _rect;
		}

		private void GUISeparator(Rect _rect, Color _color)
		{
			Color guiColor = GUI.color;
			GUI.color = _color;
			GUI.DrawTexture(_rect, Resources.PixelWhite, ScaleMode.StretchToFill);
			GUI.color = guiColor;
		}

		private struct WidthUse
		{
			public static WidthUse Zero => new(0, 0, 0);

			public float left;
			public float right;
			public float afterName;

			private WidthUse(float _left, float _right, float _afterName)
			{
				left = _left;
				right = _right;
				afterName = _afterName;
			}
		}

		private sealed class HierarchyWindow
		{
			public static readonly Dictionary<int, EditorWindow> instances = new();
			public static readonly List<HierarchyWindow> windows = new();

			private readonly int instanceID;
			public EditorWindow editorWindow;

			public HierarchyWindow(EditorWindow _editorWindow)
			{
				editorWindow = _editorWindow;

				instanceID = editorWindow.GetInstanceID();

				instances.Add(instanceID, editorWindow);
				windows.Add(this);

				Reflection();
			}

			public void Reflection() { }

			public void Dispose()
			{
				editorWindow = null;
				instances.Remove(instanceID);
				windows.Remove(this);
			}

			public TreeViewItem GetItemAndRowIndex(int _id, out int _row)
			{
				_row = -1;

				return null;
			}

			public void SetWindowTitle(string _value)
			{
				if(editorWindow == null)
					return;

				editorWindow.titleContent.text = _value;
			}
		}

		private sealed class RowItem
		{
			public int id = int.MinValue;
			public Rect rect;
			public Rect nameRect;
			public int rowIndex;
			public GameObject gameObject;
			public bool isNull = true;
			public bool isPrefab;
			public bool isPrefabMissing;
			public bool isRootObject;
			public bool isSelected;
			public bool isFirstRow;
			public bool isFirstElement;
			public bool isSeparator;
			public bool isFolder;
			public bool isDirty;
			public bool isMouseHovering;
			public HierarchyFolder hierarchyFolder;

			public string Name => isNull ? "Null" : gameObject.name;
			public int ChildCount => gameObject.transform.childCount;
			public Scene Scene => gameObject.scene;

			public bool IsStatic => !isNull && gameObject.isStatic;

			public RowItem() => isSelected = false;

			public void Dispose()
			{
				id = int.MinValue;
				gameObject = null;
				rect = Rect.zero;
				nameRect = Rect.zero;
				rowIndex = 0;
				isNull = true;
				isRootObject = false;
				isSelected = false;
				isFirstRow = false;
				isFirstElement = false;
				isSeparator = false;
				isFolder = false;
				isDirty = false;
				isMouseHovering = false;
			}
		}

		// ReSharper disable once ClassNeverInstantiated.Global
		internal sealed class Resources
		{
			private static Texture2D pixelWhite;

			public static Texture2D PixelWhite
			{
				get
				{
					if(pixelWhite == null)
					{
						pixelWhite = new Texture2D(1, 1, TextureFormat.RGBA32, false);
						pixelWhite.SetPixel(0, 0, Color.white);
						pixelWhite.Apply();
					}

					return pixelWhite;
				}
			}

			private static Texture2D alphaTexture;

			public static Texture2D AlphaTexture
			{
				get
				{
					if(alphaTexture == null)
					{
						alphaTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
						for(int x = 0; x < 16; ++x)
							for(int y = 0; y < 16; ++y)
								alphaTexture.SetPixel(x, y, Color.clear);
						alphaTexture.Apply();
					}

					return alphaTexture;
				}
			}

			private static Texture2D ramp8X8White;

			public static Texture2D Ramp8X8White
			{
				get
				{
					if(ramp8X8White == null)
					{
						ramp8X8White = new byte[]
						{
							137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 16,
							0, 0, 0, 16, 8, 6, 0, 0, 0, 31, 243, 255, 97, 0, 0, 0, 40, 73, 68, 65, 84, 56, 17, 99, 252,
							15, 4, 12, 12,
							12, 31, 8, 224, 143, 184, 228, 153, 128, 18, 20, 129, 81, 3, 24, 24, 70, 195, 96, 52, 12,
							64, 153, 104, 224,
							211, 1, 0, 153, 171, 18, 45, 165, 62, 165, 211, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130
						}.PNGImageDecode();
					}

					return ramp8X8White;
				}
			}

			internal static readonly Texture lockIconOn = EditorGUIUtility.IconContent("LockIcon-On").image;

			private static Texture folderIcon;

			public static Texture FolderIcon
			{
				get
				{
					if(folderIcon == null)
						folderIcon = EditorGUIUtility.IconContent("Folder Icon").image;

					return folderIcon;
				}
			}

			private static Texture emptyFolderIcon;

			public static Texture EmptyFolderIcon
			{
				get
				{
					if(emptyFolderIcon == null)
						emptyFolderIcon = EditorGUIUtility.IconContent("FolderEmpty Icon").image;

					return emptyFolderIcon;
				}
			}
		}

		internal static class Styles
		{
			internal static GUIStyle lineStyle = new("TV Line");

			internal static GUIStyle prDisabledLabel = new("PR DisabledLabel");

			internal static GUIStyle prPrefabLabel = new("PR PrefabLabel");

			internal static GUIStyle prDisabledPrefabLabel = new("PR DisabledPrefabLabel");

			internal static GUIStyle prBrokenPrefabLabel = new("PR BrokenPrefabLabel");

			internal static GUIStyle prDisabledBrokenPrefabLabel = new("PR DisabledBrokenPrefabLabel");

			internal static readonly GUIStyle tag = new()
			{
				padding = new RectOffset(3, 4, 0, 0),
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Italic,
				fontSize = 8,
				richText = true,
				border = new RectOffset(12, 12, 8, 8),
			};

			public static readonly GUIStyle layer = new()
			{
				padding = new RectOffset(3, 4, 0, 0),
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Italic,
				fontSize = 8,
				richText = true,
				border = new RectOffset(12, 12, 8, 8),
			};

			[Obsolete]
			internal static GUIStyle dirtyLabel = new(EditorStyles.label)
			{
				padding = new RectOffset(-1, 0, 0, 0),
				margin = new RectOffset(0, 0, 0, 0),
				border = new RectOffset(0, 0, 0, 0),
				alignment = TextAnchor.UpperLeft,
			};

			internal static readonly GUIStyle header = new(TreeBoldLabel)
			{
				richText = true,
				normal = new GUIStyleState { textColor = Color.white }
			};

			internal static GUIStyle TreeBoldLabel => TreeView.DefaultStyles.boldLabel;

			internal static readonly GUIStyle treeLabel = new(TreeView.DefaultStyles.label)
			{
				richText = true,
				normal = new GUIStyleState { textColor = Color.white }
			};
		}

		// ReSharper disable once ClassNeverInstantiated.Global
		internal sealed class MenuCommand
		{
			private const int PRIORITY = 200;

			[MenuItem("Tools/Hierarchy 2/Lock Selection %l", false, PRIORITY)]
			private static void SetNotEditableObject()
			{
				Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Set Selections Flag NotEditable");
				foreach(GameObject gameObject in Selection.gameObjects)
				{
					foreach(Component component in gameObject.GetComponents<Component>())
					{
						if(component)
						{
							Undo.RegisterCompleteObjectUndo(component, "Set Selections Flag NotEditable");
							component.hideFlags = HideFlags.NotEditable;
						}
					}
				}

				foreach(GameObject gameObject in Selection.gameObjects)
					gameObject.hideFlags = HideFlags.NotEditable;

				InternalEditorUtility.RepaintAllViews();
			}

			[MenuItem("Tools/Hierarchy 2/Lock Selection %l", true, PRIORITY)]
			private static bool ValidateSetNotEditableObject() => Selection.gameObjects.Length > 0;

			[MenuItem("Tools/Hierarchy 2/Unlock Selection %&l", false, PRIORITY)]
			private static void SetEditableObject()
			{
				Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Set Selections Flag Editable");
				foreach(GameObject gameObject in Selection.gameObjects)
				{
					foreach(Component component in gameObject.GetComponents<Component>())
					{
						if(component)
						{
							Undo.RegisterCompleteObjectUndo(component, "Set Selections Flag Editable");
							component.hideFlags = HideFlags.None;
						}
					}
				}

				foreach(GameObject gameObject in Selection.gameObjects)
					gameObject.hideFlags = HideFlags.None;

				InternalEditorUtility.RepaintAllViews();
			}

			[MenuItem("Tools/Hierarchy 2/Unlock Selection %&l", true, PRIORITY)]
			private static bool ValidateSetEditableObject() => Selection.gameObjects.Length > 0;

			[MenuItem("Tools/Hierarchy 2/Move Selection Up #w", false, PRIORITY)]
			private static void QuickSiblingUp()
			{
				GameObject gameObject = Selection.activeGameObject;

				if(gameObject == null)
					return;

				int index = gameObject.transform.GetSiblingIndex();
				if(index > 0)
				{
					Undo.SetTransformParent(gameObject.transform, gameObject.transform.parent, $"{gameObject.name} Parenting");

					gameObject.transform.SetSiblingIndex(--index);
				}
			}

			[MenuItem("Tools/Hierarchy 2/Move Selection Up #w", true)]
			private static bool ValidateQuickSiblingUp() => Selection.activeTransform != null;

			[MenuItem("Tools/Hierarchy 2/Move Selection Down #s", false, PRIORITY)]
			private static void QuickSiblingDown()
			{
				GameObject gameObject = Selection.activeGameObject;

				if(gameObject == null)
					return;

				Undo.SetTransformParent(gameObject.transform, gameObject.transform.parent, $"{gameObject.name} Parenting");

				int index = gameObject.transform.GetSiblingIndex();
				gameObject.transform.SetSiblingIndex(++index);
			}

			[MenuItem("Tools/Hierarchy 2/Move Selection Down #s", true, PRIORITY)]
			private static bool ValidateQuickSiblingDown() => Selection.activeTransform != null;

			[MenuItem("Tools/Hierarchy 2/Separator", priority = 0)]
			[MenuItem("GameObject/Hierarchy 2/Separator", priority = 0)]
			private static void CreateHeaderInstance(UnityEditor.MenuCommand _command)
			{
				GameObject gameObject = new($"{instance.settings.separatorStartWith}Separator");

				Undo.RegisterCreatedObjectUndo(gameObject, "Create Separator");

				Selection.activeTransform = gameObject.transform;
			}
		}
	}
}