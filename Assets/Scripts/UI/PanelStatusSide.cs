using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    [SerializeField] private LocalizedString command1Label;
    [SerializeField] private LocalizedString command2Label;

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
            barHp.SetPlayer(player, PlayerStats.Hp);
            barSp.SetPlayer(player, PlayerStats.Sp);
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
                    command1Label.StringChanged += (value) => textCommand.text = value;
                    command1Label.RefreshString();
                    textCommand.color = Color.white;
                    break;
                case DuelCommand.Skill:
                    command2Label.StringChanged += (value) => textCommand.text = value;
                    command2Label.RefreshString();
                    textCommand.color = Color.white;
                    break;
                case DuelCommand.Secret:
                    textCommand.text = duelParticipant.Secret.SecretName;
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
