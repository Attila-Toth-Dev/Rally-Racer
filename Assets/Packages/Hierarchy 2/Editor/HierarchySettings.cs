using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Hierarchy2
{
	[Serializable]
	[SuppressMessage("ReSharper", "NotAccessedField.Global")]
	internal class HierarchySettings : ScriptableObject
	{
		[Serializable]
		public struct ThemeData
		{
			public Color colorRowEven;
			public Color colorRowOdd;
			public Color colorGrid;
			public Color colorTreeView;
			public Color colorLockIcon;
			public Color tagColor;
			public Color layerColor;

			[FormerlySerializedAs("comSelBGColor")]
			public Color comSelBgColor;

			public Color selectionColor;
			public Color colorHeaderTitle;
			public Color colorHeaderBackground;

			public ThemeData(ThemeData _themeData)
			{
				colorRowEven = _themeData.colorRowEven;
				colorRowOdd = _themeData.colorRowOdd;
				colorGrid = _themeData.colorGrid;
				colorTreeView = _themeData.colorTreeView;
				colorLockIcon = _themeData.colorLockIcon;
				tagColor = _themeData.tagColor;
				layerColor = _themeData.layerColor;
				comSelBgColor = _themeData.comSelBgColor;
				selectionColor = _themeData.selectionColor;
				colorHeaderTitle = _themeData.colorHeaderTitle;
				colorHeaderBackground = _themeData.colorHeaderBackground;
			}

			public void BlendMultiply(Color _blend)
			{
				colorRowEven *= _blend;
				colorRowOdd *= _blend;
				colorGrid *= _blend;
				colorTreeView *= _blend;
				colorLockIcon *= _blend;
				tagColor *= _blend;
				layerColor *= _blend;
				comSelBgColor *= _blend;
				selectionColor *= _blend;
				colorHeaderTitle *= _blend;
				colorHeaderBackground *= _blend;
			}
		}

		///<summary>Define background color using prefix.</summary>
		[Serializable]
		public struct InstantBackgroundColor
		{
			public bool active;
			public bool useStartWith, useTag, useLayer;
			public string startWith;
			public string tag;
			public LayerMask layer;
			public Color color;
		}

		public enum ComponentSize
		{
			Small,
			Normal,
			Large
		}

		public enum ElementAlignment
		{
			AfterName,
			Right
		}

		[Flags]
		public enum ContentDisplay
		{
			Component = 1 << 0,
			Tag = 1 << 1,
			Layer = 1 << 2
		}

		private static HierarchySettings instance;

		public ThemeData personalTheme;
		public ThemeData professionalTheme;
		public ThemeData playmodeTheme;
		private bool useThemePlaymode;

		public ThemeData UsedTheme
		{
			get
			{
				if(EditorApplication.isPlayingOrWillChangePlaymode)
				{
					if(!useThemePlaymode)
					{
						playmodeTheme = new ThemeData(EditorGUIUtility.isProSkin ? professionalTheme : personalTheme);
						playmodeTheme.BlendMultiply(GUI.color);
						useThemePlaymode = true;
					}

					return playmodeTheme;
				}

				useThemePlaymode = false;

				return EditorGUIUtility.isProSkin ? professionalTheme : personalTheme;
			}
		}

		[HideInInspector] public bool activeHierarchy = true;
		public bool displayCustomObjectIcon = true;
		public bool displayTreeView = true;
		public bool displayRowBackground = true;
		public bool displayGrid;
		[HideInInspector] public bool displayStaticButton = true;
		public int offSetIconAfterName = 8;
		public bool displayComponents = true;
		public ElementAlignment componentAlignment = ElementAlignment.AfterName;

		public enum ComponentDisplayMode
		{
			All = 0,
			ScriptOnly = 1,
			Specified = 2,
			Ignore = 3
		}

		public ComponentDisplayMode componentDisplayMode = ComponentDisplayMode.Ignore;
		public string[] components = { "Transform", "RectTransform" };
		[HideInInspector] public int componentLimited;
		[Range(12, 16)] public int componentSize = 16;
		public int componentSpacing;
		public bool displayTag = true;
		public ElementAlignment tagAlignment = ElementAlignment.AfterName;
		public bool displayLayer = true;
		public ElementAlignment layerAlignment = ElementAlignment.AfterName;
		[HideInInspector] public bool applyStaticTargetAndChild = true;
		public bool applyTagTargetAndChild;
		public bool applyLayerTargetAndChild = true;
		public string separatorStartWith = "--->";
		public string separatorDefaultTag = "Untagged";
		public bool useInstantBackground;

		public List<InstantBackgroundColor> instantBackgroundColors = new();

		public bool onlyDisplayWhileMouseEnter;
		public ContentDisplay contentDisplay = ContentDisplay.Component | ContentDisplay.Tag | ContentDisplay.Layer;

		public delegate void OnSettingsChangedCallback(string _param);

		public OnSettingsChangedCallback onSettingsChanged;

		public void OnSettingsChanged(string _param = "")
		{
			switch(_param)
			{
				case nameof(componentSize):
					if(componentSize % 2 != 0) componentSize -= 1;

					break;

				case nameof(componentSpacing):
					if(componentSpacing < 0) componentSpacing = 0;

					break;
			}

			onSettingsChanged?.Invoke(_param);
			hideFlags = HideFlags.None;
		}

		[SettingsProvider]
		private static SettingsProvider UIElementSettingsProvider()
		{
			const float TITLE_MARGIN_BOTTOM = 8;
			const float CONTENT_MARGIN_LEFT = 10;
			const float TITLE_MARGIN_TOP = 14;

			SettingsProvider provider = new("Project/Hierarchy", SettingsScope.Project)
			{
				label = "Hierarchy",

				activateHandler = (_, _rootElement) =>
				{
					HierarchySettings settings = GetAssets();

					HorizontalLayout horizontalLayout = new()
					{
						style =
						{
							backgroundColor = new Color(0, 0, 0, 0.2f),
							paddingTop = 4,
							paddingBottom = 10
						}
					};
					_rootElement.Add(horizontalLayout);

					Label hierarchyTitle = new("Hierarchy");
					hierarchyTitle.StyleFontSize(20);
					hierarchyTitle.StyleMargin(10, 0, 2, 2);
					hierarchyTitle.StyleFont(FontStyle.Bold);
					horizontalLayout.Add(hierarchyTitle);

					Label importButton = new()
					{
						text = "  Import",
						style =
						{
							unityFontStyleAndWeight = FontStyle.Italic
						}
					};
					Color importExportButtonColor = new Color32(102, 157, 246, 255);
					importButton.style.color = importExportButtonColor;
					importButton.RegisterCallback<PointerUpEvent>(_ => instance.ImportFromJson());
					horizontalLayout.Add(importButton);

					Label exportButton = new()
					{
						text = "| Export",
						style =
						{
							unityFontStyleAndWeight = FontStyle.Italic,
							color = importExportButtonColor
						}
					};
					exportButton.RegisterCallback<PointerUpEvent>(_ => instance.ExportToJson());
					horizontalLayout.Add(exportButton);

					ScrollView scrollView = new();
					_rootElement.Add(scrollView);

					VerticalLayout verticalLayout = new();
					verticalLayout.StylePadding(8, 8, 8, 8);
					scrollView.Add(verticalLayout);

					Label obj = new Label("Object");
					obj.StyleFont(FontStyle.Bold);
					obj.StyleMargin(0, 0, 0, TITLE_MARGIN_BOTTOM);
					verticalLayout.Add(obj);

					Toggle displayCustomObjectIcon = new("Display Custom Icon")
					{
						value = settings.displayCustomObjectIcon
					};
					displayCustomObjectIcon.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayCustomObjectIcon = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayCustomObjectIcon));
					});
					displayCustomObjectIcon.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayCustomObjectIcon);

					Label view = new("View");
					view.StyleFont(FontStyle.Bold);
					view.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
					verticalLayout.Add(view);

					Toggle displayRowBackground = new("Display RowBackground")
					{
						value = settings.displayRowBackground
					};
					displayRowBackground.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayRowBackground = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayRowBackground));
					});
					displayRowBackground.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayRowBackground);

					Toggle displayTreeView = new("Display TreeView")
					{
						value = settings.displayTreeView
					};
					displayTreeView.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayTreeView = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayTreeView));
					});
					displayTreeView.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayTreeView);

					Toggle displayGrid = new("Display Grid")
					{
						value = settings.displayGrid
					};
					displayGrid.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayGrid = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayGrid));
					});
					displayGrid.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayGrid);

					Label components = new Label("Components");
					components.StyleFont(FontStyle.Bold);
					components.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
					verticalLayout.Add(components);

					Toggle displayComponents = new("Display Components Icon")
					{
						value = settings.displayComponents
					};
					displayComponents.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayComponents = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayComponents));
					});
					displayComponents.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayComponents);

					EnumField componentAlignment = new(settings.componentAlignment)
					{
						label = "Component Alignment"
					};
					componentAlignment.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.componentAlignment = (ElementAlignment) _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.componentAlignment));
					});
					componentAlignment.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(componentAlignment);

					EnumField componentDisplayMode = new(settings.componentDisplayMode)
					{
						label = "Component Display Mode"
					};
					componentDisplayMode.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(componentDisplayMode);

					TextField componentListInput = new("Components")
					{
						value = string.Join(" ", settings.components)
					};
					componentListInput.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(componentListInput);
					componentListInput.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.components = _evt.newValue.Split(' ');
						settings.OnSettingsChanged(nameof(settings.components));
					});
					componentDisplayMode.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.componentDisplayMode = (ComponentDisplayMode) _evt.newValue;
						switch(settings.componentDisplayMode)
						{
							case ComponentDisplayMode.Specified:
								componentListInput.StyleDisplay(true);

								break;

							case ComponentDisplayMode.Ignore:
								componentListInput.StyleDisplay(true);

								break;

							case ComponentDisplayMode.All:
								componentListInput.StyleDisplay(false);

								break;

							case ComponentDisplayMode.ScriptOnly:
								componentListInput.StyleDisplay(false);

								break;

							default:
								throw new ArgumentOutOfRangeException();
						}

						settings.OnSettingsChanged(nameof(settings.componentDisplayMode));
					});

					ComponentSize componentSizeEnum = settings.componentSize switch
					{
						12 => ComponentSize.Small,
						14 => ComponentSize.Normal,
						16 => ComponentSize.Large,
						_ => ComponentSize.Normal
					};

					EnumField componentSize = new(componentSizeEnum)
					{
						label = "Component Size"
					};
					componentSize.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					componentSize.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.componentSize = _evt.newValue switch
						{
							ComponentSize.Small => 12,
							ComponentSize.Normal => 14,
							ComponentSize.Large => 16,
							_ => settings.componentSize
						};

						settings.OnSettingsChanged(nameof(settings.componentSize));
					});
					verticalLayout.Add(componentSize);

					IntegerField componentSpacing = new()
					{
						label = "Component Spacing",
						value = settings.componentSpacing
					};
					componentSpacing.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					componentSpacing.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.componentSpacing = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.componentSpacing));
					});
					verticalLayout.Add(componentSpacing);

					Label tagAndLayer = new Label("Tag And Layer");
					tagAndLayer.StyleFont(FontStyle.Bold);
					tagAndLayer.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
					verticalLayout.Add(tagAndLayer);

					Toggle displayTag = new Toggle("Display Tag")
					{
						value = settings.displayTag
					};
					displayTag.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayTag = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayTag));
					});
					displayTag.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayTag);

					Toggle applyTagTargetAndChild = new("Tag Recursive Change")
					{
						value = settings.applyTagTargetAndChild
					};
					applyTagTargetAndChild.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.applyTagTargetAndChild = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.applyTagTargetAndChild));
					});
					applyTagTargetAndChild.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(applyTagTargetAndChild);

					EnumField tagAlignment = new(settings.tagAlignment)
					{
						label = "Tag Alignment"
					};
					tagAlignment.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.tagAlignment = (ElementAlignment) _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.tagAlignment));
					});
					tagAlignment.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(tagAlignment);

					Toggle displayLayer = new("Display Layer")
					{
						value = settings.displayLayer
					};
					displayLayer.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.displayLayer = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.displayLayer));
					});
					displayLayer.style.marginTop = 8;
					displayLayer.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(displayLayer);

					Toggle applyLayerTargetAndChild = new("Layer Recursive Change")
					{
						value = settings.applyLayerTargetAndChild
					};
					applyLayerTargetAndChild.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.applyLayerTargetAndChild = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.applyLayerTargetAndChild));
					});
					applyLayerTargetAndChild.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(applyLayerTargetAndChild);

					EnumField layerAlignment = new(settings.layerAlignment)
					{
						label = "Layer Alignment"
					};
					layerAlignment.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.layerAlignment = (ElementAlignment) _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.layerAlignment));
					});
					layerAlignment.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(layerAlignment);

					Label advanced = new("Advanced");
					advanced.StyleFont(FontStyle.Bold);
					advanced.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
					verticalLayout.Add(advanced);

					TextField separatorStartWith = new()
					{
						label = "Separator StartWith",
						value = settings.separatorStartWith
					};
					separatorStartWith.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					separatorStartWith.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.separatorStartWith = _evt.newValue == String.Empty ? "--->" : _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.separatorStartWith));
					});
					verticalLayout.Add(separatorStartWith);

					TagField headerDefaultTag = new()
					{
						label = "Separator Default Tag",
						value = settings.separatorDefaultTag
					};
					headerDefaultTag.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.separatorDefaultTag = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.separatorDefaultTag));
					});
					headerDefaultTag.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					headerDefaultTag.StyleMarginBottom(4);
					verticalLayout.Add(headerDefaultTag);

					SerializedObject serializedSetting = new(settings);
					IMGUIContainer instantBackgroundIMGUI = new(() =>
					{
						EditorGUILayout.BeginHorizontal();
						settings.useInstantBackground = EditorGUILayout.Toggle("Use Instant Background", settings.useInstantBackground);
						EditorGUILayout.BeginVertical("helpbox");
						EditorGUILayout.PropertyField(serializedSetting.FindProperty(nameof(instantBackgroundColors)), GUIContent.none);
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
						serializedSetting.ApplyModifiedProperties();
					});
					instantBackgroundIMGUI.StyleMarginTop(7);
					instantBackgroundIMGUI.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(instantBackgroundIMGUI);

					Toggle onlyDisplayWhileMouseHovering = new("Display Hovering")
					{
						tooltip = "Only display while mouse hovering",
						value = settings.onlyDisplayWhileMouseEnter
					};
					onlyDisplayWhileMouseHovering.StyleMarginTop(7);
					onlyDisplayWhileMouseHovering.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.onlyDisplayWhileMouseEnter = _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.onlyDisplayWhileMouseEnter));
					});
					onlyDisplayWhileMouseHovering.StyleMarginLeft(CONTENT_MARGIN_LEFT);
					verticalLayout.Add(onlyDisplayWhileMouseHovering);

					EnumFlagsField contentMaskEnumFlags = new(settings.contentDisplay)
					{
						label = "Content Mask"
					};
					contentMaskEnumFlags.StyleDisplay(onlyDisplayWhileMouseHovering.value);
					onlyDisplayWhileMouseHovering.RegisterValueChangedCallback(_evt => { contentMaskEnumFlags.StyleDisplay(_evt.newValue); });
					contentMaskEnumFlags.RegisterValueChangedCallback(_evt =>
					{
						Undo.RecordObject(settings, "Change Settings");

						settings.contentDisplay = (ContentDisplay) _evt.newValue;
						settings.OnSettingsChanged(nameof(settings.contentDisplay));
					});
					contentMaskEnumFlags.style.marginLeft = CONTENT_MARGIN_LEFT;
					verticalLayout.Add(contentMaskEnumFlags);

					Label theme = new Label("Theme");
					theme.StyleFont(FontStyle.Bold);
					theme.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
					verticalLayout.Add(theme);

					if(EditorApplication.isPlayingOrWillChangePlaymode)
					{
						EditorHelpBox themeWarningPlaymode = new("This setting only available on edit mode.", MessageType.Info);
						verticalLayout.Add(themeWarningPlaymode);
					}
					else
					{
						EditorHelpBox selectionColorHelpBox = new(
						                                          "Theme selection color require editor assembly recompile to take affect.\nBy selecting any script, right click -> Reimport. it will force the editor to recompile.",
						                                          MessageType.Info);
						selectionColorHelpBox.StyleDisplay(false);
						verticalLayout.Add(selectionColorHelpBox);

						ColorField colorRowEven = new("Row Even")
						{
							value = settings.UsedTheme.colorRowEven
						};
						colorRowEven.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorRowEven.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorRowEven = _evt.newValue;
							else
								settings.personalTheme.colorRowEven = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorRowEven);

						ColorField colorRowOdd = new("Row Odd")
						{
							value = settings.UsedTheme.colorRowOdd
						};
						colorRowOdd.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorRowOdd.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorRowOdd = _evt.newValue;
							else
								settings.personalTheme.colorRowOdd = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorRowOdd);

						ColorField colorGrid = new("Grid Color")
						{
							value = settings.UsedTheme.colorGrid
						};
						colorGrid.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorGrid.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorGrid = _evt.newValue;
							else
								settings.personalTheme.colorGrid = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorGrid);

						ColorField colorTreeView = new("TreeView")
						{
							value = settings.UsedTheme.colorTreeView
						};
						colorTreeView.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorTreeView.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorTreeView = _evt.newValue;
							else
								settings.personalTheme.colorTreeView = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorTreeView);

						ColorField colorLockIcon = new("Lock Icon")
						{
							value = settings.UsedTheme.colorLockIcon
						};
						colorLockIcon.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorLockIcon.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorLockIcon = _evt.newValue;
							else
								settings.personalTheme.colorLockIcon = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorLockIcon);

						ColorField tagColor = new("Tag Text")
						{
							value = settings.UsedTheme.tagColor
						};
						tagColor.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						tagColor.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.tagColor = _evt.newValue;
							else
								settings.personalTheme.tagColor = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(tagColor);

						ColorField layerColor = new("Layer Text")
						{
							value = settings.UsedTheme.layerColor
						};
						layerColor.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						layerColor.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.layerColor = _evt.newValue;
							else
								settings.personalTheme.layerColor = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(layerColor);

						ColorField colorHeaderTitle = new("Header Title")
						{
							value = settings.UsedTheme.colorHeaderTitle
						};
						colorHeaderTitle.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorHeaderTitle.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorHeaderTitle = _evt.newValue;
							else
								settings.personalTheme.colorHeaderTitle = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorHeaderTitle);

						ColorField colorHeaderBackground = new("Header Background")
						{
							value = settings.UsedTheme.colorHeaderBackground
						};
						colorHeaderBackground.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						colorHeaderBackground.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.colorHeaderBackground = _evt.newValue;
							else
								settings.personalTheme.colorHeaderBackground = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(colorHeaderBackground);

						ColorField comSelBgColor = new("Component Selection")
						{
							value = settings.UsedTheme.comSelBgColor
						};
						comSelBgColor.StyleMarginLeft(CONTENT_MARGIN_LEFT);
						comSelBgColor.RegisterValueChangedCallback(_evt =>
						{
							Undo.RecordObject(settings, "Change Settings");

							if(EditorGUIUtility.isProSkin)
								settings.professionalTheme.comSelBgColor = _evt.newValue;
							else
								settings.personalTheme.comSelBgColor = _evt.newValue;

							settings.OnSettingsChanged();
						});
						verticalLayout.Add(comSelBgColor);
					}

					Undo.undoRedoPerformed -= OnUndoRedoPerformed;
					Undo.undoRedoPerformed += OnUndoRedoPerformed;
				},

				deactivateHandler = () => Undo.undoRedoPerformed -= OnUndoRedoPerformed,

				keywords = new HashSet<string>(new[] { "Hierarchy" })
			};

			return provider;
		}

		private static void OnUndoRedoPerformed()
		{
			SettingsService.NotifySettingsProviderChanged();

			if(instance != null)
			{
				instance.onSettingsChanged?.Invoke(nameof(instance.components)); // Refresh components on undo & redo
			}
		}

		internal static HierarchySettings GetAssets()
		{
			if(instance != null)
				return instance;

			string[] guids = AssetDatabase.FindAssets($"t:{nameof(HierarchySettings)}");

			foreach(string guid in guids)
			{
				instance = AssetDatabase.LoadAssetAtPath<HierarchySettings>(AssetDatabase.GUIDToAssetPath(guid));

				if(instance != null)
					return instance;
			}

			return instance = CreateAssets();
		}

		internal static HierarchySettings CreateAssets()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save as...", "Hierarchy 2 Settings", "asset", "");
			if(path.Length > 0)
			{
				HierarchySettings settings = CreateInstance<HierarchySettings>();
				AssetDatabase.CreateAsset(settings, path);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = settings;

				return settings;
			}

			return null;
		}

		internal bool ImportFromJson()
		{
			string path = EditorUtility.OpenFilePanel("Import Hierarchy 2 settings", "", "json");
			if(path.Length > 0)
			{
				string json;
				using(StreamReader sr = new(path))
				{
					json = sr.ReadToEnd();
				}

				if(string.IsNullOrEmpty(json)) return false;

				JsonUtility.FromJsonOverwrite(json, this);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				return true;
			}

			return false;
		}

		internal TextAsset ExportToJson()
		{
			string path = EditorUtility.SaveFilePanelInProject("Export Hierarchy 2 settings as...", "Hierarchy 2 Settings", "json", "");
			if(path.Length > 0)
			{
				string json = JsonUtility.ToJson(instance, true);
				using(StreamWriter sw = new StreamWriter(path))
				{
					sw.Write(json);
				}

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.FocusProjectWindow();
				TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				Selection.activeObject = asset;

				return asset;
			}

			return null;
		}
	}

	[CustomEditor(typeof(HierarchySettings))]
	internal class SettingsInspector : Editor
	{
		// ReSharper disable once NotAccessedField.Local
		private HierarchySettings settings;

		private void OnEnable() => settings = target as HierarchySettings;

		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Go to Edit -> Project Settings -> Hierarchy tab", MessageType.Info);
			if(GUILayout.Button("Open Settings"))
				SettingsService.OpenProjectSettings("Project/Hierarchy");
		}
	}
}