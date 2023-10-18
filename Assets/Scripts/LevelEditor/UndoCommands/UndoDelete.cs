using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary> Performs Delete for you </summary>
public class UndoDelete : IUndo
{
    public IEnumerable<GameObject> gameObjects;

    public UndoDelete(IEnumerable<GameObject> selection)
    {
        this.gameObjects = selection;
    }

    public Hashtable RecordState()
    {
        Hashtable hash = new Hashtable();

        int n = 0;

        foreach (GameObject go in gameObjects)
        {
            go.SetActive(false);
            hash.Add(n++, go);
        }

        return hash;
    }

    public void ApplyState(Hashtable hash)
    {
        foreach (GameObject go in hash.Values)
            go.SetActive(true);
    }

    public void OnExitScope()
    {
        foreach (GameObject go in gameObjects)
            if (go != null && !go.activeSelf)
                GameObject.Destroy(go);
    }
}
