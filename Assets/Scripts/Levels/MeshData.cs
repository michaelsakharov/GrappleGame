using Shapes;
using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    private List<Vector3> vertices = new();
    private List<int>[] triangles;
    private List<Vector2> uv = new();
    private List<Color32> colors = new();

    public Mesh Mesh { get { if (mesh == null) { mesh = new Mesh(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; } return mesh; } }
    private Mesh mesh;

    public MeshData(int submeshCount)
    {
        triangles = new List<int>[submeshCount];
        for (int i = 0; i < submeshCount; i++)
            triangles[i] = new();
    }

    public void AddSquare(int submesh, Vector2 texturePos, Vector2 tileUnit, float vX0, float vY0, float vX1, float vY1, float z, float uvMinX, float uvMinY, float uvMaxX, float uvMaxY, Color32 color)
    {
        triangles[submesh].Add(vertices.Count);
        triangles[submesh].Add(vertices.Count + 1);
        triangles[submesh].Add(vertices.Count + 2);
        triangles[submesh].Add(vertices.Count);
        triangles[submesh].Add(vertices.Count + 2);
        triangles[submesh].Add(vertices.Count + 3);
        vertices.Add(new(vX0, vY0, z));
        vertices.Add(new(vX0, vY1, z));
        vertices.Add(new(vX1, vY1, z));
        vertices.Add(new(vX1, vY0, z));
        const float PP_OFFSET = 0.001f;
        uv.Add(new(tileUnit.x * texturePos.x + tileUnit.x * uvMinX + PP_OFFSET, tileUnit.y * texturePos.y + tileUnit.y * uvMinY + PP_OFFSET));
        uv.Add(new(tileUnit.x * texturePos.x + tileUnit.x * uvMinX + PP_OFFSET, tileUnit.y * texturePos.y + tileUnit.y * uvMaxY - PP_OFFSET));
        uv.Add(new(tileUnit.x * texturePos.x + tileUnit.x * uvMaxX - PP_OFFSET, tileUnit.y * texturePos.y + tileUnit.y * uvMaxY - PP_OFFSET));
        uv.Add(new(tileUnit.x * texturePos.x + tileUnit.x * uvMaxX - PP_OFFSET, tileUnit.y * texturePos.y + tileUnit.y * uvMinY + PP_OFFSET));
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    public void Clear()
    {
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = new();
        vertices = new();
        uv = new();
        colors = new();
    }

    public void ApplyToMesh()
    {
        Mesh.Clear();
        Mesh.SetVertices(vertices);
        Mesh.SetUVs(0, uv);
        Mesh.SetColors(colors);

        Mesh.subMeshCount = triangles.Length;
        for (int i = 0; i < triangles.Length; i++)
            Mesh.SetTriangles(triangles[i].ToArray(), i);

        Mesh.RecalculateNormals();
        Mesh.RecalculateTangents();
        Mesh.UploadMeshData(false);
    }
}
