using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    public bool IsPaused => _isPaused;

    [SerializeField] private Sprite pauseIcon;
    [SerializeField] private TextMeshProUGUI textTimerPause;

    private bool[] _isTeamReady = new bool[2]; 
    private float _actionTimer = 10f;
    private bool _isPaused = false;
    private float pauseCooldown = 15f;
    private float _pauseCooldownRemaining = 0f;
    private LocalizedString textCooldownPause = new LocalizedString("UITexts", "TextCooldownPause");

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


    private void Update()
    {
        if (_pauseCooldownRemaining > 0f && !GameManager.Instance.IsTimeFrozen)
        {
            _pauseCooldownRemaining -= Time.deltaTime;
            if (_pauseCooldownRemaining < 0f)
                _pauseCooldownRemaining = 0f;
        }

        UpdateText();
    }

    private void UpdateText()
    {
        if (textTimerPause == null) return;

        if (_pauseCooldownRemaining > 0f)
        {
            textTimerPause.text = Mathf.Ceil(_pauseCooldownRemaining).ToString();
        }
        else
        {
            textTimerPause.text = textCooldownPause.GetLocalizedString();
        }
    }

    public void ResetCooldown()
    {
        _pauseCooldownRemaining = 0f;
    }

    public bool CanPause()
    {
        return GameManager.Instance.CurrentPhase == GamePhase.Battle &&
            !GameManager.Instance.IsTimeFrozen &&
            _pauseCooldownRemaining <= 0f && !_isPaused;
    }

    public bool TryPause()
    {
        if (CanPause())
        {
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

        DuelLogManager.Instance.AddMatchPause(GameManager.Instance.GetLocalTeamIndex());
        GameManager.Instance.FreezeGame();
        GameManager.Instance.SetGamePhaseNetworkSafe(GamePhase.Pause);
        DuelManager.Instance.StopAndCleanupUnlockStatus();
        UIManager.Instance.SetCategorySprite(pauseIcon);
        UIManager.Instance.SetCategoryVisible(true);
        if (GameManager.Instance.IsMultiplayer)
            StartCoroutine(MultiplayerActionTimerRoutine());
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;
        _isPaused = false;
        _isTeamReady[0] = false;
        _isTeamReady[1] = false;
        _pauseCooldownRemaining = pauseCooldown; // Start cooldown
        // Insert your resume-game logic here (e.g., set Time.timeScale = 1)
        DuelLogManager.Instance.AddMatchResume();
        GameManager.Instance.SetGamePhase(GamePhase.Battle);
        GameManager.Instance.UnfreezeGame();
        UIManager.Instance.SetCategoryVisible(false);
    }

    private void HandleSwipe(SwipeDetector.SwipeDirection dir)
    {
        if (InputManager.Instance.IsDragging) return;
        if (InputManager.Instance.SwipeDetector.WasConsumedThisFrame()) return;
        if (UIManager.Instance.IsDuelLogMenuOpen()) return;

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
        if (UIManager.Instance.IsDuelLogMenuOpen()) return;

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
