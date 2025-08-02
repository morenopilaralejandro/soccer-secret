using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class DuelLogPopup : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float displayTime = 1f;
    private Action<DuelLogPopup> onHideCallback;

    // Used by the popup manager (with timer)
    public void Show(DuelLogEntry entry, Action<DuelLogPopup> onHide)
    {
        text.text = entry.LocalizedString.GetLocalizedString();
        onHideCallback = onHide;
        gameObject.SetActive(true);
        Invoke(nameof(Hide), displayTime);
    }

    // Used by the menu (no timer)
    public void ShowStatic(DuelLogEntry entry)
    {
        text.text = entry.LocalizedString.GetLocalizedString();
        gameObject.SetActive(true);
        CancelInvoke();            // Just in case
        onHideCallback = null;
    }

    public void Hide()
    {
        onHideCallback?.Invoke(this);
    }

    public void HideImmediate()
    {
        CancelInvoke();
        onHideCallback = null;
    }
}
