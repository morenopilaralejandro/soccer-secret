using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelStatusSide : MonoBehaviour
{
    [SerializeField] private GameObject panelStatusSide;
    [SerializeField] private GameObject imagePossession;
    [SerializeField] private PlayerCard playerCard;
    [SerializeField] private Bar barHp;
    [SerializeField] private Bar barSp;
    [SerializeField] private TMP_Text textCommand;
    [SerializeField] private TMP_Text textDamage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HideCommand()
    {
        textCommand.text = "";
        textDamage.text = "";
    }

    public void SetPlayer(Player player)
    {
        if (player != null)
        {
            panelStatusSide.SetActive(true);
            HideCommand();
            playerCard.SetPlayer(player);
            imagePossession.SetActive(player.IsPossession);
            barHp.SetPlayer(player);
            barSp.SetPlayer(player);
        } else {
            panelStatusSide.SetActive(false);
        }
    }

    public void SetCommand(DuelParticipant duelParticipant, float attackPressure) {
        if (duelParticipant != null) 
        {
            switch (duelParticipant.Command) 
            {
                case DuelCommand.Phys:
                    textCommand.text = "Phys.";
                    textCommand.color = Color.white;
                    break;
                case DuelCommand.Skill:
                    textCommand.text = "Skill";
                    textCommand.color = Color.white;
                    break;
                case DuelCommand.Secret:
                    textCommand.text = duelParticipant.Secret.SecretNameEn;
                    textCommand.color = ElementManager.Instance.GetElementColor(duelParticipant.Secret.Element);
                    break;
            }
            if (duelParticipant.Action == DuelAction.Offense) 
            {
                textDamage.text = attackPressure.ToString("F0");
            } else {
                textDamage.text = duelParticipant.Damage.ToString("F0");
            }           
        } else {
            panelStatusSide.SetActive(false);
        }
    }

    public void SetPlayerAndCommand(DuelParticipant duelParticipant, float attackPressure) {
        SetPlayer(duelParticipant.Player);
        SetCommand(duelParticipant, attackPressure);
    }
}
