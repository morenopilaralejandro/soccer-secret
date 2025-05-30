using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class AudioImporter : EditorWindow
{
    private AudioLibrary audioLibrary;

    [MenuItem("Tools/Audio/Import Audio")]
    public static void ShowWindow()
    {
        GetWindow<AudioImporter>("Import Audio");
    }

    void OnGUI()
    {
        GUILayout.Label("Populate Audio Library Sound Entries", EditorStyles.boldLabel);

        audioLibrary = EditorGUILayout.ObjectField("AudioLibrary", audioLibrary, typeof(AudioLibrary), false) as AudioLibrary;

        if (audioLibrary == null)
        {
            EditorGUILayout.HelpBox("Assign your AudioLibrary asset.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Import BGM and SFX"))
        {
            PopulateSoundEntries();
        }
    }

    void PopulateSoundEntries()
    {
        var bgms = GetAudioClipsAtPath("Assets/Audio/Bgm")
            .Select(c => new SoundEntry { soundName = c.name, clip = c })
            .ToList();
        var sfxs = GetAudioClipsAtPath("Assets/Audio/Sfx")
            .Select(c => new SoundEntry { soundName = c.name, clip = c })
            .ToList();

        audioLibrary.bgmClips = bgms;
        audioLibrary.sfxClips = sfxs;

        EditorUtility.SetDirty(audioLibrary);
        Debug.Log($"Imported: {bgms.Count} BGM / {sfxs.Count} SFX");
    }

    List<AudioClip> GetAudioClipsAtPath(string path)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
        return guids.Select(guid => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(clip => clip != null)
            .ToList();
    }
}
