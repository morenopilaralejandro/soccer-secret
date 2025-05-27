using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;

public class CSVSecretImporter
{
    [MenuItem("Tools/Import Secrets From CSV")]
    public static void ImportSecretsFromCSV()
    {
        string defaultPath = Application.dataPath + "/Csv";
        string path = EditorUtility.OpenFilePanel("Select CSV File", defaultPath, "csv");
        string tableCollectionName = "SecretNames";
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

        string assetFolder = "Assets/Resources/ScriptableObjects/Secret";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(assetFolder)) {
            AssetDatabase.CreateFolder("Assets/Resources", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(assetFolder)) {
            AssetDatabase.CreateFolder("Assets/Resources/ScriptableObjects", "Secret");
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSV file does not contain enough lines.");
            return;
        }

        // Get CSV header index mapping
        string[] headers = lines[0].Split(',');

        int secretIdIndex       = System.Array.IndexOf(headers, "id");
        int secretNameEnIndex   = System.Array.IndexOf(headers, "name-en");
        int secretNameJaIndex   = System.Array.IndexOf(headers, "name-ja");
        int categoryIndex       = System.Array.IndexOf(headers, "category");
        int elementIndex        = System.Array.IndexOf(headers, "element");
        int powerIndex          = System.Array.IndexOf(headers, "power");
        int costIndex           = System.Array.IndexOf(headers, "cost");

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            SecretData secretData = ScriptableObject.CreateInstance<SecretData>();

            secretData.secretId      = values[secretIdIndex].Trim();
            secretData.secretNameEn  = values[secretNameEnIndex].Trim();
            secretData.secretNameJa  = values[secretNameJaIndex].Trim();
            secretData.category       = values[categoryIndex].Trim();
            secretData.element       = values[elementIndex].Trim();
            secretData.power         = int.Parse(values[powerIndex]);
            secretData.cost          = int.Parse(values[costIndex]);

            string safeName = secretData.secretId.Replace(" ", "_").Replace("/", "_");
            string assetPath = $"{assetFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(secretData, assetPath);

            foreach (var locale in locales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    var tableAsset = collection.AddNewTable(locale.Identifier) as StringTable;
                    table = tableAsset;
                }
                table.AddEntry(secretData.secretId, locale.Identifier.Code == "ja" ? secretData.secretNameJa : secretData.secretNameEn);
                EditorUtility.SetDirty(table);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Secret ScriptableObjects created from CSV!");
    }
}
