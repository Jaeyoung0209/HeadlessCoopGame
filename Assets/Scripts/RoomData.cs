using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomData", menuName = "Scriptable Objects/RoomData")]
public class RoomData : ScriptableObject
{
    public string roomName;
    public RoomType roomType;
    public Vector2Int gridDimensions = new Vector2Int(1, 1);
    public GameObject roomPrefab;
    public List<DoorwayData> doorways = new List<DoorwayData>();
    
    [Range(0f, 1f)]
    public float spawnWeight = 1f;
    public int minDistanceFromStart = 0;
    public int maxAllowedInLevel = -1;
    
    public List<RoomType> cannotConnectTo = new List<RoomType>();
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

public enum DoorDirection
{
    North,
    South,
    East,
    West
}
