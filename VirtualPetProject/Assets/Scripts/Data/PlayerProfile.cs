using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Player's complete game state and progression
/// </summary>
[Serializable]
public class PlayerProfile
{
    // Firebase Authentication
    public string userId;                       // Firebase Auth UID
    public string displayName;                  // Google display name
    public string email;                        // Google email
    public string photoUrl;                     // Google profile picture

    // Currency
    public int coins = 100;                     // Active play currency (starts with 100)
    public int hearts = 0;                      // Idle currency (earned over time)

    // Prestige system
    public int prestigePoints = 0;              // Permanent progression
    public List<int> prestigeMilestonesUnlocked = new List<int>(); // e.g., [1, 5, 10]

    // Timestamps
    public long accountCreatedAt;
    public long lastLoginAt;
    public long totalPlayTimeMs = 0;            // Lifetime play time

    // Cats owned
    public Dictionary<string, CatData> cats = new Dictionary<string, CatData>();

    // Rooms
    public Dictionary<string, RoomData> rooms = new Dictionary<string, RoomData>();
    public string activeRoomId;

    // Stats tracking
    public PlayerStats stats = new PlayerStats();

    // Daily tasks
    public DailyTasks dailyTasks = new DailyTasks();

    // Achievements
    public List<string> unlockedAchievements = new List<string>();

    // Settings
    public PlayerSettings settings = new PlayerSettings();

    // Multiplayer
    public List<string> friendIds = new List<string>();     // Firebase UIDs of friends
    public PlayDateStatus currentPlayDate;                  // Active play date, if any

    /// <summary>
    /// Initialize new player profile
    /// </summary>
    public PlayerProfile(string firebaseUserId, string name, string userEmail)
    {
        userId = firebaseUserId;
        displayName = name;
        email = userEmail;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        accountCreatedAt = now;
        lastLoginAt = now;

        // Create starter room
        RoomData starterRoom = RoomConfig.CreateRoom(RoomType.StarterApartment);
        starterRoom.unlocked = true;
        starterRoom.unlockedAt = now;
        rooms[starterRoom.id] = starterRoom;

        // Give starter furniture (basic necessities)
        AddStarterFurniture(starterRoom);
        activeRoomId = starterRoom.id;
    }

    // Parameterless constructor for deserialization
    public PlayerProfile() { }

    /// <summary>
    /// Add starter furniture to first room
    /// </summary>
    private void AddStarterFurniture(RoomData room)
    {
        if (!TryAddFurnitureFromCatalog(room))
        {
            AddLegacyStarterFurniture(room);
        }

        room.litterBox.Clean();
    }

    bool TryAddFurnitureFromCatalog(RoomData room)
    {
        var catalog = FurnitureCatalogProvider.Catalog;
        if (catalog == null || catalog.Entries == null) return false;

        bool placedAny = false;
        foreach (var definition in catalog.Entries)
        {
            if (definition == null || !definition.includeInStarterSet || definition.starterPlacements == null) continue;

            foreach (var placement in definition.starterPlacements)
            {
                if (placement == null) continue;
                if (placement.roomType != room.roomType) continue;

                var furniture = new PlacedFurniture(definition.furnitureId, definition.furnitureType, placement.localPosition);
                furniture.rotation = Quaternion.Euler(placement.localEuler);
                furniture.displayName = definition.displayName;
                furniture.setName = string.IsNullOrEmpty(definition.defaultSetName) ? null : definition.defaultSetName;
                furniture.generationBonus = definition.defaultGenerationBonus;
                furniture.addressableKey = definition.addressableKey;
                room.furniture.Add(furniture);
                placedAny = true;
            }
        }

        return placedAny;
    }

    void AddLegacyStarterFurniture(RoomData room)
    {
        room.furniture.Add(new PlacedFurniture("litter_box_basic", FurnitureType.LitterBox, new Vector3(0, 0, 0)));
        room.furniture.Add(new PlacedFurniture("food_bowl_basic", FurnitureType.FoodBowl, new Vector3(1, 0, 0)));
        room.furniture.Add(new PlacedFurniture("water_bowl_basic", FurnitureType.WaterBowl, new Vector3(1.5f, 0, 0)));
        room.furniture.Add(new PlacedFurniture("bed_basic", FurnitureType.Bed, new Vector3(-2, 0, 0)));
        room.furniture.Add(new PlacedFurniture("toy_ball", FurnitureType.Toy, new Vector3(2, 0, 1)));
    }

    /// <summary>
    /// Get total number of cats owned across all rooms
    /// </summary>
    public int GetTotalCatCount()
    {
        return cats.Count;
    }

    /// <summary>
    /// Get maximum cat capacity across all unlocked rooms
    /// </summary>
    public int GetMaxCatCapacity()
    {
        int total = 0;
        foreach (var room in rooms.Values)
        {
            if (room.unlocked)
                total += room.maxCats;
        }
        return total;
    }

    /// <summary>
    /// Check if player can adopt another cat
    /// </summary>
    public bool CanAdoptCat()
    {
        return GetTotalCatCount() < GetMaxCatCapacity();
    }

    public RoomData GetActiveRoom()
    {
        if (rooms == null || rooms.Count == 0) return null;

        if (string.IsNullOrEmpty(activeRoomId) || !rooms.ContainsKey(activeRoomId))
        {
            activeRoomId = GetFirstUnlockedRoomId();
        }

        if (string.IsNullOrEmpty(activeRoomId)) return null;

        rooms.TryGetValue(activeRoomId, out var room);
        return room;
    }

    public string GetFirstUnlockedRoomId()
    {
        if (rooms == null || rooms.Count == 0) return null;

        var unlockedRoom = rooms.Values.FirstOrDefault(r => r.unlocked);
        if (unlockedRoom != null) return unlockedRoom.id;

        return rooms.Values.First().id;
    }

    public bool SetActiveRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId) || rooms == null) return false;
        if (!rooms.ContainsKey(roomId)) return false;

        var room = rooms[roomId];
        if (!room.unlocked) return false;

        if (activeRoomId == roomId) return false;

        activeRoomId = roomId;
        return true;
    }

    /// <summary>
    /// Add a new cat to player's collection
    /// </summary>
    public bool AdoptCat(CatData cat, string roomId)
    {
        if (!CanAdoptCat()) return false;
        if (!rooms.ContainsKey(roomId)) return false;
        if (!rooms[roomId].unlocked) return false;

        // Count cats in target room
        int catsInRoom = 0;
        foreach (var c in cats.Values)
        {
            if (c.currentRoom == roomId) catsInRoom++;
        }

        if (catsInRoom >= rooms[roomId].maxCats) return false;

        cat.currentRoom = roomId;
        cats[cat.id] = cat;

        stats.catsAdopted++;
        if (cat.source == CatSource.Runaway) stats.catsRescued++;

        return true;
    }

    /// <summary>
    /// Release cat for prestige
    /// </summary>
    public int ReleaseCatForPrestige(string catId)
    {
        if (!cats.ContainsKey(catId)) return 0;

        CatData cat = cats[catId];

        // Calculate prestige points
        int basePoints = cat.rarity switch
        {
            CatRarity.Common => 1,
            CatRarity.Rare => 3,
            CatRarity.Epic => 5,
            CatRarity.Legendary => 10,
            _ => 1
        };

        int affectionBonus = Mathf.FloorToInt(cat.affection / 20f); // 0-5 bonus
        int timeBonus = cat.daysOwned / 7; // 1 point per week
        int hardcoreBonus = cat.isHardcore ? 2 : 0;

        int totalPrestige = basePoints + affectionBonus + timeBonus + hardcoreBonus;

        prestigePoints += totalPrestige;
        stats.catsReleased++;

        // Remove from player
        cats.Remove(catId);

        // Check for prestige milestones
        CheckPrestigeMilestones();

        return totalPrestige;
    }

    /// <summary>
    /// Check and unlock prestige milestones
    /// </summary>
    private void CheckPrestigeMilestones()
    {
        int[] milestones = { 1, 5, 10, 25, 50, 100 };

        foreach (int milestone in milestones)
        {
            if (prestigePoints >= milestone && !prestigeMilestonesUnlocked.Contains(milestone))
            {
                prestigeMilestonesUnlocked.Add(milestone);
                // Unlock benefits handled in PrestigeManager
            }
        }
    }

    /// <summary>
    /// Calculate total hearts generated by all cats
    /// </summary>
    public OfflineReward CalculateTotalOfflineReward()
    {
        int totalHearts = 0;
        float maxHours = 0;
        bool wasCapped = false;

        foreach (var cat in cats.Values)
        {
            OfflineReward catReward = cat.CalculateOfflineReward();

            // Apply room multiplier
            if (rooms.ContainsKey(cat.currentRoom))
            {
                float roomMultiplier = rooms[cat.currentRoom].GetTotalGenerationMultiplier();
                catReward.heartsEarned = Mathf.FloorToInt(catReward.heartsEarned * roomMultiplier);
            }

            totalHearts += catReward.heartsEarned;
            maxHours = Mathf.Max(maxHours, catReward.hoursAway);
            wasCapped = wasCapped || catReward.wasCapped;
        }

        return new OfflineReward
        {
            heartsEarned = totalHearts,
            hoursAway = maxHours,
            wasCapped = wasCapped
        };
    }

    /// <summary>
    /// Apply offline rewards to all cats and player
    /// </summary>
    public void ApplyOfflineRewards()
    {
        foreach (var cat in cats.Values)
        {
            OfflineReward reward = cat.CalculateOfflineReward();

            // Apply room multiplier
            if (rooms.ContainsKey(cat.currentRoom))
            {
                float roomMultiplier = rooms[cat.currentRoom].GetTotalGenerationMultiplier();
                reward.heartsEarned = Mathf.FloorToInt(reward.heartsEarned * roomMultiplier);
            }

            cat.ApplyOfflineReward(reward);
            hearts += reward.heartsEarned;
        }

        // Update last login
        lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Check for runaway cats (hardcore mode)
    /// </summary>
    public List<CatData> CheckForRunawayCats()
    {
        List<CatData> runaways = new List<CatData>();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        List<string> toRemove = new List<string>();

        foreach (var cat in cats.Values)
        {
            if (!cat.isHardcore) continue;

            float daysSinceLastCare = (now - cat.lastCareTime) / (1000f * 60f * 60f * 24f);

            // Run away if neglected for 7 days AND affection is low
            if (daysSinceLastCare > 7f && cat.affection < 20f)
            {
                runaways.Add(cat);
                toRemove.Add(cat.id);
                stats.catsLost++;
            }
        }

        // Remove runaway cats
        foreach (string id in toRemove)
        {
            cats.Remove(id);
        }

        return runaways;
    }
}

/// <summary>
/// Player statistics for tracking
/// </summary>
[Serializable]
public class PlayerStats
{
    public int catsAdopted = 0;
    public int catsReleased = 0;
    public int catsRescued = 0;
    public int catsLost = 0; // Runaways

    public int litterBoxCleans = 0;
    public int feedingSessions = 0;
    public int playSessions = 0;
    public int pettingSessions = 0;

    public int enrichmentCount = 0; // Redecorating while cat sleeps
    public int furniturePlaced = 0;

    public int roomsUnlocked = 1; // Starts with starter room

    public int friendsAdded = 0;
    public int playDatesCompleted = 0;
}

/// <summary>
/// Daily task tracking (resets at midnight UTC)
/// </summary>
[Serializable]
public class DailyTasks
{
    public long lastResetAt;

    public bool fedAllCats = false;
    public bool cleanedLitterBox = false;
    public bool playedWithCat = false;
    public bool visitedFriendRoom = false;

    public int coinsEarnedToday = 0;
    public int heartsEarnedToday = 0;

    /// <summary>
    /// Check if tasks need to reset (new day)
    /// </summary>
    public void CheckReset()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long todayMidnight = GetTodayMidnightUtc();

        if (lastResetAt < todayMidnight)
        {
            ResetTasks();
        }
    }

    private void ResetTasks()
    {
        fedAllCats = false;
        cleanedLitterBox = false;
        playedWithCat = false;
        visitedFriendRoom = false;
        coinsEarnedToday = 0;
        heartsEarnedToday = 0;
        lastResetAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long GetTodayMidnightUtc()
    {
        DateTime today = DateTime.UtcNow.Date;
        return new DateTimeOffset(today).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Check if all daily tasks completed
    /// </summary>
    public bool AllTasksComplete()
    {
        return fedAllCats && cleanedLitterBox && playedWithCat && visitedFriendRoom;
    }
}

/// <summary>
/// Player settings and preferences
/// </summary>
[Serializable]
public class PlayerSettings
{
    public bool soundEnabled = true;
    public bool musicEnabled = true;
    public float masterVolume = 1f;

    public bool notificationsEnabled = true;
    public bool dailyReminderEnabled = true;

    public string languageCode = "en"; // For localization
}

/// <summary>
/// Active play date status
/// </summary>
[Serializable]
public class PlayDateStatus
{
    public string playDateId;
    public List<string> participantIds = new List<string>();
    public long startTime;
    public bool isActive = false;

    public string hostRoomId; // Which room is being shared
}
