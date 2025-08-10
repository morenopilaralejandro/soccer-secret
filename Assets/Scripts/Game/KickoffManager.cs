using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class KickoffManager : MonoBehaviour
{
    public static KickoffManager Instance { get; private set; }

    public bool IsKickoffReady { get; private set; } = false;
    public bool IsAiReady { get;  set; } = false;

    private bool[] _isTeamReady = new bool[2];
    private LocalizedString textKickOff = new LocalizedString("UITexts", "TextKickOff");
    private LocalizedString textSwipeUp = new LocalizedString("UITexts", "TextSwipeUp");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResetReady() 
    {
        _isTeamReady[0] = false;
        _isTeamReady[1] = false;
        IsKickoffReady = false;
        IsAiReady = false;
        BallBehavior.Instance.ResetPendingInputs();
        PauseManager.Instance.ResetCooldown();
        GameManager.Instance.FreezeGame();
        GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Kickoff);
        UIManager.Instance.SetHintVisible(true);
        UIManager.Instance.SetHintText(textKickOff.GetLocalizedString());
    }

    public void SetTeamReady(int teamIndex)
    {
        if (!_isTeamReady[teamIndex])
            _isTeamReady[teamIndex] = true;

        if (_isTeamReady[0] && _isTeamReady[1]) 
        {
            IsKickoffReady = true;
            DuelLogManager.Instance.AddMatchResume();
            GameManager.Instance.SetGamePhase(GamePhase.Battle);
            GameManager.Instance.UnfreezeGame();
            UIManager.Instance.SetHintText(textSwipeUp.GetLocalizedString());
            UIManager.Instance.SetHintVisible(false);
        }
    }

}
