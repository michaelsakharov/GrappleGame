using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEditorTilesSelector : MonoBehaviour
{
    public enum SelectType { Layer, Tool, Tile, State, Undo, Redo, Play }
    public SelectType selectType;
    public int Value;

    public GameObject tileSelectedObject;

    private void Update()
    {
        if (tileSelectedObject != null && selectType == SelectType.Tile)
        {
            if (Value == TileState.currentTile)
                tileSelectedObject.SetActive(true);
            else
                tileSelectedObject.SetActive(false);
        }
    }

    public void Select()
    {
        switch (selectType)
        {
            case SelectType.Layer:
                LevelEditor.currentLayer = (LevelEditor.Layer)Value;
                TileState.UpdateTilesList();
                LevelEditor.Instance.Tilemap.RefreshAll();
                SelectState.UpdatePropsList();
                break;
            case SelectType.Tool:
                TileState.currentTool = (TileState.Tools)Value;
                break;
            case SelectType.Tile:
                TileState.currentTile = (byte)Value;
                LevelEditor.SnapTo(LevelEditor.Instance.tileTools, LevelEditor.Instance.tileBrowser, this.transform as RectTransform);
                break;
            case SelectType.State:
                EditorStateAttribute.Invoke(LevelEditor.Instance.state, StateUpdate.OnLeave);
                LevelEditor.Instance.state = (EditorState)Value;
                EditorStateAttribute.Invoke(LevelEditor.Instance.state, StateUpdate.OnEnter);
                break;
            case SelectType.Undo:
                Undo.PerformUndo();
                break;
            case SelectType.Redo:
                Undo.PerformRedo();
                break;
            case SelectType.Play:
                LevelEditor.Instance.PlayTest();
                break;
        }
    }

    public void ToggleLayerOpacity(bool state)
    {
        LevelEditor.LayerOpacity = state;
        LevelEditor.Instance.Tilemap.RefreshAll();
    }
}
