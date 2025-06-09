using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CrosshairManager : MonoBehaviour
{
    public static CrosshairManager Instance { get; private set; }

    [SerializeField] private Image crosshairImage;
    [SerializeField] private float crosshairDisplayDuration = 0.2f;

    private Coroutine hideCrosshairCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowCrosshair(Vector2 screenPos)
    {
        if (crosshairImage == null) return;
        crosshairImage.transform.position = screenPos;
        crosshairImage.enabled = true;
    }

    public void HideCrosshairAfterDelay()
    {
        RestartHideCrosshairCoroutine();
    }

    public void HideCrosshairImmediately()
    {
        if (hideCrosshairCoroutine != null)
            StopCoroutine(hideCrosshairCoroutine);
        if (crosshairImage)
            crosshairImage.enabled = false;
    }

    public bool IsTouchingCrosshair(Vector2 screenPosition)
    {
        if (!crosshairImage || !crosshairImage.enabled) return false;
        Canvas canvas = crosshairImage.canvas;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(
            crosshairImage.rectTransform, screenPosition, eventCamera);
    }

    private void RestartHideCrosshairCoroutine()
    {
        if (hideCrosshairCoroutine != null)
            StopCoroutine(hideCrosshairCoroutine);
        hideCrosshairCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(crosshairDisplayDuration);
        if (crosshairImage) crosshairImage.enabled = false;
    }
}
