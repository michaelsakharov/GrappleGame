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

    public float moveSpeed = 6f;

    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    public GameObject tileTools;
    public Transform tileBrowser;
    public GameObject props;
    public Transform propBrowser;

    public Tilemap Tilemap;

    public string LevelScene = "Level";

    public TMP_InputField LevelNameInput;

    public TMP_Text SaveOverwiteMessage;
    public GameObject SaveOverwritePanel;

    public Transform loadContent;

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

        var levelObjects = Resources.LoadAll<LevelObject>("Level Objects");
        for (int i = 0; i < levelObjects.Length; i++)
        {
            if (levelObjects[i].ShowInList == false)
                continue; // Skip this one

            GameObject icon = Instantiate(propBrowser.transform.GetChild(0).gameObject);
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

        TileState.UpdateTilesList();

        EditorStateAttribute.Invoke(LevelEditor.Instance.state, StateUpdate.OnEnter);
    }

    void Update()
    {
        UpdateCamera();

        // Pressing space centers the camera to the center
        // TODO: Go to Player Spawn
        if (Input.GetKeyDown(KeyCode.Space))
            Camera.main.transform.position = new Vector3(0, 0, -10f);

        // Undo Redo
        if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl))
            Undo.PerformUndo();
        if (Input.GetKeyDown(KeyCode.Y) && Input.GetKey(KeyCode.LeftControl))
            Undo.PerformRedo();

        tileTools.SetActive(state == EditorState.Tiles);
        props.SetActive(state == EditorState.Props);

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
        level.isTest = true;
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
}