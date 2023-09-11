using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class PlayerFinishLevel : IPlayerInteractor
{
    public string nextLevel;

    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        Finish();
    }

    public override void TriggerInteract(PlayerController player)
    {
        Finish();
    }

    public void Finish()
    {
        if (string.IsNullOrWhiteSpace(nextLevel)) return;

        PlayerPrefs.SetInt("LevelState_" + nextLevel, 1);
        PlayerPrefs.SetString("LastPlayedLevel", nextLevel);
        PlayerController.Instance.FinishLevel(nextLevel);
        //SceneManager.LoadScene(nextLevel);
    }

    public override void OnLeave(PlayerController player) { }
}
