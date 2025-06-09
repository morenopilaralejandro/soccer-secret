using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PlayerLineRenderer : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Collider touchArea;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool isDragging = false;
    private List<Vector3> linePoints = new List<Vector3>();
    private int currentPointIndex = 0;
    [SerializeField] private float moveTolerance = 0.1f;

    [SerializeField] private float minSegmentDistance = 0.3f;
    [SerializeField] private float maxLineLength = 6f;
    private bool awaitingFirstSegment = false;
    [SerializeField] private LayerMask touchAreaLayer;

    [SerializeField] private BoxCollider boundTop;
    [SerializeField] private BoxCollider boundBottom;
    [SerializeField] private BoxCollider boundLeft;
    [SerializeField] private BoxCollider boundRight;
    [SerializeField] private float bottomOffset = 0.2f;
    [SerializeField] private float rightOffset = 0.2f;

    void Start()
    {

    }

    void OnEnable()
    {
        if (InputManager.Instance.DragDetector != null)
        {
            InputManager.Instance.DragDetector.OnDragStart += HandleDragStart;
            InputManager.Instance.DragDetector.OnDrag += HandleDrag;
            InputManager.Instance.DragDetector.OnDragEnd += HandleDragEnd;
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance.DragDetector != null)
        {
            InputManager.Instance.DragDetector.OnDragStart -= HandleDragStart;
            InputManager.Instance.DragDetector.OnDrag -= HandleDrag;
            InputManager.Instance.DragDetector.OnDragEnd -= HandleDragEnd;
        }
    }

    void Update()
    {
        if (player.IsStunned || player.IsKicking || player.IsControlling)
            return; // Don't process movement

        if (GameManager.Instance.CurrentPhase == GamePhase.Battle)
        {
            MoveAlongLine();
        }
    }

    private void HandleDragStart(Vector2 pointerPosition)
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, mainCamera.nearClipPlane));
        worldPosition.y = 0f;

        if (IsTouchingCharacter(pointerPosition))
        {
            isDragging = true;
            awaitingFirstSegment = true;
            linePoints.Clear();
            lineRenderer.positionCount = 0;
        }
    }

    private void HandleDrag(Vector2 pointerPosition)
    {
        if (!isDragging) return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, mainCamera.nearClipPlane));
        worldPosition.y = 0f;

        AudioManager.Instance.PlaySfx("SfxDrawLine");

        // No line yet, so just add normally
        if (linePoints.Count == 0)
        {
            if (CanAddPoint(worldPosition) && IsWithinBounds(worldPosition))
            {
                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, worldPosition);
                linePoints.Add(worldPosition);
                awaitingFirstSegment = false;
            }
        }
        // Existing line, and this is first movement since Began
        else if (awaitingFirstSegment)
        {
            float dist = Vector3.Distance(linePoints[linePoints.Count - 1], worldPosition);
            if (dist >= minSegmentDistance)
            {
                // Clear old line, start a new one!
                linePoints.Clear();
                lineRenderer.positionCount = 0;
                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, worldPosition);
                linePoints.Add(worldPosition);
            }
            // Else do not add, just wait for further movement or drag end
            awaitingFirstSegment = false;
        }
        else
        {
            // Regular ongoing drag, add new points if minSegmentDistance met
            if (CanAddPoint(worldPosition) && IsWithinBounds(worldPosition) && IsFarEnough(worldPosition))
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, worldPosition);
                linePoints.Add(worldPosition);
            }
        }
    }

    private void HandleDragEnd(Vector2 pointerPosition)
    {
        if (isDragging)
        {
            isDragging = false;
            awaitingFirstSegment = false;
            if (linePoints.Count > 0)
            {
                currentPointIndex = 0;
            }
        }
    }

    private bool IsTouchingCharacter(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, touchAreaLayer))
        {
            return hit.collider == touchArea;
        }
        return false;
    }

    private bool CanAddPoint(Vector3 newPoint)
    {
        if (linePoints.Count == 0)
        {
            return true;
        }

        float currentLength = 0f;
        for (int i = 0; i < linePoints.Count - 1; i++)
        {
            currentLength += Vector3.Distance(linePoints[i], linePoints[i + 1]);
        }

        currentLength += Vector3.Distance(linePoints[linePoints.Count - 1], newPoint);

        return currentLength <= maxLineLength;
    }

    private bool IsFarEnough(Vector3 newPoint)
    {
        if (linePoints.Count == 0) return true;
        float distance = Vector3.Distance(linePoints[linePoints.Count - 1], newPoint);
        return distance >= minSegmentDistance;
    }

    private float GetLineLength()
    {
        if (linePoints.Count < 2) return 0f;
        float length = 0f;
        for (int i = 0; i < linePoints.Count - 1; i++)
        {
            length += Vector3.Distance(linePoints[i], linePoints[i + 1]);
        }
        return length;
    }

    private bool IsWithinBounds(Vector3 point)
    {
        return point.x >= boundLeft.bounds.min.x && point.x <= (boundRight.bounds.max.x - + rightOffset) &&
               point.z >= (boundBottom.bounds.min.z + bottomOffset) && point.z <= boundTop.bounds.max.z;
    }

    private void MoveAlongLine()
    {
       if (currentPointIndex < linePoints.Count)
        {
            Vector3 targetPosition = linePoints[currentPointIndex];
            float moveSpeed = player.GetMoveSpeed();
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed);
            player.transform.position = newPosition;

            if (Vector3.Distance(transform.position, targetPosition) < moveTolerance)
            {
                linePoints.RemoveAt(currentPointIndex);
                lineRenderer.positionCount = linePoints.Count;
                lineRenderer.SetPositions(linePoints.ToArray());
            }
        }
    }

    public void ResetLine() {
        linePoints.Clear();
        lineRenderer.positionCount = 0;
    }

}
