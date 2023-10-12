using Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Records the state Prior to your change </summary>
//public class UndoTiles : IUndo
//{
//    public IEnumerable<TileState.TrackTileChanges> tiles;
//
//    public UndoTiles(TileState.TrackTileChanges tiles)
//    {
//        this.tiles = new TileState.TrackTileChanges[] { tiles };
//    }
//
//    public UndoTiles(IEnumerable<TileState.TrackTileChanges> tiles)
//    {
//        this.tiles = tiles;
//    }
//
//    public Hashtable RecordState()
//    {
//        Hashtable hash = new();
//
//        for (int i = 0; i < targets.Count(); i++)
//        {
//            Transform target = targets.ElementAt(i);
//            hash.Add(target, new TransformState(target));
//        }
//
//        for (int i = 0; i < SelectState.selectedPositions.Count; i++)
//            hash.Add(i, SelectState.selectedPositions[i]);
//
//        return hash;
//    }
//
//    public void ApplyState(Hashtable values)
//    {
//        Dictionary<int, Vector2> positions = new();
//        foreach (DictionaryEntry kvp in values)
//        {
//            if (kvp.Key is Transform target)
//            {
//                TransformState state = (TransformState)kvp.Value;
//                target.position = state.position;
//                target.rotation = state.rotation;
//            }
//            else if (kvp.Key is int)
//            {
//                positions.Add((int)kvp.Key, (Vector2)kvp.Value);
//            }
//        }
//        SelectState.selectedPositions = new();
//        //SelectState.selectedPositions.Add((Vector2)kvp.Value);
//        // Sort by key
//        foreach (var kvp in positions.OrderBy(x => x.Key))
//            SelectState.selectedPositions.Add(kvp.Value);
//    }
//
//    public void OnExitScope() { }
//}

/// <summary> Records the state Prior to your change </summary>
public class UndoTransform : IUndo
{
    public IEnumerable<Transform> targets;

    public struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;

        public TransformState(Transform target)
        {
            this.position = target.position;
            this.rotation = target.rotation;
        }
    }

    public UndoTransform(Transform target)
    {
        this.targets = new Transform[] { target };
    }

    public UndoTransform(IEnumerable<Transform> targets)
    {
        this.targets = targets;
    }

    public Hashtable RecordState()
    {
        Hashtable hash = new();

        for (int i = 0; i < targets.Count(); i++)
        {
            Transform target = targets.ElementAt(i);
            hash.Add(target, new TransformState(target));
        }

        for (int i = 0; i < SelectState.selectedPositions.Count; i++)
            hash.Add(i, SelectState.selectedPositions[i]);

        return hash;
    }

    public void ApplyState(Hashtable values)
    {
        Dictionary<int, Vector2> positions = new();
        foreach (DictionaryEntry kvp in values)
        {
            if(kvp.Key is Transform target)
            {
                TransformState state = (TransformState)kvp.Value;
                target.position = state.position;
                target.rotation = state.rotation;
            }
            else if (kvp.Key is int)
            { 
                positions.Add((int)kvp.Key, (Vector2)kvp.Value);
            }
        }
        SelectState.selectedPositions = new();
        //SelectState.selectedPositions.Add((Vector2)kvp.Value);
        // Sort by key
        foreach (var kvp in positions.OrderBy(x => x.Key))
            SelectState.selectedPositions.Add(kvp.Value);
    }

    public void OnExitScope() { }
}

/// <summary> Does not perform Instantiate, You must do that first </summary>
public class UndoInstantiate : IUndo
{
    public GameObject gameObject;
    [SerializeField] bool initialized = false;

    public UndoInstantiate(GameObject go)
    {
        this.gameObject = go;
        initialized = false;
    }

    public Hashtable RecordState()
    {
        Hashtable hash = new Hashtable();
        hash.Add(gameObject, initialized ? gameObject.activeSelf : false);
        initialized = true;
        return hash;
    }

    public void ApplyState(Hashtable hash)
    {
        foreach (DictionaryEntry kvp in hash)
            ((GameObject)kvp.Key).SetActive((bool)kvp.Value);
    }

    public void OnExitScope()
    {
        if (gameObject != null && !gameObject.activeSelf)
            GameObject.Destroy(gameObject);
    }
}

/// <summary> Performs Delete for you </summary>
public class UndoDelete : IUndo
{
    public IEnumerable<GameObject> gameObjects;

    public UndoDelete(IEnumerable<GameObject> selection)
    {
        this.gameObjects = selection;
    }

    public Hashtable RecordState()
    {
        Hashtable hash = new Hashtable();

        int n = 0;

        foreach (GameObject go in gameObjects)
        {
            go.SetActive(false);
            hash.Add(n++, go);
        }

        return hash;
    }

    public void ApplyState(Hashtable hash)
    {
        foreach (GameObject go in hash.Values)
            go.SetActive(true);
    }

    public void OnExitScope()
    {
        foreach (GameObject go in gameObjects)
            if (go != null && !go.activeSelf)
                GameObject.Destroy(go);
    }
}

public class UndoSelection : IUndo
{
    public UndoSelection() { }

    public Hashtable RecordState()
    {
        Hashtable hash = new Hashtable();

        for (int i = 0; i < SelectState.selectedObjects.Count; i++)
            hash.Add(SelectState.selectedObjects[i], SelectState.selectedPositions[i]);

        return hash;
    }

    public void ApplyState(Hashtable hash)
    {
        SelectState.selectedObjects = new();
        SelectState.selectedPositions = new();
        var keys = hash.Keys.Cast<LevelObject>().ToList();
        var values = hash.Values.Cast<Vector2>().ToList();
        for (int i = 0; i < hash.Count; i++)
        {
            SelectState.selectedObjects.Add(keys[i]);
            SelectState.selectedPositions.Add(values[i]);
        }
    }

    public void OnExitScope() { }
}

public static class SelectState
{
    internal static Vector2 objectDragStart;
    static bool isDraggingObject = false;
    static bool hasRecordedDragUndo = false;

    static Rect selectRect;
    static Vector2 selectRectStart;
    static bool isRectSelecting = false;

    static bool isCreatingProp = false;
    static GameObject prop;

    static Rect selectedRect;

    internal static List<LevelObject> selectedObjects = new();
    // First position is the position of the first selected object, the rest are the offsets relative to the first
    internal static List<Vector2> selectedPositions = new();

    public static void AddObjToSelection(LevelObject obj, bool recordUndo = true)
    {
        if (obj == null) return;
        if(recordUndo) Undo.RegisterState(new UndoSelection(), "Prop Selected");
        selectedObjects.Add(obj);
        if (selectedPositions.Count == 0)
            selectedPositions.Add(obj.transform.position);
        else
            selectedPositions.Add(selectedObjects[0].transform.InverseTransformPoint(obj.transform.position));
    }

    [EditorState(EditorState.Props, StateUpdate.OnEnter)]
    public static void Initialize()
    {
        // Reset everything
        selectedObjects.Clear();
        selectedPositions.Clear();
        isDraggingObject = false;
        hasRecordedDragUndo = false;
        isRectSelecting = false;
        isCreatingProp = false;
        prop = null;
    }

    [EditorState(EditorState.Props, StateUpdate.Update)]
    public static void Update()
    {
        if (!isCreatingProp && !MouseOnRotationRing())
        {
            var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var allHits = Physics2D.RaycastAll(mousePos, Vector2.zero);

            if (!LevelEditor.MouseOnUI && Input.GetMouseButtonDown(0))
            {
                var hit = allHits.Where(x => x.collider != null && x.collider.gameObject.GetComponent<LevelObject>() != null).FirstOrDefault();
                if (hit != default)
                {
                    var obj = hit.collider.gameObject.GetComponent<LevelObject>();
                    if (!selectedObjects.Contains(obj))
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {

                            AddObjToSelection(obj);
                        }
                        else
                        {
                            Undo.RegisterState(new UndoSelection(), "Prop Selected");
                            selectedObjects.Clear();
                            selectedPositions.Clear();
                            AddObjToSelection(obj, false);
                        }
                    }
                    objectDragStart = mousePos;
                    isDraggingObject = true;
                    hasRecordedDragUndo = false;

                }
                else
                {
                    Undo.RegisterState(new UndoSelection(), "Selection Changed");
                    selectedObjects.Clear();
                    selectedPositions.Clear();
                    selectRect = new Rect(mousePos, Vector2.zero);
                    selectRectStart = mousePos;
                    isRectSelecting = true;
                }
            }

            if (!LevelEditor.MouseOnUI && Input.GetMouseButton(0) && selectedObjects.Count > 0)
            {
                if (isDraggingObject)
                {
                    Vector2 offset = mousePos - objectDragStart;

                    if(!hasRecordedDragUndo && offset.sqrMagnitude > float.Epsilon)
                    {
                        Undo.RegisterState(new UndoTransform(selectedObjects.Select((x) => { return x.transform; })), "Moving");
                        hasRecordedDragUndo = true;
                    }

                    selectedPositions[0] += offset;
                    selectedObjects[0].transform.position = selectedPositions[0];
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        selectedObjects[0].transform.position = new Vector2(
                                                                           Mathf.Round(selectedPositions[0].x / 0.25f) * 0.25f,
                                                                           Mathf.Round(selectedPositions[0].y / 0.25f) * 0.25f
                                                                           );
                    }

                    for (int i = 1; i < selectedObjects.Count; i++)
                        selectedObjects[i].transform.position = selectedObjects[0].transform.TransformPoint(selectedPositions[i]);

                    objectDragStart += offset;
                }
                else if (isRectSelecting)
                {
                    // Update SelectRect, ise Min and Max values not size
                    selectRect.min = Vector2.Min(mousePos, selectRectStart);
                    selectRect.max = Vector2.Max(mousePos, selectRectStart);

                    // Update Selection
                    Undo.RegisterState(new UndoSelection(), "Selection Changed");
                    selectedObjects.Clear();
                    selectedPositions.Clear();
                    // Physics Overlap Box
                    var colliders = Physics2D.OverlapBoxAll(selectRect.center, selectRect.size, 0f);
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        var obj = colliders[i].gameObject.GetComponent<LevelObject>();
                        if (obj != null)
                            AddObjToSelection(obj, false);
                    }
                }
            }
        }

        // If we have selected objects, Do Rotation Logic
        if (!isCreatingProp && selectedObjects.Count > 0)
        {
            // Delete
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Undo.RegisterState(new UndoSelection(), "Selection Changed");
                var gos = selectedObjects.Select((x) => { return x.gameObject; });
                selectedObjects.Clear();
                selectedPositions.Clear();
                Undo.RegisterState(new UndoDelete(gos), "Deleted Props");

            }

            // Duplicate
            if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
            {
                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    var obj = selectedObjects[i];
                    if (obj.CanBeDuplicated)
                    {
                        var dupe = GameObject.Instantiate(obj.gameObject, obj.transform.position + Vector3.right * 0.5f, obj.transform.rotation);
                        dupe.name = dupe.name.Replace("(Clone)", "");
                        Undo.RegisterState(new UndoInstantiate(dupe), "Duplicate " + dupe.name);
                        selectedObjects[i] = dupe.GetComponent<LevelObject>();
                    }
                }
            }

            // Update Selected Rect
            selectedRect = GetSelectedRect() ?? new Rect();

            HandleRotationLogic();
        }
        else
        {
            isDraggingObject = false;
            isRectSelecting = false;

            if (isCreatingProp)
            {
                prop.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                prop.transform.position = new Vector3(prop.transform.position.x, prop.transform.position.y, 0);

                if (Input.GetMouseButtonDown(0))
                {
                    isCreatingProp = false;
                    if (LevelEditor.MouseOnUI)
                    {
                        GameObject.Destroy(prop);
                    }
                    else
                    {
                        Undo.RegisterState(new UndoInstantiate(prop), "Created " + prop.name);
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDraggingObject = false;
            isRectSelecting = false;
        }
    }

    public static Rect? GetSelectedRect()
    {
        if (selectedObjects.Count > 0)
        {
            Rect allRect = new Rect(selectedObjects[0].transform.position, Vector2.zero);
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var bounds = selectedObjects[i].GetComponent<SpriteRenderer>().bounds;
                allRect.min = Vector2.Min(allRect.min, bounds.min);
                allRect.max = Vector2.Max(allRect.max, bounds.max);
            }
            return allRect;
        }
        return null;
    }

    [EditorState(EditorState.Props, StateUpdate.Draw)]
    public static void DrawState()
    {
        if (selectedObjects.Count > 0)
        {
            // draw bounds around all selected
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                if (i > 100) break;
                var bounds = selectedObjects[i].GetComponent<SpriteRenderer>().bounds;
                Rect rect = new Rect(bounds.min, bounds.size);
                Draw.Rectangle(rect, 0.1f, new Color(0.1f, 0.1f, 0.9f, 0.2f));
                Draw.RectangleBorder(rect, 2f, 0.1f, i == 0 ? Color.yellow : Color.red);
            }
            Draw.RectangleBorder(selectedRect, 1f, 0.1f, Color.red);

            // Draw Rotation Gizmos
            float radius = Vector2.Distance(selectedRect.min, selectedRect.max) / 2f + 0.25f;
            Draw.Ring(selectedRect.center, Vector3.forward, radius, MouseOnRotationRing() ? Color.white : Color.magenta);
        }

        if (isRectSelecting)
        {
            Draw.Rectangle(selectRect, new Color(0.1f, 0.1f, 0.9f, 0.2f));
            Draw.RectangleBorder(selectRect, 2f, Color.blue);
        }
    }

    public static void Instantiate(GameObject prefab)
    {
        //Undo.RegisterState(new UndoSelection(), "Selection Changed");

        selectedObjects.Clear();
        selectedPositions.Clear();
        prop = GameObject.Instantiate(prefab);
        prop.name = prop.name.Replace("(Clone)", "");
        prop.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        prop.transform.position = new Vector3(prop.transform.position.x, prop.transform.position.y, 0);
        isCreatingProp = true;
    }


    #region Rotation

    static float rotateAngle = 0;
    static Vector2 rotateDragPos;
    static bool isRotating = false;

    public static void HandleRotationLogic()
    {
        var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool onRing = MouseOnRotationRing();

        // If you click within the radius of the ring, start rotating
        if (Input.GetMouseButtonDown(0) && onRing)
        {
            rotateDragPos = mousePos;
            rotateAngle = 0;
            isRotating = true;
            hasRecordedDragUndo = false;
        }

        // If mouse is dragged and rotating is active, calculate rotation angle
        if (isRotating && Input.GetMouseButton(0))
        {
            float angleChange = Vector2.SignedAngle(rotateDragPos - selectedRect.center, mousePos - selectedRect.center);
            if(angleChange > float.Epsilon && !hasRecordedDragUndo)
            {
                Undo.RegisterState(new UndoTransform(selectedObjects.Select((x) => { return x.transform; })), "Rotating");
                hasRecordedDragUndo = true;
            }

            float increment = 0;
            if (Input.GetKey(KeyCode.LeftControl))
            {
                rotateAngle += angleChange;
                if (Math.Abs(rotateAngle) > 15)
                {
                    increment = Math.Sign(rotateAngle) * 15f;
                    rotateAngle = 0;
                }
            }
            else
                increment = angleChange;

            // Rotate all selected objects around center
            for (int i = 0; i < selectedObjects.Count; i++)
                selectedObjects[i].transform.RotateAround(selectedRect.center, Vector3.forward, increment);
            selectedPositions[0] = selectedObjects[0].transform.position;

            rotateDragPos = mousePos;
        }

        // If mouse is released, stop rotating
        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }
    }

    public static bool MouseOnRotationRing()
    {
        if (selectedObjects.Count > 0)
        {
            float radius = Vector2.Distance(selectedRect.min, selectedRect.max) / 2f + 0.25f;
            var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float distanceToCenter = Vector2.Distance(mousePos, selectedRect.center);
            return distanceToCenter > radius - 0.1f && distanceToCenter < radius + 0.1f;
        }
        return false;
    }


    #endregion

}

public static class TileState
{
    public enum Tools { Paint, Line, Rectangle }
    public static Tools currentTool = Tools.Paint;
    public static byte currentTile = 0;
    public static int currentLayer = 0;
    public static bool LayerOpacity = true;

    public static bool isDragging = false;
    public static Vector2Int clickDragStart;

    public struct TrackTileChanges
    {
        public byte prevTile;
        public byte newTile;
        public Vector2Int tilePos;
        public int layer;
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
                    LevelEditor.Instance.Tilemap.SetTile(tile, currentTile, currentLayer);
                    LevelEditor.Instance.Tilemap.UpdateChunk(tile, currentLayer, false);
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

                    var line = GetCellsOnLine(clickDragStart, tile);
                    foreach (var point in line)
                        LevelEditor.Instance.Tilemap.SetTile(point, currentTile, currentLayer, true);
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
                        // For every tile from the start to the end, set the tile
                        for (int x = Math.Min(clickDragStart.x, tile.x); x <= Math.Max(clickDragStart.x, tile.x); x++)
                            for (int y = Math.Min(clickDragStart.y, tile.y); y <= Math.Max(clickDragStart.y, tile.y); y++)
                                LevelEditor.Instance.Tilemap.SetTile(new Vector2Int(x, y), currentTile, currentLayer, true);
                    }
                }
                else if (isDragging)
                {
                }
                break;
        }

        if (LayerOpacity)
        {
            LevelEditor.Instance.Tilemap.Layers[0].opacity = 0.5f;
            LevelEditor.Instance.Tilemap.Layers[1].opacity = 0.5f;
            LevelEditor.Instance.Tilemap.Layers[2].opacity = 0.5f;
            LevelEditor.Instance.Tilemap.Layers[currentLayer].opacity = 1f;
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
        var tileset = LevelEditor.Instance.Tilemap.Layers[currentLayer].Tileset;
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
            button.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        }


    }
}
