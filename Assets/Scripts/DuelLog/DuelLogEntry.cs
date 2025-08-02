using UnityEngine;
using UnityEngine.Localization;

public class DuelLogEntry
{
    public LocalizedString LocalizedString;
    public GameLogger.LogLevel LogLevel; 

    public DuelLogEntry(LocalizedString localizedString, GameLogger.LogLevel logLevel)
    {
        LocalizedString = localizedString;
        LogLevel = logLevel;
    }
}
