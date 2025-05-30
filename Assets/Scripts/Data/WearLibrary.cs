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
            w => (w.teamId, w.role, w.variant),
            w => w.sprite);
    }

    public Sprite GetWearSprite(string teamId, WearRole role, WearVariant variant)
    {
        WearEntry entry = wears.FirstOrDefault(w =>
            w.teamId == teamId &&
            w.role == role &&
            w.variant == variant
        );
        return entry != null ? entry.sprite : null;
    }
}
