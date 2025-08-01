using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class DuelLogManager : MonoBehaviour
{
    public TMP_Text shortLogText;
    public GameObject fullLogPanel;
    public TMP_Text fullLogText;
    public int shortLogMaxLines = 3;

    private List<string> duelLogEntries = new List<string>();

    // LocalizedString references for each line type
    private LocalizedString playerPassLocStr;


    private LocalizedString command1LocStr;
    private LocalizedString command2LocStr;

    private void Awake()
    {
        playerPassLocStr  = new LocalizedString("DuelLogTexts", "PlayerPass");

        command1LocStr  = new LocalizedString("UITexts", "Command1");
        command2LocStr  = new LocalizedString("UITexts", "Command2");
    }

    private void AddEntry(LocalizedString localizedString) {
        string s = localizedString.GetLocalizedString();
        duelLogEntries.Add(s);
        RefreshShortLog();
        localizedString.Arguments = null;
    }

    public void AddPass(string teamColor, string playerName, string secretColor)
    {
        playerPassLocStr.Arguments = new object[] { new { 
            teamColor = teamColor, 
            playerName = playerName } };

        AddEntry(playerPassLocStr);
    }

    public void AddGain(string player, string secret, string secretColor)
    {

    }

    public void AddShoot(string player, string secret, string secretColor)
    {

    }

    public void AddDuel(string player, string secret, string secretColor)
    {

    }

    private void RefreshShortLog()
    {
        var lastEntries = duelLogEntries.Skip(Mathf.Max(0, duelLogEntries.Count - shortLogMaxLines));
        shortLogText.text = string.Join("\n", lastEntries);
    }

    public void ShowFullLog()
    {
        fullLogPanel.SetActive(true);
        fullLogText.text = string.Join("\n", duelLogEntries);
    }

    public void HideFullLog()
    {
        fullLogPanel.SetActive(false);
    }

}
