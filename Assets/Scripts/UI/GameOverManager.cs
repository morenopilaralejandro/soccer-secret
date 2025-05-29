using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{

    public void ButtonConfirm()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
