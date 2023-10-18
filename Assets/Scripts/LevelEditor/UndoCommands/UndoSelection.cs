using System.Collections;
using System.Linq;
using UnityEngine;

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
