using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShootTriangle : MonoBehaviour
{
    public static ShootTriangle Instance { get; private set; }
    [Header("Reference")]
    [SerializeField] private Camera mainCamera;

    [Header("Triangle Vertices (XY ignored at runtime)")]
    [SerializeField] private Vector3 vertex0;
    [SerializeField] private Vector3 vertex1;
    [SerializeField] private Vector3 vertex2;

    [Header("Triangle Settings")]
    [SerializeField] private float coordY = 0.02f;
    [SerializeField] private float baseOffsetMin = 0.15f;
    [SerializeField] private float controlFactor = 0.05f; 
    [SerializeField] private float rangeMin = 1f;
    [SerializeField] private float rangeMax = 2.5f; 

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;


    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cache components
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

    public void SetTriangleFromPlayer(Player player, Vector2 touchPosition)
    {
        // 1. Player's world position
        vertex0 = player.transform.position;

        // 2. Find the world position touched/clicked on the near plane,
        // but we want the triangle to be at the player's y/z, so use raycasting from screen point

        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        // We'll pick a plane at the player's y coordinate
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

        // 3. Find direction from player to touched world point
        Vector3 dir = (touchWorld - vertex0).normalized;

        // 4. Compute perpendicular (on XZ plane)
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

        // 5. Set vertex1 and vertex2, offsetting them perpendicular
        float control = player.GetStat(PlayerStats.Control) * controlFactor;
        float randomValue1 = Random.Range(rangeMin, rangeMax);
        float offsetAmount1 = Mathf.Max(baseOffsetMin, randomValue1 - control);
        vertex1 = touchWorld + perp * offsetAmount1;

        float randomValue2 = Random.Range(rangeMin, rangeMax);
        float offsetAmount2 = Mathf.Max(baseOffsetMin, randomValue2 - control);
        vertex2 = touchWorld - perp * offsetAmount2;

        vertex1.z = touchWorld.z;
        vertex2.z = touchWorld.z;

        UpdateMesh();
    }

    public Vector3 GetRandomPoint()
    {
        float t = Random.Range(0f, 1f);
        return Vector3.Lerp(vertex1, vertex2, t);
    }

    // For live editor update
    private void OnValidate()
    {
        if (mesh == null)
            return;
        UpdateMesh();
    }
}
