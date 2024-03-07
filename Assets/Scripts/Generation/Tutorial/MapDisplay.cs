using Generation;

using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    // Draw Texture 
    public void DrawTexture(Texture2D _texture)
    {
        textureRenderer.sharedMaterial.mainTexture = _texture;
        textureRenderer.transform.localScale = new Vector3(_texture.width, 1, _texture.height);
    }

    // Generate Mesh
    public void DrawMesh(MeshData _meshData, Texture2D _texture)
    {
        meshFilter.sharedMesh = _meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = _texture;
    }
}
