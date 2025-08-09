using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BulkCrestImporter : EditorWindow
{
    private CrestLibrary crestLibrary;
    private string baseFolder = "Assets/Sprites/Crest";

    [MenuItem("Tools/Import Sprites/Bulk Import Crests")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BulkCrestImporter));
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Import Crests", EditorStyles.boldLabel);

        crestLibrary = (CrestLibrary)EditorGUILayout.ObjectField("Crest Library", crestLibrary, typeof(CrestLibrary), false);
        baseFolder = EditorGUILayout.TextField("Base Folder", baseFolder);

        if (GUILayout.Button("Import All Crests"))
        {
            if (crestLibrary == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a CrestLibrary asset.", "OK");
                return;
            }
            ImportCrests();
            EditorUtility.SetDirty(crestLibrary);
            AssetDatabase.SaveAssets();
        }
    }

    private void ImportCrests()
    {
        var newCrests = new List<CrestEntry>();

        // Get all PNG files in the specified baseFolder (no subdirectories)
        string[] files = Directory.GetFiles(baseFolder, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            string filename = Path.GetFileNameWithoutExtension(file); // teamId

            // Prevent duplicates
            if (crestLibrary.crests.Exists(w => w.teamId == filename))
            {
                Debug.Log($"Duplicate skipped: {filename}");
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file.Replace(Application.dataPath, "Assets"));
            if (sprite == null)
            {
                Debug.LogWarning($"Couldn't load sprite at {file}");
                continue;
            }

            CrestEntry entry = new CrestEntry
            {
                teamId = filename,
                sprite = sprite
            };
            newCrests.Add(entry);
            Debug.Log($"Imported: {filename}");
        }

        crestLibrary.crests.AddRange(newCrests);
        Debug.Log($"Bulk import complete! Imported {newCrests.Count} crests.");
    }

}
