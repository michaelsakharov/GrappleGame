using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public string startingLevel;

    string lastPlayed = "";

    public void Awake()
    {
        Instance = this;
        lastPlayed = PlayerPrefs.GetString("LastPlayedLevel", "");
    }

    public void PlayLevel(string level)
    {
        // make sure we have that level unlocked
        bool levelUnlocked = 
            PlayerPrefs.GetInt("LevelState_" + level, 0) != 0 || 
            string.Equals(level, startingLevel, StringComparison.OrdinalIgnoreCase);
        if (levelUnlocked)
        {
            // load the level
            SceneManager.LoadScene(level);
            PlayerPrefs.SetString("LastPlayedLevel", level);
        }
    }

    public void Continue()
    {
        if(string.IsNullOrWhiteSpace(lastPlayed)) // No last level
        {
            PlayLevel(startingLevel);
            return;
        }

        // load the last completed level
        SceneManager.LoadScene(lastPlayed);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
