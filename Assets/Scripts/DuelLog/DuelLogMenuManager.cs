using UnityEngine;     // For MonoBehaviour, GameObject, etc.
using UnityEngine.UI;  // For ScrollRect
using System.Collections; // For IEnumerator and Coroutines

public class DuelLogMenuManager : MonoBehaviour
{
    [SerializeField] private DuelLogPopup popupPrefab;
    [SerializeField] private Transform contentParent; // ScrollView content
    [SerializeField] private GameObject panelDuelLogMenu; // ScrollView content
    [SerializeField] private Animator animator;
    [SerializeField] private ScrollRect scrollRect;
    
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
        panelDuelLogMenu.SetActive(true);
        animator.SetTrigger("ShowMenu");
    }

    private void HideMenu()
    {
        animator.SetTrigger("HideMenu");
        //panelDuelLogMenu.SetActive(false);
    }

    private bool IsMenuOpen() 
    {
        return panelDuelLogMenu.activeSelf;
    }

    private bool CanOpenMenu() 
    {
        return GameManager.Instance.IsTimeFrozen;
    }

    private bool CanCloseMenu() 
    {
        return IsMenuOpen();
    }

    private void OpenMenu() 
    {
        if (CanOpenMenu())
            PopulateLog();  
            scrollRect.verticalNormalizedPosition = 0f;
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
