using System.Collections.Generic;
using UnityEngine;

public class RoomInstance
{
    public RoomData roomData;
    public GameObject roomObject;
    public List<DoorwayInstance> doorways = new List<DoorwayInstance>();
    public int distanceFromStart = 0;
    public Vector3 position;
    public Quaternion rotation;

    public RoomInstance(RoomData data, Vector3 pos, Quaternion rot, int distance)
    {
        roomData = data;
        position = pos;
        rotation = rot;
        distanceFromStart = distance;

        for (int i = 0; i < data.doorways.Count; i++)
        {
            DoorwayInstance doorway = new DoorwayInstance(data.doorways[i], this, i);
            doorways.Add(doorway);
        }
    }

    public Vector3 GetDoorwayWorldPosition(int doorwayIndex)
    {
        Vector3 localPos = roomData.doorways[doorwayIndex].localPosition;
        return position + rotation * localPos;
    }

    public Quaternion GetDoorwayWorldRotation(int doorwayIndex)
    {
        DoorDirection dir = roomData.doorways[doorwayIndex].direction;
        int angle = DirectionToAngle(dir);
        return rotation * Quaternion.Euler(0, angle, 0);
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
}