# 🤗 Petting Interaction System Guide

The petting system allows players to interact with cats using mouse or touch input, building affection and earning rewards.

---

## Features

✅ **Mouse/Touch Input** - Click, tap, or stroke cats
✅ **Pet Spots** - Define pettable areas (head, back, chin, etc.)
✅ **Stroke Recognition** - Detect and reward petting strokes
✅ **Affection System** - Build relationship with cats
✅ **Visual Feedback** - Particles, animations, floating hearts
✅ **Audio Feedback** - Purring and happiness sounds
✅ **Personality-Based Reactions** - Different cats react differently
✅ **Favorite Spots** - Bonus affection for preferred areas

---

## System Architecture

### Components

**PettingInteraction** (Scene singleton)
- Handles raycast input detection
- Manages stroke/tap recognition
- Calculates affection rewards
- Triggers feedback (particles, sound, animation)

**PetSpot** (On cat models)
- Defines pettable area (collider)
- Tracks petting state
- Triggers spot-specific reactions
- Identifies favorite spots

---

## Setup

### 1. Add PettingInteraction to Scene

```
GameManager (or separate GameObject)
├── PettingInteraction (component)
└── AudioSource (auto-added)
```

**Configure:**
- Pettable Layer: "Cat"
- Affection Per Stroke: 2.0
- Affection Per Tap: 1.0
- Bonus For Favorite Spot: 1.5x
- Enable particles, sound, animations

---

### 2. Set Up Pet Spots on Cat Models

**Option A: Use Pet Spot Setup Wizard** ⭐ Recommended

1. Open: Window > Virtual Pet > Pet Spot Setup
2. Select cat prefab/model
3. Click "Auto-Detect Bones"
4. Choose which spots to create:
   - ✅ Head (Favorite Spot)
   - ✅ Back (Strokes)
   - ✅ Chest
   - ☐ Chin (Scratches)
   - ☐ Ears
5. Adjust radii if needed
6. Click "✨ Create Pet Spots"

**Option B: Manual Setup**

```
Cat Model
├── Head Bone
│   └── PetSpot_Head
│       ├── SphereCollider (radius: 0.1)
│       └── PetSpot (component)
├── Spine Bone
│   └── PetSpot_Back
│       ├── SphereCollider (radius: 0.12)
│       └── PetSpot (component)
└── Chest Bone
    └── PetSpot_Chest
        ├── SphereCollider (radius: 0.08)
        └── PetSpot (component)
```

**PetSpot Configuration:**
- Spot Name: "Head"
- Spot Type: Head
- Is Favorite Spot: ✓ (for head)
- Is Pettable: ✓

---

### 3. Assign Cats to Pettable Layer

1. Create layer "Cat" (Edit > Project Settings > Tags & Layers)
2. Assign cat GameObjects to this layer
3. Set PettingInteraction > Pettable Layer to "Cat"

---

## How It Works

### Input Detection

**Tap Petting:**
1. User clicks/taps on cat
2. Raycast hits PetSpot collider
3. Instant affection reward
4. Feedback plays (particles, sound)

**Stroke Petting:**
1. User clicks and drags on cat
2. System tracks drag distance
3. Every 0.1m of stroking = affection reward
4. Continuous feedback while stroking

### Affection Calculation

Base affection per action:
- Stroke: 2.0 affection
- Tap: 1.0 affection

**Modifiers:**

Favorite Spot Bonus: **1.5x**
- Head is usually favorite
- Set in PetSpot component

Personality Modifiers:
- **Affectionate**: 1.5x (loves petting)
- **Playful**: 1.2x (enjoys petting)
- **Independent**: 0.7x (tolerates petting)
- **Shy**: 1.0x (normal reaction)

**Example:**
```
Stroke on head (favorite) with Affectionate cat:
Base: 2.0
× Favorite: 1.5
× Personality: 1.5
= 4.5 affection per stroke!
```

---

## Pet Spot Types

### 🐱 Head (Favorite)
- **Location:** Head bone
- **Radius:** 0.1
- **Reaction:** Purr, close eyes, head tilt
- **Affection:** High
- **Favorite:** Usually ✓

### 📏 Back (Strokes)
- **Location:** Spine bone
- **Radius:** 0.12 (longer for strokes)
- **Reaction:** Tail up, arch back
- **Affection:** Medium
- **Best For:** Long strokes

### 💚 Chest
- **Location:** Chest bone
- **Radius:** 0.08
- **Reaction:** Purr, lean in
- **Affection:** Medium

### 😸 Chin (Scratches)
- **Location:** Head bone (offset down)
- **Radius:** 0.06
- **Reaction:** Head tilt, eyes close
- **Affection:** Medium-High

### 👂 Ears
- **Location:** Ear bones
- **Radius:** 0.05
- **Reaction:** Ear twitch, purr
- **Affection:** Low-Medium

### ⚠️ Belly (Risky)
- **Location:** Chest bone (offset down)
- **Radius:** 0.1
- **Reaction:** Some cats love it, some bite!
- **Affection:** Variable (personality-dependent)
- **Note:** Could reduce affection for Independent cats

### 🚫 Tail (Off-Limits)
- **Location:** Tail base
- **Reaction:** Annoyance, tail swish
- **Affection:** Negative for most cats
- **Note:** Typically not pettable

---

## Reactions and Feedback

### Visual Feedback

**Particles:**
- Heart particles spawn at pet location
- Different colors for different affection gains
- Destroy after 2 seconds

**Floating Text:**
- "+2 Affection" appears at pet location
- Rises and fades

**Animations:**
- Cat plays "Happy" animation
- Head turns to look at hand (camera)
- Tail curls up (happiness indicator)

### Audio Feedback

**Sounds:**
- Purr sound (continuous during stroking)
- Happy chirp (on tap)
- Different purr intensities based on affection

### Animation Triggers

**AnimalController:**
- `TriggerAction("Happy")` - Plays happy animation
- Could also trigger:
  - "Purr" - Purring idle
  - "HeadRub" - Rubs against hand
  - "RollOver" - Very happy reaction

**IKController:**
- Head looks at camera (hand position)
- Tail curls up
- Body leans into petting

---

## Code Usage

### Basic Setup

```csharp
// Add PettingInteraction to scene
GameObject pettingManager = new GameObject("PettingManager");
PettingInteraction petting = pettingManager.AddComponent<PettingInteraction>();

// Configure
petting.pettableLayer = LayerMask.GetMask("Cat");
petting.affectionPerStroke = 2f;
petting.spawnParticles = true;
```

### Create Pet Spot Programmatically

```csharp
// Add pet spot to head bone
GameObject headBone = /* find head bone */;
GameObject petSpot = new GameObject("PetSpot_Head");
petSpot.transform.SetParent(headBone.transform);
petSpot.transform.localPosition = Vector3.zero;
petSpot.layer = LayerMask.NameToLayer("Cat");

// Add collider
SphereCollider collider = petSpot.AddComponent<SphereCollider>();
collider.radius = 0.1f;

// Add PetSpot component
PetSpot spot = petSpot.AddComponent<PetSpot>();
spot.spotName = "Head";
spot.spotType = PetSpotType.Head;
spot.isFavoriteSpot = true;
```

### Listen to Petting Events

```csharp
PetSpot spot = /* get spot */;

spot.OnPettingStartedEvent.AddListener(() => {
    Debug.Log("Started petting!");
});

spot.OnPettedEvent.AddListener(() => {
    Debug.Log("Petted!");
});

spot.OnPettingEndedEvent.AddListener(() => {
    Debug.Log("Stopped petting!");
});
```

---

## Input Controls

### Mouse Input

- **Left Click** - Tap pet
- **Left Click + Drag** - Stroke pet
- **Release** - End petting

### Touch Input

- **Tap** - Tap pet
- **Touch + Drag** - Stroke pet
- **Release** - End petting

### Settings

- **Allow Tap Petting:** Enable quick taps
- **Allow Stroke Petting:** Enable drag strokes
- **Min Stroke Distance:** 0.1m (prevent micro-movements)
- **Tap Cooldown:** 0.5s (prevent spam)
- **Max Petting Duration:** 10s (auto-end long sessions)

---

## Integration with Game Systems

### GameManager Integration

```csharp
// In PettingInteraction.ApplyAffection():
GameManager.Instance.OnCatAffectionChanged(catId, newAffection);

// GameManager then:
- Updates CatData.affection
- Updates UI
- Saves game state
```

### UI Integration

```csharp
// Show affection change in UI
void OnCatAffectionChanged(string catId, float newAffection) {
    CatData cat = playerProfile.cats[catId];
    uiManager.UpdateCatStats(cat);
    uiManager.ShowFloatingText($"+{affection:F0} Affection", cat.position);
}
```

### Camera Integration

```csharp
// In PettingInteraction.TriggerReaction():
IKController ik = cat.GetComponent<IKController>();
ik.SetLookAtTarget(Camera.main.transform); // Look at "hand"
```

---

## Configuration Tips

### For Casual Petting Experience
```
Affection Per Stroke: 3.0
Affection Per Tap: 2.0
Favorite Spot Bonus: 2.0x
Allow Tap Petting: ✓
Tap Cooldown: 0.3s
```

### For Realistic Cat Behavior
```
Affection Per Stroke: 1.0
Affection Per Tap: 0.5
Favorite Spot Bonus: 1.5x
Personality Modifiers: Enabled
Independent cats: 0.5x
```

### For Mobile (Touch-Friendly)
```
Min Stroke Distance: 0.15 (larger strokes)
Tap Cooldown: 0.4s (prevent accidental double-taps)
Show Floating Text: ✓ (visual feedback)
Pet Spot Radii: +20% (easier to tap)
```

---

## Troubleshooting

### Petting Not Working

**Check:**
1. PettingInteraction component in scene
2. Cat on correct layer ("Cat")
3. Pettable Layer set correctly
4. Pet spots have colliders
5. Colliders NOT set as triggers

**Fix:**
```csharp
// Verify layer
Debug.Log($"Cat layer: {cat.layer}");
Debug.Log($"Pettable mask: {petting.pettableLayer}");

// Ensure collider is NOT trigger
petSpot.GetComponent<Collider>().isTrigger = false;
```

### Affection Not Increasing

**Check:**
1. GameManager.Instance exists
2. Cat exists in PlayerProfile.cats
3. CatData.catId matches

**Fix:**
```csharp
// In PettingInteraction, add debug:
Debug.Log($"Cat ID: {catData?.catId}, Affection: {catData?.affection}");
```

### No Visual/Audio Feedback

**Check:**
1. Particle prefab assigned
2. Audio clip assigned
3. AudioSource component exists
4. Particles/sounds enabled in settings

**Fix:**
```csharp
// Check references
if (petParticlePrefab == null)
    Debug.LogWarning("Pet particle prefab not assigned!");

if (petSound == null)
    Debug.LogWarning("Pet sound not assigned!");
```

### Raycast Misses Pet Spot

**Check:**
1. Camera is Main Camera
2. Raycast distance (default: 100)
3. Collider size (might be too small)
4. UI blocking raycasts

**Fix:**
```csharp
// Increase collider radius
petSpot.GetComponent<SphereCollider>().radius = 0.15f;

// Increase raycast distance
petting.raycastDistance = 200f;
```

---

## Best Practices

1. **Head is Always Favorite** - Most cats love head petting
2. **Larger Radii for Back** - Allows for long strokes
3. **Smaller Radii for Face** - Precise petting (chin, ears)
4. **Use Personality Modifiers** - Makes cats feel unique
5. **Limit Petting Duration** - Prevents exploitation
6. **Test on Mobile** - Touch input needs larger targets
7. **Provide Clear Feedback** - Players need to see affection change

---

## Future Enhancements

### Planned Features
- 🎮 **Multi-Touch** - Pet multiple cats at once
- 😾 **Overpetting** - Too much petting annoys cat
- 🎯 **Combo System** - Pet multiple spots in sequence for bonus
- 📊 **Petting Mini-Game** - Simon-says style petting patterns
- 🏆 **Achievements** - "Pet 100 cats", "Master Petter"

### Possible Additions
- Different stroke directions (up/down vs. side/side)
- Pressure sensitivity (harder pets = more affection)
- Cat mood affects petting response
- Rare "petting moment" for bonus affection
- Petting unlocks new animations

---

## Example: Complete Setup

**1. Scene Setup:**
```
Scene
├── GameManager
│   └── PettingInteraction
├── Main Camera
│   └── CameraController
└── Cats (spawned at runtime)
```

**2. Cat Prefab:**
```
Cat_Simple_Medium
├── AnimalController
├── IKController
├── CatVisuals
└── Model
    ├── Head
    │   └── PetSpot_Head (SphereCollider, PetSpot)
    ├── Spine
    │   └── PetSpot_Back (SphereCollider, PetSpot)
    └── Chest
        └── PetSpot_Chest (SphereCollider, PetSpot)
```

**3. Workflow:**
1. Player clicks on cat's head
2. Raycast hits PetSpot_Head collider
3. PettingInteraction detects hit
4. Calculates affection (2.0 × 1.5 favorite × 1.5 personality = 4.5)
5. Updates CatData.affection via GameManager
6. Spawns heart particles
7. Plays purr sound
8. Triggers "Happy" animation
9. UI updates affection bar

---

**Happy petting!** 🤗🐱

For questions, check `PettingInteraction.cs` or `PetSpot.cs`.
