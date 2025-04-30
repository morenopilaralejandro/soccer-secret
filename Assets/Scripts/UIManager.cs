using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Assign this in the Inspector or with Find/other methods
    [SerializeField] private GameObject panelBottom;

    // Singleton instance (optional, for easy accessibility)
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowPanelBottom()
    {
        panelBottom.SetActive(true);
    }

    public void HidePanelBottom()
    {
        panelBottom.SetActive(false);
    }

    // Optionally, a toggle method
    public void SetPanelBottomVisible(bool visible)
    {
        panelBottom.SetActive(visible);
    }

    public void OnCommand1Tapped()
    {
        Debug.Log("Command1 tapped!");
        // Handle your logic here
    }
}
