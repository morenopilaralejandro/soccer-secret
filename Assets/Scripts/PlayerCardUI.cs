using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    public Image PlayerCard;
    public Image imagePortrait;
    public TMP_Text textName;


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
        PlayerCard.color = TypeManager.Instance.GetPosiColor(player.posi);
        if (player != null)
        {
            textName.text = player.playerName;

            if (player.spritePortrait != null)
            {
                imagePortrait.sprite = player.spritePortrait;
                imagePortrait.enabled = true;
            }
            else
            {
                imagePortrait.enabled = false;
            }
        }
        else
        {
            textName.text = "";
            imagePortrait.enabled = false;
        }
    }
}
