using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PlayerSaveSystem
{
    public static void SavePlayers(List<Player> playerComponents)
    {
        var saveList = new PlayerSaveList();
        foreach (var player in playerComponents)
            saveList.players.Add(player.ToSaveData());

        string json = JsonUtility.ToJson(saveList, true);
        File.WriteAllText(Application.persistentDataPath + "/playersave.json", json);
        Debug.Log("Saved Player Data!");
    }

    public static List<PlayerSaveData> LoadPlayers()
    {
        string path = Application.persistentDataPath + "/playersave.json";
        if (!File.Exists(path)) return new List<PlayerSaveData>();
        string json = File.ReadAllText(path);

        var saveList = JsonUtility.FromJson<PlayerSaveList>(json);
        return saveList.players ?? new List<PlayerSaveData>();
    }

    /*
    List<PlayerSaveData> saveDatas = PlayerSaveSystem.LoadPlayers();
    foreach (var saveData in saveDatas)
    {
        // 1. Get the PlayerData template
        PlayerData template = PlayerManager.Instance.GetPlayerDataById(saveData.playerId);
        // 2. Instantiate a new Player object or reuse an existing one...
        Player player = new Player();       
        //Player player = Instantiate(playerPrefab); // or however you want to do it
        // 3. Restore state
        player.FromSaveData(saveData, template);
    }
    */
}
