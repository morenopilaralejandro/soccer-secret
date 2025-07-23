using UnityEngine;
using System.Collections.Generic;

public enum PlayerSize { S, M, L, Xl }
public enum PortraitSize { Xs, S, Sm, M, Ml, L, Xl }
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

    public PortraitSize GetPortraitSizeByString(string raw)
    {
        switch (raw)
        {
            case "portrait-xs": return PortraitSize.Xs;
            case "portrait-s":  return PortraitSize.S;
            case "portrait-sm": return PortraitSize.Sm;
            case "portrait-m":  return PortraitSize.M;
            case "portrait-ml": return PortraitSize.Ml;
            case "portrait-l":  return PortraitSize.L;
            case "portrait-xl": return PortraitSize.Xl;
            default:            return PortraitSize.S; // Default fallback
        }
    }

    public PlayerSize GetPlayerSizeByString(string raw)
    {
        switch (raw)
        {
            case "size-s":  return PlayerSize.S;
            case "size-m":  return PlayerSize.M;
            case "size-l":  return PlayerSize.L;
            case "size-xl": return PlayerSize.Xl;
            default:        return PlayerSize.S; // Default fallback
        }
    }

    public bool IsHome(List<Team> teams, int teamIndex) 
    {
        if (teamIndex == 0)
        {
            return true;
        } else {
            if (teams[0].WearId != teams[1].WearId)
            {
                return true;
            } else {
                return false;
            }
        }
    }
}
