using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WearPortraitLibrary", menuName = "ScriptableObject/WearPortraitLibrary")]
public class WearPortraitLibrary : ScriptableObject
{
    public List<WearPortraitEntry> wearPortraits;
    private Dictionary<(string, PortraitSize, WearRole, WearVariant), Sprite> _wearPortraitsDict;

    private void OnEnable()
    {
        // Build lookup dictionary when asset loads
        _wearPortraitsDict = wearPortraits.ToDictionary(
            w => (w.wearId, w.portraitSize, w.wearRole, w.wearVariant),
            w => w.sprite
        );
    }

    public Sprite GetWearPortraitSprite(string wearId, PortraitSize portraitSize, WearRole wearRole, WearVariant wearVariant)
    {
        if (_wearPortraitsDict == null || _wearPortraitsDict.Count != wearPortraits.Count)
            OnEnable(); // Rebuild if needed (e.g., on domain reload)

        return _wearPortraitsDict.TryGetValue((wearId, portraitSize, wearRole, wearVariant), out var sprite)
            ? sprite
            : null;
    }
}
