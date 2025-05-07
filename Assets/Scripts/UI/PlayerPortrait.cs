using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPortrait : MonoBehaviour
{
    [SerializeField] private Image ImageWear;
    [SerializeField] private Image ImagePlayer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetWearImage(Sprite wearSprite)
    {
        if (ImageWear != null)
            ImageWear.sprite = wearSprite;
    }

    public void SetPlayerImage(Sprite playerSprite)
    {
        if (ImagePlayer != null)
            ImagePlayer.sprite = playerSprite;
    }

}
