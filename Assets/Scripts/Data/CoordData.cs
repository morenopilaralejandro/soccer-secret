using UnityEngine;

[CreateAssetMenu(fileName = "CoordData", menuName = "ScriptableObject/CoordData")]
public class CoordData : ScriptableObject
{
    public string coordId;
    public float x;
    public float y;
    public float z;
}
