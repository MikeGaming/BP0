using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGeneratorScript : MonoBehaviour
{
    public static DungeonGeneratorScript Instance { get; private set; }

    [SerializeField] private GameObject entrance;
    [SerializeField] private List<GameObject> rooms;
    [SerializeField] private List<GameObject> specialRooms;
    [SerializeField] private List<GameObject> alternateEntrances;
    [SerializeField] private List<GameObject> hallways;
    [SerializeField] private GameObject door;
    [SerializeField] private int noOfRooms = 10;
    [SerializeField] private LayerMask roomsLayermask;
    private List<DungeonPart> generatedRooms;
    private bool isGenerated = false;

    private void Start()
    {
        Instance = this;
        generatedRooms = new List<DungeonPart>();
        StartGeneration();
    }

    public void StartGeneration()
    {
        Debug.Log("Starting dungeon generation.");
        Generate();
        GenerateAlternateEntrances();
        FillEmptyEntrances();
        isGenerated = true;
        Debug.Log("Dungeon generation completed.");
    }

    private void Generate()
    {
        Debug.Log("Generating main dungeon parts.");
        for (int i = 0; i < noOfRooms - alternateEntrances.Count; i++)
        {
            if (generatedRooms.Count < 1)
            {
                Debug.Log("Generating entrance.");
                GameObject generatedRoom = Instantiate(entrance, transform.position, transform.rotation);
                generatedRoom.transform.SetParent(null);

                if (generatedRoom.TryGetComponent(out DungeonPart dungeonPart))
                {
                    generatedRooms.Add(dungeonPart);
                    Debug.Log("Entrance added to generated rooms.");
                }
            }
            else
            {
                bool shouldPlaceHallway = Random.Range(0f, 1f) > 0.5f;
                DungeonPart randomGeneratedRoom = null;
                Transform room1Entrypoint = null;
                int totalRetries = 100;
                int retryIndex = 0;

                while (randomGeneratedRoom == null && retryIndex < totalRetries)
                {
                    int randomLinkRoomIndex = Random.Range(0, generatedRooms.Count);
                    DungeonPart roomToTest = generatedRooms[randomLinkRoomIndex];
                    if (roomToTest.HasAvailableEntrypoint(out room1Entrypoint))
                    {
                        randomGeneratedRoom = roomToTest;
                        Debug.Log($"Found available entry point in room {roomToTest.name}.");
                        break;
                    }
                    retryIndex++;
                }

                if (randomGeneratedRoom == null)
                {
                    Debug.LogWarning("Failed to find a room with an available entry point after 100 retries.");
                    continue;
                }

                GameObject doorToAlign = Instantiate(door, transform.position, transform.rotation);

                if (shouldPlaceHallway)
                {
                    int randomIndex = Random.Range(0, hallways.Count);
                    GameObject generatedHallway = Instantiate(hallways[randomIndex], transform.position, transform.rotation);
                    generatedHallway.transform.SetParent(null);
                    if (generatedHallway.TryGetComponent<DungeonPart>(out DungeonPart dungeonPart))
                    {
                        if (dungeonPart.HasAvailableEntrypoint(out Transform room2Entrypoint))
                        {
                            generatedRooms.Add(dungeonPart);
                            doorToAlign.transform.position = room1Entrypoint.transform.position;
                            doorToAlign.transform.rotation = room1Entrypoint.transform.rotation;
                            AlignRooms(randomGeneratedRoom.transform, generatedHallway.transform, room1Entrypoint, room2Entrypoint);
                            Debug.Log($"Placed hallway {generatedHallway.name}.");

                            if (HandleIntersection(dungeonPart))
                            {
                                Debug.LogWarning($"Intersection detected for hallway {generatedHallway.name}. Retrying placement.");
                                dungeonPart.UnuseEntrypoint(room2Entrypoint);
                                randomGeneratedRoom.UnuseEntrypoint(room1Entrypoint);
                                RetryPlacement(generatedHallway, doorToAlign, 0);
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    GameObject generatedRoom;

                    if (specialRooms.Count > 0)
                    {
                        bool shouldPlaceSpecialRoom = Random.Range(0f, 1f) > 0.9f;

                        if (shouldPlaceSpecialRoom)
                        {
                            int randomIndex = Random.Range(0, specialRooms.Count);
                            generatedRoom = Instantiate(specialRooms[randomIndex], transform.position, transform.rotation);
                            Debug.Log($"Placed special room {generatedRoom.name}.");
                        }
                        else
                        {
                            int randomIndex = Random.Range(0, rooms.Count);
                            generatedRoom = Instantiate(rooms[randomIndex], transform.position, transform.rotation);
                            Debug.Log($"Placed regular room {generatedRoom.name}.");
                        }
                    }
                    else
                    {
                        int randomIndex = Random.Range(0, rooms.Count);
                        generatedRoom = Instantiate(rooms[randomIndex], transform.position, transform.rotation);
                        Debug.Log($"Placed regular room {generatedRoom.name}.");
                    }
                    generatedRoom.transform.SetParent(null);

                    if (generatedRoom.TryGetComponent<DungeonPart>(out DungeonPart dungeonPart))
                    {
                        if (dungeonPart.HasAvailableEntrypoint(out Transform room2Entrypoint))
                        {
                            generatedRooms.Add(dungeonPart);
                            doorToAlign.transform.position = room1Entrypoint.transform.position;
                            doorToAlign.transform.rotation = room1Entrypoint.transform.rotation;
                            AlignRooms(randomGeneratedRoom.transform, generatedRoom.transform, room1Entrypoint, room2Entrypoint);
                            Debug.Log($"Aligned room {generatedRoom.name}.");

                            if (HandleIntersection(dungeonPart))
                            {
                                Debug.LogWarning($"Intersection detected for room {generatedRoom.name}. Retrying placement.");
                                dungeonPart.UnuseEntrypoint(room2Entrypoint);
                                randomGeneratedRoom.UnuseEntrypoint(room1Entrypoint);
                                RetryPlacement(generatedRoom, doorToAlign, 0);
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }

    private void GenerateAlternateEntrances()
    {
        if (alternateEntrances.Count < 1) return;

        Debug.Log("Generating alternate entrances.");
        for (int i = 0; i < alternateEntrances.Count; i++)
        {
            DungeonPart randomGeneratedRoom = null;
            Transform room1Entrypoint = null;
            int totalRetries = 100;
            int retryIndex = 0;

            while (randomGeneratedRoom == null && retryIndex < totalRetries)
            {
                int randomLinkRoomIndex = Random.Range(0, generatedRooms.Count);
                DungeonPart roomToTest = generatedRooms[randomLinkRoomIndex];
                if (roomToTest.HasAvailableEntrypoint(out room1Entrypoint))
                {
                    randomGeneratedRoom = roomToTest;
                    Debug.Log($"Found available entry point in room {roomToTest.name}.");
                    break;
                }
                retryIndex++;
            }

            if (randomGeneratedRoom == null)
            {
                Debug.LogWarning("Failed to find a room with an available entry point after 100 retries.");
                continue;
            }

            int randomIndex = Random.Range(0, alternateEntrances.Count);
            GameObject generatedRoom = Instantiate(alternateEntrances[randomIndex], transform.position, transform.rotation);
            generatedRoom.transform.SetParent(null);

            GameObject doorToAlign = Instantiate(door, transform.position, transform.rotation);

            if (generatedRoom.TryGetComponent<DungeonPart>(out DungeonPart dungeonPart))
            {
                if (dungeonPart.HasAvailableEntrypoint(out Transform room2Entrypoint))
                {
                    generatedRooms.Add(dungeonPart);
                    doorToAlign.transform.position = room1Entrypoint.transform.position;
                    doorToAlign.transform.rotation = room1Entrypoint.transform.rotation;
                    AlignRooms(randomGeneratedRoom.transform, generatedRoom.transform, room1Entrypoint, room2Entrypoint);
                    Debug.Log($"Placed alternate entrance {generatedRoom.name}.");

                    if (HandleIntersection(dungeonPart))
                    {
                        Debug.LogWarning($"Intersection detected for alternate entrance {generatedRoom.name}. Retrying placement.");
                        dungeonPart.UnuseEntrypoint(room2Entrypoint);
                        randomGeneratedRoom.UnuseEntrypoint(room1Entrypoint);
                        RetryPlacement(generatedRoom, doorToAlign, 0);
                        continue;
                    }
                }
            }
        }
    }

    private void RetryPlacement(GameObject itemToPlace, GameObject doorToPlace, int retryCount)
    {
        if (retryCount >= 100)
        {
            Debug.LogWarning("Retry limit reached for placing item.");
            return;
        }

        Debug.Log($"Retrying placement for {itemToPlace.name}, attempt {retryCount + 1}.");
        DungeonPart randomGeneratedRoom = null;
        Transform room1Entrypoint = null;
        int totalRetries = 100;
        int retryIndex = 0;

        while (randomGeneratedRoom == null && retryIndex < totalRetries)
        {
            int randomLinkRoomIndex = Random.Range(0, generatedRooms.Count - 1);
            DungeonPart roomToTest = generatedRooms[randomLinkRoomIndex];
            if (roomToTest.HasAvailableEntrypoint(out room1Entrypoint))
            {
                randomGeneratedRoom = roomToTest;
                Debug.Log($"Found available entry point in room {roomToTest.name}.");
                break;
            }
            retryIndex++;
        }

        if (randomGeneratedRoom == null)
        {
            Debug.LogWarning("Failed to find a room with an available entry point after 100 retries.");
            return;
        }

        if (itemToPlace.TryGetComponent<DungeonPart>(out DungeonPart dungeonPart))
        {
            if (dungeonPart.HasAvailableEntrypoint(out Transform room2Entrypoint))
            {
                doorToPlace.transform.position = room1Entrypoint.transform.position;
                doorToPlace.transform.rotation = room1Entrypoint.transform.rotation;
                AlignRooms(randomGeneratedRoom.transform, itemToPlace.transform, room1Entrypoint, room2Entrypoint);
                Debug.Log($"Aligned {itemToPlace.name}.");

                if (HandleIntersection(dungeonPart))
                {
                    Debug.LogWarning($"Intersection detected for {itemToPlace.name}. Retrying placement.");
                    dungeonPart.UnuseEntrypoint(room2Entrypoint);
                    randomGeneratedRoom.UnuseEntrypoint(room1Entrypoint);
                    RetryPlacement(itemToPlace, doorToPlace, retryCount + 1);
                }
            }
        }
    }

    private void FillEmptyEntrances()
    {
        Debug.Log("Filling empty entrances.");
        generatedRooms.ForEach(room => room.FillEmptyDoors());
    }

    private bool HandleIntersection(DungeonPart dungeonPart)
    {
        bool didIntersect = false;
        Collider[] hits = Physics.OverlapBox(dungeonPart.collider.bounds.center, dungeonPart.collider.bounds.size / 2, Quaternion.identity, roomsLayermask);

        foreach (Collider hit in hits)
        {
            if (hit == dungeonPart.collider) continue;

            if (hit != dungeonPart.collider)
            {
                didIntersect = true;
                Debug.LogWarning($"Intersection detected with {hit.name}.");
                break;
            }
        }

        return didIntersect;
    }

    private void AlignRooms(Transform room1, Transform room2, Transform room1Entry, Transform room2Entry)
    {
        float angle = Vector3.Angle(room1Entry.forward, room2Entry.forward);

        room2.TransformPoint(room2Entry.position);
        room2.eulerAngles = new Vector3(room2.eulerAngles.x, room2.eulerAngles.y + angle, room2.eulerAngles.z);

        Vector3 offset = room1Entry.position - room2Entry.position;

        room2.position += offset;
        Physics.SyncTransforms();

        Debug.Log($"Aligned {room2.name} to {room1.name}.");
    }

    public List<DungeonPart> GetGeneratedRooms() => generatedRooms;

    public bool IsGenerated() => isGenerated;
}
