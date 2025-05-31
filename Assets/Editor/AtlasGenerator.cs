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
    [MenuItem("Tools/Localization/Generate Atlas")]
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

        // Add characters
        //characters.Add(' ');   // space
        //characters.Add('\n');
        characters.Add('_');
        characters.Add('A');
        characters.Add('B');
        characters.Add('C');
        characters.Add('D');
        characters.Add('E');
        characters.Add('F');
        characters.Add('G');
        characters.Add('H');
        characters.Add('I');
        characters.Add('J');
        characters.Add('K');
        characters.Add('L');
        characters.Add('M');
        characters.Add('N');
        characters.Add('O');
        characters.Add('P');
        characters.Add('Q');
        characters.Add('R');
        characters.Add('S');
        characters.Add('T');
        characters.Add('U');
        characters.Add('V');
        characters.Add('W');
        characters.Add('X');
        characters.Add('Y');
        characters.Add('Z');
        characters.Add('a');
        characters.Add('b');
        characters.Add('c');
        characters.Add('d');
        characters.Add('e');
        characters.Add('f');
        characters.Add('g');
        characters.Add('h');
        characters.Add('i');
        characters.Add('j');
        characters.Add('k');
        characters.Add('l');
        characters.Add('m');
        characters.Add('n');
        characters.Add('o');
        characters.Add('p');
        characters.Add('q');
        characters.Add('r');
        characters.Add('s');
        characters.Add('t');
        characters.Add('u');
        characters.Add('v');
        characters.Add('w');
        characters.Add('x');
        characters.Add('y');
        characters.Add('z');
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

