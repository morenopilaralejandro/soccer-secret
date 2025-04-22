using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject allyPrefab; 
    public GameObject oppPrefab;
    public GameObject[] playerAllyObjects;
    public GameObject[] playerOppObjects;

    private List<PlayerData> players;

    void Start()
    {
        string path = "Assets/Csv/player.csv"; // Path to your CSV file
        players = LoadPlayerDataFromCSV(path);
        InitializeAlly();
        InitializeOpp();
    }

    List<PlayerData> LoadPlayerDataFromCSV(string filePath)
    {
        List<PlayerData> playerList = new List<PlayerData>();
        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++) // Start from 1 to skip header
        {
            string[] values = lines[i].Split(',');
            PlayerData player = new PlayerData
            {
                id = values[0],
                playerName = values[1],
                gender = values[2],
                type = values[3],
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
    /*
    void InitializeExistingPlayers()
    {
        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            string playerName = playerObject.name; // Assuming the GameObject's name is the player's name
            PlayerData playerData = GetPlayerDataByName(playerName);

            if (playerData != null)
            {
                // Initialize the player GameObject with the data
                SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Sprite playerSprite = Resources.Load<Sprite>("Sprites/Player" + "player");
                    if (playerSprite != null)
                    {
                        spriteRenderer.sprite = playerSprite;
                    }
                    else
                    {
                        Debug.LogWarning("Sprite not found for player: " + playerData.playerName);
                    }
                }

                // Set other player stats here, e.g., playerObject.GetComponent<Player>().Initialize(playerData);
            }
        }
    }
    */

    void InitializeAlly()
    {
        string[] ids = {"001", "002", "003", "004"};

        for (int i = 0; i < playerAllyObjects.Length; i++)
        {
            GameObject playerObject = playerAllyObjects[i];      
            PlayerData playerData = GetPlayerDataById(ids[i]);
            InitializePlayer(playerObject, playerData, true);
        }
    }

    void InitializeOpp()
    {
        string[] ids = {"005", "006", "007", "008"};

        for (int i = 0; i < playerAllyObjects.Length; i++)
        {
            GameObject playerObject = playerOppObjects[i];      
            PlayerData playerData = GetPlayerDataById(ids[i]);
            InitializePlayer(playerObject, playerData, false);
        }
    }

    public void InitializePlayer(GameObject playerObject, PlayerData playerData, bool isAlly)
    {
        if (playerData != null)
        {
            SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Sprite playerSprite = Resources.Load<Sprite>("Player/" + "player");
                if (playerSprite != null)
                {
                    spriteRenderer.sprite = playerSprite;
                }
                else
                {
                    Debug.LogWarning("Sprite not found for player: " + playerData.playerName);
                }
            }
            playerObject.GetComponent<Player>().Initialize(playerData);
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

    public PlayerData GetPlayerDataById(string id)
    {
        foreach (var player in players)
        {
            if (player.id == id)
            {
                return player;
            }
        }
        Debug.LogWarning("Player not found: " + id);
        return null;
    }
}
