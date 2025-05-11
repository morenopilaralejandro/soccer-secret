using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public bool IsStatusLocked { get; private set; } = false;

    public int UserIndex { get; set; }
    public Player UserPlayer { get; set; }
    public DuelAction UserAction { get; set; }
    public Category UserCategory { get; set; }

    public int AiIndex { get; set; }
    public Player AiPlayer { get; set; }
    public DuelAction AiAction { get; set; }
    public Category AiCategory { get; set; }

    [SerializeField] private GameObject panelSecret;
    [SerializeField] private GameObject panelCommand;
    [SerializeField] private PanelStatusSide panelStatusSideAlly;
    [SerializeField] private PanelStatusSide panelStatusSideOpp;
    [SerializeField] private GameObject buttonDuelToggle;
    [SerializeField] private GameObject buttonSwap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Uncomment next line if you want it to persist across scenes:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HideStatus();
    }

    private void OnEnable() => SubscribeEvents(true);

    private void OnDisable() => SubscribeEvents(false);

    private void OnDestroy() => SubscribeEvents(false);

    private void SubscribeEvents(bool subscribe)
    {
        if (subscribe)
        {
            BallBehavior.OnSetStatusPlayer += SetStatusPlayer;
            DuelCollider.OnSetStatusPlayer += SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand += SetStatusPlayerAndCommand;
        }
        else
        {
            BallBehavior.OnSetStatusPlayer -= SetStatusPlayer;
            DuelCollider.OnSetStatusPlayer -= SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand -= SetStatusPlayerAndCommand;
        }
    }

    public void LockStatus() => IsStatusLocked = true;
    public void UnlockStatus() => IsStatusLocked = false;

    public void SetPanelSecretVisible(bool visible) => SetActiveSafe(panelSecret, visible);
    public void SetPanelCommandVisible(bool visible) => SetActiveSafe(panelCommand, visible);

    public void SetButtonDuelToggleVisible(bool visible)
    {
        SetActiveSafe(buttonDuelToggle, visible);
        SetActiveSafe(buttonSwap, visible);
    }

    public void HideDuelUi()
    {
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);
        SetButtonDuelToggleVisible(false);
    }
    // Button events
    public void OnButtonDuelToggleTapped()
    {
        Debug.Log("ButtonDuelToggle tapped!");
        bool panelSecretActive = panelSecret != null && panelSecret.activeSelf;
        bool panelCommandActive = panelCommand != null && panelCommand.activeSelf;

        if (!panelSecretActive && !panelCommandActive)
        {
            SetPanelCommandVisible(true);
        }
        else
        {
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
        if (UserPlayer != null && panelSecret != null) 
        {
            var secretPanel = panelSecret.GetComponent<SecretPanel>();
            if(secretPanel != null)
                secretPanel.UpdateSecretSlots(UserPlayer.CurrentSecret, UserCategory);
        }
    }

    public void OnCommand1Tapped()
    {
        Debug.Log("Command1 tapped!");
        RegisterUserSelections(DuelCommand.Phys, null);

        if(UserCategory == Category.Dribble) 
            RegisterAiSelections(DuelCommand.Phys, null);

        HideDuelUi();
        if (GameManager.Instance != null)
            GameManager.Instance.UnfreezeGame();
    }

    public void OnCommand2Tapped()
    {
        Debug.Log("Command2 tapped!");
        //GameManager.Instance.ExecuteDuel(UserIndex, 2, null);
    }

    public void OnSecretCommandSlotTapped(SecretCommandSlot secretCommandSlot)
    {
        Debug.Log("SecretCommandSlot tapped! " + secretCommandSlot.name);
        if (secretCommandSlot.Secret != null) {
            SetPanelSecretVisible(true);
            SetPanelCommandVisible(false);
            //GameManager.Instance.ExecuteDuel(UserIndex, 0, secretCommandSlot.Secret);
        }
    }

    // Register
    public void RegisterUserSelections(DuelCommand command, Secret secret) 
    {
        if (DuelManager.Instance != null)
            DuelManager.Instance.RegisterUISelections(UserIndex, UserCategory, UserAction, command, secret);
    }

    public void RegisterAiSelections(DuelCommand command, Secret secret) 
    {
        if (DuelManager.Instance != null)
            DuelManager.Instance.RegisterUISelections(AiIndex, AiCategory, AiAction, command, secret);
    }

    // Status
    public void HideStatus() 
    {
        if(panelStatusSideAlly != null) panelStatusSideAlly.SetPlayer(null);
        if(panelStatusSideOpp != null) panelStatusSideOpp.SetPlayer(null);
    }

    public void HideStatusPlayer(Player player) 
    {
        if(player == null) return;
        if(player.IsAlly && panelStatusSideAlly != null) 
            panelStatusSideAlly.SetPlayer(null);
        else if(panelStatusSideOpp != null) 
            panelStatusSideOpp.SetPlayer(null);
    }

    public void SetStatusPlayer(Player player) 
    {
        if(player == null) return;
        if(player.IsAlly && panelStatusSideAlly != null) 
            panelStatusSideAlly.SetPlayer(player);
        else if(panelStatusSideOpp != null) 
            panelStatusSideOpp.SetPlayer(player);
    }

    public void SetStatusPlayerAndCommand(DuelParticipant duelParticipant, float attackPressure) 
    {
        if(duelParticipant?.Player == null) return;
        if(duelParticipant.Player.IsAlly && panelStatusSideAlly != null)
            panelStatusSideAlly.SetPlayerAndCommand(duelParticipant, attackPressure);
        else if(panelStatusSideOpp != null)
            panelStatusSideOpp.SetPlayerAndCommand(duelParticipant, attackPressure);
    }

    // Utility
    private void SetActiveSafe(GameObject obj, bool value)
    {
        if (obj != null && obj.activeSelf != value)
            obj.SetActive(value);
    }
}
