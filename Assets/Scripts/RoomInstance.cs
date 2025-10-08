using System.Collections.Generic;
using UnityEngine;

public class RoomInstance
{
    public RoomData roomData;
    public GameObject roomObject;
    public Vector2Int gridPosition;
    public int rotation; // 0, 90, 180, 270 degrees
    public Vector3 positionOffset;
    public Dictionary<DoorDirection, DoorwayInstance> doorways;
    public int distanceFromStart;
    
    public RoomInstance(RoomData data, Vector2Int gridPos, int rot, int distance)
    {
        roomData = data;
        gridPosition = gridPos;
        rotation = rot;
        positionOffset = Vector3.zero;
        distanceFromStart = distance;
        doorways = new Dictionary<DoorDirection, DoorwayInstance>();
        
        // Create doorway instances for available doors (with rotation applied)
        foreach (var dir in data.GetAvailableDoors())
        {
            DoorDirection rotatedDir = RotateDirection(dir, rotation);
            doorways[rotatedDir] = new DoorwayInstance(this, rotatedDir, dir);
        }
    }
    
    public Vector3 GetWorldPosition(float gridCellSize)
    {
        Vector3 basePos = new Vector3(gridPosition.x * gridCellSize, 0, gridPosition.y * gridCellSize);
        return basePos + positionOffset;
    }
    
    public Quaternion GetWorldRotation()
    {
        return Quaternion.Euler(0, rotation, 0);
    }
    
    public bool HasDoor(DoorDirection dir)
    {
        return doorways.ContainsKey(dir);
    }
    
    // Rotate a direction by the given angle
    private DoorDirection RotateDirection(DoorDirection dir, int angle)
    {
        int newAngle = ((int)dir + angle) % 360;
        if (newAngle < 0) newAngle += 360;
        return (DoorDirection)newAngle;
    }
}
