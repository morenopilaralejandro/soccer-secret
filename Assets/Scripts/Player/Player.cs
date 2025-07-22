using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public enum PlayerStats { Hp, Sp, Kick, Body, Control, Guard, Speed, Stamina, Courage }

public enum ControlType { LocalHuman, RemoteHuman, Ai }

public enum PlayerSize { S, M, L, Xl }

public class Player : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunInstantiateMagicCallback
#endif
{   
    public string PlayerId => playerId;
    public string PlayerName => playerName;
    public PlayerSize PlayerSize => playerSize;
    public Gender Gender => gender;
    public Element Element => element;
    public Position Position => position;
    public int TeamIndex;  
    public Vector3 DefaultPosition;
    public ControlType ControlType;
    public bool IsPossession;
    public bool IsKeeper;
    public bool IsStunned => isStunned;
    public bool IsKicking => isKicking;
    public bool IsControlling => isControlling;
    public int Lv;
    public List<Secret> CurrentSecret => currentSecret;
    public List<Secret> LearnedSecret => learnedSecret;
    public Sprite SpritePlayerPortrait => spritePlayerPortrait;
    public Sprite SpriteWearPortrait => spriteWearPortrait;

    [SerializeField] private PlayerNameTag playerNameTag;
    [SerializeField] private SpriteRenderer spriteRendererPigment;
    [SerializeField] private SpriteRenderer spriteRendererHair;
    [SerializeField] private SpriteRenderer spriteRendererAccessory;
    [SerializeField] private SpriteRenderer spriteRendererWear;
    [SerializeField] private Sprite spritePlayerPortrait;
    [SerializeField] private Sprite spriteWearPortrait;
    [SerializeField] private string pathHairStyle = "HairStyle/";
    [SerializeField] private string pathAccessoryStyle = "AccessoryStyle/";
    [SerializeField] private string tableCollectionName = "PlayerNames";
 
    [SerializeField] private string playerId;
    [SerializeField] private string playerName;
    [SerializeField] private PlayerSize playerSize;
    [SerializeField] private Gender gender;
    [SerializeField] private Element element;
    [SerializeField] private Position position;
    [SerializeField] private bool isStunned;
    [SerializeField] private bool isKicking;
    [SerializeField] private bool isControlling;
    [SerializeField] private const int MAX_LV = 99;
    [SerializeField] private const int MAX_MORE = 50;
    [SerializeField] [Range(0f, 1f)] private float minStatRatioHp = 0.4f; // 40% of base at level 1
    [SerializeField] [Range(0f, 1f)] private float minStatRatioSp = 0.4f; // 40% of base at level 1
    [SerializeField] [Range(0f, 1f)] private float minStatRatioOther = 0.1f; // 10% of base at level 1
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
    private Collider keeperCollider;
    private Coroutine stunRoutine;
    private Coroutine blinkRoutine;

    [Header("Movement Parameters")]
    [SerializeField] private float speedBase = 0.2f;
    [SerializeField] private float speedMultiplierUser = 0.01f;
    [SerializeField] private float speedMultiplierAi = 0.005f;
    [SerializeField] private float speedDebuffDefault = 1f;
    [SerializeField] private float speedDebuffLow = 0.5f;
    [SerializeField] private float speedDebuffHigh = 0.2f;
    [SerializeField] private int hpThresholdLow = 10;
    [SerializeField] private int hpThresholdHigh = 30;

    // Remember the last calculated speedDebuff for reuse
    private float _lastSpeedDebuff = 1f;
    private int _lastHpChecked = int.MinValue;

    // Event
    public event Action OnPlayerNameChanged;

    private void Start()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale obj)
    {
        // Update the text whenever the language changes
        SetName();
    }

    private void OnDestroy()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void Initialize(PlayerData playerData)
    {
        playerId = playerData.playerId;

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

        auxString = playerData.playerSize;
        PlayerSize auxPlayerSize;
        isValid = Enum.TryParse(auxString, true, out auxPlayerSize); // case-insensitive parse
        if (isValid)
        {
            playerSize = auxPlayerSize;
        } else {
            playerSize = PlayerSize.S;
        }

        IsPossession = false;
        isStunned = false;
        IsKeeper = false;

        Lv = 99;

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

        int[] learnSetLv = { playerData.lv0, playerData.lv1, playerData.lv2, playerData.lv3 };
        string[] learnSetSecret = { playerData.secret0, playerData.secret1, playerData.secret2, playerData.secret3 };

        for (int i = 0; i < learnSetLv.Length; i++)
        {
            learnSet.Add(new SecretLearnEntry {
                lv = learnSetLv[i],
                secretId = learnSetSecret[i]
            });
        }

        //sprite
        Sprite spriteAux = null;

        if (spriteRendererPigment != null)
        {
            if (playerData.pigment != null)
            {
                spriteRendererPigment.color = ColorManager.GetPigmentColor(playerData.pigment);
            }
            else
            {
                Debug.LogWarning($"Pigment color not found: {playerData.pigment} for player {playerData.playerId}");
            }
        }
        else
        {
            Debug.LogWarning("SpriteRendererPigment reference is missing!");
        }

        spriteAux = Resources.Load<Sprite>(pathHairStyle + playerData.hairStyle);
        if (spriteRendererHair != null)
        {
            if (spriteAux != null)
            {
                spriteRendererHair.sprite = spriteAux;
                spriteRendererHair.color = ColorManager.GetHairColor(playerData.hairColor);
            }
            else
            {
                Debug.LogWarning($"Hair sprite not found: {pathHairStyle}{playerData.hairStyle} for player {playerData.playerId}");
            }
        }
        else
        {
            Debug.LogWarning("SpriteRendererHair reference is missing!");
        }

        spriteAux = Resources.Load<Sprite>(pathAccessoryStyle + playerData.accessoryStyle);
        if (spriteRendererAccessory != null)
        {
            if (spriteAux != null && playerData.accessoryStyle != "none")
            {
                spriteRendererAccessory.sprite = spriteAux;
                spriteRendererAccessory.color = Color.red;
            }
            else
            {
                spriteRendererAccessory.enabled = false;
                if (playerData.accessoryStyle != "none") {
                    Debug.Log($"Accessory sprite not found: {pathAccessoryStyle}{playerData.accessoryStyle} for player {playerData.playerId}");
                } else {
                    Debug.LogWarning($"Accessory sprite not found: {pathAccessoryStyle}{playerData.accessoryStyle} for player {playerData.playerId}");
                }
            }
        }
        else
        {
            Debug.LogWarning("SpriteRendererAccessory reference is missing!");
        }

        spriteAux = PlayerManager.Instance.GetPlayerPortraitSpriteById(playerData.playerId);
        if (spriteAux != null)
        {
            spritePlayerPortrait = spriteAux;
        }
        else
        {
            Debug.LogWarning("SpritePlayerPortrait not found for player id: " + playerData.playerId);
        }


        // Additional initialization logic can go here
        SetName();
        UpdateStats();
        learnedSecret = GetLearnedSecretByLv();
        currentSecret.AddRange(learnedSecret);
        playerNameTag.SetPlayer(this);
    }

    private async void SetName()
    {
        var handle = LocalizationSettings.StringDatabase.GetTableAsync(tableCollectionName);
        await handle.Task;

        var table = handle.Result;
        playerName = playerId;
        if (table != null)
        {
            var entry = table.GetEntry(playerId);
            if (entry != null)
                playerName = entry.GetLocalizedString();
        }
        OnPlayerNameChanged?.Invoke();
    }

    void Awake()
    {
        // Cache all colliders on self and children
        colliders = GetComponentsInChildren<Collider>(true);
        keeperCollider = Array.Find(colliders, c => c.name == "KeeperCollider");
        // You may want to cache your PhotonView here if used frequently!
    }
    //stun
    public void Stun()
    {
        // In case a stun is in progress, stop it before starting a new one
        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
        }
        stunRoutine = StartCoroutine(StunPlayer());
    }

    public void Unstun()
    {
        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        // Stop the blink effect if it's running
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        isStunned = false;
        SetAllCollidersEnabled(true);
        SetYPosition(DefaultPosition.y);
    }

    public IEnumerator StunPlayer()
    {
        float duration = 3f;
        if (isStunned)
            yield break;
        isStunned = true;
        SetAllCollidersEnabled(false);
        // Start and remember the blink coroutine
        blinkRoutine = StartCoroutine(BlinkEffect(duration));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!GameManager.Instance.IsTimeFrozen)
                elapsed += Time.deltaTime;
            yield return null;
        }

        // Clean up at the end of the stun
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        SetAllCollidersEnabled(true);
        isStunned = false;
        SetYPosition(DefaultPosition.y);
        stunRoutine = null;
    }

private void SetAllCollidersEnabled(bool enabled)
{
    foreach (var col in colliders)
    {
        // If this collider is keeperCollider and we are NOT keeper, don't enable it.
        if (col == keeperCollider && !IsKeeper)
        {
            col.enabled = false; // Always disable regardless of argument
        }
        else
        {
            col.enabled = enabled;
        }
    }
}

    private IEnumerator BlinkEffect(float duration)
    {
        float elapsed = 0f;
        float blinkInterval = 0.2f;
        bool visible = true;
        float blinkElapsed = 0f;
        while (elapsed < duration)
        {
            if (!GameManager.Instance.IsTimeFrozen)
            {
                elapsed += Time.deltaTime;
                blinkElapsed += Time.deltaTime;
                if (blinkElapsed >= blinkInterval)
                {
                    SetYPosition(visible ? DefaultPosition.y : -1f);
                    visible = !visible;
                    blinkElapsed = 0f;
                }
            } else {
                SetYPosition(DefaultPosition.y);
            }
            yield return null;
        }
        SetYPosition(DefaultPosition.y);
    }

    public void Kick()
    {
        StartCoroutine(StopPlayerByControl());
    }

    public void Control()
    {
        StartCoroutine(StopPlayerByControl());
    }

    private IEnumerator StopPlayerByControl()
    {
        float controlMultiplier = 0.01f;
        float duration = 0.2f - (GetStat(PlayerStats.Control) * controlMultiplier);
        float durationMin = 0.1f;
        duration = Mathf.Max(duration, durationMin);
        isControlling = true;
        yield return new WaitForSeconds(duration);
        isControlling = false;
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
            Lv++;
            UpdateStats();
        }
    }

    private int ScaleStat(int maxStat, PlayerStats stat)
    {
        float t = (float)(Lv - 1) / (MAX_LV - 1);

        // HP and SP - Linear scaling
        if (stat == PlayerStats.Hp)
        {
            float minRatio = minStatRatioHp; // Example: 0.4f for 40 at lvl 1 if maxStat is 100
            float value = maxStat * Mathf.Lerp(minRatio, 1f, t);
            return Mathf.RoundToInt(value);
        }
        if (stat == PlayerStats.Sp)
        {
            float minRatio = minStatRatioSp;
            float value = maxStat * Mathf.Lerp(minRatio, 1f, t);
            return Mathf.RoundToInt(value);
        }

        // Other stats - Quadratic scaling
        {
            float minRatio = minStatRatioOther; // Example: 0.1f for 10 at lvl 1 if maxStat is 100
            // Quadratic interpolation between minRatio and 1, more curve!
            float q = t * t; // Quadratic interpolation (t squared)
            float value = maxStat * Mathf.Lerp(minRatio, 1f, q);
            return Mathf.RoundToInt(value);
        }
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
            currStats[i] = ScaleStat(baseStats[i], (PlayerStats)i) + moreStats[i];
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
            if (Lv == entry.lv && entry.secretId != null)
            {
                learnedSecret.Add(SecretManager.Instance.GetSecretById(entry.secretId));
            }
        }
    }

    private List<Secret> GetLearnedSecretByLv()
    {
        //when scouted set level (ex 5) then call this method
        List<Secret> secrets = new List<Secret>();
        foreach (var entry in learnSet)
        {
            if (Lv >= entry.lv && entry.secretId != null)
            {
                secrets.Add(SecretManager.Instance.GetSecretById(entry.secretId));
            }
        }
        return secrets;
    }

    // Call this before movement to ensure debuff is correct
    public void UpdateSpeedDebuff()
    {
        int playerHp = GetStat(PlayerStats.Hp);
        if (playerHp == _lastHpChecked) return; // avoid unnecessary recalculation

        _lastHpChecked = playerHp;
        if (playerHp <= hpThresholdLow)
            _lastSpeedDebuff = speedDebuffLow;
        else if (playerHp <= hpThresholdHigh)
            _lastSpeedDebuff = speedDebuffHigh;
        else
            _lastSpeedDebuff = speedDebuffDefault;
    }

    // Calculate final speed this frame (already includes deltaTime for direct use with MoveTowards)
    public float GetMoveSpeed()
    {
        UpdateSpeedDebuff();
        float speedMultiplier = ControlType == ControlType.Ai ? speedMultiplierAi : speedMultiplierUser; 
        return (GetStat(PlayerStats.Speed) * speedMultiplier + speedBase) * _lastSpeedDebuff * Time.deltaTime;
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData
        {
            playerId = this.playerId,
            lv = this.Lv,
            moreStats = (int[])this.moreStats.Clone(), // Deep copy
            currFreedom = this.currFreedom,
            currentSecretIds = this.currentSecret?.ConvertAll(sec => sec.SecretId) ?? new List<string>(),
            learnedSecretIds = this.learnedSecret?.ConvertAll(sec => sec.SecretId) ?? new List<string>()
        };
    }

    public void FromSaveData(PlayerSaveData data, PlayerData template)
    {
        Initialize(template);  // Reset everything (or just set the parts that never change)

        this.Lv = data.lv;

        Array.Copy(data.moreStats, this.moreStats, Mathf.Min(data.moreStats.Length, this.moreStats.Length));
        this.currFreedom = data.currFreedom;

        this.currentSecret = data.currentSecretIds?
            .ConvertAll(id => SecretManager.Instance.GetSecretById(id)) ?? new List<Secret>();

        this.learnedSecret = data.learnedSecretIds?
            .ConvertAll(id => SecretManager.Instance.GetSecretById(id)) ?? new List<Secret>();
        
        UpdateStats();
    }

    public void SetWear(Team team) 
    {
        bool isHome = true;

        WearRole role = IsKeeper ? WearRole.Keeper : WearRole.Field;
        WearVariant variant = isHome ? WearVariant.Home : WearVariant.Away;
    
        Sprite spriteAux = WearManager.Instance.GetWearSprite(team.TeamId, role, variant);
        if (spriteRendererWear != null)
        {
            if (spriteAux != null)
                spriteRendererWear.sprite = spriteAux;
            else
                Debug.LogWarning("No matching wear sprite found for {role}/{variant}/{team.TeamId}");
        }
        else
        {
            Debug.LogWarning("SpriteRendererWear reference is missing!");
        }

        spriteAux = WearManager.Instance.GetWearPortraitSprite("wearId", PortraitSize.M, role, variant);
        if (spriteAux != null)
            spriteWearPortrait = spriteAux;
        else
            Debug.LogWarning("No matching wear portrait sprite found for {size}/{role}/{variant}/{team.TeamId}");
    }

    #if PHOTON_UNITY_NETWORKING
    public void OnPhotonInstantiate(Photon.Pun.PhotonMessageInfo info)
    {
        // You can leave this empty unless you want networked spawn logic
    }
    #endif

public void UpdateKeeperColliderState()
{
    if (keeperCollider != null)
        keeperCollider.enabled = IsKeeper;
}
}
