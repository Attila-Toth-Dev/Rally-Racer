using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

using Debug = System.Diagnostics.Debug;

namespace Hierarchy2
{
	public class InstantInspector : EditorWindow
	{
		public enum FillMode
		{
			Default,
			Add
		}

		private ScrollView scrollView;
		private readonly List<Editor> editors = new();
		private readonly Color objectNameColor = new Color32(58, 121, 187, 255);
		private List<Object> components = new();
		// ReSharper disable once CollectionNeverQueried.Local
		private readonly List<Foldout> foldouts = new();

		public static InstantInspector OpenEditor()
		{
			InstantInspector window = GetWindow<InstantInspector>("Instant Inspector");
			window.titleContent.image = EditorGUIUtility.IconContent("UnityEditor.InspectorWindow").image;

			return window;
		}

		private void OnEnable()
		{
			if(rootVisualElement.childCount == 0)
			{
				scrollView = new ScrollView(ScrollViewMode.Vertical);
				rootVisualElement.Add(scrollView);
			}
		}

		private void OnDisable() => Dispose();

		private void Dispose()
		{
			components.Clear();

			while(scrollView.childCount > 0)
			{
				scrollView[0].RemoveFromHierarchy();
			}

			while(editors.Count > 0)
			{
				DestroyImmediate(editors[0]);
				editors.RemoveAt(0);
			}
		}

		public void Fill(List<Object> _objects, FillMode _fillMode = FillMode.Default)
		{
			_objects = new List<Object>(_objects);

			if(_fillMode == FillMode.Add)
			{
				components.RemoveAll(_item => _item == null);
				foreach(Object component in components.Where(_component => !_objects.Contains(_component)))
					_objects.Add(component);
			}

			Dispose();
			components = _objects;
			foldouts.Clear();

			foreach(Object component in components)
			{
				Foldout foldout = new(component.GetType().Name)
				{
					Value = components.Count == 1
				};
				foldout.name = foldout.Title;
				foldouts.Add(foldout);

				foldout.imageElement.image = EditorGUIUtility.ObjectContent(component, component.GetType()).image;
				foldout.headerElement.RegisterCallback<MouseUpEvent>(_evt =>
				{
					if(_evt.button == 1)
					{
						Rect rect = new(foldout.headerElement.layout)
						{
							position = _evt.mousePosition
						};
						HierarchyEditor.DisplayObjectContextMenu(rect, component, 0);
						_evt.StopPropagation();
					}
				});

				Label objectName = new(component.name);
				objectName.StyleTextColor(objectNameColor);
				objectName.RegisterCallback<MouseUpEvent>(_evt =>
				{
					if(_evt.button == 0)
					{
						EditorGUIUtility.PingObject(component);
						Selection.activeObject = component;
						_evt.StopPropagation();
					}
				});
				foldout.headerElement.Add(objectName);

				Image remove = new()
				{
					image = EditorGUIUtility.IconContent("winbtn_win_close").image
				};
				remove.StyleSize(13, 13);
				remove.StylePosition(Position.Absolute);
				remove.StyleRight(8);
				remove.StyleAlignSelf(Align.Center);
				remove.RegisterCallback<MouseUpEvent>(_evt =>
				{
					if(_evt.button == 0)
					{
						if(component != null)
							components.Remove(component);
						else
							components.RemoveAll(_item => _item == null);

						Fill(new List<Object>(components));
						_evt.StopPropagation();
					}
				});
				foldout.headerElement.Add(remove);

				bool isMat = component is Material;

				Editor editor;

				if(isMat)
					editor = Editor.CreateEditor(component) as MaterialEditor;
				else
					editor = Editor.CreateEditor(component);

				Debug.Assert(editor != null, nameof(editor) + " != null");
				VisualElement inspector = editor.CreateInspectorGUI() ?? new IMGUIContainer(() =>
				{
					bool tempState = EditorGUIUtility.wideMode;
					float tempWidth = EditorGUIUtility.labelWidth;

					EditorGUIUtility.wideMode = true;

					if(component is Transform)
						EditorGUIUtility.labelWidth = 64;

					if(editor.target != null)
					{
						if(isMat)
						{
							MaterialEditor maEditor = editor as MaterialEditor;

							EditorGUILayout.BeginVertical();
							Debug.Assert(maEditor != null, nameof(maEditor) + " != null");
							if(maEditor.PropertiesGUI())
								maEditor.PropertiesChanged();
							EditorGUILayout.EndVertical();
						}
						else
						{
							editor.OnInspectorGUI();
						}

						objectName.StyleTextColor(objectNameColor);
					}
					else
					{
						objectName.StyleTextColor(Color.red);
						EditorGUILayout.HelpBox("Reference not found.", MessageType.Info);
					}

					EditorGUIUtility.wideMode = tempState;
					EditorGUIUtility.labelWidth = tempWidth;
				});

				inspector.style.marginLeft = 16;
				inspector.style.marginRight = 2;
				inspector.style.marginTop = 4;

				foldout.Add(inspector);
				editors.Add(editor);
				scrollView.Add(foldout);

				if(isMat)
				{
					IMGUIContainer preview = new IMGUIContainer(() =>
					{
						editor.DrawPreview(new Rect(0, 0, inspector.layout.size.x,
						                            Mathf.Clamp(inspector.layout.width / 2, 64, 200)));
					});
					preview.StyleMarginTop(4);
					preview.StretchToParentWidth();
					inspector.RegisterCallback<GeometryChangedEvent>(_ => { preview.StyleHeight(Mathf.Clamp(inspector.layout.width / 2, 64, 200)); });
					preview.StylePosition(Position.Relative);
					preview.name = "Material Preview";
					foldout.Add(preview);
				}
			}

			Repaint();
		}
	}
}