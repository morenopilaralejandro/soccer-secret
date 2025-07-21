using UnityEngine;

public enum PortraitSize { Xs, S, Ms, M, Ml, L, Xl }
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

    // Update these methods to use wearId and PortraitSize
    public Sprite GetWearPortraitSprite(
        string wearId,
        PortraitSize portraitSize,
        WearRole wearRole,
        WearVariant wearVariant)
    {
        return wearPortraitLibrary.GetWearPortraitSprite(wearId, portraitSize, wearRole, wearVariant);
    }

    public Sprite GetWearSprite(
        string wearId,
        WearRole wearRole,
        WearVariant wearVariant)
    {
        return wearLibrary.GetWearSprite(wearId, wearRole, wearVariant);
    }
}
