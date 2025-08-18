using UnityEngine;
using UnityEngine.Localization;

public static class ConditionManager
{

    public static string GetConditionByWinScore(WinScore winScore) 
    {
        LocalizedString localizedString = null;

        switch (winScore) 
        {
            case WinScore.One:
                localizedString = new LocalizedString("UITexts", "ConditionScore1");
                break;
            case WinScore.Three:
                localizedString = new LocalizedString("UITexts", "ConditionScore3");
                break;
            case WinScore.Five:
                localizedString = new LocalizedString("UITexts", "ConditionScore5");
                break;
        }

        return localizedString.GetLocalizedString();
    }
}
