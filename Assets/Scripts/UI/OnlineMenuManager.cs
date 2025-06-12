using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public class OnlineMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject panelMain;

    [SerializeField] private TextMeshProUGUI loadingStatusText;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private QuickPlay quickPlay;

    [SerializeField] public Button buttonQuickPlay;

    [SerializeField] private LocalizedString messageConnecting;
    [SerializeField] private LocalizedString messageSearching;
    [SerializeField] private LocalizedString messageFailed;
    [SerializeField] private LocalizedString messageFound;
    [SerializeField] private LocalizedString messageDisconnected;

    void Start()
    {
        AudioManager.Instance.PlayBgm("BgmMainTheme");
        loadingStatusText.text = "";
        buttonQuickPlay.interactable = true;
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void OnEnable()
    {
        if (quickPlay != null)
        {
            QuickPlay.OnStatusUpdate += OnStatusUpdate;
            QuickPlay.OnMatchFound += OnMatchFound;
        }
    }

    private void OnDisable()
    {
        if (quickPlay != null)
        {
            QuickPlay.OnStatusUpdate -= OnStatusUpdate;
            QuickPlay.OnMatchFound -= OnMatchFound;
        }
    }

    public void ButtonButtonQuickPlay()
    {
        AudioManager.Instance.PlaySfx("SfxMenuTap");
        buttonQuickPlay.interactable = false;
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
        quickPlay.StartQuickPlay();
    }

    private void OnStatusUpdate(int messageCode, bool isDisconnected)
    {
        switch(messageCode)
        {
            case 1:
                loadingStatusText.text = messageConnecting.GetLocalizedString();
                break;
            case 2:
                loadingStatusText.text = messageSearching.GetLocalizedString();
                break;
            case 3:
                loadingStatusText.text = messageFailed.GetLocalizedString();
                break;
            case 4:
                loadingStatusText.text = messageFound.GetLocalizedString();
                break;
            case 5:
                loadingStatusText.text = messageDisconnected.GetLocalizedString();
                break;
            default:
                loadingStatusText.text = "";
                break;
        }

        if (isDisconnected)
            buttonQuickPlay.interactable = true;
    }

    void OnMatchFound()
    {
        //PhotonNetwork.LoadLevel("YourGameScene");
    }
}
