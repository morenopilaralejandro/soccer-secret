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
            GameLogger.Warning("[BallTravelController] Duplicate instance found, destroying.", this);
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody>();
        GameLogger.Info("[BallTravelController] Instance initialized.", this);
    }

    private void Update()
    {
        if (isTraveling && !isPaused)
        {
            Vector3 prevPos = transform.position;
            float step = travelSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, step);
            travelVelocity = (transform.position - prevPos) / Time.deltaTime;
            /*
            GameLogger.DebugLog($"[BallTravelController] Moving towards {currentTarget}. " +
                                $"Current: {transform.position}, Step: {step}, Velocity: {travelVelocity.magnitude}", this);
            */
            if (Vector3.Distance(transform.position, currentTarget) < endThreshold)
            {
                GameLogger.Info("[BallTravelController] Arrived at target; ending travel.", this);
                EndTravel();
            }
        }
    }

    public void StartTravel(Vector3 target)
    {
        if (isTraveling)
        {
            GameLogger.Warning("[BallTravelController] Already traveling, cannot start new travel.", this);
            return;
        }

        isTraveling = true;
        isPaused = false;
        currentTarget = target;
        if (rb) rb.isKinematic = true;
        GameLogger.Info($"[BallTravelController] Travel started to {target}.", this);
        OnTravelStart?.Invoke(target);
    }

    public void PauseTravel()
    {
        if (isTraveling && !isPaused)
        {
            isPaused = true;
            GameLogger.Info("[BallTravelController] Travel paused.", this);
            OnTravelPause?.Invoke();
        }
        else
        {
            GameLogger.DebugLog("[BallTravelController] Cannot pause: either not traveling or already paused.", this);
        }
    }

    public void ResumeTravel()
    {
        if (isTraveling && isPaused)
        {
            isPaused = false;
            GameLogger.Info("[BallTravelController] Travel resumed.", this);
            OnTravelResume?.Invoke();
        }
        else
        {
            GameLogger.DebugLog("[BallTravelController] Cannot resume: either not traveling or not paused.", this);
        }
    }

    public void CancelTravel()
    {
        if (!isTraveling)
        {
            GameLogger.DebugLog("[BallTravelController] CancelTravel called but not traveling.", this);
            return;
        }
        isTraveling = false;
        isPaused = false;
        if (rb) rb.isKinematic = false;
        GameLogger.Info($"[BallTravelController] Travel cancelled at position {transform.position}.", this);
        OnTravelCancel?.Invoke();
    }

    private void EndTravel()
    {
        isTraveling = false;
        if (rb)
        {
            if (travelVelocity.magnitude > maxVelocity)
            {
                travelVelocity = travelVelocity.normalized * maxVelocity;
                GameLogger.DebugLog("[BallTravelController] Travel velocity clamped to max.", this);
            }
            rb.isKinematic = false;
            rb.velocity = travelVelocity;
        }
        GameLogger.Info($"[BallTravelController] Travel ended at {currentTarget} with velocity {travelVelocity}.", this);
        OnTravelEnd?.Invoke(currentTarget);
    }
}
