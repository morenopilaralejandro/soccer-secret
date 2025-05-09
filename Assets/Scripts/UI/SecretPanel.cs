using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SecretPanel : MonoBehaviour
{
    [SerializeField] private List<SecretCommandSlot> slots;     // Assign 6 slot elements in Inspector

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateSecretSlots(List<Secret> currentSecret, Category category)
    {
        int slotIndex = 0;

        // Fill slots only with secrets matching the given category
        foreach (var secret in currentSecret)
        {
            if (secret.Category == category && slotIndex < slots.Count)
            {
                slots[slotIndex].SetSecret(secret);
                slotIndex++;
            }
        }

        // Set remaining slots to null
        for (; slotIndex < slots.Count; slotIndex++)
        {
            slots[slotIndex].SetSecret(null);
        }
    }

}
