using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public int maxRooms = 15;
    public int seed = 0;
    public bool useRandomSeed = true;

    public RoomData startRoomData;
    public List<RoomData> availableRoomData;
    public RoomData endRoomData;

    public float gridCellSize = 10f;
    public int maxPlacementAttempts = 50;
    public int desiredLoopCount = 3;
    public float maxLoopDistance = 15f;
    public RoomData loopCorridorPrefab;

    private List<RoomInstance> spawnedRooms = new List<RoomInstance>();
    private Queue<DoorwayInstance> availableDoorways = new Queue<DoorwayInstance>();
    private Dictionary<RoomData, int> roomCountTracker = new Dictionary<RoomData, int>();
    private System.Random rng;

    void Start()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        ClearLevel();

        if (useRandomSeed)
            seed = Random.Range(0, 999999);

        rng = new System.Random(seed);
        roomCountTracker.Clear();

        Debug.Log($"Generating Level with seed: {seed}");

        if (startRoomData == null)
        {
            Debug.LogError("Start room data not assigned!");
            return;
        }

        SpawnStartRoom();

        int roomCount = 1;
        int attemptCount = 0;

        while (roomCount < maxRooms && availableDoorways.Count > 0 && attemptCount < maxPlacementAttempts * maxRooms)
        {
            attemptCount++;

            if (availableDoorways.Count == 0)
                break;

            DoorwayInstance currentDoorway = availableDoorways.Dequeue();

            if (currentDoorway.isConnected)
                continue;

            RoomData selectedRoom = SelectRoomData(currentDoorway, roomCount);

            if (selectedRoom == null)
            {
                if (currentDoorway.data.isOptional)
                    currentDoorway.isConnected = true;
                continue;
            }

            if (TryPlaceRoom(selectedRoom, currentDoorway, roomCount))
            {
                roomCount++;

                if (!roomCountTracker.ContainsKey(selectedRoom))
                    roomCountTracker[selectedRoom] = 0;
                roomCountTracker[selectedRoom]++;
            }
        }

        Debug.Log($"Generated {roomCount} rooms in {attemptCount} attempts");

    }

    void SpawnStartRoom()
    {
        RoomInstance room = new RoomInstance(startRoomData, Vector3.zero, Quaternion.identity, 0);
        GameObject roomObj = Instantiate(startRoomData.roomPrefab, Vector3.zero, Quaternion.identity, transform);
        room.roomObject = roomObj;

        spawnedRooms.Add(room);

        foreach (var doorway in room.doorways)
        {
            availableDoorways.Enqueue(doorway);
        }
    }

    RoomData SelectRoomData(DoorwayInstance connectingDoorway, int currentRoomCount)
    {
        if (currentRoomCount >= maxRooms - 1 && endRoomData != null)
        {
            if (IsRoomDataValid(endRoomData, connectingDoorway))
                return endRoomData;
        }

        List<RoomData> validRooms = new List<RoomData>();
        List<float> weights = new List<float>();

        foreach (var roomData in availableRoomData)
        {
            if (!IsRoomDataValid(roomData, connectingDoorway))
                continue;

            if (roomData.maxAllowedInLevel > 0)
            {
                int currentCount = roomCountTracker.ContainsKey(roomData) ? roomCountTracker[roomData] : 0;
                if (currentCount >= roomData.maxAllowedInLevel)
                    continue;
            }

            validRooms.Add(roomData);
            weights.Add(roomData.spawnWeight);
        }

        if (validRooms.Count == 0)
            return null;

        return GetWeightedRandom(validRooms, weights);
    }

    bool IsRoomDataValid(RoomData roomData, DoorwayInstance connectingDoorway)
    {
        if (roomData == null || roomData.roomPrefab == null)
            return false;

        if (connectingDoorway.parentRoom.distanceFromStart < roomData.minDistanceFromStart)
            return false;

        if (roomData.cannotConnectTo.Contains(connectingDoorway.parentRoom.roomData.roomType))
            return false;

        if (connectingDoorway.data.allowedConnections.Count > 0)
        {
            if (!connectingDoorway.data.allowedConnections.Contains(roomData.roomType))
                return false;
        }

        return true;
    }

    bool TryPlaceRoom(RoomData roomData, DoorwayInstance connectToDoorway, int distanceFromStart)
    {
        List<DoorwayData> doorways = new List<DoorwayData>(roomData.doorways);
        ShuffleList(doorways);

        foreach (var doorwayData in doorways)
        {
            DoorDirection connectWorldDir = connectToDoorway.GetWorldDirection();

            Quaternion rotation = CalculateRoomRotation(connectWorldDir, doorwayData.direction);

            Vector3 rotatedDoorwayPos = rotation * doorwayData.localPosition;
            Vector3 roomPosition = connectToDoorway.GetWorldPosition() - rotatedDoorwayPos;

            if (IsPositionValid(roomPosition, rotation, roomData))
            {
                RoomInstance room = new RoomInstance(roomData, roomPosition, rotation, distanceFromStart);

                GameObject roomObj = Instantiate(roomData.roomPrefab, roomPosition, rotation, transform);
                room.roomObject = roomObj;

                spawnedRooms.Add(room);

                DoorwayInstance newDoorway = room.doorways[roomData.doorways.IndexOf(doorwayData)];
                connectToDoorway.isConnected = true;
                connectToDoorway.connectedTo = newDoorway;
                newDoorway.isConnected = true;
                newDoorway.connectedTo = connectToDoorway;

                foreach (var doorway in room.doorways)
                {
                    if (!doorway.isConnected)
                    {
                        availableDoorways.Enqueue(doorway);
                    }
                }

                return true;
            }
        }

        return false;
    }

    Quaternion CalculateRoomRotation(DoorDirection connectDir, DoorDirection newRoomDir)
    {
        int connectAngle = DirectionToAngle(connectDir);
        int newRoomAngle = DirectionToAngle(newRoomDir);
        int targetAngle = (connectAngle + 180) % 360;
        int rotationNeeded = targetAngle - newRoomAngle;

        return Quaternion.Euler(0, rotationNeeded, 0);
    }

    int DirectionToAngle(DoorDirection dir)
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

    bool IsPositionValid(Vector3 position, Quaternion rotation, RoomData newRoomData)
    {
        // Calculate bounds for the new room
        Vector3 newRoomSize = new Vector3(
            newRoomData.gridDimensions.x * gridCellSize,
            gridCellSize, // Height (arbitrary, can be adjusted)
            newRoomData.gridDimensions.y * gridCellSize
        );

        Bounds newBounds = new Bounds(position, newRoomSize);

        foreach (var existingRoom in spawnedRooms)
        {
            Vector3 existingSize = new Vector3(
                existingRoom.roomData.gridDimensions.x * gridCellSize,
                gridCellSize,
                existingRoom.roomData.gridDimensions.y * gridCellSize
            );

            Bounds existingBounds = new Bounds(existingRoom.position, existingSize);

            existingBounds.Expand(0.5f);

            if (newBounds.Intersects(existingBounds))
            {
                return false;
            }
        }

        return true;
    }

    RoomData GetWeightedRandom(List<RoomData> items, List<float> weights)
    {
        float totalWeight = 0;
        foreach (float w in weights)
            totalWeight += w;

        float randomValue = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0;

        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
                return items[i];
        }

        return items[items.Count - 1];
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    bool TryConnectDoorwaysWithCorridor(DoorwayInstance d1, DoorwayInstance d2)
    {
        Vector3 pos1 = d1.GetWorldPosition();
        Vector3 pos2 = d2.GetWorldPosition();

        float distance = Vector3.Distance(pos1, pos2);

        if (distance > maxLoopDistance || distance < gridCellSize * 1.5f)
            return false;

        Vector3 dir1 = d1.GetWorldRotation() * Vector3.forward;
        Vector3 dir2 = d2.GetWorldRotation() * Vector3.forward;
        Vector3 between = (pos2 - pos1).normalized;

        float dot1 = Vector3.Dot(dir1, between);
        float dot2 = Vector3.Dot(dir2, -between);

        if (dot1 < 0.5f || dot2 < 0.5f)
            return false;

        if (loopCorridorPrefab != null)
        {
            Vector3 midpoint = (pos1 + pos2) * 0.5f;
            Vector3 direction = (pos2 - pos1).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            if (!IsPositionValid(midpoint, rotation, loopCorridorPrefab))
                return false;

            RoomInstance corridor = new RoomInstance(loopCorridorPrefab, midpoint, rotation,
                Mathf.Max(d1.parentRoom.distanceFromStart, d2.parentRoom.distanceFromStart) + 1);

            GameObject corridorObj = Instantiate(loopCorridorPrefab.roomPrefab, midpoint, rotation, transform);
            corridor.roomObject = corridorObj;

            spawnedRooms.Add(corridor);

            d1.isConnected = true;
            d2.isConnected = true;

            if (corridor.doorways.Count >= 2)
            {
                corridor.doorways[0].isConnected = true;
                corridor.doorways[1].isConnected = true;

                d1.connectedTo = corridor.doorways[0];
                corridor.doorways[0].connectedTo = d1;

                d2.connectedTo = corridor.doorways[1];
                corridor.doorways[1].connectedTo = d2;
            }

            return true;
        }
        else
        {
            d1.isConnected = true;
            d2.isConnected = true;
            d1.connectedTo = d2;
            d2.connectedTo = d1;
            return true;
        }
    }

    void ClearLevel()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        spawnedRooms.Clear();
        availableDoorways.Clear();
    }
}