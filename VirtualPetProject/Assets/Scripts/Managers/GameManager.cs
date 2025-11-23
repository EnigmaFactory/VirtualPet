using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/// <summary>
/// Main game manager - orchestrates virtual pet game
/// Handles animal AI, idle progression, and player interactions
/// Generic architecture - works with cats, dogs, or other animals
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private UIManager uiManager;

    [Header("Game State")]
    [SerializeField] private bool gameActive = false;
    [SerializeField] private float updateInterval = 1f; // Update cats every second

    // Current player data (cached from Firebase)
    private PlayerProfile playerProfile;

    // Active cats (instantiated GameObjects)
    private Dictionary<string, GameObject> activeCatObjects = new Dictionary<string, GameObject>();

    // Events
    public event Action<CatData> OnCatAdopted;
    public event Action<CatData> OnCatReleased;
    public event Action<int> OnCoinsChanged;
    public event Action<int> OnHeartsChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Get reference to FirebaseManager if not assigned
        if (firebaseManager == null)
        {
            firebaseManager = FirebaseManager.Instance;
        }

        // Subscribe to Firebase events
        if (firebaseManager != null)
        {
            firebaseManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
            firebaseManager.OnAuthenticationChanged += OnAuthStateChanged;
        }

        // Start authentication flow
        StartCoroutine(InitializeGame());
    }

    void OnDestroy()
    {
        if (firebaseManager != null)
        {
            firebaseManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            firebaseManager.OnAuthenticationChanged -= OnAuthStateChanged;
        }
    }

    /// <summary>
    /// Initialize game (wait for Firebase, then authenticate)
    /// </summary>
    IEnumerator InitializeGame()
    {
        Debug.Log("🎮 Initializing Cat Virtual Pet...");

        // Wait for Firebase to initialize
        while (!firebaseManager.IsInitialized)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Auto sign-in (in test mode, this is instant)
        bool signedIn = false;
        firebaseManager.SignInWithGoogle().ContinueWith(task =>
        {
            signedIn = task.Result;
        });

        // Wait for sign-in
        while (!firebaseManager.IsAuthenticated)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("✅ Game initialized and authenticated");
    }

    /// <summary>
    /// Called when player data is loaded from Firebase
    /// </summary>
    void OnPlayerDataLoaded(PlayerProfile profile)
    {
        playerProfile = profile;

        Debug.Log($"👤 Player loaded: {profile.displayName}");
        Debug.Log($"💰 Coins: {profile.coins} | ❤️ Hearts: {profile.hearts}");
        Debug.Log($"🐱 Cats owned: {profile.GetTotalCatCount()}");

        // Process offline progression
        StartCoroutine(ProcessOfflineRewards());
    }

    /// <summary>
    /// Process offline progression (show reward popup, etc.)
    /// </summary>
    IEnumerator ProcessOfflineRewards()
    {
        var rewardTask = firebaseManager.ProcessOfflineProgression();

        yield return new WaitUntil(() => rewardTask.IsCompleted);

        OfflineReward reward = rewardTask.Result;

        if (reward.heartsEarned > 0)
        {
            Debug.Log($"⭐ Offline reward: +{reward.heartsEarned} hearts ({reward.hoursAway:F1} hours away)");

            // Show UI popup (will implement later)
            if (uiManager != null)
            {
                uiManager.UpdateStatus($"Welcome back! Earned {reward.heartsEarned} hearts while away! ⭐");
            }

            OnHeartsChanged?.Invoke(playerProfile.hearts);
        }

        // Check if player has no cats - trigger adoption flow
        if (playerProfile.GetTotalCatCount() == 0)
        {
            Debug.Log("🐾 No cats! Starting adoption flow...");
            // Trigger adoption UI (will implement later)
        }

        // Start game loop
        StartGameLoop();
    }

    /// <summary>
    /// Start main game loop
    /// </summary>
    void StartGameLoop()
    {
        gameActive = true;
        StartCoroutine(GameUpdateLoop());
        StartCoroutine(CatAILoop());

        Debug.Log("▶️ Game loop started");
    }

    /// <summary>
    /// Main game update loop (updates stats, checks sleep, etc.)
    /// </summary>
    IEnumerator GameUpdateLoop()
    {
        while (gameActive)
        {
            yield return new WaitForSeconds(updateInterval);

            if (playerProfile == null) continue;

            // Update all cats
            foreach (var cat in playerProfile.cats.Values)
            {
                UpdateCatStats(cat);
            }

            // Update litter boxes
            UpdateLitterBoxes();

            // Check daily tasks reset
            playerProfile.dailyTasks.CheckReset();

            // Auto-save every 30 seconds
            if (Time.frameCount % 30 == 0)
            {
                SaveGameState();
            }
        }
    }

    /// <summary>
    /// Update individual cat stats (hunger, energy, etc.)
    /// </summary>
    void UpdateCatStats(CatData cat)
    {
        if (cat.isSleeping)
        {
            // Check if sleep cycle is over
            CheckSleepCycle(cat);
        }
        else
        {
            // Increase hunger slowly
            cat.hunger += 0.1f * updateInterval;
            cat.hunger = Mathf.Min(100, cat.hunger);

            // Decrease energy slowly
            if (cat.currentState == CatState.Playing || cat.currentState == CatState.Exploring)
            {
                cat.energy -= 0.5f * updateInterval;
            }
            else
            {
                cat.energy -= 0.1f * updateInterval;
            }
            cat.energy = Mathf.Max(0, cat.energy);

            // Check if should sleep
            if (cat.energy < 20f)
            {
                PutCatToSleep(cat);
            }
        }

        // Decay affection very slowly (0.5 per hour = ~0.00014 per second)
        cat.affection -= 0.00014f * updateInterval;
        cat.affection = Mathf.Max(0, cat.affection);
    }

    /// <summary>
    /// Check if cat's sleep cycle is complete
    /// </summary>
    void CheckSleepCycle(CatData cat)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long sleepDurationMs = now - cat.sleepStartTime;
        float sleepHours = sleepDurationMs / (1000f * 60f * 60f);

        // Wake up after 2-4 hours (realistic cat nap)
        float napDuration = UnityEngine.Random.Range(2f, 4f);

        if (sleepHours >= napDuration)
        {
            WakeCatUp(cat);
        }
    }

    /// <summary>
    /// Put cat to sleep (automatic or player-triggered)
    /// </summary>
    void PutCatToSleep(CatData cat)
    {
        cat.isSleeping = true;
        cat.currentState = CatState.Sleeping;
        cat.sleepStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Debug.Log($"😴 {cat.name} is taking a nap");

        // Play sleep animation (will implement with Red Deer cats)
    }

    /// <summary>
    /// Wake cat up naturally
    /// </summary>
    void WakeCatUp(CatData cat)
    {
        cat.isSleeping = false;
        cat.currentState = CatState.Idle;
        cat.energy = 100f; // Fully rested

        Debug.Log($"🌅 {cat.name} woke up refreshed!");

        // Play wake animation
    }

    /// <summary>
    /// Update all litter boxes
    /// </summary>
    void UpdateLitterBoxes()
    {
        foreach (var room in playerProfile.rooms.Values)
        {
            if (!room.unlocked) continue;

            // Count cats in room
            int catsInRoom = playerProfile.cats.Values.Count(c => c.currentRoom == room.id);

            // Update litter box cleanliness
            room.litterBox.UpdateCleanliness(catsInRoom);
        }
    }

    /// <summary>
    /// Cat AI loop (makes cats do things autonomously)
    /// </summary>
    IEnumerator CatAILoop()
    {
        while (gameActive)
        {
            yield return new WaitForSeconds(5f); // Check every 5 seconds

            if (playerProfile == null) continue;

            foreach (var cat in playerProfile.cats.Values)
            {
                if (cat.isSleeping) continue;

                // Random chance to change state
                if (UnityEngine.Random.value < 0.2f) // 20% chance
                {
                    AssignRandomCatActivity(cat);
                }
            }
        }
    }

    /// <summary>
    /// Assign random activity based on personality
    /// </summary>
    void AssignRandomCatActivity(CatData cat)
    {
        List<CatState> possibleStates = new List<CatState>
        {
            CatState.Idle,
            CatState.Grooming,
            CatState.Exploring,
            CatState.Watching
        };

        // Personality influences behavior
        switch (cat.personality)
        {
            case CatPersonality.Playful:
                possibleStates.Add(CatState.Playing);
                possibleStates.Add(CatState.Playing); // Higher chance
                break;
            case CatPersonality.Curious:
                possibleStates.Add(CatState.Exploring);
                possibleStates.Add(CatState.Exploring);
                break;
            case CatPersonality.Lazy:
                possibleStates.Add(CatState.Idle);
                possibleStates.Add(CatState.Grooming);
                break;
        }

        cat.currentState = possibleStates[UnityEngine.Random.Range(0, possibleStates.Count)];

        Debug.Log($"🐾 {cat.name} is now {cat.currentState}");

        // Play appropriate animation (will implement with Red Deer)
    }

    #region Player Actions

    /// <summary>
    /// Adopt a cat from adoption center
    /// </summary>
    public bool AdoptCat(CatData cat, string targetRoomId, int cost)
    {
        if (!playerProfile.CanAdoptCat())
        {
            Debug.LogWarning("Cannot adopt: room limit reached");
            return false;
        }

        // Check currency
        if (cost > playerProfile.hearts)
        {
            Debug.LogWarning($"Not enough hearts! Need {cost}, have {playerProfile.hearts}");
            return false;
        }

        // Deduct cost
        playerProfile.hearts -= cost;
        OnHeartsChanged?.Invoke(playerProfile.hearts);

        // Adopt
        bool success = playerProfile.AdoptCat(cat, targetRoomId);

        if (success)
        {
            Debug.Log($"🎉 Adopted {cat.name}! Welcome home!");
            OnCatAdopted?.Invoke(cat);

            // Save
            SaveGameState();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Feed a cat
    /// </summary>
    public void FeedCat(string catId, string foodType, int coinCost)
    {
        if (!playerProfile.cats.ContainsKey(catId)) return;

        var cat = playerProfile.cats[catId];

        if (cat.isSleeping)
        {
            if (uiManager != null)
                uiManager.UpdateStatus($"{cat.name} is sleeping! Wake them up first.");
            return;
        }

        // Check coins
        if (coinCost > playerProfile.coins)
        {
            if (uiManager != null)
                uiManager.UpdateStatus("Not enough coins!");
            return;
        }

        // Deduct coins
        playerProfile.coins -= coinCost;
        OnCoinsChanged?.Invoke(playerProfile.coins);

        // Feed
        bool isPreferred = (foodType == cat.preferredFood);
        cat.Feed(30f, isPreferred);

        playerProfile.stats.feedingSessions++;

        Debug.Log($"🍽️ Fed {cat.name} with {foodType}");

        if (uiManager != null)
        {
            string message = isPreferred
                ? $"{cat.name} loves this food! ❤️ +15 affection"
                : $"{cat.name} ate the food. +5 affection";
            uiManager.UpdateStatus(message);
        }

        SaveGameState();
    }

    /// <summary>
    /// Pet a cat (earns coins)
    /// </summary>
    public void PetCat(string catId, float duration)
    {
        if (!playerProfile.cats.ContainsKey(catId)) return;

        var cat = playerProfile.cats[catId];

        if (cat.isSleeping)
        {
            if (uiManager != null)
                uiManager.UpdateStatus($"Shh! {cat.name} is sleeping. They look so peaceful...");
            return;
        }

        // Pet
        cat.Pet(duration);

        // Earn coins (2 coins per second)
        int coinsEarned = Mathf.FloorToInt(duration * 2f);
        playerProfile.coins += coinsEarned;
        OnCoinsChanged?.Invoke(playerProfile.coins);

        playerProfile.stats.pettingSessions++;

        Debug.Log($"🤗 Petted {cat.name} for {duration:F1}s, earned {coinsEarned} coins");

        if (uiManager != null)
            uiManager.UpdateStatus($"{cat.name} purrs happily! +{coinsEarned} coins");

        SaveGameState();
    }

    /// <summary>
    /// Called when cat affection changes (from PettingInteraction or other sources)
    /// </summary>
    public void OnCatAffectionChanged(string catId, float newAffection)
    {
        if (!playerProfile.cats.ContainsKey(catId)) return;

        var cat = playerProfile.cats[catId];
        cat.affection = newAffection;

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateCatStats(cat);
        }

        // Save state
        SaveGameState();
    }

    /// <summary>
    /// Release cat for prestige
    /// </summary>
    public void ReleaseCat(string catId)
    {
        if (!playerProfile.cats.ContainsKey(catId)) return;

        var cat = playerProfile.cats[catId];

        // Calculate prestige
        int prestigeGained = playerProfile.ReleaseCatForPrestige(catId);

        Debug.Log($"⭐ Released {cat.name} for {prestigeGained} prestige points");

        // Add to global released pool
        firebaseManager.AddToReleasedPool(cat);

        OnCatReleased?.Invoke(cat);

        if (uiManager != null)
            uiManager.UpdateStatus($"Released {cat.name} with love. +{prestigeGained} Prestige ⭐");

        SaveGameState();
    }

    /// <summary>
    /// Wake up sleeping cat (player action)
    /// </summary>
    public void WakeUpCat(string catId)
    {
        if (!playerProfile.cats.ContainsKey(catId)) return;

        var cat = playerProfile.cats[catId];

        if (!cat.isSleeping)
        {
            if (uiManager != null)
                uiManager.UpdateStatus($"{cat.name} is already awake!");
            return;
        }

        cat.WakeUp();

        Debug.Log($"😾 Woke up {cat.name} (they're a bit grumpy...)");

        if (uiManager != null)
            uiManager.UpdateStatus($"{cat.name} yawns sleepily... -2 affection (groggy for 10 min)");

        SaveGameState();
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Save current game state to Firebase
    /// </summary>
    async void SaveGameState()
    {
        if (firebaseManager != null && playerProfile != null)
        {
            await firebaseManager.SavePlayerData();
        }
    }

    #endregion

    #region Event Handlers

    void OnAuthStateChanged(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.Log("👋 User signed out");
            gameActive = false;
            playerProfile = null;
        }
    }

    #endregion

    #region Public Getters

    public PlayerProfile PlayerProfile => playerProfile;
    public bool IsGameActive => gameActive;

    #endregion
}
