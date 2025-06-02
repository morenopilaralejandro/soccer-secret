#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Generates a text file that contains every unique character used in all the
/// column of all String Tables in the project.
/// 
/// Menu:  Localization ▸ AtlasGenerator
/// Output: Assets/Localization/Aatlas/atlas-characters.txt
/// </summary>
public static class AtlasGenerator
{
    private const string OutputPath = "Assets/Localization/Atlas/atlas-characters.txt";

    /// <summary>
    /// Collects all strings from every <see cref="StringTableCollection"/> and writes the
    /// unique set of characters to <see cref="OutputPath"/>. If the target directory does not exist
    /// it will be created automatically.
    /// </summary>
    [MenuItem("Tools/Localization/Generate Atlas")]
    public static void GenerateAtlas()
    {
        // Ensure destination directory exists ----------------------------------------------
        var directory = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        // Gather characters ----------------------------------------------------------------
        var characters = new HashSet<char>();

        // Find every StringTableCollection asset
        foreach (var guid in AssetDatabase.FindAssets("t:StringTableCollection"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
            if (collection == null) continue;

            // Go through all StringTables (i.e., all locales in this collection)
            foreach (var table in collection.StringTables)
            {
                if (table == null) continue;

                foreach (var entry in table.Values)
                {
                    if (string.IsNullOrEmpty(entry?.LocalizedValue)) continue;

                    foreach (var c in entry.LocalizedValue!)
                    {
                        characters.Add(c);
                    }
                }
            }
        }


        // Add characters
        //characters.Add(' ');   // space
        //characters.Add('\n');
        characters.Add('_');
        characters.Add('/');
        characters.Add('0');
        characters.Add('1');
        characters.Add('2');
        characters.Add('3');
        characters.Add('4');
        characters.Add('5');
        characters.Add('6');
        characters.Add('7');
        characters.Add('8');
        characters.Add('9');

        // Sort for consistency --------------------------------------------------------------
        var sortedCharacters = characters.ToList();
        sortedCharacters.Sort();

        // Write the atlas file --------------------------------------------------------------
        File.WriteAllText(OutputPath, new string(sortedCharacters.ToArray()).Normalize(NormalizationForm.FormC));
        Debug.Log($"[AtlasGenerator] Wrote {sortedCharacters.Count} unique characters to {OutputPath}");

        // Refresh the AssetDatabase so the newly‑created/updated file appears in the Project
        AssetDatabase.Refresh();
    }
}
#endif

