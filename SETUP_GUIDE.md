# Bob's Petroleum - Complete Setup Guide
## The MEGA Detailed Unity Implementation Guide

This guide will walk you through EVERY step to set up Bob's Petroleum in Unity. Follow exactly.

---

# PART 1: INITIAL UNITY SETUP

## Step 1.1: Open Unity

1. Launch Unity Hub
2. Click "Open" button
3. Navigate to the Bob folder
4. Click "Select Folder"
5. Wait for Unity to import all scripts (may take 2-5 minutes)

## Step 1.2: Create Your Main Scene

1. In Unity, go to **File → New Scene**
2. Save it: **File → Save As**
3. Navigate to `Assets/Scenes/`
4. Name it `MainGame.unity`
5. Click **Save**

---

# PART 2: ONE-CLICK SETUP (RECOMMENDED)

## Step 2.1: Open the MASTER SETUP Tool

1. In Unity's top menu bar, click **Window**
2. Hover over **Bob's Petroleum**
3. Click **MASTER SETUP**
4. A comprehensive 5-tab window opens

## Step 2.2: Follow the Tabs in Order

### Tab 1: Asset Checklist
- Review what assets you need to prepare
- Check off items as you import them
- Green checkmarks = ready, Yellow = needs attention

### Tab 2: Scene Setup
1. Click the big button: **Create Complete Scene**
2. All managers, player, and UI are created automatically
3. Check Console for "Complete scene created!"

### Tab 3: Prefab Setup
1. Drag your character models into the prefab slots
2. Click "Configure Prefab" for each
3. NFT models go in the NFT Models array

### Tab 4: Validation
1. Click "Validate All Systems"
2. Green = good, Red = needs fixing
3. Click "Auto-Fix Issues" to resolve problems

### Tab 5: Build Checklist
- Final checks before building for WebGL
- Ensures everything is ready for deployment

## Step 2.3: What Was Created

Look in your Hierarchy panel (left side). You should see:
- `---MANAGERS---` (folder object containing all managers)
  - `GameBootstrapper`
  - `GameManager`
  - `AudioManager`
  - `DayNightCycle`
  - `HorrorEventsSystem`
  - `RandomEventsSystem`
  - `QuestSystem`
- `GameCanvas` (UI container)
  - `HUDManager`
  - `PauseMenu`
  - `MinimapSystem`
- `EventSystem`
- `Player`

---

# PART 3: MANUAL SETUP (Alternative)

If Quick Setup didn't work, do this manually:

## Step 3.1: Create the Managers Parent

1. Right-click in Hierarchy panel (empty area)
2. Click **Create Empty**
3. Name it `---MANAGERS---`

## Step 3.2: Create Each Manager

For EACH of the following, repeat these exact steps:

### GameBootstrapper
1. Right-click on `---MANAGERS---`
2. Click **Create Empty**
3. Name it `GameBootstrapper`
4. With it selected, look at Inspector panel (right side)
5. Click **Add Component** button
6. Type `GameBootstrapper` in search box
7. Click on `GameBootstrapper` script

### GameManager
1. Right-click on `---MANAGERS---`
2. Click **Create Empty**
3. Name it `GameManager`
4. Click **Add Component**
5. Type `GameManager`
6. Click the script

### AudioManager
1. Same process - create empty child of `---MANAGERS---`
2. Name it `AudioManager`
3. Add Component → `AudioManager`

### DayNightCycle
1. Create empty → name `DayNightCycle`
2. Add Component → `DayNightCycle`

### Continue for:
- `HorrorEventsSystem`
- `RandomEventsSystem`
- `QuestSystem`
- `ShopManager`
- `DialogueSystem`
- `PetCaptureSystem`

---

# PART 4: CREATING THE PLAYER

## Step 4.1: Use Quick Create

1. Go to **Window → Bob's Petroleum → Setup Wizard**
2. Expand the **🎮 Player** section
3. Click **Create Player**

OR do it manually:

## Step 4.2: Manual Player Creation

1. Right-click in Hierarchy → **Create Empty**
2. Name it `Player`
3. Click on Player to select it
4. In Inspector, click **Add Component**
5. Add these components (one at a time):
   - `Character Controller`
   - `Player Controller`
   - `Player Health`
   - `Player Inventory`

### Configure Character Controller:
1. Click on Character Controller component
2. Set **Height** to `1.8`
3. Set **Radius** to `0.4`
4. Set **Center** to `0, 0.9, 0`

### Create Camera:
1. Right-click on `Player` object
2. Click **Create Empty**
3. Name it `CameraHolder`
4. Select `CameraHolder`
5. In Inspector, set Position to `0, 1.6, 0`
6. Click **Add Component** → `Camera`
7. Click **Add Component** → `Audio Listener`
8. Change the **Tag** dropdown to `MainCamera`

### Create Flashlight:
1. Right-click on `CameraHolder`
2. Click **Create Empty**
3. Name it `Flashlight`
4. Add Component → `Flashlight`
5. Add Component → `Light`
6. In the Light component, set **Type** to `Spot`

### Set Player Tag:
1. Click on the `Player` object
2. At the top of Inspector, find **Tag** dropdown
3. Click it and select `Player`

### Position Player:
1. In the Transform component, set Position to `0, 1, 0`

---

# PART 5: SETTING UP UI

## Step 5.1: Create Canvas

1. Right-click in Hierarchy
2. Go to **UI → Canvas**
3. A Canvas and EventSystem are created automatically

## Step 5.2: Configure Canvas

1. Click on `Canvas`
2. In Inspector, find **Canvas** component
3. Set **Render Mode** to `Screen Space - Overlay`
4. Find **Canvas Scaler** component
5. Set **UI Scale Mode** to `Scale With Screen Size`
6. Set **Reference Resolution** to `1920 x 1080`

## Step 5.3: Add HUD Manager

1. Right-click on `Canvas`
2. **Create Empty**
3. Name it `HUDManager`
4. Add Component → `HUD Manager`

## Step 5.4: Create Basic HUD Elements

### Health Bar:
1. Right-click on `HUDManager`
2. **UI → Image**
3. Name it `HealthBarBackground`
4. Position: Anchor to top-left, Position X: 100, Y: -50, Width: 200, Height: 20
5. Right-click on `HealthBarBackground` → **UI → Image**
6. Name it `HealthBarFill`
7. Set same size
8. Change Image Type to `Filled` → `Horizontal`
9. In HUDManager Inspector, drag `HealthBarFill` to the `Health Bar Fill` slot

### Money Text:
1. Right-click on `HUDManager`
2. **UI → Text - TextMeshPro**
3. Name it `MoneyText`
4. Position in top-right corner
5. Set text to `$0`
6. Drag to HUDManager's `Money Text` slot

---

# PART 6: CREATING NPCS

## Step 6.1: Create a Zombie

### From Scratch:
1. Import your 3D character model (drag FBX into Project)
2. Drag model into Scene
3. With model selected:
4. Add Component → `NavMesh Agent`
5. Add Component → `Zombie AI`
6. Add Component → `Animator`
7. Add Component → `Simple Animation Player`

### Configure NavMesh Agent:
- Speed: `3.5`
- Angular Speed: `120`
- Acceleration: `8`
- Stopping Distance: `2`

### Configure ZombieAI:
- Attack Damage: `25`
- Attack Range: `2`
- Detection Range: `15`

### Add Collider:
1. Add Component → `Capsule Collider`
2. Height: `2`
3. Radius: `0.5`
4. Center: `0, 1, 0`

### Set Layer:
1. At top of Inspector, click **Layer** dropdown
2. Click **Add Layer...**
3. Add a layer named `Enemy`
4. Go back to zombie, set Layer to `Enemy`

## Step 6.2: Create a Customer

Same process as zombie, but:
1. Add Component → `Customer AI` (not Zombie AI)
2. NavMesh Speed: `2` (slower)
3. Layer: Create/use `NPC` layer

## Step 6.3: Using Quick Setup Component

Alternative faster method:
1. Select any 3D model in scene
2. Add Component → `Quick Setup`
3. In Quick Setup component, select **Setup Type**: `Zombie`
4. Right-click component → **Run Setup**
5. All components added automatically!

---

# PART 7: SETTING UP ANIMATIONS (Meshy/Mixamo)

## Step 7.1: Import Animation Clips

1. Download animations from Meshy or Mixamo
2. Drag the FBX files into `Assets/Animations/`
3. Click on imported FBX
4. In Inspector, go to **Rig** tab
5. Set **Animation Type** to `Humanoid`
6. Click **Apply**
7. Go to **Animation** tab
8. Check **Loop Time** for looping animations (Idle, Walk, Run)
9. Click **Apply**

## Step 7.2: Configure Simple Animation Player

1. Select your character
2. Find **Simple Animation Player** component
3. Click **+** in **Animation Clips** list

For each animation:
1. Set **Animation Name**: e.g., `Walk`
2. Drag the animation clip to **Clip** field
3. Check **Return To Default** if it should go back to Idle after

Example setup:
```
Animation Clips:
- Idle (clip: idle_anim, returnToDefault: false)
- Walk (clip: walk_anim, returnToDefault: false)
- Run (clip: run_anim, returnToDefault: false)
- Attack (clip: attack_anim, returnToDefault: true)
- Death (clip: death_anim, returnToDefault: false)
```

## Step 7.3: Create Animator Controller

1. Select character
2. Find **Runtime Animator Setup** component (add if missing)
3. Right-click component → **Create Controller From Clips**
4. A controller is created at `Assets/GeneratedAnimators/`

## Step 7.4: Playing Animations from Code

In your scripts, call:
```csharp
GetComponent<SimpleAnimationPlayer>().Play("Walk");
GetComponent<SimpleAnimationPlayer>().PlayOnce("Attack");
GetComponent<SimpleAnimationPlayer>().PlayLooped("Idle");
```

---

# PART 8: SETTING UP NAVIGATION (NavMesh)

## Step 8.1: Open Navigation Window

1. Go to **Window → AI → Navigation**
2. Navigation window opens

## Step 8.2: Mark Walkable Areas

1. Select your ground/terrain object
2. In Navigation window, click **Object** tab
3. Check **Navigation Static**
4. Set **Navigation Area** to `Walkable`

## Step 8.3: Mark Obstacles

1. Select buildings/walls
2. Check **Navigation Static**
3. Set **Navigation Area** to `Not Walkable`

## Step 8.4: Bake the NavMesh

1. In Navigation window, click **Bake** tab
2. Set **Agent Radius**: `0.5`
3. Set **Agent Height**: `2`
4. Click **Bake** button
5. Wait for blue mesh to appear on ground

---

# PART 9: SETTING UP SHOPS

## Step 9.1: Create Weapon Shop

1. Create Empty → name `WeaponShop`
2. Add Component → `Shop System`
3. Set **Shop Type** to `Weapons`
4. Set **Shop Name** to `Bob's Arsenal`

### Add Items:
1. Expand **Shop Items** list
2. Click **+** to add item
3. Fill in:
   - Item ID: `pistol_01`
   - Item Name: `Pistol`
   - Description: `Basic sidearm`
   - Price: `500`
   - Item Type: `Weapon`

4. Create weapon data:
   - Right-click in Project → **Create → Bob's Petroleum → Weapon Data**
   - Name it `Pistol`
   - Configure damage, fire rate, etc.
   - Drag to shop item's **Weapon Data** field

## Step 9.2: Create Shop Trigger

1. Create Empty child of shop → name `ShopTrigger`
2. Add Component → `Box Collider`
3. Check **Is Trigger**
4. Size it around shop entrance
5. Create new script or use:

```csharp
// Simple shop trigger
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        GetComponentInParent<ShopSystem>().OpenShop();
    }
}
```

---

# PART 10: SETTING UP PET CAPTURE

## Step 10.1: Configure Pet Capture System

1. Find `PetCaptureSystem` in hierarchy (create if missing)
2. Assign these fields:
   - **Net Prefab**: Create a simple net object (sphere/capsule with collider)
   - **Throw Force**: `20`
   - **Base Capture Chance**: `0.3`

## Step 10.2: Create Net Prefab

1. Right-click → **3D Object → Sphere**
2. Scale to `0.3, 0.3, 0.3`
3. Add Component → `Rigidbody`
4. Add Component → `Sphere Collider`
5. Drag to `Assets/Prefabs/` to create prefab
6. Delete from scene
7. Drag prefab to Pet Capture System's **Net Prefab** field

## Step 10.3: Create Wild Pet

1. Import a creature model
2. Add Component → `Wild Pet`
3. Add Component → `NavMesh Agent`
4. Create Pet Data:
   - **Create → Bob's Petroleum → Pet Data**
   - Set name, stats, rarity
5. Assign Pet Data to Wild Pet

---

# PART 11: SETTING UP DIALOGUE

## Step 11.1: Create Dialogue System

1. Find/create `DialogueSystem` object
2. Create UI panel for dialogue:
   - Under Canvas: **UI → Panel**
   - Name it `DialoguePanel`
   - Add Text elements for speaker name and dialogue
3. Drag references to Dialogue System

## Step 11.2: Create Dialogue Data

1. Right-click in Project → **Create → Bob's Petroleum → Dialogue Data**
2. Name it `NPC_Greeting`
3. Click on it, expand **Lines**
4. Add lines:
   - Speaker: `Bob`
   - Text: `Welcome to my gas station!`
   - (Add voice clip if you have one)

## Step 11.3: Setup NPC for Dialogue

1. Add Component → `NPC Dialogue` to NPC
2. Drag your dialogue data to **Dialogue** field
3. Set **Talking Animation**: name from your animation clips

---

# PART 12: MULTIPLAYER LOBBY

## Step 12.1: Create Lobby UI

1. Create a new Panel under Canvas → name `LobbyPanel`
2. Add Text for lobby code
3. Add Button for "Create Lobby"
4. Add Button for "Join Lobby"
5. Add Input Field for lobby code entry
6. Add "Ready" button
7. Add player list container

## Step 12.2: Wire Lobby System

1. Create `LobbySystem` object
2. Drag all UI references to the fields
3. Set up button onClick events:
   - Create Lobby button → `LobbySystem.CreateLobby()`
   - Ready button → `LobbySystem.ToggleReady()`

---

# PART 13: DAY/NIGHT CYCLE

## Step 13.1: Configure Sun

1. Find your Directional Light (or create one)
2. Select `DayNightCycle` object
3. Drag Directional Light to **Sun Light** field

## Step 13.2: Set Times

- **Day Duration**: `300` (5 minutes)
- **Night Duration**: `180` (3 minutes)
- **Sunrise Hour**: `6`
- **Sunset Hour**: `18`

## Step 13.3: Connect to Shop

Shop will auto-open at day and close at night if:
- `ShopManager` has **Auto Open At Day**: ✓
- `ShopManager` has **Auto Close At Night**: ✓

---

# PART 14: HORROR EVENTS

## Step 14.1: Configure

1. Select `HorrorEventsSystem`
2. Check **Events Enabled**
3. Check **Night Only** (horror at night)
4. Set intervals as desired

## Step 14.2: Add Audio

Add creepy sounds to arrays:
- **Creepy Sounds**: ambient horror
- **Whisper Sounds**: whisper audio
- **Jump Scare Stingers**: loud sudden sounds

## Step 14.3: Add Shadow Figure

1. Create a black humanoid shape (cube stretched, or dark model)
2. Make it a prefab
3. Drag to **Shadow Figure Prefab** field

---

# PART 15: BUILDING FOR WEBGL

## Step 15.1: Switch Platform

1. **File → Build Settings**
2. Select **WebGL**
3. Click **Switch Platform** (takes a few minutes)

## Step 15.2: Player Settings

1. Click **Player Settings...**
2. Under **Resolution and Presentation**:
   - **Default Canvas Width**: `1920`
   - **Default Canvas Height**: `1080`
3. Under **Publishing Settings**:
   - **Compression Format**: `Gzip`
   - **Memory Size**: `512`

## Step 15.3: Build

1. Click **Build**
2. Create a folder named `WebGL_Build`
3. Click **Select Folder**
4. Wait for build (5-15 minutes)

---

# PART 16: TESTING

## Step 16.1: Hit Play

1. Press the **Play** button in Unity
2. The GameBootstrapper auto-wires everything

## Step 16.2: Test Controls

- **WASD**: Move
- **Mouse**: Look
- **Space**: Jump
- **Shift**: Sprint
- **C**: Crouch
- **F**: Toggle Flashlight
- **M**: Toggle Minimap
- **O**: Toggle Shop Open/Closed
- **Tab**: Inventory
- **1-5**: Quick Slots
- **ESC**: Pause Menu
- **E**: Interact
- **~** (Tilde): Debug Console

## Step 16.3: Debug Console Commands

Press **~** to open debug console. Useful commands:
- `god` - Toggle invincibility
- `money 1000` - Add money
- `hamburger 10` - Add hamburgers
- `spawn zombie` - Spawn a zombie
- `spawn customer` - Spawn a customer
- `time day` - Set to daytime
- `time night` - Set to nighttime
- `heal` - Restore full health
- `fps` - Toggle FPS display
- `help` - List all commands

## Step 16.4: Validate Scene

1. **Window → Bob's Petroleum → MASTER SETUP** → **Validation** tab
2. Click "Validate All Systems"
3. Review any red items
4. Click "Auto-Fix Issues" to resolve problems automatically

---

# QUICK REFERENCE: Component Locations

| System | Script Location |
|--------|-----------------|
| Player Movement | `Scripts/Player/PlayerController.cs` |
| Health | `Scripts/Player/PlayerHealth.cs` |
| Inventory | `Scripts/Player/PlayerInventory.cs` |
| Flashlight | `Scripts/Player/Flashlight.cs` |
| Skins | `Scripts/Player/SkinManager.cs` |
| Zombies | `Scripts/NPC/ZombieAI.cs` |
| Customers | `Scripts/AI/CustomerAI.cs` |
| Customer Spawner | `Scripts/AI/CustomerSpawner.cs` |
| Day/Night | `Scripts/Systems/DayNightCycle.cs` |
| Horror | `Scripts/Systems/HorrorEventsSystem.cs` |
| Quests | `Scripts/Systems/QuestSystem.cs` |
| Dialogue | `Scripts/Systems/DialogueSystem.cs` |
| Shop | `Scripts/Economy/ShopSystem.cs` |
| Shop Open/Close | `Scripts/Economy/ShopManager.cs` |
| Gas Pumps | `Scripts/Economy/GasPump.cs` |
| Guns | `Scripts/Combat/SimpleGunSystem.cs` |
| Pet Capture | `Scripts/Battle/PetCaptureSystem.cs` |
| Pet Battles | `Scripts/Battle/BattleSystem.cs` |
| Battle Camera | `Scripts/Battle/BattleCameraSystem.cs` |
| Lobby | `Scripts/Multiplayer/LobbySystem.cs` |
| Animations | `Scripts/Animation/SimpleAnimationPlayer.cs` |
| HUD | `Scripts/UI/HUDManager.cs` |
| Pause Menu | `Scripts/UI/PauseMenu.cs` |
| Minimap | `Scripts/UI/MinimapSystem.cs` |
| Setup Tools | `Scripts/Editor/BobsPetroleumSetupWizard.cs` |
| Quick Setup | `Scripts/Utilities/QuickSetup.cs` |
| Validation | `Scripts/Utilities/SceneValidator.cs` |

---

# TROUBLESHOOTING

## "Script Missing"
- Make sure scripts compiled (no errors in Console)
- Check namespace: `BobsPetroleum.____`

## "Null Reference"
- Run Scene Validator: **Window → Bob's Petroleum → Validate Scene**
- Auto-fix will add missing components

## "NavMesh not found"
- Bake NavMesh: **Window → AI → Navigation → Bake**

## "Animations not playing"
- Check Animator has controller assigned
- Check clip names match in SimpleAnimationPlayer

## "UI not showing"
- Check Canvas is in scene
- Check EventSystem exists
- Check panels are active

---

**You're all set! Go make an awesome game!** 🎮

