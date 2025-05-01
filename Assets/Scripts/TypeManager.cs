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

public enum Posi {
    Fw,
    Mf,
    Df,
    Gk
}

public class TypeManager : MonoBehaviour
{
    public static TypeManager Instance { get; private set; }

    public Sprite[] TypeIcons; // assign in Inspector, matches PlayerType order
    public Color[] PosiColors = {
        new Color(0.8549f, 0.0941f, 0.0941f, 1f),
        new Color(30f/255f, 62f/255f, 186f/255f, 1f),
        new Color(2f/255f, 122f/255f, 4f/255f, 1f),
        new Color(226f/255f, 120f/255f, 0f/255f, 1f)
    };

    // Get the type order as an array
    public Type[] TypeOrder = {
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

    public Posi[] PosiOrder = {
        Posi.Fw,
        Posi.Mf,
        Posi.Df,
        Posi.Gk
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

        int offIndex = System.Array.IndexOf(TypeOrder, offPlayer.type);
        int defIndex = System.Array.IndexOf(TypeOrder, defPlayer.type);

        // Super effective if def is the next in order (with wrap-around)
        int nextIndex = (offIndex + 1) % TypeOrder.Length;

        return defIndex == nextIndex;
    }

    public Sprite GetTypeIcon(Type type)
    {
        //iconRenderer = transform.Find("TypeIcon").GetComponent<SpriteRenderer>();
        int typeIndex = (int)type; // enum to int index
        if (TypeIcons != null &&
            TypeIcons.Length > typeIndex &&
            TypeIcons[typeIndex] != null)
        {
            return TypeIcons[typeIndex];
        }
        else
        {
            return null; // or set to a default/placeholder icon
        }
    }

    public Color GetPosiColor(Posi posi)
    {
        int index = (int)posi; // enum to int index
        if (PosiColors != null &&
            PosiColors.Length > index &&
            PosiColors[index] != null)
        {
            return PosiColors[index];
        }
        else
        {
            return new Color();
        }
    }
}
