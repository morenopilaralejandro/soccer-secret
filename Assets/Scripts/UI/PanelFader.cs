using UnityEngine;

public class PanelFader : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float visibleDuration = 1.5f;
    [SerializeField] private float invisibleDuration = 1.5f;
    [SerializeField] private float targetAlpha = 1f;
    [SerializeField] private  CanvasGroup canvasGroup;

    private Coroutine fadeCoroutine;

    void OnEnable()
    {
        // When panel becomes active, start fading
        fadeCoroutine = StartCoroutine(FadeLoop());
    }

    void OnDisable()
    {
        // Prevent coroutine from running after the panel is disabled
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    System.Collections.IEnumerator FadeLoop()
    {
        while (true)
        {
            // Fade In
            yield return StartCoroutine(FadeTo(targetAlpha));
            // Stay fully visible
            yield return new WaitForSeconds(visibleDuration);
            // Fade Out
            yield return StartCoroutine(FadeTo(0f));
            // Stay fully invisible
            yield return new WaitForSeconds(invisibleDuration);
        }
    }

    System.Collections.IEnumerator FadeTo(float alpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, alpha, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = alpha;
    }
}
