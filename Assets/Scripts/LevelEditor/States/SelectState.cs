using Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

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

        UpdatePropsList();
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
                var gos = selectedObjects.Select((x) => { return x.gameObject; }).ToArray();
                Undo.RegisterState(new UndoSelection(), "Selection Changed");
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

    internal static void UpdatePropsList()
    {
        var propBrowser = LevelEditor.Instance.propBrowser.transform;
        // Delete all children except the first one
        for (int i = 1; i < propBrowser.childCount; i++)
            GameObject.Destroy(propBrowser.GetChild(i).gameObject);

        var levelObjects = Resources.LoadAll<LevelObject>("Level Objects");
        for (int i = 0; i < levelObjects.Length; i++)
        {
            if (levelObjects[i].ShowInList == false)
                continue; // Skip this one

            if (levelObjects[i].Layer != LevelEditor.currentLayer)
                continue; // Skip this one

            GameObject icon = GameObject.Instantiate(propBrowser.GetChild(0).gameObject);
            icon.name = levelObjects[i].name;
            icon.transform.SetParent(propBrowser);
            icon.transform.localScale = Vector3.one;
            icon.transform.localPosition = Vector3.zero;

            var image = icon.transform.GetChild(0).GetComponent<Image>();
            image.sprite = levelObjects[i].Icon;
            var button = icon.GetComponent<PropButton>();
            button.prefab = levelObjects[i].gameObject;

            icon.SetActive(true);
        }
    }


    #endregion

}
