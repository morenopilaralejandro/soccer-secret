using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance.PlayBgm("BgmGameOver");
    }

    public void ButtonConfirm()
    {
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        SceneManager.LoadScene("MainMenu");
    }

}
