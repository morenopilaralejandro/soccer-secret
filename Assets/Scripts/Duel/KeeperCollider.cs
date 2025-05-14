using UnityEngine;
using System;

public class KeeperCollider : MonoBehaviour
{
    private Player cachedPlayer;

    public static event Action<Player> OnSetStatusPlayer;

    void Awake()
    {
        cachedPlayer = GetComponentInParent<Player>();
    }

    void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider otherCollider)
    {
        Debug.Log("KeeperCollider OnTriggerEnter");
            GameObject thisRootObj = cachedPlayer.gameObject;
            GameObject otherRootObj = otherCollider.transform.root.gameObject;

            if (otherRootObj.tag != "Ball")
                return;

        if (!DuelManager.Instance.GetDuelIsResolved() && !GameManager.Instance.IsMovementFrozen) 
        {
            //Duel Catch
            if (DuelManager.Instance.GetLastDef() != null && DuelManager.Instance.GetLastDef().Player == cachedPlayer)
                return;


            Player lastOff = DuelManager.Instance.GetLastOff().Player;
            int currentIndex = DuelManager.Instance.GetDuelParticipants().Count;

            DuelManager.Instance.RegisterTrigger(thisRootObj);
            KeeperCollider.OnSetStatusPlayer?.Invoke(cachedPlayer);


            if (lastOff.IsAi)
            {
                //user catch
                    GameManager.Instance.FreezeGame();
                    BallBehavior.Instance.PauseTravel();
                    UIManager.Instance.UserCategory = Category.Catch;
                    UIManager.Instance.UserIndex = currentIndex;
                    UIManager.Instance.UserPlayer = cachedPlayer;
                    UIManager.Instance.UserAction = DuelAction.Defense;
                    UIManager.Instance.SetButtonDuelToggleVisible(true);

            } else {
                //ai catch
                    DuelManager.Instance.RegisterUISelections(
                    currentIndex,
                    Category.Catch,
                    DuelAction.Defense,
                    DuelCommand.Phys,
                    null);
            }
        }
    }
}
