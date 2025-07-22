using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class PlayerLineRenderer : MonoBehaviour
{
    public static PlayerLineRenderer LocalInstance;

    [SerializeField] private Player player;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Collider touchArea;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float moveTolerance = 0.1f;
    [SerializeField] private float minSegmentDistance = 0.3f;
    [SerializeField] private float maxLineLength = 6f;
    [SerializeField] private LayerMask touchAreaLayer;
    [SerializeField] private BoxCollider boundTop;
    [SerializeField] private BoxCollider boundBottom;
    [SerializeField] private BoxCollider boundLeft;
    [SerializeField] private BoxCollider boundRight;
    [SerializeField] private float topOffset = 0.3f;
    [SerializeField] private float bottomOffset = 0.5f;
    [SerializeField] private float leftOffset = 0.4f;
    [SerializeField] private float rightOffset = 0.5f;
    [SerializeField] private float  touchAreaOffset = 0.1f;

    private bool isDragging = false;
    private bool awaitingFirstSegment = false;
    private List<Vector3> linePoints = new List<Vector3>();
    private int currentPointIndex = 0;

#if PHOTON_UNITY_NETWORKING
    private PhotonView photonView => PhotonView.Get(this);
    private Vector3 networkedPosition; // For smooth sync
#endif

    private void Awake()
    {
        if (IsInputLocalPlayer())
            LocalInstance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        // Find by name if missing
        if (boundTop == null)
            boundTop = GameObject.Find("BoundTop1")?.GetComponent<BoxCollider>();
        if (boundBottom == null)
            boundBottom = GameObject.Find("BoundBottom1")?.GetComponent<BoxCollider>();
        if (boundLeft == null)
            boundLeft = GameObject.Find("BoundLeft")?.GetComponent<BoxCollider>();
        if (boundRight == null)
            boundRight = GameObject.Find("BoundRight")?.GetComponent<BoxCollider>();

        touchAreaLayer = LayerMask.GetMask("PlayerTouchArea");
    }

    private void Start()
    {
        lineRenderer.enabled = IsInputLocalPlayer();
    }

    private void OnEnable()
    {
        if (player != null && IsInputLocalPlayer() && InputManager.Instance?.DragDetector != null)
        {
            var drag = InputManager.Instance.DragDetector;
            drag.OnDragStart += HandleDragStart;
            drag.OnDrag += HandleDrag;
            drag.OnDragEnd += HandleDragEnd;
        }
    }

    private void OnDisable()
    {
        if (player != null && IsInputLocalPlayer() && InputManager.Instance?.DragDetector != null)
        {
            var drag = InputManager.Instance.DragDetector;
            drag.OnDragStart -= HandleDragStart;
            drag.OnDrag -= HandleDrag;
            drag.OnDragEnd -= HandleDragEnd;
        }
    }

    private void Update()
    {
        if (player == null || player.IsStunned || player.IsKicking || player.IsControlling)
            return;

        if (GameManager.Instance.CurrentPhase == GamePhase.Battle && IsInputLocalPlayer())
        {
            MoveAlongLine();
        }
    }

    // ---- Input Handlers ----
    private void HandleDragStart(Vector2 pointerPosition)
    {
        if (!IsInputLocalPlayer() || (EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            return;

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
        if (!IsInputLocalPlayer() || !isDragging)
            return;

        var worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, mainCamera.nearClipPlane));
        worldPosition = ClampToBounds(worldPosition);
        worldPosition.y = player.DefaultPosition.y;

        AudioManager.Instance.PlaySfx("SfxDrawLine");

        if (linePoints.Count == 0)
        {
            if (CanAddPoint(worldPosition))
            {
                AddInitialLine(player.transform.position, worldPosition);
                awaitingFirstSegment = false;
            }
        }
        else if (awaitingFirstSegment)
        {
            if (Vector3.Distance(linePoints[linePoints.Count - 1], worldPosition) >= minSegmentDistance)
                AddInitialLine(player.transform.position, worldPosition);

            awaitingFirstSegment = false;
        }
        else if (CanAddPoint(worldPosition) && IsFarEnough(worldPosition))
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, worldPosition);
            linePoints.Add(worldPosition);
        }
    }

    private void HandleDragEnd(Vector2 pointerPosition)
    {
        if (!IsInputLocalPlayer())
            return;

        if (isDragging)
        {
            isDragging = false;
            awaitingFirstSegment = false;
            if (linePoints.Count > 0)
            {
                currentPointIndex = 0;
#if PHOTON_UNITY_NETWORKING
                // Send line in multiplayer mode
                if (GameManager.Instance.IsMultiplayer && photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_SetLinePoints), RpcTarget.All, Vector3ArrayToFloatArray(linePoints.ToArray()), linePoints.Count);
                }
#endif
            }
        }
    }

    private void AddInitialLine(Vector3 start, Vector3 end)
    {
        start.z -= touchAreaOffset;
        start.y = player.DefaultPosition.y;
        end.y = player.DefaultPosition.y;

        start = ClampToBounds(start);
        end = ClampToBounds(end);
        
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, start);
        linePoints.Clear();
        linePoints.Add(start);

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(1, end);
        linePoints.Add(end);
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_SetLinePoints(float[] lineData, int count)
    {
        linePoints.Clear();
        for (int i = 0; i < count; i++)
        {
            linePoints.Add(new Vector3(lineData[i * 3], lineData[i * 3 + 1], lineData[i * 3 + 2]));
        }
        lineRenderer.positionCount = linePoints.Count;
        lineRenderer.SetPositions(linePoints.ToArray());
        currentPointIndex = 0;
    }
#endif

    private float[] Vector3ArrayToFloatArray(Vector3[] arr)
    {
        float[] data = new float[arr.Length * 3];
        for (int i = 0; i < arr.Length; i++)
        {
            data[i * 3 + 0] = arr[i].x;
            data[i * 3 + 1] = arr[i].y;
            data[i * 3 + 2] = arr[i].z;
        }
        return data;
    }

    private void MoveAlongLine()
    {
        if (currentPointIndex < linePoints.Count)
        {
            Vector3 target = linePoints[currentPointIndex];
            float moveSpeed = player.GetMoveSpeed();
            Vector3 newPosition = Vector3.MoveTowards(player.transform.position, target, moveSpeed);
            newPosition = ClampToBounds(newPosition);
            newPosition.y = player.DefaultPosition.y;    // Ensures the player never goes below default Y
            player.transform.position = newPosition;

#if PHOTON_UNITY_NETWORKING
            if (GameManager.Instance.IsMultiplayer && photonView.IsMine)
                networkedPosition = newPosition;
#endif

            if (Vector3.Distance(player.transform.position, target) < moveTolerance)
            {
                linePoints.RemoveAt(currentPointIndex);
                lineRenderer.positionCount = linePoints.Count;
                lineRenderer.SetPositions(linePoints.ToArray());
            }
        }
    }

    private bool IsTouchingCharacter(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, touchAreaLayer))
            return hit.collider == touchArea;
        return false;
    }

    private bool CanAddPoint(Vector3 newPoint)
    {
        if (linePoints.Count == 0) return true;
        float currentLength = 0f;
        for (int i = 0; i < linePoints.Count - 1; i++)
            currentLength += Vector3.Distance(linePoints[i], linePoints[i + 1]);
        currentLength += Vector3.Distance(linePoints[^1], newPoint);
        return currentLength <= maxLineLength;
    }

    private bool IsFarEnough(Vector3 newPoint)
    {
        if (linePoints.Count == 0) return true;
        return Vector3.Distance(linePoints[^1], newPoint) >= minSegmentDistance;
    }

    private Vector3 ClampToBounds(Vector3 point)
    {
        float minX = boundLeft.bounds.min.x + leftOffset;
        float maxX = boundRight.bounds.max.x - rightOffset;
        float minZ = boundBottom.bounds.min.z + bottomOffset - touchAreaOffset;
        float maxZ = boundTop.bounds.max.z - topOffset;

        return new Vector3(
            Mathf.Clamp(point.x, minX, maxX),
            point.y,
            Mathf.Clamp(point.z, minZ, maxZ)
        );
    }

    public void ResetLine()
    {
        linePoints.Clear();
        lineRenderer.positionCount = 0;
    }

    private bool IsInputLocalPlayer()
    {
#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsMultiplayer)
            return photonView != null && photonView.IsMine;
        else
            return player != null && player.ControlType == ControlType.LocalHuman;
#else
        return player != null && player.ControlType == ControlType.LocalHuman;
#endif
    }
}
