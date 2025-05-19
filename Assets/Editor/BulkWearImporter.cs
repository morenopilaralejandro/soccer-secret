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

            foreach (var role in System.Enum.GetNames(typeof(WearRole)))
            {
                foreach (var variant in System.Enum.GetNames(typeof(WearVariant)))
                {
                    string path = Path.Combine(baseFolder, role, variant);
                    
                    if (!Directory.Exists(path))
                        continue;

                    Debug.Log($"Found directory: {path}");

                    string[] files = Directory.GetFiles(path, "*.png");
                    foreach (string file in files)
                    {
                        Debug.Log($"Trying: {role}/{variant}");
                        string teamId = Path.GetFileNameWithoutExtension(file);
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                        if (sprite == null)
                        {
                            Debug.LogWarning($"Couldn't load sprite at {file}");
                            continue;
                        }

                        // Prevent duplicates
                        if (wearLibrary.wears.Exists(w =>
                            w.teamId == teamId &&
                            w.role.ToString() == role &&
                            w.variant.ToString() == variant
                        ))
                        {
                            Debug.Log($"Duplicate skipped: {role}/{variant}/{teamId}");
                            continue;
                        }

                        WearEntry entry = new WearEntry
                        {
                            teamId = teamId,
                            role = (WearRole)System.Enum.Parse(typeof(WearRole), role),
                            variant = (WearVariant)System.Enum.Parse(typeof(WearVariant), variant),
                            sprite = sprite
                        };
                        newWears.Add(entry);
                        Debug.Log($"Imported: {role}/{variant}/{teamId}");
                    }
                }
        }

        wearLibrary.wears.AddRange(newWears);
        Debug.Log($"Bulk import complete! Imported {newWears.Count} wears.");
    }
}
