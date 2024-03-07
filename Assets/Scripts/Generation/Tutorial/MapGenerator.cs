using Generation;

using System;

using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        MeshGenerator
    }

    public DrawMode drawMode;
    
    [Header("Map Generation Setup")]
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    [Header("Noise Settings")]
    public int octaves;
    [Range(0, 1)] public float persistence;
    public float lacunarity;

    [Header("Noise Seed")] 
    public int seed;
    public Vector2 offset;
    
    [Header("Auto Update Map Generation")]
    public bool autoUpdate;

    public TerrainType[] regions;
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);

        Color[] colourMap = new Color[mapWidth * mapHeight];
        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];

                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapWidth + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }
        
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if(drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight));
        else if(drawMode == DrawMode.MeshGenerator)
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight));
    }

    private void OnValidate()
    {
        // Make it so width and height
        // can never be below value 1
        if(mapWidth < 1)
            mapWidth = 1;
        if(mapHeight < 1)
            mapHeight = 1;

        // Reset lacunarity value
        if(lacunarity < 1)
            lacunarity = 1;

        // Reset octaves value
        if(octaves < 0)
            octaves = 0;
    }
}

[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}
