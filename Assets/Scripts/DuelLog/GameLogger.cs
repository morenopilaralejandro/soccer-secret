using UnityEngine;

public static class GameLogger
{
    public enum LogLevel
    {
        Verbose = 0, // Most detailed
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    // Minimum log level for printing messages
    public static LogLevel MinimumLevel = LogLevel.Verbose;

    public static void Log(string message, LogLevel level = LogLevel.Info, UnityEngine.Object context = null)
    {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (level >= MinimumLevel)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(message, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Debug.LogError(message, context);
                    break;
            }
        }
    #endif
    }

    // Optional Helper Methods for each log level
    public static void Verbose(string message, UnityEngine.Object context = null)
        => Log(message, LogLevel.Verbose, context);

    public static void DebugLog(string message, UnityEngine.Object context = null)
        => Log(message, LogLevel.Debug, context);

    public static void Info(string message, UnityEngine.Object context = null)
        => Log(message, LogLevel.Info, context);

    public static void Warning(string message, UnityEngine.Object context = null)
        => Log(message, LogLevel.Warning, context);

    public static void Error(string message, UnityEngine.Object context = null)
        => Log(message, LogLevel.Error, context);

    public static void Fatal(string message, UnityEngine.Object context = null)
        => Log(message, LogLevel.Fatal, context);
}

/*
Usage: Log with the appropriate LogLevel.
GameLogger.Verbose("A very detailed message.", this);
GameLogger.DebugLog("Debug info", this);
GameLogger.Info("Something happened.", this);
GameLogger.Warning("Potential issue.", this);
GameLogger.Error("An error occurred.", this);
GameLogger.Fatal("Critical failure!", this);
*/
