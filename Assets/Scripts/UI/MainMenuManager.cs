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
        AudioManager.Instance.PlayBgm("BgmMainTheme");
        HideCredits();
        HideSettings();
    }

    public void ButtonCpu()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        SceneManager.LoadScene("Battle");
    }

    public void ButtonOnline()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        SceneManager.LoadScene("OnlineMenu");
    }

    public void ButtonQuit()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        Application.Quit();
        Debug.Log("Game is exiting");
    }

    public void ButtonSettings()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        ShowSettings();
    }

    public void ButtonCredits()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
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
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        dropdownLanguage.ConfirmLanguage();
        HideSettings();
    }

    public void CancelSettings()
    {
        AudioManager.Instance.PlaySfx("SfxMenuCancel");
        dropdownLanguage.CancelLanguage();
        HideSettings();
    }

    public void ConfirmCredits()
    {
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        HideCredits();
    }

}
