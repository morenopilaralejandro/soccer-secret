using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DuelLogPopupManager : MonoBehaviour
{
    [SerializeField] private DuelLogPopup popupPrefab;
    [SerializeField] private Transform popupParent; // E.g. a vertical layout group under your canvas
    [SerializeField] private int maxPopups = 3;

    private List<DuelLogPopup> activePopups = new List<DuelLogPopup>();

    void OnEnable()
    {
        DuelLogManager.Instance.OnNewEntry += ShowPopup;
    }
    void OnDisable()
    {
        DuelLogManager.Instance.OnNewEntry -= ShowPopup;
    }

    void ShowPopup(DuelLogEntry entry)
    {
        // Remove oldest if at max capacity
        if (activePopups.Count >= maxPopups)
        {
            // Hide & remove first popup
            activePopups[0].HideImmediate();
            Destroy(activePopups[0].gameObject);
            activePopups.RemoveAt(0);
        }

        // Instantiate new popup at the end (bottom)
        DuelLogPopup newPopup = Instantiate(popupPrefab, popupParent);
        newPopup.Show(entry, OnPopupHidden); // Pass a callback for when it hides
        activePopups.Add(newPopup);
    }

    // Callback for when a popup's display time finishes
    void OnPopupHidden(DuelLogPopup popup)
    {
        activePopups.Remove(popup);
        Destroy(popup.gameObject);
    }
}
