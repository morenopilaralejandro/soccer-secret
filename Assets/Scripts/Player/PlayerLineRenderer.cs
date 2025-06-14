using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class PlayerLineRenderer : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunObservable
#endif
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

#if PHOTON_UNITY_NETWORKING
    // Helper for safe network mode
    private PhotonView photonView => PhotonView.Get(this);
    private bool IsMultiplayer => PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined;
#else
    private bool IsMultiplayer => false;
#endif

    private Vector3 networkedPosition; // For smooth sync

    void OnEnable()
    {
        // Only local player gets input hooks!
        if (player != null && player.IsLocal && InputManager.Instance.DragDetector != null)
        {
            InputManager.Instance.DragDetector.OnDragStart += HandleDragStart;
            InputManager.Instance.DragDetector.OnDrag += HandleDrag;
            InputManager.Instance.DragDetector.OnDragEnd += HandleDragEnd;
        }
    }

    void OnDisable()
    {
        if (player != null && player.IsLocal && InputManager.Instance.DragDetector != null)
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
            if (player.IsLocal) // Local/owned player
            {
                MoveAlongLine();
            }
#if PHOTON_UNITY_NETWORKING
            else if (IsMultiplayer) // Remote player, interpolate to networkedPosition
            {
                player.transform.position =
                    Vector3.Lerp(player.transform.position, networkedPosition, Time.deltaTime * 10f);
            }
#endif
        }
    }

    // --- Input handlers: Only for local/owned player ---

    private void HandleDragStart(Vector2 pointerPosition)
    {
        if (!player.IsLocal) return;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

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
        if (!player.IsLocal || !isDragging) return;

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
        if (!player.IsLocal) return;

        if (isDragging)
        {
            isDragging = false;
            awaitingFirstSegment = false;
            if (linePoints.Count > 0)
            {
                currentPointIndex = 0;

#if PHOTON_UNITY_NETWORKING
                // Send line to all in multiplayer mode
                if (IsMultiplayer && photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_SetLinePoints), RpcTarget.All, Vector3ArrayToFloatArray(linePoints.ToArray()), linePoints.Count);
                }
#endif
            }
        }
    }

#if PHOTON_UNITY_NETWORKING
    // Deserialize a line sent over network
    [PunRPC]
    void RPC_SetLinePoints(float[] lineData, int count)
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
            data[i * 3] = arr[i].x;
            data[i * 3 + 1] = arr[i].y;
            data[i * 3 + 2] = arr[i].z;
        }
        return data;
    }

    private void MoveAlongLine()
    {
        if (currentPointIndex < linePoints.Count)
        {
            Vector3 targetPosition = linePoints[currentPointIndex];
            float moveSpeed = player.GetMoveSpeed();
            Vector3 newPosition = Vector3.MoveTowards(player.transform.position, targetPosition, moveSpeed);
            player.transform.position = newPosition;

#if PHOTON_UNITY_NETWORKING
            // Network sync current position for other clients only if multiplayer and owner
            if (IsMultiplayer && photonView.IsMine)
            {
                networkedPosition = newPosition;
            }
#endif

            if (Vector3.Distance(player.transform.position, targetPosition) < moveTolerance)
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
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, touchAreaLayer))
        {
            return hit.collider == touchArea;
        }
        return false;
    }

    private bool CanAddPoint(Vector3 newPoint)
    {
        if (linePoints.Count == 0) return true;
        float currentLength = 0f;
        for (int i = 0; i < linePoints.Count - 1; i++)
            currentLength += Vector3.Distance(linePoints[i], linePoints[i + 1]);
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
        return point.x >= boundLeft.bounds.min.x && point.x <= (boundRight.bounds.max.x - rightOffset) &&
               point.z >= (boundBottom.bounds.min.z + bottomOffset) && point.z <= boundTop.bounds.max.z;
    }

    public void ResetLine()
    {
        linePoints.Clear();
        lineRenderer.positionCount = 0;
    }

#if PHOTON_UNITY_NETWORKING
    //--- Sync interpolated position for remote clients
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(player.transform.position);
        }
        else
        {
            networkedPosition = (Vector3)stream.ReceiveNext();
        }
    }
#endif
}
