using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public enum BubbleType 
{ 
    Volley
}

public class Bubble : MonoBehaviour
{
    [Header("Assign these in the inspector")]
    [SerializeField] private Image panelVolley;
    [SerializeField] private TextMeshProUGUI textComment;
    [SerializeField] private float duration = 1f;

    private Coroutine hideCoroutine;

    void Start()
    {
        gameObject.SetActive(false); // Hide bubble initially
    }

    public void ShowBubble(BubbleType bubbleType)
    {
        // Enable the whole Bubble
        gameObject.SetActive(true);

        switch (bubbleType)
        {
            case BubbleType.Volley:
                panelVolley.enabled = true;

                // Stop any previous coroutine
                if (hideCoroutine != null)
                    StopCoroutine(hideCoroutine);

                // Start new coroutine and store the reference
                hideCoroutine = StartCoroutine(HideAfterSeconds(duration));
                break;
        }
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
        hideCoroutine = null;
    }
}
