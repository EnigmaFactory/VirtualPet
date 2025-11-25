using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Centralized room ID constants to prevent mismatches
/// </summary>
public static class RoomIds
{
    public const string StarterApartment = "starter_apartment";
    public const string LivingRoom = "living_room";
    public const string GardenPatio = "garden_patio";
    public const string Bedroom = "bedroom";
    public const string CatCafe = "cat_cafe";
    public const string LuxuryPenthouse = "luxury_penthouse";

    /// <summary>
    /// Get room ID for a given RoomType
    /// </summary>
    public static string GetRoomId(RoomType type)
    {
        switch (type)
        {
            case RoomType.StarterApartment: return StarterApartment;
            case RoomType.LivingRoom: return LivingRoom;
            case RoomType.GardenPatio: return GardenPatio;
            case RoomType.Bedroom: return Bedroom;
            case RoomType.CatCafe: return CatCafe;
            case RoomType.LuxuryPenthouse: return LuxuryPenthouse;
            default: return type.ToString().ToLower();
        }
    }
}

/// <summary>
/// Room types with different costs and benefits
/// </summary>
public enum RoomType
{
    StarterApartment,   // Free, 2 cats
    LivingRoom,         // 150 hearts, 3 cats
    GardenPatio,        // 400 hearts, 4 cats
    Bedroom,            // 600 hearts, 3 cats (sleep-focused)
    CatCafe,            // 1000 hearts OR $4.99, 5 cats
    LuxuryPenthouse     // $9.99 only, 6 cats
}

/// <summary>
/// Furniture categories
/// </summary>
public enum FurnitureType
{
    Bed,            // Cats sleep here, +generation
    Toy,            // Cats play here, +coins when active
    FoodBowl,       // Slows affection decay
    WaterBowl,      // Slows affection decay
    LitterBox,      // Required, needs cleaning
    ScratchPost,    // Cats use periodically
    CatTree,        // Multi-purpose climbing structure
    Decoration      // Aesthetic + small generation bonus
}

/// <summary>
/// Room configuration and state
/// </summary>
[Serializable]
public class RoomData
{
    // Identity
    public string id;                           // e.g., "starter_apartment", "living_room"
    public string displayName;                  // e.g., "Starter Apartment"
    public RoomType roomType;

    // Configuration
    public bool unlocked = false;
    public int maxCats = 2;                     // How many cats can be in this room
    public int maxFurniture = 15;               // How many furniture pieces
    public float generationBonus = 1f;          // Room-wide multiplier (1.5 = +50%)

    // Costs
    public int heartCost = 0;                   // 0 for starter
    public float premiumCost = 0f;              // USD, 0 if not premium

    // Furniture in this room
    public List<PlacedFurniture> furniture = new List<PlacedFurniture>();

    // Litter box status (required for cat happiness!)
    public LitterBoxStatus litterBox = new LitterBoxStatus();

    // Unlock timestamp
    public long unlockedAt = 0;

    /// <summary>
    /// Get furniture set bonuses (matching furniture gives bonuses)
    /// </summary>
    public Dictionary<string, int> GetFurnitureSets()
    {
        Dictionary<string, int> setCounts = new Dictionary<string, int>();

        foreach (var item in furniture)
        {
            if (!string.IsNullOrEmpty(item.setName))
            {
                if (!setCounts.ContainsKey(item.setName))
                    setCounts[item.setName] = 0;

                setCounts[item.setName]++;
            }
        }

        return setCounts;
    }

    /// <summary>
    /// Calculate total room generation multiplier including furniture bonuses
    /// </summary>
    public float GetTotalGenerationMultiplier()
    {
        float multiplier = generationBonus;

        // Set bonuses (5+ matching furniture = +20%)
        var sets = GetFurnitureSets();
        foreach (var set in sets)
        {
            if (set.Value >= 5)
            {
                multiplier += 0.2f;
            }
        }

        // Litter box cleanliness affects generation
        multiplier *= litterBox.GetCleanlinessMultiplier();

        return multiplier;
    }

    /// <summary>
    /// Check if room has required furniture (litter box)
    /// </summary>
    public bool HasLitterBox()
    {
        foreach (var item in furniture)
        {
            if (item.furnitureType == FurnitureType.LitterBox)
                return true;
        }
        return false;
    }
}

/// <summary>
/// A piece of furniture placed in a room
/// </summary>
[Serializable]
public class PlacedFurniture
{
    public string id;                       // Unique instance ID
    public string furnitureId;              // Reference to furniture definition
    public string displayName;              // e.g., "Cozy Cat Bed"
    public FurnitureType furnitureType;
    public string setName;                  // e.g., "Modern Set", null if no set

    // Position in room (for 3D placement)
    public Vector3 position;
    public Quaternion rotation;

    // Addressable asset reference (for lazy loading)
    public string addressableKey;

    // Stats
    public float generationBonus = 0f;      // Individual item bonus
    public bool isFavoriteOfCat = false;    // Set at runtime if cat loves this item

    // Placement timestamp
    public long placedAt;

    public PlacedFurniture(string furnitureId, FurnitureType type, Vector3 pos)
    {
        this.id = Guid.NewGuid().ToString();
        this.furnitureId = furnitureId;
        this.furnitureType = type;
        this.position = pos;
        this.rotation = Quaternion.identity;
        this.placedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Parameterless constructor for deserialization
    public PlacedFurniture() { }
}

/// <summary>
/// Litter box cleaning status (mini-game mechanic)
/// </summary>
[Serializable]
public class LitterBoxStatus
{
    public long lastCleanedAt;
    public float cleanliness = 100f;        // 0-100, decays over time per cat

    private const float CLEAN_DURATION_HOURS = 8f; // Lasts 8 hours per cat

    /// <summary>
    /// Update cleanliness based on time and number of cats
    /// </summary>
    public void UpdateCleanliness(int catsInRoom)
    {
        if (catsInRoom == 0) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float hoursSinceClean = (now - lastCleanedAt) / (1000f * 60f * 60f);

        // Decay rate increases with more cats
        float decayRate = (100f / CLEAN_DURATION_HOURS) * catsInRoom;
        cleanliness = Mathf.Max(0, 100f - (hoursSinceClean * decayRate));
    }

    /// <summary>
    /// Clean the litter box (mini-game completion)
    /// </summary>
    public void Clean()
    {
        cleanliness = 100f;
        lastCleanedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Get generation multiplier based on cleanliness
    /// </summary>
    public float GetCleanlinessMultiplier()
    {
        // 100% clean = 1.5x generation
        // 50% clean = 1.0x generation
        // 0% clean = 0.5x generation
        return 0.5f + (cleanliness / 100f);
    }

    /// <summary>
    /// Check if needs cleaning (below 50%)
    /// </summary>
    public bool NeedsCleaning()
    {
        return cleanliness < 50f;
    }
}

/// <summary>
/// Static configuration for room types
/// </summary>
public static class RoomConfig
{
    public static RoomData CreateRoom(RoomType type)
    {
        RoomData room = new RoomData
        {
            id = RoomIds.GetRoomId(type), // Use centralized room ID
            roomType = type,
            unlocked = (type == RoomType.StarterApartment) // Starter is free
        };

        switch (type)
        {
            case RoomType.StarterApartment:
                room.displayName = "Starter Apartment";
                room.maxCats = 2;
                room.maxFurniture = 15;
                room.generationBonus = 1f;
                room.heartCost = 0;
                room.premiumCost = 0f;
                break;

            case RoomType.LivingRoom:
                room.displayName = "Living Room";
                room.maxCats = 3;
                room.maxFurniture = 25;
                room.generationBonus = 1.5f; // +50%
                room.heartCost = 150;
                room.premiumCost = 0f;
                break;

            case RoomType.GardenPatio:
                room.displayName = "Garden Patio";
                room.maxCats = 4;
                room.maxFurniture = 30;
                room.generationBonus = 2f; // +100%
                room.heartCost = 400;
                room.premiumCost = 0f;
                break;

            case RoomType.Bedroom:
                room.displayName = "Cozy Bedroom";
                room.maxCats = 3;
                room.maxFurniture = 20;
                room.generationBonus = 1.3f; // Sleep-focused
                room.heartCost = 600;
                room.premiumCost = 0f;
                break;

            case RoomType.CatCafe:
                room.displayName = "Cat Cafe";
                room.maxCats = 5;
                room.maxFurniture = 40;
                room.generationBonus = 2.5f;
                room.heartCost = 1000;
                room.premiumCost = 4.99f; // OR hearts
                break;

            case RoomType.LuxuryPenthouse:
                room.displayName = "Luxury Penthouse";
                room.maxCats = 6;
                room.maxFurniture = 50;
                room.generationBonus = 3f;
                room.heartCost = 0; // Premium only
                room.premiumCost = 9.99f;
                break;
        }

        return room;
    }

    public static List<RoomType> GetAllRoomTypes()
    {
        return new List<RoomType>
        {
            RoomType.StarterApartment,
            RoomType.LivingRoom,
            RoomType.GardenPatio,
            RoomType.Bedroom,
            RoomType.CatCafe,
            RoomType.LuxuryPenthouse
        };
    }
}
