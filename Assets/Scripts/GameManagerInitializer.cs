using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        GameManager.Initialize();
    }
}
