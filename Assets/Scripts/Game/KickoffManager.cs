using UnityEngine;
using System.Collections.Generic;

public class KickoffManager : MonoBehaviour
{
    public static KickoffManager Instance { get; private set; }

    public bool IsKickoffReady { get; private set; } = false;
    public bool IsAiReady { get;  set; } = false;

    private bool[] _isTeamReady = new bool[2];

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
        GameManager.Instance.FreezeGame();
        GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Kickoff);
    }

    public void SetTeamReady(int teamIndex)
    {
        if (!_isTeamReady[teamIndex])
            _isTeamReady[teamIndex] = true;

        if (_isTeamReady[0] && _isTeamReady[1]) 
        {
            IsKickoffReady = true;
            GameManager.Instance.SetGamePhase(GamePhase.Battle);
            GameManager.Instance.UnfreezeGame();
        }
    }

}
