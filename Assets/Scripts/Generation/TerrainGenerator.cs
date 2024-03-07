using NaughtyAttributes;

using UnityEngine;

namespace Tools
{
	public class TerrainGenerator : MonoBehaviour
	{
		[Header("Terrain Sizing")]
		[SerializeField] private int xWidth = 256;
		[SerializeField] private int zWidth = 256;
		[SerializeField] private int height = 20;

		[Header("Generation Settings")]
		[SerializeField] private float scale = 20;
		[SerializeField] private float offsetX = 100;
		[SerializeField] private float offsetY = 100;

		[Header("Debugging Tools")]
		[SerializeField] private bool isTerrainMoving = false;
		[SerializeField, EnableIf("isTerrainMoving")] private float offsetTime = 5f;

		private void Start()
		{
			offsetX = Random.Range(0f, 1000f);
			offsetY = Random.Range(0f, 1000f);
		}

		private void Update()
		{
			Terrain terrain = GetComponent<Terrain>();
			terrain.terrainData = GenerateTerrain(terrain.terrainData);

			if(isTerrainMoving)
			{
				offsetX += Time.deltaTime * offsetTime;
				offsetY += Time.deltaTime * offsetTime;
			}
		}

		private TerrainData GenerateTerrain(TerrainData _terrainData)
		{
			_terrainData.heightmapResolution = xWidth + 1;

			_terrainData.size = new Vector3(xWidth, height, zWidth);
			_terrainData.SetHeights(0, 0, GenerateHeights());

			return _terrainData;
		}

		private float[,] GenerateHeights()
		{
			float[,] heights = new float[xWidth, zWidth];
			for(int x = 0; x < xWidth; x++)
			{
				for(int z = 0; z < zWidth; z++)
				{
					heights[x, z] = CalculateHeight(x, z);
				}
			}

			return heights;
		}

		private float CalculateHeight(int _x, int _z)
		{
			float xCoord = (float) _x / xWidth * scale + offsetX;
			float yCoord = (float) _z / zWidth * scale + offsetY;

			return Mathf.PerlinNoise(xCoord, yCoord);
		}
	}
}