using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializedLevel
{
    [Serializable]
    public struct Prop
    {
        public string name;

        public float posX;
        public float posY;

        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;
    }

    [NonSerialized] public bool isPlayTesting = false;

    public string UniqueID; // Becomes new every time the level is serialized to reset times and track information per level

    public string Name;

    public float CameraBoundsX;
    public float CameraBoundsY;
    public float CameraBoundsWidth;
    public float CameraBoundsHeight;

    public List<Prop> props;

    public byte[] tilemapData;
}
