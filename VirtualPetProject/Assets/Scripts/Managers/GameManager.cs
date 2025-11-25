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
    [SerializeField] private CameraController cameraController; // Camera for auto-focus
    [SerializeField] private GameObject catPrefab; // Cat prefab to instantiate

    [Header("Game State")]
    [SerializeField] private bool gameActive = false;
    [SerializeField] private float updateInterval = 1f; // Update cats every second
    [SerializeField] private Vector3 catSpawnPosition = new Vector3(0, 0, 0); // Where to spawn cats

    // Current player data (cached from Firebase)
    private PlayerProfile playerProfile;

    // Active cats (instantiated GameObjects)
    private Dictionary<string, GameObject> activeCatObjects = new Dictionary<string, GameObject>();

    // Events
    public event Action<CatData> OnCatAdopted;
    public event Action<CatData> OnCatReleased;
    public event Action<int> OnCoinsChanged;
    public event Action<int> OnHeartsChanged;
    public event Action<RoomData> OnRoomChanged;

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

        if (string.IsNullOrEmpty(playerProfile.activeRoomId))
        {
            playerProfile.activeRoomId = playerProfile.GetFirstUnlockedRoomId();
        }

        NotifyActiveRoomChanged();

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

        // Spawn all existing cats in scene
        SpawnAllCats();

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
                UpdateCatGameObject(cat);
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
            CatState.Idle, // Double weight for idle - cats should chill more
            CatState.Idle, // Triple weight - they're cats!
            CatState.Exploring,
            CatState.Watching
        };

        // Check if grooming is available (cooldown: 5-10 minutes)
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long timeSinceLastGroom = cat.lastGroomTime > 0 ? (now - cat.lastGroomTime) : long.MaxValue;
        float minutesSinceGroom = timeSinceLastGroom / (1000f * 60f);
        
        if (minutesSinceGroom > 5f) // At least 5 minutes since last groom
        {
            possibleStates.Add(CatState.Grooming);
            // Lazy cats groom more often
            if (cat.personality == CatPersonality.Lazy && minutesSinceGroom > 3f)
            {
                possibleStates.Add(CatState.Grooming);
            }
        }

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
                possibleStates.Add(CatState.Idle); // Lazy cats prefer idle
                // Add grooming if available
                if (minutesSinceGroom > 3f)
                {
                    possibleStates.Add(CatState.Grooming);
                }
                break;
        }

        CatState newState = possibleStates[UnityEngine.Random.Range(0, possibleStates.Count)];
        
        // Track grooming time
        if (newState == CatState.Grooming)
        {
            cat.lastGroomTime = now;
        }
        
        cat.currentState = newState;

        Debug.Log($"🐾 {cat.name} is now {cat.currentState}");

        // Play appropriate animation (will implement with Red Deer)
    }

    #region Player Actions

    /// <summary>
    /// Set the room that should be active in the world
    /// </summary>
    public bool SetActiveRoom(string roomId, bool forceNotify = false)
    {
        if (playerProfile == null || string.IsNullOrEmpty(roomId)) return false;
        if (!playerProfile.rooms.ContainsKey(roomId))
        {
            Debug.LogWarning($"Active room change failed. Unknown room id: {roomId}");
            return false;
        }

        var room = playerProfile.rooms[roomId];
        if (!room.unlocked)
        {
            Debug.LogWarning($"Active room change failed. Room locked: {room.displayName}");
            return false;
        }

        if (playerProfile.activeRoomId == roomId && !forceNotify)
        {
            return true;
        }

        playerProfile.activeRoomId = roomId;
        NotifyActiveRoomChanged();
        SaveGameState();
        return true;
    }

    void NotifyActiveRoomChanged()
    {
        if (playerProfile == null) return;
        var activeRoom = playerProfile.GetActiveRoom();
        OnRoomChanged?.Invoke(activeRoom);
        if (activeRoom != null)
        {
            Debug.Log($"🏠 Active room set to {activeRoom.displayName} ({activeRoom.id})");
        }
    }

    /// <summary>
    /// Adopt a cat from adoption center
    /// </summary>
    public bool AdoptCat(CatData cat, string targetRoomId, int cost)
    {
        // Ensure player profile exists (create default if needed for testing)
        if (playerProfile == null)
        {
            Debug.LogWarning("PlayerProfile is null! Creating default profile for testing...");
            playerProfile = CreateDefaultPlayerProfile();
        }

        // Debug: Check room exists
        Debug.Log($"🔍 Checking adoption - Room '{targetRoomId}' exists: {playerProfile.rooms.ContainsKey(targetRoomId)}");
        if (playerProfile.rooms.Count > 0)
        {
            Debug.Log($"🔍 Available rooms: {string.Join(", ", playerProfile.rooms.Keys)}");
        }

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

        // Ensure room exists before adopting (use centralized room ID)
        // If targetRoomId doesn't match a known room ID, try to find by type or use StarterApartment
        if (!playerProfile.rooms.ContainsKey(targetRoomId))
        {
            // Try to find a room by matching against known room IDs
            string normalizedRoomId = RoomIds.GetRoomId(RoomType.StarterApartment);
            
            // Check if it's a known room ID constant
            if (targetRoomId == RoomIds.StarterApartment || targetRoomId == "starter_apartment")
                normalizedRoomId = RoomIds.StarterApartment;
            else if (targetRoomId == RoomIds.LivingRoom || targetRoomId == "living_room")
                normalizedRoomId = RoomIds.LivingRoom;
            else if (targetRoomId == RoomIds.GardenPatio || targetRoomId == "garden_patio")
                normalizedRoomId = RoomIds.GardenPatio;
            else if (targetRoomId == RoomIds.Bedroom)
                normalizedRoomId = RoomIds.Bedroom;
            else if (targetRoomId == RoomIds.CatCafe || targetRoomId == "cat_cafe")
                normalizedRoomId = RoomIds.CatCafe;
            else if (targetRoomId == RoomIds.LuxuryPenthouse || targetRoomId == "luxury_penthouse")
                normalizedRoomId = RoomIds.LuxuryPenthouse;
            
            if (!playerProfile.rooms.ContainsKey(normalizedRoomId))
            {
                Debug.LogWarning($"⚠️ Room '{targetRoomId}' does not exist! Creating Starter Apartment...");
                var newRoom = RoomConfig.CreateRoom(RoomType.StarterApartment);
                newRoom.unlocked = true;
                newRoom.unlockedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                playerProfile.rooms[normalizedRoomId] = newRoom;
                Debug.Log($"✅ Created missing room: {normalizedRoomId}");
            }
            
            targetRoomId = normalizedRoomId;
        }

        // Deduct cost
        playerProfile.hearts -= cost;
        OnHeartsChanged?.Invoke(playerProfile.hearts);

        // Adopt
        bool success = playerProfile.AdoptCat(cat, targetRoomId);

        if (success)
        {
            Debug.Log($"🎉 Adopted {cat.name}! Welcome home!");
            
            // Spawn cat in scene
            GameObject catObject = SpawnCatInScene(cat);
            
            // Notify camera to focus on new cat (if auto-focus enabled)
            if (catObject != null)
            {
                // Find camera controller if not assigned
                if (cameraController == null)
                {
                    cameraController = FindObjectOfType<CameraController>();
                }
                
                if (cameraController != null && cameraController.autoFocusOnAdopt)
                {
                    cameraController.OnCatAdopted(catObject);
                    Debug.Log($"📷 Camera focusing on newly adopted cat: {cat.name}");
                }
            }

            OnCatAdopted?.Invoke(cat);

            // Save
            SaveGameState();

            return true;
        }
        else
        {
            Debug.LogError($"❌ Adoption failed! Reasons:");
            Debug.LogError($"  - CanAdoptCat: {playerProfile.CanAdoptCat()}");
            Debug.LogError($"  - Room exists: {playerProfile.rooms.ContainsKey(targetRoomId)}");
            if (playerProfile.rooms.ContainsKey(targetRoomId))
            {
                var room = playerProfile.rooms[targetRoomId];
                Debug.LogError($"  - Room unlocked: {room.unlocked}");
                int catsInRoom = playerProfile.cats.Values.Count(c => c.currentRoom == targetRoomId);
                Debug.LogError($"  - Cats in room: {catsInRoom}/{room.maxCats}");
            }
        }

        return false;
    }

    /// <summary>
    /// Spawn cat GameObject in the scene
    /// </summary>
    GameObject SpawnCatInScene(CatData cat)
    {
        if (catPrefab == null)
        {
            Debug.LogWarning("Cat Prefab not assigned in GameManager! Cannot spawn cat.");
            return null;
        }

        // Check if already spawned
        if (activeCatObjects.ContainsKey(cat.id))
        {
            Debug.LogWarning($"Cat {cat.name} already spawned!");
            return activeCatObjects[cat.id];
        }

        // Instantiate cat
        GameObject catInstance = Instantiate(catPrefab, catSpawnPosition, Quaternion.identity);
        catInstance.name = cat.name;

        // Initialize components
        AnimalController animalController = catInstance.GetComponent<AnimalController>();
        if (animalController != null)
        {
            animalController.Initialize(cat);
            animalController.SetState(cat.currentState);
        }

        MovementController movementController = catInstance.GetComponent<MovementController>();
        if (movementController != null)
        {
            movementController.Initialize(cat);
            Debug.Log($"✅ MovementController initialized for {cat.name}");
        }
        else
        {
            Debug.LogError($"❌ MovementController not found on cat prefab! Cat won't be able to move.");
        }

        // Store reference
        activeCatObjects[cat.id] = catInstance;

        Debug.Log($"🐱 Spawned {cat.name} in scene at {catSpawnPosition}");
        
        return catInstance;
    }

    /// <summary>
    /// Spawn all cats that should be active
    /// </summary>
    void SpawnAllCats()
    {
        if (playerProfile == null || playerProfile.cats == null) return;

        foreach (var cat in playerProfile.cats.Values)
        {
            if (!activeCatObjects.ContainsKey(cat.id))
            {
                SpawnCatInScene(cat);
            }
        }
    }

    /// <summary>
    /// Update cat GameObject state to match CatData
    /// </summary>
    void UpdateCatGameObject(CatData cat)
    {
        if (!activeCatObjects.ContainsKey(cat.id)) return;

        GameObject catObj = activeCatObjects[cat.id];
        AnimalController animController = catObj.GetComponent<AnimalController>();
        MovementController moveController = catObj.GetComponent<MovementController>();

        if (animController != null)
        {
            animController.SetState(cat.currentState);
        }
    }

    /// <summary>
    /// Create a default player profile for testing (when Firebase isn't initialized)
    /// </summary>
    PlayerProfile CreateDefaultPlayerProfile()
    {
        var profile = new PlayerProfile();
        profile.displayName = "Test Player";
        profile.coins = 1000;
        profile.hearts = 500;
        profile.prestigePoints = 0;

        // Create starter room using RoomConfig (ID will be normalized automatically)
        var starterRoom = RoomConfig.CreateRoom(RoomType.StarterApartment);
        starterRoom.unlocked = true;
        starterRoom.unlockedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // RoomConfig now generates "starter_apartment" automatically
        profile.rooms[starterRoom.id] = starterRoom;
        profile.activeRoomId = starterRoom.id;
        
        Debug.Log($"✅ Created starter room: {starterRoom.id} (unlocked: {starterRoom.unlocked}, maxCats: {starterRoom.maxCats})");

        Debug.Log("✅ Created default test player profile");
        return profile;
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
            uiManager.UpdateCatStatsDisplay(cat);
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
    public string ActiveRoomId => playerProfile?.activeRoomId;
    public RoomData ActiveRoom => playerProfile?.GetActiveRoom();

    #endregion
}
