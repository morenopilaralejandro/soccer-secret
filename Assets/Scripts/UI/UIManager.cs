using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Singleton instance (optional, for easy accessibility)
    public static UIManager Instance { get; private set; }
    public int UserIndex { get; set; }
    public Player UserPlayer { get; set; }
    public DuelAction UserAction { get; set; }
    public Category UserCategory { get; set; }

    public int AiIndex { get; set; }
    public Player AiPlayer { get; set; }
    public DuelAction AiAction { get; set; }
    public Category AiCategory { get; set; }

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
        panelSecret.GetComponent<SecretPanel>().UpdateSecretSlots(UserPlayer.CurrentSecret, UserCategory);
    }

    public void OnCommand1Tapped()
    {
        Debug.Log("Command1 tapped!");
        //DuelCommand.Phys Secret
            DuelManager.Instance.RegisterUISelections(
            UserIndex,
            UserCategory,
            UserAction,
            DuelCommand.Phys,
            null);

        if(UserCategory == Category.Dribble) {
            DuelManager.Instance.RegisterUISelections(
            AiIndex,
            AiCategory,
            AiAction,
            DuelCommand.Phys,
            null);
        }

        HideDuelUi();
        GameManager.Instance.UnfreezeGame();





    }

    public void OnCommand2Tapped()
    {
        Debug.Log("Command2 tapped!");
        GameManager.Instance.ExecuteDuel(UserIndex, 2, null);
    }

    public void OnSecretCommandSlotTapped(SecretCommandSlot secretCommandSlot)
    {
        Debug.Log("SecretCommandSlot tapped! " + secretCommandSlot.name);
        if (secretCommandSlot.Secret != null) {
            SetPanelSecretVisible(true);
            SetPanelCommandVisible(false);
            GameManager.Instance.ExecuteDuel(UserIndex, 0, secretCommandSlot.Secret);
        }
    }
}
