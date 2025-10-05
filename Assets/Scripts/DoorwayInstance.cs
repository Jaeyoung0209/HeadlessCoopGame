using System.Collections.Generic;
using UnityEngine;

public class DoorwayInstance
{
    public DoorwayData data;
    public RoomInstance parentRoom;
    public int doorwayIndex;
    public bool isConnected = false;
    public DoorwayInstance connectedTo;
    
    public DoorwayInstance(DoorwayData doorwayData, RoomInstance room, int index)
    {
        data = doorwayData;
        parentRoom = room;
        doorwayIndex = index;
    }
    
    public Vector3 GetWorldPosition()
    {
        return parentRoom.GetDoorwayWorldPosition(doorwayIndex);
    }
    
    public Quaternion GetWorldRotation()
    {
        return parentRoom.GetDoorwayWorldRotation(doorwayIndex);
    }
    
    public DoorDirection GetWorldDirection()
    {
        int localAngle = DirectionToAngle(data.direction);

        float roomYRotation = parentRoom.rotation.eulerAngles.y;
        int totalAngle = (localAngle + Mathf.RoundToInt(roomYRotation)) % 360;

        if (totalAngle < 0) totalAngle += 360;

        return AngleToDirection(totalAngle);
    }
    
    private int DirectionToAngle(DoorDirection dir)
    {
        switch (dir)
        {
            case DoorDirection.North: return 0;
            case DoorDirection.East: return 90;
            case DoorDirection.South: return 180;
            case DoorDirection.West: return 270;
            default: return 0;
        }
    }
    
    private DoorDirection AngleToDirection(int angle)
    {
        angle = Mathf.RoundToInt(angle / 90f) * 90;
        angle = angle % 360;
        if (angle < 0) angle += 360;
        
        switch (angle)
        {
            case 0: return DoorDirection.North;
            case 90: return DoorDirection.East;
            case 180: return DoorDirection.South;
            case 270: return DoorDirection.West;
            default: return DoorDirection.North;
        }
    }
}