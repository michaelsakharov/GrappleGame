using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerUpdater : MonoBehaviour
{
    void Start()
    {
        GameManager.PlayerStart();
    }

    void Update()
    {
        GameManager.PlayerUpdate();
    }

    void LateUpdate()
    {
        GameManager.PlayerLateUpdate();
    }
}
