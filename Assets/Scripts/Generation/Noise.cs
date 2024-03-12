using UnityEngine;

namespace Generation
{
	public static class Noise
	{
		/// <summary>Generate a Noise Map with values passed through, using the built in Perlin Noise function.</summary>
		/// <param name="_mapWidth">The width of the set map.</param>
		/// <param name="_mapHeight">The height of the set map</param>
		/// <param name="_seed">The seeded value of the map.</param>
		/// <param name="_scale">The scale of which the noise if produced.</param>
		/// <param name="_octaves">The number of loops that make the code run, making for finer detail.</param>
		/// <param name="_persistence"></param>
		/// <param name="_lacunarity">Determines the change in frequency between octaves. Smaller values result in coarser noise, finer results in more uniform noise. Default is 2</param>
		/// <param name="_offset">The offset of the noise, being used to move across noise values.</param>
		public static float[,] GenerateNoiseMap(int _mapWidth, int _mapHeight, int _seed, float _scale, 
		                                        int _octaves, float _persistence, float _lacunarity, Vector2 _offset)
		{
			// Initialize Noise Map
			float[,] noiseMap = new float[_mapWidth, _mapHeight];
			
			// Seeded Random
			System.Random prng = new System.Random(_seed);
			Vector2[] octaveOffsets = new Vector2[_octaves];
			for(int i = 0; i < _octaves; i++)
			{
				float offsetX = prng.Next(-10000, 10000) + _offset.x;
				float offsetY = prng.Next(-10000, 10000) + _offset.y;
				octaveOffsets[i] = new Vector2(offsetX, offsetY);
			}
			
			// If scalar value is 0 or less than
			if(_scale <= 0)
				_scale = 0.0001f;

			float maxNoiseHeight = float.MinValue;
			float minNoiseHeight = float.MaxValue;

			float halfWidth = _mapWidth / 2f;
			float halfHeight = _mapHeight / 2f;
			
			// Go through each width and height pixel
			// within the necessary map dimensions
			for(int y = 0; y < _mapHeight; y++)
			{
				for(int x = 0; x < _mapWidth; x++)
				{
					float amplitude = 1;
					float frequency = 1;
					float noiseHeight = 0;
					
					for(int i = 0; i < _octaves; i++)
					{
						// Set the X and Y values with necessary scaling
						// and frequency + offsetting for movement between noise texture
						float sampleX = (x - halfWidth) / _scale * frequency + octaveOffsets[i].x;
						float sampleY = (y - halfHeight) / _scale * frequency + octaveOffsets[i].y;

						// Calculate Perlin Noise value
						float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
						noiseHeight += perlinValue * amplitude;

						amplitude *= _persistence;
						frequency *= _lacunarity;
					}

					if(noiseHeight > maxNoiseHeight)
						maxNoiseHeight = noiseHeight;
					
					else if(noiseHeight < minNoiseHeight)
						minNoiseHeight = noiseHeight;
					
					noiseMap[x, y] = noiseHeight;
				}
			}

			// Normalizing Noise Map
			for(int y = 0; y < _mapHeight; y++)
				for(int x = 0; x < _mapWidth; x++)
					noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);

			return noiseMap;
		}
	}
}