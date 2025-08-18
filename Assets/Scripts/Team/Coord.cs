using UnityEngine;

public class Coord
{
    public string CoordId;
    public Position Position;
    public Vector3 DefaultPosition;

    public Coord(string coordId, Position position, Vector3 defaultPosition)
    {
        CoordId = coordId;
        Position = position;
        DefaultPosition = defaultPosition;
    }

    public Coord(string coordId, Position position, float x, float y, float z)
    {
        CoordId = coordId;
        Position = position;
        DefaultPosition = new Vector3(x, y, z);
    }
}
