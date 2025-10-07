using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Room Data", menuName = "Level/Room Data")]
public class RoomData : ScriptableObject
{
    public string roomName;
    public RoomSize roomSize;
    public GameObject roomPrefab;
    public bool hasNorthDoor = false;
    public bool hasSouthDoor = false;
    public bool hasEastDoor = false;
    public bool hasWestDoor = false;
    public Vector2 dimensions;

    [Range(0f, 1f)]
    public float spawnWeight = 1f;
    public bool isStartRoom = false;
    public bool isEndRoom = false;

    public List<DoorDirection> GetAvailableDoors()
    {
        List<DoorDirection> doors = new List<DoorDirection>();
        if (hasNorthDoor) doors.Add(DoorDirection.North);
        if (hasEastDoor) doors.Add(DoorDirection.East);
        if (hasSouthDoor) doors.Add(DoorDirection.South);
        if (hasWestDoor) doors.Add(DoorDirection.West);
        return doors;
    }

    public int GetDoorCount()
    {
        int count = 0;
        if (hasNorthDoor) count++;
        if (hasSouthDoor) count++;
        if (hasEastDoor) count++;
        if (hasWestDoor) count++;
        return count;
    }
}

[System.Serializable]
public class DoorwayData
{
    public Vector3 localPosition;
    public DoorDirection direction;
    public float width = 2f;

    public bool isOptional = false;
    public List<RoomType> allowedConnections = new List<RoomType>();
}

public enum RoomType
{
    Start,
    Corridor,
    Standard,
    End
}

public enum RoomSize
{
    Big,
    Small
}

public enum DoorDirection
{
    North = 0,
    East = 90,
    South = 180,
    West = 270
}
