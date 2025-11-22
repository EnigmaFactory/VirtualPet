using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Personality types that affect cat behavior and generation rates
/// </summary>
public enum CatPersonality
{
    Playful,    // Generates more with toys, loves active play
    Lazy,       // Generates more while sleeping, low maintenance
    Curious,    // Explores furniture, finds bonus coins
    Social,     // Generates more with other cats, great for multiplayer
    Shy         // Slow generation but rare/valuable, needs patience
}

/// <summary>
/// Current activity state of the cat
/// </summary>
public enum CatState
{
    Sleeping,       // High generation, can't interact (unless woken)
    Idle,           // Standing/sitting, available for interaction
    Grooming,       // Medium generation, looks cute
    Exploring,      // Interacting with furniture/toys
    Watching,       // Stares at cursor/other cats
    Playing,        // With player or solo with toys
    Eating,         // At food bowl
    BeingPetted,    // Player interaction
    Following       // Cat-takeover mode or following player
}

/// <summary>
/// Rarity affects generation rate and adoption cost
/// </summary>
public enum CatRarity
{
    Common,     // 1x generation
    Rare,       // 2x generation
    Epic,       // 3x generation
    Legendary   // 5x generation (premium only)
}

/// <summary>
/// Where the cat came from
/// </summary>
public enum CatSource
{
    Generated,      // Fresh, system-generated
    Released,       // From another player's prestige release
    Runaway,        // From hardcore mode neglect
    Premium         // Purchased with real money
}

/// <summary>
/// Core cat data model - server-authoritative
/// </summary>
[Serializable]
public class CatData
{
    // Identity
    public string id;                       // Unique cat ID (Firebase key)
    public string name;                     // Display name
    public CatPersonality personality;
    public CatRarity rarity;
    public CatSource source;

    // Appearance (for Red Deer Cat Family Pack integration)
    public string coatType;                 // e.g., "tabby", "calico", "black"
    public string eyeColor;                 // e.g., "green", "blue", "yellow"
    public int variantIndex;                // Which Red Deer model variant

    // Stats (0-100 scale)
    public float affection = 50f;           // Primary stat, affects generation
    public float hunger = 50f;              // Increases over time, feed to reduce
    public float energy = 100f;             // Decreases with play, increases with sleep

    // Gameplay flags
    public bool isHardcore = false;         // Opted into runaway risk
    public CatState currentState = CatState.Idle;
    public string currentRoom = "starter_apartment";

    // Timestamps (Unix milliseconds for Firebase compatibility)
    public long adoptedAt;                  // When player got this cat
    public long lastCareTime;               // Last feed/play/pet interaction
    public long lastCollectTime;            // Last time hearts were collected
    public long lastFeedTime;               // Tracks feeding schedule
    public long lastPlayTime;               // Tracks play schedule

    // Sleep system
    public bool isSleeping = false;
    public long sleepStartTime;             // When current nap started
    public int sleepCycleHours = 14;        // Personality-dependent (12-16)

    // Preferences (learned over time)
    public string preferredFood;            // e.g., "wet_food", "dry_food"
    public string favoriteToy;              // Furniture ID they love
    public List<string> preferredRooms = new List<string>(); // Where they like to wander

    // Progression
    public int daysOwned = 0;               // For prestige calculation
    public float totalHeartsGenerated = 0f; // Stat tracking

    // Temporary buffs/debuffs
    public List<TemporaryEffect> activeEffects = new List<TemporaryEffect>();

    // Provenance (for released/runaway cats)
    public string previousOwner;            // Display name of player who released/neglected
    public string backstory;                // Emotional flavor text

    // Constructor for new cat
    public CatData(string catId, string catName, CatPersonality catPersonality, CatRarity catRarity, CatSource catSource)
    {
        id = catId;
        name = catName;
        personality = catPersonality;
        rarity = catRarity;
        source = catSource;

        // Set personality-specific defaults
        switch (personality)
        {
            case CatPersonality.Lazy:
                sleepCycleHours = 16;
                preferredFood = "dry_food";
                break;
            case CatPersonality.Playful:
                sleepCycleHours = 12;
                preferredFood = "wet_food";
                break;
            case CatPersonality.Curious:
                sleepCycleHours = 13;
                break;
            default:
                sleepCycleHours = 14;
                break;
        }

        // Initialize timestamps
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        adoptedAt = now;
        lastCareTime = now;
        lastCollectTime = now;
        lastFeedTime = now;
        lastPlayTime = now;
    }

    // Parameterless constructor for deserialization
    public CatData() { }

    /// <summary>
    /// Calculate base heart generation rate (hearts per hour)
    /// </summary>
    public float GetBaseGenerationRate()
    {
        float baseRate = 5f; // Default: 5 hearts/hour

        // Rarity multiplier
        switch (rarity)
        {
            case CatRarity.Common: baseRate *= 1f; break;
            case CatRarity.Rare: baseRate *= 2f; break;
            case CatRarity.Epic: baseRate *= 3f; break;
            case CatRarity.Legendary: baseRate *= 5f; break;
        }

        // Personality modifier
        switch (personality)
        {
            case CatPersonality.Lazy:
                if (isSleeping) baseRate *= 2f; // Double while sleeping!
                break;
            case CatPersonality.Playful:
                if (currentState == CatState.Playing) baseRate *= 1.5f;
                break;
            case CatPersonality.Curious:
                if (currentState == CatState.Exploring) baseRate *= 1.3f;
                break;
        }

        // Affection multiplier (0.5x at 0 affection, 1.5x at 100 affection)
        float affectionMultiplier = 0.5f + (affection / 100f);
        baseRate *= affectionMultiplier;

        // Sleep bonus (all cats generate 1.5x while sleeping)
        if (isSleeping && personality != CatPersonality.Lazy) // Lazy already got 2x
        {
            baseRate *= 1.5f;
        }

        return baseRate;
    }

    /// <summary>
    /// Calculate hearts earned during offline time
    /// </summary>
    public OfflineReward CalculateOfflineReward()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long timeDeltaMs = now - lastCollectTime;
        float hoursAway = timeDeltaMs / (1000f * 60f * 60f);

        // Cap at 24 hours to prevent exploits
        float cappedHours = Mathf.Min(hoursAway, 24f);

        // Calculate affection decay (0.5 per hour)
        float affectionDecay = cappedHours * 0.5f;
        float startAffection = affection;
        float endAffection = Mathf.Max(0, startAffection - affectionDecay);

        // Average affection over the period (for fair calculation)
        float avgAffection = (startAffection + endAffection) / 2f;

        // Temporarily set affection to average for generation calculation
        float originalAffection = affection;
        affection = avgAffection;
        float generationRate = GetBaseGenerationRate();
        affection = originalAffection; // Restore

        // Calculate total hearts
        float heartsEarned = generationRate * cappedHours;

        // Apply temporary effects
        foreach (var effect in activeEffects)
        {
            if (effect.IsActive())
            {
                heartsEarned *= effect.generationMultiplier;
            }
        }

        return new OfflineReward
        {
            heartsEarned = Mathf.FloorToInt(heartsEarned),
            newAffection = endAffection,
            hoursAway = cappedHours,
            wasCapped = hoursAway > 24f
        };
    }

    /// <summary>
    /// Apply the offline reward (call after showing player)
    /// </summary>
    public void ApplyOfflineReward(OfflineReward reward)
    {
        affection = reward.newAffection;
        totalHeartsGenerated += reward.heartsEarned;
        lastCollectTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Update days owned
        daysOwned = Mathf.FloorToInt((lastCollectTime - adoptedAt) / (1000f * 60f * 60f * 24f));
    }

    /// <summary>
    /// Wake up sleeping cat (player action)
    /// </summary>
    public void WakeUp()
    {
        if (!isSleeping) return;

        isSleeping = false;
        currentState = CatState.Idle;

        // Affection penalty
        affection = Mathf.Max(0, affection - 2f);

        // Add groggy debuff
        activeEffects.Add(new TemporaryEffect
        {
            name = "Groggy",
            generationMultiplier = 0.5f,
            durationMs = 10 * 60 * 1000, // 10 minutes
            startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    /// <summary>
    /// Feed the cat
    /// </summary>
    public void Feed(float amount, bool isPreferredFood)
    {
        hunger = Mathf.Max(0, hunger - amount);

        if (isPreferredFood)
        {
            affection = Mathf.Min(100, affection + 15f);
        }
        else
        {
            affection = Mathf.Min(100, affection + 5f);
        }

        lastFeedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lastCareTime = lastFeedTime;
    }

    /// <summary>
    /// Play with cat
    /// </summary>
    public void Play(float playAmount, bool isPreferredToy)
    {
        float affectionGain = isPreferredToy ? 35f : 20f;
        affection = Mathf.Min(100, affection + affectionGain);

        energy = Mathf.Max(0, energy - playAmount * 0.5f);

        lastPlayTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lastCareTime = lastPlayTime;
    }

    /// <summary>
    /// Pet the cat (short interaction)
    /// </summary>
    public void Pet(float duration)
    {
        float affectionGain = duration * 2f; // 2 affection per second
        affection = Mathf.Min(100, affection + affectionGain);

        lastCareTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

/// <summary>
/// Temporary buff or debuff on a cat
/// </summary>
[Serializable]
public class TemporaryEffect
{
    public string name;                     // e.g., "Enrichment Bonus", "Groggy"
    public float generationMultiplier = 1f; // Affects heart generation
    public long startTime;                  // Unix milliseconds
    public long durationMs;                 // How long it lasts

    public bool IsActive()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return (now - startTime) < durationMs;
    }

    public float GetRemainingSeconds()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long remaining = durationMs - (now - startTime);
        return Mathf.Max(0, remaining / 1000f);
    }
}

/// <summary>
/// Result of offline progression calculation
/// </summary>
public struct OfflineReward
{
    public int heartsEarned;
    public float newAffection;
    public float hoursAway;
    public bool wasCapped;
}
