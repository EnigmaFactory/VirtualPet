# 🐱 Red Deer Cat Family Pack - Integration Guide

This guide shows how to integrate the Red Deer Cat Family Pack animations with our IK-enhanced animation system.

---

## 📦 Step 1: Import Red Deer Cats

1. Import the Red Deer Cat Family Pack into Unity
2. Extract to: `Assets/ThirdParty/RedDeerCats/`
3. Suggested folder structure:
```
Assets/ThirdParty/RedDeerCats/
├── Models/
│   ├── Cat_01.fbx
│   ├── Cat_02.fbx
│   └── ... (all cat variants)
├── Animations/
│   ├── Idle.anim
│   ├── Walk.anim
│   ├── Run.anim
│   ├── Sit.anim
│   ├── Sleep.anim
│   └── ... (all animation clips)
├── Materials/
└── Textures/
```

---

## 🎬 Step 2: Set Up Animation Clips

The `AnimalController` expects these animation clips from Red Deer:

### **Required Animations:**
- `Idle` - Standing still, breathing
- `Walk` - Walking loop
- `Run` - Running loop
- `Sit` - Sitting pose (looping)
- `Sleep` - Sleeping loop

### **Recommended Animations:**
- `Idle_Variation_01` - Look around, tail flick, etc.
- `Idle_Variation_02` - Stretch, yawn
- `LayDown` - Transition from standing to laying
- `Groom` - Licking paw, grooming
- `Eat` - Eating from bowl
- `Jump` - Jump (one-shot)
- `Land` - Landing from jump
- `Play_01` - Pounce
- `Play_02` - Bat with paw
- `Play_03` - Roll over

### **Nice to Have:**
- `SitToStand` - Smooth transition
- `Stretch` - Wake-up stretch
- `Drink` - Drinking from bowl

---

## 🦴 Step 3: Identify Bone Names

Find the bone hierarchy in Red Deer cats. Common names:

```
Cat_Root
├── Pelvis
│   ├── Spine_01
│   │   ├── Spine_02
│   │   │   ├── Chest
│   │   │   │   ├── Neck_01
│   │   │   │   │   ├── Neck_02
│   │   │   │   │   │   └── Head
│   │   │   │   │   │       ├── Ear_L
│   │   │   │   │   │       └── Ear_R
│   │   │   │   ├── FrontLeg_L
│   │   │   │   │   └── ... → Paw_L
│   │   │   │   └── FrontLeg_R
│   │   │   │       └── ... → Paw_R
│   │   └── Tail_01
│   │       ├── Tail_02
│   │       └── ... (tail chain)
│   ├── BackLeg_L
│   │   └── ... → Paw_L
│   └── BackLeg_R
│       └── ... → Paw_R
```

**IMPORTANT:** Note the exact names - you'll assign these in the Inspector.

---

## 🎮 Step 4: Create Cat Prefab

### A. Create Base Prefab

1. Drag a Red Deer cat model into scene
2. Rename to `Cat_Playful` (or personality type)

### B. Add Components

Add these components **in this order**:

1. **AnimalController**
   - Assign all animation clips from Red Deer pack
   - Set blend times (0.3s is good default)

2. **IKController**
   - Assign bone references (spine, chest, head, etc.)
   - Assign all 4 paw bones
   - Assign tail base and tail chain bones
   - Assign ear bones (if separate)
   - Enable paw IK, head IK
   - Disable tail IK if using Dynamic Bone

3. **MovementController**
   - Set walk speed (~1.0)
   - Set run speed (~3.0)
   - Enable wandering

4. **NavMeshAgent** (auto-added by MovementController)
   - Radius: 0.2
   - Height: 0.4 (or cat height)
   - Base Offset: 0

5. **Rigidbody** (for physics)
   - Mass: 4.5 (average cat weight in kg)
   - Drag: 1
   - Angular Drag: 0.5
   - Use Gravity: ✓
   - Is Kinematic: ✗
   - Constraints: Freeze Rotation X, Y, Z (rotation handled by code)

6. **Capsule Collider** (or use cat mesh collider)
   - Radius: 0.15
   - Height: 0.3
   - Center: (0, 0.15, 0)

### C. Optional: Dynamic Bone

If you have Dynamic Bone asset:

1. Add `DynamicBone` component
2. Set Root: `Tail_01`
3. Set Damping: 0.1
4. Set Elasticity: 0.05
5. Set Stiffness: 0.2
6. Set Inert: 0.1
7. In `IKController`:
   - Set "Use Dynamic Bone": ✓
   - Assign the DynamicBone component

---

## 🎨 Step 5: Assign Animations in Inspector

With your cat prefab selected:

### AnimalController Component:
```
Idle Clip:          RedDeerCats/Animations/Idle
Idle Variations:    [Idle_Variation_01, Idle_Variation_02]
Walk Clip:          RedDeerCats/Animations/Walk
Run Clip:           RedDeerCats/Animations/Run
Sit Clip:           RedDeerCats/Animations/Sit
Sit To Stand Clip:  RedDeerCats/Animations/SitToStand
Lay Down Clip:      RedDeerCats/Animations/LayDown
Sleep Clip:         RedDeerCats/Animations/Sleep
Sleep Variations:   [Sleep_Breathing, Sleep_Twitch]
Groom Clip:         RedDeerCats/Animations/Groom
Stretch Clip:       RedDeerCats/Animations/Stretch
Jump Clip:          RedDeerCats/Animations/Jump
Land Clip:          RedDeerCats/Animations/Land
Play Clips:         [Play_Pounce, Play_Bat, Play_Roll]
Eat Clip:           RedDeerCats/Animations/Eat
Drink Clip:         RedDeerCats/Animations/Drink
```

### IKController Component:
```
Spine:              Cat_Root/Pelvis/Spine_01
Chest:              .../Spine_02/Chest
Head:               .../Neck_02/Head
Neck:               .../Neck_01

Front Left Paw:     .../FrontLeg_L/.../Paw_L
Front Right Paw:    .../FrontLeg_R/.../Paw_R
Back Left Paw:      .../BackLeg_L/.../Paw_L
Back Right Paw:     .../BackLeg_R/.../Paw_R

Tail Base:          .../Tail_01
Tail Bones:         [Tail_01, Tail_02, Tail_03, ...]

Left Ear:           .../Head/Ear_L
Right Ear:          .../Head/Ear_R
```

**TIP:** Use the hierarchy picker (circle button) to select bones easily!

---

## 🧪 Step 6: Test the Cat

1. Create test scene
2. Add NavMesh to floor (Window > AI > Navigation > Bake)
3. Drag cat prefab into scene
4. Add `DevTools` GameObject
5. Press **Play**
6. Open **Window > Virtual Pet > Dev Tools**
7. Click **"Adopt Cat"**
8. Watch it move, animate, and interact!

### What to Look For:

✅ **Idle**: Breathing animation, occasional idle variations
✅ **Walk**: Smooth walk cycle when moving
✅ **Run**: Transitions to run at higher speeds
✅ **IK**: Paws adjust to ground, head turns naturally
✅ **Tail**: Sways gently (or Dynamic Bone physics)
✅ **Ears**: Occasional twitches

---

## 🎯 Step 7: Create Interaction Points

### A. Create Furniture with Interaction Points

1. Create furniture model (bed, chair, etc.)
2. Add empty GameObject as child
3. Rename to "InteractionPoint_Sit" (or type)
4. Add `InteractionPoint` component
5. Set Interaction Type (Sit, LayDown, etc.)
6. Position at exact spot cat should be
7. Rotate forward direction where cat should face

### B. Example: Cat Bed

```
CatBed (3D model)
├── InteractionPoint_Sleep
│   ├── FrontLeftPaw_Target (optional)
│   ├── FrontRightPaw_Target (optional)
│   └── HeadLook_Target (optional)
└── Collider
```

### C. Test Interaction

In DevTools window:
```csharp
// Find interaction point
var bed = GameObject.Find("CatBed/InteractionPoint_Sleep").GetComponent<InteractionPoint>();

// Find cat
var cat = GameObject.FindObjectOfType<MovementController>();

// Make cat go to bed
cat.MoveToInteraction(bed);
```

Cat should:
1. Walk to bed
2. Climb onto it
3. Lay down
4. Start sleeping animation
5. IK paws to bed surface!

---

## 🔧 Troubleshooting

### Cat not animating
- Check Animation component has clips added
- Verify AnimalController has clips assigned
- Check Console for errors

### IK not working
- Ensure bone references are correct (check names)
- Enable "Enable Paw IK" in IKController
- Check ground layer mask includes floor

### Cat sliding/floating
- Adjust NavMeshAgent base offset
- Check collider size matches cat
- Ensure ground has collider

### Tail not moving
- If using Dynamic Bone, ensure component is added
- If not, enable "Enable Tail IK" in IKController
- Check tail bone chain is assigned

### Cat walks through walls
- Add colliders to walls
- Check NavMesh baking includes obstacles
- Increase NavMeshAgent radius

---

## 💡 Advanced: Custom Animations

You can add custom animations:

1. Create animation clip in Unity
2. Assign to `AnimalController` custom slot
3. Call via code:
```csharp
animalController.TriggerAction("customAnimName");
```

---

## 🎨 Multiple Cat Variants

Red Deer pack has multiple cat models. Create prefab variants:

1. Create base prefab (above)
2. Duplicate for each cat model
3. Replace mesh/materials
4. Keep all scripts and settings
5. Save as `Cat_Tabby`, `Cat_Calico`, etc.

Then in `CatData`, reference different prefabs:
```csharp
catData.variantIndex = 0; // Tabby
catData.variantIndex = 1; // Calico
// etc.
```

---

## 🚀 Next Steps

Once Red Deer is integrated:

1. **Create room with NavMesh**
2. **Add furniture with InteractionPoints**
3. **Set up toys (balls, feather wands)**
4. **Test cat AI wandering and interactions**
5. **Build camera system to follow cats**
6. **Add UI to spawn/interact with cats**

---

**You're ready to bring cats to life!** 🐱✨

Check `DevToolsWindow.cs` for testing shortcuts or ask for help with specific animations!
