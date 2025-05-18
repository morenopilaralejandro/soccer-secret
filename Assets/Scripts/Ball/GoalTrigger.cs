using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public Team Team;

    private void OnTriggerEnter(Collider other)
    {
        // Make sure this script is on the goal, and the ball has a Collider & Rigidbody!
        if (other.CompareTag("Ball") && !GameManager.Instance.IsTimeFrozen)
        {
            GameManager.Instance.OnGoalScored(Team);
        }
    }
}
