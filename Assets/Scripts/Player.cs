using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public string id;
    public string playerName;
    public string gender;
    public string type;
    public string position;
    public bool isAlly;
    public bool isStunned;
    public int hpMax;
    public int hp;
    public int spMax;
    public int sp;
    public int kick;
    public int body;
    public int control;
    public int guard;
    public int speed;
    public int stamina;
    public int courage;
    public int freedom;

    private Collider[] colliders;

    public void Initialize(PlayerData playerData)
    {
        id = playerData.id;
        playerName = playerData.playerName;
        gender = playerData.gender;
        type = playerData.type;
        position = playerData.position;
        isAlly = true;
        isStunned = false;
        hpMax = playerData.hp;
        hp = playerData.hp;
        spMax = playerData.sp;
        sp = playerData.sp;
        kick = playerData.kick;
        body = playerData.body;
        control = playerData.control;
        guard = playerData.guard;
        speed = playerData.speed;
        stamina = playerData.stamina;
        courage = playerData.courage;
        freedom = playerData.freedom;

        // Additional initialization logic can go here
    }

    void Awake()
    {
        // Cache all colliders on self and children
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public IEnumerator Stun(float duration)
    {
        /*
            if (isStunned)
            {
                // Optionally, you could play a stun animation or effects here
                return; // Don't process movement
            }

            player.StartCoroutine(player.Stun(3f));
        */

        isStunned = true;
        SetAllCollidersEnabled(false);
        StartCoroutine(BlinkEffect(duration));
        yield return new WaitForSeconds(duration);
        SetAllCollidersEnabled(true);
        isStunned = false;
        // Ensure player is visible at end
        SetYPosition(0);
    }

    private void SetAllCollidersEnabled(bool enabled)
    {
        foreach (var col in colliders)
            col.enabled = enabled;
    }

    private IEnumerator BlinkEffect(float duration)
    {
        float elapsed = 0f;
        float blinkInterval = 0.2f; // seconds between blinks
        bool visible = true;

        while (elapsed < duration)
        {
            SetYPosition(visible ? 0f : -1f);
            visible = !visible;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
        // Always end visible
        SetYPosition(0f);
    }

    private void SetYPosition(float yVal)
    {
        Vector3 pos = transform.position;
        pos.y = yVal;
        transform.position = pos;
    }

}
