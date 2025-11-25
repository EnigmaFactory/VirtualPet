using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RoomFurnitureSpawner : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private FurnitureCatalog furnitureCatalog;
    [SerializeField] private Transform roomRoot;
    [SerializeField] private bool rebuildOnEnable = true;
    [SerializeField] private bool logSpawns;

    readonly Dictionary<string, GameObject> spawnedFurniture = new Dictionary<string, GameObject>();
    readonly Dictionary<string, AsyncOperationHandle<GameObject>> addressableHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (roomRoot == null)
        {
            roomRoot = transform;
        }
    }

    void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnRoomChanged += HandleRoomChanged;
        }

        if (rebuildOnEnable && gameManager != null)
        {
            BuildRoom(gameManager.ActiveRoom);
        }
    }

    void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnRoomChanged -= HandleRoomChanged;
        }

        ClearFurniture();
    }

    void HandleRoomChanged(RoomData room)
    {
        BuildRoom(room);
    }

    public void BuildRoom(RoomData room)
    {
        ClearFurniture();

        if (room == null || room.furniture == null) return;

        foreach (var furniture in room.furniture)
        {
            SpawnFurniture(furniture);
        }
    }

    void SpawnFurniture(PlacedFurniture placedFurniture)
    {
        if (placedFurniture == null) return;

        FurnitureDefinition definition = null;
        if (furnitureCatalog != null)
        {
            furnitureCatalog.TryGetDefinition(placedFurniture.furnitureId, out definition);
        }

        string spawnKey = !string.IsNullOrEmpty(placedFurniture.addressableKey)
            ? placedFurniture.addressableKey
            : definition?.addressableKey;

        Quaternion rotation = placedFurniture.rotation;
        if (rotation == Quaternion.identity && definition != null)
        {
            rotation = Quaternion.Euler(definition.defaultEulerRotation);
        }

        Vector3 localPosition = placedFurniture.position;
        if (definition != null)
        {
            localPosition += definition.defaultPositionOffset;
        }

        if (!string.IsNullOrEmpty(spawnKey))
        {
            var handle = Addressables.InstantiateAsync(spawnKey, Vector3.zero, Quaternion.identity, roomRoot);
            addressableHandles[placedFurniture.id] = handle;
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    InitializeInstance(op.Result, placedFurniture, definition, localPosition, rotation);
                }
                else
                {
                    Debug.LogWarning($"Failed to spawn addressable furniture: {spawnKey}");
                    addressableHandles.Remove(placedFurniture.id);
                }
            };
        }
        else if (definition?.prefabFallback != null)
        {
            var instance = Instantiate(definition.prefabFallback, roomRoot);
            InitializeInstance(instance, placedFurniture, definition, localPosition, rotation);
        }
        else
        {
            Debug.LogWarning($"Furniture '{placedFurniture.furnitureId}' has no addressable key or prefab fallback.");
        }
    }

    void InitializeInstance(GameObject instance, PlacedFurniture data, FurnitureDefinition definition, Vector3 localPosition, Quaternion rotation)
    {
        if (instance == null) return;

        spawnedFurniture[data.id] = instance;
        instance.name = $"{data.displayName ?? data.furnitureId}_{data.id.Substring(0, 6)}";
        instance.transform.SetParent(roomRoot, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = rotation;

        if (definition != null && definition.defaultScale != Vector3.zero)
        {
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, definition.defaultScale);
        }

        var furnitureInstance = instance.GetComponent<RoomFurnitureInstance>();
        if (furnitureInstance == null)
        {
            furnitureInstance = instance.AddComponent<RoomFurnitureInstance>();
        }

        furnitureInstance.Initialize(data, definition);

        if (logSpawns)
        {
            Debug.Log($"Spawned furniture: {data.displayName} ({data.furnitureId})");
        }
    }

    public void ClearFurniture()
    {
        foreach (var handle in addressableHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        addressableHandles.Clear();

        foreach (var instance in spawnedFurniture.Values)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        spawnedFurniture.Clear();
    }
}

