using UnityEngine;

public class BallTrail : MonoBehaviour
{
    public static BallTrail Instance { get; private set; }

    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Material material;
    [SerializeField] private string pathMaterial = "Materials/Trail";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (trailRenderer == null)
        {
            Debug.LogError("No TrailRenderer found on this GameObject!");
        }
    }

    public bool IsEmitting()
    {
        if (trailRenderer != null)
            return trailRenderer.emitting;
        return false;
    }

    public void SetTrailVisible(bool visible)
    {
        if (trailRenderer != null)
            trailRenderer.emitting = visible;
    }

    // Call to set the material by resource name (without ".mat")
    public void SetTrailMaterial(Element element)
    {
        if (trailRenderer == null) return;

        material = Resources.Load<Material>(pathMaterial + element);
        if (material == null)
        {
            Debug.LogError("Material '" + element + "' not found in " + pathMaterial + "!");
            return;
        }
        trailRenderer.material = material;
    }
}
