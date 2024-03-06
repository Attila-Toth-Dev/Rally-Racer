using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

using Debug = System.Diagnostics.Debug;

namespace Hierarchy2
{
	public sealed class SelectionsRenamePopup : EditorWindow
	{
		private static EditorWindow window;
		private TextField textField;
		private EnumField enumModeField;
		private EditorHelpBox helpBox;

		private enum Mode
		{
			None,

			// ReSharper disable once UnusedMember.Local
			Number,
			NumberReverse
		}

		// ReSharper disable once UnusedMethodReturnValue.Global
		public new static SelectionsRenamePopup ShowPopup()
		{
			if(window == null)
				window = CreateInstance<SelectionsRenamePopup>();

			Vector2 v2 = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			window.position = new Rect(v2.x, v2.y, 200, 70);
			window.ShowPopup();
			window.Focus();

			SelectionsRenamePopup selectionsRenamePopup = window as SelectionsRenamePopup;
			Debug.Assert(selectionsRenamePopup != null, nameof(selectionsRenamePopup) + " != null");
			selectionsRenamePopup.textField.Query("unity-text-input").First().Focus();

			return selectionsRenamePopup;
		}

		public void OnLostFocus() => Close();

		private void OnEnable()
		{
			rootVisualElement.StyleBorderWidth(1);
			Color c = new Color32(58, 121, 187, 255);
			rootVisualElement.StyleBorderColor(c);
			rootVisualElement.StyleJustifyContent(Justify.Center);

			textField = new TextField
			{
				value = "New Name..."
			};
			rootVisualElement.Add(textField);
			textField.RegisterCallback<KeyUpEvent>(_evt =>
			{
				if(_evt.keyCode == KeyCode.Return) Apply();
			});

			enumModeField = new EnumField(new Mode())
			{
				label = "Mode",
				tooltip = "Rename with prefix."
			};
			enumModeField.labelElement.StyleMinWidth(64);
			enumModeField.labelElement.StyleMaxWidth(64);

			rootVisualElement.Add(enumModeField);

			helpBox = new EditorHelpBox("This mode require selections with the same parent.", MessageType.Info);
			helpBox.StyleDisplay(false);
			rootVisualElement.Add(helpBox);

			enumModeField.RegisterValueChangedCallback(_evt => { OnModeChanged(_evt.newValue); });

			Button apply = new(Apply)
			{
				text = nameof(Apply)
			};
			rootVisualElement.Add(apply);
		}

		private void OnModeChanged(Enum _mode)
		{
			Rect rect = window.position;
			rect.height = 70;

			if(!Equals(_mode, Mode.None))
			{
				rect.height = 70;
				if(!IsSelectionsSameParent())
				{
					helpBox.StyleDisplay(true);
					rect.height += 44;
				}
				else
				{
					helpBox.StyleDisplay(false);
				}
			}
			else
			{
				rect.height = 70;
				helpBox.StyleDisplay(false);
			}

			window.position = rect;
		}

		private bool IsSelectionsSameParent()
		{
			Transform parent = Selection.activeGameObject.transform.parent;
			foreach(GameObject gameObject in Selection.gameObjects)
			{
				if(parent != gameObject.transform.parent)
					return false;
			}

			return true;
		}

		private void Apply()
		{
			bool sameParent = IsSelectionsSameParent();

			List<GameObject> sortedSelections;

			int index = 0;

			if(Equals(enumModeField.value, Mode.NumberReverse))
			{
				sortedSelections = Selection.gameObjects.ToList()
				                            .OrderByDescending(_gameObject => _gameObject.transform.GetSiblingIndex()).ToList();
			}
			else
			{
				sortedSelections = Selection.gameObjects.ToList()
				                            .OrderBy(_gameObject => _gameObject.transform.GetSiblingIndex()).ToList();
			}

			foreach(GameObject gameObject in sortedSelections.Where(_gameObject => _gameObject != null))
			{
				Undo.RegisterCompleteObjectUndo(gameObject, "Selections Renaming...");

				if(!Equals(enumModeField.value, Mode.None) && sameParent)
					gameObject.name = $"{textField.value} ({index++})";
				else
					gameObject.name = textField.value;
			}

			rootVisualElement.StyleDisplay(DisplayStyle.None);

			Close();
		}
	}
}