using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/*
    The height of the triangle from vertex0 determines the distance between vertex1 and vertex2.
    If the height is smaller, the base (distance between vertex1 and vertex2) is greater.
*/

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShootTriangle : MonoBehaviour
{
    public static ShootTriangle Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoxCollider boundTop;
    [SerializeField] private BoxCollider boundBottom;

    [Header("Triangle Vertices (XY ignored at runtime)")]
    [SerializeField] private Vector3 vertex0;
    [SerializeField] private Vector3 vertex1;
    [SerializeField] private Vector3 vertex2;

    [Header("Triangle Settings")]
    [SerializeField] private float coordY = 0.02f;

    [Header("Base Length/Range Settings")]
    [SerializeField] private float medianMin = 0.1f;
    [SerializeField] private float medianMax = 2.0f;
    [SerializeField] private float baseLengthAtDefault = 1.2f;
    [SerializeField] private float widenFactorDefault = 1.0f;
    [SerializeField] private float widenFactorMax = 2.0f;
    [SerializeField] private int controlMin = 0;
    [SerializeField] private int controlMax = 130;
    [SerializeField] private float narrowFactorMin = 1.0f;
    [SerializeField] private float narrowFactorMax = 0.3f;

    private Mesh triangleMesh;
    private MeshFilter triangleMeshFilter;
    private MeshRenderer triangleMeshRenderer;

#if PHOTON_UNITY_NETWORKING
    private bool IsMultiplayer => PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined;
    private PhotonView photonView => PhotonView.Get(this);
#else
    private bool IsMultiplayer => false;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        triangleMeshFilter = GetComponent<MeshFilter>();
        triangleMeshRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        triangleMesh = new Mesh();
        triangleMeshFilter.mesh = triangleMesh;
        UpdateMesh();
        GameLogger.Info("[ShootTriangle] Mesh initialized and mesh assigned.", this);
    }

    private void UpdateMesh()
    {
        vertex0.y = coordY;
        vertex1.y = coordY;
        vertex2.y = coordY;
        Vector3[] vertices = { vertex0, vertex1, vertex2 };
        int[] triangles = { 0, 1, 2 };

        triangleMesh.Clear();
        triangleMesh.vertices = vertices;
        triangleMesh.triangles = triangles;
        triangleMesh.RecalculateNormals();
        triangleMesh.RecalculateBounds();
        GameLogger.DebugLog("[ShootTriangle] Mesh updated with current vertices.", this);
    }

    public void SetTriangleVisible(bool visible)
    {
        triangleMeshRenderer.enabled = visible;
        GameLogger.Info($"[ShootTriangle] Triangle visibility set to {visible}.", this);
    }

    /// <summary>
    /// Should only be called by the master client in multiplayer, or anyone in single-player.
    /// </summary>
    public void SetTriangleFromTap(Player player, Vector2 touchPosition)
    {
        GameLogger.Info($"[ShootTriangle] SetTriangleFromTap called. Touch position: {touchPosition}", this);

        // Only calculate triangle if we are master (multiplayer) or local/offline.
        if (IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            && !PhotonNetwork.IsMasterClient
#endif
        )
            return;

        vertex0 = player.transform.position;
        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        Plane groundPlane = new Plane(Vector3.up, vertex0);
        Vector3 targetWorldPosition;
        if (groundPlane.Raycast(ray, out float distance))
        {
            targetWorldPosition = ray.GetPoint(distance);
        }
        else
        {
            targetWorldPosition = vertex0 + ray.direction * 2f; // fallback
            GameLogger.Warning("[ShootTriangle] Could not raycast touch to ground plane. Using fallback target.", this);
        }

        SetTriangleFromPlayer(player, targetWorldPosition);
    }

    public void SetTriangleFromPlayer(Player player, Vector3 worldCoord)
    {
        if (IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            && !PhotonNetwork.IsMasterClient
#endif
        )
            return;

        // --- The actual triangle math ---
        vertex0 = player.transform.position;
        Vector3 dirToTarget = (worldCoord - vertex0).normalized;
        Vector3 perpendicular = Vector3.Cross(dirToTarget, Vector3.up).normalized;

        vertex1 = worldCoord + perpendicular;
        vertex2 = worldCoord - perpendicular;

        float borderZ = (worldCoord.z >= 0f) ? boundTop.bounds.min.z : boundBottom.bounds.max.z;
        vertex1.z = borderZ;
        vertex2.z = borderZ;

        GameLogger.DebugLog("[ShootTriangle] Base triangle set with player and world positions.", this);

        AdjustBaseLengthByMedian(player);

        // Multiplay: Sync to all players via RPC!
        if (IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            && PhotonNetwork.IsMasterClient
#endif
        )
        {
#if PHOTON_UNITY_NETWORKING
            photonView.RPC(nameof(RPC_SyncTriangle), RpcTarget.Others, vertex0, vertex1, vertex2);
#endif
        }

        UpdateMesh();
        GameLogger.Info("[ShootTriangle] Triangle set and mesh updated.", this);
    }

    /// <summary>
    /// Adjusts the base width of the triangle based on how far the tap is from the player's center (median)
    /// and the player's control stat (both serialized for inspector tuning).
    /// </summary>
    private void AdjustBaseLengthByMedian(Player player)
    {
        float baseMedianX = (vertex1.x + vertex2.x) * 0.5f;
        float median = Mathf.Abs(vertex0.x - baseMedianX);
        GameLogger.Info($"[ShootTriangle] Median distance for base adjustment: {median}", this);

        float t = Mathf.InverseLerp(medianMin, medianMax, median);
        float widenFactor = Mathf.Lerp(widenFactorDefault, widenFactorMax, t);

        float playerControl = player.GetStat(PlayerStats.Control);
        float controlT = Mathf.InverseLerp(controlMin, controlMax, playerControl);
        float narrowFactor = Mathf.Lerp(narrowFactorMin, narrowFactorMax, controlT);

        // Apply both control and widen factors:
        float baseAdjustment = baseLengthAtDefault * widenFactor * narrowFactor;
        float halfBase = baseAdjustment * 0.5f;

        // Re-calculate the "base" midpoint and set vertices symmetrically
        float baseZ = (vertex1.z + vertex2.z) * 0.5f;
        float baseY = (vertex1.y + vertex2.y) * 0.5f;
        float baseMidX = baseMedianX;
        Vector3 baseMid = new Vector3(baseMidX, baseY, baseZ);

        vertex1 = baseMid - Vector3.right * halfBase;
        vertex2 = baseMid + Vector3.right * halfBase;

        GameLogger.Info($"[ShootTriangle] Base adjusted: width={baseAdjustment}, factors (widen={widenFactor}, narrow={narrowFactor}), " +
            $"vertices: v1={vertex1}, v2={vertex2}", this);
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_SyncTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        vertex0 = v0;
        vertex1 = v1;
        vertex2 = v2;
        UpdateMesh();
        GameLogger.Info("[ShootTriangle] Triangle synced from RPC.", this);
    }
#endif

    public Vector3 GetRandomPoint()
    {
        float t = Random.Range(0f, 1f);
        Vector3 randomPoint = Vector3.Lerp(vertex1, vertex2, t);
        GameLogger.DebugLog($"[ShootTriangle] Generated random point on base: {randomPoint}", this);
        return randomPoint;
    }

    private void OnValidate()
    {
        if (triangleMesh == null)
            return;
        UpdateMesh();
        GameLogger.DebugLog("[ShootTriangle] OnValidate called; mesh updated.", this);
    }
}
