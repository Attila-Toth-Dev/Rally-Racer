using UnityEngine;

namespace Generation
{
	public static class MeshGenerator
	{
		public static MeshData GenerateTerrainMesh(float[,] _heightMap)
		{
			int width = _heightMap.GetLength(0);
			int height = _heightMap.GetLength(1);

			float topLeftX = (width - 1) / -2f;
			float topLeftZ = (height - 1) / -2f;
			
			MeshData meshData = new MeshData(width, height);
			int vertexIndex = 0;
			
			for(int y = 0; y < height; y++)
			{
				for(int x = 0; x < width; x++)
				{
					meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, _heightMap[x, y], topLeftZ - y);
					meshData.uvs[vertexIndex] = new Vector2(x / (float) width, x / (float) height);
					
					if(x < width - 1 && y < height - 1)
					{
						meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width);
						meshData.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
					}
					
					vertexIndex++;
				}
			}

			return meshData;
		}
	}

	public class MeshData
	{
		public Vector3[] vertices;
		public int[] triangles;
		public Vector2[] uvs;

		private int triangleIndex;
		
		public MeshData(int _meshWidth, int _meshHeight)
		{
			vertices = new Vector3[_meshWidth * _meshHeight];
			uvs = new Vector2[_meshWidth * _meshHeight];
			triangles = new int[(_meshWidth - 1) * (_meshHeight - 1) * 6];
		}

		public void AddTriangle(int _a, int _b, int _c)
		{
			triangles[triangleIndex] = _a;
			triangles[triangleIndex + 1] = _b;
			triangles[triangleIndex + 2] = _c;

			triangleIndex += 3;
		}

		public Mesh CreateMesh()
		{
			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.uv = uvs;
			
			mesh.RecalculateNormals();

			return mesh;
		}
	} 
}