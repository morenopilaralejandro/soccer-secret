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
    
    [SerializeField] private int hpThresholdLow = 10;
    [SerializeField] private int hpThresholdHigh = 30;
    [SerializeField] private float speedBase = 0.2f;
    [SerializeField] private float speedMultiplier = 0.02f;
    [SerializeField] private float speedDebuff = 1f;
    [SerializeField] private float speedDebuffDefault = 1f;
    [SerializeField] private float speedDebuffLow = 0.5f;
    [SerializeField] private float speedDebuffHigh = 0.2f;

    [SerializeField] private float minSegmentDistance = 0.2f;
    [SerializeField] private float maxLineLength = 6f;
    [SerializeField] private LayerMask ignoreLayer;
    [SerializeField] private Transform playerChildWithCollider;

    [SerializeField] private BoxCollider boundTop;
    [SerializeField] private BoxCollider boundBottom;
    [SerializeField] private BoxCollider boundLeft;
    [SerializeField] private BoxCollider boundRight;
    [SerializeField] private float bottomOffset = 0.5f;

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
                        if (CanAddPoint(touchPosition) 
                            && IsWithinBounds(touchPosition) 
                            && IsFarEnough(touchPosition))
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

        if (player.IsStunned)
        {
            // Optionally, you could play a stun animation or effects here
            return; // Don't process movement
        }

        if (isMoving && !GameManager.Instance.IsMovementFrozen)
        {
            CalcSpeedDebuff(player);
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

    private bool IsFarEnough(Vector3 newPoint)
    {
        if (linePoints.Count == 0) return true;
        float distance = Vector3.Distance(linePoints[linePoints.Count - 1], newPoint);
        return distance >= minSegmentDistance;
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
            float speed = CalcSpeed(player);
            //Debug.Log("speed:" + speed);
            //Debug.Log("speedDebuff:" + speedDebuff);
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, speed);
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

    private float CalcSpeed(Player player) {
        return (player.GetStat(PlayerStats.Speed) * speedMultiplier + speedBase) * speedDebuff * Time.deltaTime;
    }

    private void CalcSpeedDebuff(Player player) {
        int playerHp = player.GetStat(PlayerStats.Hp);
        if (playerHp <= hpThresholdLow) {
            speedDebuff = speedDebuffLow;
        } else if (playerHp <= hpThresholdHigh) {
            speedDebuff = speedDebuffHigh;
        } else {
            speedDebuff = speedDebuffDefault;
        }
    }
}
