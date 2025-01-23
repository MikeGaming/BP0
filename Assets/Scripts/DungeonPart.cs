using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DungeonPart : MonoBehaviour
{
    public enum DungeonPartType
    {
        Room,
        Hallway
    }

    [SerializeField] private LayerMask roomLayermask;
    [SerializeField] private DungeonPartType dungeonPartType;
    [SerializeField] private GameObject fillerWall;
    public List<Transform> entryPoints;
    public new Collider collider;

    public bool HasAvailableEntrypoint(out Transform entrypoint)
    {
        Transform resultingEntry = null;
        bool result = false;

        int totalRetries = 100;
        int retryIndex = 0;
        if (entryPoints.Count == 1)
        {
            Transform entry = entryPoints[0];
            if (entry.TryGetComponent<EntryPoint>(out EntryPoint res))
            {
                if (res.IsOccupied())
                {
                    Debug.Log($"Entry point {entry.name} is occupied.");
                    result = false;
                    resultingEntry = null;
                }
                else
                {
                    Debug.Log($"Entry point {entry.name} is available.");
                    result = true;
                    resultingEntry = entry;
                    res.SetOccupied();
                }
                entrypoint = resultingEntry;
                return result;
            }
        }

        while (resultingEntry == null && retryIndex < totalRetries)
        {
            int randomEntryIndex = Random.Range(0, entryPoints.Count);
            Transform entry = entryPoints[randomEntryIndex];

            if (entry.TryGetComponent<EntryPoint>(out EntryPoint entryPoint))
            {
                if (!entryPoint.IsOccupied())
                {
                    Debug.Log($"Entry point {entry.name} is available.");
                    resultingEntry = entry;
                    result = true;
                    entryPoint.SetOccupied();
                    break;
                }
                else
                {
                    Debug.Log($"Entry point {entry.name} is occupied.");
                }
            }
            else
            {
                Debug.LogWarning($"Entry point {entry.name} does not have an EntryPoint component.");
            }
            retryIndex++;
        }

        if (resultingEntry == null)
        {
            Debug.LogWarning("Failed to find an available entry point after 50 retries.");
        }

        entrypoint = resultingEntry;
        return result;
    }

    public void UnuseEntrypoint(Transform entrypoint)
    {
        if (entrypoint.TryGetComponent<EntryPoint>(out EntryPoint entry))
        {
            Debug.Log($"Unusing entry point {entrypoint.name}.");
            entry.SetOccupied(false);
        }
        else
        {
            Debug.LogWarning($"Entry point {entrypoint.name} does not have an EntryPoint component.");
        }
    }

    public void FillEmptyDoors()
    {
        entryPoints.ForEach((entry) =>
        {
            if (entry.TryGetComponent(out EntryPoint entryPoint))
            {
                if (!entryPoint.IsOccupied())
                {
                    Debug.Log($"Filling empty door at entry point {entry.name}.");
                    GameObject wall = Instantiate(fillerWall);
                    wall.transform.position = entry.transform.position;
                    wall.transform.rotation = entry.transform.rotation;
                }
                else
                {
                    Debug.Log($"Entry point {entry.name} is already occupied.");
                }
            }
            else
            {
                Debug.LogWarning($"Entry point {entry.name} does not have an EntryPoint component.");
            }
        });
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }
}
