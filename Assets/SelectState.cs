using Shapes;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class SelectState
{
    static Vector2 objectDragStart;
    static bool isDraggingObject = false;

    static Rect selectRect;
    static Vector2 selectRectStart;
    static bool isRectSelecting = false;

    static Rect selectedRect;

    static GameObject main;

    static List<GameObject> selectedObjects = new();
    // First position is the position of the first selected object, the rest are the offsets relative to the first
    static List<Vector2> selectedPositions = new();

    static void AddObjToSelection(GameObject obj)
    {
        selectedObjects.Add(obj);
        if (selectedPositions.Count == 0)
            selectedPositions.Add(obj.transform.position);
        else
            selectedPositions.Add(selectedObjects[0].transform.InverseTransformPoint(obj.transform.position));
    }

    [EditorState(EditorState.Select, StateUpdate.Update)]
    public static void Update()
    {
        if (!MouseOnRotationRing())
        {
            var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (Input.GetMouseButtonDown(0))
            {
                if (hit.collider != null)
                {
                    var obj = hit.collider.gameObject;
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (!selectedObjects.Contains(obj))
                            AddObjToSelection(obj);
                    }
                    else
                    {
                        if (selectedObjects.Count <= 1)
                        {
                            selectedObjects.Clear();
                            selectedPositions.Clear();
                            AddObjToSelection(obj);
                        }
                        else if (!selectedObjects.Contains(obj))
                        {
                            AddObjToSelection(obj);
                        }
                    }
                    objectDragStart = mousePos;
                    isDraggingObject = true;
                }
                else
                {
                    selectedObjects.Clear();
                    selectedPositions.Clear();
                    selectRect = new Rect(mousePos, Vector2.zero);
                    selectRectStart = mousePos;
                    isRectSelecting = true;
                }
            }

            if (Input.GetMouseButton(0) && selectedObjects.Count > 0)
            {
                if (isDraggingObject)
                {
                    Vector2 offset = mousePos - objectDragStart;
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
                    selectedObjects.Clear();
                    selectedPositions.Clear();
                    // Physics Overlap Box
                    var colliders = Physics2D.OverlapBoxAll(selectRect.center, selectRect.size, 0f);
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        if (colliders[i].gameObject.GetComponent<SpriteRenderer>() != null)
                            AddObjToSelection(colliders[i].gameObject);
                    }
                }
            }
        }

        // If we have selected objects, Do Rotation Logic
        if (selectedObjects.Count > 0)
        {
            // Delete
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                for (int i = 0; i < selectedObjects.Count; i++)
                    GameObject.Destroy(selectedObjects[i]);
                selectedObjects.Clear();
                selectedPositions.Clear();
            }

            // Duplicate
            if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
            {
                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    var obj = selectedObjects[i];
                    var newObj = GameObject.Instantiate(obj);
                    newObj.transform.position = obj.transform.position + Vector3.right * 0.5f;
                    selectedObjects[i] = newObj;
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

    [EditorState(EditorState.Select, StateUpdate.Draw)]
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
        }

        // If mouse is dragged and rotating is active, calculate rotation angle
        if (isRotating && Input.GetMouseButton(0))
        {
            float angleChange = Vector2.SignedAngle(rotateDragPos - selectedRect.center, mousePos - selectedRect.center);

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

            rotateDragPos = mousePos;
        }

        // If mouse is released, stop rotating
        if (Input.GetMouseButtonUp(0))
            isRotating = false;
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
