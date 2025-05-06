using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelCollider : MonoBehaviour
{
    public BallBehavior ball;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider otherDuelCollider)
    {
        Debug.Log("DuelCollider OnTriggerEnter: " + otherDuelCollider.name + " (Tag: " + otherDuelCollider.tag + ")");
        GameObject thisRootObj = GetComponent<Collider>().transform.root.gameObject;
        GameObject otherRootObj = otherDuelCollider.transform.root.gameObject;

        // Get the tag of the player in possession
        string possesionPlayerTag = tag;

        // Get the tag of the other collider
        string otherPlayerTag = otherDuelCollider.tag;

        Debug.Log("DuelCollider Tags: (" + possesionPlayerTag + ", " + otherPlayerTag + ")");
        // If tags are different and both are either "Ally" or "Opp"
        if (thisRootObj == ball.possesionPlayer && possesionPlayerTag != null && (otherPlayerTag == "Ally" || otherPlayerTag == "Opp") && possesionPlayerTag != otherPlayerTag)
        {            
            GameManager.Instance.HandleDuel(thisRootObj, otherRootObj, 0);
        }
    }
}
