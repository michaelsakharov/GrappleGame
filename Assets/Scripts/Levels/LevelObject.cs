using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelObject : MonoBehaviour
{
    public Sprite Icon;
    public Behaviour[] DisableInPlay;
    public bool DisableRenderers = false;

    public bool CanBeDuplicated = true;
    public bool CanBeDestroyed = true;

    public bool ShowInList = true;
    public LevelEditor.Layer Layer = LevelEditor.Layer.Ground;
}
