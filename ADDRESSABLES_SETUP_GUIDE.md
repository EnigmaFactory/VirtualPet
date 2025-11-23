# 📦 Addressables Setup for Red Deer Cat Models

Complete guide for setting up quality-based cat model loading with Addressables.

---

## 🎯 Goals

- Load cat models dynamically (not in build)
- Support 3 quality tiers: High (Default), Medium (NoAlpha), Low (LowPoly)
- Switch quality at runtime without restart
- Minimal WebGL build size
- Texture variants (Regular / Mobile)

---

## 📁 Step 1: Folder Structure

Create this structure in your project:

```
Assets/Addressables/
├── Cats/
│   ├── High/
│   │   ├── CatSimple.prefab [11,344 tris]
│   │   ├── KittenSimple.prefab
│   │   ├── CatStray.prefab
│   │   └── CatSkinny.prefab
│   │
│   ├── Medium/
│   │   ├── CatSimple_NoAlpha.prefab [10,590 tris]
│   │   ├── KittenSimple_NoAlpha.prefab
│   │   ├── CatStray_NoAlpha.prefab
│   │   └── CatSkinny_NoAlpha.prefab
│   │
│   └── Low/
│       ├── CatSimple_LowPoly.prefab [2,332 tris]
│       ├── KittenSimple_LowPoly.prefab
│       ├── CatStray_LowPoly.prefab
│       └── CatSkinny_LowPoly.prefab
│
└── Materials/
    ├── Regular/
    │   ├── Tabby.mat
    │   ├── Calico.mat
    │   ├── Black.mat
    │   ├── White.mat
    │   └── ... (all color variants)
    │
    └── Mobile/
        ├── Tabby_Mobile.mat
        ├── Calico_Mobile.mat
        ├── Black_Mobile.mat
        ├── White_Mobile.mat
        └── ... (all mobile variants)
```

---

## 🏗️ Step 2: Import Red Deer Models

### A. Import FBX Files

1. Copy Red Deer models to `Assets/ThirdParty/RedDeerCats/Models/`
2. For each model (CatSimple, KittenSimple, CatStray, CatSkinny):
   - Import all variants: Default, NoAlpha, LowPoly

### B. Create Prefabs

For each model × variant combination:

1. Drag FBX into scene
2. Add these components:
   - `AnimalController`
   - `IKController`
   - `MovementController`
   - `CatVisuals`
   - `Rigidbody`
   - `CapsuleCollider`
3. Configure IKController (assign bones)
4. Save as prefab in appropriate folder (High/Medium/Low)
5. Delete from scene

**Example: CatSimple_NoAlpha**

```
1. Drag CatSimple_NoAlpha.fbx to scene
2. Rename to "CatSimple_NoAlpha"
3. Add components (above)
4. Assign bones in IKController:
   - Spine, Chest, Head, Neck
   - Paws (all 4)
   - Tail bones
   - Ears
5. Save as "Assets/Addressables/Cats/Medium/CatSimple_NoAlpha.prefab"
6. Delete from scene
```

---

## 📦 Step 3: Mark as Addressables

### A. Make Folder Addressable

1. Select `Assets/Addressables/Cats/` folder
2. In Inspector, check **"Addressable"**
3. Set Address: `Cats` (this is the group name)

### B. Set Individual Addresses

For each prefab, set a specific address:

| Prefab | Address | Labels |
|--------|---------|--------|
| High/CatSimple.prefab | `Cats/Simple/High` | `cat`, `simple`, `high` |
| Medium/CatSimple_NoAlpha.prefab | `Cats/Simple/Medium` | `cat`, `simple`, `medium` |
| Low/CatSimple_LowPoly.prefab | `Cats/Simple/Low` | `cat`, `simple`, `low` |
| High/KittenSimple.prefab | `Cats/Kitten/High` | `cat`, `kitten`, `high` |
| Medium/KittenSimple_NoAlpha.prefab | `Cats/Kitten/Medium` | `cat`, `kitten`, `medium` |
| ... etc ... | | |

**Why labels?**
- Can query all "cat" assets
- Can query all "medium" quality assets
- Can query specific body types

### C. Create Addressable Groups

1. Open **Window > Asset Management > Addressables > Groups**
2. Create groups:
   - `Cats_High` (for High quality models)
   - `Cats_Medium` (for Medium quality models)
   - `Cats_Low` (for Low quality models)
   - `Cat_Materials` (for materials/textures)

3. Assign prefabs to groups:
   - All High/* prefabs → `Cats_High`
   - All Medium/* prefabs → `Cats_Medium`
   - All Low/* prefabs → `Cats_Low`
   - All materials → `Cat_Materials`

---

## 🎨 Step 4: Setup Materials

### A. Create Material Variants

For each color (Tabby, Calico, Black, etc.):

1. **Regular Material** (for High/Medium quality):
   - Shader: URP/Lit
   - Textures: Red Deer regular textures
   - Save in `Assets/Addressables/Materials/Regular/`

2. **Mobile Material** (for Low quality):
   - Shader: URP/Simple Lit (simpler)
   - Textures: Red Deer mobile textures
   - Save in `Assets/Addressables/Materials/Mobile/`

### B. Make Materials Addressable

1. Select all materials in `Assets/Addressables/Materials/`
2. Check **"Addressable"**
3. Set addresses:
   - `Materials/Tabby` (regular)
   - `Materials/Tabby_Mobile` (mobile)
   - etc.

---

## ⚙️ Step 5: Create CatModelConfig ScriptableObject

### A. Create Asset

1. In Project window: Right-click > Create > VirtualPet > Cat Model Config
2. Name it `CatModelConfig`
3. Save in `Assets/Resources/` (so it can be loaded without Addressables)

### B. Configure Model Variants

In the Inspector:

**Simple Config:**
```
Body Type: Simple
Variants (array size 3):
  [0] High
    - Name: "CatSimple Default"
    - Quality: High
    - Model Reference: Cats/Simple/High
    - Tri Count: 11344
    - Use Mobile Textures: false

  [1] Medium
    - Name: "CatSimple NoAlpha"
    - Quality: Medium
    - Model Reference: Cats/Simple/Medium
    - Tri Count: 10590
    - Use Mobile Textures: false

  [2] Low
    - Name: "CatSimple LowPoly"
    - Quality: Low
    - Model Reference: Cats/Simple/Low
    - Tri Count: 2332
    - Use Mobile Textures: true
```

Repeat for Kitten Config, Stray Config, Skinny Config.

### C. Configure Color Variants

```
Color Variants (array):
  [0] Tabby
    - Color ID: "tabby"
    - Display Name: "Tabby"
    - Regular Material: Materials/Tabby
    - Mobile Material: Materials/Tabby_Mobile
    - Preview Sprite: (optional)
    - UI Tint Color: (brown)

  [1] Calico
    - Color ID: "calico"
    - Display Name: "Calico"
    - Regular Material: Materials/Calico
    - Mobile Material: Materials/Calico_Mobile
    - Preview Sprite: (optional)
    - UI Tint Color: (orange/white/black)

  ... (repeat for all colors)
```

---

## 🧪 Step 6: Test Loading

### A. Create Test Scene

1. Create empty scene: `Scenes/AddressablesTest.unity`
2. Add GameObject: "GraphicsQualityManager"
   - Add component: `GraphicsQualityManager`
3. Add GameObject: "TestCat"
   - Add component: `CatVisuals`
   - Assign Model Config reference

### B. Test Quality Switching

```csharp
// In Unity Console or DevTools:

// Load a cat
CatVisuals catVisuals = FindObjectOfType<CatVisuals>();
catVisuals.SetBodyType(CatBodyType.Simple);

// Change quality
GraphicsQualityManager.Instance.SetQuality(GraphicsQuality.Low);
// Cat should reload with LowPoly variant

// Change again
GraphicsQualityManager.Instance.SetQuality(GraphicsQuality.High);
// Cat should reload with Default variant
```

### C. Check Build Size

1. Build for WebGL
2. Check `Build/WebGL/Build/` folder size
3. Models should NOT be in main bundle
4. Each quality group should be separate .bundle file

---

## 📊 Step 7: Addressables Build Settings

### A. Configure Groups

For each group (`Cats_High`, `Cats_Medium`, `Cats_Low`):

**Build Settings:**
- Build Path: `ServerData` (for remote loading)
  - Or `LocalBuildPath` (for local testing)
- Load Path: `ServerData` (for remote)
  - Or `LocalLoadPath` (for local)
- Bundle Mode: "Pack Together"
  - All models in this quality tier in one bundle
- Compression: LZ4 (fast decompression for WebGL)

**Advanced Settings:**
- Include In Build: ✓ (checked)
- Force Build: ✗ (unchecked)
- Bundle Timeout: 0

### B. Profile Settings

Create profiles for different platforms:

**Default (Desktop WebGL):**
- Load all quality tiers
- Default to Medium

**Mobile:**
- Load only Medium and Low
- Default to Low

---

## 🚀 Step 8: Build Addressables

1. Open **Window > Asset Management > Addressables > Groups**
2. Click **Build > New Build > Default Build Script**
3. Wait for build to complete
4. Check console for any errors
5. Verify bundles created in `ServerData/` folder

---

## 🌐 Step 9: WebGL Integration

### A. Host Addressables Remotely (Optional)

For smaller initial load:

1. Upload `ServerData/` folder to CDN/web server
2. Update Addressables Profile:
   - Remote Load Path: `https://yourcdn.com/ServerData`
3. Rebuild Addressables

### B. Local Bundles (Simpler)

Keep bundles in build:
- Load Path: `{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]`

---

## 🔧 Step 10: Usage in Code

### Load a cat:

```csharp
// CatVisuals component handles this automatically
CatVisuals catVisuals = catObject.AddComponent<CatVisuals>();
catVisuals.SetBodyType(CatBodyType.Simple);
catVisuals.SetColorVariant("tabby");
// Automatically loads correct quality variant!
```

### Change quality at runtime:

```csharp
// User changes setting
GraphicsQualityManager.Instance.SetQuality(GraphicsQuality.Low);
// All cats reload with LowPoly variants automatically!
```

### Manual loading (advanced):

```csharp
using UnityEngine.AddressableAssets;

// Load specific variant
var handle = Addressables.InstantiateAsync("Cats/Simple/Medium");
await handle;
GameObject cat = handle.Result;
```

---

## 🐛 Troubleshooting

### "Asset not found"
- Check address is correct (case-sensitive!)
- Rebuild Addressables (Build > New Build)
- Check asset is marked Addressable

### "Models not loading"
- Check Addressables initialized: `await Addressables.InitializeAsync()`
- Check internet connection (if remote)
- Check Console for errors

### "Wrong quality loading"
- Check GraphicsQualityManager.CurrentQuality
- Check CatModelConfig has all variants assigned
- Check prefab addresses match config

### "WebGL build huge"
- Check Addressables Build Path is ServerData (not local)
- Check groups are set to Pack Together
- Check compression is LZ4 or Uncompressed

---

## 📈 Expected Results

### Build Sizes:

**Without Addressables:**
- WebGL Build: ~150-200 MB (all models included)

**With Addressables:**
- Initial WebGL Build: ~50-80 MB (code + UI only)
- Cats_Medium bundle: ~30-40 MB (loaded on first cat)
- Cats_Low bundle: ~8-12 MB (loaded if quality downgraded)
- Cats_High bundle: ~40-50 MB (loaded if quality upgraded)

### Load Times:

- First cat spawn: 2-5 seconds (downloads + instantiates)
- Subsequent cats: <1 second (cached)
- Quality switch: 1-3 seconds (unload + reload)

---

## ✅ Verification Checklist

- [ ] All 12 cat prefabs created (4 body types × 3 qualities)
- [ ] All prefabs have components (AnimalController, IKController, etc.)
- [ ] All prefabs marked Addressable with correct addresses
- [ ] All materials created (Regular + Mobile variants)
- [ ] CatModelConfig created and configured
- [ ] Addressables groups created (Cats_High, Medium, Low)
- [ ] Addressables built successfully
- [ ] Test scene loads cat with Medium quality
- [ ] Quality switching works (High → Medium → Low)
- [ ] WebGL build is <100 MB initial
- [ ] Models load in-game successfully

---

## 🎯 Next Steps

Once Addressables is working:

1. **Add more color variants** (more materials)
2. **Add body evolution** (Kitten grows to Simple over time)
3. **Add blendshapes** (smooth morphing between body types)
4. **Add procedural fur** (replace textures with fur shader)
5. **Remote hosting** (CDN for faster global loads)

---

**You're ready to build a quality-adaptive, efficiently-loading cat system!** 🐱📦
