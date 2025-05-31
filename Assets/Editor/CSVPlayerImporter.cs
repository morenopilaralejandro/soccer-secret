using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using System.IO;

public class CSVPlayerImporter
{
    [MenuItem("Tools/Import Data From CSV/Import Players From CSV")]
    public static void ImportPlayersFromCSV()
    {
        string defaultPath = Application.dataPath + "/Csv";
        string path = EditorUtility.OpenFilePanel("Select CSV File", defaultPath, "csv");
        string tableCollectionName = "PlayerNames";
        string tablePath = "Assets/Localization/StringTables/" + tableCollectionName + "/";

        var locales = LocalizationEditorSettings.GetLocales();
        var collection = LocalizationEditorSettings.GetStringTableCollection(tableCollectionName);
        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection(tableCollectionName, tablePath);
        }


        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No CSV file selected.");
            return;
        }

        string assetFolder = "Assets/Resources/ScriptableObjects/Player";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(assetFolder)) {
            AssetDatabase.CreateFolder("Assets/Resources", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(assetFolder)) {
            AssetDatabase.CreateFolder("Assets/Resources/ScriptableObjects", "Player");
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSV file does not contain enough lines.");
            return;
        }

        // Get CSV header index mapping
        string[] headers = lines[0].Split(',');

        int playerIdIndex       = System.Array.IndexOf(headers, "id");
        int playerNameEnIndex   = System.Array.IndexOf(headers, "name-en");
        int playerNameJaIndex   = System.Array.IndexOf(headers, "name-ja");
        int genderIndex         = System.Array.IndexOf(headers, "gender");
        int elementIndex        = System.Array.IndexOf(headers, "element");
        int positionIndex       = System.Array.IndexOf(headers, "position");
        int hpIndex             = System.Array.IndexOf(headers, "hp");
        int spIndex             = System.Array.IndexOf(headers, "sp");
        int kickIndex           = System.Array.IndexOf(headers, "kick");
        int bodyIndex           = System.Array.IndexOf(headers, "body");
        int controlIndex        = System.Array.IndexOf(headers, "control");
        int guardIndex          = System.Array.IndexOf(headers, "guard");
        int speedIndex          = System.Array.IndexOf(headers, "speed");
        int staminaIndex        = System.Array.IndexOf(headers, "stamina");
        int courageIndex        = System.Array.IndexOf(headers, "courage");
        int freedomIndex        = System.Array.IndexOf(headers, "freedom");
        int secret0Index        = System.Array.IndexOf(headers, "secret0");
        int secret1Index        = System.Array.IndexOf(headers, "secret1");
        int secret2Index        = System.Array.IndexOf(headers, "secret2");
        int secret3Index        = System.Array.IndexOf(headers, "secret3");
        int lv0Index            = System.Array.IndexOf(headers, "lv0");
        int lv1Index            = System.Array.IndexOf(headers, "lv1");
        int lv2Index            = System.Array.IndexOf(headers, "lv2");
        int lv3Index            = System.Array.IndexOf(headers, "lv3");



        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            PlayerData playerData = ScriptableObject.CreateInstance<PlayerData>();

            playerData.playerId      = values[playerIdIndex].Trim();
            playerData.playerNameEn  = values[playerNameEnIndex].Trim();
            playerData.playerNameJa  = values[playerNameJaIndex].Trim();
            playerData.gender        = values[genderIndex].Trim();
            playerData.element       = values[elementIndex].Trim();
            playerData.position      = values[positionIndex].Trim();
            playerData.hp            = int.Parse(values[hpIndex]);
            playerData.sp            = int.Parse(values[spIndex]);
            playerData.kick          = int.Parse(values[kickIndex]);
            playerData.body          = int.Parse(values[bodyIndex]);
            playerData.control       = int.Parse(values[controlIndex]);
            playerData.guard         = int.Parse(values[guardIndex]);
            playerData.speed         = int.Parse(values[speedIndex]);
            playerData.stamina       = int.Parse(values[staminaIndex]);
            playerData.courage       = int.Parse(values[courageIndex]);
            playerData.freedom       = int.Parse(values[freedomIndex]);

            string[] auxStringArr = values[secret0Index].Split('-');
            playerData.secret0       = auxStringArr[auxStringArr.Length -1];
            auxStringArr = values[secret1Index].Split('-');
            playerData.secret1       = auxStringArr[auxStringArr.Length -1];
            auxStringArr = values[secret2Index].Split('-');
            playerData.secret2       = auxStringArr[auxStringArr.Length -1];
            auxStringArr = values[secret3Index].Split('-');
            playerData.secret3       = auxStringArr[auxStringArr.Length -1];

            playerData.lv0           = int.Parse(values[lv0Index]);
            playerData.lv1           = int.Parse(values[lv1Index]);
            playerData.lv2           = int.Parse(values[lv2Index]);
            playerData.lv3           = int.Parse(values[lv3Index]);

            string safeName = playerData.playerId.Replace(" ", "_").Replace("/", "_");
            string assetPath = $"{assetFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(playerData, assetPath);


            foreach (var locale in locales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    var tableAsset = collection.AddNewTable(locale.Identifier) as StringTable;
                    table = tableAsset;
                }
                table.AddEntry(playerData.playerId, locale.Identifier.Code == "ja" ? playerData.playerNameJa : playerData.playerNameEn);
                EditorUtility.SetDirty(table);
            }

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Player ScriptableObjects created from CSV!");
    }
}
