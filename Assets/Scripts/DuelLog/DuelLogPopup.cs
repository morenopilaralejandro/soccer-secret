using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class DuelLogPopup : MonoBehaviour
{
    public TextMeshProUGUI text;
    private Action<DuelLogPopup> onHideCallback;

    public float spawnTime { get; private set; }

    public void Show(DuelLogEntry entry)
    {
        text.text = entry.LocalizedString.GetLocalizedString();
        spawnTime = Time.unscaledTime;
        gameObject.SetActive(true);
    }

    // Used by the menu (no timer)
    public void ShowStatic(DuelLogEntry entry)
    {
        text.text = entry.LocalizedString.GetLocalizedString();
        gameObject.SetActive(true);
        /*
        CancelInvoke();            // Just in case
        onHideCallback = null;
        */
    }

    /*
    public void Hide()
    {
        onHideCallback?.Invoke(this);
    }

    public void HideImmediate()
    {
        CancelInvoke();
        onHideCallback = null;
    }
    */
}
