using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BulkWearPortraitImporter : EditorWindow
{
    private WearPortraitLibrary wearPortraitLibrary;
    private string baseFolder = "Assets/Sprites/WearPortrait";

    [MenuItem("Tools/Import Wear Sprites/Bulk Import WearPortraits")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BulkWearPortraitImporter));
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Import WearPortraits", EditorStyles.boldLabel);

        wearPortraitLibrary = (WearPortraitLibrary)EditorGUILayout.ObjectField("WearPortrait Library", wearPortraitLibrary, typeof(WearPortraitLibrary), false);
        baseFolder = EditorGUILayout.TextField("Base Folder", baseFolder);

        if (GUILayout.Button("Import All WearPortraits"))
        {
            if (wearPortraitLibrary == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a WearPortraitLibrary asset.", "OK");
                return;
            }
            ImportWearPortraits();
            EditorUtility.SetDirty(wearPortraitLibrary);
            AssetDatabase.SaveAssets();
        }
    }

    private void ImportWearPortraits()
    {
        var newWearPortraits = new List<WearPortraitEntry>();

        // Get all PNG files in all subdirectories
        string[] files = Directory.GetFiles(baseFolder, "*.png", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string filename = Path.GetFileNameWithoutExtension(file); // Example: wearId-wearVariant-wearRole-portraitSize
            string[] parts = filename.Split('-');
            if (parts.Length != 4)
            {
                Debug.LogWarning($"Filename format incorrect: {filename}");
                continue;
            }

            string wearIdString = parts[0];
            string wearVariantString = parts[1];
            string wearRoleString = parts[2];
            string portraitSizeString = parts[3];

            // Parse enums, case-insensitive
            if (!System.Enum.TryParse(typeof(WearVariant), wearVariantString, true, out object wearVariantObj)
                || !System.Enum.TryParse(typeof(WearRole), wearRoleString, true, out object wearRoleObj)
                || !System.Enum.TryParse(typeof(PortraitSize), portraitSizeString, true, out object portraitSizeObj))
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
            if (wearPortraitLibrary.wearPortraits.Exists(w =>
                w.wearId == wearIdString
                && w.wearVariant == (WearVariant)wearVariantObj
                && w.wearRole == (WearRole)wearRoleObj
                && w.portraitSize == (PortraitSize)portraitSizeObj))
            {
                Debug.Log($"Duplicate skipped: {filename}");
                continue;
            }

            WearPortraitEntry entry = new WearPortraitEntry
            {
                wearId = wearIdString,
                wearVariant = (WearVariant)wearVariantObj,
                wearRole = (WearRole)wearRoleObj,
                portraitSize = (PortraitSize)portraitSizeObj,
                sprite = sprite
            };
            newWearPortraits.Add(entry);
            Debug.Log($"Imported: {filename}");
        }

        wearPortraitLibrary.wearPortraits.AddRange(newWearPortraits);
        Debug.Log($"Bulk import complete! Imported {newWearPortraits.Count} wearPortraits.");
    }
}
