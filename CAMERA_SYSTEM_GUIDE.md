# 📷 Camera System Guide

The virtual pet camera system provides smooth, intuitive camera control with multiple modes and automatic cat tracking.

---

## Features

✅ **Orbit Mode** - Free rotation around cats or room center
✅ **Focus Mode** - Follow and frame specific cat
✅ **Smooth Transitions** - Seamless mode switching
✅ **Zoom Control** - Scroll wheel or pinch to zoom
✅ **Click to Focus** - Click on cat to focus camera
✅ **Auto-Framing** - Automatically frame all cats
✅ **Boundary System** - Keep camera in room bounds
✅ **Touch/Mouse Input** - Drag to rotate, click to focus

---

## Setup

### Basic Setup

1. **Add to Main Camera:**
   ```
   Main Camera
   ├── Camera (component)
   └── CameraController (component)
   ```

2. **Configure Settings:**
   - Orbit Distance: 5.0
   - Orbit Height: 2.0
   - Focus Distance: 3.0
   - Min/Max Distance: 2.0 / 10.0
   - Room Bounds: (10, 5, 10)

3. **Set Clickable Layer:**
   - Create layer "Cat" or use "Default"
   - Assign to cat colliders
   - Set in CameraController > Clickable Layer

---

## Camera Modes

### 🌐 Orbit Mode
**Purpose:** Free rotation around cats or room center

**Controls:**
- **Right-Click Drag** - Rotate camera around target
- **Scroll Wheel** - Zoom in/out
- **Tab** - Switch to Focus mode

**Behavior:**
- Orbits around center of all cats (or room center)
- Maintains set distance and tilt angle
- Auto-frames when multiple cats exist

**Best For:** Overview of room, multiple cats

---

### 🎯 Focus Mode
**Purpose:** Follow specific cat

**Controls:**
- **Click on Cat** - Focus on that cat
- **Scroll Wheel** - Adjust follow distance
- **Tab** - Switch to Orbit mode
- **F** - Focus on nearest cat

**Behavior:**
- Camera follows cat from behind and above
- Smoothly tracks cat movement
- Auto-switches to Orbit if cat is removed

**Best For:** Watching single cat, close-up interactions

---

### 🎥 Free Mode
**Purpose:** Manual control (for cinematics)

**Behavior:**
- Camera doesn't auto-update
- Useful for cutscenes or manual control
- Set programmatically only

---

## Input Controls

### Keyboard Shortcuts

**Mode Switching:**
- `Tab` - Toggle between Orbit/Focus modes
- `F` - Focus on nearest cat
- `Home` - Reset to Orbit mode
- `A` - Auto-frame all cats

**DevTools (Play Mode):**
- `F5` - Orbit Mode
- `F6` - Focus Mode
- `F7` - Frame All Cats

### Mouse Controls

- **Right-Click + Drag** - Rotate camera (Orbit mode)
- **Scroll Wheel** - Zoom in/out
- **Left-Click on Cat** - Focus on that cat

### Touch Controls (Mobile)

- **One Finger Drag** - Rotate camera
- **Pinch** - Zoom (not yet implemented)
- **Tap on Cat** - Focus on that cat

---

## Code Usage

### Focus on Specific Cat

```csharp
CameraController cam = Camera.main.GetComponent<CameraController>();

// Focus on transform
cam.FocusOnTarget(catTransform);

// Focus on nearest cat
cam.FocusOnNearestCat();
```

### Switch Modes

```csharp
// Set specific mode
cam.SetMode(CameraMode.Orbit);
cam.SetMode(CameraMode.Focus);

// Toggle between modes
cam.ToggleMode();
```

### Frame All Cats

```csharp
// Auto-zoom to fit all cats on screen
cam.FrameAllCats();
```

### Track Cat Adoption/Removal

```csharp
// When cat is adopted
cam.OnCatAdopted(catGameObject);

// When cat is removed/released
cam.OnCatRemoved(catGameObject);

// Manually refresh list
cam.RefreshCatList();
```

---

## CameraSettings ScriptableObject

Create reusable camera presets:

**Create:** Assets > Create > VirtualPet > Camera Settings

**Settings:**
- Orbit/Focus distances and speeds
- Zoom min/max and speed
- Input sensitivity
- Smoothing times
- Behavior flags (auto-focus, auto-frame)
- Room boundaries

**Apply to Camera:**
```csharp
CameraSettings settings = Resources.Load<CameraSettings>("MyCameraSettings");
settings.ApplyTo(cameraController);
```

---

## Configuration Tips

### For Close-Up Cat View
```
Focus Distance: 2.0
Focus Height: 1.0
Min Distance: 1.5
Position Smooth Time: 0.2 (faster follow)
```

### For Room Overview
```
Orbit Distance: 7.0
Orbit Height: 3.0
Tilt Angle: 45.0
Max Distance: 15.0
```

### For Smooth Cinematic Look
```
Position Smooth Time: 0.5 (slower, smoother)
Rotation Smooth Time: 0.3
Focus Follow Speed: 3.0
```

### For Responsive Action View
```
Position Smooth Time: 0.1 (faster, snappier)
Rotation Smooth Time: 0.1
Focus Follow Speed: 8.0
```

---

## Room Boundaries

Prevents camera from leaving playable area:

```csharp
// Enable boundaries
useBoundaries = true;

// Set room size (centered at origin)
roomBounds = new Bounds(Vector3.zero, new Vector3(10f, 5f, 10f));
```

**Visualization:**
- Enable "Show Bounds" in inspector
- Cyan wireframe box shows boundaries
- Camera position clamped to stay inside

---

## Auto-Focus Behavior

**On Cat Adoption:**
```csharp
autoFocusOnAdopt = true; // Camera auto-focuses new cats
```

**On Scene Start:**
```csharp
autoFrameOnStart = true; // Camera frames all cats at start
```

---

## Debugging

### Inspector Debug Info

Enable in CameraController:
- `Show Debug Info` - Shows focus target, cats center
- `Show Bounds` - Shows room boundaries

### Debug Visualization

**In Scene View:**
- Yellow line = Camera to focus target
- Green ray = Cats center point
- Cyan box = Room boundaries

**In Console:**
```
📷 Camera mode: Orbit → Focus
📷 Focusing on cat: TestCat
📷 Framing 3 cats (distance: 6.5)
```

---

## Common Scenarios

### 1. Single Cat Following
```csharp
// When cat is adopted
cameraController.FocusOnTarget(catTransform);
```

### 2. Multiple Cats Overview
```csharp
// Frame all cats on screen
cameraController.FrameAllCats();
cameraController.SetMode(CameraMode.Orbit);
```

### 3. Click to Select Cat
- User clicks on cat
- Camera automatically focuses
- User can drag to rotate around cat

### 4. Room Tour Mode
```csharp
// Orbit around room center
cameraController.SetMode(CameraMode.Orbit);
cameraController.orbitAngle = 0f;

// Slowly rotate
while (touring) {
    cameraController.orbitAngle += Time.deltaTime * 10f;
    yield return null;
}
```

---

## Performance Notes

### Smooth Damp Performance
- Uses `Vector3.SmoothDamp` for position
- Uses `Quaternion.Slerp` for rotation
- Very efficient, ~0.1ms per frame

### Cat Tracking
- `RefreshCatList()` uses `FindObjectsOfType`
- Call sparingly (on adopt/remove only)
- Tracked list cached for performance

---

## Integration with Other Systems

### GameManager Integration
```csharp
// In GameManager.AdoptCat()
if (success) {
    cameraController?.OnCatAdopted(catGameObject);
}

// In GameManager.ReleaseCat()
cameraController?.OnCatRemoved(catGameObject);
```

### UI Integration
```csharp
// UI button to focus on cat
public void OnCatButtonClicked(Transform catTransform) {
    cameraController.FocusOnTarget(catTransform);
}
```

---

## Troubleshooting

### Camera Not Following Cat
**Check:**
- Focus target is set
- Mode is Focus (not Orbit)
- Cat transform still exists

**Fix:**
```csharp
cameraController.FocusOnTarget(catTransform);
cameraController.SetMode(CameraMode.Focus);
```

### Camera Jumpy/Not Smooth
**Check:**
- Position Smooth Time (increase for smoother)
- Rotation Smooth Time (increase for smoother)
- Frame rate (low FPS causes jitter)

**Fix:**
```csharp
positionSmoothTime = 0.5f; // Increase
rotationSmoothTime = 0.3f; // Increase
```

### Click to Focus Not Working
**Check:**
- Clickable Layer is set
- Cats have colliders
- Cats are on correct layer

**Fix:**
```csharp
// Assign layer to cat
catGameObject.layer = LayerMask.NameToLayer("Cat");

// Set in CameraController
clickableLayer = LayerMask.GetMask("Cat");
```

### Camera Goes Outside Room
**Check:**
- Use Boundaries is enabled
- Room Bounds size is correct

**Fix:**
```csharp
useBoundaries = true;
roomBounds = new Bounds(Vector3.zero, new Vector3(10f, 5f, 10f));
```

---

## Future Enhancements

### Planned Features
- 📱 **Touch Gestures** - Pinch to zoom, swipe to rotate
- 🎬 **Camera Shake** - For cat jumps and impacts
- 🎯 **Smart Framing** - Predict cat movement, frame ahead
- 📐 **Collision Avoidance** - Raycast to prevent wall clipping
- 🎮 **Cinemachine Integration** - Advanced camera behaviors

### Possible Additions
- Multiple camera presets (Close, Medium, Far)
- Save/load camera position
- Camera shake on specific events
- Depth of field focus
- Screenshot mode with pose controls

---

## Best Practices

1. **Always set Clickable Layer** - Required for click-to-focus
2. **Call RefreshCatList() on adopt/remove** - Keeps tracking accurate
3. **Use boundaries for WebGL** - Prevents confusing camera angles
4. **Test both modes** - Ensure smooth transition
5. **Tune smoothing for your game feel** - Fast vs. cinematic

---

**Happy filming your cats!** 📷🐱✨

For questions, check `CameraController.cs` or `CameraSettings.cs`.
