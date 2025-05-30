using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class DrawLineOnDrag : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Collider touchArea;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool isDragging = false;
    private List<Vector3> linePoints = new List<Vector3>();
    private int currentPointIndex = 0;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private float moveTolerance = 0.1f;

    [SerializeField] private float minSegmentDistance = 0.3f;
    [SerializeField] private float maxLineLength = 6f;
    private bool awaitingFirstSegment = false;
    [SerializeField] private LayerMask ignoreLayer;

    [SerializeField] private BoxCollider boundTop;
    [SerializeField] private BoxCollider boundBottom;
    [SerializeField] private BoxCollider boundLeft;
    [SerializeField] private BoxCollider boundRight;
    [SerializeField] private float bottomOffset = 0.2f;

    void Start()
    {

    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, mainCamera.nearClipPlane));
            touchPosition.y = 0f;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                                    return;
                    if (IsTouchingCharacter(touch.position))
                    {
                        isDragging = true;
                        awaitingFirstSegment = true; // We're waiting for the first drag
                    }
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        AudioManager.Instance.PlaySfx("SfxDrawLine");
                        // No line yet, so just add normally
                        if (linePoints.Count == 0)
                        {
                            if (CanAddPoint(touchPosition) && IsWithinBounds(touchPosition))
                            {
                                lineRenderer.positionCount = 1;
                                lineRenderer.SetPosition(0, touchPosition);
                                linePoints.Add(touchPosition);
                                awaitingFirstSegment = false; // Done!
                            }
                        }
                        // Existing line, and this is first movement since Began
                        else if (awaitingFirstSegment)
                        {
                            float dist = Vector3.Distance(linePoints[linePoints.Count - 1], touchPosition);
                            if (dist >= minSegmentDistance)
                            {
                                // Clear old line, start a new one!
                                linePoints.Clear();
                                lineRenderer.positionCount = 0;

                                lineRenderer.positionCount = 1;
                                lineRenderer.SetPosition(0, touchPosition);
                                linePoints.Add(touchPosition);
                            }
                            // Else do not add, just wait for further movement or drag end
                            awaitingFirstSegment = false; // Don't wait anymore
                        }
                        else
                        {
                            // Regular ongoing drag, add new points if minSegmentDistance met
                            if (CanAddPoint(touchPosition) && IsWithinBounds(touchPosition) && IsFarEnough(touchPosition))
                            {
                                lineRenderer.positionCount++;
                                lineRenderer.SetPosition(lineRenderer.positionCount - 1, touchPosition);
                                linePoints.Add(touchPosition);
                            }
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    awaitingFirstSegment = false;
                    if (linePoints.Count > 0)
                    {
                        isMoving = true;
                        currentPointIndex = 0;
                    }
                    break;
            }
        }

        if (player.IsStunned || player.IsKicking || player.IsControlling)
            return; // Don't process movement

        if (isMoving && !GameManager.Instance.IsMovementFrozen)
        {
            MoveAlongLine();
        }
    }

    private bool IsTouchingCharacter(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayer))
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
        return point.x >= boundLeft.bounds.min.x && point.x <= boundRight.bounds.max.x &&
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
        else
        {
            isMoving = false;
        }
    }

    public void ResetLine() {
        linePoints.Clear();
        lineRenderer.positionCount = 0;
    }

}
