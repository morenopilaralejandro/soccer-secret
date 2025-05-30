using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Category { Shoot, Dribble, Block, Catch }

public class SecretManager : MonoBehaviour
{
    public static SecretManager Instance { get; private set; }

    private Dictionary<string, SecretData> secretDataDict = new Dictionary<string, SecretData>();
    private Dictionary<string, Secret> secretDict = new Dictionary<string, Secret>();

    [SerializeField] private string effectPrefabPath = "Effects/Secret/";
    [SerializeField] private string sfxPath = "SfxSecret";

    [SerializeField] private Sprite[] categoryIcons;
    [SerializeField] private Color[] categoryColors = {
        new Color(0.906f, 0.420f, 0.482f, 1.0f),
        new Color(0.290f, 0.420f, 0.839f, 1.0f),
        new Color(0.224f, 0.710f, 0.160f, 1.0f),
        new Color(0.871f, 0.549f, 0.0f, 1.0f)
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SecretData[] allSecrets = Resources.LoadAll<SecretData>("ScriptableObjects/Secret");
        foreach (SecretData secretData in allSecrets)
        {
            AddSecretDataToDict(secretData);
            Secret auxSecret = gameObject.AddComponent<Secret>();
            auxSecret.Initialize(secretData);
            AddSecretToDict(auxSecret);
        }
    }

    void Start()
    {

    }

    public void AddSecretDataToDict(SecretData secretData)
    {
        if (!secretDataDict.ContainsKey(secretData.secretId))
            secretDataDict.Add(secretData.secretId, secretData);
        else
            Debug.LogWarning("Duplicate secretId: " + secretData.secretId);
    }

    public void AddSecretToDict(Secret secret)
    {
        if (!secretDict.ContainsKey(secret.SecretId))
            secretDict.Add(secret.SecretId, secret);
        else
            Debug.LogWarning("Duplicate secretId: " + secret.SecretId);
    }

    public SecretData GetSecretDataById(string secretId)
    {
        if (secretDataDict.TryGetValue(secretId, out var secretData))
            return secretData;

        Debug.LogWarning("Secret not found: " + secretId);
        return null;
    }

    public Secret GetSecretById(string secretId)
    {
        if (secretDict.TryGetValue(secretId, out var secret))
            return secret;

        Debug.LogWarning("Secret not found: " + secretId);
        return null;
    }

    public Sprite GetCategoryIcon(Category category)
    {
        int index = (int)category;
        if (categoryIcons != null &&
            categoryIcons.Length > index &&
            categoryIcons[index] != null)
        {
            return categoryIcons[index];
        }
        else
        {
            return categoryIcons[0];
        }
    }

    public Color GetCategoryColor(Category category)
    {
        int index = (int)category;
        if (categoryColors != null &&
            categoryColors.Length > index &&
            categoryColors[index] != null)
        {
            return categoryColors[index];
        }
        else
        {
            return new Color();
        }
    }

    /*
    public void PlaySecretEffect(string secretId, Vector3 position)
    {
        SecretData secretData = GetSecretDataById(secretId);
        if (secretData != null && !string.IsNullOrEmpty(secretData.effectPrefabPath))
        {
            // If using Resources folder:
            GameObject effectPrefab = Resources.Load<GameObject>(secretData.effectPrefabPath);
            if (effectPrefab != null)
                Instantiate(effectPrefab, position, Quaternion.identity);
            else
                Debug.LogWarning("Effect prefab not found at: " + secretData.effectPrefabPath);
        }
        else
        {
            Debug.LogWarning("Effect prefab path not assigned for secretId: " + secretId);
        }
    }
    */

    public void PlaySecretEffect(Secret secret, Vector3 position)
    {
        if (secret != null && !string.IsNullOrEmpty(effectPrefabPath))
        {
            // Audio
            AudioManager.Instance.PlaySfx(sfxPath + secret.Element);
            // Particle
            GameObject effectPrefab = Resources.Load<GameObject>(effectPrefabPath + secret.Element);
            if (effectPrefab != null)
                Instantiate(effectPrefab, position, Quaternion.identity);
            else
                Debug.LogWarning("Effect prefab not found at: " + effectPrefabPath + secret.Element);
        }
        else
        {
            Debug.LogWarning("Effect prefab path not assigned for secretId: " + secret.SecretId);
        }
    }
}
