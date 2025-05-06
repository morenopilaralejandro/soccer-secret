using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelCollider : MonoBehaviour
{
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
        string possessionPlayerTag = tag;

        // Get the tag of the other collider
        string otherPlayerTag = otherDuelCollider.tag;

        Debug.Log("DuelCollider Tags: (" + possessionPlayerTag + ", " + otherPlayerTag + ")");
        // If tags are different and both are either "Ally" or "Opp"
 
        if (thisRootObj.GetComponent<Player>().IsPossession
            && possessionPlayerTag != null 
            && (otherPlayerTag == "Ally" 
            || otherPlayerTag == "Opp") 
            && possessionPlayerTag != otherPlayerTag)
        {            
            GameManager.Instance.HandleDuel(thisRootObj, otherRootObj, 0);
        }
       
    }
}
