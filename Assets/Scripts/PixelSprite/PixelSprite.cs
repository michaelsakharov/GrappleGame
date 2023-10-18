using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelState : MonoBehaviour
{

}

public class PixelSprite : MonoBehaviour
{

    public struct Bone
    {
        public string Name;
        [Header("Width and Height of the bone should be Tight! (No empty space around the Bone)")]
        public Texture2D Texture;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
