using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Singleton instance (optional, for easy accessibility)
    public static UIManager Instance { get; private set; }
    public int DuelPlayerIndex { get; set; }
    public string secretCat { get; set; }

    // Assign this in the Inspector or with Find/other methods
    [SerializeField] private GameObject panelBottom;
    [SerializeField] private GameObject panelExtra;
    [SerializeField] private GameObject buttonInfo;

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

    // Toggle method
    public void SetDuelUiMainVisible(bool visible)
    {
        panelBottom.SetActive(visible);
        buttonInfo.SetActive(visible);
    }

    public void SetDuelUiExtraVisible(bool visible)
    {
        panelExtra.SetActive(visible);
    }

    public void OnButtonInfoTapped()
    {
        Debug.Log("ButtonInfo tapped!");
        SetDuelUiExtraVisible(!panelExtra.activeSelf);
    }

    public void OnCommand1Tapped()
    {
        Debug.Log("Command1 tapped!");
        GameManager.Instance.ExecuteDuel(DuelPlayerIndex, 1, null);
    }

    public void OnCommand2Tapped()
    {
        Debug.Log("Command2 tapped!");
        GameManager.Instance.ExecuteDuel(DuelPlayerIndex, 2, null);
    }
}
