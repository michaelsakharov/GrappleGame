using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

[Serializable]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    public class Data
    {
        public byte[] tiles;
        public byte[] bitmasks;
        public byte[] variations;
        public Color32[] colors;
        public int tileCount;

        public Data(int tileCount) 
        {
            this.tileCount = tileCount;
            int length = tileCount * tileCount;
            tiles = new byte[length];
            bitmasks = new byte[length];
            variations = new byte[length];
            colors = new Color32[length];
        }

        public Data(Data other)
        {
            // Copy all data from other
            tiles = new byte[other.tiles.Length];
            bitmasks = new byte[other.bitmasks.Length];
            variations = new byte[other.variations.Length];
            colors = new Color32[other.colors.Length];
            tileCount = other.tileCount;
            Array.Copy(other.tiles, tiles, other.tiles.Length);
            Array.Copy(other.bitmasks, bitmasks, other.bitmasks.Length);
            Array.Copy(other.variations, variations, other.variations.Length);
            Array.Copy(other.colors, colors, other.colors.Length);
        }

        public byte GetTile(Vector2Int relativeTile) => tiles[relativeTile.x + relativeTile.y * tileCount];
        public void SetTile(Vector2Int relativeTile, byte tileIdx = 1) => tiles[relativeTile.x + relativeTile.y * tileCount] = tileIdx;

        public byte GetBitmask(Vector2Int relativeTile) => bitmasks[relativeTile.x + relativeTile.y * tileCount];
        public void SetBitmask(Vector2Int relativeTile, byte bitmask) => bitmasks[relativeTile.x + relativeTile.y * tileCount] = bitmask;

        public byte GetVariation(Vector2Int relativeTile) => variations[relativeTile.x + relativeTile.y * tileCount];
        public void SetVariation(Vector2Int relativeTile, byte variation) => variations[relativeTile.x + relativeTile.y * tileCount] = variation;

        public Color32 GetColor(Vector2Int relativeTile) => colors[relativeTile.x + relativeTile.y * tileCount];
        public void SetColor(Vector2Int relativeTile, Color32 color) => colors[relativeTile.x + relativeTile.y * tileCount] = color;
    }

    internal Layer Layer;
    internal Vector2Int Coord;

    internal Data data;
    MeshData meshData;
    MaterialPropertyBlock prop;
    bool finishedUpdate = false;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Dictionary<int, List<Vector4>> colliderBoxes; 
    List<GameObject> colliders; 

    internal bool isUpdating = false;
    internal string error = string.Empty;
    static List<Chunk> chunksToUpdate = new List<Chunk>();


    // If all tiles are a value of 0
    public bool IsEmpty => data == null || data.tiles == null || data.tiles.Length == 0 || data.tiles.All(t => t == 0);

    public void Initialize(Vector2Int coord, Layer layer)
    {
        Coord = coord;

        // Setup Tile Arrays
        data = new(layer.tilemap.ChunkTileCount);

        // Setup Chunk Data and Components
        Layer = layer;
        meshData = new(layer.Tileset.Tiles.Count);
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter.sharedMesh = meshData.Mesh;
        gameObject.name = "Chunk " + coord.x + " " + coord.y;

        // Setup Collider List
        colliders ??= new(); // Create colliders list if null
        foreach (var collider in colliders)
            Destroy(collider); // Destroy all colliders if any

        // Set Tags and Layers
        if (Layer.tilemap.ShowChunksInHierarchy) gameObject.hideFlags &= ~HideFlags.HideInHierarchy;
        else                                     gameObject.hideFlags |= HideFlags.HideInHierarchy;
        gameObject.tag = Layer.ChunkTag;
        gameObject.layer = Layer.ChunkLayerMask;

        // Setup Renderer
        prop ??= new();
        meshRenderer.GetPropertyBlock(prop);
        if (Layer.Tileset.Texture != null) prop.SetTexture("_MainTex", Layer.Tileset.Texture);
        meshRenderer.SetPropertyBlock(prop);
        meshRenderer.sortingOrder = Layer.SortingOrder;
        meshRenderer.materials = Layer.mats;
    }

    public void LateUpdate()
    {
        if (finishedUpdate)
        {
            if (error.Length > 0)
            {
                Debug.Log(error);
                error = string.Empty;
                finishedUpdate = false;
                return;
            }

            meshData.ApplyToMesh();

            // Destroy all children
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            foreach (var goboxes in colliderBoxes)
            {
                // Create GameObject

                // Find the matching tile from goboxes.Key (GameObjectIdentifier) or GameObject.InstanceID
                GameObject find = null;
                if (goboxes.Key == -1) find = new GameObject();
                else
                {
                    foreach (var tile in Layer.Tileset.Tiles)
                    {
                        if (tile.GameObjectIdentifier == goboxes.Key)
                        {
                            find = tile.ColliderTemplate;
                            break;
                        }
                    }
                }

                GameObject go = goboxes.Key == -1 ? find : Instantiate(find);
                go.transform.SetParent(this.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                foreach (var box in goboxes.Value)
                {
                    // Create a BoxCollider2D
                    BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(box.z, box.w);
                    collider.offset = new Vector2(box.x + box.z / 2f, box.y + box.w / 2f);
                }

            }

            finishedUpdate = false;
        }
    }

    public bool RequestUpdate()
    {
        if (chunksToUpdate.Contains(this)) return false;
        chunksToUpdate.Add(this);
        return true;
    }

    public static void UpdateChunks()
    {
        List<Chunk> chunksToRemove = new List<Chunk>();
        foreach(var chunk in chunksToUpdate)
        {
            if (chunk == null) // Chunk was destroyed
            {
                chunksToRemove.Add(null);
                continue;
            }
            if (chunk.isUpdating) continue; // Skip if chunk is already updating
            if (chunk.finishedUpdate) continue; // It just finished an update, we need to wait for the Finisher to end

            chunksToRemove.Add(chunk);

            chunk.isUpdating = true;
            // This updates the chunk on a thread with a snapshot of the data, This way the chunk can be modified while updating
            var snapshot = new Data(chunk.data);
            ThreadPool.QueueUserWorkItem((state) => HandleChunkTask(chunk, snapshot)); 
        }
        foreach (var chunk in chunksToRemove)
            chunksToUpdate.Remove(chunk);
    }

    public static void UpdateAllImmediately()
    {
        // TODO: Implement
    }

    static void HandleChunkTask(Chunk chunk, Data snapshot)
    {
        try
        {
            chunk.meshData.Clear();
            chunk.colliderBoxes = new();
            Vector2 tileUnit = new Vector2(chunk.Layer.Tileset.tileTexUnitX, chunk.Layer.Tileset.tileTexUnitY);
            for (int i = 0; i < snapshot.tiles.Length; i++)
            {
                if (snapshot.tiles[i] == 0) continue; // Empty tile

                int x = i % chunk.Layer.tilemap.ChunkTileCount;
                int y = i / chunk.Layer.tilemap.ChunkTileCount;
                float tSize = chunk.Layer.tilemap.TileSize;
                float posx = x * tSize;
                float posy = y * tSize;

                Vector2Int relativeTile = new Vector2Int(x, y);

                int tileIdx = snapshot.tiles[i];
                var tile = chunk.Layer.Tileset.GetTile(snapshot.tiles[i]);
                if (tile == null) continue;

                var col = snapshot.GetColor(relativeTile);
                col.a = (byte)((float)col.a * Mathf.Clamp01(chunk.Layer.opacity));

                switch (tile.Type)
                {
                    case Tile.BlockType.Overlap:
                        AddOverlapBlock(snapshot, chunk.meshData, chunk.Layer, relativeTile, posx, posy, tileIdx, tile, tile.BlendOverlap, col);
                        break;
                    case Tile.BlockType.Auto:
                        // TODO
                        chunk.meshData.AddSquare(tileIdx - 1, tile.GetTexPos(), tileUnit, posx, posy, posx + tSize, posy + tSize, (tileIdx * 0.25f) * chunk.Layer.tilemap.ZOffset + chunk.Layer.ZOrder, 0, 0, 1, 1, col);
                        break;
                    default:
                        chunk.meshData.AddSquare(tileIdx - 1, tile.GetTexPos(), tileUnit, posx, posy, posx + tSize, posy + tSize, (tileIdx * 0.25f) * chunk.Layer.tilemap.ZOffset + chunk.Layer.ZOrder, 0, 0, 1, 1, col);
                        break;
                }
            }

            if (chunk.Layer.ForceDisableColliders == false)
                GenerateColliders(chunk);
        }
        catch(Exception e)
        {
            //Debug.LogError(e);
            chunk.error = e.ToSummaryString();
        }
        finally
        {
            chunk.isUpdating = false;
            chunk.finishedUpdate = true;
        }

    }

    static void AddOverlapBlock(Data data, MeshData meshData, Layer layer, Vector2Int tileCoord, float posX, float posY, int tileIdx, Tile tile, bool blend, Color32 color)
    {
        Vector2 tileUnit = new(layer.Tileset.tileTexUnitX, layer.Tileset.tileTexUnitY);
        var cellMax = posX + layer.tilemap.TileSize;
        var cellMin = posY + layer.tilemap.TileSize;
        var z = (tileIdx * 0.25f) * layer.tilemap.ZOffset + layer.ZOrder;
        var texPos = tile.GetTexPos(data.GetVariation(tileCoord));
        var bitmask = data.GetBitmask(tileCoord);

        void Add(float vX0, float vY0, float vX1, float vY1, float z, float uvMinX, float uvMinY, float uvMaxX, float uvMaxY)
            => meshData.AddSquare(tileIdx - 1, texPos, tileUnit, vX0, vY0, vX1, vY1, z, uvMinX, uvMinY, uvMaxX, uvMaxY, color);

        bool Bit(byte position) => (bitmask & position) == position;

        // Base tile
        Add(posX, posY, cellMax, cellMin, z, 0.5f, 0.5f, 1.5f, 1.5f);

        // 1  | 2  |  4
        // 8  |tile|  16
        // 32 | 64 |  128

        float halfSize = layer.tilemap.TileSize / 2f;
        
        if (!Bit(1) && !Bit(8) && !Bit(2)) Add(posX - halfSize, cellMin, posX, cellMin + halfSize, z, 0f, 1.5f, 0.5f, 2f); //Left top corner
        if (!Bit(4) && !Bit(16) && !Bit(2)) Add(cellMax, cellMin, cellMax + halfSize, cellMin + halfSize, z, 1.5f, 1.5f, 2f, 2f); //Right top corner
        if (!Bit(32) && !Bit(8) && !Bit(64)) Add(posX - halfSize, posY - halfSize, posX, posY, z, 0f, 0f, 0.5f, 0.5f); //Left bottom corner
        if (!Bit(128) && !Bit(16) && !Bit(64)) Add(cellMax, posY - halfSize, cellMax + halfSize, posY, z, 1.5f, 0f, 2f, 0.5f); //Right bottom corner

        if (blend)
        {
            //Left side
            if (!Bit(8))
            {
                if (Bit(1)) Add(posX - halfSize, posY + halfSize, posX, cellMin, z, 0.5f, 2.5f, 1f, 3f); //Left Top exists
                else Add(posX - halfSize, posY + halfSize, posX, cellMin, z, 0f, 1f, 0.5f, 1.5f); //Left Top empty

                if (Bit(32)) Add(posX - halfSize, posY, posX, posY + halfSize, z, 0.5f, 2f, 1f, 2.5f); //Left Bottom exists
                else Add(posX - halfSize, posY, posX, posY + halfSize, z, 0f, 0.5f, 0.5f, 1f); //Left Bottom empty
            }

            //Bottom side
            if (!Bit(64))
            {
                if (Bit(32)) Add(posX, posY - halfSize, posX + halfSize, posY, z, 0f, 2.5f, 0.5f, 3f); //Left Bottom exists
                else Add(posX, posY - halfSize, posX + halfSize, posY, z, 0.5f, 0f, 1f, 0.5f); //Left Bottom empty

                if (!Bit(128)) Add(cellMax - halfSize, posY - halfSize, cellMax, posY, z, 1f, 0f, 1.5f, 0.5f); //Right Bottom empty
            }

            //Right side
            if (!Bit(16))
            {
                if (Bit(128)) Add(cellMax, posY, cellMax + halfSize, posY + halfSize, z, 0f, 2f, 0.5f, 2.5f); //Right Bottom exists
                else Add(cellMax, posY, cellMax + halfSize, posY + halfSize, z, 1.5f, 0.5f, 2f, 1f); //Right Bottom empty

                if (!Bit(4)) Add(cellMax, posY + halfSize, cellMax + halfSize, cellMin, z, 1.5f, 1f, 2f, 1.5f); //Right Top empty
            }

            //Top side
            if (!Bit(2))
            {
                if (!Bit(4)) Add(cellMax - halfSize, cellMin, cellMax, cellMin + halfSize, z, 1f, 1.5f, 1.5f, 2f); //Right Top empty
                if (!Bit(1)) Add(posX, cellMin, posX + halfSize, cellMin + halfSize, z, 0.5f, 1.5f, 1f, 2f); //Left Top Empty
            }
        }
        else
        {
            if (!Bit(8)) Add(posX - halfSize, posY, posX, cellMin, z, 0f, 0.5f, 0.5f, 1.5f); //Left
            if (!Bit(64)) Add(posX, posY - halfSize, cellMax, posY, z, 0.5f, 0f, 1.5f, 0.5f); //Bottom
            if (!Bit(16)) Add(cellMax, posY, cellMax + halfSize, cellMin, z, 1.5f, 0.5f, 2f, 1.5f); //Right
            if (!Bit(2)) Add(posX, cellMin, cellMax, cellMin + halfSize, z, 0.5f, 1.5f, 1.5f, 2f); //Top
        }
    }


    #region Colliders

    static void GenerateColliders(Chunk chunk)
    {
        int res = chunk.Layer.tilemap.ChunkTileCount;
        bool[,] tested = new bool[res, res];

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                var tileIdx = chunk.data.GetTile(new Vector2Int(x, y));
                if (tileIdx == 0) continue;
                var tile = chunk.Layer.Tileset.GetTile(tileIdx);
                bool hasCollider = tile != null && tile.HasCollider;
                if (!tested[x, y] && hasCollider)
                {
                    
                    int colliderWidth = 1;
                    int colliderHeight = 1;

                    // Expand the collider horizontally
                    for (int i = x + 1; i < res && !tested[i, y]; i++)
                    {
                        var n = chunk.data.GetTile(new Vector2Int(i, y));
                        if (!tile.CanMergeColliders(chunk.Layer.Tileset.GetTile(n)))
                            break;

                        colliderWidth++;
                        tested[i, y] = true;
                    }

                    // Expand the collider vertically
                    for (int j = y + 1; j < res; j++)
                    {
                        bool canExpand = true;
                        for (int i = x; i < x + colliderWidth; i++)
                        {
                            var n = chunk.data.GetTile(new Vector2Int(i, j));
                            if (tested[i, j] || !tile.CanMergeColliders(chunk.Layer.Tileset.GetTile(n)))
                            {
                                canExpand = false;
                                break;
                            }
                        }
                    
                        if (canExpand)
                        {
                            colliderHeight++;
                            for (int i = x; i < x + colliderWidth; i++)
                                tested[i, j] = true;
                        }
                        else
                            break;
                    }

                    float width = colliderWidth * chunk.Layer.tilemap.TileSize;
                    float height = colliderHeight * chunk.Layer.tilemap.TileSize;
                    float fx = x * chunk.Layer.tilemap.TileSize;
                    float fy = y * chunk.Layer.tilemap.TileSize;
                    if (chunk.colliderBoxes.ContainsKey(tile.GameObjectIdentifier))
                        chunk.colliderBoxes[tile.GameObjectIdentifier].Add(new Vector4(fx, fy, width, height));
                    else
                        chunk.colliderBoxes.Add(tile.GameObjectIdentifier, new List<Vector4>() { new Vector4(fx, fy, width, height) });
                }
            }
        }
    }

    #endregion


    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1, 0, 0, 0.25f);
        var tm = Layer.tilemap;
        var x = transform.position.x;
        var y = transform.position.y;
        var pos = transform.position;
        var size = tm.TileSize * tm.ChunkTileCount;
        UnityEditor.Handles.DrawLine(pos, new Vector3(x + size, y));
        UnityEditor.Handles.DrawLine(new Vector3(x + size, y), new Vector3(x + size, y + size));
        UnityEditor.Handles.DrawLine(pos, new Vector3(x, y + size));
        UnityEditor.Handles.DrawLine(new Vector3(x, y + size), new Vector3(x + size, y + size));
#endif
    }

}
