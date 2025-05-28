using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject PanelMain;
    public GameObject PanelSettings;
    public GameObject PanelCredits;

    public DropdownLanguage dropdownLanguage;

    void Start()
    {
        HideCredits();
        HideSettings();
    }

    public void ButtonCpu()
    {
        SceneManager.LoadScene("Battle");
    }

    public void ButtonQuit()
    {
        Application.Quit();
        Debug.Log("Game is exiting");
    }

    public void ButtonSettings()
    {
        ShowSettings();
    }

    public void ButtonCredits()
    {
        ShowCredits();
    }

    public void ShowSettings()
    {
        PanelMain.SetActive(false);
        PanelSettings.SetActive(true);
    }

    public void HideSettings()
    {
        PanelMain.SetActive(true);
        PanelSettings.SetActive(false);
    }

    public void ShowCredits()
    {
        PanelMain.SetActive(false);
        PanelCredits.SetActive(true);
    }

    public void HideCredits()
    {
        PanelMain.SetActive(true);
        PanelCredits.SetActive(false);
    }

    public void ConfirmSettings()
    {
        dropdownLanguage.ConfirmLanguage();
        HideSettings();
    }

    public void CancelSettings()
    {
        dropdownLanguage.CancelLanguage();
        HideSettings();
    }

    public void ConfirmCredits()
    {
        HideCredits();
    }

}
