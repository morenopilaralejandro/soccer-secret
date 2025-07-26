public class PendingSwipeHandler
{
    private bool hasPendingSwipeUp = false;

    public bool HasPendingSwipeUp => hasPendingSwipeUp;

    public void QueuePendingSwipeUp()
    {
        hasPendingSwipeUp = true;
    }

    public bool TryConsumePendingSwipeUp()
    {
        if (hasPendingSwipeUp)
        {
            hasPendingSwipeUp = false;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        hasPendingSwipeUp = false;
    }
}
