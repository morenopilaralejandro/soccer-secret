using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bar : MonoBehaviour
{
    [SerializeField] private TMP_Text textNumber;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayer(Player player, PlayerStats stat)
    {
        if (player != null)
        {
            textNumber.text = $"{player.GetStat(stat)}/{player.GetMaxStat(stat)}";
        }
        else
        {
            textNumber.text = "";
        }
    }
}
