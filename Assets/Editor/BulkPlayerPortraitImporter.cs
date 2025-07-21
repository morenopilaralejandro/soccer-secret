using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BulkPlayerPortraitImporter : EditorWindow
{
    private PlayerPortraitLibrary playerPortraitLibrary;
    private string baseFolder = "Assets/Sprites/PlayerPortrait";

    [MenuItem("Tools/Import Player Sprites/Bulk Import PlayerPortraits")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BulkPlayerPortraitImporter));
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Import PlayerPortraits", EditorStyles.boldLabel);

        playerPortraitLibrary = (PlayerPortraitLibrary)EditorGUILayout.ObjectField("PlayerPortrait Library", playerPortraitLibrary, typeof(PlayerPortraitLibrary), false);
        baseFolder = EditorGUILayout.TextField("Base Folder", baseFolder);

        if (GUILayout.Button("Import All PlayerPortraits"))
        {
            if (playerPortraitLibrary == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a PlayerPortraitLibrary asset.", "OK");
                return;
            }
            ImportPlayerPortraits();
            EditorUtility.SetDirty(playerPortraitLibrary);
            AssetDatabase.SaveAssets();
        }
    }

    private void ImportPlayerPortraits()
    {
        var newPlayerPortraits = new List<PlayerPortraitEntry>();

        // Get all PNG files in the folder
        string[] files = Directory.GetFiles(baseFolder, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            string playerId = Path.GetFileNameWithoutExtension(file);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
            if (sprite == null)
            {
                Debug.LogWarning($"Couldn't load sprite at: {file}");
                continue;
            }

            // Prevent duplicates
            if (playerPortraitLibrary.playerPortraits.Exists(w => w.playerId == playerId))
            {
                Debug.Log($"Duplicate skipped: {playerId}");
                continue;
            }

            PlayerPortraitEntry entry = new PlayerPortraitEntry
            {
                playerId = playerId,
                sprite = sprite
            };
            newPlayerPortraits.Add(entry);
            Debug.Log($"Imported: {playerId}");
        }

        playerPortraitLibrary.playerPortraits.AddRange(newPlayerPortraits);
        Debug.Log($"Bulk import complete! Imported {newPlayerPortraits.Count} playerPortraits.");
    }
}
