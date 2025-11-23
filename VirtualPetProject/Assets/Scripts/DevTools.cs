using UnityEngine;

/// <summary>
/// Development tools for quick testing in Unity Editor
/// Right-click the component in Inspector to run test methods
/// </summary>
public class DevTools : MonoBehaviour
{
    [Header("Test Parameters")]
    [SerializeField] private string testCatName = "TestCat";
    [SerializeField] private CatPersonality testPersonality = CatPersonality.Playful;
    [SerializeField] private CatRarity testRarity = CatRarity.Common;

    private GameManager gameManager;
    private FirebaseManager firebaseManager;
    private CameraController cameraController;

    void Start()
    {
        gameManager = GameManager.Instance;
        firebaseManager = FirebaseManager.Instance;
        cameraController = Camera.main?.GetComponent<CameraController>();
    }

    #region Quick Test Methods (Right-click in Inspector)

    [ContextMenu("1. Sign In (Mock)")]
    void TestSignIn()
    {
        if (firebaseManager == null)
        {
            Debug.LogError("FirebaseManager not found!");
            return;
        }

        firebaseManager.SignInWithGoogle();
        Debug.Log("🧪 Mock sign-in initiated");
    }

    [ContextMenu("2. Adopt Test Cat")]
    void TestAdoptCat()
    {
        if (gameManager == null || gameManager.PlayerProfile == null)
        {
            Debug.LogError("GameManager or PlayerProfile not ready!");
            return;
        }

        var cat = new CatData(
            System.Guid.NewGuid().ToString(),
            testCatName,
            testPersonality,
            testRarity,
            CatSource.Generated
        );

        bool success = gameManager.AdoptCat(cat, "starter_apartment", 50);

        if (success)
        {
            Debug.Log($"✅ Adopted {cat.name} ({cat.personality}, {cat.rarity})");
        }
        else
        {
            Debug.LogWarning("❌ Adoption failed - check hearts/room capacity");
        }
    }

    [ContextMenu("3. Feed First Cat")]
    void TestFeedCat()
    {
        if (gameManager?.PlayerProfile?.cats.Count > 0)
        {
            string firstCatId = GetFirstCatId();
            gameManager.FeedCat(firstCatId, "dry_food", 10);
            Debug.Log($"🍽️ Fed cat {gameManager.PlayerProfile.cats[firstCatId].name}");
        }
        else
        {
            Debug.LogWarning("No cats to feed! Adopt one first.");
        }
    }

    [ContextMenu("4. Pet First Cat")]
    void TestPetCat()
    {
        if (gameManager?.PlayerProfile?.cats.Count > 0)
        {
            string firstCatId = GetFirstCatId();
            gameManager.PetCat(firstCatId, 3f);
            Debug.Log($"🤗 Petted cat {gameManager.PlayerProfile.cats[firstCatId].name}");
        }
        else
        {
            Debug.LogWarning("No cats to pet! Adopt one first.");
        }
    }

    [ContextMenu("5. Wake Up First Cat")]
    void TestWakeCat()
    {
        if (gameManager?.PlayerProfile?.cats.Count > 0)
        {
            string firstCatId = GetFirstCatId();
            var cat = gameManager.PlayerProfile.cats[firstCatId];

            if (cat.isSleeping)
            {
                gameManager.WakeUpCat(firstCatId);
                Debug.Log($"😾 Woke up {cat.name}");
            }
            else
            {
                Debug.Log($"{cat.name} is already awake!");
            }
        }
        else
        {
            Debug.LogWarning("No cats!");
        }
    }

    [ContextMenu("6. Add 100 Coins")]
    void TestAddCoins()
    {
        if (gameManager?.PlayerProfile != null)
        {
            gameManager.PlayerProfile.coins += 100;
            Debug.Log($"💰 Added 100 coins. Total: {gameManager.PlayerProfile.coins}");
        }
    }

    [ContextMenu("7. Add 50 Hearts")]
    void TestAddHearts()
    {
        if (gameManager?.PlayerProfile != null)
        {
            gameManager.PlayerProfile.hearts += 50;
            Debug.Log($"❤️ Added 50 hearts. Total: {gameManager.PlayerProfile.hearts}");
        }
    }

    [ContextMenu("8. Show Player Stats")]
    void TestShowStats()
    {
        if (gameManager?.PlayerProfile != null)
        {
            var profile = gameManager.PlayerProfile;
            Debug.Log("=== PLAYER STATS ===");
            Debug.Log($"Name: {profile.displayName}");
            Debug.Log($"Coins: {profile.coins}");
            Debug.Log($"Hearts: {profile.hearts}");
            Debug.Log($"Prestige: {profile.prestigePoints}");
            Debug.Log($"Cats: {profile.cats.Count}/{profile.GetMaxCatCapacity()}");

            foreach (var cat in profile.cats.Values)
            {
                Debug.Log($"  🐱 {cat.name} ({cat.personality}) - Affection: {cat.affection:F0}, " +
                         $"Sleeping: {cat.isSleeping}, State: {cat.currentState}");
            }
        }
    }

    [ContextMenu("9. Simulate 1 Hour Offline")]
    void TestOfflineProgression()
    {
        if (gameManager?.PlayerProfile?.cats.Count > 0)
        {
            Debug.Log("⏰ Simulating 1 hour offline...");

            // Manually adjust last collect time (hack for testing)
            long oneHourAgo = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (60 * 60 * 1000);

            foreach (var cat in gameManager.PlayerProfile.cats.Values)
            {
                cat.lastCollectTime = oneHourAgo;
            }

            // Process offline rewards
            var reward = gameManager.PlayerProfile.CalculateTotalOfflineReward();
            gameManager.PlayerProfile.ApplyOfflineRewards();

            Debug.Log($"⭐ Offline reward: {reward.heartsEarned} hearts ({reward.hoursAway:F1} hours)");
        }
        else
        {
            Debug.LogWarning("Need cats to test offline progression!");
        }
    }

    // Camera Tests

    [ContextMenu("Camera/Switch to Orbit Mode")]
    void TestOrbitMode()
    {
        if (cameraController != null)
        {
            cameraController.SetMode(CameraMode.Orbit);
            Debug.Log("📷 Camera: Orbit Mode");
        }
        else
        {
            Debug.LogWarning("CameraController not found!");
        }
    }

    [ContextMenu("Camera/Switch to Focus Mode")]
    void TestFocusMode()
    {
        if (cameraController != null)
        {
            cameraController.FocusOnNearestCat();
            Debug.Log("📷 Camera: Focus Mode");
        }
        else
        {
            Debug.LogWarning("CameraController not found!");
        }
    }

    [ContextMenu("Camera/Frame All Cats")]
    void TestFrameAll()
    {
        if (cameraController != null)
        {
            cameraController.FrameAllCats();
            Debug.Log("📷 Camera: Framing all cats");
        }
        else
        {
            Debug.LogWarning("CameraController not found!");
        }
    }

    [ContextMenu("Camera/Refresh Cat List")]
    void TestRefreshCats()
    {
        if (cameraController != null)
        {
            cameraController.RefreshCatList();
            Debug.Log($"📷 Camera: Tracking {cameraController.TrackedCatsCount} cats");
        }
        else
        {
            Debug.LogWarning("CameraController not found!");
        }
    }

    #endregion

    #region Helper Methods

    private string GetFirstCatId()
    {
        if (gameManager?.PlayerProfile?.cats.Count > 0)
        {
            foreach (var id in gameManager.PlayerProfile.cats.Keys)
            {
                return id;
            }
        }
        return null;
    }

    #endregion

    #region Keyboard Shortcuts (in Play Mode)

    void Update()
    {
        if (!Application.isPlaying) return;

        // Quick test shortcuts
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestAdoptCat();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestFeedCat();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestPetCat();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            TestShowStats();
        }

        // Camera shortcuts
        if (Input.GetKeyDown(KeyCode.F5))
        {
            TestOrbitMode();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            TestFocusMode();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            TestFrameAll();
        }
    }

    #endregion
}
