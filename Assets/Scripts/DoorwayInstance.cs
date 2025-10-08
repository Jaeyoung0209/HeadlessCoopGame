using System.Collections.Generic;
using UnityEngine;

public class DoorwayInstance
{
    public RoomInstance parentRoom;
    public DoorDirection worldDirection;
    public DoorDirection localDirection;
    public bool isConnected = false;
    public DoorwayInstance connectedTo;
    
    public DoorwayInstance(RoomInstance room, DoorDirection worldDir, DoorDirection localDir)
    {
        parentRoom = room;
        worldDirection = worldDir;
        localDirection = localDir;
    }
    
    public Vector2Int GetAdjacentGridPosition()
    {
        Vector2Int offset = GetDirectionOffset(worldDirection);
        return parentRoom.gridPosition + offset;
    }
    
    public DoorDirection GetOppositeDirection()
    {
        return (DoorDirection)(((int)worldDirection + 180) % 360);
    }

    public Vector3 GetWorldPosition(float gridCellSize)
    {
        Vector3 localPos = parentRoom.roomData.GetDoorLocalPosition(localDirection);

        Quaternion rotation = parentRoom.GetWorldRotation();
        Vector3 rotatedLocalPos = rotation * localPos;

        Vector3 roomWorldPos = parentRoom.GetWorldPosition(gridCellSize);
        return roomWorldPos + rotatedLocalPos;
    }
    
    private Vector2Int GetDirectionOffset(DoorDirection dir)
    {
        switch (dir)
        {
            case DoorDirection.North: return new Vector2Int(0, 1);
            case DoorDirection.South: return new Vector2Int(0, -1);
            case DoorDirection.East: return new Vector2Int(1, 0);
            case DoorDirection.West: return new Vector2Int(-1, 0);
            default: return Vector2Int.zero;
        }
    }
}
