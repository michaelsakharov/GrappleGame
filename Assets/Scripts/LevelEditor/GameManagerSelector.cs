using UnityEngine;

public class GameManagerSelector : MonoBehaviour
{
    public enum SelectType { PlayCampaign, PlayNextCampaign, ResumeCampaign, BackToMenu, LoadEditor, LoadEditorForCampaign, RestartLevel, Quit }
    public SelectType selectType;
    [Header("PlayCampaign, ResumeCampaign, LoadEditorForCampaign")]
    public string CampaignName;
    [Header("PlayCampaign, LoadEditorForCampaign")]
    public int Level;

    public void Select()
    {
        switch (selectType)
        {
            case SelectType.PlayCampaign:
                GameManager.PlayCampaign(CampaignName, Level);
                break;

            case SelectType.PlayNextCampaign:
                GameManager.PlayNextCampaign();
                break;

            case SelectType.ResumeCampaign:
                GameManager.PlayCampaign(CampaignName);
                break;

            case SelectType.BackToMenu:
                GameManager.BackToMenu();
                break;

            case SelectType.LoadEditor:
                GameManager.LoadEditor();
                break;

            case SelectType.LoadEditorForCampaign:
                GameManager.LoadEditor(CampaignName, Level);
                break;

            case SelectType.RestartLevel:
                GameManager.RestartLevel();
                break;

            case SelectType.Quit:
                GameManager.QuitGame();
                break;
        }
    }
}
