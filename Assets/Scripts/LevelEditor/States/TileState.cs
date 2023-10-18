using Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class TileState
{
    public enum Tools { Paint, Line, Rectangle }
    public static Tools currentTool = Tools.Paint;
    public static byte currentTile = 0;

    public static bool isDragging = false;
    public static Vector2Int clickDragStart;

    readonly static List<Vector2Int> paintedTiles = new();
    readonly static Hashtable paintedTilesData = new();

    [EditorState(EditorState.Tiles, StateUpdate.OnEnter)]
    public static void Start()
    {
        UpdateTilesList();
    }

    [EditorState(EditorState.Tiles, StateUpdate.Update)]
    public static void Update()
    {
        var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var tile = LevelEditor.Instance.Tilemap.WorldToTile(mousePos);

        switch (currentTool)
        {
            case Tools.Paint:
                if (!LevelEditor.MouseOnUI && Input.GetMouseButton(0))
                {
                    var oldTile = LevelEditor.Instance.Tilemap.GetTile(tile, (int)LevelEditor.currentLayer);
                    if (oldTile != currentTile)
                    {
                        //Undo.RegisterState(new UndoTiles(tile, currentLayer, LevelEditor.Instance.Tilemap), "Tile Paint");
                        int i = paintedTiles.Count;
                        paintedTiles.Add(tile);
                        paintedTilesData.Add(i + "_Tile", LevelEditor.Instance.Tilemap.GetTile(tile.x, tile.y, (int)LevelEditor.currentLayer));
                        paintedTilesData.Add(i + "_Variation", LevelEditor.Instance.Tilemap.GetVariation(tile.x, tile.y, (int)LevelEditor.currentLayer));
                        paintedTilesData.Add(i + "_Color", LevelEditor.Instance.Tilemap.GetColor(tile.x, tile.y, (int)LevelEditor.currentLayer));
                        LevelEditor.Instance.Tilemap.SetTile(tile, currentTile, (int)LevelEditor.currentLayer, false);
                        LevelEditor.Instance.Tilemap.UpdateChunk(tile, (int)LevelEditor.currentLayer, true);
                    }
                }
                if (paintedTiles.Count > 0)
                {
                    if (!LevelEditor.MouseOnUI && Input.GetMouseButtonUp(0))
                    {
                        var undo = new UndoTiles(null, (int)LevelEditor.currentLayer, LevelEditor.Instance.Tilemap);
                        undo.SetValues(paintedTilesData, paintedTiles);
                        Undo.RegisterState(undo, "Tile Paint");
                        paintedTiles.Clear();
                        paintedTilesData.Clear();
                    }
                }
                break;
            case Tools.Line:
                if (!LevelEditor.MouseOnUI && Input.GetMouseButtonDown(0))
                {
                    clickDragStart = tile;
                    isDragging = true;
                }
                else if (isDragging && Input.GetMouseButtonUp(0))
                {
                    isDragging = false;

                    var tiles = GetCellsOnLine(clickDragStart, tile);
                    Undo.RegisterState(new UndoTiles(tiles, (int)LevelEditor.currentLayer, LevelEditor.Instance.Tilemap), "Tile Line");
                    foreach (var point in tiles)
                        LevelEditor.Instance.Tilemap.SetTile(point, currentTile, (int)LevelEditor.currentLayer, true);
                }
                else if (isDragging)
                {
                }
                break;
            case Tools.Rectangle:
                if (!LevelEditor.MouseOnUI && Input.GetMouseButtonDown(0))
                {
                    clickDragStart = LevelEditor.Instance.Tilemap.WorldToTile(mousePos);
                    isDragging = true;
                }
                else if (isDragging && Input.GetMouseButtonUp(0))
                {
                    isDragging = false;

                    if ((clickDragStart != tile))
                    {
                        // Create Undo first
                        List<Vector2Int> tiles = new List<Vector2Int>();
                        for (int x = Math.Min(clickDragStart.x, tile.x); x <= Math.Max(clickDragStart.x, tile.x); x++)
                            for (int y = Math.Min(clickDragStart.y, tile.y); y <= Math.Max(clickDragStart.y, tile.y); y++)
                                tiles.Add(new Vector2Int(x, y));
                        Undo.RegisterState(new UndoTiles(tiles, (int)LevelEditor.currentLayer, LevelEditor.Instance.Tilemap), "Tile Rectangle");
                        // For every tile from the start to the end, set the tile
                        foreach (var point in tiles)
                            LevelEditor.Instance.Tilemap.SetTile(point, currentTile, (int)LevelEditor.currentLayer, true);
                    }
                }
                else if (isDragging)
                {
                }
                break;
        }

        if (LevelEditor.LayerOpacity)
        {
            LevelEditor.Instance.Tilemap.Layers[0].opacity = 0.5f;
            LevelEditor.Instance.Tilemap.Layers[1].opacity = 0.5f;
            LevelEditor.Instance.Tilemap.Layers[2].opacity = 0.5f;
            LevelEditor.Instance.Tilemap.Layers[(int)LevelEditor.currentLayer].opacity = 1f;
        }
        else
        {
            LevelEditor.Instance.Tilemap.Layers[0].opacity = 1f;
            LevelEditor.Instance.Tilemap.Layers[1].opacity = 1f;
            LevelEditor.Instance.Tilemap.Layers[2].opacity = 1f;
        }
    }

    [EditorState(EditorState.Tiles, StateUpdate.OnLeave)]
    public static void OnLeave()
    {
        LevelEditor.Instance.Tilemap.Layers[0].opacity = 1f;
        LevelEditor.Instance.Tilemap.Layers[1].opacity = 1f;
        LevelEditor.Instance.Tilemap.Layers[2].opacity = 1f;
        LevelEditor.Instance.Tilemap.RefreshAll();
    }

    [EditorState(EditorState.Tiles, StateUpdate.Draw)]
    public static void DrawState()
    {
        var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var tile = LevelEditor.Instance.Tilemap.WorldToTile(mousePos);

        Draw.Matrix = Matrix4x4.Translate(new Vector3(0, 0, -45));

        switch (currentTool)
        {
            case Tools.Paint:
                if (LevelEditor.MouseOnUI) return;

                // Draw a rectangle at the current tile
                var snapped = LevelEditor.Instance.Tilemap.TileToWorld(tile);
                Draw.Rectangle(new Rect(snapped, Vector2.one * LevelEditor.Instance.Tilemap.TileSize), new Color(0.25f, 0.25f, 0.5f, 0.9f));
                break;
            case Tools.Line:
                if (isDragging)
                {
                    var line = GetCellsOnLine(clickDragStart, tile);
                    foreach (var point in line)
                    {
                        var worldTile = LevelEditor.Instance.Tilemap.TileToWorld(point);
                        Draw.Rectangle(new Rect(worldTile, Vector2.one * LevelEditor.Instance.Tilemap.TileSize), new Color(0.25f, 0.25f, 0.5f, 0.9f));
                    }
                }
                break;
            case Tools.Rectangle:
                if (isDragging)
                {
                    // For every tile from the start to the end, set the tile
                    for (int x = Math.Min(clickDragStart.x, tile.x); x <= Math.Max(clickDragStart.x, tile.x); x++)
                        for (int y = Math.Min(clickDragStart.y, tile.y); y <= Math.Max(clickDragStart.y, tile.y); y++)
                        {
                            var worldTile = LevelEditor.Instance.Tilemap.TileToWorld(new Vector2Int(x, y));
                            Draw.Rectangle(new Rect(worldTile, Vector2.one * LevelEditor.Instance.Tilemap.TileSize), new Color(0.25f, 0.25f, 0.5f, 0.9f));
                        }
                }
                break;
        }
        Draw.Matrix = Matrix4x4.identity;
    }

    public static List<Vector2Int> GetCellsOnLine(Vector2Int start, Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        List<Vector2Int> cells = new List<Vector2Int>();

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            cells.Add(new Vector2Int(x0, y0));

            if (x0 == x1 && y0 == y1)
                break;

            int err2 = 2 * err;

            if (err2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (err2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return cells;
    }

    internal static void UpdateTilesList()
    {
        Transform content = LevelEditor.Instance.tileBrowser;
        // Delete all children except the first one
        for (int i = 1; i < content.childCount; i++)
            GameObject.Destroy(content.GetChild(i).gameObject);

        // Create a new button for each tile in this layer
        var tileset = LevelEditor.Instance.Tilemap.Layers[(int)LevelEditor.currentLayer].Tileset;
        for (int i = 0; i < tileset.Tiles.Count; i++)
        {
            var tile = tileset.Tiles[i];
            var button = GameObject.Instantiate(content.GetChild(0).gameObject, content);
            button.name = tile.Name + "Tile";
            //button.SetActive(true);
            button.GetComponent<LevelEditorTilesSelector>().Value = i + 1;
            button.GetComponentInChildren<TMPro.TMP_Text>().text = tile.Name;
            var rect = tile.TextureRectPixels();
            if (tile.Type == Tile.BlockType.Overlap)
                rect.height -= tileset.TileSize.y;
            Sprite sprite = Sprite.Create(tileset.Texture, rect, Vector2.one * 0.5f);
            button.transform.GetChild(1).GetComponent<Image>().sprite = sprite;
        }
    }
}
