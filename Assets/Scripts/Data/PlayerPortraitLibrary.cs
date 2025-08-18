using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPortraitLibrary", menuName = "ScriptableObject/PlayerPortraitLibrary")]
public class PlayerPortraitLibrary : ScriptableObject
{
    public List<PlayerPortraitEntry> playerPortraits;

    // For efficient lookup:
    private Dictionary<string, Sprite> _playerPortraitsDict;

    private void OnEnable()
    {
        _playerPortraitsDict = playerPortraits.ToDictionary(w => w.playerId, w => w.sprite);
    }

    public Sprite GetPlayerPortraitSpriteById(string playerId)
    {
        if (_playerPortraitsDict == null) OnEnable(); // Ensure dictionary is built

        _playerPortraitsDict.TryGetValue(playerId, out var sprite);
        return sprite;
    }
}
