using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public bool IsDragging { get; set; }
    public DragDetector DragDetector => dragDetector;
    public TapDetector TapDetector => tapDetector;
    public SwipeDetector SwipeDetector => swipeDetector;

    [Header("Detectors on this GameObject")]
    [SerializeField] private DragDetector dragDetector;
    [SerializeField] private TapDetector tapDetector;
    [SerializeField] private SwipeDetector swipeDetector;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Optional: Don't destroy on load (if you want persistent input)
        // DontDestroyOnLoad(gameObject);

        // Auto-assign components if not set in Inspector
        if (dragDetector == null) dragDetector = GetComponent<DragDetector>();
        if (tapDetector == null) tapDetector = GetComponent<TapDetector>();
        if (swipeDetector == null) swipeDetector = GetComponent<SwipeDetector>();
    }
}
