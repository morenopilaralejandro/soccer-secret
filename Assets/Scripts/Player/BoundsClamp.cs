using UnityEngine;

public static class BoundsClamp
{
    public static float MinX, MaxX, MinZ, MaxZ;

    public static void Setup(BoxCollider top, BoxCollider bottom, BoxCollider left, BoxCollider right,
                             float topOffset, float bottomOffset, float leftOffset, float rightOffset, float touchAreaOffset)
    {
        MinX = left.bounds.min.x + leftOffset;
        MaxX = right.bounds.max.x - rightOffset;
        MinZ = bottom.bounds.min.z + bottomOffset - touchAreaOffset;
        MaxZ = top.bounds.max.z - topOffset;
    }

    public static Vector3 Clamp(Vector3 point)
    {
        return new Vector3(
            Mathf.Clamp(point.x, MinX, MaxX),
            point.y,
            Mathf.Clamp(point.z, MinZ, MaxZ)
        );
    }
}
