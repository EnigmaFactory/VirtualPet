using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

/// <summary>
/// Manages cat visual representation (model, textures, body type)
/// Handles quality switching and body transformation over time
/// Integrates with Addressables for dynamic loading
/// </summary>
public class CatVisuals : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CatModelConfig modelConfig;

    [Header("Cat Appearance")]
    [SerializeField] private CatBodyType currentBodyType = CatBodyType.Simple;
    [SerializeField] private string colorVariantId = "tabby";

    [Header("Body Evolution")]
    [SerializeField] private bool canEvolve = true;
    [SerializeField] private CatBodyType targetBodyType = CatBodyType.Simple;
    [SerializeField] private float evolutionProgress = 0f; // 0-1

    [Header("Runtime State")]
    [SerializeField] private GameObject currentModelInstance;
    [SerializeField] private GraphicsQuality loadedQuality;
    [SerializeField] private bool isLoading = false;

    // References to child components (set after model loads)
    private AnimalController animalController;
    private IKController ikController;
    private MovementController movementController;
    private Animator animator; // Red Deer models use Animator, not Animation

    // Addressables handle
    private AsyncOperationHandle<GameObject> currentLoadHandle;

    // Future: Blendshapes
    private SkinnedMeshRenderer[] skinnedMeshes;
    private Dictionary<string, float> blendShapeWeights = new Dictionary<string, float>();

    void Start()
    {
        // Get model config if not assigned
        if (modelConfig == null)
        {
            modelConfig = Resources.Load<CatModelConfig>("CatModelConfig");
        }

        // Subscribe to quality changes
        if (GraphicsQualityManager.Instance != null)
        {
            GraphicsQualityManager.Instance.OnQualityChanged += OnQualityChanged;
        }

        // Load initial model
        LoadCatModel();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (GraphicsQualityManager.Instance != null)
        {
            GraphicsQualityManager.Instance.OnQualityChanged -= OnQualityChanged;
        }

        // Release Addressables handle
        if (currentLoadHandle.IsValid())
        {
            Addressables.Release(currentLoadHandle);
        }
    }

    #region Model Loading

    /// <summary>
    /// Load cat model based on current body type and quality settings
    /// </summary>
    public void LoadCatModel()
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading a cat model!");
            return;
        }

        if (modelConfig == null)
        {
            Debug.LogError("CatModelConfig not assigned!");
            return;
        }

        StartCoroutine(LoadCatModelAsync());
    }

    private IEnumerator LoadCatModelAsync()
    {
        isLoading = true;

        // Get current quality setting
        GraphicsQuality quality = GraphicsQualityManager.Instance != null
            ? GraphicsQualityManager.Instance.CurrentQuality
            : GraphicsQuality.Medium;

        // Handle Auto quality
        if (quality == GraphicsQuality.Auto)
        {
            quality = GraphicsQuality.Medium; // Default to Medium
        }

        // Get model variant
        var variant = modelConfig.GetModelVariant(currentBodyType, quality);

        if (variant == null || variant.modelReference == null)
        {
            Debug.LogError($"No model variant found for {currentBodyType} at {quality} quality!");
            isLoading = false;
            yield break;
        }

        Debug.Log($"🐱 Loading cat model: {currentBodyType} ({quality}) - {variant.triCount} tris");

        // Unload previous model
        if (currentModelInstance != null)
        {
            if (currentLoadHandle.IsValid())
            {
                Addressables.Release(currentLoadHandle);
            }
            Destroy(currentModelInstance);
        }

        // Load new model from Addressables
        currentLoadHandle = variant.modelReference.InstantiateAsync(transform);

        yield return currentLoadHandle;

        if (currentLoadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            currentModelInstance = currentLoadHandle.Result;
            loadedQuality = quality;

            // Position and setup
            currentModelInstance.transform.localPosition = Vector3.zero;
            currentModelInstance.transform.localRotation = Quaternion.identity;

            // Apply color/texture
            ApplyColorVariant();

            // Setup references to components
            SetupComponentReferences();

            Debug.Log($"✅ Cat model loaded successfully: {variant.name}");
        }
        else
        {
            Debug.LogError($"Failed to load cat model: {currentLoadHandle.OperationException}");
        }

        isLoading = false;
    }

    /// <summary>
    /// Setup references to components on loaded model
    /// </summary>
    void SetupComponentReferences()
    {
        if (currentModelInstance == null) return;

        // Find animator (Red Deer uses Animator)
        animator = currentModelInstance.GetComponentInChildren<Animator>();

        // Get skinned meshes for future blendshapes
        skinnedMeshes = currentModelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

        // Connect to other controllers
        animalController = GetComponent<AnimalController>();
        ikController = GetComponent<IKController>();
        movementController = GetComponent<MovementController>();

        // If AnimalController needs animator reference, set it here
        // (You'll need to add a SetAnimator method to AnimalController)

        Debug.Log($"🔧 Setup complete: Animator={animator != null}, Meshes={skinnedMeshes.Length}");
    }

    #endregion

    #region Appearance

    /// <summary>
    /// Apply color/coat variant
    /// </summary>
    void ApplyColorVariant()
    {
        if (currentModelInstance == null || modelConfig == null) return;

        var colorConfig = modelConfig.GetColorConfig(colorVariantId);

        if (colorConfig == null)
        {
            Debug.LogWarning($"Color variant '{colorVariantId}' not found!");
            return;
        }

        // Get appropriate material for quality
        Material material = colorConfig.GetMaterialForQuality(loadedQuality);

        if (material == null)
        {
            Debug.LogWarning($"No material found for {colorVariantId} at {loadedQuality}");
            return;
        }

        // Apply to all renderers
        Renderer[] renderers = currentModelInstance.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            // Create instance of material (so each cat can have different colors/dirt/etc.)
            renderer.material = new Material(material);
        }

        Debug.Log($"🎨 Applied color: {colorConfig.displayName} ({loadedQuality})");
    }

    /// <summary>
    /// Change cat color at runtime
    /// </summary>
    public void SetColorVariant(string newColorId)
    {
        if (colorVariantId != newColorId)
        {
            colorVariantId = newColorId;
            ApplyColorVariant();
        }
    }

    #endregion

    #region Body Evolution

    /// <summary>
    /// Start body transformation (e.g., Kitten → Simple, Skinny → Simple)
    /// </summary>
    public void StartBodyEvolution(CatBodyType newBodyType, float durationDays)
    {
        if (!canEvolve || currentBodyType == newBodyType) return;

        targetBodyType = newBodyType;
        evolutionProgress = 0f;

        Debug.Log($"🌱 Starting evolution: {currentBodyType} → {targetBodyType} over {durationDays} days");

        // Start evolution coroutine
        StartCoroutine(EvolutionCoroutine(durationDays));
    }

    private IEnumerator EvolutionCoroutine(float durationDays)
    {
        float startTime = Time.time;
        float durationSeconds = durationDays * 24f * 60f * 60f; // Convert days to seconds (in-game time)

        // For testing, use real-time seconds instead:
        durationSeconds = durationDays * 10f; // 10 seconds per "day" for testing

        while (evolutionProgress < 1f)
        {
            float elapsed = Time.time - startTime;
            evolutionProgress = Mathf.Clamp01(elapsed / durationSeconds);

            // TODO: Apply blendshapes here for smooth transition
            // For now, just log progress
            if (evolutionProgress % 0.25f < 0.01f) // Every 25%
            {
                Debug.Log($"📈 Evolution progress: {evolutionProgress * 100f:F0}%");
            }

            yield return null;
        }

        // Evolution complete - swap model
        Debug.Log($"🎉 Evolution complete! Swapping to {targetBodyType}");
        currentBodyType = targetBodyType;
        LoadCatModel(); // Reload with new body type
    }

    /// <summary>
    /// Instant body type change (no transition)
    /// </summary>
    public void SetBodyType(CatBodyType newBodyType)
    {
        if (currentBodyType != newBodyType)
        {
            currentBodyType = newBodyType;
            targetBodyType = newBodyType;
            evolutionProgress = 0f;
            LoadCatModel();
        }
    }

    #endregion

    #region Quality Switching

    /// <summary>
    /// Called when graphics quality changes
    /// </summary>
    void OnQualityChanged(GraphicsQuality newQuality)
    {
        if (newQuality == GraphicsQuality.Auto) return;

        if (newQuality != loadedQuality && !isLoading)
        {
            Debug.Log($"🔄 Quality changed: {loadedQuality} → {newQuality}. Reloading model...");
            LoadCatModel();
        }
    }

    #endregion

    #region Blendshapes (Future)

    /// <summary>
    /// Set blendshape weight (for future procedural morphing)
    /// Example: "Weight" blendshape for Skinny → Simple transition
    /// </summary>
    public void SetBlendShapeWeight(string blendShapeName, float weight)
    {
        blendShapeWeights[blendShapeName] = Mathf.Clamp01(weight);

        if (skinnedMeshes != null)
        {
            foreach (var mesh in skinnedMeshes)
            {
                int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(blendShapeName);

                if (shapeIndex >= 0)
                {
                    mesh.SetBlendShapeWeight(shapeIndex, weight * 100f);
                }
            }
        }
    }

    /// <summary>
    /// Get current blendshape weight
    /// </summary>
    public float GetBlendShapeWeight(string blendShapeName)
    {
        return blendShapeWeights.ContainsKey(blendShapeName) ? blendShapeWeights[blendShapeName] : 0f;
    }

    #endregion

    #region Getters

    public CatBodyType CurrentBodyType => currentBodyType;
    public CatBodyType TargetBodyType => targetBodyType;
    public float EvolutionProgress => evolutionProgress;
    public bool IsEvolving => evolutionProgress > 0f && evolutionProgress < 1f;
    public GameObject ModelInstance => currentModelInstance;
    public Animator Animator => animator;

    #endregion
}
