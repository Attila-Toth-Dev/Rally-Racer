using System;

using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using Debug = System.Diagnostics.Debug;

namespace Hierarchy2
{
    public class SceneRenamePopup : EditorWindow
    {
        private static EditorWindow window;
        public Scene scene;
        private TextField nameField;

        [Obsolete("Use ShowPopup(Scene) instead.")]
        public new static SceneRenamePopup ShowPopup() => null;

        public static SceneRenamePopup ShowPopup(Scene _scene)
        {
            if (window == null)
                window = CreateInstance<SceneRenamePopup>();

            Vector2 v2 = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.position = new Rect(v2.x, v2.y, 200, 50);
            window.ShowPopup();
            window.Focus();

            SceneRenamePopup sceneRenamePopup = window as SceneRenamePopup;
            Debug.Assert(sceneRenamePopup != null, nameof(sceneRenamePopup) + " != null");
            sceneRenamePopup.scene = _scene;
            sceneRenamePopup.nameField.value = _scene.name;
            sceneRenamePopup.nameField.Query("unity-text-input").First().Focus();

            return sceneRenamePopup;
        }

        public void OnLostFocus() => Close();

        private void OnEnable()
        {
            rootVisualElement.StyleBorderWidth(1);
            Color c = new Color32(58, 121, 187, 255);
            rootVisualElement.StyleBorderColor(c);
            rootVisualElement.StyleJustifyContent(Justify.Center);

            nameField = new TextField();
            nameField.RegisterCallback<KeyUpEvent>(_evt =>
            {
                if (_evt.keyCode == KeyCode.Return) 
                    Apply();
            });
            rootVisualElement.Add(nameField);

            Button apply = new(Apply)
            {
                text = "Apply"
            };

            rootVisualElement.Add(apply);
        }

        private void Apply()
        {
            AssetDatabase.RenameAsset(scene.path, nameField.value);
            rootVisualElement.StyleDisplay(DisplayStyle.None);
            nameField.value = "";
            Close();
        }
    }
}