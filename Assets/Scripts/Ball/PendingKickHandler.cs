using UnityEngine;

public class PendingKickHandler
{
    private Vector2? pendingKick;

    public bool HasPendingKick => pendingKick.HasValue;
    public Vector2? PendingKickPosition => pendingKick;

    public void QueuePendingKick(Vector2 pos)
        => pendingKick = pos;

    public void Clear() => pendingKick = null;

    /// <summary>
    /// If there's a pending kick, consume it and return true.
    /// </summary>
    public bool TryConsumePendingKick(out Vector2 pos)
    {
        if (pendingKick.HasValue)
        {
            pos = pendingKick.Value;
            pendingKick = null;
            return true;
        }
        pos = default;
        return false;
    }
}
