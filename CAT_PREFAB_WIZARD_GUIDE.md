# 🎨 Cat Prefab Wizard - Quick Start Guide

**Location:** Window > Virtual Pet > Cat Prefab Wizard

The Cat Prefab Wizard automates the entire cat prefab setup workflow, from Red Deer model import to Addressables configuration.

---

## What It Does

✅ **Creates production-ready cat prefabs** with all components
✅ **Auto-detects bones** for IK system
✅ **Marks prefabs as Addressable** with proper addresses and labels
✅ **Updates CatModelConfig** ScriptableObject automatically
✅ **Supports all quality tiers** (High/Medium/Low)
✅ **Handles all 4 body types** (Simple, Kitten, Stray, Skinny)

---

## Step-by-Step Workflow

### 📦 **Step 1: Select Red Deer Models**

1. Choose body type: Simple, Kitten, Stray, or Skinny
2. Drag in FBX models from Red Deer Cat Family Pack:
   - **High Quality** (Default with alpha) - ~11,344 tris
   - **Medium Quality** (NoAlpha) ⭐ Recommended - ~10,590 tris
   - **Low Quality** (LowPoly for mobile) - ~2,332 tris
3. Select output folder (default: `Assets/Addressables/Cats/`)
4. Click **Next ▶**

**Tip:** You can select 1-3 quality variants. The wizard will create all selected variants.

---

### 🦴 **Step 2: Configure Bones**

**Option A: Auto-Detect (Recommended)**
1. Enable "Auto-Detect Bones"
2. Click **🔍 Auto-Detect Now**
3. Wizard searches for bones by common names
4. Click **Next ▶** when bones are detected

**Option B: Manual Assignment**
1. Disable "Auto-Detect Bones"
2. Manually assign each bone using hierarchy picker
3. Required bones: spine, head, 4 paws
4. Optional: chest, neck, tail, ears

The wizard validates that all required bones are assigned before proceeding.

---

### ⚙️ **Step 3: Setup Components**

Review which components will be added:
- ✅ **AnimalController** - Animation playback and blending
- ✅ **IKController** - Paw IK, head look-at, tail control
- ✅ **MovementController** - Navigation, wandering, jumping
- ✅ **CatVisuals** - Model loading, quality switching
- ✅ **Physics Components** - Rigidbody, CapsuleCollider

**Options:**
- ☑️ **Mark as Addressable** - Auto-setup Addressables (recommended)
- ☑️ **Update Config** - Auto-update CatModelConfig (recommended)

Click **Next ▶**

---

### 🎨 **Step 4: Create Prefabs**

**Review summary:**
- Body Type: Simple (or chosen type)
- Variants to create: 3 (High, Medium, Low)
- Output folder path
- Bones configured: Yes ✅

Click **🎨 CREATE PREFABS**

**The wizard will:**
1. Create prefab variants in quality folders
2. Add all components with proper configuration
3. Assign bone references via SerializedObject
4. Mark prefabs as Addressable with addresses like:
   - `Cats/Simple/High`
   - `Cats/Simple/Medium`
   - `Cats/Simple/Low`
5. Apply labels: `cat`, `simple`, `high`/`medium`/`low`
6. Create or update `CatModelConfig` ScriptableObject
7. Set AssetReferenceGameObject GUIDs

---

### 🎉 **Step 5: Complete!**

**Created prefabs appear in list:**
- Click **Select** to ping in Project window
- Click **🔄 Create More Prefabs** to set up another body type
- Click **📚 Open Addressables Guide** for next steps

**Next Steps:**
1. ✅ Setup Addressables (Window > Asset Management > Addressables)
2. ✅ Assign animation clips in AnimalController
3. ✅ Test in DevTools (Window > Virtual Pet > Dev Tools)

---

## Output Structure

```
Assets/
├── Addressables/
│   └── Cats/
│       ├── High/
│       │   ├── Simple_High.prefab
│       │   ├── Kitten_High.prefab
│       │   ├── Stray_High.prefab
│       │   └── Skinny_High.prefab
│       ├── Medium/
│       │   ├── Simple_Medium.prefab
│       │   └── ... (same)
│       └── Low/
│           ├── Simple_Low.prefab
│           └── ... (same)
└── Resources/
    └── CatModelConfig.asset
```

---

## CatModelConfig ScriptableObject

The wizard automatically creates/updates this config:

```csharp
CatModelConfig
├── simpleConfig (BodyTypeConfig)
│   ├── bodyType: Simple
│   └── variants: [High, Medium, Low]
│       ├── name: "Simple_High"
│       ├── quality: High
│       ├── modelReference: AssetReferenceGameObject (GUID)
│       ├── triCount: 11344
│       └── useMobileTextures: false
├── kittenConfig (same structure)
├── strayConfig (same structure)
└── skinnyConfig (same structure)
```

**Location:** `Assets/Resources/CatModelConfig.asset`

---

## Addressables Addresses and Labels

Each created prefab is marked as Addressable with:

**Address Format:** `Cats/{BodyType}/{Quality}`

**Examples:**
- `Cats/Simple/High`
- `Cats/Kitten/Medium`
- `Cats/Stray/Low`

**Labels Applied:**
- `cat` (all cat prefabs)
- `simple`/`kitten`/`stray`/`skinny` (body type)
- `high`/`medium`/`low` (quality tier)

**Usage in Code:**
```csharp
// Load specific cat
Addressables.LoadAssetAsync<GameObject>("Cats/Simple/Medium");

// Load all cats
Addressables.LoadAssetsAsync<GameObject>("cat", null);

// Load all medium quality
Addressables.LoadAssetsAsync<GameObject>("medium", null);

// Load all simple cats
Addressables.LoadAssetsAsync<GameObject>("simple", null);
```

---

## Typical Workflow

**Creating all 12 cat prefabs (4 body types × 3 qualities):**

1. Run wizard for **Simple** cat → Create 3 variants
2. Run wizard for **Kitten** cat → Create 3 variants
3. Run wizard for **Stray** cat → Create 3 variants
4. Run wizard for **Skinny** cat → Create 3 variants

**Result:** 12 prefabs, all marked as Addressable, CatModelConfig fully populated!

---

## Troubleshooting

### ⚠️ "Addressables package not installed"
**Solution:** Install Addressables via Package Manager
Window > Package Manager > Unity Registry > Addressables > Install

### ⚠️ "Addressables not initialized"
**Solution:** Create Addressables settings
Window > Asset Management > Addressables > Groups > Create Addressables Settings

### ⚠️ "Missing required bones"
**Solution:**
1. Try Auto-Detect again
2. Manually assign bones using hierarchy picker
3. Required: spine, head, frontLeftPaw, frontRightPaw, backLeftPaw, backRightPaw

### ⚠️ "Could not find field in CatModelConfig"
**Solution:** Ensure CatModelConfig.cs has correct field names:
- `simpleConfig` (BodyTypeConfig)
- `kittenConfig` (BodyTypeConfig)
- `strayConfig` (BodyTypeConfig)
- `skinnyConfig` (BodyTypeConfig)

---

## Advanced: Using Without Addressables

If you don't want to use Addressables:
1. Uncheck "Mark as Addressable" in Step 3
2. Wizard will still create prefabs and assign bones
3. Manually reference prefabs in CatModelConfig inspector

---

## Components Added to Each Prefab

### AnimalController
- Animation clip slots (assign manually)
- Blend times: 0.3s default
- State-based animation system

### IKController
- **Bones assigned:** spine, chest, head, neck, 4 paws, tail, ears
- **Paw IK:** Enabled (raycasts to ground)
- **Head IK:** Enabled (look-at targets)
- **Tail IK:** Disabled if using Dynamic Bone

### MovementController
- Walk speed: 1.0
- Run speed: 3.0
- Wandering enabled
- NavMeshAgent auto-added

### CatVisuals
- Body type: Set to prefab body type
- Color variant: "tabby" (default)
- Evolution enabled

### Physics
- **Rigidbody:**
  - Mass: 4.5 kg (average cat)
  - Drag: 1.0
  - Angular Drag: 0.5
  - Freeze Rotation: X, Y, Z (controlled by code)
- **CapsuleCollider:**
  - Radius: 0.15
  - Height: 0.3
  - Center: (0, 0.15, 0)

---

## Next: Animation Assignment

After creating prefabs, assign Red Deer animation clips:

1. Select prefab in Project window
2. Find **AnimalController** component
3. Assign clips:
   - Idle Clip → Red Deer "Idle" animation
   - Walk Clip → Red Deer "Walk" animation
   - Run Clip → Red Deer "Run" animation
   - Sit Clip → Red Deer "Sit" animation
   - Sleep Clip → Red Deer "Sleep" animation
   - etc.

See `RED_DEER_SETUP.md` for full animation list.

---

## Testing Your Cats

1. Open DevTools window (Window > Virtual Pet > Dev Tools)
2. Enter Play Mode
3. Click **"Adopt Cat"**
4. Watch your cat spawn, animate, and move!

**Keyboard shortcuts (in Play Mode):**
- `F1` - Adopt cat
- `F2` - Feed cat
- `F3` - Pet cat
- `F4` - Make cat sleep

---

**You're ready to rapidly create production-ready cat prefabs!** 🐱✨

For questions or issues, check `ADDRESSABLES_SETUP_GUIDE.md` or `RED_DEER_SETUP.md`.
