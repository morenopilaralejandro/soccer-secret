using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WearPortraitLibrary", menuName = "ScriptableObject/Wear Portrait Library")]
public class WearPortraitLibrary : ScriptableObject
{
    public List<WearPortraitEntry> wearPortraits;
    private Dictionary<(string, Size, WearRole, WearVariant), Sprite> _wearPortraitsDict;

    private void OnEnable()
    {
        // Build lookup dictionary when asset loads
        _wearPortraitsDict = wearPortraits.ToDictionary(
            w => (w.teamId, w.size, w.role, w.variant),
            w => w.sprite);
    }

    public Sprite GetWearSprite(string teamId, Size size, WearRole role, WearVariant variant)
    {
        WearPortraitEntry entry = wearPortraits.FirstOrDefault(w =>
            w.teamId == teamId &&
            w.size == size &&
            w.role == role &&
            w.variant == variant
        );
        return entry != null ? entry.sprite : null;
    }
}
