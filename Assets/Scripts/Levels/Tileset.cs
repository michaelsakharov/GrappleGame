using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
[CreateAssetMenu(fileName = "Tileset")]
public class Tileset : ScriptableObject
{
    public Texture2D Texture;
    public Vector2Int TileSize;
    public List<Tile> Tiles = new List<Tile>();
    internal float tileTexUnitX = 0, tileTexUnitY = 0;

    public void OnValidate()
    {
        tileTexUnitX = (Texture == null) ? 0 : (float)TileSize.x / Texture.width;
        tileTexUnitY = (Texture == null) ? 0 : (float)TileSize.y / Texture.height;

        foreach (var tile in Tiles)
        {
            tile.tileset = this;
            tile.OnValidate();
        }

        // Prevent duplicate tile names, add an incrementing number to the last one
        bool hasDupe = true;
        while (hasDupe)
        {
            bool found = false;
            for (int i = 0; i < Tiles.Count; i++)
            {
                for (int j = Tiles.Count-1; j >= 0; j--)
                {
                    if (i == j) continue;
                    if (Tiles[i].Name == Tiles[j].Name)
                    {
                        Tiles[j].Name += " Dupe";
                        found = true;
                        break;
                    }
                }
            }
            if(!found)
                hasDupe = false;
        }
    }

    public Tile GetTile(int index) => ((index - 1) >= 0 && (index - 1) < Tiles.Count) ? Tiles[index - 1] : null;
    public Tile GetTile(string name) => Tiles.Find(n => n.Name == name);
}

[Serializable]
public class Tile
{
    public enum BlockType { Single, Overlap, Auto }

    public Tileset tileset;

    public string Name = "Tile";
    public BlockType Type;

    public bool HasCollider;
    public GameObject ColliderTemplate;
    public PhysicsMaterial2D PhysicsMaterial;

    public int VariationsCount;
    public Vector2Int Position;

    public bool BlendOverlap = true;

    public Material Material;

    public int GameObjectIdentifier = -1;

    public void OnValidate()
    {
        Material = Material != null ? Material : new Material(Shader.Find("Tilemap/Default"));
        if (Material.shader.name != "Tilemap/Default") Debug.LogWarning("Tile material is not using the 'Tilemap/Default' shader, Some features may not work as intended");

        GameObjectIdentifier = ColliderTemplate != null ? ColliderTemplate.GetInstanceID() : -1;
    }

    public Vector2 GetTexPos(int variation = 0) => (tileset.Texture == null) ? Vector2.zero : new Vector2(Position.x + (Type == BlockType.Overlap ? 2 : 1) * variation, Position.y);

    public Rect TextureRect() 
        => new(new(Position.x * tileset.tileTexUnitX, Position.y * tileset.tileTexUnitY), new((Type == BlockType.Overlap ? 2 : 1) * VariationsCount * tileset.tileTexUnitX, (Type == BlockType.Overlap ? 3 : 1) * tileset.tileTexUnitY));

    public Rect TextureRectPixels() 
        => new(new(Position.x * tileset.TileSize.x, Position.y * tileset.TileSize.y), new((Type == BlockType.Overlap ? 2 : 1) * VariationsCount * tileset.TileSize.x, (Type == BlockType.Overlap ? 3 : 1) * tileset.TileSize.y));

    public bool CanMergeColliders(Tile other)
    {
        if (other == null) return false;
        if (other.HasCollider != HasCollider) return false;
        //if (other.CollisionTypes != CollisionTypes) return false;
        if (other.ColliderTemplate != ColliderTemplate) return false;
        if (other.PhysicsMaterial != PhysicsMaterial) return false;
        return true;
    }
}

[Serializable]
public class Layer
{
    internal Tilemap tilemap;
    internal Material[] mats;

    public string Name;
    public Tileset Tileset;
    public int SortingOrder;
    public int ZOrder = 0;

    public string ChunkTag = "Untagged";
    public LayerMask ChunkLayerMask;
    public bool ForceDisableColliders = false;
    public PhysicsMaterial2D DefaultPhysicsMaterial;

    Dictionary<Vector2Int, Chunk> chunksCache;

    [NonSerialized] public float opacity = 1f;
    internal GameObject GO;

    internal void Initialize()
    {
        chunksCache = new();
        List<Material> matList = new List<Material>();
        Tileset.OnValidate();
        foreach (var tile in Tileset.Tiles)
        {
            tile.OnValidate();
            matList.Add(tile.Material);
        }
        mats = matList.ToArray();
    }

    public Chunk GetChunk(Vector2Int coord) => chunksCache.ContainsKey(coord) ? chunksCache[coord] : null;

    public bool HasChunk(Vector2Int coord) => chunksCache.ContainsKey(coord);

    public Chunk CreateChunk(Vector2Int coord)
    {
        if (HasChunk(coord)) return GetChunk(coord);

        var go = new GameObject($"Chunk {coord.x}, {coord.y}");
        go.transform.parent = GO.transform;
        go.transform.localPosition = new Vector3(coord.x * tilemap.ChunkTileCount * tilemap.TileSize, coord.y * tilemap.ChunkTileCount * tilemap.TileSize, 0);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Chunk chunk = go.AddComponent<Chunk>();
        chunk.Initialize(coord, this);
        chunksCache.Add(coord, chunk);

        return chunk;
    }

    public bool DestroyChunk(Vector2Int coord)
    {
        Chunk chunk = GetChunk(coord);
        if (chunk == null) return false; // Not Loaded
        if (chunk.isUpdating) return false; // Is Updating - Cannot Destroy

        chunksCache.Remove(coord);
        GameObject.Destroy(chunk.gameObject);

        return true;
    }

    internal void Refresh()
    {
        foreach (var chunk in chunksCache)
            chunk.Value.RequestUpdate();
    }


    public void Trim()
    {
        foreach (var chunk in chunksCache)
            if (chunk.Value.IsEmpty)
                DestroyChunk(chunk.Key);
    }

    public void Serialize(TileStream stream)
    {
        stream.WriteInt32(chunksCache.Count);
        foreach (var chunk in chunksCache)
        {
            stream.WriteInt32(chunk.Key.x);
            stream.WriteInt32(chunk.Key.y);
            stream.WriteInt32(chunk.Value.data.tileCount);
            stream.Write(chunk.Value.data.tiles);
            stream.Write(chunk.Value.data.variations);
            stream.Write(chunk.Value.data.bitmasks);
            for (int i = 0; i < chunk.Value.data.colors.Length; i++)
            {
                stream.WriteByte(chunk.Value.data.colors[i].r);
                stream.WriteByte(chunk.Value.data.colors[i].g);
                stream.WriteByte(chunk.Value.data.colors[i].b);
                stream.WriteByte(chunk.Value.data.colors[i].a);
            }
        }
    }

    public void Deserialize(TileStream stream)
    {
        int count = stream.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int x = stream.ReadInt32();
            int y = stream.ReadInt32();
            int tileCount = stream.ReadInt32();
            var data = new Chunk.Data(tileCount);
            stream.Read(data.tiles);
            stream.Read(data.variations);
            stream.Read(data.bitmasks);

            for (int j = 0; j < data.colors.Length; j++)
            {
                data.colors[j].r = (byte)stream.ReadByte();
                data.colors[j].g = (byte)stream.ReadByte();
                data.colors[j].b = (byte)stream.ReadByte();
                data.colors[j].a = (byte)stream.ReadByte();
            }

            Chunk chunk = CreateChunk(new Vector2Int(x, y));
            chunk.data = data;
            chunk.RequestUpdate();
        }
    }

}