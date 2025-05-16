using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCard : MonoBehaviour
{
    [SerializeField] private Image playerCard;
    [SerializeField] private PlayerPortrait playerPortrait;
    [SerializeField] private Image imageElement;
    [SerializeField] private Image imageGender;
    [SerializeField] private TMP_Text textName;


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
        //add the two card components to the game manager and set the player there
        /*
            public PlayerCardUI playerCardUIPrefab;
            PlayerCardUI playerCardUI = Instantiate(playerCardUIPrefab, someParentTransform);
            playerCardUI.SetPlayer(newPlayer);
        */
        playerCard.color = ElementManager.Instance.GetPositionColor(player.Position);
        if (player != null)
        {
            textName.text = player.PlayerNameEn;
            playerPortrait.SetPlayerImage(player.SpritePlayerPortrait);
            playerPortrait.SetPlayerImage(player.SpriteWearPortrait);
            imageElement.sprite = ElementManager.Instance.GetElementIcon(player.Element);
            imageGender.sprite = ElementManager.Instance.GetGenderIcon(player.Gender); 
        }
        else
        {
            textName.text = "";
            playerPortrait.enabled = false;
        }
    }
}
