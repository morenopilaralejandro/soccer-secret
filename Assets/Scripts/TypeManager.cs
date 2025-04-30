using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Type {
    Fire,
    Ice,
    Light,
    Evil,
    Air,
    Forest,
    Earth,
    Electric,
    Water
}

public class TypeManager : MonoBehaviour
{
    public static TypeManager Instance { get; private set; }

    public Sprite[] typeIcons; // assign in Inspector, matches PlayerType order

    // Get the type order as an array
    private Type[] typeOrder = {
        Type.Fire,
        Type.Ice,
        Type.Light,
        Type.Evil,
        Type.Air,
        Type.Forest,
        Type.Earth,
        Type.Electric,
        Type.Water
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsPlayerEffective(GameObject off, GameObject def) {
        Player offPlayer = off.GetComponent<Player>();
        Player defPlayer = def.GetComponent<Player>();

        if(offPlayer == null || defPlayer == null) return false;

        int offIndex = System.Array.IndexOf(typeOrder, offPlayer.type);
        int defIndex = System.Array.IndexOf(typeOrder, defPlayer.type);

        // Super effective if def is the next in order (with wrap-around)
        int nextIndex = (offIndex + 1) % typeOrder.Length;

        return defIndex == nextIndex;
    }

    public Sprite UpdateTypeIcon(Type type)
    {
        //iconRenderer = transform.Find("TypeIcon").GetComponent<SpriteRenderer>();
        int typeIndex = (int)type; // enum to int index
        if (typeIcons != null &&
            typeIcons.Length > typeIndex &&
            typeIcons[typeIndex] != null)
        {
            return typeIcons[typeIndex];
        }
        else
        {
            return null; // or set to a default/placeholder icon
        }
    }
}
