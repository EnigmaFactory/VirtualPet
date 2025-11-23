using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages graphics quality settings and device capability detection
/// Determines which cat model variants to load (Default, NoAlpha, LowPoly)
/// </summary>
public class GraphicsQualityManager : MonoBehaviour
{
    public static GraphicsQualityManager Instance { get; private set; }

    [Header("Quality Settings")]
    [SerializeField] private GraphicsQuality currentQuality = GraphicsQuality.Auto;
    [SerializeField] private bool allowRuntimeSwitch = true;

    [Header("Performance Thresholds")]
    [SerializeField] private int highQualityMinVRAM = 4096; // MB
    [SerializeField] private int mediumQualityMinVRAM = 2048; // MB
    [SerializeField] private int targetFrameRate = 60;

    [Header("Platform Overrides")]
    [SerializeField] private bool forceLowPolyOnMobile = false;
    [SerializeField] private bool preferNoAlphaOnWebGL = true;

    [Header("Runtime Stats")]
    [SerializeField] private GraphicsQuality detectedQuality;
    [SerializeField] private int vramMB;
    [SerializeField] private string gpuName;
    [SerializeField] private float avgFrameRate;

    // Events
    public event System.Action<GraphicsQuality> OnQualityChanged;

    // Frame rate tracking
    private Queue<float> frameTimeQueue = new Queue<float>();
    private const int frameTimeQueueLength = 60;

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

    void Start()
    {
        DetectHardware();
        ApplyQualitySettings();
    }

    void Update()
    {
        TrackFrameRate();
    }

    #region Hardware Detection

    /// <summary>
    /// Detect device capabilities and determine appropriate quality
    /// </summary>
    void DetectHardware()
    {
        vramMB = SystemInfo.graphicsMemorySize;
        gpuName = SystemInfo.graphicsDeviceName;

        Debug.Log($"🖥️ GPU: {gpuName}");
        Debug.Log($"💾 VRAM: {vramMB} MB");
        Debug.Log($"📱 Platform: {Application.platform}");
        Debug.Log($"🌐 WebGL: {Application.platform == RuntimePlatform.WebGLPlayer}");

        if (currentQuality == GraphicsQuality.Auto)
        {
            detectedQuality = AutoDetectQuality();
            currentQuality = detectedQuality;
            Debug.Log($"✅ Auto-detected quality: {currentQuality}");
        }
        else
        {
            detectedQuality = currentQuality;
        }
    }

    /// <summary>
    /// Auto-detect appropriate quality tier based on hardware
    /// </summary>
    GraphicsQuality AutoDetectQuality()
    {
        // Mobile platform
        if (Application.isMobilePlatform || forceLowPolyOnMobile)
        {
            // Check mobile GPU tier
            if (vramMB >= 3072) // 3GB+ (flagship phones)
            {
                return GraphicsQuality.Medium; // NoAlpha
            }
            else if (vramMB >= 2048) // 2GB (mid-range)
            {
                return GraphicsQuality.Low; // LowPoly + Mobile textures
            }
            else // <2GB (budget phones)
            {
                return GraphicsQuality.Low;
            }
        }

        // WebGL platform (special considerations)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (preferNoAlphaOnWebGL)
            {
                // WebGL: Always prefer NoAlpha for draw call reduction
                if (vramMB >= 2048)
                {
                    return GraphicsQuality.Medium; // NoAlpha is best for WebGL
                }
                else
                {
                    return GraphicsQuality.Low; // LowPoly for weak devices
                }
            }
        }

        // Desktop platform
        if (vramMB >= highQualityMinVRAM) // 4GB+ VRAM
        {
            return GraphicsQuality.High; // Default (with alpha)
        }
        else if (vramMB >= mediumQualityMinVRAM) // 2-4GB VRAM
        {
            return GraphicsQuality.Medium; // NoAlpha
        }
        else // <2GB VRAM (integrated graphics)
        {
            return GraphicsQuality.Low; // LowPoly
        }
    }

    #endregion

    #region Quality Management

    /// <summary>
    /// Apply quality settings to Unity
    /// </summary>
    void ApplyQualitySettings()
    {
        switch (currentQuality)
        {
            case GraphicsQuality.High:
                QualitySettings.SetQualityLevel(2); // High
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                break;

            case GraphicsQuality.Medium:
                QualitySettings.SetQualityLevel(1); // Medium
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                break;

            case GraphicsQuality.Low:
                QualitySettings.SetQualityLevel(0); // Low
                Application.targetFrameRate = 30; // Cap at 30 for consistency
                QualitySettings.shadows = ShadowQuality.Disable;
                break;
        }

        Debug.Log($"🎨 Applied quality settings: {currentQuality}");
    }

    /// <summary>
    /// Change quality setting at runtime
    /// </summary>
    public void SetQuality(GraphicsQuality newQuality)
    {
        if (!allowRuntimeSwitch && newQuality != GraphicsQuality.Auto)
        {
            Debug.LogWarning("Runtime quality switching is disabled");
            return;
        }

        if (newQuality == GraphicsQuality.Auto)
        {
            newQuality = AutoDetectQuality();
        }

        if (newQuality != currentQuality)
        {
            currentQuality = newQuality;
            ApplyQualitySettings();
            OnQualityChanged?.Invoke(currentQuality);

            Debug.Log($"🔄 Quality changed to: {currentQuality}");
        }
    }

    #endregion

    #region Frame Rate Monitoring

    /// <summary>
    /// Track frame rate for dynamic quality adjustment
    /// </summary>
    void TrackFrameRate()
    {
        frameTimeQueue.Enqueue(Time.deltaTime);

        if (frameTimeQueue.Count > frameTimeQueueLength)
        {
            frameTimeQueue.Dequeue();
        }

        // Calculate average FPS
        float avgFrameTime = 0f;
        foreach (float time in frameTimeQueue)
        {
            avgFrameTime += time;
        }
        avgFrameTime /= frameTimeQueue.Count;
        avgFrameRate = 1f / avgFrameTime;
    }

    /// <summary>
    /// Check if performance is struggling (for auto-downgrade)
    /// </summary>
    public bool IsPerformanceStruggling()
    {
        return avgFrameRate < (targetFrameRate * 0.75f); // Below 75% of target
    }

    /// <summary>
    /// Auto-downgrade quality if performance is bad
    /// </summary>
    public void AutoAdjustQuality()
    {
        if (IsPerformanceStruggling() && currentQuality != GraphicsQuality.Low)
        {
            GraphicsQuality newQuality = currentQuality == GraphicsQuality.High
                ? GraphicsQuality.Medium
                : GraphicsQuality.Low;

            Debug.LogWarning($"⚠️ Performance struggling ({avgFrameRate:F1} FPS). Downgrading to {newQuality}");
            SetQuality(newQuality);
        }
    }

    #endregion

    #region Getters

    public GraphicsQuality CurrentQuality => currentQuality;
    public GraphicsQuality DetectedQuality => detectedQuality;
    public float AverageFrameRate => avgFrameRate;
    public int VRAM_MB => vramMB;

    /// <summary>
    /// Get max cats that should be on screen for current quality
    /// </summary>
    public int GetMaxCatsOnScreen()
    {
        return currentQuality switch
        {
            GraphicsQuality.High => 8,
            GraphicsQuality.Medium => 10,
            GraphicsQuality.Low => 12,
            _ => 5
        };
    }

    /// <summary>
    /// Should use mobile textures?
    /// </summary>
    public bool UseMobileTextures()
    {
        return currentQuality == GraphicsQuality.Low;
    }

    #endregion
}

/// <summary>
/// Graphics quality tiers
/// </summary>
public enum GraphicsQuality
{
    Auto,   // Detect automatically
    High,   // Default models with alpha (11,344 tris)
    Medium, // NoAlpha models (10,590 tris)
    Low     // LowPoly + Mobile textures (2,332 tris)
}
