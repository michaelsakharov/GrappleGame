using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class GrappleFinishLevel : IGrappleInteractor
{
    public string nextLevel;

    public override bool Interact(PlayerController player, RaycastHit2D hit)
    {
        if (string.IsNullOrWhiteSpace(nextLevel)) return false;

        PlayerPrefs.SetInt("LevelState_" + nextLevel, 1);
        PlayerPrefs.SetString("LastPlayedLevel", nextLevel);
        SceneManager.LoadScene(nextLevel);

        return true;
    }

    public override void OnLeave(PlayerController player) { }
}
