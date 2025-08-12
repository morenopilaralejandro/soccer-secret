using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("References")]
    public AudioLibrary audioLibrary;
    public AudioSource sourceBgm;
    public AudioSource sourceSfx;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyVolumesFromSettings();
    }

    public void ApplyVolumesFromSettings()
    {
        sourceBgm.volume = SettingsManager.GetBgmVolume();
        sourceSfx.volume = SettingsManager.GetSfxVolume();
    }

    public void SetBgmVolume(float volume)
    {
        sourceBgm.volume = volume;
    }

    public void SetSfxVolume(float volume)
    {
        sourceSfx.volume = volume;
    }

    public void PlayBgm(string name)
    {
        AudioClip clip = audioLibrary.GetBgm(name);
        if (clip == null)
        {
            Debug.LogWarning($"BGM '{name}' not found in AudioLibrary.");
            return;
        }
        sourceBgm.clip = clip;
        sourceBgm.Play();
    }

    public void PlaySfx(string name)
    {
        AudioClip clip = audioLibrary.GetSfx(name);
        if (clip == null)
        {
            Debug.LogWarning($"SFX '{name}' not found in AudioLibrary.");
            return;
        }
        sourceSfx.PlayOneShot(clip);
    }
}
