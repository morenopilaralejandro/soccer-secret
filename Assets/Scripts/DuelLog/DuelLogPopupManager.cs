using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DuelLogPopupManager : MonoBehaviour
{
    [Header("Popup Lifetime")]
    [SerializeField] private float minDisplayTime = 2f;           // Minimum on screen time
    [SerializeField] private float autoHideTime = 2f;             // Disappear after this
    [Header("Stagger Intervals")]
    [SerializeField] private float addStaggerInterval = 0.3f;     // Delay between shows
    [SerializeField] private float removeStaggerInterval = 1f;    // Delay between removals
    [Header("Setup")]
    [SerializeField] private DuelLogPopup popupPrefab;
    [SerializeField] private Transform popupParent;
    [SerializeField] private int maxPopups = 2;

    private class PopupData
    {
        public DuelLogPopup popup;
        public float spawnTime;
        public bool removalScheduled;
    }

    private List<PopupData> activePopups = new List<PopupData>();
    private Queue<DuelLogEntry> pendingEntries = new Queue<DuelLogEntry>();
    private bool isAdding = false;
    private bool isRemoving = false;

    void OnEnable()    { DuelLogManager.Instance.OnNewEntry += EnqueuePopup; }
    void OnDisable()   { DuelLogManager.Instance.OnNewEntry -= EnqueuePopup; }

    void EnqueuePopup(DuelLogEntry entry)
    {
        pendingEntries.Enqueue(entry);

        if (!isAdding)
            StartCoroutine(AddPopupsStaggered());
    }

    IEnumerator AddPopupsStaggered()
    {
        isAdding = true;

        while (pendingEntries.Count > 0 && activePopups.Count < maxPopups)
        {
            DuelLogEntry entry = pendingEntries.Dequeue();
            DuelLogPopup popup = Instantiate(popupPrefab, popupParent);
            popup.Show(entry);
            activePopups.Add(new PopupData { popup = popup, spawnTime = popup.spawnTime, removalScheduled = false });

            if (activePopups.Count < maxPopups)
                yield return new WaitForSecondsRealtime(addStaggerInterval);
        }

        isAdding = false;

        // Start removal process if any popups need to be removed (either new popups can't fit, or need auto-hide)
        if (!isRemoving)
            StartCoroutine(RemoveDuePopupsStaggered());
    }

    IEnumerator RemoveDuePopupsStaggered()
    {
        isRemoving = true;
        while (true)
        {
            float now = Time.unscaledTime;
            bool removed = false;

            // --- 1. Remove for overflow (when at or over maxPopups) ---
            while (activePopups.Count > maxPopups)
            {
                var oldest = activePopups[0];
                float alive = now - oldest.spawnTime;
                float wait = Mathf.Max(minDisplayTime - alive, 0f);
                if (wait > 0f)
                    yield return new WaitForSecondsRealtime(wait);

                RemovePopup(oldest);
                removed = true;

                // Wait between removals for stagger
                yield return new WaitForSecondsRealtime(removeStaggerInterval);

                // Update now in case time elapsed
                now = Time.unscaledTime;
            }

            // --- 2. Remove for auto-hide (popups that reached their 5s) ---
            foreach (var pd in new List<PopupData>(activePopups))
            {
                float alive = now - pd.spawnTime;
                // Only remove if at least autoHideTime elapsed
                if (!pd.removalScheduled && alive >= autoHideTime)
                {
                    RemovePopup(pd);
                    removed = true;
                    yield return new WaitForSecondsRealtime(removeStaggerInterval);
                    break; // Only one removal per interval for staggering
                }
            }

            if (!removed)
                yield return null;

            // End if there’s nothing left to remove or add
            if (activePopups.Count == 0 && pendingEntries.Count == 0)
                break;
        }
        isRemoving = false;
    }

    void RemovePopup(PopupData pd)
    {
        pd.removalScheduled = true;
        activePopups.Remove(pd);
        Destroy(pd.popup.gameObject);
    }

    void Update()
    {
        // For robustness: if popups on screen and neither stagger is running, restart
        if (!isAdding && pendingEntries.Count > 0 && activePopups.Count < maxPopups)
            StartCoroutine(AddPopupsStaggered());

        if (!isRemoving && activePopups.Count > 0)
            StartCoroutine(RemoveDuePopupsStaggered());
    }
}
