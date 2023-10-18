using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary> Records the state Prior to your change </summary>
public class UndoTiles : IUndo
{
    Tilemap tilemap;
    int layer;
    List<Vector2Int> tiles;
    Hashtable? overriddenTiles = null;

    public UndoTiles(Vector2Int tile, int layer, Tilemap tilemap)
    {
        this.tiles = new List<Vector2Int> { tile };
        this.layer = layer;
        this.tilemap = tilemap;
    }

    public UndoTiles(List<Vector2Int> tiles, int layer, Tilemap tilemap)
    {
        this.tiles = tiles;
        this.layer = layer;
        this.tilemap = tilemap;
    }

    public Hashtable RecordState()
    {
        if (overriddenTiles != null)
        {
            var result = overriddenTiles;
            overriddenTiles = null;
            return result;
        }

        Hashtable hash = new();
        for (int i = 0; i < tiles.Count(); i++)
        {
            Vector2Int tile = tiles.ElementAt(i);
            hash.Add(i + "_Tile", tilemap.GetTile(tile.x, tile.y, layer));
            hash.Add(i + "_Variation", tilemap.GetVariation(tile.x, tile.y, layer));
            hash.Add(i + "_Color", tilemap.GetColor(tile.x, tile.y, layer));
        }
        return hash;
    }

    public void ApplyState(Hashtable values)
    {
        for (int i = 0; i < tiles.Count(); i++)
        {
            Vector2Int tile = tiles.ElementAt(i);
            tilemap.SetTile(tile, (byte)values[i + "_Tile"], layer);
            tilemap.SetVariation(tile, (byte)values[i + "_Variation"], layer);
            tilemap.SetColor(tile, (Color32)values[i + "_Color"], layer);
        }
    }

    public void OnExitScope() { }

    internal void SetValues(Hashtable hashtable, List<Vector2Int> tiles)
    {
        overriddenTiles = new Hashtable(hashtable);
        // Extract tiles
        this.tiles = new List<Vector2Int>(tiles);
    }
}
