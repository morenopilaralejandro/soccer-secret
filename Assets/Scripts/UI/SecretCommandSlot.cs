using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SecretCommandSlot : MonoBehaviour
{
    [SerializeField] private Image secretCommandSlot;
    [SerializeField] private TMP_Text textName;
    [SerializeField] private TMP_Text textCost;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSecret(Secret secret)
    {
        //playerCard.color = ElementManager.Instance.GetPositionColor(player.Position);
        if (secret != null)
        {
            textName.text = secret.SecretNameEn;
            textName.color = ElementManager.Instance.GetElementColor(secret.Element);
            textCost.text = $"{secret.Cost}";
        }
        else
        {
            textName.text = "";
            textCost.text = "";
            secretCommandSlot.enabled = false;
        }
    }
}
