using UnityEngine;
using UnityEngine.UI;

public class PropButton : Button
{
    public GameObject prefab;

    protected override void Start()
    {
        base.Start();
        onClick.AddListener(() => SelectState.Instantiate(prefab));
    }
}
