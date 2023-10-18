using UnityEngine;
using UnityEngine.UI;
using static SerializedLevel;

public class PropButton : Button
{
    public GameObject prefab;

    protected override void Start()
    {
        base.Start();
        LevelEditor.SnapTo(LevelEditor.Instance.props, LevelEditor.Instance.propBrowser, this.transform as RectTransform);
        onClick.AddListener(() => SelectState.Instantiate(prefab));
    }
}
