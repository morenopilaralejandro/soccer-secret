using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject allyPrefab; 
    [SerializeField] private GameObject oppPrefab;
    [SerializeField] private GameObject[] playerAllyObjects;
    [SerializeField] private GameObject[] playerOppObjects;

    private List<PlayerData> players;

    void Start()
    {
        string path = "Csv/"; // Path to your CSV file
        string fileName = "player"; // Path to your CSV file

        TextAsset csv = Resources.Load<TextAsset>(path + fileName);
        string csvText = csv.text;
        players = LoadPlayerDataFromCSV(csvText);

        InitializeAlly();
        InitializeOpp();
    }

    List<PlayerData> LoadPlayerDataFromCSV(string csvText)
    {
        List<PlayerData> playerList = new List<PlayerData>();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++) // Start from 1 to skip header
        {
            string[] values = lines[i].Split(',');
            PlayerData player = new PlayerData
            {
                playerId = values[0],
                playerName = values[1],
                gender = values[2],
                element = values[3],
                position = values[4],
                hp = int.Parse(values[5]),
                sp = int.Parse(values[6]),
                kick = int.Parse(values[7]),
                body = int.Parse(values[8]),
                control = int.Parse(values[9]),
                guard = int.Parse(values[10]),
                speed = int.Parse(values[11]),
                stamina = int.Parse(values[12]),
                courage = int.Parse(values[13]),	
                freedom = int.Parse(values[14])
            };
            playerList.Add(player);
        }
        return playerList;
    }

    void InstantiatePlayers(List<PlayerData> players)
    {
        foreach (var playerData in players)
        {
            GameObject player = Instantiate(allyPrefab);
            // Set player stats here, e.g., player.GetComponent<Player>().Initialize(playerData);
        }
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

    public PlayerData GetPlayerDataByName(string playerName)
    {
        foreach (var player in players)
        {
            if (player.playerName == playerName)
            {
                return player;
            }
        }
        Debug.LogWarning("Player not found: " + playerName);
        return null;
    }

    public PlayerData GetPlayerDataById(string playerId)
    {
        foreach (var player in players)
        {
            if (player.playerId == playerId)
            {
                return player;
            }
        }
        Debug.LogWarning("Player not found: " + playerId);
        return null;
    }
}
