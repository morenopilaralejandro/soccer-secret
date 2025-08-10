using UnityEngine;     // For MonoBehaviour, GameObject, etc.
using UnityEngine.UI;  // For ScrollRect
using System.Collections; // For IEnumerator and Coroutines
using UnityEngine.Localization;

public class DuelLogMenuManager : MonoBehaviour
{
    [SerializeField] private DuelLogPopup popupPrefab;
    [SerializeField] private Transform contentParent; // ScrollView content
    [SerializeField] private GameObject panelDuelLogMenu; // ScrollView content
    [SerializeField] private Animator animator;
    [SerializeField] private ScrollRect scrollRect;
    
    private LocalizedString textSwipeUp = new LocalizedString("UITexts", "TextSwipeUp");
    private LocalizedString textSwipeLeft = new LocalizedString("UITexts", "TextSwipeLeft");
    private LocalizedString textKickOff = new LocalizedString("UITexts", "TextKickOff");

    private bool isMenuOpen = false;
    
    void Awake()
    {

    }

    private void OnEnable()
    {
        InputManager.Instance.SwipeDetector.OnSwipe += HandleSwipe;
        InputManager.Instance.KeyboardDetector.OnDuelLogKey += HandleDuelLogKey;
    }

    private void OnDisable()
    {
        InputManager.Instance.SwipeDetector.OnSwipe -= HandleSwipe;
        InputManager.Instance.KeyboardDetector.OnDuelLogKey -= HandleDuelLogKey;
    }


    // Call this when opening the menu
    private void PopulateLog()
    {
        // Clear previous items
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Add a static popup for each log entry
        foreach (DuelLogEntry entry in DuelLogManager.Instance.DuelLogEntries)
        {
            var popup = Instantiate(popupPrefab, contentParent);

            // Use a special method or overload that doesn't start a timer
            popup.ShowStatic(entry);
        }

        StartCoroutine(ScrollToBottomNextFrame()); // <-- Fix here
        isMenuOpen = true;
        animator.SetTrigger("ShowMenu");       
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        UIManager.Instance.SetHintVisible(true);
        UIManager.Instance.SetHintText(textSwipeLeft.GetLocalizedString());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return new WaitForEndOfFrame(); // Wait one frame for UI to rebuild
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void HideMenu()
    {
        isMenuOpen = false;
        animator.SetTrigger("HideMenu");
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        //if game phase is kickoff -> text tap : else -> textSwipeUp
        if (GameManager.Instance.CurrentPhase == GamePhase.Kickoff) 
        {
            UIManager.Instance.SetHintText(textKickOff.GetLocalizedString());        
        } else {
            UIManager.Instance.SetHintText(textSwipeUp.GetLocalizedString());
        }

        if (UIManager.Instance.IsDuelMenuOpen() || UIManager.Instance.IsSecretMenuOpen() || PauseManager.Instance.IsPaused)
            UIManager.Instance.SetHintVisible(false);
    }

    public bool IsMenuOpen() 
    {
        return isMenuOpen;
    }

    private bool CanOpenMenu() 
    {
        return GameManager.Instance.IsTimeFrozen && !IsMenuOpen();
    }

    private bool CanCloseMenu() 
    {
        return IsMenuOpen();
    }

    private void OpenMenu() 
    {
        if (CanOpenMenu()) 
            PopulateLog();
    }

    private void CloseMenu() 
    {
        if (CanCloseMenu())
            HideMenu();
    }

    
    private void HandleSwipe(SwipeDetector.SwipeDirection dir)
    {
        if (InputManager.Instance.IsDragging) return;
        if (InputManager.Instance.SwipeDetector.WasConsumedThisFrame()) return;

        if (dir == SwipeDetector.SwipeDirection.Right) 
        {
            OpenMenu();
        }

        if (dir == SwipeDetector.SwipeDirection.Left) 
        {
            CloseMenu();
        }

    }

    private void HandleDuelLogKey() {
        if (!IsMenuOpen()) {
            OpenMenu();
        } else 
        {
            CloseMenu();
        }
    }

}
