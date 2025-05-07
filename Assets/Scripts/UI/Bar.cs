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

    public void SetPlayer(Player player)
    {
        if (player != null)
        {
            textNumber.text = $"{player.GetStat(PlayerStats.Hp)}/{player.GetMaxStat(PlayerStats.Hp)}";
        }
        else
        {
            textNumber.text = "";
        }
    }
}
