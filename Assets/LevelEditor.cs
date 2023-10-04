using Shapes;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum EditorState { Select, Create, Config }

//public class EditorStateAttribute : Attribute
//{
//    public EditorState state;
//    public bool isDraw = false;
//    public EditorStateAttribute(EditorState state, bool isDraw)
//    {
//        this.state = state;
//        this.isDraw = isDraw;
//    }
//
//    public static Dictionary<(EditorState, bool), MethodInfo> methods = new();
//
//    public static void FindAllMethods()
//    {
//        // Find in all static classes
//        var types = Assembly.GetExecutingAssembly().GetTypes();
//        for (int i = 0; i < types.Length; i++)
//        {
//            var methods = types[i].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
//            for (int j = 0; j < methods.Length; j++)
//            {
//                var attr = methods[j].GetCustomAttribute<EditorStateAttribute>();
//                if (attr != null)
//                    EditorStateAttribute.methods.Add((attr.state, attr.isDraw), methods[j]);
//            }
//        }
//    }
//
//    public static void Invoke(EditorState state, bool isDraw)
//    {
//        if (methods.ContainsKey((state, isDraw)))
//            methods[(state, isDraw)].Invoke(null, null);
//    }
//}
//
//public static class SelectState
//{
//    [EditorState(EditorState.Select, false)]
//    public static void Update()
//    {
//    }
//
//    [EditorState(EditorState.Select, true)]
//    public static void Draw()
//    {
//    }
//}

public class LevelEditor : ImmediateModeShapeDrawer
{
    public EditorState state;

    public float moveSpeed = 6f;

    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    Vector2 objectDragStart;
    bool isDraggingObject = false;

    private Rect selectedRect;

    Vector2 camDragStart;
    List<GameObject> selectedObjects = new();

    void Update()
    {
        UpdateCamera();

        if (state == EditorState.Select)
        {
            HandleSelectionState();
        }
        else if (state == EditorState.Create)
        {

        }
        else if (state == EditorState.Config)
        {

        }
    }

    void UpdateCamera()
    {
        var cam = Camera.main;

        // Move Camera
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        cam.transform.position += new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;

        if (Input.GetMouseButtonDown(1))
            camDragStart = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(1))
        {
            var dragEnd = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position -= (Vector3)(dragEnd - camDragStart);
            //dragStart = dragEnd;
        }

        // Zoom Camera
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
    }

    public override void DrawShapes(Camera cam)
    {
        using (Draw.Command(cam))
        {
            // set up static parameters. these are used for all following Draw.Line calls
            Draw.LineGeometry = LineGeometry.Flat2D;
            Draw.ThicknessSpace = ThicknessSpace.Pixels;
            Draw.Thickness = 4; // 4px wide

            // draw bounds around all selected
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var bounds = selectedObjects[i].GetComponent<SpriteRenderer>().bounds;
                Rect rect = new Rect(bounds.min, bounds.size);
                Draw.Rectangle(rect, 0.1f, new Color(0.1f, 0.1f, 0.9f, 0.2f));
                Draw.RectangleBorder(rect, 2f, 0.1f, Color.red);
            }

            if (selectedObjects.Count > 0)
                Draw.RectangleBorder(selectedRect, 1f, 0.1f, Color.red);

            HandleRotationDraw();
        }
    }

    #region Selection

    void HandleSelectionState()
    {
        bool onGizmos = MouseOnRotationRing();

        if (!onGizmos)
        {
            HandleSelectionLogic();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            for (int i = 0; i < selectedObjects.Count; i++)
                Destroy(selectedObjects[i]);
            selectedObjects.Clear();
        }

        // If we have selected objects, Do Rotation Logic
        if (selectedObjects.Count > 0)
        {
            // Update Selected Rect
            selectedRect = GetSelectedRect() ?? new Rect();

            HandleRotationLogic();
        }
    }

    public Rect? GetSelectedRect()
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

    public void HandleSelectionLogic()
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
                        selectedObjects.Add(obj);
                }
                else
                {
                    if (selectedObjects.Count <= 1)
                    {
                        selectedObjects.Clear();
                        selectedObjects.Add(obj);
                    }
                    else if (!selectedObjects.Contains(obj))
                        selectedObjects.Add(obj);
                }
                objectDragStart = mousePos;
                isDraggingObject = true;
            }
            else
            {
                selectedObjects.Clear();
            }
        }

        if (isDraggingObject && Input.GetMouseButton(0))
        {
            Vector2 offset = mousePos - objectDragStart;
            if (Input.GetKey(KeyCode.LeftControl))
            {
                // TODO: Make more reliable
                offset = new Vector2(
                                     Mathf.Round(offset.x / 0.25f) * 0.25f,
                                     Mathf.Round(offset.y / 0.25f) * 0.25f
                                     );
            } 

            for (int i = 0; i < selectedObjects.Count; i++)
                selectedObjects[i].transform.position += (Vector3)offset;

            objectDragStart += offset;
        }

        if (Input.GetMouseButtonUp(0))
            isDraggingObject = false;
    }

    #endregion


    #region Rotation

    float rotateAngle = 0;
    Vector2 rotateDragPos;
    bool isRotating = false;

    public void HandleRotationLogic()
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

    public void HandleRotationDraw()
    {
        if (selectedObjects.Count == 0) return;
        float radius = Vector2.Distance(selectedRect.min, selectedRect.max) / 2f + 0.25f;
        Draw.Ring(selectedRect.center, Vector3.forward, radius, MouseOnRotationRing() ? Color.white : Color.magenta);
    }

    public bool MouseOnRotationRing()
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


    #region Creation

    #endregion

}
