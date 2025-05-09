using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Singleton instance (optional, for easy accessibility)
    public static UIManager Instance { get; private set; }
    public int DuelPlayerIndex { get; set; }
    public Player DuelPlayer { get; set; }
    public Category SecretCat { get; set; }

    // Assign this in the Inspector or with Find/other methods
    [SerializeField] private GameObject panelSecret;
    [SerializeField] private GameObject panelCommand;
    [SerializeField] private GameObject buttonDuelToggle;

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Toggle method
    public void SetPanelSecretVisible(bool visible)
    {
        panelSecret.SetActive(visible);
    }

    public void SetPanelCommandVisible(bool visible)
    {
        panelCommand.SetActive(visible);
    }

    public void SetButtonDuelToggleVisible(bool visible)
    {
        buttonDuelToggle.SetActive(visible);
    }

    public void HideDuelUi()
    {
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);
        SetButtonDuelToggleVisible(false);
    }

    public void OnButtonDuelToggleTapped()
    {
        Debug.Log("ButtonDuelToggle tapped!");
        if (!panelSecret.activeSelf && !panelCommand.activeSelf) 
        {
            SetPanelCommandVisible(true);
        } else {
            SetPanelCommandVisible(false);
            SetPanelSecretVisible(false);
        }
        
    }

    public void OnButtonBackTapped()
    {
        Debug.Log("ButtonBack tapped!");
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(true);
    }

    public void OnCommand0Tapped()
    {
        Debug.Log("Command0 tapped!");
        SetPanelSecretVisible(true);
        SetPanelCommandVisible(false);
        panelSecret.GetComponent<SecretPanel>().UpdateSecretSlots(DuelPlayer.CurrentSecret, SecretCat);
    }

    public void OnCommand1Tapped()
    {
        Debug.Log("Command1 tapped!");
        GameManager.Instance.ExecuteDuel(DuelPlayerIndex, 1, null);
    }

    public void OnCommand2Tapped()
    {
        Debug.Log("Command2 tapped!");
        GameManager.Instance.ExecuteDuel(DuelPlayerIndex, 2, null);
    }

    public void OnSecretCommandSlotTapped(SecretCommandSlot secretCommandSlot)
    {
        Debug.Log("SecretCommandSlot tapped! " + secretCommandSlot.name);
        if (secretCommandSlot.Secret != null) {
            SetPanelSecretVisible(true);
            SetPanelCommandVisible(false);
            GameManager.Instance.ExecuteDuel(DuelPlayerIndex, 0, secretCommandSlot.Secret);
        }
    }
}
