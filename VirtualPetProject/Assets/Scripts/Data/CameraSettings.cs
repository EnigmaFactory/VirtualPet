using UnityEngine;

/// <summary>
/// ScriptableObject for camera configuration
/// Allows designers to tweak camera behavior without code changes
/// Create via: Assets > Create > VirtualPet > Camera Settings
/// </summary>
[CreateAssetMenu(fileName = "CameraSettings", menuName = "VirtualPet/Camera Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Orbit Settings")]
    [Tooltip("Default distance from orbit center")]
    public float defaultOrbitDistance = 5f;

    [Tooltip("Default camera height above ground")]
    public float defaultOrbitHeight = 2f;

    [Tooltip("Orbit rotation speed")]
    [Range(0.5f, 5f)]
    public float orbitSpeed = 2f;

    [Tooltip("Default tilt angle (0 = side view, 90 = top-down)")]
    [Range(10f, 80f)]
    public float defaultTiltAngle = 30f;

    [Header("Focus Settings")]
    [Tooltip("Distance behind cat when focused")]
    public float focusDistance = 3f;

    [Tooltip("Height above cat when focused")]
    public float focusHeight = 1.5f;

    [Tooltip("How fast camera follows cat")]
    [Range(1f, 10f)]
    public float focusFollowSpeed = 5f;

    [Tooltip("Additional offset from cat")]
    public Vector3 focusOffset = Vector3.zero;

    [Header("Zoom Settings")]
    [Tooltip("Minimum zoom distance")]
    public float minDistance = 2f;

    [Tooltip("Maximum zoom distance")]
    public float maxDistance = 10f;

    [Tooltip("Zoom sensitivity")]
    [Range(1f, 5f)]
    public float zoomSpeed = 2f;

    [Header("Input Settings")]
    [Tooltip("Allow right-click drag to orbit")]
    public bool allowOrbitDrag = true;

    [Tooltip("Allow scroll wheel zoom")]
    public bool allowZoom = true;

    [Tooltip("Allow click on cat to focus")]
    public bool allowClickToFocus = true;

    [Tooltip("Mouse drag sensitivity")]
    [Range(0.1f, 1f)]
    public float dragSensitivity = 0.3f;

    [Header("Smoothing")]
    [Tooltip("Camera position smoothing (lower = smoother)")]
    [Range(0.1f, 1f)]
    public float positionSmoothTime = 0.3f;

    [Tooltip("Camera rotation smoothing (lower = smoother)")]
    [Range(0.05f, 0.5f)]
    public float rotationSmoothTime = 0.2f;

    [Header("Behavior")]
    [Tooltip("Auto-focus when cat is adopted")]
    public bool autoFocusOnAdopt = true;

    [Tooltip("Auto-frame all cats on scene start")]
    public bool autoFrameOnStart = false;

    [Header("Boundaries (Optional)")]
    [Tooltip("Restrict camera to room bounds")]
    public bool useBoundaries = true;

    [Tooltip("Room size (center at origin)")]
    public Vector3 roomSize = new Vector3(10f, 5f, 10f);

    /// <summary>
    /// Apply these settings to a CameraController
    /// </summary>
    public void ApplyTo(CameraController controller)
    {
        if (controller == null) return;

        // Use reflection to set private fields (or make them public)
        var type = controller.GetType();

        SetField(type, controller, "orbitDistance", defaultOrbitDistance);
        SetField(type, controller, "orbitHeight", defaultOrbitHeight);
        SetField(type, controller, "orbitSpeed", orbitSpeed);
        SetField(type, controller, "tiltAngle", defaultTiltAngle);

        SetField(type, controller, "focusDistance", focusDistance);
        SetField(type, controller, "focusHeight", focusHeight);
        SetField(type, controller, "focusFollowSpeed", focusFollowSpeed);
        SetField(type, controller, "focusOffset", focusOffset);

        SetField(type, controller, "minDistance", minDistance);
        SetField(type, controller, "maxDistance", maxDistance);
        SetField(type, controller, "zoomSpeed", zoomSpeed);

        SetField(type, controller, "allowOrbitDrag", allowOrbitDrag);
        SetField(type, controller, "allowZoom", allowZoom);
        SetField(type, controller, "allowClickToFocus", allowClickToFocus);
        SetField(type, controller, "dragSensitivity", dragSensitivity);

        SetField(type, controller, "positionSmoothTime", positionSmoothTime);
        SetField(type, controller, "rotationSmoothTime", rotationSmoothTime);

        SetField(type, controller, "autoFocusOnAdopt", autoFocusOnAdopt);
        SetField(type, controller, "useBoundaries", useBoundaries);

        Bounds bounds = new Bounds(Vector3.zero, roomSize);
        SetField(type, controller, "roomBounds", bounds);

        Debug.Log($"✅ Applied camera settings: {name}");
    }

    private void SetField(System.Type type, object obj, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}
