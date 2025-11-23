using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor wizard to quickly set up pet spots on cat models
/// Window > Virtual Pet > Pet Spot Setup
/// </summary>
public class PetSpotSetupWizard : EditorWindow
{
    [MenuItem("Window/Virtual Pet/Pet Spot Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<PetSpotSetupWizard>("Pet Spot Setup");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private GameObject catModel;
    private Transform headBone;
    private Transform spineBone;
    private Transform chestBone;

    private bool setupHead = true;
    private bool setupBack = true;
    private bool setupChest = true;
    private bool setupChin = false;
    private bool setupEars = false;

    private float headRadius = 0.1f;
    private float backRadius = 0.12f;
    private float chestRadius = 0.08f;

    private LayerMask petSpotLayer;
    private string layerName = "Cat";

    Vector2 scrollPosition;

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawModelSelection();
        EditorGUILayout.Space(10);

        DrawBoneAssignment();
        EditorGUILayout.Space(10);

        DrawPetSpotOptions();
        EditorGUILayout.Space(10);

        DrawSetupButton();

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        EditorGUILayout.LabelField("🤗 Pet Spot Setup Wizard", headerStyle);
        EditorGUILayout.HelpBox(
            "Quickly add pettable areas to your cat model.\n" +
            "This wizard creates colliders and PetSpot components on bones.",
            MessageType.Info
        );
    }

    void DrawModelSelection()
    {
        EditorGUILayout.LabelField("Cat Model", EditorStyles.boldLabel);
        catModel = (GameObject)EditorGUILayout.ObjectField("Cat Prefab/Object", catModel, typeof(GameObject), true);

        if (catModel == null)
        {
            EditorGUILayout.HelpBox("Select a cat model or prefab to set up pet spots.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"✅ Selected: {catModel.name}", MessageType.Info);
        }
    }

    void DrawBoneAssignment()
    {
        EditorGUILayout.LabelField("Bone References", EditorStyles.boldLabel);

        if (catModel == null)
        {
            EditorGUILayout.HelpBox("Select a cat model first.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical("box");

        headBone = (Transform)EditorGUILayout.ObjectField("Head Bone", headBone, typeof(Transform), true);
        spineBone = (Transform)EditorGUILayout.ObjectField("Spine Bone", spineBone, typeof(Transform), true);
        chestBone = (Transform)EditorGUILayout.ObjectField("Chest Bone", chestBone, typeof(Transform), true);

        EditorGUILayout.EndVertical();

        if (GUILayout.Button("🔍 Auto-Detect Bones"))
        {
            AutoDetectBones();
        }
    }

    void DrawPetSpotOptions()
    {
        EditorGUILayout.LabelField("Pet Spots to Create", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        setupHead = EditorGUILayout.Toggle("Head (Favorite Spot)", setupHead);
        if (setupHead)
        {
            EditorGUI.indentLevel++;
            headRadius = EditorGUILayout.Slider("Radius", headRadius, 0.05f, 0.3f);
            EditorGUI.indentLevel--;
        }

        setupBack = EditorGUILayout.Toggle("Back (Strokes)", setupBack);
        if (setupBack)
        {
            EditorGUI.indentLevel++;
            backRadius = EditorGUILayout.Slider("Radius", backRadius, 0.05f, 0.3f);
            EditorGUI.indentLevel--;
        }

        setupChest = EditorGUILayout.Toggle("Chest", setupChest);
        if (setupChest)
        {
            EditorGUI.indentLevel++;
            chestRadius = EditorGUILayout.Slider("Radius", chestRadius, 0.05f, 0.3f);
            EditorGUI.indentLevel--;
        }

        setupChin = EditorGUILayout.Toggle("Chin (Scratches)", setupChin);
        setupEars = EditorGUILayout.Toggle("Ears", setupEars);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Layer Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        layerName = EditorGUILayout.TextField("Layer Name", layerName);
        EditorGUILayout.HelpBox("Pet spots will be assigned to this layer.\nMake sure it exists in Tags & Layers.", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    void DrawSetupButton()
    {
        EditorGUILayout.Space(10);

        bool canSetup = catModel != null && (setupHead || setupBack || setupChest || setupChin || setupEars);

        EditorGUI.BeginDisabledGroup(!canSetup);

        if (GUILayout.Button("✨ Create Pet Spots", GUILayout.Height(40)))
        {
            CreatePetSpots();
        }

        EditorGUI.EndDisabledGroup();

        if (!canSetup)
        {
            EditorGUILayout.HelpBox("Select a cat model and at least one pet spot option.", MessageType.Warning);
        }
    }

    void AutoDetectBones()
    {
        if (catModel == null)
        {
            EditorUtility.DisplayDialog("Error", "Select a cat model first!", "OK");
            return;
        }

        Transform[] allBones = catModel.GetComponentsInChildren<Transform>();

        // Try to find bones by name
        headBone = FindBone(allBones, "head");
        spineBone = FindBone(allBones, "spine", "spine1", "spine_01");
        chestBone = FindBone(allBones, "chest", "spine2", "spine_02");

        Debug.Log($"✅ Auto-detected: Head={headBone != null}, Spine={spineBone != null}, Chest={chestBone != null}");
    }

    Transform FindBone(Transform[] bones, params string[] names)
    {
        foreach (string name in names)
        {
            foreach (Transform bone in bones)
            {
                if (bone.name.ToLower().Contains(name.ToLower()))
                {
                    return bone;
                }
            }
        }
        return null;
    }

    void CreatePetSpots()
    {
        if (catModel == null)
        {
            EditorUtility.DisplayDialog("Error", "No cat model selected!", "OK");
            return;
        }

        int count = 0;

        // Get layer index
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            Debug.LogWarning($"⚠️ Layer '{layerName}' not found. Using Default layer.");
            layer = 0;
        }

        // Create head spot
        if (setupHead && headBone != null)
        {
            CreatePetSpot(headBone.gameObject, "Head", PetSpotType.Head, headRadius, true, layer);
            count++;
        }

        // Create back spot
        if (setupBack && spineBone != null)
        {
            CreatePetSpot(spineBone.gameObject, "Back", PetSpotType.Back, backRadius, false, layer);
            count++;
        }

        // Create chest spot
        if (setupChest && chestBone != null)
        {
            CreatePetSpot(chestBone.gameObject, "Chest", PetSpotType.Chest, chestRadius, false, layer);
            count++;
        }

        // Create chin spot
        if (setupChin && headBone != null)
        {
            CreatePetSpot(headBone.gameObject, "Chin", PetSpotType.Chin, headRadius * 0.6f, false, layer);
            count++;
        }

        // Mark dirty for saving
        EditorUtility.SetDirty(catModel);

        EditorUtility.DisplayDialog("Success!", $"Created {count} pet spot(s) on {catModel.name}!", "OK");
        Debug.Log($"✅ Created {count} pet spots on {catModel.name}");
    }

    void CreatePetSpot(GameObject parent, string spotName, PetSpotType spotType, float radius, bool isFavorite, int layer)
    {
        // Create child object for pet spot
        GameObject spotObject = new GameObject($"PetSpot_{spotName}");
        spotObject.transform.SetParent(parent.transform);
        spotObject.transform.localPosition = Vector3.zero;
        spotObject.transform.localRotation = Quaternion.identity;
        spotObject.layer = layer;

        // Add sphere collider
        SphereCollider collider = spotObject.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = false; // Raycasting, not triggers

        // Add PetSpot component
        PetSpot petSpot = spotObject.AddComponent<PetSpot>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(petSpot);
        so.FindProperty("spotName").stringValue = spotName;
        so.FindProperty("spotType").enumValueIndex = (int)spotType;
        so.FindProperty("isFavoriteSpot").boolValue = isFavorite;
        so.FindProperty("isPettable").boolValue = true;
        so.ApplyModifiedProperties();

        Debug.Log($"  ✅ Created {spotName} pet spot (radius: {radius:F2})");
    }
}
