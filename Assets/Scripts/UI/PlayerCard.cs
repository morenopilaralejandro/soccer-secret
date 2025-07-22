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
        if (player != null)
            player.OnPlayerNameChanged -= UpdatePlayer;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayer(Player player)
    {
        this.player = player;
        this.player.OnPlayerNameChanged += UpdatePlayer;
        if (player != null)
        {
            textName.text = player.PlayerName;
            playerCard.color = ElementManager.Instance.GetPositionColor(player.Position);
            playerPortrait.SetPlayerImage(player.SpritePlayerPortrait);
            playerPortrait.SetWearImage(player.SpriteWearPortrait);
            imageElement.sprite = ElementManager.Instance.GetElementIcon(player.Element);
            imageGender.sprite = ElementManager.Instance.GetGenderIcon(player.Gender); 
        }
        else
        {
            textName.text = "";
            playerPortrait.enabled = false;
        }
    }

    public void UpdatePlayer()
    {
        SetPlayer(player);
    }
}
