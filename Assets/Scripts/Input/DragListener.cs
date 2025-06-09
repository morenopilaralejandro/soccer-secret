using UnityEngine;

public class DragListener : MonoBehaviour
{
    public DragDetector dragDetector;

    void OnEnable()
    {
        if (dragDetector != null)
        {
            dragDetector.OnDragStart += DragStarted;
            dragDetector.OnDrag += Dragging;
            dragDetector.OnDragEnd += DragEnded;
        }
    }

    void OnDisable()
    {
        if (dragDetector != null)
        {
            dragDetector.OnDragStart -= DragStarted;
            dragDetector.OnDrag -= Dragging;
            dragDetector.OnDragEnd -= DragEnded;
        }
    }

    void DragStarted(Vector2 pos) => Debug.Log("Drag START at " + pos);
    void Dragging(Vector2 pos)    => Debug.Log("Dragging at " + pos);
    void DragEnded(Vector2 pos)   => Debug.Log("Drag END at " + pos);
}
