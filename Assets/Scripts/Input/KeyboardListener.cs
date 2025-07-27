using UnityEngine;

public class KeyboardListener : MonoBehaviour
{
    public KeyboardDetector keyboardDetector;

    void OnEnable()
    {
        if (keyboardDetector != null)
            keyboardDetector.OnActionKey += HandleActionKey;
    }

    void OnDisable()
    {
        if (keyboardDetector != null)
            keyboardDetector.OnActionKey -= HandleActionKey;
    }

    void HandleActionKey()
    {
        Debug.Log("ActionKey - Space pressed");
    }
}
