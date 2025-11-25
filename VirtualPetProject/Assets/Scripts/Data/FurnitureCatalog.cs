using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Virtual Pet/Furniture Catalog", fileName = "FurnitureCatalog")]
public class FurnitureCatalog : ScriptableObject
{
    [SerializeField] private List<FurnitureDefinition> entries = new List<FurnitureDefinition>();
    private Dictionary<string, FurnitureDefinition> lookup;

    public IReadOnlyList<FurnitureDefinition> Entries => entries;

    public FurnitureDefinition GetDefinition(string furnitureId)
    {
        if (string.IsNullOrEmpty(furnitureId)) return null;
        EnsureLookup();
        lookup.TryGetValue(furnitureId, out var definition);
        return definition;
    }

    public bool TryGetDefinition(string furnitureId, out FurnitureDefinition definition)
    {
        definition = GetDefinition(furnitureId);
        return definition != null;
    }

    public FurnitureDefinition GetOrCreateDefinition(string furnitureId)
    {
        if (string.IsNullOrEmpty(furnitureId)) return null;
        EnsureLookup();

        if (lookup.TryGetValue(furnitureId, out var existing))
        {
            return existing;
        }

        var created = new FurnitureDefinition { furnitureId = furnitureId };
        entries.Add(created);
        lookup[furnitureId] = created;
        return created;
    }

    public void RefreshLookup()
    {
        EnsureLookup(true);
    }

    void EnsureLookup(bool force = false)
    {
        if (!force && lookup != null && lookup.Count == entries.Count) return;

        lookup = new Dictionary<string, FurnitureDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.furnitureId)) continue;
            lookup[entry.furnitureId] = entry;
        }
    }
}

[Serializable]
public class FurnitureDefinition
{
    [Header("Identity")]
    public string furnitureId = "new_furniture";
    public string displayName = "New Furniture";
    public FurnitureType furnitureType = FurnitureType.Bed;

    [Header("Assets")]
    public string addressableKey;
    public GameObject prefabFallback;

    [Header("Defaults")]
    public Vector3 defaultPositionOffset = Vector3.zero;
    public Vector3 defaultEulerRotation = Vector3.zero;
    public Vector3 defaultScale = Vector3.one;
    public float defaultGenerationBonus;
    public string defaultSetName;

    [Header("Starter Set")]
    public bool includeInStarterSet;
    public List<FurnitureStarterPlacement> starterPlacements = new List<FurnitureStarterPlacement>();

    [Header("Interaction Points")]
    public List<InteractionPointDefinition> interactionPoints = new List<InteractionPointDefinition>();
}

[Serializable]
public class FurnitureStarterPlacement
{
    public RoomType roomType = RoomType.StarterApartment;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEuler = Vector3.zero;
}

[Serializable]
public class InteractionPointDefinition
{
    public string anchorName = "Anchor";
    public InteractionType interactionType = InteractionType.Sit;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEuler = Vector3.zero;
    public bool snapToPosition = true;
    public bool matchRotation = true;
    public int maxOccupants = 1;
    public bool isToy;
    public float playDuration = 5f;
}

