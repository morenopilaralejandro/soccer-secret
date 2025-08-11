public enum PitchMaterial
{
    Grass,
    Dirt,
    Ice,
    Lava
}

public enum MatchTime
{
    Endless,
    Short,
    Long
}

public enum WinScore
{
    None,
    One,
    Three
}

public static class BattleArgs
{
    public static string TeamId0;
    public static string TeamId1;
    public static PitchMaterial PitchMaterial;
    public static MatchTime MatchTime;
    public static WinScore WinScore;

    public static void Clear()
    {
        TeamId0 = null;
        TeamId1 = null;
        PitchMaterial = PitchMaterial.Grass;
        MatchTime = MatchTime.Long;
        WinScore = WinScore.Three;
    }
}
