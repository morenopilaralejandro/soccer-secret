using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Element { Fire, Ice, Holy, Evil, Air, Forest, Earth, Electric, Water }

public enum Position { Fw, Mf, Df, Gk }

public enum Gender { M, F }

public class ElementManager : MonoBehaviour
{
    public static ElementManager Instance { get; private set; }

    [SerializeField] private Color[] positionColors = {
        new Color(0.8549f, 0.0941f, 0.0941f, 1f),
        new Color(30f/255f, 62f/255f, 186f/255f, 1f),
        new Color(2f/255f, 122f/255f, 4f/255f, 1f),
        new Color(226f/255f, 120f/255f, 0f/255f, 1f)
    };
    [SerializeField] private Color[] elementColors = {
        new Color(1f, 0f, 0f, 1f),
        new Color(0.5f, 0.8f, 1f, 1f),
        new Color(1f, 1f, 0f, 1f),
        new Color(0.8f, 0.6f, 0.8f, 1f),
        new Color(1f, 1f, 1f, 1f),
        new Color(0.2f, 0.8f, 0.2f, 1f),
        new Color(0.6f, 0.4f, 0.2f, 1f),
        new Color(1f, 0.92f, 0.016f, 1f),
        new Color(0f, 0f, 1f, 1f)
    };

    [SerializeField] private Sprite[] elementIcons; // assign in Inspector, matches elementOrder
    [SerializeField] private Sprite[] genderIcons; // assign in Inspector, matches Gender Order
    [SerializeField] private Element[] elementOrder = { Element.Fire, Element.Ice, Element.Holy, Element.Evil, Element.Air, Element.Forest, Element.Earth, Element.Electric, Element.Water };

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

    public bool IsEffective(Element off, Element def) {
        int offIndex = System.Array.IndexOf(elementOrder, off);
        int defIndex = System.Array.IndexOf(elementOrder, def);

        // Super effective if def is the next in order (with wrap-around)
        int nextIndex = (offIndex + 1) % elementOrder.Length;

        return defIndex == nextIndex;
    }

    public Sprite GetElementIcon(Element element)
    {
        //iconRenderer = transform.Find("ElementIcon").GetComponent<SpriteRenderer>();
        int index = (int)element; // enum to int index
        if (elementIcons != null &&
            elementIcons.Length > index &&
            elementIcons[index] != null)
        {
            return elementIcons[index];
        }
        else
        {
            return elementIcons[0]; // or set to a default/placeholder icon
        }
    }

    public Sprite GetGenderIcon(Gender gender)
    {
        //iconRenderer = transform.Find("ElementIcon").GetComponent<SpriteRenderer>();
        int index = (int)gender; // enum to int index
        if (genderIcons != null &&
            genderIcons.Length > index &&
            genderIcons[index] != null)
        {
            return genderIcons[index];
        }
        else
        {
            return genderIcons[0]; // or set to a default/placeholder icon
        }
    }

    public Color GetPositionColor(Position position)
    {
        int index = (int)position; // enum to int index
        if (positionColors != null &&
            positionColors.Length > index &&
            positionColors[index] != null)
        {
            return positionColors[index];
        }
        else
        {
            return new Color();
        }
    }

    public Color GetElementColor(Element element)
    {
        int index = (int)element; // enum to int index
        if (elementColors != null &&
            elementColors.Length > index &&
            elementColors[index] != null)
        {
            return elementColors[index];
        }
        else
        {
            return new Color();
        }
    }
}
