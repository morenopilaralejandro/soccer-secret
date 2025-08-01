using UnityEngine;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    public bool IsPaused => _isPaused;

    private bool[] _isTeamReady = new bool[2]; 
    private float _actionTimer = 10f;
    private float _lastPauseTime = -Mathf.Infinity;
    private bool _isPaused = false;
    private float pauseCooldown = 15f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        InputManager.Instance.SwipeDetector.OnSwipe += HandleSwipe;
        InputManager.Instance.KeyboardDetector.OnPauseKey += HandlePauseKey;
    }

    private void OnDisable()
    {
        InputManager.Instance.SwipeDetector.OnSwipe -= HandleSwipe;
        InputManager.Instance.KeyboardDetector.OnPauseKey -= HandlePauseKey;
    }

    public bool CanPause()
    {
        return GameManager.Instance.CurrentPhase == GamePhase.Battle &&
            !GameManager.Instance.IsTimeFrozen &&
            Time.time - _lastPauseTime >= pauseCooldown && !_isPaused;
    }

    public bool TryPause()
    {
        if (CanPause())
        {
            _lastPauseTime = Time.time; // Only 1 value
            PauseGame();
            return true;
        }
        return false;
    }

    // Call this when a player wants to signal readiness to resume
    public void SetTeamReady(int teamIndex)
    {
        if (!_isPaused) return; // Only allow marking ready if we're paused!
        _isTeamReady[teamIndex] = true;

        if (GameManager.Instance.IsMultiplayer)
        {
            if (_isTeamReady[0] && _isTeamReady[1])
            {
                ResumeGame();
            }
        }
        else
        {
            // Single player, resume immediately
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (_isPaused) return;
        _isPaused = true;
        _isTeamReady[0] = false;
        _isTeamReady[1] = false;
        _actionTimer = 10f;
        // Insert your pause-game logic here (e.g., set Time.timeScale = 0)
        GameManager.Instance.FreezeGame();
        GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Pause);
        if (GameManager.Instance.IsMultiplayer)
            StartCoroutine(MultiplayerActionTimerRoutine());
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;
        _isPaused = false;
        _isTeamReady[0] = false;
        _isTeamReady[1] = false;
        // Insert your resume-game logic here (e.g., set Time.timeScale = 1)
        GameManager.Instance.SetGamePhase(GamePhase.Battle);
        GameManager.Instance.UnfreezeGame();
    }

    private void HandleSwipe(SwipeDetector.SwipeDirection dir)
    {
        if (InputManager.Instance.IsDragging) return;
        if (InputManager.Instance.SwipeDetector.WasConsumedThisFrame()) return;

        GameLogger.Log("[PauseManager] swipe");

        if (dir == SwipeDetector.SwipeDirection.Up) 
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Pause) return; 
            if (!IsPaused) return; 

            GameLogger.Log("[PauseManager] swipe up");
            if (IsPaused) {
                GameLogger.Log("[PauseManager] swipe pause");
                SetTeamReady(GameManager.Instance.GetLocalTeamIndex());
            }
        }

        if (dir == SwipeDetector.SwipeDirection.Down) 
        {
            if (!IsPaused)
            {
                if (!TryPause())
                {
                    GameLogger.Log("[PauseManager] Pause is on cooldown!");
                }
            }
        }

    }

    private void HandlePauseKey() 
    {
        if (!IsPaused)
        {
            if (!TryPause())
            {
                GameLogger.Log("[PauseManager] Pause is on cooldown!");
            }
        }
        else
        {
            SetTeamReady(GameManager.Instance.GetLocalTeamIndex());
        }
    }

    private IEnumerator MultiplayerActionTimerRoutine()
    {
        while (_actionTimer > 0f)
        {
            _actionTimer -= Time.deltaTime;
            if (_isTeamReady[0] && _isTeamReady[1])
                break;
            yield return null;
        }
        
        ResumeGame();
    }

}
