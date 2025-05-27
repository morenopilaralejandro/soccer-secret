#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Generates a text file that contains every unique character used in the Japanese ("ja")
/// column of all String Tables in the project.
/// 
/// Menu:  Localization ▸ AtlasGenerator
/// Output: Assets/Localization/Aatlas/atlas-ja-characters.txt
/// </summary>
public static class AtlasGenerator
{
    private const string OutputPath = "Assets/Localization/Atlas/atlas-ja-characters.txt";
    private const string LocaleCode = "ja";

    /// <summary>
    /// Collects all Japanese strings from every <see cref="StringTableCollection"/> and writes the
    /// unique set of characters to <see cref="OutputPath"/>. If the target directory does not exist
    /// it will be created automatically.
    /// </summary>
    [MenuItem("Tools/Localization/AtlasGenerator")]
    public static void GenerateAtlasJa()
    {
        // Ensure destination directory exists ----------------------------------------------
        var directory = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        // Gather characters ----------------------------------------------------------------
        var characters = new HashSet<char>();

        // Locate every StringTableCollection asset in the project
        foreach (var guid in AssetDatabase.FindAssets("t:StringTableCollection"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
            if (collection == null) continue;

            // Try to get the Japanese table in this collection
            if (collection.GetTable(LocaleCode) is StringTable jaTable)
            {
                foreach (var entry in jaTable.Values)
                {
                    if (string.IsNullOrEmpty(entry?.LocalizedValue)) continue;

                    foreach (var c in entry.LocalizedValue!)
                    {
                        characters.Add(c);
                    }
                }
            }
        }

        // Add a few always‑useful characters
        //characters.Add(' ');   // space
        //characters.Add('\n'); // newline

        // Sort for consistency --------------------------------------------------------------
        var sortedCharacters = characters.ToList();
        sortedCharacters.Sort();

        // Write the atlas file --------------------------------------------------------------
        File.WriteAllText(OutputPath, new string(sortedCharacters.ToArray()));
        Debug.Log($"[AtlasGenerator] Wrote {sortedCharacters.Count} unique characters to {OutputPath}");

        // Refresh the AssetDatabase so the newly‑created/updated file appears in the Project
        AssetDatabase.Refresh();
    }
}
#endif

