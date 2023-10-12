using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManager
{
    public const string menuScene = "Menu";
    public const string levelScene = "Level";
    public const string levelEditorScene = "LevelEditor";


    public readonly static string[] Campaigns = new string[] { "Game" };
    static readonly Dictionary<string, List<SerializedLevel>> CampaignLevels = new(StringComparer.OrdinalIgnoreCase);

    public static SerializedLevel CurrentLevel { get; private set; }
    public static float levelTimer { get; private set; }
    public static float bestTimer { get; private set; }

    public static bool IsInCampaign => isCampaign;
    public static bool PlayerFinished => PlayerController.Instance.State == PlayerController.PlayerState.Finished;
    public static bool PlayerDead => PlayerController.Instance.State == PlayerController.PlayerState.Dead;
    public static string CurrentCampaign => curCampaign.Item1;
    public static int CurrentCampaignLevel => curCampaign.Item2;

    static bool isCampaign = false;
    static (string, int) curCampaign;
    static bool isInitialized = false;
    static bool doTimer = false;

    public static void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Load all Campaign scenes from resources
        for (int i = 0; i < Campaigns.Length; i++)
        {
            CampaignLevels.Add(Campaigns[i], new());

            var levels = Resources.LoadAll<TextAsset>("Campaigns/" + Campaigns[i]);
            // Sort levels by name (name is a number)
            Array.Sort(levels, (a, b) => int.Parse(a.name).CompareTo(int.Parse(b.name)));

            // Make sure theres no gaps or duplicates
            bool hasProblem = false;
            for (int j = 0; j < levels.Length; j++)
            {
                if (int.Parse(levels[j].name) != j)
                {
                    Debug.LogError("Level " + levels[j].name + " in Campaign " + Campaigns[i] + " is out of order or has a duplicate");
                    hasProblem = true;
                    break;
                }
            }
            if (hasProblem) continue; // Skip this campaign, its broken

            for (int j = 0; j < levels.Length; j++)
            {
                var serializedLevel = JsonConvert.DeserializeObject<SerializedLevel>(levels[j].text);
                CampaignLevels[Campaigns[i]].Add(serializedLevel);
            }
        }

        // if not already on menu Scene go to it
        if (SceneManager.GetActiveScene().name != menuScene)
            SceneManager.LoadScene(menuScene);
    }

    /// <summary> Called when the player initializes </summary>
    public static void PlayerStart()
    {
        bestTimer = GetBestTime();
        doTimer = false;
    }

    public static void PlayerUpdate()
    {
        if (CurrentLevel != null)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartLevel();

            if (PlayerController.Instance.State != PlayerController.PlayerState.Finished && doTimer)
                levelTimer += Time.deltaTime;

            if (GameHud.Instance != null)
                GameHud.Instance.UpdateLevelTimers(levelTimer, bestTimer);


            // Level Editor - Play Testing
            if (CurrentLevel.isPlayTesting)
            {
                // Noclip
                if (Input.GetKeyDown(KeyCode.N))
                    PlayerController.Instance.ToggleCollider();

                // Back to Level Editor
                if (Input.GetKeyDown(KeyCode.B))
                    GameManager.GoBackToLevelEditor();
            }
        }
    }

    public static void PlayerLateUpdate()
    {

    }


    #region Public Methods

    public static void StartTimer() => doTimer = true;
    public static void StopTimer() => doTimer = false;

    public static void PlayLevel(SerializedLevel level)
    {
        isCampaign = false;
        CurrentLevel = level;
        SceneManager.LoadScene(levelScene);
    }

    public static bool HasUnlockedCampaignLevel(string name, int level)
    {
        if (!CampaignLevels.ContainsKey(name)) throw new InvalidOperationException("Campaign " + name + " does not exist!");
        if (level >= CampaignLevels[name].Count) throw new InvalidOperationException("Campaign " + name + " does not have a level " + level + "!");
        if (level == 0) return true; // First level is always unlocked
        int prevHighest = PlayerPrefs.GetInt($"Campaign_{name}_UnlockedTo", 0);
        return level <= prevHighest;
    }

    public static void PlayCampaign(string name, int level)
    {
        if (!CampaignLevels.ContainsKey(name)) throw new InvalidOperationException("Campaign " + name + " does not exist!");
        if (level >= CampaignLevels[name].Count) throw new InvalidOperationException("Campaign " + name + " does not have a level " + level + "!");

        curCampaign = (name, level);
        isCampaign = true;
        CurrentLevel = CampaignLevels[name][level];
        SceneManager.LoadScene(levelScene);
    }

    public static void PlayCampaign(string name)
    {
        int level = PlayerPrefs.GetInt($"Campaign_{name}_UnlockedTo", 0);
        PlayCampaign(name, level);
    }

    public static bool PlayNextCampaign()
    {
        if (!isCampaign) throw new InvalidOperationException("Not in campaign mode!");
        if (!CampaignLevels.ContainsKey(curCampaign.Item1)) throw new InvalidOperationException("Campaign " + curCampaign.Item1 + " does not exist!");
        int level = curCampaign.Item2 + 1;
        if (level >= CampaignLevels[curCampaign.Item1].Count)
            return false; // No more campaign levels

        int prevHighest = PlayerPrefs.GetInt($"Campaign_{curCampaign.Item1}_UnlockedTo", 0);
        if (level > prevHighest)
            PlayerPrefs.SetInt($"Campaign_{curCampaign.Item1}_UnlockedTo", level);

        PlayCampaign(curCampaign.Item1, level);
        return true;
    }

    public static void GoBackToLevelEditor()
    {
        if (CurrentLevel.isPlayTesting == false) throw new InvalidOperationException("Not in test mode!");
        SceneManager.LoadScene(levelEditorScene);
    }

    public static void LoadEditor()
    {
        isCampaign = false;
        CurrentLevel = null;
        SceneManager.LoadScene(levelEditorScene);
    }

    public static void LoadEditor(string filePath, string backScene = menuScene)
    {
        isCampaign = false;

        // Load custom level from Streaming Assets
        var serializedLevel = JsonConvert.DeserializeObject<SerializedLevel>(System.IO.File.ReadAllText(filePath));
        CurrentLevel = serializedLevel;

        SceneManager.LoadScene(levelEditorScene);
    }

    public static void LoadEditor(string name, int level)
    {
        curCampaign = (name, level);
        isCampaign = true;
        CurrentLevel = CampaignLevels[name][level];
        SceneManager.LoadScene(levelEditorScene);
    }

    public static void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void BackToMenu()
    {
        SceneManager.LoadScene(menuScene);
    }

    public static void FinishLevel()
    {
        if (isCampaign)
        {
            HandleBestTime("Campaign", levelTimer);
            if (!PlayNextCampaign())
            {
                // Go to menu, We finished the Campaign
                BackToMenu();
            }
        }
        else
        {
            HandleBestTime("CustomLevel", levelTimer);
            RestartLevel();
        }
    }

    public static float GetBestTime()
    {
        if (CurrentLevel == null) return 0;
        if (isCampaign)
            return PlayerPrefs.GetFloat("Campaign_" + CurrentLevel.Name + "_" + CurrentLevel.UniqueID, 0);
        return PlayerPrefs.GetFloat("CustomLevel_" + CurrentLevel.UniqueID, 0);
    }

    static void HandleBestTime(string key, float currentTime)
    {
        if (CurrentLevel.isPlayTesting) return; // Don't save test times
        string fullKey = key + "_" + CurrentLevel.Name + "_" + CurrentLevel.UniqueID;
        float bestTime = PlayerPrefs.GetFloat(fullKey, float.MaxValue);
        if (currentTime < bestTime)
            PlayerPrefs.SetFloat(fullKey, currentTime);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Bunny/Clear All Campaign Progress")]
    public static void ClearCampaignProgress()
    {
        foreach (var campaign in Campaigns)
            PlayerPrefs.DeleteKey($"Campaign_{campaign}_UnlockedTo");
    }
#endif

    public static void QuitGame()
    {
        Application.Quit();
    }

    #endregion
}
