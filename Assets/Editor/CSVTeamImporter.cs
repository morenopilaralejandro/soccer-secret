using UnityEngine;
using UnityEditor;
using System.IO;

public class CSVTeamImporter
{
    [MenuItem("Tools/Import Teams From CSV")]
    public static void ImportTeamsFromCSV()
    {
        string defaultPath = Application.dataPath + "/Csv";
        string path = EditorUtility.OpenFilePanel("Select Team CSV File", defaultPath, "csv");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No CSV file selected.");
            return;
        }

        string assetFolder = "Assets/Resources/ScriptableObjects/Team";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(assetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources/ScriptableObjects", "Team");
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSV file does not contain enough lines.");
            return;
        }

        string[] headers = lines[0].Split(',');

        int teamIdIndex        = System.Array.IndexOf(headers, "id");
        int teamNameEnIndex    = System.Array.IndexOf(headers, "name-en");
        int teamNameJaIndex    = System.Array.IndexOf(headers, "name-ja");
        int lvIndex    = System.Array.IndexOf(headers, "lv");
        int formationIndex = System.Array.IndexOf(headers, "formation");
        int player0Index   = System.Array.IndexOf(headers, "player0");
        int player1Index   = System.Array.IndexOf(headers, "player1");
        int player2Index   = System.Array.IndexOf(headers, "player2");
        int player3Index   = System.Array.IndexOf(headers, "player3");

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');

            TeamData teamData = ScriptableObject.CreateInstance<TeamData>();
            teamData.teamId = values[teamIdIndex].Trim();
            teamData.teamNameEn = values[teamNameEnIndex].Trim();
            teamData.teamNameJa = values[teamNameJaIndex].Trim();
            teamData.lv         = int.Parse(values[lvIndex]);
            teamData.formation = values[formationIndex].Trim();
            teamData.playerIds = new string[4]
            {
                values[player0Index].Trim(),
                values[player1Index].Trim(),
                values[player2Index].Trim(),
                values[player3Index].Trim()
            };

            string safeName = teamData.teamId.Replace(" ", "_").Replace("/", "_");
            string assetPath = $"{assetFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(teamData, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Team ScriptableObjects created from CSV!");
    }
}
