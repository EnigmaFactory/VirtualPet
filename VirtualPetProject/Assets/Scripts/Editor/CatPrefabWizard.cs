using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor wizard for rapid cat prefab setup
/// Automates Red Deer model import, bone assignment, and Addressables setup
/// Window > Virtual Pet > Cat Prefab Wizard
/// </summary>
public class CatPrefabWizard : EditorWindow
{
    // Wizard steps
    private enum WizardStep
    {
        SelectModels,
        ConfigureBones,
        SetupComponents,
        CreatePrefabs,
        Complete
    }

    private WizardStep currentStep = WizardStep.SelectModels;

    // Model selection
    private GameObject defaultModel;
    private GameObject noAlphaModel;
    private GameObject lowPolyModel;
    private CatBodyType bodyType = CatBodyType.Simple;

    // Bone detection
    private Transform rootBone;
    private BoneReferences boneRefs = new BoneReferences();

    // Component settings
    private bool autoDetectBones = true;
    private bool createAddressables = true;
    private bool updateConfig = true;

    // Prefab output
    private string outputFolder = "Assets/Addressables/Cats/";
    private List<GameObject> createdPrefabs = new List<GameObject>();

    // UI
    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    [MenuItem("Window/Virtual Pet/Cat Prefab Wizard")]
    public static void ShowWindow()
    {
        var window = GetWindow<CatPrefabWizard>("Cat Prefab Wizard");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }

    void OnEnable()
    {
        // Initialize styles
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        headerStyle.margin = new RectOffset(0, 0, 10, 10);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(20);

        switch (currentStep)
        {
            case WizardStep.SelectModels:
                DrawSelectModelsStep();
                break;
            case WizardStep.ConfigureBones:
                DrawConfigureBonesStep();
                break;
            case WizardStep.SetupComponents:
                DrawSetupComponentsStep();
                break;
            case WizardStep.CreatePrefabs:
                DrawCreatePrefabsStep();
                break;
            case WizardStep.Complete:
                DrawCompleteStep();
                break;
        }

        EditorGUILayout.Space(20);
        DrawNavigationButtons();

        EditorGUILayout.EndScrollView();
    }

    #region UI Drawing

    void DrawHeader()
    {
        EditorGUILayout.LabelField("🐱 Cat Prefab Setup Wizard", headerStyle);
        EditorGUILayout.LabelField($"Step {(int)currentStep + 1}/5: {GetStepName()}", EditorStyles.boldLabel);

        // Progress bar
        Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
        float progress = (int)currentStep / 4f;
        EditorGUI.ProgressBar(progressRect, progress, $"{progress * 100:F0}% Complete");
    }

    string GetStepName()
    {
        return currentStep switch
        {
            WizardStep.SelectModels => "Select Red Deer Models",
            WizardStep.ConfigureBones => "Configure Bones",
            WizardStep.SetupComponents => "Setup Components",
            WizardStep.CreatePrefabs => "Create Prefabs",
            WizardStep.Complete => "Complete!",
            _ => "Unknown"
        };
    }

    #endregion

    #region Step 1: Select Models

    void DrawSelectModelsStep()
    {
        EditorGUILayout.HelpBox(
            "Select the Red Deer Cat Family Pack FBX models.\n" +
            "You can select 1-3 quality variants (Default, NoAlpha, LowPoly).",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Body type selection
        EditorGUILayout.LabelField("Cat Type", EditorStyles.boldLabel);
        bodyType = (CatBodyType)EditorGUILayout.EnumPopup("Body Type", bodyType);

        EditorGUILayout.Space(10);

        // Model selection
        EditorGUILayout.LabelField("Quality Variants", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("High Quality (Default with alpha)", EditorStyles.miniBoldLabel);
        defaultModel = (GameObject)EditorGUILayout.ObjectField("Default Model", defaultModel, typeof(GameObject), false);
        if (defaultModel != null)
        {
            ShowModelInfo(defaultModel, "~11,344 tris");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Medium Quality (NoAlpha) ⭐ Recommended", EditorStyles.miniBoldLabel);
        noAlphaModel = (GameObject)EditorGUILayout.ObjectField("NoAlpha Model", noAlphaModel, typeof(GameObject), false);
        if (noAlphaModel != null)
        {
            ShowModelInfo(noAlphaModel, "~10,590 tris");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Low Quality (LowPoly for mobile)", EditorStyles.miniBoldLabel);
        lowPolyModel = (GameObject)EditorGUILayout.ObjectField("LowPoly Model", lowPolyModel, typeof(GameObject), false);
        if (lowPolyModel != null)
        {
            ShowModelInfo(lowPolyModel, "~2,332 tris");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Output folder
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                outputFolder = "Assets" + path.Replace(Application.dataPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Validation
        if (!HasAnyModel())
        {
            EditorGUILayout.HelpBox("⚠️ Please select at least one model variant.", MessageType.Warning);
        }
    }

    void ShowModelInfo(GameObject model, string estimatedTris)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Estimated:", GUILayout.Width(70));
        EditorGUILayout.LabelField(estimatedTris, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Count actual tris
        MeshFilter[] meshes = model.GetComponentsInChildren<MeshFilter>();
        int totalTris = 0;
        foreach (var mf in meshes)
        {
            if (mf.sharedMesh != null)
            {
                totalTris += mf.sharedMesh.triangles.Length / 3;
            }
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Actual:", GUILayout.Width(70));
        EditorGUILayout.LabelField($"{totalTris:N0} tris", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    bool HasAnyModel()
    {
        return defaultModel != null || noAlphaModel != null || lowPolyModel != null;
    }

    #endregion

    #region Step 2: Configure Bones

    void DrawConfigureBonesStep()
    {
        EditorGUILayout.HelpBox(
            "Assign bone references for IK system.\n" +
            "Auto-detect will search for bones by common names.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Auto-detect toggle
        autoDetectBones = EditorGUILayout.Toggle("Auto-Detect Bones", autoDetectBones);

        if (GUILayout.Button("🔍 Auto-Detect Now", GUILayout.Height(30)))
        {
            DetectBones();
        }

        EditorGUILayout.Space(10);

        // Manual bone assignment
        EditorGUILayout.LabelField("Core Bones", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(autoDetectBones);

        boneRefs.spine = (Transform)EditorGUILayout.ObjectField("Spine", boneRefs.spine, typeof(Transform), true);
        boneRefs.chest = (Transform)EditorGUILayout.ObjectField("Chest", boneRefs.chest, typeof(Transform), true);
        boneRefs.neck = (Transform)EditorGUILayout.ObjectField("Neck", boneRefs.neck, typeof(Transform), true);
        boneRefs.head = (Transform)EditorGUILayout.ObjectField("Head", boneRefs.head, typeof(Transform), true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Paws", EditorStyles.boldLabel);

        boneRefs.frontLeftPaw = (Transform)EditorGUILayout.ObjectField("Front Left Paw", boneRefs.frontLeftPaw, typeof(Transform), true);
        boneRefs.frontRightPaw = (Transform)EditorGUILayout.ObjectField("Front Right Paw", boneRefs.frontRightPaw, typeof(Transform), true);
        boneRefs.backLeftPaw = (Transform)EditorGUILayout.ObjectField("Back Left Paw", boneRefs.backLeftPaw, typeof(Transform), true);
        boneRefs.backRightPaw = (Transform)EditorGUILayout.ObjectField("Back Right Paw", boneRefs.backRightPaw, typeof(Transform), true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tail", EditorStyles.boldLabel);

        boneRefs.tailBase = (Transform)EditorGUILayout.ObjectField("Tail Base", boneRefs.tailBase, typeof(Transform), true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ears (Optional)", EditorStyles.boldLabel);

        boneRefs.leftEar = (Transform)EditorGUILayout.ObjectField("Left Ear", boneRefs.leftEar, typeof(Transform), true);
        boneRefs.rightEar = (Transform)EditorGUILayout.ObjectField("Right Ear", boneRefs.rightEar, typeof(Transform), true);

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Validation
        if (!boneRefs.IsValid())
        {
            EditorGUILayout.HelpBox("⚠️ Missing required bones. Click Auto-Detect or assign manually.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("✅ All required bones assigned!", MessageType.Info);
        }
    }

    void DetectBones()
    {
        // Get first available model
        GameObject sourceModel = defaultModel ?? noAlphaModel ?? lowPolyModel;

        if (sourceModel == null)
        {
            EditorUtility.DisplayDialog("Error", "No model selected!", "OK");
            return;
        }

        Debug.Log("🔍 Auto-detecting bones...");

        // Find all transforms
        Transform[] allTransforms = sourceModel.GetComponentsInChildren<Transform>();

        // Common bone name patterns
        boneRefs.spine = FindBone(allTransforms, "spine", "spine1", "spine_01");
        boneRefs.chest = FindBone(allTransforms, "chest", "spine2", "spine_02", "ribcage");
        boneRefs.neck = FindBone(allTransforms, "neck", "neck1", "neck_01");
        boneRefs.head = FindBone(allTransforms, "head");

        // Paws (common names in cat rigs)
        boneRefs.frontLeftPaw = FindBone(allTransforms, "paw_l", "hand_l", "frontpaw_l", "front_paw_l");
        boneRefs.frontRightPaw = FindBone(allTransforms, "paw_r", "hand_r", "frontpaw_r", "front_paw_r");
        boneRefs.backLeftPaw = FindBone(allTransforms, "foot_l", "backpaw_l", "back_paw_l", "hindpaw_l");
        boneRefs.backRightPaw = FindBone(allTransforms, "foot_r", "backpaw_r", "back_paw_r", "hindpaw_r");

        // Tail
        boneRefs.tailBase = FindBone(allTransforms, "tail", "tail1", "tail_01", "tail_base");

        // Ears
        boneRefs.leftEar = FindBone(allTransforms, "ear_l", "ear.l", "ear_left");
        boneRefs.rightEar = FindBone(allTransforms, "ear_r", "ear.r", "ear_right");

        Debug.Log($"✅ Bones detected: {boneRefs.GetDetectedCount()}/11");

        if (!boneRefs.IsValid())
        {
            Debug.LogWarning("⚠️ Some required bones not found. Please assign manually.");
        }
    }

    Transform FindBone(Transform[] transforms, params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            foreach (Transform t in transforms)
            {
                if (t.name.ToLower().Contains(name.ToLower()))
                {
                    return t;
                }
            }
        }
        return null;
    }

    #endregion

    #region Step 3: Setup Components

    void DrawSetupComponentsStep()
    {
        EditorGUILayout.HelpBox(
            "Configure component settings for the prefabs.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Component Options", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✅ AnimalController", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  - Handles animation playback and blending", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✅ IKController", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  - Paw IK, head look-at, tail control", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  - Bones: {boneRefs.GetDetectedCount()}/11 detected", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✅ MovementController", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  - Navigation, wandering, jumping", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✅ CatVisuals", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  - Model loading, quality switching", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✅ Physics Components", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  - Rigidbody, CapsuleCollider", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Addressables", EditorStyles.boldLabel);
        createAddressables = EditorGUILayout.Toggle("Mark as Addressable", createAddressables);
        if (createAddressables)
        {
            EditorGUILayout.HelpBox("Prefabs will be automatically marked as Addressable and grouped.", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("CatModelConfig", EditorStyles.boldLabel);
        updateConfig = EditorGUILayout.Toggle("Update Config", updateConfig);
        if (updateConfig)
        {
            EditorGUILayout.HelpBox("Will create or update CatModelConfig ScriptableObject with references.", MessageType.Info);
        }
    }

    #endregion

    #region Step 4: Create Prefabs

    void DrawCreatePrefabsStep()
    {
        EditorGUILayout.HelpBox(
            "Ready to create prefabs! Click Create to generate all variants.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Summary
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Body Type: {bodyType}");
        EditorGUILayout.LabelField($"Variants to create: {GetVariantCount()}");
        EditorGUILayout.LabelField($"Output: {outputFolder}");
        EditorGUILayout.LabelField($"Bones configured: {boneRefs.IsValid() ? "Yes ✅" : "No ⚠️"}");

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🎨 CREATE PREFABS", GUILayout.Height(50)))
        {
            CreateAllPrefabs();
        }

        EditorGUILayout.Space(10);

        // Show created prefabs
        if (createdPrefabs.Count > 0)
        {
            EditorGUILayout.LabelField("Created Prefabs:", EditorStyles.boldLabel);
            foreach (var prefab in createdPrefabs)
            {
                EditorGUILayout.ObjectField(prefab.name, prefab, typeof(GameObject), false);
            }
        }
    }

    int GetVariantCount()
    {
        int count = 0;
        if (defaultModel != null) count++;
        if (noAlphaModel != null) count++;
        if (lowPolyModel != null) count++;
        return count;
    }

    void CreateAllPrefabs()
    {
        createdPrefabs.Clear();

        // Ensure output folders exist
        CreateOutputFolders();

        // Create High quality prefab
        if (defaultModel != null)
        {
            GameObject prefab = CreatePrefabVariant(defaultModel, GraphicsQuality.High);
            if (prefab != null) createdPrefabs.Add(prefab);
        }

        // Create Medium quality prefab
        if (noAlphaModel != null)
        {
            GameObject prefab = CreatePrefabVariant(noAlphaModel, GraphicsQuality.Medium);
            if (prefab != null) createdPrefabs.Add(prefab);
        }

        // Create Low quality prefab
        if (lowPolyModel != null)
        {
            GameObject prefab = CreatePrefabVariant(lowPolyModel, GraphicsQuality.Low);
            if (prefab != null) createdPrefabs.Add(prefab);
        }

        // Mark as Addressables
        if (createAddressables)
        {
            MarkAsAddressables();
        }

        // Update config
        if (updateConfig)
        {
            UpdateCatModelConfig();
        }

        EditorUtility.DisplayDialog("Success!", $"Created {createdPrefabs.Count} prefab(s) successfully!", "OK");

        // Move to complete step
        currentStep = WizardStep.Complete;
    }

    GameObject CreatePrefabVariant(GameObject sourceModel, GraphicsQuality quality)
    {
        string qualityFolder = quality.ToString();
        string prefabName = $"{bodyType}_{quality}.prefab";
        string fullPath = $"{outputFolder}/{qualityFolder}/{prefabName}";

        // Instantiate model
        GameObject instance = Instantiate(sourceModel);
        instance.name = bodyType.ToString();

        // Add components
        AddAllComponents(instance);

        // Configure IK bones
        ConfigureIKBones(instance);

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, fullPath);

        // Cleanup
        DestroyImmediate(instance);

        Debug.Log($"✅ Created prefab: {fullPath}");

        return prefab;
    }

    void CreateOutputFolders()
    {
        // Create quality folders
        string[] folders = { "High", "Medium", "Low" };

        foreach (string folder in folders)
        {
            string path = $"{outputFolder}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = outputFolder;
                AssetDatabase.CreateFolder(parentFolder, folder);
            }
        }
    }

    void AddAllComponents(GameObject go)
    {
        // Add AnimalController
        if (go.GetComponent<AnimalController>() == null)
        {
            go.AddComponent<AnimalController>();
        }

        // Add IKController
        if (go.GetComponent<IKController>() == null)
        {
            go.AddComponent<IKController>();
        }

        // Add MovementController
        if (go.GetComponent<MovementController>() == null)
        {
            go.AddComponent<MovementController>();
        }

        // Add CatVisuals
        if (go.GetComponent<CatVisuals>() == null)
        {
            go.AddComponent<CatVisuals>();
        }

        // Add Rigidbody
        if (go.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 4.5f; // Average cat weight
            rb.drag = 1f;
            rb.angularDrag = 0.5f;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Rotation controlled by code
        }

        // Add CapsuleCollider
        if (go.GetComponent<CapsuleCollider>() == null)
        {
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.15f;
            col.height = 0.3f;
            col.center = new Vector3(0, 0.15f, 0);
        }
    }

    void ConfigureIKBones(GameObject go)
    {
        IKController ik = go.GetComponent<IKController>();
        if (ik == null) return;

        // Use SerializedObject to assign private fields
        SerializedObject so = new SerializedObject(ik);

        // Find and assign bone fields
        AssignBoneField(so, "spine", boneRefs.spine, go);
        AssignBoneField(so, "chest", boneRefs.chest, go);
        AssignBoneField(so, "neck", boneRefs.neck, go);
        AssignBoneField(so, "head", boneRefs.head, go);

        AssignBoneField(so, "frontLeftPaw", boneRefs.frontLeftPaw, go);
        AssignBoneField(so, "frontRightPaw", boneRefs.frontRightPaw, go);
        AssignBoneField(so, "backLeftPaw", boneRefs.backLeftPaw, go);
        AssignBoneField(so, "backRightPaw", boneRefs.backRightPaw, go);

        AssignBoneField(so, "tailBase", boneRefs.tailBase, go);
        AssignBoneField(so, "leftEar", boneRefs.leftEar, go);
        AssignBoneField(so, "rightEar", boneRefs.rightEar, go);

        so.ApplyModifiedProperties();
    }

    void AssignBoneField(SerializedObject so, string fieldName, Transform boneRef, GameObject prefabRoot)
    {
        if (boneRef == null) return;

        // Find corresponding bone in prefab instance
        Transform[] allBones = prefabRoot.GetComponentsInChildren<Transform>();
        Transform matchingBone = System.Array.Find(allBones, t => t.name == boneRef.name);

        if (matchingBone != null)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = matchingBone;
            }
        }
    }

    void MarkAsAddressables()
    {
        // Use reflection to access Addressables API (to avoid hard dependency)
        var addressableAssetSettingsType = System.Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor");

        if (addressableAssetSettingsType == null)
        {
            Debug.LogWarning("⚠️ Addressables package not installed. Skipping Addressables setup.");
            Debug.LogWarning("   Install via: Window > Package Manager > Unity Registry > Addressables");
            return;
        }

        // Get AddressableAssetSettings
        var getSettingsMethod = addressableAssetSettingsType.GetMethod("get_DefaultObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var settings = getSettingsMethod?.Invoke(null, null);

        if (settings == null)
        {
            Debug.LogWarning("⚠️ Addressables not initialized. Please create Addressables settings first:");
            Debug.LogWarning("   Window > Asset Management > Addressables > Groups > Create Addressables Settings");
            return;
        }

        Debug.Log("📦 Marking prefabs as Addressable...");

        foreach (var prefab in createdPrefabs)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            // Determine quality from path
            GraphicsQuality quality = GraphicsQuality.Medium;
            if (assetPath.Contains("/High/")) quality = GraphicsQuality.High;
            else if (assetPath.Contains("/Low/")) quality = GraphicsQuality.Low;

            // Create address: "Cats/Simple/High"
            string address = $"Cats/{bodyType}/{quality}";

            // Create entry using reflection
            var createOrMoveEntryMethod = settings.GetType().GetMethod("CreateOrMoveEntry");
            if (createOrMoveEntryMethod != null)
            {
                var entry = createOrMoveEntryMethod.Invoke(settings, new object[] { assetGuid, null, false, false });

                if (entry != null)
                {
                    // Set address
                    var addressProperty = entry.GetType().GetProperty("address");
                    addressProperty?.SetValue(entry, address);

                    // Add labels
                    var labelsProperty = entry.GetType().GetProperty("labels");
                    var labels = labelsProperty?.GetValue(entry) as System.Collections.IList;

                    if (labels != null)
                    {
                        string bodyTypeLabel = bodyType.ToString().ToLower();
                        string qualityLabel = quality.ToString().ToLower();

                        if (!labels.Contains("cat")) labels.Add("cat");
                        if (!labels.Contains(bodyTypeLabel)) labels.Add(bodyTypeLabel);
                        if (!labels.Contains(qualityLabel)) labels.Add(qualityLabel);
                    }

                    Debug.Log($"  ✅ {prefab.name} → {address} [labels: cat, {bodyTypeLabel}, {qualityLabel}]");
                }
            }
        }

        // Save settings
        var setDirtyMethod = settings.GetType().GetMethod("SetDirty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        setDirtyMethod?.Invoke(settings, new object[] { UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryAdded, null, true, true });

        Debug.Log("✅ Addressables setup complete!");
    }

    void UpdateCatModelConfig()
    {
        // Find or create CatModelConfig
        string configPath = "Assets/Resources/CatModelConfig.asset";
        CatModelConfig config = AssetDatabase.LoadAssetAtPath<CatModelConfig>(configPath);

        if (config == null)
        {
            // Create new config
            config = ScriptableObject.CreateInstance<CatModelConfig>();

            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            AssetDatabase.CreateAsset(config, configPath);
            Debug.Log($"📄 Created new CatModelConfig at {configPath}");
        }
        else
        {
            Debug.Log($"📄 Updating existing CatModelConfig at {configPath}");
        }

        // Get or create BodyTypeConfig for this body type
        SerializedObject so = new SerializedObject(config);

        string configFieldName = bodyType switch
        {
            CatBodyType.Simple => "simpleConfig",
            CatBodyType.Kitten => "kittenConfig",
            CatBodyType.Stray => "strayConfig",
            CatBodyType.Skinny => "skinnyConfig",
            _ => "simpleConfig"
        };

        SerializedProperty bodyTypeConfigProp = so.FindProperty(configFieldName);

        if (bodyTypeConfigProp == null)
        {
            Debug.LogError($"⚠️ Could not find field '{configFieldName}' in CatModelConfig!");
            return;
        }

        // Ensure bodyType field is set
        SerializedProperty bodyTypeProp = bodyTypeConfigProp.FindPropertyRelative("bodyType");
        if (bodyTypeProp != null)
        {
            bodyTypeProp.enumValueIndex = (int)bodyType;
        }

        // Get variants array
        SerializedProperty variantsProp = bodyTypeConfigProp.FindPropertyRelative("variants");

        if (variantsProp == null)
        {
            Debug.LogError("⚠️ Could not find 'variants' array in BodyTypeConfig!");
            return;
        }

        // Clear existing variants for this body type (we're recreating them)
        variantsProp.ClearArray();

        // Add new variants
        int variantIndex = 0;
        foreach (var prefab in createdPrefabs)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);

            // Determine quality and tri count from path
            GraphicsQuality quality = GraphicsQuality.Medium;
            int triCount = 10590;
            bool useMobileTextures = false;

            if (assetPath.Contains("/High/"))
            {
                quality = GraphicsQuality.High;
                triCount = 11344;
                useMobileTextures = false;
            }
            else if (assetPath.Contains("/Low/"))
            {
                quality = GraphicsQuality.Low;
                triCount = 2332;
                useMobileTextures = true;
            }

            // Add variant entry
            variantsProp.InsertArrayElementAtIndex(variantIndex);
            SerializedProperty variantProp = variantsProp.GetArrayElementAtIndex(variantIndex);

            // Set variant fields
            variantProp.FindPropertyRelative("name").stringValue = $"{bodyType}_{quality}";
            variantProp.FindPropertyRelative("quality").enumValueIndex = (int)quality;
            variantProp.FindPropertyRelative("triCount").intValue = triCount;
            variantProp.FindPropertyRelative("useMobileTextures").boolValue = useMobileTextures;

            // Set modelReference (AssetReferenceGameObject)
            SerializedProperty modelRefProp = variantProp.FindPropertyRelative("modelReference");

            if (modelRefProp != null)
            {
                // Set the GUID for the AssetReference
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                SerializedProperty guidProp = modelRefProp.FindPropertyRelative("m_AssetGUID");

                if (guidProp != null)
                {
                    guidProp.stringValue = guid;
                }
            }

            Debug.Log($"  ✅ Added variant: {bodyType}_{quality} ({triCount} tris)");
            variantIndex++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ CatModelConfig updated with {createdPrefabs.Count} variant(s)!");
    }

    #endregion

    #region Step 5: Complete

    void DrawCompleteStep()
    {
        EditorGUILayout.HelpBox(
            "🎉 Prefabs created successfully!",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Created Prefabs:", EditorStyles.boldLabel);
        foreach (var prefab in createdPrefabs)
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Next Steps:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Setup Addressables (Window > Asset Management > Addressables)", EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Assign animation clips in AnimalController", EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. Test in DevTools (Window > Virtual Pet > Dev Tools)", EditorStyles.helpBox);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔄 Create More Prefabs", GUILayout.Height(40)))
        {
            ResetWizard();
        }

        if (GUILayout.Button("📚 Open Addressables Guide", GUILayout.Height(30)))
        {
            System.Diagnostics.Process.Start("ADDRESSABLES_SETUP_GUIDE.md");
        }
    }

    void ResetWizard()
    {
        currentStep = WizardStep.SelectModels;
        defaultModel = null;
        noAlphaModel = null;
        lowPolyModel = null;
        createdPrefabs.Clear();
        boneRefs = new BoneReferences();
    }

    #endregion

    #region Navigation

    void DrawNavigationButtons()
    {
        EditorGUILayout.BeginHorizontal();

        // Back button
        EditorGUI.BeginDisabledGroup(currentStep == WizardStep.SelectModels);
        if (GUILayout.Button("◀ Back", GUILayout.Height(30), GUILayout.Width(100)))
        {
            currentStep--;
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();

        // Next/Finish button
        bool canProceed = CanProceedToNextStep();
        EditorGUI.BeginDisabledGroup(!canProceed);

        string nextButtonText = currentStep == WizardStep.CreatePrefabs ? "Create ▶" : "Next ▶";

        if (currentStep != WizardStep.Complete && GUILayout.Button(nextButtonText, GUILayout.Height(30), GUILayout.Width(100)))
        {
            if (currentStep == WizardStep.ConfigureBones && autoDetectBones)
            {
                DetectBones(); // Auto-detect before proceeding
            }

            currentStep++;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (!canProceed)
        {
            EditorGUILayout.HelpBox(GetBlockerMessage(), MessageType.Warning);
        }
    }

    bool CanProceedToNextStep()
    {
        return currentStep switch
        {
            WizardStep.SelectModels => HasAnyModel(),
            WizardStep.ConfigureBones => boneRefs.IsValid() || autoDetectBones,
            WizardStep.SetupComponents => true,
            WizardStep.CreatePrefabs => true,
            WizardStep.Complete => false,
            _ => false
        };
    }

    string GetBlockerMessage()
    {
        return currentStep switch
        {
            WizardStep.SelectModels => "Select at least one model variant to continue.",
            WizardStep.ConfigureBones => "Assign required bones or enable Auto-Detect.",
            _ => ""
        };
    }

    #endregion

    #region Helper Classes

    [System.Serializable]
    private class BoneReferences
    {
        public Transform spine;
        public Transform chest;
        public Transform neck;
        public Transform head;

        public Transform frontLeftPaw;
        public Transform frontRightPaw;
        public Transform backLeftPaw;
        public Transform backRightPaw;

        public Transform tailBase;
        public Transform leftEar;
        public Transform rightEar;

        public bool IsValid()
        {
            // Required bones for IK to work
            return spine != null
                && head != null
                && frontLeftPaw != null
                && frontRightPaw != null
                && backLeftPaw != null
                && backRightPaw != null;
        }

        public int GetDetectedCount()
        {
            int count = 0;
            if (spine != null) count++;
            if (chest != null) count++;
            if (neck != null) count++;
            if (head != null) count++;
            if (frontLeftPaw != null) count++;
            if (frontRightPaw != null) count++;
            if (backLeftPaw != null) count++;
            if (backRightPaw != null) count++;
            if (tailBase != null) count++;
            if (leftEar != null) count++;
            if (rightEar != null) count++;
            return count;
        }
    }

    #endregion
}
