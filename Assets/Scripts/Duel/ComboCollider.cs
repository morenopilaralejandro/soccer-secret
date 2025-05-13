using UnityEngine;
using System;

public class ComboCollider : MonoBehaviour
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
        Debug.Log("ComboCollider OnTriggerEnter");
        if (!DuelManager.Instance.GetDuelIsResolved() && !GameManager.Instance.IsMovementFrozen && cachedPlayer != DuelManager.Instance.GetLastOff().Player) 
        {
            Debug.Log("ComboCollider OnTriggerEnter 1");
            if (DuelManager.Instance.GetLastDef() != null && DuelManager.Instance.GetLastDef().Player == cachedPlayer)
                return;

            GameObject thisRootObj = cachedPlayer.gameObject;
            GameObject otherRootObj = otherCollider.transform.root.gameObject;


            if (otherRootObj.tag != "Ball")
                return;


            Player lastOff = DuelManager.Instance.GetLastOff().Player;
            int currentIndex = DuelManager.Instance.GetDuelParticipants().Count;

            DuelManager.Instance.RegisterTrigger(thisRootObj);
            ComboCollider.OnSetStatusPlayer?.Invoke(cachedPlayer);




            if (lastOff.IsAlly)
            {
                if (cachedPlayer.IsAi) {
                    //ai block
                    DuelManager.Instance.RegisterUISelections(
                    currentIndex,
                    Category.Block,
                    DuelAction.Defense,
                    DuelCommand.Phys,
                    null);
                } else {
                    //user chain
                    GameManager.Instance.FreezeGame();
                    BallBehavior.Instance.PauseTravel();
                    UIManager.Instance.UserCategory = Category.Shoot;
                    UIManager.Instance.UserIndex = currentIndex;
                    UIManager.Instance.UserPlayer = cachedPlayer;
                    UIManager.Instance.UserAction = DuelAction.Offense;
                    UIManager.Instance.SetButtonDuelToggleVisible(true);
                }
            } else {
                if (cachedPlayer.IsAi) {
                    //ai chain
                    DuelManager.Instance.RegisterUISelections(
                    currentIndex,
                    Category.Shoot,
                    DuelAction.Offense,
                    DuelCommand.Phys,
                    null);
                } else {
                    //user block
                    GameManager.Instance.FreezeGame();
                    BallBehavior.Instance.PauseTravel();
                    UIManager.Instance.UserCategory = Category.Block;
                    UIManager.Instance.UserIndex = currentIndex;
                    UIManager.Instance.UserPlayer = cachedPlayer;
                    UIManager.Instance.UserAction = DuelAction.Defense;
                    UIManager.Instance.SetButtonDuelToggleVisible(true);
                }
            }
        }
    }
}
