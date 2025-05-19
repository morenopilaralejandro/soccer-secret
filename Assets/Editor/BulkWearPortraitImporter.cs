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

        foreach (var size in System.Enum.GetNames(typeof(Size)))
        {
            foreach (var role in System.Enum.GetNames(typeof(WearRole)))
            {
                foreach (var variant in System.Enum.GetNames(typeof(WearVariant)))
                {
                    string path = Path.Combine(baseFolder, size, role, variant);
                    
                    if (!Directory.Exists(path))
                        continue;

                    Debug.Log($"Found directory: {path}");

                    string[] files = Directory.GetFiles(path, "*.png");
                    foreach (string file in files)
                    {
                        Debug.Log($"Trying: {size}/{role}/{variant}");
                        string teamId = Path.GetFileNameWithoutExtension(file);
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                        if (sprite == null)
                        {
                            Debug.LogWarning($"Couldn't load sprite at {file}");
                            continue;
                        }

                        // Prevent duplicates
                        if (wearPortraitLibrary.wearPortraits.Exists(w =>
                            w.teamId == teamId &&
                            w.size.ToString() == size &&
                            w.role.ToString() == role &&
                            w.variant.ToString() == variant
                        ))
                        {
                            Debug.Log($"Duplicate skipped: {size}/{role}/{variant}/{teamId}");
                            continue;
                        }

                        WearPortraitEntry entry = new WearPortraitEntry
                        {
                            teamId = teamId,
                            size = (Size)System.Enum.Parse(typeof(Size), size),
                            role = (WearRole)System.Enum.Parse(typeof(WearRole), role),
                            variant = (WearVariant)System.Enum.Parse(typeof(WearVariant), variant),
                            sprite = sprite
                        };
                        newWearPortraits.Add(entry);
                        Debug.Log($"Imported: {size}/{role}/{variant}/{teamId}");
                    }
                }
            }
        }

        wearPortraitLibrary.wearPortraits.AddRange(newWearPortraits);
        Debug.Log($"Bulk import complete! Imported {newWearPortraits.Count} wearPortraits.");
    }
}
