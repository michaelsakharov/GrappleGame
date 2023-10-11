using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEditorTilesSelector : MonoBehaviour
{
    public enum SelectType { Layer, Tool, Tile, State, Undo, Redo, Play }
    public SelectType selectType;
    public int Value;

    public void Select()
    {
        switch (selectType)
        {
            case SelectType.Layer:
                TileState.currentLayer = Value;
                TileState.UpdateTilesList();
                LevelEditor.Instance.Tilemap.RefreshAll();
                break;
            case SelectType.Tool:
                TileState.currentTool = (TileState.Tools)Value;
                break;
            case SelectType.Tile:
                TileState.currentTile = (byte)Value;
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
        TileState.LayerOpacity = state;
        LevelEditor.Instance.Tilemap.RefreshAll();
    }
}
