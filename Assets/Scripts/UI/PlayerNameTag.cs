using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerNameTag : MonoBehaviour
{
    [Header("Assign these in the inspector")]
    [SerializeField] private Player player;
    [SerializeField] private Image panel;
    [SerializeField] private Image imageElement;
    [SerializeField] private TextMeshProUGUI textName;

    void OnDisable()
    {
        if (player != null)
            player.OnPlayerNameChanged -= UpdatePlayer;
    }

    void OnDestroy() {
        if (player != null)
            player.OnPlayerNameChanged -= UpdatePlayer;
    }

    private void SetElement(Sprite sprite) 
    {
        imageElement.sprite = sprite;
    }

    public void SetName(string playerName)
    {
        if (textName)
        {
            // Ensure at least 3 characters, pad if needed, then take substring and make uppercase
            string displayName = playerName.ToUpper().PadRight(3).Substring(0, 3);
            textName.text = displayName;
        }
    }

    private void SetBackgroundColor(Color color)
    {
        if (panel)
            panel.color = color;
    }

    public void SetPlayer(Player player)
    {
        // Unsubscribe from old player's event!
        if (this.player != null)
            this.player.OnPlayerNameChanged -= UpdatePlayer;

        this.player = player;

        if (this.player != null)
        {
            this.player.OnPlayerNameChanged += UpdatePlayer;

            if (this == null || !this.isActiveAndEnabled)
                return;

            SetElement(ElementManager.Instance.GetElementIcon(player.Element));
            SetName(player.PlayerName);

            if (player.TeamIndex == 0)
                SetBackgroundColor(ColorManager.GetTeamIndicatorColor("ally"));
            else
                SetBackgroundColor(ColorManager.GetTeamIndicatorColor("opp"));
        }
        else
        {
            if (textName != null)
                textName.text = "";
            if (this != null) 
                enabled = false;
        }
    }

    public void UpdatePlayer()
    {
        if (this == null || !this.isActiveAndEnabled || player == null)
            return;
        SetPlayer(player);
    }

}
