using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject allyPrefab; 
    [SerializeField] private GameObject oppPrefab;
    [SerializeField] private GameObject[] playerAllyObjects;
    [SerializeField] private GameObject[] playerOppObjects;

    private Dictionary<string, PlayerData> playerDict = new Dictionary<string, PlayerData>();

    void Awake()
    {
        PlayerData[] allPlayers = Resources.LoadAll<PlayerData>("ScriptableObjects/Player");
        foreach (PlayerData playerData in allPlayers)
        {
            AddPlayerToDict(playerData);
        }
    }

    void Start()
    {
        InitializeAlly();
        InitializeOpp();
    }

    public void AddPlayerToDict(PlayerData playerData)
    {
        if (!playerDict.ContainsKey(playerData.playerId))
            playerDict.Add(playerData.playerId, playerData);
        else
            Debug.LogWarning("Duplicate playerId: " + playerData.playerId);
    }

    void InitializeAlly()
    {
        string[] ids = {"001", "002", "003", "004"};
        string wearId = "w001";
        for (int i = 0; i < playerAllyObjects.Length; i++)
        {
            GameObject playerObject = playerAllyObjects[i];      
            PlayerData playerData = GetPlayerDataById(ids[i]);
            InitializePlayer(playerObject, playerData, wearId);
        }
    }

    void InitializeOpp()
    {
        string[] ids = {"005", "006", "007", "008"};
        string wearId = "w002";
        for (int i = 0; i < playerAllyObjects.Length; i++)
        {
            GameObject playerObject = playerOppObjects[i];      
            PlayerData playerData = GetPlayerDataById(ids[i]);
            InitializePlayer(playerObject, playerData, wearId);
            playerObject.GetComponent<Player>().IsAlly = false;
            playerObject.GetComponent<Player>().IsAi = true;
        }
    }

    public void InitializePlayer(GameObject playerObject, PlayerData playerData, string wearId)
    {
        if (playerData != null)
        {
            playerObject.GetComponent<Player>().Initialize(playerData);

            SpriteRenderer elementIconSpriteRenderer = playerObject.transform.Find("ElementIcon").GetComponent<SpriteRenderer>(); 
            if (elementIconSpriteRenderer != null)
            {
                elementIconSpriteRenderer.sprite = ElementManager.Instance.GetElementIcon(playerObject.GetComponent<Player>().Element);
            }

            SpriteRenderer playerSpriteRenderer = playerObject.GetComponent<SpriteRenderer>();
            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.sprite = playerObject.GetComponent<Player>().SpritePlayer;
            }

            Transform wearTransform = playerObject.transform.GetChild(0);
            SpriteRenderer wearSpriteRenderer = wearTransform.GetComponent<SpriteRenderer>();
            if (wearSpriteRenderer != null)
            {
                Sprite wearSprite = Resources.Load<Sprite>("Wear/" + wearId);
                if (wearSprite != null)
                {
                    wearSpriteRenderer.sprite = wearSprite;
                }
                else
                {
                    Debug.LogWarning("Sprite not found for wear: " + wearId);
                }
            }

        }
    }

    public PlayerData GetPlayerDataById(string playerId)
    {
        if (playerDict.TryGetValue(playerId, out var playerData))
            return playerData;

        Debug.LogWarning("Player not found: " + playerId);
        return null;
    }
}
