using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleResultManager : MonoBehaviour
{
    void Start()
    {
        CompleteStage();
        AudioManager.Instance.PlayBgm("BgmFanfare");
    }

    public void ButtonConfirm()
    {
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        SceneManager.LoadScene("MainMenu");
    }

    public void CompleteStage()
    {
        int stageIndex = 1;
        switch(BattleArgs.TeamId1) 
        {
            case "T3":
                stageIndex = 1;
                break;
            case "T6":
                stageIndex = 2;
                break;
            case "T4":
                stageIndex = 3;
                break;
            case "T5":
                stageIndex = 4;
                break;
            case "T2":
                stageIndex = 5;
                break;
        }

        int unlockedStage = SettingsManager.GetUnlockedStage();
        if (stageIndex + 1 > unlockedStage && stageIndex < 5)
        {
            SettingsManager.SetUnlockedStage(stageIndex + 1);
            SettingsManager.Save();
        }
    }
}
