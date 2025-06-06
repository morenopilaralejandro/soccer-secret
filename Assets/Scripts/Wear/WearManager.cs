using UnityEngine;

public enum WearRole { Field, Keeper }
public enum WearVariant { Home, Away }

public class WearManager : MonoBehaviour
{

    public static WearManager Instance { get; private set; }
    public WearPortraitLibrary wearPortraitLibrary; // Drag your asset here in inspector
    public WearLibrary wearLibrary; // Drag your asset here in inspector

    private void Awake()
    {
        // Standard Unity Singleton pattern:
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Sprite GetWearPortraitSprite(
        string teamId,
        Size size,
        WearRole role,
        WearVariant variant)
    {
        return wearPortraitLibrary.GetWearPortraitSprite(teamId, size, role, variant);
    }

    public Sprite GetWearSprite(
        string teamId,
        WearRole role,
        WearVariant variant)
    {
        return wearLibrary.GetWearSprite(teamId, role, variant);
    }
}
