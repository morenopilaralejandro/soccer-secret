using UnityEngine;

public static class GameLogger
{
    public enum LogLevel { Info, Warning, Error }
    public static LogLevel MinimumLevel = LogLevel.Info;

    public static void Log(string message, LogLevel level = LogLevel.Info, UnityEngine.Object context = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (level >= MinimumLevel)
        {
            switch (level)
            {
                case LogLevel.Info:
                    Debug.Log(message, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message, context);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message, context);
                    break;
            }
        }
#endif
    }
}

/*
Usage: Log only important events (state change, error, or key actions)
GameLogger.Log("[ComboCollider] Could not find Player in parent.", GameLogger.LogLevel.Error, this);
GameLogger.Log($"[ComboCollider] Found Player: {_cachedPlayer.name}", GameLogger.LogLevel.Info, this);
*/
