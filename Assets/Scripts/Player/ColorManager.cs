using System.Collections.Generic;
using UnityEngine;

public static class ColorManager
{
    static Dictionary<string, Color> pigmentColors = new Dictionary<string, Color>()
    {
        {"white",        new Color(1.00f, 0.99f, 0.96f)},
        {"default",      new Color(0.93f, 0.73f, 0.52f)},
        {"tanned",       new Color(0.78f, 0.60f, 0.39f)},
        {"asian",        new Color(0.92f, 0.80f, 0.53f)},
        {"hispanic",     new Color(0.73f, 0.49f, 0.32f)},
        {"indian",       new Color(0.52f, 0.26f, 0.18f)},
        {"arab",         new Color(0.68f, 0.45f, 0.26f)},
        {"african",      new Color(0.21f, 0.13f, 0.05f)},
        {"black",        new Color(0.10f, 0.07f, 0.04f)},
        {"green",        new Color(0.25f, 0.56f, 0.31f)}
    };

    static Dictionary<string, Color> hairColors = new Dictionary<string, Color>()
    {
        {"black",   new Color(0.11f, 0.10f, 0.10f)},
        {"blonde",  new Color(1.00f, 0.92f, 0.51f)},
        {"blue",    new Color(0.23f, 0.51f, 0.87f)},
        {"brown",   new Color(0.37f, 0.22f, 0.09f)},
        {"green",   new Color(0.24f, 0.54f, 0.18f)},
        {"orange",  new Color(1.00f, 0.63f, 0.24f)},
        {"pink",    new Color(1.00f, 0.16f, 1.00f)},
        {"purple",  new Color(0.65f, 0.28f, 0.72f)},
        {"red",     new Color(0.80f, 0.07f, 0.09f)},
        {"white",   Color.white}
    };

    static Dictionary<string, Color> accessoryColors = new Dictionary<string, Color>()
    {
        {"default", Color.white},
        {"black",   Color.black},
        {"blue",    new Color(0.13f, 0.40f, 0.81f)},
        {"red",     new Color(0.80f, 0.16f, 0.14f)},
        {"purple",  new Color(0.41f, 0.33f, 0.56f)},
        {"yellow",  new Color(0.98f, 0.87f, 0.49f)}
    };

    static Dictionary<string, Color> teamIndicatorColors = new Dictionary<string, Color>()
    {
        {"ally",    Color.blue},
        {"opp",     Color.red},
    };

    static Dictionary<string, Color> duelOutcomeColors = new Dictionary<string, Color>()
    {
        {"win",     new Color(0.98f, 0.84f, 0.09f)},
        {"lose",    new Color(0.61f, 0.61f, 1.00f)},
    };

    public static Color GetPigmentColor(string pigment)
    {
        if(pigmentColors.TryGetValue(pigment.ToLower(), out var color))
            return color;
        return pigmentColors["default"];
    }

    public static Color GetHairColor(string hairColor)
    {
        if(hairColors.TryGetValue(hairColor.ToLower(), out var color))
            return color;
        return hairColors["black"];
    }

    public static Color GetAccessoryColor(string accessoryColor)
    {
        if(accessoryColors.TryGetValue(accessoryColor.ToLower(), out var color))
            return color;
        return accessoryColors["default"];
    }

    public static Color GetTeamIndicatorColor(string teamIndicatorColor)
    {
        if(teamIndicatorColors.TryGetValue(teamIndicatorColor.ToLower(), out var color))
            return color;
        return teamIndicatorColors["ally"];
    }

    public static Color GetDuelOutcomeColor(string duelOutcomeColor)
    {
        if(duelOutcomeColors.TryGetValue(duelOutcomeColor.ToLower(), out var color))
            return color;
        return duelOutcomeColors["win"];
    }
}
