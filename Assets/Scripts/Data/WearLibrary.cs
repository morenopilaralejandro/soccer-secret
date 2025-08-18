using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WearLibrary", menuName = "ScriptableObject/WearLibrary")]
public class WearLibrary : ScriptableObject
{
    public List<WearEntry> wears;
    private Dictionary<(string, WearRole, WearVariant), Sprite> _wearsDict;

    private void OnEnable()
    {
        // Build lookup dictionary when asset loads
        _wearsDict = wears.ToDictionary(
            w => (w.wearId, w.wearRole, w.wearVariant),
            w => w.sprite);
    }

    public Sprite GetWearSprite(string wearId, WearRole wearRole, WearVariant wearVariant)
    {
        WearEntry entry = wears.FirstOrDefault(w =>
            w.wearId == wearId &&
            w.wearRole == wearRole &&
            w.wearVariant == wearVariant
        );
        return entry != null ? entry.sprite : null;
    }
}
