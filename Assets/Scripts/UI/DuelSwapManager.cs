using UnityEngine;

public class DuelSwapManager : MonoBehaviour
{
    public Transform leftAnchor;
    public Transform rightAnchor;
    public RectTransform panelToggle;
    public RectTransform panelSwap;

    private bool duelOnLeft = true;

    public void SwapSides()
    {
        if (duelOnLeft)
        {
            panelToggle.SetParent(rightAnchor, false);
            panelSwap.SetParent(leftAnchor, false);
        }
        else
        {
            panelToggle.SetParent(leftAnchor, false);
            panelSwap.SetParent(rightAnchor, false);
        }
        duelOnLeft = !duelOnLeft;
    }
}
