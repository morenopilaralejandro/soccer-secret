using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DebuffIcon : MonoBehaviour
{
    [Header("Assign these in the inspector")]
    [SerializeField] private SpriteRenderer debuffSprite;

    void Start()
    {

    }

    public void ShowSpeedDebuff()
    {
        if (debuffSprite != null)
        {
            debuffSprite.enabled = true; // Show the sprite
            debuffSprite.color = ColorManager.GetDebuffColor("speed");
        }
    }

    public void HideDebuff()
    {
        if (debuffSprite != null)
        {
            debuffSprite.enabled = false; // Hide the sprite
        }
    }
}
