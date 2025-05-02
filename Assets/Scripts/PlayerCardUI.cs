using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    [SerializeField] private Image playerCard;
    [SerializeField] private Image imagePortrait;
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
            textName.text = player.PlayerName;
            imagePortrait.sprite = player.SpritePortrait;
            imageElement.sprite = ElementManager.Instance.GetElementIcon(player.Element);
        }
        else
        {
            textName.text = "";
            imagePortrait.enabled = false;
        }
    }
}
