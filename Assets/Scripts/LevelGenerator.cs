using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public int minBigRooms = 5;
    public int maxBigRooms = 10;
    public int seed = 0;
    public bool useRandomSeed = true;

    public RoomData startRoomData;
    public List<RoomData> bigRoomData;
    public List<RoomData> smallRoomData;
    public RoomData endRoomData;

    public float gridCellSize = 10f;

    public float smallRoomChance = 0.6f;
    public int maxConsecutiveSmallRooms = 2;
    public float continueChainChance = 0.3f;
    public bool allowRoomRotation = true;

    private Dictionary<Vector2Int, RoomInstance> grid = new Dictionary<Vector2Int, RoomInstance>();
    private List<RoomInstance> allRooms = new List<RoomInstance>();
    private Queue<DoorwayInstance> availableDoorways = new Queue<DoorwayInstance>();
    private System.Random rng;

    void Start()
    {
        GenerateLevel();
    }

    [ContextMenu("Generate New Level")]
    public void GenerateLevel()
    {
        ClearLevel();

        if (useRandomSeed)
            seed = Random.Range(0, 999999);

        rng = new System.Random(seed);
        Debug.Log($"Generating Level with seed: {seed}");

        SpawnStartRoom();
        GenerateBigRooms();
        FillSmallRoomsOrSealDoors();

        if (endRoomData != null)
        {
            PlaceEndRoom();
        }

        Debug.Log(
            $"Generated {allRooms.Count} total rooms ({CountBigRooms()} big, {CountSmallRooms()} small)"
        );
    }

    void SpawnStartRoom()
    {
        if (startRoomData == null)
        {
            Debug.LogError("Start room data not assigned!");
            return;
        }

        RoomInstance startRoom = new RoomInstance(startRoomData, Vector2Int.zero, 0, 0);
        PlaceRoom(startRoom);

        foreach (var doorway in startRoom.doorways.Values)
        {
            availableDoorways.Enqueue(doorway);
        }
    }

    void GenerateBigRooms()
    {
        int bigRoomsPlaced = 1;
        int targetBigRooms = rng.Next(minBigRooms, maxBigRooms + 1);
        int attempts = 0;
        int maxAttempts = targetBigRooms * 50;

        while (
            bigRoomsPlaced < targetBigRooms && availableDoorways.Count > 0 && attempts < maxAttempts
        )
        {
            attempts++;

            DoorwayInstance currentDoorway = availableDoorways.Dequeue();

            if (currentDoorway.isConnected)
                continue;

            RoomData selectedRoom = SelectWeightedRandom(bigRoomData);
            if (selectedRoom == null)
                continue;

            if (TryPlaceBigRoom(selectedRoom, currentDoorway))
            {
                bigRoomsPlaced++;
            }
        }

        Debug.Log($"Placed {bigRoomsPlaced} big rooms in {attempts} attempts");
    }

    bool TryPlaceBigRoom(RoomData roomData, DoorwayInstance connectToDoorway)
    {
        Vector2Int targetGridPos = connectToDoorway.GetAdjacentGridPosition();

        if (grid.ContainsKey(targetGridPos))
            return false;

        DoorDirection requiredWorldDirection = connectToDoorway.GetOppositeDirection();

        List<int> rotations = allowRoomRotation
            ? new List<int> { 0, 90, 180, 270 }
            : new List<int> { 0 };

        ShuffleList(rotations);

        foreach (int rotation in rotations)
        {
            if (RoomHasDoorInDirectionWithRotation(roomData, requiredWorldDirection, rotation))
            {
                int distance = connectToDoorway.parentRoom.distanceFromStart + 1;
                RoomInstance newRoom = new RoomInstance(
                    roomData,
                    targetGridPos,
                    rotation,
                    distance
                );
                PlaceRoom(newRoom);

                DoorwayInstance newDoorway = newRoom.doorways[requiredWorldDirection];
                ConnectDoorways(connectToDoorway, newDoorway);

                foreach (var doorway in newRoom.doorways.Values)
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

    bool RoomHasDoorInDirectionWithRotation(
        RoomData roomData,
        DoorDirection targetWorldDir,
        int rotation
    )
    {
        int targetLocalAngle = ((int)targetWorldDir - rotation) % 360;
        if (targetLocalAngle < 0)
            targetLocalAngle += 360;

        DoorDirection targetLocalDir = (DoorDirection)targetLocalAngle;

        return HasDoorInDirection(roomData, targetLocalDir);
    }

    void FillSmallRoomsOrSealDoors()
    {
        List<DoorwayInstance> unconnectedDoorways = new List<DoorwayInstance>();

        foreach (var room in allRooms)
        {
            if (room.roomData.roomSize == RoomSize.Big)
            {
                foreach (var doorway in room.doorways.Values)
                {
                    if (!doorway.isConnected)
                    {
                        unconnectedDoorways.Add(doorway);
                    }
                }
            }
        }

        ShuffleList(unconnectedDoorways);

        foreach (var doorway in unconnectedDoorways)
        {
            if (doorway.isConnected)
                continue;

            if (rng.NextDouble() < smallRoomChance)
            {
                TryAddSmallRoomChain(doorway);
            }
            else
            {
                SealDoorway(doorway);
            }
        }
    }

    void TryAddSmallRoomChain(DoorwayInstance startDoorway)
    {
        DoorwayInstance currentDoorway = startDoorway;
        int smallRoomsInChain = 0;

        while (smallRoomsInChain < maxConsecutiveSmallRooms && !currentDoorway.isConnected)
        {
            if (smallRoomsInChain > 0 && rng.NextDouble() > continueChainChance)
            {
                SealDoorway(currentDoorway);
                return;
            }

            RoomData selectedSmallRoom = SelectWeightedRandom(smallRoomData);
            if (selectedSmallRoom == null)
                break;

            Vector2Int targetGridPos = currentDoorway.GetAdjacentGridPosition();

            if (grid.ContainsKey(targetGridPos))
                break;

            DoorDirection requiredWorldDirection = currentDoorway.GetOppositeDirection();

            List<int> rotations = allowRoomRotation
                ? new List<int> { 0, 90, 180, 270 }
                : new List<int> { 0 };

            ShuffleList(rotations);

            bool placed = false;
            foreach (int rotation in rotations)
            {
                if (
                    RoomHasDoorInDirectionWithRotation(
                        selectedSmallRoom,
                        requiredWorldDirection,
                        rotation
                    )
                )
                {
                    int distance = currentDoorway.parentRoom.distanceFromStart + 1;
                    RoomInstance smallRoom = new RoomInstance(
                        selectedSmallRoom,
                        targetGridPos,
                        rotation,
                        distance
                    );

                    Vector3 targetDoorPos = currentDoorway.GetWorldPosition(gridCellSize);
                    Vector3 smallRoomDoorPos = smallRoom
                        .doorways[requiredWorldDirection]
                        .GetWorldPosition(gridCellSize);
                    Vector3 offset = targetDoorPos - smallRoomDoorPos;

                    smallRoom.positionOffset = offset;

                    PlaceRoom(smallRoom);

                    DoorwayInstance smallRoomDoorway = smallRoom.doorways[requiredWorldDirection];
                    ConnectDoorways(currentDoorway, smallRoomDoorway);

                    smallRoomsInChain++;
                    placed = true;

                    DoorwayInstance nextDoorway = null;
                    foreach (var doorway in smallRoom.doorways.Values)
                    {
                        if (!doorway.isConnected)
                        {
                            nextDoorway = doorway;
                            break;
                        }
                    }

                    if (nextDoorway == null)
                    {
                        return;
                    }

                    currentDoorway = nextDoorway;
                    break;
                }
            }

            if (!placed)
                break;
        }

        if (currentDoorway != null && !currentDoorway.isConnected)
        {
            SealDoorway(currentDoorway);
        }
    }

    void PlaceEndRoom()
    {
        RoomInstance furthestRoom = null;
        int maxDistance = 0;

        foreach (var room in allRooms)
        {
            if (room.roomData.roomSize == RoomSize.Big && room.distanceFromStart > maxDistance)
            {
                maxDistance = room.distanceFromStart;
                furthestRoom = room;
            }
        }

        if (furthestRoom == null)
            return;

        DoorwayInstance doorwayForEnd = null;
        foreach (var doorway in furthestRoom.doorways.Values)
        {
            if (!doorway.isConnected)
            {
                doorwayForEnd = doorway;
                break;
            }
        }

        if (doorwayForEnd == null)
            return;

        Vector2Int targetGridPos = doorwayForEnd.GetAdjacentGridPosition();

        if (grid.ContainsKey(targetGridPos))
            return;

        DoorDirection requiredWorldDirection = doorwayForEnd.GetOppositeDirection();

        List<int> rotations = allowRoomRotation
            ? new List<int> { 0, 90, 180, 270 }
            : new List<int> { 0 };

        foreach (int rotation in rotations)
        {
            if (RoomHasDoorInDirectionWithRotation(endRoomData, requiredWorldDirection, rotation))
            {
                RoomInstance endRoom = new RoomInstance(
                    endRoomData,
                    targetGridPos,
                    rotation,
                    maxDistance + 1
                );
                PlaceRoom(endRoom);

                DoorwayInstance endDoorway = endRoom.doorways[requiredWorldDirection];
                ConnectDoorways(doorwayForEnd, endDoorway);

                Debug.Log($"Placed end room at distance {endRoom.distanceFromStart}");
                return;
            }
        }
    }

    void PlaceRoom(RoomInstance room)
    {
        grid[room.gridPosition] = room;
        allRooms.Add(room);

        Vector3 worldPos = room.GetWorldPosition(gridCellSize);
        Quaternion worldRot = room.GetWorldRotation();
        GameObject roomObj = Instantiate(room.roomData.roomPrefab, worldPos, worldRot, transform);
        room.roomObject = roomObj;
    }

    void ConnectDoorways(DoorwayInstance d1, DoorwayInstance d2)
    {
        d1.isConnected = true;
        d1.connectedTo = d2;
        d2.isConnected = true;
        d2.connectedTo = d1;
    }

    void SealDoorway(DoorwayInstance doorway)
    {
        doorway.isConnected = true;
        // TODO
    }

    bool HasDoorInDirection(RoomData roomData, DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.North:
                return roomData.hasNorthDoor;
            case DoorDirection.South:
                return roomData.hasSouthDoor;
            case DoorDirection.East:
                return roomData.hasEastDoor;
            case DoorDirection.West:
                return roomData.hasWestDoor;
            default:
                return false;
        }
    }

    RoomData SelectWeightedRandom(List<RoomData> rooms)
    {
        if (rooms == null || rooms.Count == 0)
            return null;

        float totalWeight = 0;
        foreach (var room in rooms)
            totalWeight += room.spawnWeight;

        float randomValue = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0;

        foreach (var room in rooms)
        {
            cumulative += room.spawnWeight;
            if (randomValue <= cumulative)
                return room;
        }

        return rooms[rooms.Count - 1];
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

    int CountBigRooms()
    {
        int count = 0;
        foreach (var room in allRooms)
        {
            if (room.roomData.roomSize == RoomSize.Big)
                count++;
        }
        return count;
    }

    int CountSmallRooms()
    {
        int count = 0;
        foreach (var room in allRooms)
        {
            if (room.roomData.roomSize == RoomSize.Small)
                count++;
        }
        return count;
    }

    [ContextMenu("Clear Level")]
    void ClearLevel()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        grid.Clear();
        allRooms.Clear();
        availableDoorways.Clear();
    }
}
