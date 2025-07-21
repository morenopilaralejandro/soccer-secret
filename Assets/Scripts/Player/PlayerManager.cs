using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public PlayerPortraitLibrary playerPortraitLibrary; // Drag your asset here in inspector

    private Dictionary<string, PlayerData> playerDataDict = new Dictionary<string, PlayerData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PlayerData[] allPlayers = Resources.LoadAll<PlayerData>("ScriptableObjects/Player");
        foreach (PlayerData playerData in allPlayers)
        {
            AddPlayerDataToDict(playerData);
        }
    }

    void Start()
    {

    }

    public void AddPlayerDataToDict(PlayerData playerData)
    {
        if (!playerDataDict.ContainsKey(playerData.playerId))
            playerDataDict.Add(playerData.playerId, playerData);
        else
            Debug.LogWarning("Duplicate playerId: " + playerData.playerId);
    }

    public PlayerData GetPlayerDataById(string playerId)
    {
        if (playerDataDict.TryGetValue(playerId, out var playerData))
            return playerData;

        Debug.LogWarning("Player not found: " + playerId);
        return null;
    }

    public Sprite GetPlayerPortraitSpriteById(
        string playerId)
    {
        return playerPortraitLibrary.GetPlayerPortraitSpriteById(playerId);
    }

}
