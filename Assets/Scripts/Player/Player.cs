using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public enum PlayerStats { Hp, Sp, Kick, Body, Control, Guard, Speed, Stamina, Courage }

public class Player : MonoBehaviour
{   
    public string PlayerId => playerId;
    public string PlayerNameEn => playerNameEn;
    public string PlayerNameJa => playerNameJa;
    public Gender Gender => gender;
    public Element Element => element;
    public Position Position => position;
    public bool IsAlly;
    public bool IsAi;
    public bool IsPossession;
    public bool IsStunned => isStunned;
    public bool IsKicking => isKicking;
    public int Lv => lv;
    public Sprite SpritePlayer => spritePlayer;
    public Sprite SpritePortrait => spritePortrait;

    [SerializeField] private string playerId;
    [SerializeField] private string playerNameEn;
    [SerializeField] private string playerNameJa;
    [SerializeField] private int lv;
    [SerializeField] private Gender gender;
    [SerializeField] private Element element;
    [SerializeField] private Position position;
    [SerializeField] private bool isStunned;
    [SerializeField] private bool isKicking;
    [SerializeField] private Sprite spritePlayer;
    [SerializeField] private Sprite spritePortrait;
    [SerializeField] private string pathPlayer = "Player/";
    [SerializeField] private string pathPortrait = "PlayerPortrait/";
    [SerializeField] private const int MAX_LV = 99;
    [SerializeField] private const int MAX_MORE = 50;
    [SerializeField] [Range(0f, 1f)] private float minStatRatio = 0.1f; // Value at level 1 is 10% of max stat
    [SerializeField] private List<Secret> currentSecret;
    [SerializeField] private List<Secret> learnedSecret;
    [SerializeField] private List<SecretLearnEntry> learnSet;
    [SerializeField] private int[] baseStats = new int[9]; // Match PlayerStats enum count
    [SerializeField] private int[] moreStats = new int[9];
    [SerializeField] private int[] currStats = new int[9];
    [SerializeField] private int baseFreedom;
    [SerializeField] private int currFreedom;
    private int maxHp;
    private int maxSp;

    private Collider[] colliders;


    public void Initialize(PlayerData playerData)
    {
        playerId = playerData.playerId;
        playerNameEn = playerData.playerNameEn;
        playerNameJa = playerData.playerNameJa;

        string auxString = playerData.gender;
        Gender auxGender;
        bool isValid = Enum.TryParse(auxString, true, out auxGender); // case-insensitive parse
        if (isValid)
        {
            gender = auxGender;
        } else {
            gender = Gender.M;
        }

        auxString = playerData.element;
        Element auxElement;
        isValid = Enum.TryParse(auxString, true, out auxElement); // case-insensitive parse
        if (isValid)
        {
            element = auxElement;
        } else {
            element = Element.Fire;
        }

        auxString = playerData.position;
        Position auxPosition;
        isValid = Enum.TryParse(auxString, true, out auxPosition); // case-insensitive parse
        if (isValid)
        {
            position = auxPosition;
        } else {
            position = Position.Fw;
        }

        IsAlly = true;
        IsAi = false;
        IsPossession = false;
        isStunned = false;

        lv = 99;

        baseStats[0] = playerData.hp;
        baseStats[1] = playerData.sp;
        baseStats[2] = playerData.kick;
        baseStats[3] = playerData.body;
        baseStats[4] = playerData.control;
        baseStats[5] = playerData.guard;
        baseStats[6] = playerData.speed;
        baseStats[7] = playerData.stamina;
        baseStats[8] = playerData.courage;
        baseFreedom = playerData.freedom;

        int[] learnSetLv = { playerData.lv1, playerData.lv2, playerData.lv3, playerData.lv4 };
        string[] learnSetSecret = { playerData.secret1, playerData.secret2, playerData.secret3, playerData.secret4 };

        for (int i = 0; i < learnSetLv.Length; i++)
        {
            learnSet.Add(new SecretLearnEntry {
                lv = learnSetLv[i],
                secretId = learnSetSecret[i]
            });
        }

        //sprite
        Sprite spriteAux = null;

        spriteAux = Resources.Load<Sprite>(pathPlayer + "player");
        if (spriteAux != null)
        {
            spritePlayer = spriteAux;
        }
        else
        {
            Debug.LogWarning("Sprite not found for portrait: " + playerData.playerId);
        }

        spriteAux = Resources.Load<Sprite>(pathPortrait + playerData.playerId);
        if (spriteAux != null)
        {
            spritePortrait = spriteAux;
        }
        else
        {
            spriteAux = Resources.Load<Sprite>(pathPortrait + "default");
            spritePortrait = spriteAux;
            Debug.LogWarning("Sprite not found for portrait: " + playerData.playerId);
        }

        // Additional initialization logic can go here
        UpdateStats();
        learnedSecret = GetLearnedSecretByLv();
        currentSecret.AddRange(learnedSecret);
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

    public IEnumerator KickCoroutine()
    {
        float duration = 0.3f;
        isKicking = true;
        yield return new WaitForSeconds(duration);
        isKicking = false;
    }

    private void SetYPosition(float yVal)
    {
        Vector3 pos = transform.position;
        pos.y = yVal;
        transform.position = pos;
    }
    
    //Lv
    public void LevelUp()
    {
        if(Lv < MAX_LV)
        {
            lv++;
            UpdateStats();
        }
    }

    private int ScaleStat(int maxStat)
    {
        // Linear down-scaling: at lvl 1 it's minStatRatio, at 99 it's 1.0 of maxStat
        float t = (float)(Lv - 1) / (MAX_LV - 1);
        float statValue = maxStat * Mathf.Lerp(minStatRatio, 1f, t);
        return Mathf.RoundToInt(statValue);
    }

    public int GetStat(PlayerStats stat) => currStats[(int)stat];

    public int GetMaxStat(PlayerStats stat){
        switch(stat) {
            case PlayerStats.Hp:
                return maxHp;
            case PlayerStats.Sp:
                return maxSp;
        }
        return 0;
    }

    public void UpdateStats()
    {
        for (int i = 0; i < baseStats.Length; i++)
            currStats[i] = ScaleStat(baseStats[i]) + moreStats[i];
        maxHp = currStats[(int)PlayerStats.Hp];
        maxSp = currStats[(int)PlayerStats.Sp];
    }

    public bool IsTrainable(PlayerStats stat)
    {
        if (moreStats[(int)stat] >= MAX_MORE)
            return false;

        int totalTrained = 0;
        for (int i = 0; i < moreStats.Length; i++)
            totalTrained += moreStats[i];

        if (currFreedom <= 0 || totalTrained > baseFreedom)
            return false;

        return true;
    }

    public void TrainStat(PlayerStats playerStat, int amount)
    {
        moreStats[(int)playerStat] = Mathf.Clamp(moreStats[(int)playerStat] + amount, 0, MAX_MORE);
        ReduceFreedom(amount);
    }

    public void ReduceHp(int amount)
    {
        currStats[(int)PlayerStats.Hp] -= amount;
        currStats[(int)PlayerStats.Hp] = Mathf.Clamp(currStats[(int)PlayerStats.Hp], 0, maxHp);
    }

    public void ReduceSp(int amount)
    {
        currStats[(int)PlayerStats.Sp] -= amount;
        currStats[(int)PlayerStats.Sp] = Mathf.Clamp(currStats[(int)PlayerStats.Sp], 0, maxHp);
    }

    public void ReduceFreedom(int amount)
    {
        currFreedom -= amount;
        currFreedom = Mathf.Clamp(currFreedom, 0, baseFreedom);
    }

    public void RecoverHp(int amount)
    {
        currStats[(int)PlayerStats.Hp] += amount;
        currStats[(int)PlayerStats.Hp] = Mathf.Clamp(currStats[(int)PlayerStats.Hp], 0, maxHp);
    }

    public void RecoverSp(int amount)
    {
        currStats[(int)PlayerStats.Sp] += amount;
        currStats[(int)PlayerStats.Sp] = Mathf.Clamp(currStats[(int)PlayerStats.Sp], 0, maxHp);
    }

    private void TryLearnSecret()
    {
        foreach (var entry in learnSet)
        {
            if (lv == entry.lv && entry.secretId != null)
            {
                learnedSecret.Add(SecretManager.Instance.GetSecretById(entry.secretId));
            }
        }
    }

    private List<Secret> GetLearnedSecretByLv()
    {
        //when scouted set level (ex 5) then call this method
        List<Secret> secres = new List<Secret>();
        foreach (var entry in learnSet)
        {
            if (lv >= entry.lv && entry.secretId != null)
            {
                secres.Add(SecretManager.Instance.GetSecretById(entry.secretId));
            }
        }
        return secres;
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData
        {
            playerId = this.playerId,
            lv = this.lv,
            moreStats = (int[])this.moreStats.Clone(), // Deep copy
            currFreedom = this.currFreedom,
            currentSecretIds = this.currentSecret?.ConvertAll(sec => sec.SecretId) ?? new List<string>(),
            learnedSecretIds = this.learnedSecret?.ConvertAll(sec => sec.SecretId) ?? new List<string>()
        };
    }

    public void FromSaveData(PlayerSaveData data, PlayerData template)
    {
        Initialize(template);  // Reset everything (or just set the parts that never change)

        this.lv = data.lv;

        Array.Copy(data.moreStats, this.moreStats, Mathf.Min(data.moreStats.Length, this.moreStats.Length));
        this.currFreedom = data.currFreedom;

        this.currentSecret = data.currentSecretIds?
            .ConvertAll(id => SecretManager.Instance.GetSecretById(id)) ?? new List<Secret>();

        this.learnedSecret = data.learnedSecretIds?
            .ConvertAll(id => SecretManager.Instance.GetSecretById(id)) ?? new List<Secret>();
        
        UpdateStats();
    }
}
