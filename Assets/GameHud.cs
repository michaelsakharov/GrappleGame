using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerController;

public class GameHud : MonoBehaviour
{
    public static GameHud Instance;

    public PlayerController player;

    public TMPro.TMP_Text levelTimerText;
    public TMPro.TMP_Text bestLevelTimerText;
    public GameObject levelFinishUI;
    public TMPro.TMP_Text finishLevelTimerText;
    public TMPro.TMP_Text bestFinishLevelTimerText;

    bool doTimer;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        if(GameManager.CurrentLevel == null)
            gameObject.SetActive(false);
    }

    public void UpdateLevelTimers(float levelTimer, float bestTimer)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(levelTimer);
        string levelTimerTextString = timeSpan.Hours > 0 ?
            timeSpan.ToString(@"hh\:mm\:ss\:fff") :
            timeSpan.ToString(@"mm\:ss\:fff");

        TimeSpan bestTimeSpan = TimeSpan.FromSeconds(bestTimer);
        string bestLevelTimerTextString = bestTimeSpan.Hours > 0 ?
            bestTimeSpan.ToString(@"hh\:mm\:ss\:fff") :
            bestTimeSpan.ToString(@"mm\:ss\:fff");

        levelTimerText.text = levelTimerTextString;
        bestLevelTimerText.text = bestLevelTimerTextString;

        timeSpan = TimeSpan.FromSeconds(levelTimer);
        string levelTimeText = timeSpan.TotalHours >= 1 ?
            "Time: " + timeSpan.ToString(@"hh\:mm\:ss\:fff") :
            "Time: " + timeSpan.ToString(@"mm\:ss\:fff");

        bestTimeSpan = TimeSpan.FromSeconds(bestTimer);
        string bestTimeText = bestTimeSpan.TotalHours >= 1 ?
            "Best: " + bestTimeSpan.ToString(@"hh\:mm\:ss\:fff") :
            "Best: " + bestTimeSpan.ToString(@"mm\:ss\:fff");

        finishLevelTimerText.text = levelTimeText;
        bestFinishLevelTimerText.text = bestTimeText;
    }
}
