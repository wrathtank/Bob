# Bob's Petroleum - Project Documentation

## Game Overview

**Bob's Petroleum** is a 1-4 player co-op WebGL horror gas station simulator.

**Core Premise:** Players are clones of Bob, spawned from tubes. The original Bob is injured/dying. Players must run his gas station, earn money, buy hamburgers, and feed Bob to revive him.

**Inspirations:** Lego Island + Goat Simulator + Schedule 1

## Game Modes

1. **Forever Mode** - Persistent world, cloud saves, endless play
2. **7 Night Runs** - Survival runs with leaderboard scoring (roguelike-style)

## Core Game Loop

```
Clone Spawn (tubes) → Bob explains mission → Run gas station → Earn money
    → Buy hamburgers → Feed Bob → Bob revived = WIN
```

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/                 # Core game systems
│   ├── Player/               # Player controllers, inventory
│   ├── Economy/              # Shops, cash register, money
│   ├── Combat/               # Weapons, gun systems
│   ├── Battle/               # Pet battles, capture system
│   ├── Items/                # Consumables, crafting
│   ├── Systems/              # Dialogue, fast travel, horror
│   ├── UI/                   # HUD, menus
│   ├── Networking/           # Multiplayer, cloud saves
│   ├── Environment/          # Water, world elements
│   ├── Animation/            # Simple animation system
│   ├── Utilities/            # Helpers, setup tools
│   └── Editor/               # Unity editor tools
├── Shaders/                  # Custom shaders (water, etc)
└── Prefabs/                  # Ready-to-use prefabs
```

## Key Systems

### Core Systems

| System | File | Purpose |
|--------|------|---------|
| GameManager | `Core/GameManager.cs` | Central game state, day/night, scoring |
| GameFlowController | `Core/GameFlowController.cs` | **THE GLUE** - connects all systems |
| BobCharacter | `Core/BobCharacter.cs` | The dying owner, hamburger feeding, revival |
| CloneSpawnSystem | `Core/CloneSpawnSystem.cs` | Spawns players from tubes, intro sequence |
| GameBootstrapper | `Core/GameBootstrapper.cs` | Auto-creates managers on Play |

### Player Systems

| System | File | Purpose |
|--------|------|---------|
| PlayerController | `Player/PlayerController.cs` | FPS movement, input |
| PlayerInventory | `Player/PlayerInventory.cs` | Money, items, hamburgers |
| DeathRespawnSystem | `Player/DeathRespawnSystem.cs` | Death, respawn, home spots |

### Economy Systems

| System | File | Purpose |
|--------|------|---------|
| ShopManager | `Economy/ShopManager.cs` | Open/close shop, NPC behavior |
| ShopSystem | `Economy/ShopSystem.cs` | Buy weapons/pets/upgrades |
| CashRegister | `Economy/CashRegister.cs` | Customer transactions, change |

### Combat Systems

| System | File | Purpose |
|--------|------|---------|
| SimpleGunSystem | `Combat/SimpleGunSystem.cs` | Point-and-shoot weapons |
| WeaponVisuals | `Combat/WeaponVisuals.cs` | FPS reload, flamethrower particles |

### Battle/Pet Systems

| System | File | Purpose |
|--------|------|---------|
| PetCaptureSystem | `Battle/PetCaptureSystem.cs` | Net throwing, capture mechanics |
| PetAnimationController | `Battle/PetAnimationController.cs` | Quadruped animations |
| BattleCameraSystem | `Battle/BattleCameraSystem.cs` | Pokemon-style battle camera |

### Item Systems

| System | File | Purpose |
|--------|------|---------|
| ConsumableSystem | `Items/ConsumableSystem.cs` | Inventory with thumbnails |
| CigarCraftingSystem | `Items/CigarCraftingSystem.cs` | Lab table, recipes, powers |

### World Systems

| System | File | Purpose |
|--------|------|---------|
| FastTravelSystem | `Systems/FastTravelSystem.cs` | Subway stations, pipe unlock |
| DialogueSystem | `Systems/DialogueSystem.cs` | Auto camera, typing subtitles |
| HorrorEventsSystem | `Systems/HorrorEventsSystem.cs` | Spooky random events |
| WaterSystem | `Environment/WaterSystem.cs` | Wavy water for planes |

### Networking Systems

| System | File | Purpose |
|--------|------|---------|
| NetworkGameManager | `Networking/NetworkGameManager.cs` | Client-hosted multiplayer |
| NetworkedPlayer | `Networking/NetworkedPlayer.cs` | Player sync component |
| NetworkedObject | `Networking/NetworkedObject.cs` | Object sync (pickups, doors) |
| SupabaseSaveSystem | `Networking/SupabaseSaveSystem.cs` | Cloud saves, leaderboards |

### UI Systems

| System | File | Purpose |
|--------|------|---------|
| HUDManager | `UI/HUDManager.cs` | Health, money, notifications |

### Animation

| System | File | Purpose |
|--------|------|---------|
| SimpleAnimationPlayer | `Animation/SimpleAnimationPlayer.cs` | Play clips without state machines |

### Editor Tools

| Tool | File | Purpose |
|------|------|---------|
| BobsPetroleumSetupWizard | `Editor/BobsPetroleumSetupWizard.cs` | One-click scene setup |
| GameSetupChecklist | `Editor/GameSetupChecklist.cs` | Visual progress tracker |

## Quick Setup

1. **Open Unity** (2022.3+ recommended)
2. **Window > Bob's Petroleum > Setup Wizard**
3. **Click "Setup Everything"**
4. **Done!** All managers auto-created.

Or manually:
1. Create empty GameObject named "GameBootstrapper"
2. Add `GameBootstrapper` component
3. Press Play - all managers created automatically

## Multiplayer Setup

### Install Netcode
1. Window > Package Manager
2. Search "Netcode for GameObjects"
3. Install

### Configure
1. Add `NetworkGameManager` to scene
2. Assign player prefab (must have `NetworkObject`)
3. Host calls `HostGame()`, clients call `JoinGame(ip)`

### Supabase Cloud Saves
1. Create free project at supabase.com
2. Run the SQL from `SupabaseSaveSystem.cs` comments
3. Add `SupabaseSaveSystem` to scene
4. Paste URL and anon key

## Key Prefab Requirements

### Player Prefab
- `NetworkObject` (for multiplayer)
- `NetworkedPlayer`
- `PlayerController`
- `PlayerInventory`
- `DeathRespawnSystem`
- `CharacterController`
- Camera (child)

### Bob Prefab
- `BobCharacter`
- Animator or `SimpleAnimationPlayer`
- AudioSource
- Health bar UI (world space canvas)

### Spawn Tube Prefab
- Door with Animator (Open/Close triggers)
- SpawnPoint transform (child)

### Home Spot Prefab
- `HomeSpot` component
- Visual mesh (bed, couch, etc)
- SpawnPoint transform

## Animation System

Uses `SimpleAnimationPlayer` - no complex state machines!

```csharp
// Just call Play with animation name
simpleAnimator.Play("Walk");
simpleAnimator.Play("Attack");
```

For pets, use `PetAnimationController` which has slots for all quadruped anims.

## Code Patterns

### Singleton Pattern
Most managers use singleton with auto-find:
```csharp
public static MyManager Instance { get; private set; }

void Awake() {
    if (Instance == null) Instance = this;
}
```

### Auto-Find References
Systems auto-find dependencies:
```csharp
void Start() {
    bob = bob ?? BobCharacter.Instance ?? FindObjectOfType<BobCharacter>();
}
```

### ScriptableObjects for Data
Use ScriptableObjects for items, weapons, pets:
```csharp
[CreateAssetMenu(fileName = "NewConsumable", menuName = "Bob's Petroleum/Consumable Data")]
public class ConsumableData : ScriptableObject { ... }
```

## Important Files

- `SETUP_GUIDE.md` - Detailed setup instructions
- `CLAUDE.md` - This file (project overview)
- `Assets/Scripts/Editor/GameSetupChecklist.cs` - Visual setup tracker

## Namespaces

All code is in `BobsPetroleum.*` namespace:
- `BobsPetroleum.Core`
- `BobsPetroleum.Player`
- `BobsPetroleum.Economy`
- `BobsPetroleum.Combat`
- `BobsPetroleum.Battle`
- `BobsPetroleum.Items`
- `BobsPetroleum.Systems`
- `BobsPetroleum.UI`
- `BobsPetroleum.Networking`
- `BobsPetroleum.Environment`
- `BobsPetroleum.Animation`

## Common Tasks

### Add New Weapon
1. Create WeaponData ScriptableObject
2. Assign to SimpleGunSystem or ShopSystem
3. Set up WeaponVisuals for FPS view

### Add New Pet
1. Create pet prefab with `PetAnimationController`
2. Create PetData ScriptableObject
3. Add to PetCaptureSystem's capturablePets

### Add New Consumable
1. Create ConsumableData ScriptableObject
2. Assign icon (thumbnail for inventory)
3. Define effects (heal, speed, etc)
4. Add to shop

### Add Home Spot
1. Create GameObject with `HomeSpot` component
2. Position spawn point child
3. Add visual (bed, sleeping bag, etc)

### Add Fast Travel Station
1. Create `SubwayStation` component
2. Assign to FastTravelSystem's stations list
3. Add attendant NPC (optional)

## WebGL Compatibility

All systems designed for WebGL:
- No threading (single-threaded)
- No File.Write (use PlayerPrefs or Supabase)
- No DLL plugins (pure C#)
- Compressed textures recommended
- Keep draw calls low
