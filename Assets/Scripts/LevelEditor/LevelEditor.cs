using Newtonsoft.Json;
using Shapes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum EditorState { Tiles, Props, }
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
    public static LevelEditor Instance;

    public EditorState state = EditorState.Tiles;

    [Space]
    [Header("Camera")]
    public float moveSpeed = 6f;

    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    [Space]
    public ScrollRect tileTools;
    public RectTransform tileBrowser;
    public ScrollRect props;
    public RectTransform propBrowser;
    [Space]
    public Button undoButton;
    public Button redoButton;

    public Tilemap Tilemap;

    public string LevelScene = "Level";

    public TMP_InputField LevelNameInput;

    public TMP_Text SaveOverwiteMessage;
    public GameObject SaveOverwritePanel;

    public Transform loadContent;

    public GameObject[] LayerButtHighlights;
    public GameObject[] TileEditorButts;

    public enum Layer { Background = 0, Ground = 1, Foreground = 2 }
    public static Layer currentLayer = Layer.Ground;
    public static bool LayerOpacity = true;

    public static bool MouseOnUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    Vector2 camDragStart;

    public override void OnEnable()
    {
        base.OnEnable();

        Instance = this;

        if(GameManager.CurrentLevel != null)
        {
            LevelNameInput.text = GameManager.CurrentLevel.Name;
        }
        else
        {
            LevelNameInput.text = "New Level";
        }

        // Load all levels in StreamingAssets/CustomLevels into loadContent using its first child as a template
        var levelFiles = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/CustomLevels", "*.level");
        for (int i = 0; i < levelFiles.Length; i++)
        {
            var level = Instantiate(loadContent.GetChild(0).gameObject);
            level.transform.parent = loadContent;

            var text = level.GetComponentInChildren<TMP_Text>();
            text.text = System.IO.Path.GetFileNameWithoutExtension(levelFiles[i]);
            var button = level.GetComponentInChildren<Button>();
            int index = i;
            button.onClick.AddListener(() =>
            {
                GameManager.LoadEditor(levelFiles[index]);
            });

            level.SetActive(true);
        }

        EditorStateAttribute.FindAllMethods();

        SelectState.UpdatePropsList();
        TileState.UpdateTilesList();

        EditorStateAttribute.Invoke(LevelEditor.Instance.state, StateUpdate.OnEnter);
    }

    void Update()
    {
        UpdateCamera();

        // Pressing space centers the camera to the center
        // TODO: Go to Player Spawn
        if (Input.GetKeyDown(KeyCode.F))
            Camera.main.transform.position = new Vector3(0, 0, -10f);

        // Undo Redo
#if !UNITY_EDITOR
if (Input.GetKey(KeyCode.LeftControl))
{
#endif
        if (Input.GetKeyDown(KeyCode.Z))
            Undo.PerformUndo();
        if (Input.GetKeyDown(KeyCode.Y))
            Undo.PerformRedo();
#if !UNITY_EDITOR
}
#endif

        tileTools.gameObject.SetActive(state == EditorState.Tiles);
        props.gameObject.SetActive(state == EditorState.Props);

        undoButton.interactable = Undo.CanUndo;
        redoButton.interactable = Undo.CanRedo;

        for (int i = 0; i < LayerButtHighlights.Length; i++)
            LayerButtHighlights[i].SetActive(i == (int)currentLayer);

        for (int i = 0; i < TileEditorButts.Length; i++)
            TileEditorButts[i].SetActive(state == EditorState.Tiles);


        EditorStateAttribute.Invoke(state, StateUpdate.Update);
    }

    void UpdateCamera()
    {
        var cam = Camera.main;

        // Move Camera
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        cam.transform.position += new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;

        if (Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Space))
            camDragStart = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(2) || Input.GetKey(KeyCode.Space))
        {
            var dragEnd = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position -= (Vector3)(dragEnd - camDragStart);
            //dragStart = dragEnd;
        }

        // Zoom Camera
        if (!LevelEditor.MouseOnUI)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }
    }

    void OnGUI()
    {
        //GUI.Window(0, new Rect(10, 10, 200, 200), GUI.WindowFunction., "My Window");

        if (GUI.Button(new Rect(10, 10, 150, 100), "I am a button"))
        {
            print("You clicked the button!");
        }
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

    public SerializedLevel SerializeLevel()
    {
        SerializedLevel level = new();

        level.Name = LevelNameInput.text;

        level.UniqueID = Guid.NewGuid().ToString();

        // Serialize the Tilemap
        level.tilemapData = Tilemap.Serialize();

        // Serialize the Props
        level.props = new();
        var props = GameObject.FindObjectsByType<LevelObject>(FindObjectsSortMode.None);
        for (int i = 0; i < props.Length; i++)
        {
            var prop = new SerializedLevel.Prop();
            prop.name = props[i].gameObject.name;
            prop.posX = props[i].transform.position.x;
            prop.posY = props[i].transform.position.y;
            prop.rotX = props[i].transform.rotation.x;
            prop.rotY = props[i].transform.rotation.y;
            prop.rotZ = props[i].transform.rotation.z;
            prop.rotW = props[i].transform.rotation.w;
            level.props.Add(prop);
        }

        return level;
    }

    public void PlayTest()
    {
        var level = SerializeLevel();
        level.isPlayTesting = true;
        GameManager.PlayLevel(level);
    }

    public void SaveSafe()
    {
        // Save the level to StreamingAssets/CustomLevels/LevelName.level
        var path = Application.streamingAssetsPath + "/CustomLevels/" + LevelNameInput.text + ".level";
        if (System.IO.File.Exists(path))
        {
            SaveOverwritePanel.SetActive(true);
            SaveOverwiteMessage.text = "Are you sure you want to overwrite the previous level: " + LevelNameInput.text + "?";
            SaveOverwiteMessage.text += "\n\nThis action cannot be undone.";
            SaveOverwiteMessage.text += "\n\n";
            SaveOverwiteMessage.text += "\n\nYou can change the name of this level in the Level Settings.";
        }
        else
        {
            DoSave();
        }
    }

    public void DoSave()
    {
        var level = SerializeLevel();
        var path = Application.streamingAssetsPath + "/CustomLevels/" + level.Name + ".level";
        // save with JsonUtility
        System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(level));
    }

    public static void SnapTo(ScrollRect view, RectTransform content, RectTransform target)
    {
        //var pos = 1 - ((content.rect.width - target.localPosition.x) / content.rect.width);
        //view.normalizedPosition = new Vector2(pos * 2.0f, 0);
        //return;
        //BringChildIntoView(view, target);
        //return;
        Canvas.ForceUpdateCanvases();

        var result = (Vector2)view.transform.InverseTransformPoint(content.position)
                - (Vector2)view.transform.InverseTransformPoint(target.position);
        result.y = content.anchoredPosition.y;
        result.x += (view.transform as RectTransform).rect.width / 2f;
        content.anchoredPosition = result;
    }

    /// <summary>
    /// Based on https://stackoverflow.com/a/50191835
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    public static void BringChildIntoView(ScrollRect instance, RectTransform child)
    {
        instance.content.ForceUpdateRectTransforms();
        instance.viewport.ForceUpdateRectTransforms();

        // now takes scaling into account
        Vector2 viewportLocalPosition = instance.viewport.localPosition;
        Vector2 childLocalPosition = child.localPosition;
        Vector2 newContentPosition = new Vector2(
            0 - ((viewportLocalPosition.x * instance.viewport.localScale.x) + (childLocalPosition.x * instance.content.localScale.x)),
            0 - ((viewportLocalPosition.y * instance.viewport.localScale.y) + (childLocalPosition.y * instance.content.localScale.y))
        );

        // clamp positions
        instance.content.localPosition = newContentPosition;
        Rect contentRectInViewport = TransformRectFromTo(instance.content.transform, instance.viewport);
        float deltaXMin = contentRectInViewport.xMin - instance.viewport.rect.xMin;
        if (deltaXMin > 0) // clamp to <= 0
        {
            newContentPosition.x -= deltaXMin;
        }
        float deltaXMax = contentRectInViewport.xMax - instance.viewport.rect.xMax;
        if (deltaXMax < 0) // clamp to >= 0
        {
            newContentPosition.x -= deltaXMax;
        }
        float deltaYMin = contentRectInViewport.yMin - instance.viewport.rect.yMin;
        if (deltaYMin > 0) // clamp to <= 0
        {
            newContentPosition.y -= deltaYMin;
        }
        float deltaYMax = contentRectInViewport.yMax - instance.viewport.rect.yMax;
        if (deltaYMax < 0) // clamp to >= 0
        {
            newContentPosition.y -= deltaYMax;
        }

        // apply final position
        instance.content.localPosition = newContentPosition;
        instance.content.ForceUpdateRectTransforms();
    }

    /// <summary>
    /// Converts a Rect from one RectTransfrom to another RectTransfrom.
    /// Hint: use the root Canvas Transform as "to" to get the reference pixel positions.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static Rect TransformRectFromTo(Transform from, Transform to)
    {
        RectTransform fromRectTrans = from.GetComponent<RectTransform>();
        RectTransform toRectTrans = to.GetComponent<RectTransform>();

        if (fromRectTrans != null && toRectTrans != null)
        {
            Vector3[] fromWorldCorners = new Vector3[4];
            Vector3[] toLocalCorners = new Vector3[4];
            Matrix4x4 toLocal = to.worldToLocalMatrix;
            fromRectTrans.GetWorldCorners(fromWorldCorners);
            for (int i = 0; i < 4; i++)
            {
                toLocalCorners[i] = toLocal.MultiplyPoint3x4(fromWorldCorners[i]);
            }

            return new Rect(toLocalCorners[0].x, toLocalCorners[0].y, toLocalCorners[2].x - toLocalCorners[1].x, toLocalCorners[1].y - toLocalCorners[0].y);
        }

        return default(Rect);
    }
}