using UnityEngine;

public static class SettingsManager
{
    private const string Key_LanguageIndex = "LanguageIndex";
    private const string Key_IsShootOnSwipeUp = "IsShootOnSwipeUp";
    private const string Key_BgmVolume = "BgmVolume";
    private const string Key_SfxVolume = "SfxVolume";

    private const string Key_UnlockedStage = "UnlockedStage";

    public static int GetLanguageIndex()
    {
        return PlayerPrefs.GetInt(Key_LanguageIndex, 0);
    }

    public static void SetLanguageIndex(int index)
    {
        PlayerPrefs.SetInt(Key_LanguageIndex, index);
    }

    public static bool GetIsShootOnSwipeUp()
    {
        return PlayerPrefs.GetInt(Key_IsShootOnSwipeUp, 1) == 1;
    }

    public static void SetIsShootOnSwipeUp(bool isOn)
    {
        PlayerPrefs.SetInt(Key_IsShootOnSwipeUp, isOn ? 1 : 0);
    }

    public static float GetBgmVolume()
    {
        return PlayerPrefs.GetFloat(Key_BgmVolume, 1f); // default volume = 1
    }

    public static void SetBgmVolume(float volume)
    {
        PlayerPrefs.SetFloat(Key_BgmVolume, Mathf.Clamp01(volume));
    }

    public static float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(Key_SfxVolume, 1f);
    }

    public static void SetSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(Key_SfxVolume, Mathf.Clamp01(volume));
    }

    public static int GetUnlockedStage()
    {
        return PlayerPrefs.GetInt(Key_UnlockedStage, 1);
    }

    public static void SetUnlockedStage(int stageIndex)
    {
        PlayerPrefs.SetInt(Key_UnlockedStage, stageIndex);
    }

    public static bool IsStageUnlocked(int stageIndex)
    {
        return stageIndex <= GetUnlockedStage();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(Key_LanguageIndex);
        PlayerPrefs.DeleteKey(Key_IsShootOnSwipeUp);
        PlayerPrefs.DeleteKey(Key_BgmVolume);
        PlayerPrefs.DeleteKey(Key_SfxVolume);
        PlayerPrefs.Save();
    }

    public static void ResetUnlockedStage()
    {
        PlayerPrefs.DeleteKey(Key_UnlockedStage);
        PlayerPrefs.Save();
    }
}
