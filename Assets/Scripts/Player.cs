using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class Player : MonoBehaviour
{    
    public string id;
    public string playerName;

    public string gndr;
    public Type type;
    public Posi posi;

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

    public int maxMore = 50;
    public int moreHp;
    public int moreSp;
    public int moreKick;
    public int moreBody;
    public int moreControl;
    public int moreGuard;
    public int moreSpeed;
    public int moreStamina;
    public int moreCourage;

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

    public Sprite spritePlayer;
    public Sprite spritePortrait;

    private Collider[] colliders;

    public void Initialize(PlayerData playerData)
    {
        id = playerData.id;
        playerName = playerData.playerName;
        gndr = playerData.gndr;

        string auxString = playerData.type;
        Type auxType;
        bool isValid = Enum.TryParse(auxString, true, out auxType); // case-insensitive parse
        if (isValid)
        {
            type = auxType;
        } else {
            type = Type.Fire;
        }

        auxString = playerData.posi;
        Posi auxPosi;
        isValid = Enum.TryParse(auxString, true, out auxPosi); // case-insensitive parse
        if (isValid)
        {
            posi = auxPosi;
        } else {
            posi = Posi.Fw;
        }

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

        //sprite
        Sprite spriteAux = null;

        spriteAux = Resources.Load<Sprite>("Player/" + "player");
        if (spriteAux != null)
        {
            spritePlayer = spriteAux;
        }
        else
        {
            Debug.LogWarning("Sprite not found for portrait: " + playerData.playerName);
        }

        spriteAux = Resources.Load<Sprite>("Portrait/" + "portrait");
        if (spriteAux != null)
        {
            spritePortrait = spriteAux;
        }
        else
        {
            Debug.LogWarning("Sprite not found for portrait: " + playerData.playerName);
        }

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
        maxHp = ScaleStat(baseHp) + moreHp;
        maxSp = ScaleStat(baseSp) + moreSp;
        
        currHp = maxHp;
        currSp = maxSp;

        currKick = ScaleStat(baseKick) + moreKick;
        currBody = ScaleStat(baseBody) + moreBody;
        currControl = ScaleStat(baseControl) + moreControl;
        currGuard = ScaleStat(baseGuard) + moreGuard;
        currSpeed = ScaleStat(baseSpeed) + moreSpeed;
        currStamina = ScaleStat(baseStamina) + moreStamina;
        currCourage = ScaleStat(baseCourage) + moreCourage;
    }

    public bool isTrainable(int statIndex)
    {
        switch (statIndex)
        {
            case 0:
                if (moreHp >= maxMore)
                    return false;
                break;
            case 1:
                if (moreSp >= maxMore)
                    return false;
                break;
            case 2:
                if (moreKick >= maxMore)
                    return false;
                break;
            case 3:
                if (moreBody >= maxMore)
                    return false;
                break;
            case 4:
                if (moreControl >= maxMore)
                    return false;
                break;
            case 5:
                if (moreGuard >= maxMore)
                    return false;
                break;
            case 6:
                if (moreSpeed >= maxMore)
                    return false;
                break;
            case 7:
                if (moreStamina >= maxMore)
                    return false;
                break;
            case 8:
                if (moreCourage >= maxMore)
                    return false;
                break;
            default:
                Debug.LogWarning("Unknown statIndex: " + statIndex);
                break;
        }

        if (currFreedom <= 0 || moreKick + moreBody + moreControl + moreGuard
            + moreSpeed + moreStamina + moreCourage > baseFreedom) 
        {
            return false;
        } else {
            return true;
        }
    }

    public void TrainStat(int statIndex, int amount)
    {
        switch (statIndex)
        {
            case 0:
                moreHp += amount;
                moreHp = Mathf.Clamp(moreHp, 0, maxMore);
                break;
            case 1:
                moreSp += amount;
                moreSp = Mathf.Clamp(moreSp, 0, maxMore);
                break;
            case 2:
                moreKick += amount;
                moreKick = Mathf.Clamp(moreKick, 0, maxMore);
                break;
            case 3:
                moreBody += amount;
                moreBody = Mathf.Clamp(moreBody, 0, maxMore);
                break;
            case 4:
                moreControl += amount;
                moreControl = Mathf.Clamp(moreControl, 0, maxMore);
                break;
            case 5:
                moreGuard += amount;
                moreGuard = Mathf.Clamp(moreGuard, 0, maxMore);
                break;
            case 6:
                moreSpeed += amount;
                moreSpeed = Mathf.Clamp(moreSpeed, 0, maxMore);
                break;
            case 7:
                moreStamina += amount;
                moreStamina = Mathf.Clamp(moreStamina, 0, maxMore);
                break;
            case 8:
                moreCourage += amount;
                moreCourage = Mathf.Clamp(moreCourage, 0, maxMore);
                break;
            default:
                Debug.LogWarning("Unknown statIndex: " + statIndex);
                break;
        }
        ReduceFreedom(amount);
    }

    public void ReduceHp(int amount)
    {
        currHp -= amount;
        currHp = Mathf.Clamp(currHp, 0, maxHp);
    }

    public void ReduceSp(int amount)
    {
        currSp -= amount;
        currSp = Mathf.Clamp(currSp, 0, maxSp);
    }

    public void ReduceFreedom(int amount)
    {
        currFreedom -= amount;
        currFreedom = Mathf.Clamp(currFreedom, 0, baseFreedom);
    }

    public void RecoverHp(int amount)
    {
        currHp += amount;
        currHp = Mathf.Clamp(currHp, 0, maxHp);
    }

    public void RecoverSp(int amount)
    {
        currSp += amount;
        currSp = Mathf.Clamp(currSp, 0, maxSp);
    }

}
