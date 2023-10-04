using Shapes;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum EditorState { Select, Create, Config }
public enum StateUpdate { Update, Draw, OnLeave, OnEnter }

public class EditorStateAttribute : Attribute
{
    public EditorState state;
    public StateUpdate update;
    public EditorStateAttribute(EditorState state, StateUpdate update)
    {
        this.state = state;
        this.update = update;
    }

    public static Dictionary<(EditorState, StateUpdate), MethodInfo> methods = new();

    public static void FindAllMethods()
    {
        methods.Clear();
        // Find in all static classes
        var types = Assembly.GetExecutingAssembly().GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            var methods = types[i].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int j = 0; j < methods.Length; j++)
            {
                var attr = methods[j].GetCustomAttribute<EditorStateAttribute>();
                if (attr != null)
                    EditorStateAttribute.methods.Add((attr.state, attr.update), methods[j]);
            }
        }
    }

    public static void Invoke(EditorState state, StateUpdate update)
    {
        if (methods.ContainsKey((state, update)))
            methods[(state, update)].Invoke(null, null);
    }
}

public class LevelEditor : ImmediateModeShapeDrawer
{
    public EditorState state = EditorState.Select;

    public float moveSpeed = 6f;

    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    Vector2 camDragStart;

    public override void OnEnable()
    {
        base.OnEnable();
        EditorStateAttribute.FindAllMethods();
    }

    void Update()
    {
        UpdateCamera();

        EditorStateAttribute.Invoke(state, StateUpdate.Update);
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

            EditorStateAttribute.Invoke(state, StateUpdate.Draw);
        }
    }
}


public static class CreateState
{

    [EditorState(EditorState.Create, StateUpdate.Update)]
    public static void Update()
    {

    }
}