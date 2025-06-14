using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShootTriangle : MonoBehaviour
{
    public static ShootTriangle Instance { get; private set; }
    [Header("Reference")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoxCollider boundTop;
    [SerializeField] private BoxCollider boundBottom;

    [Header("Triangle Vertices (XY ignored at runtime)")]
    [SerializeField] private Vector3 vertex0;
    [SerializeField] private Vector3 vertex1;
    [SerializeField] private Vector3 vertex2;

    [Header("Triangle Settings")]
    [SerializeField] private float coordY = 0.02f;
    [SerializeField] private float baseOffsetMin = 0.5f;
    [SerializeField] private float controlFactor = 0.05f; 
    [SerializeField] private float rangeMin = 1f;
    [SerializeField] private float rangeMax = 2.5f; 

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

#if PHOTON_UNITY_NETWORKING
    private bool IsMultiplayer => PhotonNetwork.InRoom && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joined;
    private PhotonView photonView => PhotonView.Get(this);
#else
    private bool IsMultiplayer => false;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        vertex0.y = coordY;
        vertex1.y = coordY;
        vertex2.y = coordY;
        Vector3[] vertices = { vertex0, vertex1, vertex2 };
        int[] triangles = { 0, 1, 2 };

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void SetTriangleVisible(bool visible)
    {
        meshRenderer.enabled = visible;
    }

    /// <summary>
    /// This should be called only by the master client in multiplayer, or anyone in single-player.
    /// </summary>
    public void SetTriangleFromUser(Player player, Vector2 touchPosition)
    {
        Debug.Log("ShootTriangle touchPosition: " + touchPosition);

        // Only calculate triangle if we are master (multiplayer) or local/offline.
        if (IsMultiplayer
#if PHOTON_UNITY_NETWORKING
            && !PhotonNetwork.IsMasterClient
#endif
        )
            return;

        vertex0 = player.transform.position;
        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        Plane plane = new Plane(Vector3.up, vertex0);
        float distance;
        Vector3 touchWorld;
        if (plane.Raycast(ray, out distance))
        {
            touchWorld = ray.GetPoint(distance);
        }
        else
        {
            touchWorld = vertex0 + ray.direction * 2f; // fallback
        }

        SetTriangleFromPlayer(player, touchWorld);
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
        Vector3 dir = (worldCoord - vertex0).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

        float control = player.GetStat(PlayerStats.Control) * controlFactor;
        float randomValue1 = Random.Range(rangeMin, rangeMax);
        float offsetAmount1 = Mathf.Max(baseOffsetMin, randomValue1 - control);
        vertex1 = worldCoord + perp * offsetAmount1;

        float randomValue2 = Random.Range(rangeMin, rangeMax);
        float offsetAmount2 = Mathf.Max(baseOffsetMin, randomValue2 - control);
        vertex2 = worldCoord - perp * offsetAmount2;

        float borderZ = (worldCoord.z >= 0f) ? boundTop.bounds.min.z : boundBottom.bounds.max.z;
        vertex1.z = borderZ;
        vertex2.z = borderZ;

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
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_SyncTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        vertex0 = v0;
        vertex1 = v1;
        vertex2 = v2;
        UpdateMesh();
    }
#endif

    public Vector3 GetRandomPoint()
    {
        // In multiplayer, only the master chooses, then syncs that coordinate
        float t = Random.Range(0f, 1f);
        return Vector3.Lerp(vertex1, vertex2, t);
    }

    private void OnValidate()
    {
        if (mesh == null)
            return;
        UpdateMesh();
    }
}
