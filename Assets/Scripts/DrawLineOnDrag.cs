using UnityEngine;
using System.Collections.Generic;

public class DrawLineOnDrag : MonoBehaviour
{
    private Player player;
    private LineRenderer lineRenderer;
    private bool isDragging = false;
    private Camera mainCamera;
    private List<Vector3> linePoints = new List<Vector3>();
    private int currentPointIndex = 0;
    private bool isMoving = false;
    private float hpThreshold1 = 30;
    private float hpThreshold2 = 30;
    private float speedBase = 0.2f;
    private float speedMultiplier = 0.02f;
    private float speedDebuff = 1f;
    private float speedDebuff1 = 0.5f;
    private float speedDebuff2 = 0.2f;

    public float maxLineLength = 6f;
    public LayerMask ignoreLayer;
    public Transform playerChildWithCollider;

    public BoxCollider boundTop;
    public BoxCollider boundBottom;
    public BoxCollider boundLeft;
    public BoxCollider boundRight;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        player = GetComponent<Player>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, mainCamera.nearClipPlane));

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsTouchingCharacter(touch.position))
                    {
                        isDragging = true;
                        lineRenderer.positionCount = 0;
                        linePoints.Clear();
                    }
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        touchPosition.y = 0;
                        if (CanAddPoint(touchPosition) && IsWithinBounds(touchPosition))
                        {
                            lineRenderer.positionCount++;
                            lineRenderer.SetPosition(lineRenderer.positionCount - 1, touchPosition);
                            linePoints.Add(touchPosition);
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    if (linePoints.Count > 0)
                    {
                        isMoving = true;
                        currentPointIndex = 0;
                    }
                    break;
            }
        }

        if (player.isStunned)
        {
            // Optionally, you could play a stun animation or effects here
            return; // Don't process movement
        }

        if (isMoving && !GameManager.Instance.IsGameFrozen)
        {
            if (player.hp <= hpThreshold1) {
                speedDebuff = speedDebuff1;   
            } else {
                if (player.hp <= hpThreshold2) {
                    speedDebuff = speedDebuff2;   
                } else {
                    speedDebuff = 1f;
                }
            }
            MoveAlongLine();
        }
    }

    private bool IsTouchingCharacter(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayer))
        {
            return hit.collider.gameObject == playerChildWithCollider.gameObject;
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

    private bool IsWithinBounds(Vector3 point)
    {
        float bottomOffset = 0.5f;

        return point.x >= boundLeft.bounds.min.x && point.x <= boundRight.bounds.max.x &&
               point.z >= (boundBottom.bounds.min.z + bottomOffset) && point.z <= boundTop.bounds.max.z;
    }

    private void MoveAlongLine()
    {
        if (currentPointIndex < linePoints.Count)
        {
            Vector3 targetPosition = linePoints[currentPointIndex];
            //Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            float speedCalc = (player.speed * speedMultiplier + speedBase) * speedDebuff * Time.deltaTime;
            Debug.Log("speedCalc:" + speedCalc);
            Debug.Log("speedDebuff:" + speedDebuff);
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, speedCalc);
            transform.position = newPosition;

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
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
}
