using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
