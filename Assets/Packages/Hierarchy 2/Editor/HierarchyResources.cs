using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace Hierarchy2
{
    internal class HierarchyResources : ScriptableObject
    {
        private Dictionary<string, Texture2D> dicIcons = new();
        public List<Texture2D> listIcons = new();

        public void GenerateKeyForAssets()
        {
            dicIcons.Clear();
            dicIcons = listIcons.ToDictionary(_texture2D => _texture2D.name);
        }

        public Texture2D GetIcon(string _key)
        {
            bool getResult = dicIcons.TryGetValue(_key, out Texture2D texture2D);
            if (!getResult)
                Debug.Log($"Icon with {_key} not found, return null.");
            return texture2D;
        }

        internal static HierarchyResources GetAssets()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(HierarchyResources)}");

            if (guids.Length > 0)
            {
                HierarchyResources asset = AssetDatabase.LoadAssetAtPath<HierarchyResources>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (asset != null)
                    return asset;
            }

            return null;
        }

        internal static HierarchyResources CreateAssets()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save as...", "Resources", "asset", "");
            if (path.Length > 0)
            {
                HierarchyResources settings = CreateInstance<HierarchyResources>();
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = settings;
                return settings;
            }

            return null;
        }
    }

    [CustomEditor(typeof(HierarchyResources))]
    internal class ResourcesInspector : Editor
    {
        private HierarchyResources resources;

        private void OnEnable() => resources = target as HierarchyResources;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Generate Key For Assets"))
                resources.GenerateKeyForAssets();
        }
    }
}