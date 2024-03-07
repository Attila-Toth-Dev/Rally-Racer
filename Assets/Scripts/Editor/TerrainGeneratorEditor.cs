using Tools;

using UnityEditor;

using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator terrainGen = (TerrainGenerator) target;

        if(DrawDefaultInspector())
            if(terrainGen.autoUpdate)
                terrainGen.GenerateTerrain(terrainGen.GetComponent<Terrain>().terrainData);
        
        if(GUILayout.Button("Generate"))
            terrainGen.GenerateTerrain(terrainGen.GetComponent<Terrain>().terrainData);
    }
    
}
