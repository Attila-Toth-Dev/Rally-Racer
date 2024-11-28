using Generation;

using NaughtyAttributes;

using System;

using UnityEngine;

using Random = UnityEngine.Random;

namespace Tools
{
	public class TerrainGenerator : MonoBehaviour
	{
		[Header("Terrain Sizing")]
		[SerializeField, Range(1, 512)] private int xWidth;
		[SerializeField, Range(1, 512)] private int zWidth;
		[SerializeField] private int height = 20;

		[Header("Generation Settings")]
		[SerializeField] private float noiseScale = 20;

		[SerializeField] private int octaves;
		[Range(0, 1), SerializeField] private float persistence;
		[SerializeField] private float lacunarity;

		[Header("Noise Seed")]
		[SerializeField] private int seed;
		[SerializeField] private Vector2 offset;

		[Header("Debugging Tools")] public bool autoUpdate;
		[SerializeField] private bool isTerrainMoving = false;
		[SerializeField, EnableIf("isTerrainMoving")] private float offsetTime = 5f;

		private void Start() => offset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));

		private void Update()
		{
			Terrain terrain = GetComponent<Terrain>();
			terrain.terrainData = GenerateTerrain(terrain.terrainData);

			if(isTerrainMoving)
			{
				offset.x += Time.deltaTime * offsetTime;
				offset.y += Time.deltaTime * offsetTime;
			}
		}

		public TerrainData GenerateTerrain(TerrainData _terrainData)
		{
			_terrainData.heightmapResolution = xWidth + 1;

			_terrainData.size = new Vector3(xWidth, height, zWidth);
			_terrainData.SetHeights(0, 0, Noise.GenerateNoiseMap(xWidth, zWidth, seed, noiseScale, octaves, persistence, lacunarity, offset));

			return _terrainData;
		}

		private void OnValidate()
		{
			// Reset lacunarity value
			if(lacunarity < 1)
				lacunarity = 1;

			// Reset octaves value
			if(octaves < 0)
				octaves = 0;
		}
	}
}