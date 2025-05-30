using UnityEngine;
using UnityEditor;
using System.IO;

public class CSVCoordImporter
{
    [MenuItem("Tools/Import Data From CSV/Import Coords From CSV")]
    public static void ImportCoordsFromCSV()
    {
        string defaultPath = Application.dataPath + "/Csv";
        string path = EditorUtility.OpenFilePanel("Select Coordination CSV File", defaultPath, "csv");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No CSV file selected.");
            return;
        }

        string assetFolder = "Assets/Resources/ScriptableObjects/Coord";
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
            AssetDatabase.CreateFolder("Assets/Resources/ScriptableObjects", "Coord");
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSV file does not contain enough lines.");
            return;
        }

        // Get CSV header index mapping
        string[] headers = lines[0].Split(',');

        int coordIdIndex = System.Array.IndexOf(headers, "id");
        int xIndex = System.Array.IndexOf(headers, "x");
        int yIndex = System.Array.IndexOf(headers, "y");
        int zIndex = System.Array.IndexOf(headers, "z");

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            CoordData coordData = ScriptableObject.CreateInstance<CoordData>();

            coordData.coordId = values[coordIdIndex].Trim();
            coordData.x = float.Parse(values[xIndex]);
            coordData.y = float.Parse(values[yIndex]);
            coordData.z = float.Parse(values[zIndex]);

            string safeName = coordData.coordId.Replace(" ", "_").Replace("/", "_");
            string assetPath = $"{assetFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(coordData, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Coord ScriptableObjects created from CSV!");
    }
}
