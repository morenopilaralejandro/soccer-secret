using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public bool IsStatusLocked { get; private set; } = false;

    // User and AI selection data
    public int UserIndex { get; private set; }
    public Player UserPlayer { get; private set; }
    public Category UserCategory { get; private set; }

    public int AiIndex { get; private set; }
    public Player AiPlayer { get; private set; }
    public Category AiCategory { get; private set; }

    [Header("Panels & UI Elements")]
    [SerializeField] private GameObject panelSecret;
    [SerializeField] private GameObject panelCommand;
    [SerializeField] private PanelStatusSide panelStatusSideAlly;
    [SerializeField] private PanelStatusSide panelStatusSideOpp;
    [SerializeField] private GameObject buttonDuelToggle;
    [SerializeField] private GameObject buttonSwap;
    [SerializeField] private GameObject textWin;
    [SerializeField] private GameObject textLose;
    [SerializeField] private Image imageCategory;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Uncomment to persist across scenes if required
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
            ComboCollider.OnSetStatusPlayer += SetStatusPlayer;
            KeeperCollider.OnSetStatusPlayer += SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand += SetStatusPlayerAndCommand;
        }
        else
        {
            BallBehavior.OnSetStatusPlayer -= SetStatusPlayer;
            DuelCollider.OnSetStatusPlayer -= SetStatusPlayer;
            ComboCollider.OnSetStatusPlayer -= SetStatusPlayer;
            KeeperCollider.OnSetStatusPlayer -= SetStatusPlayer;
            DuelManager.OnSetStatusPlayerAndCommand -= SetStatusPlayerAndCommand;
        }
    }
    #endregion

    #region Status Panel Lock/Unlock

    public void LockStatus() => IsStatusLocked = true;
    public void UnlockStatus() => IsStatusLocked = false;

    #endregion

    #region Panel & Button Visibility
    public void SetImageCategoryVisible(bool visible) => SetActiveSafe(imageCategory.gameObject, visible);
    public void SetPanelSecretVisible(bool visible) => SetActiveSafe(panelSecret, visible);
    public void SetPanelCommandVisible(bool visible) => SetActiveSafe(panelCommand, visible);

    public void SetButtonDuelToggleVisible(bool visible)
    {
        SetActiveSafe(buttonDuelToggle, visible);
        SetActiveSafe(buttonSwap, visible);
        SetImageCategoryVisible(true);
        imageCategory.sprite = SecretManager.Instance.GetCategoryIcon(UserCategory);
    }

    public void HideDuelUi()
    {
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);
        SetButtonDuelToggleVisible(false);
        SetImageCategoryVisible(false);
    }

    #endregion

    #region Duel Toggle & Command Button Logic

    public void OnButtonDuelToggleTapped()
    {
        Debug.Log("ButtonDuelToggle tapped!");
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        // Toggle open/close dual panels
        if (!IsPanelActive(panelSecret) && !IsPanelActive(panelCommand))
            SetPanelCommandVisible(true);
        else
        {
            SetPanelCommandVisible(false);
            SetPanelSecretVisible(false);
        }
    }

    public void OnButtonBackTapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuBack");
        Debug.Log("ButtonBack tapped!");
        SetPanelSecretVisible(false);
        SetPanelCommandVisible(true);
    }

    public void OnCommand0Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxSecretCommand");
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
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        Debug.Log("Command1 tapped!");
        HandleRegister(DuelCommand.Phys, null);
    }

    public void OnCommand2Tapped()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        Debug.Log("Command2 tapped!");
        HandleRegister(DuelCommand.Skill, null);
    }

    public void OnSecretCommandSlotTapped(SecretCommandSlot secretCommandSlot)
    {
        Debug.Log("SecretCommandSlot tapped: " + secretCommandSlot.name);
        if (secretCommandSlot.Secret == null)
            return;
        if (UserPlayer.GetStat(PlayerStats.Sp) < secretCommandSlot.Secret.Cost) 
        {
            AudioManager.Instance.PlaySfx("SfxForbidden");
            return;
        }

        SetPanelSecretVisible(false);
        SetPanelCommandVisible(false);

        AudioManager.Instance.PlaySfx("SfxSecretSelect");
        HandleRegister(DuelCommand.Secret, secretCommandSlot.Secret);
    }

    #endregion

    #region Selection Registration
    private void HandleRegister(DuelCommand command, Secret secret) 
    {
        RegisterUserSelections(command, secret);

        if (UserCategory == Category.Dribble)
            RegisterAiSelections();

        HideDuelUi();
        if (GameManager.Instance != null)
            GameManager.Instance.UnfreezeGame();
    }

    public void RegisterUserSelections(DuelCommand command, Secret secret)
    {
        DuelAction action = DuelManager.Instance.GetActionByCategory(UserCategory);
        if (DuelManager.Instance != null)
            DuelManager.Instance.RegisterUISelections(UserIndex, UserCategory, action, command, secret);
    }

    public void RegisterAiSelections()
    {
        if (AiPlayer != null)
            AiPlayer.GetComponent<PlayerAi>().RegisterAiSelections(AiIndex, AiCategory);
    }

    public void SetUserRole(Category category, int index, Player player)
    {
        UserCategory = category;
        UserIndex = index;
        UserPlayer = player;
    }
    public void SetAiRole(Category category, int index, Player player)
    {
        AiCategory = category;
        AiIndex = index;
        AiPlayer = player;
    }
    #endregion

    #region Duel Status Display

    public void ShowTextDuelResult(DuelParticipant winningPart)
    {
        if (winningPart?.Player == null)
            return;
        SetActiveSafe(textWin, winningPart.Player.IsAlly);
        SetActiveSafe(textLose, !winningPart.Player.IsAlly);
    }

    public void HideStatus()
    {
        panelStatusSideAlly?.SetPlayer(null);
        panelStatusSideOpp?.SetPlayer(null);
        SetActiveSafe(textWin, false);
        SetActiveSafe(textLose, false);
    }

    public void HideStatusPlayer(Player player)
    {
        if (player == null) return;
        if (player.IsAlly)
            panelStatusSideAlly?.SetPlayer(null);
        else
            panelStatusSideOpp?.SetPlayer(null);
    }

    public void SetStatusPlayer(Player player)
    {
        if (player == null) return;
        if (player.IsAlly)
            panelStatusSideAlly?.SetPlayer(player);
        else
            panelStatusSideOpp?.SetPlayer(player);
    }

    public void SetStatusPlayerAndCommand(DuelParticipant duelParticipant, float attackPressure)
    {
        var player = duelParticipant?.Player;
        if (player == null) return;
        if (player.IsAlly)
            panelStatusSideAlly?.SetPlayerAndCommand(duelParticipant, attackPressure);
        else
            panelStatusSideOpp?.SetPlayerAndCommand(duelParticipant, attackPressure);
    }
    #endregion

    #region Utility

    /// <summary>
    /// Sets a GameObject active only if it exists and if the state is different than desired
    /// </summary>
    private void SetActiveSafe(GameObject obj, bool shouldBeActive)
    {
        if (obj != null && obj.activeSelf != shouldBeActive)
            obj.SetActive(shouldBeActive);
    }

    /// <summary>
    /// Returns true if a panel GameObject is non-null and active.
    /// </summary>
    private bool IsPanelActive(GameObject obj) => obj != null && obj.activeSelf;

    #endregion
}

