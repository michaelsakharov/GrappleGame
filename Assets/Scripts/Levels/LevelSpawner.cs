using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSpawner : MonoBehaviour
{
    public bool isGame = true;
    public string MainMenuScene;
    public Tilemap tilemap;
    [Header("Game Settings")]
    public GameObject PlayerPrefab;
    [Header("Editor Settings")]
    public GameObject PlayerSpawnPrefab;

    void Awake()
    {
        if (GameManager.CurrentLevel == null)
        {
            if (isGame)
            {
                Debug.LogError("No Level Loaded!");
                // No level to load! Go back to the main menu
                SceneManager.LoadScene(MainMenuScene);
            }
            else
            {
                // New Level
                // Create Player Spawn Point
                var playerSpawn = Instantiate(PlayerSpawnPrefab);
                // remove the (Clone) from the name
                playerSpawn.name = playerSpawn.name.Replace("(Clone)", "");
                playerSpawn.transform.parent = null;
                playerSpawn.transform.position = Vector3.zero;
                playerSpawn.transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            // spawn all the props
            GameObject playerSpawn = null;
            foreach (var prop in GameManager.CurrentLevel.props)
            {
                var prefab = Resources.Load<GameObject>("Level Objects/" + prop.name);
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    instance.name = instance.name.Replace("(Clone)", "");
                    instance.transform.parent = null;
                    instance.transform.position = new Vector3(prop.posX, prop.posY, 0);
                    instance.transform.rotation = new Quaternion(prop.rotX, prop.rotY, prop.rotZ, prop.rotW);

                    // Disable all the components that shouldn't be active in play mode
                    var lo = instance.GetComponent<LevelObject>();
                    if (isGame)
                    {
                        foreach (var component in lo.DisableInPlay)
                            component.enabled = false;
                        if(lo.DisableRenderers)
                        {
                            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                                renderer.enabled = false;
                        }
                    }
                    else
                    {
                        // Set any rigidbody to kinematic while in Editor
                        foreach (var rb in instance.GetComponentsInChildren<Rigidbody2D>())
                            rb.isKinematic = true;
                    }

                    if (instance.CompareTag("Respawn"))
                        playerSpawn = instance;

                }
            }

            if (isGame)
            {
                if (playerSpawn == null)
                {
                    // No player spawn point found! Map Currupt
                    Debug.LogError("No Player Spawn Point Found! Map Currupt!");
                    SceneManager.LoadScene(MainMenuScene);
                    return;
                }

                // spawn the player
                var player = Instantiate(PlayerPrefab);
                player.transform.parent = null;
                player.transform.position = playerSpawn.transform.position;
            }

            // spawn the tilemap
            tilemap.Deserialize(GameManager.CurrentLevel.tilemapData);
            tilemap.WaitForChunks();
            //tilemap.RefreshAll();
        }
    }

}
