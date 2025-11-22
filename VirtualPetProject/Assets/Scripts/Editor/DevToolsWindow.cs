using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for testing game systems
/// Open via: Window > Virtual Pet > Dev Tools
/// </summary>
public class DevToolsWindow : EditorWindow
{
    private GameManager gameManager;
    private FirebaseManager firebaseManager;

    private string catName = "TestCat";
    private CatPersonality selectedPersonality = CatPersonality.Playful;
    private CatRarity selectedRarity = CatRarity.Common;

    private Vector2 scrollPosition;

    [MenuItem("Window/Virtual Pet/Dev Tools")]
    public static void ShowWindow()
    {
        var window = GetWindow<DevToolsWindow>("Dev Tools");
        window.minSize = new Vector2(400, 600);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Get references
        if (Application.isPlaying)
        {
            gameManager = GameManager.Instance;
            firebaseManager = FirebaseManager.Instance;
        }

        GUILayout.Label("🛠️ Virtual Pet Dev Tools", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use dev tools", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (gameManager == null || firebaseManager == null)
        {
            EditorGUILayout.HelpBox("GameManager or FirebaseManager not found in scene!", MessageType.Error);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawPlayerStats();
        GUILayout.Space(10);
        DrawCurrencyTools();
        GUILayout.Space(10);
        DrawCatTools();
        GUILayout.Space(10);
        DrawTestingTools();

        EditorGUILayout.EndScrollView();
    }

    void DrawPlayerStats()
    {
        GUILayout.Label("📊 Player Stats", EditorStyles.boldLabel);

        if (gameManager.PlayerProfile != null)
        {
            var profile = gameManager.PlayerProfile;

            EditorGUILayout.LabelField("Name:", profile.displayName);
            EditorGUILayout.LabelField("Coins:", profile.coins.ToString());
            EditorGUILayout.LabelField("Hearts:", profile.hearts.ToString());
            EditorGUILayout.LabelField("Prestige:", profile.prestigePoints.ToString());
            EditorGUILayout.LabelField("Cats:", $"{profile.cats.Count}/{profile.GetMaxCatCapacity()}");
        }
        else
        {
            EditorGUILayout.HelpBox("Player not loaded yet", MessageType.Warning);
        }
    }

    void DrawCurrencyTools()
    {
        GUILayout.Label("💰 Currency", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 100 Coins"))
        {
            gameManager.PlayerProfile.coins += 100;
            Debug.Log("💰 Added 100 coins");
        }
        if (GUILayout.Button("+ 50 Hearts"))
        {
            gameManager.PlayerProfile.hearts += 50;
            Debug.Log("❤️ Added 50 hearts");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 500 Coins"))
        {
            gameManager.PlayerProfile.coins += 500;
            Debug.Log("💰 Added 500 coins");
        }
        if (GUILayout.Button("+ 200 Hearts"))
        {
            gameManager.PlayerProfile.hearts += 200;
            Debug.Log("❤️ Added 200 hearts");
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawCatTools()
    {
        GUILayout.Label("🐱 Cat Management", EditorStyles.boldLabel);

        // Cat creation parameters
        catName = EditorGUILayout.TextField("Cat Name:", catName);
        selectedPersonality = (CatPersonality)EditorGUILayout.EnumPopup("Personality:", selectedPersonality);
        selectedRarity = (CatRarity)EditorGUILayout.EnumPopup("Rarity:", selectedRarity);

        if (GUILayout.Button("Adopt Cat (Free)"))
        {
            var cat = new CatData(
                System.Guid.NewGuid().ToString(),
                catName,
                selectedPersonality,
                selectedRarity,
                CatSource.Generated
            );

            // Free adoption (bypass cost)
            bool success = gameManager.AdoptCat(cat, "starter_apartment", 0);

            if (success)
            {
                Debug.Log($"✅ Adopted {cat.name}");
            }
            else
            {
                Debug.LogWarning("❌ Adoption failed - room full?");
            }
        }

        GUILayout.Space(5);

        // List all cats
        if (gameManager.PlayerProfile?.cats.Count > 0)
        {
            GUILayout.Label("Current Cats:", EditorStyles.miniLabel);

            foreach (var cat in gameManager.PlayerProfile.cats.Values)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField($"🐱 {cat.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Personality:", $"{cat.personality} ({cat.rarity})");
                EditorGUILayout.LabelField("State:", cat.isSleeping ? "😴 Sleeping" : cat.currentState.ToString());
                EditorGUILayout.LabelField("Affection:", $"{cat.affection:F0}/100");
                EditorGUILayout.LabelField("Hunger:", $"{cat.hunger:F0}/100");
                EditorGUILayout.LabelField("Energy:", $"{cat.energy:F0}/100");

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Feed"))
                {
                    gameManager.FeedCat(cat.id, "dry_food", 0); // Free
                    Debug.Log($"🍽️ Fed {cat.name}");
                }

                if (GUILayout.Button("Pet"))
                {
                    gameManager.PetCat(cat.id, 3f);
                    Debug.Log($"🤗 Petted {cat.name}");
                }

                if (cat.isSleeping)
                {
                    if (GUILayout.Button("Wake Up"))
                    {
                        gameManager.WakeUpCat(cat.id);
                        Debug.Log($"😾 Woke up {cat.name}");
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("Awake");
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No cats adopted yet", MessageType.Info);
        }
    }

    void DrawTestingTools()
    {
        GUILayout.Label("🧪 Testing", EditorStyles.boldLabel);

        if (GUILayout.Button("Simulate 1 Hour Offline"))
        {
            if (gameManager.PlayerProfile?.cats.Count > 0)
            {
                long oneHourAgo = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (60 * 60 * 1000);

                foreach (var cat in gameManager.PlayerProfile.cats.Values)
                {
                    cat.lastCollectTime = oneHourAgo;
                }

                var reward = gameManager.PlayerProfile.CalculateTotalOfflineReward();
                gameManager.PlayerProfile.ApplyOfflineRewards();

                Debug.Log($"⭐ Offline reward: {reward.heartsEarned} hearts ({reward.hoursAway:F1} hours)");
            }
            else
            {
                Debug.LogWarning("Need cats to test offline progression!");
            }
        }

        if (GUILayout.Button("Simulate 8 Hours Offline"))
        {
            if (gameManager.PlayerProfile?.cats.Count > 0)
            {
                long eightHoursAgo = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (8 * 60 * 60 * 1000);

                foreach (var cat in gameManager.PlayerProfile.cats.Values)
                {
                    cat.lastCollectTime = eightHoursAgo;
                }

                var reward = gameManager.PlayerProfile.CalculateTotalOfflineReward();
                gameManager.PlayerProfile.ApplyOfflineRewards();

                Debug.Log($"⭐ Offline reward: {reward.heartsEarned} hearts ({reward.hoursAway:F1} hours)");
            }
        }

        if (GUILayout.Button("Print Full Stats to Console"))
        {
            if (gameManager.PlayerProfile != null)
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
                             $"Hunger: {cat.hunger:F0}, Energy: {cat.energy:F0}, " +
                             $"Sleeping: {cat.isSleeping}, State: {cat.currentState}");
                }
            }
        }
    }

    void OnInspectorUpdate()
    {
        // Refresh window every frame when in play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
