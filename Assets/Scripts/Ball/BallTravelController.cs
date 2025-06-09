using System;
using UnityEngine;

/// <summary>
/// Handles ball travel between points and exposes travel events.
/// This can be attached to the Ball GameObject or managed by BallBehavior.
/// </summary>
public class BallTravelController : MonoBehaviour
{
    public static BallTravelController Instance { get; private set; }

    [Header("Travel Settings")]
    [SerializeField] private float travelSpeed = 3f;
    [SerializeField] private float endThreshold = 0.01f;
    [SerializeField] private float maxVelocity = 10f;

    private Rigidbody rb;
    private Vector3 travelVelocity;
    private Vector3 currentTarget;
    private bool isTraveling;
    private bool isPaused;

    // Events
    public event Action<Vector3> OnTravelStart;
    public event Action OnTravelPause;
    public event Action OnTravelResume;
    public event Action OnTravelCancel;
    public event Action<Vector3> OnTravelEnd;

    public bool IsTraveling => isTraveling;
    public bool IsPaused => isPaused;
    public Vector3 CurrentTarget => currentTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody>();
    }


    private void Update()
    {
        if (isTraveling && !isPaused)
        {
            Vector3 prevPos = transform.position;
            float step = travelSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, step);
            travelVelocity = (transform.position - prevPos) / Time.deltaTime;

            if (Vector3.Distance(transform.position, currentTarget) < endThreshold)
            {
                EndTravel();
            }
        }
    }

    public void StartTravel(Vector3 target)
    {
        if (isTraveling) return;

        isTraveling = true;
        isPaused = false;
        currentTarget = target;
        if (rb) rb.isKinematic = true;
        OnTravelStart?.Invoke(target);
    }

    public void PauseTravel()
    {
        if (isTraveling && !isPaused)
        {
            isPaused = true;
            OnTravelPause?.Invoke();
        }
    }

    public void ResumeTravel()
    {
        if (isTraveling && isPaused)
        {
            isPaused = false;
            OnTravelResume?.Invoke();
        }
    }

    public void CancelTravel()
    {
        if (!isTraveling) return;
        isTraveling = false;
        isPaused = false;
        if (rb) rb.isKinematic = false;
        OnTravelCancel?.Invoke();
    }

    private void EndTravel()
    {
        isTraveling = false;
        if (rb)
        {
            if (travelVelocity.magnitude > maxVelocity)
                travelVelocity = travelVelocity.normalized * maxVelocity;
            rb.isKinematic = false;
            rb.velocity = travelVelocity;
        }
        OnTravelEnd?.Invoke(currentTarget);
    }
}
