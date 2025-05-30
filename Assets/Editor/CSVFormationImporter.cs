using UnityEngine;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using System.IO;

public class CSVFormationImporter
{
    [MenuItem("Tools/Import Data From CSV/Import Formations From CSV")]
    public static void ImportFormationsFromCSV()
    {
        string defaultPath = Application.dataPath + "/Csv";
        string path = EditorUtility.OpenFilePanel("Select Formation CSV File", defaultPath, "csv");
        string tableCollectionName = "FormationNames";
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

        string assetFolder = "Assets/Resources/ScriptableObjects/Formation";
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
            AssetDatabase.CreateFolder("Assets/Resources/ScriptableObjects", "Formation");
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSV file does not contain enough lines.");
            return;
        }

        string[] headers = lines[0].Split(',');

        int formationIdIndex        = System.Array.IndexOf(headers, "id");
        int formationNameEnIndex    = System.Array.IndexOf(headers, "name-en");
        int formationNameJaIndex    = System.Array.IndexOf(headers, "name-ja");
        int coord0Index    = System.Array.IndexOf(headers, "coord0");
        int coord1Index    = System.Array.IndexOf(headers, "coord1");
        int coord2Index    = System.Array.IndexOf(headers, "coord2");
        int coord3Index    = System.Array.IndexOf(headers, "coord3");
        int kickOffIndex   = System.Array.IndexOf(headers, "kick-off");

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');

            FormationData formationData = ScriptableObject.CreateInstance<FormationData>();
            formationData.formationId      = values[formationIdIndex].Trim();
            formationData.formationNameEn  = values[formationNameEnIndex].Trim();
            formationData.formationNameJa  = values[formationNameJaIndex].Trim();
            formationData.coordIds = new string[4]
            {
                values[coord0Index].Trim(),
                values[coord1Index].Trim(),
                values[coord2Index].Trim(),
                values[coord3Index].Trim()
            };
            formationData.kickOff = int.Parse(values[kickOffIndex].Trim());

            string safeName = formationData.formationId.Replace(" ", "_").Replace("/", "_");
            string assetPath = $"{assetFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(formationData, assetPath);

            foreach (var locale in locales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    var tableAsset = collection.AddNewTable(locale.Identifier) as StringTable;
                    table = tableAsset;
                }
                table.AddEntry(formationData.formationId, locale.Identifier.Code == "ja" ? formationData.formationNameJa : formationData.formationNameEn);
                EditorUtility.SetDirty(table);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Formation ScriptableObjects created from CSV!");
    }
}
