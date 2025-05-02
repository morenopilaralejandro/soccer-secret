using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Element { Fire, Ice, Light, Evil, Air, Forest, Earth, Electric, Water }

public enum Position { Fw, Mf, Df, Gk }

public enum Gender { M, F }

public class ElementManager : MonoBehaviour
{
    public static ElementManager Instance { get; private set; }

    [SerializeField] private Color[] PositionColors = {
        new Color(0.8549f, 0.0941f, 0.0941f, 1f),
        new Color(30f/255f, 62f/255f, 186f/255f, 1f),
        new Color(2f/255f, 122f/255f, 4f/255f, 1f),
        new Color(226f/255f, 120f/255f, 0f/255f, 1f)
    };
    [SerializeField] private Sprite[] ElementIcons; // assign in Inspector, matches ElementOrder
    [SerializeField] private Sprite[] GenderIcons; // assign in Inspector, matches Gender Order
    [SerializeField] private Element[] ElementOrder = { Element.Fire, Element.Ice, Element.Light, Element.Evil, Element.Air, Element.Forest, Element.Earth, Element.Electric, Element.Water };

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

        int offIndex = System.Array.IndexOf(ElementOrder, offPlayer.Element);
        int defIndex = System.Array.IndexOf(ElementOrder, defPlayer.Element);

        // Super effective if def is the next in order (with wrap-around)
        int nextIndex = (offIndex + 1) % ElementOrder.Length;

        return defIndex == nextIndex;
    }

    public Sprite GetElementIcon(Element element)
    {
        //iconRenderer = transform.Find("ElementIcon").GetComponent<SpriteRenderer>();
        int index = (int)element; // enum to int index
        if (ElementIcons != null &&
            ElementIcons.Length > index &&
            ElementIcons[index] != null)
        {
            return ElementIcons[index];
        }
        else
        {
            return ElementIcons[0]; // or set to a default/placeholder icon
        }
    }

    public Sprite GetGenderIcon(Gender gender)
    {
        //iconRenderer = transform.Find("ElementIcon").GetComponent<SpriteRenderer>();
        int index = (int)gender; // enum to int index
        if (GenderIcons != null &&
            GenderIcons.Length > index &&
            GenderIcons[index] != null)
        {
            return GenderIcons[index];
        }
        else
        {
            return GenderIcons[0]; // or set to a default/placeholder icon
        }
    }

    public Color GetPositionColor(Position position)
    {
        int index = (int)position; // enum to int index
        if (PositionColors != null &&
            PositionColors.Length > index &&
            PositionColors[index] != null)
        {
            return PositionColors[index];
        }
        else
        {
            return new Color();
        }
    }
}
