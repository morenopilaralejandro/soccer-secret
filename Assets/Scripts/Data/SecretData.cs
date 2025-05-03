using UnityEngine;

[CreateAssetMenu(fileName = "SecretData", menuName = "ScriptableObject/SecretData")]
public class SecretData : ScriptableObject
{
    public string secretId;
    public string secretNameEn;
    public string secretNameJa;
    public string element;
    public int power;
    public int cost;
}
