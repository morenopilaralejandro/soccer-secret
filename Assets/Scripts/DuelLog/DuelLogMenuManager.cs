using UnityEngine;

public class DuelLogMenuManager : MonoBehaviour
{
    [SerializeField] private DuelLogPopup popupPrefab;
    [SerializeField] private Transform contentParent; // ScrollView content

    // Call this when opening the menu
    public void PopulateLog()
    {
        // Clear previous items
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Add a static popup for each log entry
        foreach (DuelLogEntry entry in DuelLogManager.Instance.DuelLogEntries)
        {
            var popup = Instantiate(popupPrefab, contentParent);

            // Use a special method or overload that doesn't start a timer
            popup.ShowStatic(entry);
        }
        gameObject.SetActive(true);
    }

    public void HideMenu()
    {
        gameObject.SetActive(false);
    }
}
