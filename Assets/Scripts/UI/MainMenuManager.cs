using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelCpu;
    [SerializeField] private GameObject panelSettings;
    [SerializeField] private GameObject panelCredits;

    [SerializeField] private DropdownLanguage dropdownLanguage;
    [SerializeField] private Toggle toggleShootOnSwipeUp;
    [SerializeField] private Slider sliderBgm;
    [SerializeField] private Slider sliderSfx;

    [SerializeField] private Button[] cpuStageButtons;

    void Start()
    {
        dropdownLanguage.InitializeDropdown();
        toggleShootOnSwipeUp.isOn = SettingsManager.GetIsShootOnSwipeUp();
        sliderBgm.value = SettingsManager.GetBgmVolume();
        sliderSfx.value = SettingsManager.GetSfxVolume();

        AudioManager.Instance.PlayBgm("BgmMainTheme");
        EnableStageButtons();
        HideCpu();
        HideCredits();
        HideSettings();
    }

    public void OnButtonCpuTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        ShowCpu();
    }

    private void LoadSceneBattle(string teamId1, PitchMaterial pitchMaterial, WinScore winScore)
    {
        BattleArgs.Clear();
        BattleArgs.TeamId0 = "T1";
        BattleArgs.TeamId1 = teamId1;
        BattleArgs.PitchMaterial = pitchMaterial;
        BattleArgs.WinScore = winScore;
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        SceneManager.LoadScene("Battle");
    }

    private void EnableStageButtons()
    {
        for (int i = 1; i <= cpuStageButtons.Length; i++)
        {
            bool unlocked = SettingsManager.IsStageUnlocked(i);
            cpuStageButtons[i-1].gameObject.SetActive(unlocked);
        }
    }

    public void OnButtonCpuStage1Tapped()
    {
        LoadSceneBattle("T3", PitchMaterial.Grass, WinScore.Three);
    }

    public void OnButtonCpuStage2Tapped()
    {
        LoadSceneBattle("T6", PitchMaterial.Grass, WinScore.Three);
    }

    public void OnButtonCpuStage3Tapped()
    {
        LoadSceneBattle("T4", PitchMaterial.Dirt, WinScore.Three);
    }

    public void OnButtonCpuStage4Tapped()
    {
        LoadSceneBattle("T5", PitchMaterial.Ice, WinScore.Three);
    }

    public void OnButtonCpuStage5Tapped()
    {
        LoadSceneBattle("T2", PitchMaterial.Fire, WinScore.Five);
    }

    public void OnButtonOnlineTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        SceneManager.LoadScene("OnlineMenu");
    }

    public void OnButtonQuitTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        Application.Quit();
        Debug.Log("Game is exiting");
    }

    public void OnButtonSettingsTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        ShowSettings();
    }

    public void OnButtonCreditsTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        ShowCredits();
    }

    public void OnBgmVolumeChanged(float volume)
    {
        AudioManager.Instance.SetBgmVolume(volume);
    }

    public void OnSfxVolumeChanged(float volume)
    {
        AudioManager.Instance.SetSfxVolume(volume);
    }

    public void ShowCpu()
    {
        panelMain.SetActive(false);
        panelCpu.SetActive(true);
    }

    public void HideCpu()
    {
        panelMain.SetActive(true);
        panelCpu.SetActive(false);
    }


    public void ShowSettings()
    {
        panelMain.SetActive(false);
        panelSettings.SetActive(true);
    }

    public void HideSettings()
    {
        panelMain.SetActive(true);
        panelSettings.SetActive(false);
    }

    public void ShowCredits()
    {
        panelMain.SetActive(false);
        panelCredits.SetActive(true);
    }

    public void HideCredits()
    {
        panelMain.SetActive(true);
        panelCredits.SetActive(false);
    }

    public void ConfirmSettings()
    {
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        dropdownLanguage.ConfirmLanguage();
        SettingsManager.SetIsShootOnSwipeUp(toggleShootOnSwipeUp.isOn);
        SettingsManager.SetBgmVolume(sliderBgm.value);
        SettingsManager.SetSfxVolume(sliderSfx.value);
        SettingsManager.Save();

        ShowCpu();
        HideCpu();
        ShowCredits();
        HideCredits();
        HideSettings();
    }

    public void CancelSettings()
    {
        AudioManager.Instance.PlaySfx("SfxMenuCancel");
        dropdownLanguage.CancelLanguage();
        HideSettings();
    }

    public void CancelCpu()
    {
        AudioManager.Instance.PlaySfx("SfxMenuCancel");
        HideCpu();
    }

    public void ConfirmCredits()
    {
        AudioManager.Instance.PlaySfx("SfxMenuConfirm");
        HideCredits();
    }

}
