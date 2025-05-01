using UnityEngine;
using System.Collections;
using System;

public class Player : MonoBehaviour
{    
    public string id;
    public string playerName;

    public string gender;
    public Type type;
    public string position;

    public bool isAlly;
    public bool isAi;
    public bool isStunned;

    public int lv;
    public const int maxLv = 99;
    [Range(0f, 1f)] public float minStatRatio = 0.1f; // Value at level 1 is 10% of max stat

    public int baseHp;
    public int baseSp;
    public int baseKick;
    public int baseBody;
    public int baseControl;
    public int baseGuard;
    public int baseSpeed;
    public int baseStamina;
    public int baseCourage;
    public int baseFreedom;

    public int maxHp;
    public int maxSp;

    public int currHp;
    public int currSp;
    public int currKick;
    public int currBody;
    public int currControl;
    public int currGuard;
    public int currSpeed;
    public int currStamina;
    public int currCourage;
    public int currFreedom;

    private Collider[] colliders;

    public void Initialize(PlayerData playerData)
    {
        id = playerData.id;
        playerName = playerData.playerName;
        gender = playerData.gender;

        string typeString = playerData.type;
        Type auxType;
        bool isValidType = Enum.TryParse(typeString, true, out auxType); // case-insensitive parse
        if (isValidType)
        {
            type = auxType;
        } else {
            type = Type.Fire;
        }

        position = playerData.position;
        isAlly = true;
        isAi = false;
        isStunned = false;

        lv = 99;

        baseHp = playerData.hp;
        baseSp = playerData.sp;
        baseKick = playerData.kick;
        baseBody = playerData.body;
        baseControl = playerData.control;
        baseGuard = playerData.guard;
        baseSpeed = playerData.speed;
        baseStamina = playerData.stamina;
        baseCourage = playerData.courage;
        baseFreedom = playerData.freedom;

        // Additional initialization logic can go here
        UpdateStats();
    }

    void Awake()
    {
        // Cache all colliders on self and children
        colliders = GetComponentsInChildren<Collider>(true);
    }
    //stun
    public IEnumerator Stun()
    {
        /*
            if (isStunned)
            {
                // Optionally, you could play a stun animation or effects here
                return; // Don't process movement
            }

            player.StartCoroutine(player.Stun());
        */
        float duration = 3f;
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
    
    //lv
    public void LevelUp()
    {
        if(lv < maxLv)
        {
            lv++;
            UpdateStats();
        }
    }

    private int ScaleStat(int maxStat)
    {
        // Linear down-scaling: at lvl 1 it's minStatRatio, at 99 it's 1.0 of maxStat
        float t = (float)(lv - 1) / (maxLv - 1);
        float statValue = maxStat * Mathf.Lerp(minStatRatio, 1f, t);
        return Mathf.RoundToInt(statValue);
    }

    public void UpdateStats()
    {
        maxHp = ScaleStat(baseHp);
        maxSp = ScaleStat(baseSp);
        
        currHp = maxHp;
        currSp = maxSp;

        currKick = ScaleStat(baseKick);
        currBody = ScaleStat(baseBody);
        currControl = ScaleStat(baseControl);
        currGuard = ScaleStat(baseGuard);
        currSpeed = ScaleStat(baseSpeed);
        currStamina = ScaleStat(baseStamina);
        currCourage = ScaleStat(baseCourage);
    }

    public void reduceHp(int amount)
    {
        currHp -= amount;
        currHp = Mathf.Clamp(currHp, 0, maxHp);
    }

    public void reduceSp(int amount)
    {
        currSp -= amount;
        currSp = Mathf.Clamp(currSp, 0, maxSp);
    }

    public void reduceFreedom(int amount)
    {
        currFreedom -= amount;
        currFreedom = Mathf.Clamp(currFreedom, 0, baseFreedom);
    }

    public void Heal(int amount)
    {
        currHp += amount;
        currHp = Mathf.Clamp(currHp, 0, maxHp);
    }

}
