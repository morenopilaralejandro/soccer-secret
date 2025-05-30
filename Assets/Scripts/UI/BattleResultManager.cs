using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleResultManager : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance.PlayBgm("BgmFanfare");
    }

    public void ButtonConfirm()
    {
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        SceneManager.LoadScene("MainMenu");
    }

}
