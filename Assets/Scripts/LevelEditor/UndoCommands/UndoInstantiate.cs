using System.Collections;
using UnityEngine;
/// <summary> Does not perform Instantiate, You must do that first </summary>
public class UndoInstantiate : IUndo
{
    public GameObject gameObject;
    [SerializeField] bool initialized = false;

    public UndoInstantiate(GameObject go)
    {
        this.gameObject = go;
        initialized = false;
    }

    public Hashtable RecordState()
    {
        Hashtable hash = new Hashtable();
        hash.Add(gameObject, initialized ? gameObject.activeSelf : false);
        initialized = true;
        return hash;
    }

    public void ApplyState(Hashtable hash)
    {
        foreach (DictionaryEntry kvp in hash)
            ((GameObject)kvp.Key).SetActive((bool)kvp.Value);
    }

    public void OnExitScope()
    {
        if (gameObject != null && !gameObject.activeSelf)
            GameObject.Destroy(gameObject);
    }
}
