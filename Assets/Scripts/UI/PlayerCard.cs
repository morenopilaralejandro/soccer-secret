using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCard : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image playerCard;
    [SerializeField] private PlayerPortrait playerPortrait;
    [SerializeField] private Image imageElement;
    [SerializeField] private Image imageGender;
    [SerializeField] private TMP_Text textName;

    void OnDisable()
    {
        UnsubscribeFromPlayerNameChanged();
    }

    void OnDestroy()
    {
        UnsubscribeFromPlayerNameChanged();
    }

    private void UnsubscribeFromPlayerNameChanged()
    {
        if (player != null)
            player.OnPlayerNameChanged -= UpdatePlayer;
    }

    public void SetPlayer(Player player)
    {
        // Unsubscribe from previous player's event before changing reference.
        if (this.player != null)
            this.player.OnPlayerNameChanged -= UpdatePlayer;

        this.player = player;

        if (this.player != null)
        {
            this.player.OnPlayerNameChanged += UpdatePlayer;

            if (textName != null)
                textName.text = player.PlayerName;
            if (playerCard != null)
                playerCard.color = ElementManager.Instance.GetPositionColor(player.Position);

            if (playerPortrait != null)
            {
                playerPortrait.SetPlayerImage(player.SpritePlayerPortrait);
                playerPortrait.SetWearImage(player.SpriteWearPortrait);
            }

            if (imageElement != null)
                imageElement.sprite = ElementManager.Instance.GetElementIcon(player.Element);
            if (imageGender != null)
                imageGender.sprite = ElementManager.Instance.GetGenderIcon(player.Gender);
        }
        else
        {
            if (textName != null)
                textName.text = "";
            if (playerPortrait != null)
                playerPortrait.enabled = false;
        }
    }

    public void UpdatePlayer()
    {
        if (this == null || !this.isActiveAndEnabled || player == null)
            return;
        SetPlayer(player);
    }
}
