using UnityEngine;

public class EnableIf : MonoBehaviour
{
    public enum ConditionType { IsPlayTesting, HasUnlockedCampaignLevel }
    public ConditionType Condition;
    public bool invert = false;

    public bool showIfFailed = true;

    [Header("HasUnlockedCampaignLevel")]
    public string Campaign = "Game";
    public int Level = 0;

    void Awake()
    {
        try
        {
            bool condition = false;
            switch (Condition)
            {
                case ConditionType.IsPlayTesting:
                    condition = GameManager.CurrentLevel.isPlayTesting;
                    break;

                case ConditionType.HasUnlockedCampaignLevel:
                    condition = GameManager.HasUnlockedCampaignLevel(Campaign, Level);
                    break;
            }
            if (invert) condition = !condition;
            gameObject.SetActive(condition);
        }
        catch
        {
            gameObject.SetActive(showIfFailed);
        }
    }
}
