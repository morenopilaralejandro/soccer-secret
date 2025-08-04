using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class DuelLogManager : MonoBehaviour
{
    #region Fields

    public static DuelLogManager Instance { get; private set; }

    public event System.Action<DuelLogEntry> OnNewEntry;

    public List<DuelLogEntry> DuelLogEntries = new List<DuelLogEntry>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Add Methods

    private void AddEntry(LocalizedString localizedString, GameLogger.LogLevel logLevel) 
    {
        DuelLogEntry entry = new DuelLogEntry(localizedString, logLevel);
        DuelLogEntries.Add(entry);
        GameLogger.Verbose("[DuelLogManager] Shown: " + (logLevel == GameLogger.LogLevel.Info) + ". String: " + entry.LocalizedString.GetLocalizedString());
        if (logLevel == GameLogger.LogLevel.Info)
            OnNewEntry?.Invoke(entry);
    }

    public void AddMatchStart()
    {
        var localizedString = new LocalizedString("DuelLogTexts", "MatchStart");
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddMatchEnd()
    {
        var localizedString = new LocalizedString("DuelLogTexts", "MatchEnd");
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddMatchPause(int teamIndex)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "MatchPause");
        localizedString.Arguments = new object[] { new { teamColor = GetTeamColor(teamIndex), teamName = GetTeamName(teamIndex) } };
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddMatchResume()
    {
        if (DuelLogEntries.Count > 3) 
        {
            var localizedString = new LocalizedString("DuelLogTexts", "MatchResume");
            var logLevel = GameLogger.LogLevel.Info;
            AddEntry(localizedString, logLevel);
        }
    }

    public void AddActionPass(Player player)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "ActionPass");
        localizedString.Arguments = new object[] { new { teamColor = GetTeamColor(player.TeamIndex), playerName = GetPlayerName(player) } };
        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddActionShoot(Player player)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "ActionShoot");
        localizedString.Arguments = new object[] { new { teamColor = GetTeamColor(player.TeamIndex), playerName = GetPlayerName(player) } };
        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddActionCommand(Player player, DuelCommand command, Secret secret)
    {
        string teamColor = GetTeamColor(player.TeamIndex);
        string playerName = GetPlayerName(player);
        string commandColor, commandName;

        if (secret)
        {
            commandName = secret.SecretName;
            commandColor = ColorManager.ColorToHex(ElementManager.Instance.GetElementColor(secret.Element));
        }
        else
        {
            commandName = command == DuelCommand.Phys
                ? new LocalizedString("UITexts", "Command1").GetLocalizedString()
                : new LocalizedString("UITexts", "Command2").GetLocalizedString();
            commandColor = ColorManager.ColorToHex(Color.white);
        }

        var localizedString = new LocalizedString("DuelLogTexts", "ActionCommand");
        localizedString.Arguments = new object[] { new { 
            teamColor = teamColor,
            playerName = playerName,
            commandColor = commandColor,
            commandName = commandName
        }};

        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddActionDamage(DuelAction action, float damage)
    {
        string actionSymbol = (action == DuelAction.Offense)
            ? new LocalizedString("UITexts", "SymbolPlus").GetLocalizedString()
            : new LocalizedString("UITexts", "SymbolMinus").GetLocalizedString();

        string damageNumber = Mathf.RoundToInt(damage).ToString();

        var localizedString = new LocalizedString("DuelLogTexts", "ActionDamage");
        localizedString.Arguments = new object[] { new { 
            actionSymbol = actionSymbol,
            damageNumber = damageNumber
        }};

        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddActionScore(Player player)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "ActionScore");
        localizedString.Arguments = new object[] { new { teamColor = GetTeamColor(player.TeamIndex), playerName = GetPlayerName(player) } };
        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddActionStop(Player player)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "ActionStop");
        localizedString.Arguments = new object[] { new { teamColor = GetTeamColor(player.TeamIndex), playerName = GetPlayerName(player) } };
        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddElementOffense(Category category)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "ElementOffense");
        localizedString.Arguments = new object[] { new { categoryColor = GetCategoryColor(category) } };
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddElementDefense(Category category)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "ElementDefense");
        localizedString.Arguments = new object[] { new { categoryColor = GetCategoryColor(category) } };
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddDuel()
    {
        var localizedString = new LocalizedString("DuelLogTexts", "Duel");
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddDuelWin(int teamIndex)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "DuelWin");
        localizedString.Arguments = new object[] { new { outcomeColor = GetOutcomeColor(teamIndex) } };
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddDuelLose(int teamIndex)
    {
        var localizedString = new LocalizedString("DuelLogTexts", "DuelLose");
        localizedString.Arguments = new object[] { new { outcomeColor = GetOutcomeColor(teamIndex) } };
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    public void AddDuelCancel()
    {
        var localizedString = new LocalizedString("DuelLogTexts", "DuelCancel");
        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddGoal()
    {
        var localizedString = new LocalizedString("DuelLogTexts", "Goal");
        var logLevel = GameLogger.LogLevel.Verbose;
        AddEntry(localizedString, logLevel);
    }

    public void AddGainPossession(Player player)
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Kickoff)
        {
            var localizedString = new LocalizedString("DuelLogTexts", "GainPossession");
            localizedString.Arguments = new object[] { new { teamColor = GetTeamColor(player.TeamIndex), playerName = GetPlayerName(player) } };
            var logLevel = GameLogger.LogLevel.Verbose;
            AddEntry(localizedString, logLevel);
        }
    }

    public void AddCondition()
    {
        var localizedString = new LocalizedString("DuelLogTexts", "Condition");
        localizedString.Arguments = new object[] { new { condition = new LocalizedString("UITexts", "ConditionScore3").GetLocalizedString() } };
        var logLevel = GameLogger.LogLevel.Info;
        AddEntry(localizedString, logLevel);
    }

    #endregion

    #region Helper Methods

    private string GetTeamColor(int teamIndex)
    {
        Color auxColor = ColorManager.GetTeamIndicatorColor(teamIndex);
        string hexColor = ColorManager.ColorToHex(auxColor);
        return hexColor;
    }

    private string GetTeamName(int teamIndex)
    {
        return GameManager.Instance.Teams[teamIndex].TeamName;
    }

    private string GetPlayerName(Player player)
    {
        return player.PlayerName;
    }

    private string GetCategoryColor(Category category)
    {
        Color auxColor = SecretManager.Instance.GetCategoryColor(category);
        string hexColor = ColorManager.ColorToHex(auxColor);
        return hexColor;
    }

    private string GetOutcomeColor(int teamIndex)
    {
        Color auxColor = ColorManager.GetDuelOutcomeColor(teamIndex);
        string hexColor = ColorManager.ColorToHex(auxColor);
        return hexColor;
    }

    #endregion

    #region Other Methods

    public void Clear()
    {
        DuelLogEntries.Clear();
    }

    #endregion
}
