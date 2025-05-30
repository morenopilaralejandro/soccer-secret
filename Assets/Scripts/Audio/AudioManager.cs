using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Instance { get; private set; }

    [Header("References")]
    public AudioLibrary audioLibrary;
    public AudioSource sourceBgm;
    public AudioSource sourceSfx;

    void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    // -- BGM --
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

    // -- SFX --
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
