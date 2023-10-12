using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class Tilemap : MonoBehaviour
{
    public List<Layer> Layers = new();

    public bool ShowChunksInHierarchy = true;

    public int ChunkTileCount = 8;
    public float TileSize = 0.5f;
    public float ZOffset = 1;
    bool hasInitialize = false;

    internal int updatingChunkCounter = 0;

    public void OnValidate()
    {
        foreach (var layer in Layers) layer.tilemap = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (hasInitialize) return;
        hasInitialize = true;

        foreach (var layer in Layers)
        {
            layer.Initialize();
            layer.GO = new GameObject(layer.Name);
            layer.GO.transform.parent = transform;
            layer.GO.transform.localPosition = Vector3.zero;
        }

        //// Place a bunch of random tiles
        //for (int l = 0; l < Layers.Count; l++)
        //    for (int i = 0; i < 1500; i++)
        //    {
        //        Vector2Int tile = new(Random.Range(-50, 50), Random.Range(-50, 50));
        //        SetTile(tile, (byte)Random.Range(0, Layers[l].Tileset.Tiles.Count + 1), l);
        //    }
    }

    void Update()
    {
        Chunk.UpdateChunks();
    }


    public Vector2Int WorldToTile(Vector2 worldPosition) => new(Mathf.FloorToInt(worldPosition.x / TileSize), Mathf.FloorToInt(worldPosition.y / TileSize));
    public Vector2 TileToWorld(Vector2Int tilePosition) => (Vector2)tilePosition * TileSize;

    public Vector2Int TileToChunk(Vector2Int tilePosition)
    {
        int x = Mathf.FloorToInt(tilePosition.x);
        int y = Mathf.FloorToInt(tilePosition.y);
        int chunkX, chunkY;

        if (x >= 0) chunkX = x / ChunkTileCount;
        else        chunkX = (x + 1) / ChunkTileCount - 1;

        if (y >= 0) chunkY = y / ChunkTileCount;
        else        chunkY = (y + 1) / ChunkTileCount - 1;

        return new Vector2Int(chunkX, chunkY);
    }

    public Vector2Int TileRelativeToChunk(Vector2Int tilePosition)
    {
        int localX;
        int localY;

        if (tilePosition.x >= 0)
            localX = tilePosition.x % ChunkTileCount;
        else
            localX = ChunkTileCount - 1 - (-tilePosition.x - 1) % ChunkTileCount;

        if (tilePosition.y >= 0)
            localY = tilePosition.y % ChunkTileCount;
        else
            localY = ChunkTileCount - 1 - (-tilePosition.y - 1) % ChunkTileCount;

        return new Vector2Int(localX, localY);
    }

    public Vector2Int WorldToChunk(Vector2 worldPosition) => new(Mathf.FloorToInt(worldPosition.x / (TileSize * ChunkTileCount)), Mathf.FloorToInt(worldPosition.y / (TileSize * ChunkTileCount)));
    public Vector2 ChunkToWorld(Vector2Int chunkPosition) => (Vector2)chunkPosition * TileSize * ChunkTileCount;

    public Chunk GetChunkFromWorld(Vector2 worldPosition, int layer) => Layers[layer].GetChunk(WorldToChunk(worldPosition));
    public Chunk CreateChunkFromWorld(Vector2 worldPosition, int layer) => Layers[layer].CreateChunk(WorldToChunk(worldPosition));
    public Chunk GetChunkFromTile(Vector2Int tilePosition, int layer) => Layers[layer].GetChunk(TileToChunk(tilePosition));
    public Chunk CreateChunkFromTile(Vector2Int tilePosition, int layer) => Layers[layer].CreateChunk(TileToChunk(tilePosition));


    public void SetTile(Vector2Int worldTile, byte tileID, int layer, bool update = true)
    {

        Chunk chunk = CreateChunkFromTile(worldTile, layer);
        Vector2Int relativeTile = TileRelativeToChunk(worldTile);

        if (chunk.data.GetTile(relativeTile) == tileID)
            return;

        chunk.data.SetTile(relativeTile, tileID);
        chunk.data.SetColor(relativeTile, Color.white);

        if (tileID != 0)
        {
            var tile = Layers[layer].Tileset.GetTile(tileID);
            if (tile == null) return;
            chunk.data.SetVariation(relativeTile, (byte)Random.Range(0, tile.VariationsCount));
        }

        UpdateBitmask(worldTile, layer);

        // If we are setting a tile to 0 (deleting a tile) check if the chunk is empty, if so destroy it
        if (tileID == 0 && chunk.IsEmpty) Layers[layer].DestroyChunk(chunk.Coord);
        else if (update) chunk.RequestUpdate();
    }

    public byte GetTile(int x, int y, int layer) => GetTile(new Vector2Int(x, y), layer);
    public byte GetTile(Vector2Int worldTile, int layer)
    {
        Chunk chunk = GetChunkFromTile(worldTile, layer);
        if (chunk == null) return 0; // Chunk not loaded
        Vector2Int relativeTile = TileRelativeToChunk(worldTile);
        return chunk.data.GetTile(relativeTile);
    }

    public void SetColor(Vector2Int worldTile, Color32 color, int layer)
    {
        Chunk chunk = CreateChunkFromTile(worldTile, layer);
        Vector2Int relativeTile = TileRelativeToChunk(worldTile);
        chunk.data.SetColor(relativeTile, color);
        chunk.RequestUpdate();
    }
    public Color32 GetColor(int x, int y, int layer) => GetColor(new Vector2Int(x, y), layer);
    public Color32 GetColor(Vector2Int worldTile, int layer)
    {
        Chunk chunk = GetChunkFromTile(worldTile, layer);
        if (chunk == null) return Color.white; // Chunk not loaded
        Vector2Int relativeTile = TileRelativeToChunk(worldTile);
        return chunk.data.GetColor(relativeTile);
    }

    public void UpdateChunk(Vector2Int tile, int layer, bool andNeighbors = true)
    {
        Chunk chunk = GetChunkFromTile(tile, layer);
        chunk.RequestUpdate();
        if (andNeighbors)
        {
            Chunk up = GetChunkFromTile(tile + Vector2Int.up, layer);
            if (up != chunk) up.RequestUpdate();
            Chunk down = GetChunkFromTile(tile + Vector2Int.down, layer);
            if (down != chunk) down.RequestUpdate();
            Chunk left = GetChunkFromTile(tile + Vector2Int.left, layer);
            if (left != chunk) left.RequestUpdate();
            Chunk right = GetChunkFromTile(tile + Vector2Int.right, layer);
            if (right != chunk) right.RequestUpdate();
        }
    }

    public void WaitForChunks()
    {
        // await all chunks to finish updating
        while (updatingChunkCounter > 0)
        {
            // Dispatch chunk updates
            Chunk.UpdateChunks();
            // Force call all chunk LateUpdate() methods since unity doesn't do it since were blocking the main thread
            foreach (var layer in Layers)
                foreach (var chunk in layer.chunksCache.Values)
                    chunk.LateUpdate();
        }
    }

    public void RefreshAll()
    {
        foreach (var layer in Layers)
            layer.Refresh();
    }

    byte CalculateBitmask(Vector2Int tile, int layer)
    {
        var tileIdx = GetTile(tile, layer);
        if (tileIdx == 0) return 0;

        byte bitmask = 0;
        int x = tile.x, y = tile.y;
        if (GetTile(x - 1, y + 1, layer) == tileIdx) bitmask |= 1;
        if (GetTile(x, y + 1, layer) == tileIdx) bitmask |= 2;
        if (GetTile(x + 1, y + 1, layer) == tileIdx) bitmask |= 4;
        if (GetTile(x - 1, y, layer) == tileIdx) bitmask |= 8;
        if (GetTile(x + 1, y, layer) == tileIdx) bitmask |= 16;
        if (GetTile(x - 1, y - 1, layer) == tileIdx) bitmask |= 32;
        if (GetTile(x, y - 1, layer) == tileIdx) bitmask |= 64;
        if (GetTile(x + 1, y - 1, layer) == tileIdx) bitmask |= 128;
        return bitmask;
    }

    void UpdateBitmask(Vector2Int worldTile, int layer, bool updateNeighbors = true)
    {
        var newBitmask = CalculateBitmask(worldTile, layer);
        Chunk chunk = GetChunkFromTile(worldTile, layer);
        if (chunk == null) return; // Chunk not loaded
        chunk.data.SetBitmask(TileRelativeToChunk(worldTile), newBitmask);
        if (updateNeighbors)
        {
            // Update neighbors as well
            UpdateBitmask(worldTile + new Vector2Int(0, 1), layer, false);
            UpdateBitmask(worldTile + new Vector2Int(0, -1), layer, false);
            UpdateBitmask(worldTile + new Vector2Int(1, 0), layer, false);
            UpdateBitmask(worldTile + new Vector2Int(-1, 0), layer, false);
            // Diagonals
            UpdateBitmask(worldTile + new Vector2Int(-1, 1), layer, false);
            UpdateBitmask(worldTile + new Vector2Int(1, 1), layer, false);
            UpdateBitmask(worldTile + new Vector2Int(-1, -1), layer, false);
            UpdateBitmask(worldTile + new Vector2Int(1, -1), layer, false);
        }
    }

    public byte[] Serialize()
    {
        using System.IO.MemoryStream ms = new System.IO.MemoryStream();
        using TileStream ts = new TileStream(ms);
        foreach (var layer in Layers) layer.Serialize(ts);
        return ms.ToArray();
    }

    public void Deserialize(byte[] data)
    {
        // Call start just to be sure that the tilemap and layers are fully initialized
        Start();
        using System.IO.MemoryStream ms = new System.IO.MemoryStream(data);
        using TileStream ts = new TileStream(ms);
        foreach (var layer in Layers) layer.Deserialize(ts);
    }
}
