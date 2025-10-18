using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Room Data", menuName = "Dungeon/Room Data")]
public class RoomData : ScriptableObject
{
    public string roomName;
    public RoomSize roomSize;
    public GameObject roomPrefab;

    public Vector2 roomDimensions = new Vector2(10f, 10f); // Width (X) and Depth (Z) in Unity units

    public float northDoorOffset = 0f;
    public float southDoorOffset = 0f;
    public float eastDoorOffset = 0f;
    public float westDoorOffset = 0f;

    public bool hasNorthDoor = false;
    public bool hasSouthDoor = false;
    public bool hasEastDoor = false;
    public bool hasWestDoor = false;

    [Range(0f, 1f)]
    public float spawnWeight = 1f;

    public bool isStartRoom = false;
    public bool isEndRoom = false;

    public List<DoorDirection> GetAvailableDoors()
    {
        List<DoorDirection> doors = new List<DoorDirection>();
        if (hasNorthDoor)
            doors.Add(DoorDirection.North);
        if (hasEastDoor)
            doors.Add(DoorDirection.East);
        if (hasSouthDoor)
            doors.Add(DoorDirection.South);
        if (hasWestDoor)
            doors.Add(DoorDirection.West);
        return doors;
    }

    public int GetDoorCount()
    {
        int count = 0;
        if (hasNorthDoor)
            count++;
        if (hasSouthDoor)
            count++;
        if (hasEastDoor)
            count++;
        if (hasWestDoor)
            count++;
        return count;
    }

    public Vector3 GetDoorLocalPosition(DoorDirection direction)
    {
        float halfWidth = roomDimensions.x * 0.5f;
        float halfDepth = roomDimensions.y * 0.5f;

        switch (direction)
        {
            case DoorDirection.North:
                return new Vector3(northDoorOffset, 0, halfDepth);
            case DoorDirection.South:
                return new Vector3(southDoorOffset, 0, -halfDepth);
            case DoorDirection.East:
                return new Vector3(halfWidth, 0, eastDoorOffset);
            case DoorDirection.West:
                return new Vector3(-halfWidth, 0, westDoorOffset);
            default:
                return Vector3.zero;
        }
    }
}

public enum RoomSize
{
    Big,
    Small,
}

public enum DoorDirection
{
    North = 0,
    East = 90,
    South = 180,
    West = 270,
}
