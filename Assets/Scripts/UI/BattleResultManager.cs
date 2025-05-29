using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleResultManager : MonoBehaviour
{

    public void ButtonConfirm()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
