using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;

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

    // Tab system
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Quick Start", "Cats", "Rooms", "Testing" };

    // Room management
    private string selectedRoomId = "";
    private string pendingRoomSelection = ""; // Defer selection to avoid GUI layout issues
    private RoomType newRoomType = RoomType.LivingRoom;
    private string newRoomCustomId = "";
    private string newRoomDisplayName = "";
    private Vector2 roomScrollPosition;
    private Vector2 furnitureScrollPosition;
    private int selectedFurnitureIndex = -1;
    private FurnitureType newFurnitureType = FurnitureType.Bed;
    private string newFurnitureId = "";
    private string newFurnitureSetName = "";
    private float newFurnitureGenBonus = 0f;
    private FurnitureCatalog furnitureCatalog;
    private string[] catalogFurnitureLabels = Array.Empty<string>();
    private int selectedCatalogDefinitionIndex = -1;
    
    // Cached room values to avoid modifying during GUI
    private string cachedDisplayName = "";
    private RoomType cachedRoomType = RoomType.StarterApartment;
    private bool cachedUnlocked = false;
    private int cachedMaxCats = 2;
    private int cachedMaxFurniture = 15;
    private float cachedGenerationBonus = 1f;
    private int cachedHeartCost = 0;
    private float cachedPremiumCost = 0f;
    private float cachedCleanliness = 100f;

    [MenuItem("Window/Virtual Pet/Dev Tools")]
    public static void ShowWindow()
    {
        var window = GetWindow<DevToolsWindow>("Dev Tools");
        window.minSize = new Vector2(400, 600);
    }

    void OnGUI()
    {
        // Apply pending room selection from previous frame (avoids GUI layout issues)
        if (!string.IsNullOrEmpty(pendingRoomSelection))
        {
            selectedRoomId = pendingRoomSelection;
            pendingRoomSelection = "";
        }

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

        EnsureFurnitureCatalogLoaded();

        // Tab selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        GUILayout.Space(10);

        // Draw selected tab
        switch (selectedTab)
        {
            case 0: // Quick Start
                DrawQuickStartTab();
                break;
            case 1: // Cats
                DrawCatsTab();
                break;
            case 2: // Rooms
                DrawRoomsTab();
                break;
            case 3: // Testing
                DrawTestingTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawQuickStartTab()
    {
        DrawPlayerStats();
        GUILayout.Space(10);
        DrawCurrencyTools();
        GUILayout.Space(10);
        DrawCatTools();
        GUILayout.Space(10);
        DrawCatStateTools();
        GUILayout.Space(10);
        DrawSpawnTools();
    }

    void DrawCatsTab()
    {
        DrawCatTools();
        GUILayout.Space(10);
        DrawCatStateTools();
    }

    void DrawTestingTab()
    {
        DrawTestingTools();
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

        if (gameManager.PlayerProfile == null)
        {
            EditorGUILayout.HelpBox("Player profile not loaded", MessageType.Warning);
            return;
        }

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

        if (gameManager.PlayerProfile == null)
        {
            EditorGUILayout.HelpBox("Player profile not loaded. GameManager will create a default profile when adopting.", MessageType.Info);
        }

        // Cat creation parameters
        catName = EditorGUILayout.TextField("Cat Name:", catName);
        selectedPersonality = (CatPersonality)EditorGUILayout.EnumPopup("Personality:", selectedPersonality);
        selectedRarity = (CatRarity)EditorGUILayout.EnumPopup("Rarity:", selectedRarity);

        if (GUILayout.Button("Adopt Cat (Free)"))
        {
            if (gameManager == null)
            {
                Debug.LogError("GameManager is null!");
                return;
            }

            var cat = new CatData(
                System.Guid.NewGuid().ToString(),
                catName,
                selectedPersonality,
                selectedRarity,
                CatSource.Generated
            );

            // Free adoption (bypass cost)
            bool success = gameManager.AdoptCat(cat, RoomIds.StarterApartment, 0);

            if (success)
            {
                Debug.Log($"✅ Adopted {cat.name} (ID: {cat.id})");
                // Reset cat name field for next adoption
                catName = "TestCat";
            }
            else
            {
                Debug.LogWarning("❌ Adoption failed - check console for details");
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

                EditorGUILayout.LabelField($"🐱 {cat.name} (ID: {cat.id})", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Room:", cat.currentRoom ?? "None");
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

    void DrawCatStateTools()
    {
        GUILayout.Label("🎭 Cat State Testing", EditorStyles.boldLabel);

        // Null checks
        if (gameManager == null || gameManager.PlayerProfile == null)
        {
            EditorGUILayout.HelpBox("GameManager or PlayerProfile not available", MessageType.Warning);
            return;
        }

        if (gameManager.PlayerProfile.cats == null || gameManager.PlayerProfile.cats.Count == 0)
        {
            EditorGUILayout.HelpBox("Adopt a cat first!", MessageType.Info);
            return;
        }

        // Get first cat GameObject
        GameObject firstCat = null;
        var firstCatData = gameManager.PlayerProfile.cats.Values.FirstOrDefault();
        if (firstCatData != null)
        {
            firstCat = GameObject.Find(firstCatData.name);
        }

        if (firstCat == null)
        {
            EditorGUILayout.HelpBox("Cat GameObject not found in scene", MessageType.Warning);
            return;
        }

        AnimalController animController = firstCat.GetComponent<AnimalController>();
        MovementController moveController = firstCat.GetComponent<MovementController>();

        if (animController == null)
        {
            EditorGUILayout.HelpBox("AnimalController not found on cat", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Set State:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Idle"))
        {
            animController.SetState(CatState.Idle);
            Debug.Log("🎭 Set cat to Idle");
        }
        if (GUILayout.Button("Exploring"))
        {
            animController.SetState(CatState.Exploring);
            Debug.Log("🎭 Set cat to Exploring");
        }
        if (GUILayout.Button("Playing"))
        {
            animController.SetState(CatState.Playing);
            Debug.Log("🎭 Set cat to Playing");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Grooming"))
        {
            animController.SetState(CatState.Grooming);
            Debug.Log("🎭 Set cat to Grooming");
        }
        if (GUILayout.Button("Sleeping"))
        {
            animController.SetState(CatState.Sleeping);
            Debug.Log("🎭 Set cat to Sleeping");
        }
        if (GUILayout.Button("Being Petted"))
        {
            animController.SetState(CatState.BeingPetted);
            Debug.Log("🎭 Set cat to Being Petted");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Watching"))
        {
            animController.SetState(CatState.Watching);
            Debug.Log("🎭 Set cat to Watching");
        }
        if (GUILayout.Button("Eating"))
        {
            animController.SetState(CatState.Eating);
            Debug.Log("🎭 Set cat to Eating");
        }
        if (GUILayout.Button("Following"))
        {
            animController.SetState(CatState.Following);
            Debug.Log("🎭 Set cat to Following");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Actions:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Scratch Horiz"))
        {
            animController.TriggerAction("scratch_horiz");
            Debug.Log("🎭 Triggered scratch_horiz");
        }
        if (GUILayout.Button("Scratch Vert"))
        {
            animController.TriggerAction("scratch_vert");
            Debug.Log("🎭 Triggered scratch_vert");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Jump Forward"))
        {
            if (moveController != null && firstCat != null)
            {
                Vector3 jumpTarget = firstCat.transform.position + firstCat.transform.forward * 2f;
                moveController.JumpTo(jumpTarget);
                Debug.Log("🎭 Triggered jump");
            }
        }
        if (GUILayout.Button("Stop Moving"))
        {
            if (moveController != null)
            {
                moveController.StopMovement();
                Debug.Log("🎭 Stopped movement");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Wander"))
        {
            if (moveController != null && firstCat != null)
            {
                // Force movement to a random nearby point
                Vector3 randomPoint = firstCat.transform.position + new Vector3(
                    UnityEngine.Random.Range(-3f, 3f),
                    0,
                    UnityEngine.Random.Range(-3f, 3f)
                );
                moveController.MoveTo(randomPoint, false);
                Debug.Log($"🎭 Forced movement to {randomPoint}");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Movement Speed:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Slow"))
        {
            if (moveController != null)
            {
                moveController.SetMovementSpeed(1f);
                Debug.Log("🎭 Set speed to Slow");
            }
        }
        if (GUILayout.Button("Normal"))
        {
            if (moveController != null)
            {
                moveController.SetMovementSpeed(2f);
                Debug.Log("🎭 Set speed to Normal");
            }
        }
        if (GUILayout.Button("Fast"))
        {
            if (moveController != null)
            {
                moveController.SetMovementSpeed(4f);
                Debug.Log("🎭 Set speed to Fast");
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void DrawSpawnTools()
    {
        GUILayout.Label("🎯 Spawn Objects", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Quick Spawn:", EditorStyles.miniLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Waypoint"))
        {
            SpawnWaypointAtCamera();
        }
        if (GUILayout.Button("Spawn Watch Point"))
        {
            SpawnWatchPointAtCamera();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Scene Objects:", EditorStyles.miniLabel);
        
        // List existing waypoints
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        GameObject[] watchPoints = GameObject.FindGameObjectsWithTag("WatchPoint");
        
        if (waypoints.Length > 0 || watchPoints.Length > 0)
        {
            EditorGUILayout.LabelField($"Waypoints: {waypoints.Length}");
            EditorGUILayout.LabelField($"Watch Points: {watchPoints.Length}");
            
            if (GUILayout.Button("Clear All"))
            {
                ClearAllSpawnedObjects();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No waypoints or watch points in scene", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }

    void SpawnWaypointAtCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("No main camera found!");
            return;
        }

        Vector3 spawnPos = mainCam.transform.position + mainCam.transform.forward * 3f;
        spawnPos.y = 0f; // Put on ground

        GameObject waypoint = new GameObject("Waypoint_" + System.DateTime.Now.ToString("HHmmss"));
        waypoint.transform.position = spawnPos;
        waypoint.tag = "Waypoint";
        
        // Add visual marker
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(waypoint.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.3f;
        sphere.name = "Visual";
        
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.cyan;
        renderer.material = mat;
        
        Collider col = sphere.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        
        // Add InteractionPoint
        InteractionPoint interactionPoint = waypoint.AddComponent<InteractionPoint>();
        // Set interaction type using SerializedObject (since it's private)
        SerializedObject so = new SerializedObject(interactionPoint);
        so.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Play;
        so.ApplyModifiedProperties();
        
        Selection.activeGameObject = waypoint;
        Debug.Log($"✅ Spawned waypoint at {spawnPos}");
    }

    void SpawnWatchPointAtCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("No main camera found!");
            return;
        }

        Vector3 spawnPos = mainCam.transform.position + mainCam.transform.forward * 3f;
        spawnPos.y = 0.5f; // Slightly elevated for watching

        GameObject watchPoint = new GameObject("WatchPoint_" + System.DateTime.Now.ToString("HHmmss"));
        watchPoint.transform.position = spawnPos;
        watchPoint.tag = "WatchPoint";
        
        // Add visual marker
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(watchPoint.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        cube.name = "Visual";
        
        Renderer renderer = cube.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.yellow;
        renderer.material = mat;
        
        Collider col = cube.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        
        // Add InteractionPoint
        InteractionPoint interactionPoint = watchPoint.AddComponent<InteractionPoint>();
        // Set interaction type using SerializedObject (since it's private)
        SerializedObject so = new SerializedObject(interactionPoint);
        so.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Watch;
        so.ApplyModifiedProperties();
        
        Selection.activeGameObject = watchPoint;
        Debug.Log($"✅ Spawned watch point at {spawnPos}");
    }

    void ClearAllSpawnedObjects()
    {
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        GameObject[] watchPoints = GameObject.FindGameObjectsWithTag("WatchPoint");
        
        int count = 0;
        foreach (var obj in waypoints)
        {
            DestroyImmediate(obj);
            count++;
        }
        foreach (var obj in watchPoints)
        {
            DestroyImmediate(obj);
            count++;
        }
        
        Debug.Log($"🗑️ Cleared {count} spawned objects");
    }

    void DrawRoomsTab()
    {
        GUILayout.Label("🏠 Room Management", EditorStyles.boldLabel);

        if (gameManager == null || gameManager.PlayerProfile == null)
        {
            EditorGUILayout.HelpBox("GameManager or PlayerProfile not available", MessageType.Warning);
            return;
        }

        var profile = gameManager.PlayerProfile;
        var activeRoom = profile.GetActiveRoom();

        EditorGUILayout.HelpBox($"Active Room: {(activeRoom != null ? activeRoom.displayName : "None")}", MessageType.Info);
        if (GUILayout.Button("Rebuild Active Room Prefabs"))
        {
            var spawner = FindObjectOfType<RoomFurnitureSpawner>();
            if (spawner != null)
            {
                spawner.BuildRoom(activeRoom);
            }
            else
            {
                Debug.LogWarning("RoomFurnitureSpawner not found in scene. Add one to preview furniture.");
            }
        }

        GUILayout.Space(10);

        // Quick Room Creation
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("⚡ Quick Create Room", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        newRoomType = (RoomType)EditorGUILayout.EnumPopup("Room Type:", newRoomType);
        if (GUILayout.Button("Create & Unlock", GUILayout.Width(120)))
        {
            try
            {
                CreateRoom(newRoomType, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating room: {e.Message}");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create All Room Types"))
        {
            CreateAllRoomTypes();
        }
        if (GUILayout.Button("Unlock All Rooms"))
        {
            UnlockAllRooms();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Check for duplicate rooms and merge them
        CheckAndMergeDuplicateRooms(profile);

        // Room List & Selection
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📋 Rooms", EditorStyles.boldLabel);

        if (profile.rooms == null || profile.rooms.Count == 0)
        {
            EditorGUILayout.HelpBox("No rooms created yet. Use Quick Create above.", MessageType.Info);
        }
        else
        {
            roomScrollPosition = EditorGUILayout.BeginScrollView(roomScrollPosition, GUILayout.Height(150));

            foreach (var room in profile.rooms.Values)
            {
                EditorGUILayout.BeginHorizontal("box");

                bool isSelected = selectedRoomId == room.id;
                bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));
                
                if (newSelected != isSelected)
                {
                    // Defer selection change to avoid GUI layout issues
                    if (newSelected)
                    {
                        pendingRoomSelection = room.id;
                    }
                    else
                    {
                        pendingRoomSelection = "";
                    }
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(room.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"ID: {room.id} | Type: {room.roomType}");
                EditorGUILayout.LabelField($"Unlocked: {(room.unlocked ? "✅" : "❌")} | Cats: {GetCatsInRoom(room.id)}/{room.maxCats} | Furniture: {room.furniture.Count}/{room.maxFurniture}");
                EditorGUILayout.LabelField($"Gen Bonus: {room.generationBonus:F2}x | Total Multiplier: {room.GetTotalGenerationMultiplier():F2}x");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(100));
                if (GUILayout.Button(room.unlocked ? "🔒 Lock" : "🔓 Unlock", GUILayout.Width(80)))
                {
                    room.unlocked = !room.unlocked;
                    Debug.Log($"{(room.unlocked ? "Unlocked" : "Locked")} room: {room.displayName}");
                }
                if (GUILayout.Button("🗑️ Delete", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Delete Room", $"Delete {room.displayName}?", "Yes", "No"))
                    {
                        profile.rooms.Remove(room.id);
                        if (selectedRoomId == room.id) selectedRoomId = "";
                        Debug.Log($"🗑️ Deleted room: {room.displayName}");
                    }
                }
                GUI.enabled = gameManager.PlayerProfile.activeRoomId != room.id;
                if (GUILayout.Button(GUI.enabled ? "Set Active" : "Active", GUILayout.Width(80)))
                {
                    if (gameManager.SetActiveRoom(room.id, true))
                    {
                        Repaint();
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Room Editor (when room is selected)
        if (!string.IsNullOrEmpty(selectedRoomId) && profile.rooms.ContainsKey(selectedRoomId))
        {
            var room = profile.rooms[selectedRoomId];
            
            // Cache values at start of GUI frame
            if (Event.current.type == EventType.Layout)
            {
                cachedDisplayName = room.displayName;
                cachedRoomType = room.roomType;
                cachedUnlocked = room.unlocked;
                cachedMaxCats = room.maxCats;
                cachedMaxFurniture = room.maxFurniture;
                cachedGenerationBonus = room.generationBonus;
                cachedHeartCost = room.heartCost;
                cachedPremiumCost = room.premiumCost;
                cachedCleanliness = room.litterBox.cleanliness;
            }
            
            DrawRoomEditor(room);
            
            // Apply cached changes only on non-layout events
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            {
                room.displayName = cachedDisplayName;
                room.roomType = cachedRoomType;
                room.unlocked = cachedUnlocked;
                room.maxCats = cachedMaxCats;
                room.maxFurniture = cachedMaxFurniture;
                room.generationBonus = cachedGenerationBonus;
                room.heartCost = cachedHeartCost;
                room.premiumCost = cachedPremiumCost;
                room.litterBox.cleanliness = cachedCleanliness;
            }
        }
        else if (profile.rooms.Count > 0)
        {
            EditorGUILayout.HelpBox("Select a room above to edit its properties", MessageType.Info);
        }
    }

    void DrawRoomEditor(RoomData room)
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label($"✏️ Editing: {cachedDisplayName}", EditorStyles.boldLabel);

        // Basic Properties
        EditorGUILayout.BeginVertical("helpbox");
        GUILayout.Label("Basic Properties:", EditorStyles.miniLabel);
        
        // Edit cached values, not room directly
        cachedDisplayName = EditorGUILayout.TextField("Display Name:", cachedDisplayName);
        
        // Room ID is read-only to prevent dictionary key issues
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Room ID:", room.id);
        EditorGUI.EndDisabledGroup();
        
        cachedRoomType = (RoomType)EditorGUILayout.EnumPopup("Room Type:", cachedRoomType);
        cachedUnlocked = EditorGUILayout.Toggle("Unlocked:", cachedUnlocked);
        
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Capacity & Stats
        EditorGUILayout.BeginVertical("helpbox");
        GUILayout.Label("Capacity & Stats:", EditorStyles.miniLabel);
        
        cachedMaxCats = EditorGUILayout.IntField("Max Cats:", cachedMaxCats);
        cachedMaxFurniture = EditorGUILayout.IntField("Max Furniture:", cachedMaxFurniture);
        cachedGenerationBonus = EditorGUILayout.Slider("Generation Bonus:", cachedGenerationBonus, 0.5f, 5f);
        
        // Calculate multiplier using cached values
        float totalMultiplier = cachedGenerationBonus * (0.5f + (cachedCleanliness / 100f));
        EditorGUILayout.LabelField("Total Multiplier:", $"{totalMultiplier:F2}x");
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Costs
        EditorGUILayout.BeginVertical("helpbox");
        GUILayout.Label("Costs:", EditorStyles.miniLabel);
        
        cachedHeartCost = EditorGUILayout.IntField("Heart Cost:", cachedHeartCost);
        cachedPremiumCost = EditorGUILayout.FloatField("Premium Cost ($):", cachedPremiumCost);
        
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Litter Box Status
        EditorGUILayout.BeginVertical("helpbox");
        GUILayout.Label("🧹 Litter Box:", EditorStyles.miniLabel);
        
        EditorGUILayout.LabelField("Cleanliness:", $"{room.litterBox.cleanliness:F1}%");
        EditorGUILayout.LabelField("Needs Cleaning:", room.litterBox.NeedsCleaning() ? "⚠️ Yes" : "✅ No");
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clean Litter Box"))
        {
            cachedCleanliness = 100f;
            room.litterBox.Clean();
            Debug.Log($"🧹 Cleaned litter box in {cachedDisplayName}");
        }
        cachedCleanliness = EditorGUILayout.Slider("Set Cleanliness:", cachedCleanliness, 0f, 100f);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Furniture Management
        EditorGUILayout.BeginVertical("helpbox");
        GUILayout.Label($"🪑 Furniture ({room.furniture.Count}/{room.maxFurniture}):", EditorStyles.miniLabel);

        furnitureScrollPosition = EditorGUILayout.BeginScrollView(furnitureScrollPosition, GUILayout.Height(200));

        int furnitureToRemoveIndex = -1;
        PlacedFurniture furnitureToRemove = null;

        if (room.furniture.Count == 0)
        {
            EditorGUILayout.HelpBox("No furniture in this room", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < room.furniture.Count; i++)
            {
                var furniture = room.furniture[i];
                bool isSelected = selectedFurnitureIndex == i;

                EditorGUILayout.BeginHorizontal("box");
                
                bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));
                
                if (newSelected != isSelected)
                {
                    // Update selection immediately for furniture (smaller scope, less likely to cause issues)
                    if (newSelected)
                    {
                        selectedFurnitureIndex = i;
                    }
                    else
                    {
                        selectedFurnitureIndex = -1;
                    }
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"{furniture.displayName} ({furniture.furnitureType})", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"ID: {furniture.furnitureId}");
                if (!string.IsNullOrEmpty(furniture.setName))
                {
                    EditorGUILayout.LabelField($"Set: {furniture.setName}");
                }
                EditorGUILayout.LabelField($"Position: ({furniture.position.x:F1}, {furniture.position.y:F1}, {furniture.position.z:F1})");
                EditorGUILayout.LabelField($"Gen Bonus: +{furniture.generationBonus:F2}");
                EditorGUILayout.EndVertical();

                bool removeClicked = GUILayout.Button("🗑️", GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(3);

                if (removeClicked)
                {
                    furnitureToRemoveIndex = i;
                    furnitureToRemove = furniture;
                    break;
                }
            }
        }

        EditorGUILayout.EndScrollView();

        if (furnitureToRemoveIndex >= 0 && furnitureToRemove != null)
        {
            room.furniture.RemoveAt(furnitureToRemoveIndex);

            if (selectedFurnitureIndex == furnitureToRemoveIndex)
            {
                selectedFurnitureIndex = -1;
            }
            else if (selectedFurnitureIndex > furnitureToRemoveIndex)
            {
                selectedFurnitureIndex--;
            }

            Debug.Log($"🗑️ Removed furniture: {furnitureToRemove.displayName}");
        }

        // Add Furniture
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Add Furniture:", EditorStyles.miniLabel);
        DrawFurnitureCatalogHelpers();
        
        newFurnitureType = (FurnitureType)EditorGUILayout.EnumPopup("Type:", newFurnitureType);
        newFurnitureId = EditorGUILayout.TextField("Furniture ID:", newFurnitureId);
        if (string.IsNullOrEmpty(newFurnitureId))
        {
            newFurnitureId = $"{newFurnitureType.ToString().ToLower()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }
        
        EditorGUILayout.BeginHorizontal();
        newFurnitureSetName = EditorGUILayout.TextField("Set Name (optional):", newFurnitureSetName);
        newFurnitureGenBonus = EditorGUILayout.FloatField("Gen Bonus:", newFurnitureGenBonus);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add at Origin"))
        {
            AddFurniture(room, Vector3.zero);
        }
        if (GUILayout.Button("Add at Random"))
        {
            Vector3 randomPos = new Vector3(
                UnityEngine.Random.Range(-5f, 5f),
                0f,
                UnityEngine.Random.Range(-5f, 5f)
            );
            AddFurniture(room, randomPos);
        }
        if (GUILayout.Button("Add Starter Set"))
        {
            AddStarterFurnitureSet(room);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Room Actions
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Actions:", EditorStyles.miniLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💾 Save Room Config"))
        {
            SaveRoomConfig(room);
        }
        if (GUILayout.Button("📥 Load Room Config"))
        {
            LoadRoomConfig(room);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Reset to Default"))
        {
            ResetRoomToDefault(room);
        }
        if (GUILayout.Button("📊 Print Room Stats"))
        {
            PrintRoomStats(room);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📦 Apply Catalog Layout"))
        {
            ApplyCatalogLayout(room);
        }
        if (GUILayout.Button("🧪 Spawn Selected Furniture"))
        {
            SpawnSelectedFurniture(room);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    int GetCatsInRoom(string roomId)
    {
        if (gameManager?.PlayerProfile?.cats == null) return 0;
        return gameManager.PlayerProfile.cats.Values.Count(cat => cat.currentRoom == roomId);
    }

    void CreateRoom(RoomType type, bool unlock = false)
    {
        if (gameManager?.PlayerProfile == null) return;

        // Check if room of this type already exists
        var existingRoom = gameManager.PlayerProfile.rooms.Values.FirstOrDefault(r => r.roomType == type);
        if (existingRoom != null)
        {
            Debug.LogWarning($"⚠️ Room type {type} already exists: {existingRoom.id}");
            // Defer selection to avoid GUI layout issues
            pendingRoomSelection = existingRoom.id;
            if (unlock && !existingRoom.unlocked)
            {
                existingRoom.unlocked = true;
                existingRoom.unlockedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Debug.Log($"🔓 Unlocked existing room: {existingRoom.displayName}");
            }
            return;
        }

        var room = RoomConfig.CreateRoom(type);
        if (unlock)
        {
            room.unlocked = true;
            room.unlockedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // Room ID is already set correctly by RoomConfig using RoomIds constants
        gameManager.PlayerProfile.rooms[room.id] = room;
        // Defer selection to avoid GUI layout issues
        pendingRoomSelection = room.id;
        Debug.Log($"✅ Created room: {room.displayName} (ID: {room.id})");
    }

    void CreateAllRoomTypes()
    {
        foreach (RoomType type in RoomConfig.GetAllRoomTypes())
        {
            if (!gameManager.PlayerProfile.rooms.Values.Any(r => r.roomType == type))
            {
                CreateRoom(type, false);
            }
        }
        Debug.Log("✅ Created all room types");
    }

    void UnlockAllRooms()
    {
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var room in gameManager.PlayerProfile.rooms.Values)
        {
            room.unlocked = true;
            if (room.unlockedAt == 0)
            {
                room.unlockedAt = now;
            }
        }
        Debug.Log("🔓 Unlocked all rooms");
    }

    void DrawFurnitureCatalogHelpers()
    {
        EditorGUILayout.BeginVertical("helpbox");
        var newCatalog = (FurnitureCatalog)EditorGUILayout.ObjectField("Catalog Asset:", furnitureCatalog, typeof(FurnitureCatalog), false);
        if (newCatalog != furnitureCatalog)
        {
            furnitureCatalog = newCatalog;
            RefreshCatalogLabels();
        }

        if (furnitureCatalog == null)
        {
            if (GUILayout.Button("Open Furniture Catalog"))
            {
                FurnitureCatalogWindow.ShowWindow();
            }
            EditorGUILayout.EndVertical();
            return;
        }

        if (catalogFurnitureLabels.Length == 0)
        {
            RefreshCatalogLabels();
        }

        if (catalogFurnitureLabels.Length > 0)
        {
            if (selectedCatalogDefinitionIndex < 0)
            {
                selectedCatalogDefinitionIndex = 0;
            }
            selectedCatalogDefinitionIndex = EditorGUILayout.Popup("Catalog Entry:", selectedCatalogDefinitionIndex, catalogFurnitureLabels);
            if (selectedCatalogDefinitionIndex >= 0 && selectedCatalogDefinitionIndex < furnitureCatalog.Entries.Count)
            {
                var selectedDefinition = furnitureCatalog.Entries[selectedCatalogDefinitionIndex];
                if (GUILayout.Button("Use Catalog Defaults"))
                {
                    ApplyCatalogDefaultsToNewFurniture(selectedDefinition);
                }
            }
        }

        if (GUILayout.Button("Open Furniture Catalog"))
        {
            FurnitureCatalogWindow.ShowWindow();
        }

        EditorGUILayout.EndVertical();
    }

    void ApplyCatalogDefaultsToNewFurniture(FurnitureDefinition definition)
    {
        if (definition == null) return;
        newFurnitureId = definition.furnitureId;
        newFurnitureType = definition.furnitureType;
        newFurnitureSetName = definition.defaultSetName;
        newFurnitureGenBonus = definition.defaultGenerationBonus;
    }

    void ApplyDefinitionToPlacedFurniture(PlacedFurniture furniture)
    {
        if (furnitureCatalog == null || furniture == null) return;
        var definition = furnitureCatalog.GetDefinition(furniture.furnitureId);
        if (definition == null) return;

        furniture.displayName = definition.displayName;
        furniture.furnitureType = definition.furnitureType;
        furniture.generationBonus = definition.defaultGenerationBonus;
        if (!string.IsNullOrEmpty(definition.defaultSetName))
        {
            furniture.setName = definition.defaultSetName;
        }
        if (!string.IsNullOrEmpty(definition.addressableKey))
        {
            furniture.addressableKey = definition.addressableKey;
        }
        if (definition.defaultEulerRotation != Vector3.zero)
        {
            furniture.rotation = Quaternion.Euler(definition.defaultEulerRotation);
        }
    }

    void RefreshCatalogLabels()
    {
        if (furnitureCatalog == null || furnitureCatalog.Entries == null || furnitureCatalog.Entries.Count == 0)
        {
            catalogFurnitureLabels = Array.Empty<string>();
            selectedCatalogDefinitionIndex = -1;
            return;
        }

        catalogFurnitureLabels = furnitureCatalog.Entries
            .Select(entry => $"{entry.displayName} ({entry.furnitureId})")
            .ToArray();

        if (selectedCatalogDefinitionIndex >= catalogFurnitureLabels.Length)
        {
            selectedCatalogDefinitionIndex = catalogFurnitureLabels.Length - 1;
        }
    }

    void EnsureFurnitureCatalogLoaded()
    {
        if (furnitureCatalog != null) return;
        string[] guids = AssetDatabase.FindAssets("t:FurnitureCatalog");
        if (guids.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        furnitureCatalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(path);
        FurnitureCatalogProvider.SetCatalog(furnitureCatalog);
        RefreshCatalogLabels();
    }

    void AddFurniture(RoomData room, Vector3 position)
    {
        if (room.furniture.Count >= room.maxFurniture)
        {
            Debug.LogWarning($"⚠️ Room {room.displayName} is at max furniture capacity ({room.maxFurniture})");
            return;
        }

        var furniture = new PlacedFurniture(newFurnitureId, newFurnitureType, position);
        furniture.displayName = $"{newFurnitureType} {room.furniture.Count + 1}";
        furniture.setName = string.IsNullOrEmpty(newFurnitureSetName) ? null : newFurnitureSetName;
        furniture.generationBonus = newFurnitureGenBonus;
        ApplyDefinitionToPlacedFurniture(furniture);

        room.furniture.Add(furniture);
        Debug.Log($"✅ Added {furniture.displayName} to {room.displayName} at {position}");

        // Reset fields
        newFurnitureId = "";
        newFurnitureSetName = "";
        newFurnitureGenBonus = 0f;
    }

    void AddStarterFurnitureSet(RoomData room)
    {
        if (room.HasLitterBox())
        {
            Debug.LogWarning($"⚠️ Room {room.displayName} already has a litter box");
        }
        else
        {
            newFurnitureType = FurnitureType.LitterBox;
            newFurnitureId = "litter_box_basic";
            AddFurniture(room, new Vector3(0, 0, 0));
        }

        newFurnitureType = FurnitureType.FoodBowl;
        newFurnitureId = "food_bowl_basic";
        AddFurniture(room, new Vector3(1, 0, 0));

        newFurnitureType = FurnitureType.WaterBowl;
        newFurnitureId = "water_bowl_basic";
        AddFurniture(room, new Vector3(1.5f, 0, 0));

        newFurnitureType = FurnitureType.Bed;
        newFurnitureId = "bed_basic";
        AddFurniture(room, new Vector3(-2, 0, 0));

        newFurnitureType = FurnitureType.Toy;
        newFurnitureId = "toy_ball";
        AddFurniture(room, new Vector3(2, 0, 1));

        room.litterBox.Clean();
        Debug.Log($"✅ Added starter furniture set to {room.displayName}");
    }

    void ApplyCatalogLayout(RoomData room)
    {
        EnsureFurnitureCatalogLoaded();
        var catalog = furnitureCatalog ?? FurnitureCatalogProvider.Catalog;
        if (catalog == null)
        {
            Debug.LogWarning("Furniture catalog not found. Cannot apply layout.");
            return;
        }

        room.furniture.Clear();
        foreach (var definition in catalog.Entries)
        {
            if (definition == null || definition.starterPlacements == null) continue;
            foreach (var placement in definition.starterPlacements)
            {
                if (placement.roomType != room.roomType) continue;

                var furniture = new PlacedFurniture(definition.furnitureId, definition.furnitureType, placement.localPosition);
                furniture.rotation = Quaternion.Euler(placement.localEuler);
                furniture.displayName = definition.displayName;
                furniture.setName = string.IsNullOrEmpty(definition.defaultSetName) ? null : definition.defaultSetName;
                furniture.generationBonus = definition.defaultGenerationBonus;
                furniture.addressableKey = definition.addressableKey;
                room.furniture.Add(furniture);
            }
        }

        Debug.Log($"📦 Applied catalog layout to {room.displayName}");

        var spawner = FindObjectOfType<RoomFurnitureSpawner>();
        if (spawner != null && room.id == gameManager.ActiveRoomId)
        {
            spawner.BuildRoom(room);
        }
    }

    void SpawnSelectedFurniture(RoomData room)
    {
        if (selectedFurnitureIndex < 0 || selectedFurnitureIndex >= room.furniture.Count)
        {
            Debug.LogWarning("Select furniture entry first.");
            return;
        }

        var spawner = FindObjectOfType<RoomFurnitureSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("RoomFurnitureSpawner not found in scene.");
            return;
        }

        if (room.id != gameManager.ActiveRoomId)
        {
            Debug.LogWarning("Selected room is not active. Set it active before spawning.");
            return;
        }

        spawner.BuildRoom(room);
    }

    void ResetRoomToDefault(RoomData room)
    {
        if (!EditorUtility.DisplayDialog("Reset Room", $"Reset {room.displayName} to default configuration?", "Yes", "No"))
            return;

        RoomType type = room.roomType;
        string oldId = room.id;
        bool wasUnlocked = room.unlocked;

        var defaultRoom = RoomConfig.CreateRoom(type);
        room.displayName = defaultRoom.displayName;
        room.maxCats = defaultRoom.maxCats;
        room.maxFurniture = defaultRoom.maxFurniture;
        room.generationBonus = defaultRoom.generationBonus;
        room.heartCost = defaultRoom.heartCost;
        room.premiumCost = defaultRoom.premiumCost;
        room.id = oldId; // Keep original ID
        room.unlocked = wasUnlocked; // Keep unlock status
        room.furniture.Clear();
        room.litterBox.Clean();

        Debug.Log($"🔄 Reset {room.displayName} to default");
    }

    void SaveRoomConfig(RoomData room)
    {
        string path = EditorUtility.SaveFilePanel("Save Room Config", "Assets", $"{room.id}_config", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = JsonUtility.ToJson(room, true);
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"💾 Saved room config to: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to save room config: {e.Message}");
        }
    }

    void LoadRoomConfig(RoomData room)
    {
        string path = EditorUtility.OpenFilePanel("Load Room Config", "Assets", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = System.IO.File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, room);
            Debug.Log($"📥 Loaded room config from: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to load room config: {e.Message}");
        }
    }

    void CheckAndMergeDuplicateRooms(PlayerProfile profile)
    {
        if (profile.rooms == null) return;

        // Find rooms with same type but different IDs (e.g., "starterapartment" vs "starter_apartment")
        var roomGroups = profile.rooms.Values.GroupBy(r => r.roomType).Where(g => g.Count() > 1);
        
        foreach (var group in roomGroups)
        {
            var rooms = group.ToList();
            // Keep the one with the normalized ID (contains underscore)
            var normalizedRoom = rooms.FirstOrDefault(r => r.id.Contains("_"));
            var duplicateRoom = rooms.FirstOrDefault(r => !r.id.Contains("_"));
            
            if (normalizedRoom != null && duplicateRoom != null)
            {
                // Merge cats from duplicate to normalized
                int movedCats = 0;
                foreach (var cat in profile.cats.Values.ToList())
                {
                    if (cat.currentRoom == duplicateRoom.id)
                    {
                        cat.currentRoom = normalizedRoom.id;
                        movedCats++;
                    }
                }
                
                // Merge furniture (avoid duplicates)
                foreach (var furniture in duplicateRoom.furniture)
                {
                    if (!normalizedRoom.furniture.Any(f => f.id == furniture.id))
                    {
                        normalizedRoom.furniture.Add(furniture);
                    }
                }
                
                // Use better litter box state (cleaner one)
                if (duplicateRoom.litterBox.cleanliness > normalizedRoom.litterBox.cleanliness)
                {
                    normalizedRoom.litterBox = duplicateRoom.litterBox;
                }
                
                // Remove duplicate
                profile.rooms.Remove(duplicateRoom.id);
                
                if (selectedRoomId == duplicateRoom.id)
                {
                    selectedRoomId = normalizedRoom.id;
                }
                
                Debug.LogWarning($"🔧 Merged duplicate room '{duplicateRoom.id}' into '{normalizedRoom.id}' (moved {movedCats} cats)");
            }
        }
    }

    void PrintRoomStats(RoomData room)
    {
        Debug.Log($"=== ROOM STATS: {room.displayName} ===");
        Debug.Log($"ID: {room.id}");
        Debug.Log($"Type: {room.roomType}");
        Debug.Log($"Unlocked: {room.unlocked}");
        Debug.Log($"Max Cats: {room.maxCats} (Current: {GetCatsInRoom(room.id)})");
        Debug.Log($"Max Furniture: {room.maxFurniture} (Current: {room.furniture.Count})");
        Debug.Log($"Generation Bonus: {room.generationBonus:F2}x");
        Debug.Log($"Total Multiplier: {room.GetTotalGenerationMultiplier():F2}x");
        Debug.Log($"Heart Cost: {room.heartCost}");
        Debug.Log($"Premium Cost: ${room.premiumCost:F2}");
        Debug.Log($"Litter Box Cleanliness: {room.litterBox.cleanliness:F1}%");
        Debug.Log($"Furniture Sets: {string.Join(", ", room.GetFurnitureSets().Select(s => $"{s.Key}: {s.Value}"))}");
        Debug.Log($"Furniture Count: {room.furniture.Count}");
        foreach (var f in room.furniture)
        {
            Debug.Log($"  - {f.displayName} ({f.furnitureType}) at {f.position}");
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
