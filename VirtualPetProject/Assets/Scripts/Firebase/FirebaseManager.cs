using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Central Firebase manager - handles auth, database, and offline sync
/// Can run in TEST_MODE for development without Firebase
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // Singleton
    public static FirebaseManager Instance { get; private set; }

    // TEST MODE: Set to true to use mock data instead of real Firebase
    // This allows testing game logic before Firebase is set up
    public const bool TEST_MODE = true;

    [Header("Status")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private bool isAuthenticated = false;
    [SerializeField] private string currentUserId;

    // Events
    public event Action<PlayerProfile> OnPlayerDataLoaded;
    public event Action<string> OnAuthenticationChanged;
    public event Action<string> OnError;

    // Current player data (cached)
    private PlayerProfile currentPlayerProfile;

    // Mock data for testing
    private Dictionary<string, object> mockDatabase = new Dictionary<string, object>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        await InitializeFirebase();
    }

    /// <summary>
    /// Initialize Firebase SDK (or mock mode)
    /// </summary>
    public async Task<bool> InitializeFirebase()
    {
        if (isInitialized) return true;

        try
        {
            if (TEST_MODE)
            {
                Debug.Log("🧪 Firebase running in TEST MODE (mock data)");
                Debug.Log("💡 Set FirebaseManager.TEST_MODE = false to use real Firebase");
                isInitialized = true;
                return true;
            }

            // Real Firebase initialization will go here
            // For now, this is a placeholder for when Firebase SDK is imported

            /*
            // Uncomment when Firebase SDK is imported:

            Firebase.FirebaseApp app = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

            if (app.Status == Firebase.DependencyStatus.Available)
            {
                Firebase.FirebaseApp defaultApp = Firebase.FirebaseApp.DefaultInstance;
                Debug.Log("✅ Firebase initialized successfully");
                isInitialized = true;
                return true;
            }
            else
            {
                Debug.LogError($"❌ Firebase initialization failed: {app.Status}");
                OnError?.Invoke($"Firebase initialization failed: {app.Status}");
                return false;
            }
            */

            Debug.LogWarning("⚠️ Firebase SDK not imported yet. See FIREBASE_SETUP.md");
            Debug.Log("🧪 Switching to TEST MODE automatically");
            isInitialized = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Firebase initialization error: {e.Message}");
            OnError?.Invoke(e.Message);
            return false;
        }
    }

    #region Authentication

    /// <summary>
    /// Sign in with Google (or mock sign-in in test mode)
    /// </summary>
    public async Task<bool> SignInWithGoogle()
    {
        if (TEST_MODE)
        {
            return await MockSignIn();
        }

        // Real Google Sign-In will go here
        /*
        try
        {
            Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            Firebase.Auth.Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(idToken, accessToken);

            var authResult = await auth.SignInWithCredentialAsync(credential);

            currentUserId = authResult.User.UserId;
            isAuthenticated = true;

            OnAuthenticationChanged?.Invoke(currentUserId);

            // Load player data
            await LoadPlayerData();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Google Sign-In failed: {e.Message}");
            OnError?.Invoke($"Sign-in failed: {e.Message}");
            return false;
        }
        */

        Debug.LogWarning("Real Google Sign-In not implemented yet. Use TEST_MODE.");
        return false;
    }

    /// <summary>
    /// Mock sign-in for testing
    /// </summary>
    private async Task<bool> MockSignIn()
    {
        await Task.Delay(500); // Simulate network delay

        currentUserId = "test_user_" + SystemInfo.deviceUniqueIdentifier;
        isAuthenticated = true;

        Debug.Log($"🧪 Mock sign-in successful: {currentUserId}");
        OnAuthenticationChanged?.Invoke(currentUserId);

        // Load or create test player data
        await LoadPlayerData();

        return true;
    }

    /// <summary>
    /// Sign out
    /// </summary>
    public void SignOut()
    {
        if (TEST_MODE)
        {
            isAuthenticated = false;
            currentUserId = null;
            currentPlayerProfile = null;
            Debug.Log("🧪 Mock sign-out successful");
        }
        else
        {
            // Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
        }

        OnAuthenticationChanged?.Invoke(null);
    }

    #endregion

    #region Player Data

    /// <summary>
    /// Load player data from database (or create new profile)
    /// </summary>
    public async Task<PlayerProfile> LoadPlayerData()
    {
        if (!isAuthenticated)
        {
            Debug.LogWarning("Cannot load player data: not authenticated");
            return null;
        }

        if (TEST_MODE)
        {
            return await MockLoadPlayerData();
        }

        // Real Firebase database load will go here
        /*
        try
        {
            var dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance
                .GetReference($"users/{currentUserId}/profile");

            var snapshot = await dbRef.GetValueAsync();

            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                currentPlayerProfile = JsonConvert.DeserializeObject<PlayerProfile>(json);

                Debug.Log($"✅ Player data loaded: {currentPlayerProfile.displayName}");
            }
            else
            {
                // Create new player
                currentPlayerProfile = new PlayerProfile(
                    currentUserId,
                    "New Player",
                    "player@example.com"
                );

                await SavePlayerData();
                Debug.Log("✅ New player profile created");
            }

            OnPlayerDataLoaded?.Invoke(currentPlayerProfile);
            return currentPlayerProfile;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player data: {e.Message}");
            OnError?.Invoke($"Data load failed: {e.Message}");
            return null;
        }
        */

        return null;
    }

    /// <summary>
    /// Mock load for testing
    /// </summary>
    private async Task<PlayerProfile> MockLoadPlayerData()
    {
        await Task.Delay(300); // Simulate network

        string key = $"mock_player_{currentUserId}";

        if (mockDatabase.ContainsKey(key))
        {
            string json = mockDatabase[key] as string;
            currentPlayerProfile = JsonConvert.DeserializeObject<PlayerProfile>(json);
            Debug.Log($"🧪 Mock player data loaded: {currentPlayerProfile.displayName}");
        }
        else
        {
            // Create new test player
            currentPlayerProfile = new PlayerProfile(
                currentUserId,
                "Test Player",
                "test@example.com"
            );

            // Give some starter resources for testing
            currentPlayerProfile.coins = 200;
            currentPlayerProfile.hearts = 50;

            await SavePlayerData();
            Debug.Log("🧪 New mock player created");
        }

        OnPlayerDataLoaded?.Invoke(currentPlayerProfile);
        return currentPlayerProfile;
    }

    /// <summary>
    /// Save current player data to database
    /// </summary>
    public async Task<bool> SavePlayerData()
    {
        if (!isAuthenticated || currentPlayerProfile == null)
        {
            Debug.LogWarning("Cannot save: not authenticated or no player data");
            return false;
        }

        if (TEST_MODE)
        {
            return await MockSavePlayerData();
        }

        // Real Firebase save will go here
        /*
        try
        {
            string json = JsonConvert.SerializeObject(currentPlayerProfile);

            var dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance
                .GetReference($"users/{currentUserId}/profile");

            await dbRef.SetRawJsonValueAsync(json);

            Debug.Log("✅ Player data saved");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save player data: {e.Message}");
            OnError?.Invoke($"Save failed: {e.Message}");
            return false;
        }
        */

        return false;
    }

    /// <summary>
    /// Mock save for testing
    /// </summary>
    private async Task<bool> MockSavePlayerData()
    {
        await Task.Delay(200); // Simulate network

        string key = $"mock_player_{currentUserId}";
        string json = JsonConvert.SerializeObject(currentPlayerProfile);
        mockDatabase[key] = json;

        Debug.Log("🧪 Mock player data saved");
        return true;
    }

    /// <summary>
    /// Save specific cat data
    /// </summary>
    public async Task<bool> SaveCatData(CatData cat)
    {
        if (!isAuthenticated) return false;

        if (currentPlayerProfile.cats.ContainsKey(cat.id))
        {
            currentPlayerProfile.cats[cat.id] = cat;
        }
        else
        {
            currentPlayerProfile.cats.Add(cat.id, cat);
        }

        return await SavePlayerData();
    }

    /// <summary>
    /// Save specific room data
    /// </summary>
    public async Task<bool> SaveRoomData(RoomData room)
    {
        if (!isAuthenticated) return false;

        currentPlayerProfile.rooms[room.id] = room;
        return await SavePlayerData();
    }

    #endregion

    #region Global Pools (Adoption Center, Released Cats, Runaways)

    /// <summary>
    /// Add cat to released pool (prestige system)
    /// </summary>
    public async Task<bool> AddToReleasedPool(CatData cat)
    {
        if (TEST_MODE)
        {
            // Mock: just store in memory
            string key = $"released_{cat.id}";
            mockDatabase[key] = JsonConvert.SerializeObject(cat);
            Debug.Log($"🧪 Cat {cat.name} added to mock released pool");
            return true;
        }

        // Real Firebase implementation
        /*
        var dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance
            .GetReference($"globalPools/releasedCats/{cat.id}");

        string json = JsonConvert.SerializeObject(cat);
        await dbRef.SetRawJsonValueAsync(json);

        return true;
        */

        return false;
    }

    /// <summary>
    /// Add cat to runaway pool (hardcore mode)
    /// </summary>
    public async Task<bool> AddToRunawayPool(CatData cat)
    {
        if (TEST_MODE)
        {
            string key = $"runaway_{cat.id}";
            mockDatabase[key] = JsonConvert.SerializeObject(cat);
            Debug.Log($"🧪 Cat {cat.name} added to mock runaway pool");
            return true;
        }

        // Real Firebase implementation
        /*
        var dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance
            .GetReference($"globalPools/runawayCats/{cat.id}");

        string json = JsonConvert.SerializeObject(cat);
        await dbRef.SetRawJsonValueAsync(json);

        return true;
        */

        return false;
    }

    /// <summary>
    /// Get current adoption center batch
    /// </summary>
    public async Task<List<CatData>> GetAdoptionCenterBatch()
    {
        if (TEST_MODE)
        {
            return await MockGenerateAdoptionBatch();
        }

        // Real Firebase implementation
        /*
        var dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance
            .GetReference("globalPools/adoptionCenter/currentBatch");

        var snapshot = await dbRef.GetValueAsync();

        if (snapshot.Exists)
        {
            // Parse and return cats
        }
        */

        return new List<CatData>();
    }

    /// <summary>
    /// Mock adoption center (generates test cats)
    /// </summary>
    private async Task<List<CatData>> MockGenerateAdoptionBatch()
    {
        await Task.Delay(100);

        List<CatData> batch = new List<CatData>();

        string[] names = { "Luna", "Milo", "Bella", "Oliver", "Chloe", "Leo" };
        CatPersonality[] personalities = { CatPersonality.Playful, CatPersonality.Lazy, CatPersonality.Curious };

        for (int i = 0; i < 6; i++)
        {
            var cat = new CatData(
                Guid.NewGuid().ToString(),
                names[i],
                personalities[i % personalities.Length],
                CatRarity.Common,
                CatSource.Generated
            );

            batch.Add(cat);
        }

        Debug.Log($"🧪 Mock adoption center generated {batch.Count} cats");
        return batch;
    }

    #endregion

    #region Public Getters

    public bool IsInitialized => isInitialized;
    public bool IsAuthenticated => isAuthenticated;
    public string CurrentUserId => currentUserId;
    public PlayerProfile CurrentPlayerProfile => currentPlayerProfile;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get server timestamp (Firebase ServerValue.TIMESTAMP equivalent)
    /// </summary>
    public long GetServerTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Calculate offline progression and apply rewards
    /// </summary>
    public async Task<OfflineReward> ProcessOfflineProgression()
    {
        if (currentPlayerProfile == null) return default;

        // Check for runaways first (hardcore mode)
        var runaways = currentPlayerProfile.CheckForRunawayCats();
        foreach (var cat in runaways)
        {
            await AddToRunawayPool(cat);
            Debug.Log($"😿 Cat {cat.name} ran away due to neglect");
        }

        // Calculate and apply offline rewards
        OfflineReward reward = currentPlayerProfile.CalculateTotalOfflineReward();
        currentPlayerProfile.ApplyOfflineRewards();

        // Save updated data
        await SavePlayerData();

        return reward;
    }

    #endregion
}
