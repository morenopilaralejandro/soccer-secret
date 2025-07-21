using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BulkWearImporter : EditorWindow
{
    private WearLibrary wearLibrary;
    private string baseFolder = "Assets/Sprites/Wear";

    [MenuItem("Tools/Import Wear Sprites/Bulk Import Wears")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BulkWearImporter));
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Import Wears", EditorStyles.boldLabel);

        wearLibrary = (WearLibrary)EditorGUILayout.ObjectField("Wear Library", wearLibrary, typeof(WearLibrary), false);
        baseFolder = EditorGUILayout.TextField("Base Folder", baseFolder);

        if (GUILayout.Button("Import All Wears"))
        {
            if (wearLibrary == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a WearLibrary asset.", "OK");
                return;
            }
            ImportWears();
            EditorUtility.SetDirty(wearLibrary);
            AssetDatabase.SaveAssets();
        }
    }

    private void ImportWears()
    {
        var newWears = new List<WearEntry>();

        // Get all PNG files in all subdirectories
        string[] files = Directory.GetFiles(baseFolder, "*.png", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string filename = Path.GetFileNameWithoutExtension(file); // Example: wearId-wearVariant-wearRole
            string[] parts = filename.Split('-');
            if (parts.Length != 3)
            {
                Debug.LogWarning($"Filename format incorrect: {filename}");
                continue;
            }

            string wearIdString = parts[0];
            string wearVariantString = parts[1];
            string wearRoleString = parts[2];

            // Parse enums, case-insensitive
            if (!System.Enum.TryParse(typeof(WearVariant), wearVariantString, true, out object wearVariantObj)
                || !System.Enum.TryParse(typeof(WearRole), wearRoleString, true, out object wearRoleObj))
            {
                Debug.LogWarning($"Enum parse failed for file: {filename}");
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
            if (sprite == null)
            {
                Debug.LogWarning($"Couldn't load sprite at {file}");
                continue;
            }

            // Prevent duplicates
            if (wearLibrary.wears.Exists(w =>
                w.wearId == wearIdString
                && w.wearVariant == (WearVariant)wearVariantObj
                && w.wearRole == (WearRole)wearRoleObj))
            {
                Debug.Log($"Duplicate skipped: {filename}");
                continue;
            }

            WearEntry entry = new WearEntry
            {
                wearId = wearIdString,
                wearVariant = (WearVariant)wearVariantObj,
                wearRole = (WearRole)wearRoleObj,
                sprite = sprite
            };
            newWears.Add(entry);
            Debug.Log($"Imported: {filename}");
        }

        wearLibrary.wears.AddRange(newWears);
        Debug.Log($"Bulk import complete! Imported {newWears.Count} wears.");
    }
}
