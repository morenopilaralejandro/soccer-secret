using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "ScriptableObject/AudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    public List<SoundEntry> bgmClips;
    public List<SoundEntry> sfxClips;

    private Dictionary<string, AudioClip> _bgmDict;
    private Dictionary<string, AudioClip> _sfxDict;

    private void OnEnable()
    {
        _bgmDict = bgmClips.ToDictionary(e => e.soundName, e => e.clip);
        _sfxDict = sfxClips.ToDictionary(e => e.soundName, e => e.clip);
    }

    public AudioClip GetBgm(string name)
    {
        _bgmDict.TryGetValue(name, out AudioClip clip);
        return clip;
    }

    public AudioClip GetSfx(string name)
    {
        _sfxDict.TryGetValue(name, out AudioClip clip);
        return clip;
    }
}
