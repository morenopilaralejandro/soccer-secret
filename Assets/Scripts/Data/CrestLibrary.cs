using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CrestLibrary", menuName = "ScriptableObject/CrestLibrary")]
public class CrestLibrary : ScriptableObject
{
    public List<CrestEntry> crests = new List<CrestEntry>();
    private Dictionary<string, Sprite> _crestsDict;

    private void OnEnable()
    {
        // Build lookup dictionary when asset loads
        _crestsDict = crests.ToDictionary(
            w => w.teamId,
            w => w.sprite
        );
    }

    public Sprite GetCrestSprite(string teamId)
    {
        if (_crestsDict != null && _crestsDict.TryGetValue(teamId, out Sprite sprite))
        {
            return sprite;
        }
        //change this for default crest
        return null;
    }
}
