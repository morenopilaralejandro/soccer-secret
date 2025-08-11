using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PitchManager : MonoBehaviour
{
    public static PitchManager Instance { get; private set; }

    [SerializeField] private Material[] pitchMaterials;

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

    public Material GetPitchMaterial(PitchMaterial pitchMaterial)
    {
        int index = (int)pitchMaterial;
        if (pitchMaterials != null &&
            pitchMaterials.Length > index &&
            pitchMaterials[index] != null)
        {
            return pitchMaterials[index];
        }
        else
        {
            return pitchMaterials[0];
        }
    }

}
