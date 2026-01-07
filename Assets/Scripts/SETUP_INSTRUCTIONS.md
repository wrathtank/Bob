# Bob's Petroleum - Complete Setup Guide

This document provides detailed instructions for setting up all systems in Bob's Petroleum.

---

## Table of Contents

1. [Required Packages](#required-packages)
2. [Project Setup](#project-setup)
3. [Scene Setup](#scene-setup)
4. [Player Setup](#player-setup)
5. [NPC Setup](#npc-setup)
6. [Gas Station Setup](#gas-station-setup)
7. [Day/Night Cycle](#daynight-cycle)
8. [Battle System](#battle-system)
9. [Vehicle System](#vehicle-system)
10. [Web3/Wallet Integration](#web3wallet-integration)
11. [UI Setup](#ui-setup)
12. [Animation System](#animation-system)
13. [Inspector Reference Guide](#inspector-reference-guide)

---

## Required Packages

Install these via Package Manager (Window > Package Manager):

1. **Unity Netcode for GameObjects** - For multiplayer
2. **TextMeshPro** - For UI text (usually auto-installed)
3. **Input System** (Optional) - For new input system

For WebGL builds, also consider:
- **WebGL Publisher** for easy deployment

---

## Project Setup

### 1. Create Core Manager GameObject

1. Create empty GameObject: `GameManagers`
2. Add these components:
   - `GameManager`
   - `SaveSystem`
   - `BobsNetworkManager`
   - `DayNightCycle`
   - `CigarSystem`
   - `GarbageSystem`
   - `AttackDatabase`
   - `BattleManager`
   - `ObjectPool` (optional, for performance)

### 2. Configure GameManager

```
Inspector Settings:
├── Game Settings
│   ├── Total Days: 7
│   └── Hamburgers To Revive Bob: 10
├── Spawn Settings
│   ├── Player Spawn Point: [Assign Transform]
│   ├── Starting Health: 100
│   └── Starting Money: 0
├── End Game Objects
│   ├── Game Over Object: [Your game over screen]
│   └── Victory Object: [Your victory screen]
└── Pre-Run Rewards
    └── [Configure reward items]
```

### 3. Setup Network Manager

1. Add Unity's `NetworkManager` component (from Netcode)
2. Add `BobsNetworkManager` component
3. Configure:
   - Max Players: 4
   - Set up player prefab (see Player Setup)

---

## Scene Setup

### Recommended Hierarchy

```
Scene
├── GameManagers (persistent)
├── Environment
│   ├── Terrain
│   ├── Buildings
│   └── Props
├── GasStation
│   ├── Building
│   ├── Pumps
│   ├── Shelves
│   ├── CashRegister
│   ├── VendingMachine
│   └── BobsBed
├── Spawners
│   ├── ZombieSpawner
│   ├── CustomerSpawner
│   ├── NPCCarSpawner
│   └── GarbageSpawnPoints
├── NPCs
│   ├── Vendors
│   ├── Battlers
│   └── BobNPC
├── Vehicles
├── UI
│   ├── FadeCanvas
│   ├── PhoneUI
│   └── MainMenuCanvas
└── Cameras
    └── IntroCamera
```

---

## Player Setup

### 1. Create Player Prefab

1. Create player model GameObject
2. Add components:
   - `CharacterController`
   - `PlayerController`
   - `PlayerHealth`
   - `PlayerInventory`
   - `PlayerCombat`
   - `PlayerNetworkSync`
   - `NetworkObject` (for Netcode)
   - `AnimationEventHandler`

### 2. Configure PlayerController

```
Inspector Settings:
├── Movement Settings
│   ├── Walk Speed: 5
│   ├── Run Speed: 8
│   ├── Jump Force: 5
│   └── Gravity Multiplier: 2
├── Mouse Look
│   ├── Mouse Sensitivity: 2
│   ├── Min Look Angle: -90
│   └── Max Look Angle: 90
├── Camera
│   └── Player Camera: [Child camera]
├── Interaction
│   ├── Interaction Range: 3
│   ├── Interact Key: E
│   └── Interaction Layer: [Set layer mask]
├── Fart Ability
│   ├── Fart Ability Enabled: ✓
│   ├── Fart Key: F
│   ├── Fart Force: 20
│   ├── Fart Upward Force: 5
│   ├── Fart Cooldown: 5
│   └── Ragdoll Duration: 2
└── Animation Events
    └── Animation Handler: [Self reference]
```

### 3. Setup Ragdoll (Optional)

1. Create ragdoll skeleton on player model
2. Add `RagdollController` component
3. Assign Hips Rigidbody
4. Set `Start Disabled: true`

### 4. Player Camera Setup

1. Create Camera as child of player
2. Position at eye level
3. Add `AudioListener`
4. Camera will auto-enable for local player only

---

## NPC Setup

### Customer NPC

1. Create NPC model
2. Add `NavMeshAgent`
3. Add `CustomerAI`
4. Add `NPCHealth`
5. Add `AnimationEventHandler`

```
CustomerAI Inspector:
├── Movement
│   ├── Wander Radius: 20
│   └── Wander Interval: 5
├── Shopping
│   ├── Shop Chance: 0.3
│   ├── Max Items To Buy: 3
│   └── Shopping Time: 2
└── Target Points (Set via CustomerSpawner)
```

### Zombie NPC

1. Create zombie model
2. Add `NavMeshAgent`
3. Add `ZombieAI`
4. Add `NPCHealth`
5. Add `AnimationEventHandler`

```
ZombieAI Inspector:
├── Detection
│   ├── Detection Range: 15
│   ├── Lose Interest Range: 25
│   └── Target Layers: Player, Customer
├── Movement
│   ├── Walk Speed: 2
│   └── Chase Speed: 4
├── Combat
│   ├── Attack Range: 2
│   ├── Attack Damage: 15
│   └── Attack Cooldown: 1.5
└── Audio
    ├── Groans: [Array of clips]
    └── Attack Sound: [Clip]
```

### Vendor NPC

1. Create vendor model
2. Add `VendorNPC`
3. Add `AnimationEventHandler`

```
VendorNPC Inspector:
├── Vendor Info
│   ├── Vendor Name: "Shop Keeper"
│   └── Vendor Type: General/Armory/CarDealer
├── Inventory
│   └── [Add VendorItems with prices]
└── UI
    └── Shop UI Prefab: [Assign UI prefab]
```

### Battler NPC

1. Create trainer model
2. Add `BattlerNPC`
3. Add `AnimationEventHandler`

```
BattlerNPC Inspector:
├── Battler Info
│   ├── Battler Name: "Wild Trainer"
│   ├── Pre Battle Dialogue: "Let's battle!"
│   └── Win/Lose Dialogue: [Set messages]
├── Animals
│   └── [Add BattlerAnimal entries with prefabs]
├── Rewards
│   ├── Money Reward: 50
│   └── Item Rewards: [Configure drops]
└── Battle Settings
    ├── Can Rebattle: false
    └── Rebattle Cooldown: 300
```

### Wandering Animals (Capturable)

1. Create animal model (rat, etc.)
2. Add `NavMeshAgent`
3. Add `WanderingAnimalAI`
4. Add `AnimationEventHandler`

```
WanderingAnimalAI Inspector:
├── Animal Info
│   ├── Animal Name: "Wild Rat"
│   └── Animal Type: "Rodent"
├── Stats
│   ├── Base Health: 30
│   └── Catch Difficulty: 0.5
├── Movement
│   ├── Walk Speed: 2
│   ├── Flee Speed: 5
│   └── Wander Radius: 10
└── Detection
    ├── Flee Distance: 5
    └── Safe Distance: 15
```

---

## Gas Station Setup

### 1. Main Building

Create your gas station building with:
- Interior with shelves
- Cash register area
- Vending machine location
- Bob's hospital bed area

### 2. Shop Shelves

For each shelf:
1. Create shelf object
2. Add `ShopShelf` component
3. Configure:

```
ShopShelf Inspector:
├── Item Settings
│   └── Shop Item
│       ├── Item ID: "chips"
│       ├── Item Name: "Chips"
│       └── Price: 5
├── Restock
│   ├── Auto Restock: ✓
│   ├── Current Stock: 10
│   └── Max Stock: 10
```

### 3. Cash Register

1. Create register object with TMP display
2. Add `CashRegisterUI` component
3. Configure display text reference

### 4. Vending Machine

1. Create vending machine object
2. Add `VendingMachine` component
3. Add hamburgers to the items list:

```
VendingMachine Inspector:
├── Items
│   └── [0] Hamburger
│       ├── Item ID: "hamburger"
│       ├── Item Name: "Old Hamburger"
│       ├── Price: 25
│       └── Is Consumable: ✓
└── UI
    └── Vending UI Prefab: [Assign]
```

### 5. Bob's Bed Area

1. Create hospital bed with Bob model
2. Add `BobRevivalSystem` component
3. Create `BobFeedingZone` trigger near bed

```
BobRevivalSystem Inspector:
├── Bob Setup
│   ├── Bob On Bed: [Sick Bob model]
│   └── Bob Revived: [Healthy Bob model - disabled]
├── Hamburger Requirements
│   ├── Hamburgers Needed: 10
│   └── Hamburgers Fed: 0 (runtime)
└── UI
    └── Progress Text: [TMP reference]
```

### 6. Gas Pumps

1. Create pump object
2. Add `GasStation` component
3. Create `GasPumpTrigger` for player interaction

```
GasStation Inspector:
├── Station Settings
│   ├── Fuel Price: 2
│   └── Fuel Rate: 10
├── Pump Positions
│   ├── Pump Position: [Transform]
│   └── Attendant Position: [Transform]
└── Queue
    └── Queue Positions: [Array of transforms]
```

---

## Day/Night Cycle

### Setup

1. Add `DayNightCycle` to GameManagers
2. Assign your directional light (sun)
3. Configure day duration

```
DayNightCycle Inspector:
├── Time Settings
│   ├── Day Duration Seconds: 600 (10 min)
│   ├── Day Time Ratio: 0.6
│   └── Current Time: 0.25 (start at morning)
├── Lighting
│   ├── Sun Light: [Directional Light]
│   ├── Day Color: White
│   ├── Sunset Color: Orange
│   ├── Night Color: Dark Blue
│   ├── Day Intensity: 1
│   └── Night Intensity: 0.1
└── Day Phases
    ├── Dawn Start: 0.2
    ├── Day Start: 0.3
    ├── Dusk Start: 0.7
    └── Night Start: 0.8
```

---

## Battle System

### 1. Setup Battle Manager

```
BattleManager Inspector:
├── Battle Settings
│   ├── Turn Delay: 1
│   └── Attack Animation Time: 1.5
├── Battle Arena
│   ├── Player Animal Position: [Transform]
│   ├── Enemy Animal Position: [Transform]
│   └── Battle Camera Position: [Transform]
└── UI
    └── Battle UI Prefab: [Assign]
```

### 2. Setup Attack Database

The `AttackDatabase` auto-populates with default attacks, or add your own:

```
AttackDatabase Inspector:
└── All Attacks
    └── [0] Attack
        ├── Attack Name: "Tackle"
        ├── Damage: 10
        ├── Accuracy: 0.95
        ├── Attack Type: Normal
        ├── Animation Trigger: "Attack"
        └── Effect Prefab: [Optional particle]
```

### 3. Create Battle Arena (Optional)

1. Create a separate area for battles
2. Set up player/enemy spawn positions
3. Add camera position for battle view

---

## Vehicle System

### 1. Create Car Prefab

1. Create car model
2. Add `Rigidbody` (mass ~1000)
3. Add `CarController`
4. Add `CarEntryTrigger` on doors

```
CarController Inspector:
├── Movement
│   ├── Max Speed: 30
│   ├── Acceleration: 10
│   ├── Brake Force: 15
│   └── Steering Speed: 100
├── Fuel
│   ├── Max Fuel: 100
│   └── Fuel Consumption: 1
├── Seats
│   ├── Driver Seat: [Transform]
│   └── Passenger Seats: [Array]
├── Entry/Exit
│   ├── Entry Triggers: [Door triggers]
│   └── Exit Point: [Transform]
└── Collision Damage
    ├── Collision Damage: 30
    └── Min Damage Speed: 5
```

### 2. Setup NPC Car Spawner

```
NPCCarSpawner Inspector:
├── Car Prefabs: [Array of NPC cars]
├── Spawn Settings
│   ├── Spawn Points: [Around map]
│   ├── Max Cars: 5
│   └── Spawn Interval: 30
└── Waypoints
    └── City Waypoints: [Path transforms]
```

### 3. Setup Race System (Optional)

1. Create race start zone with `RaceStartZone`
2. Add `RaceCheckpoint` at each checkpoint
3. Configure `RaceSystem`:

```
RaceSystem Inspector:
├── Race Settings
│   ├── Total Laps: 3
│   └── Countdown Duration: 3
├── Track
│   ├── Checkpoints: [Ordered transforms]
│   ├── Start Line: [Transform]
│   └── Starting Positions: [Grid positions]
└── Rewards
    ├── First Place: 500
    ├── Second Place: 200
    └── Third Place: 100
```

---

## Web3/Wallet Integration

### 1. Setup Wallet Connector

1. Create 3D object (plane/screen) for wallet UI
2. Add `WalletConnector` component
3. Add collider and set to Interactable layer

```
WalletConnector Inspector:
├── Connection Settings
│   ├── Chain ID: [Your L1 chain ID]
│   ├── RPC URL: [Your RPC endpoint]
│   └── Chain Name: [Your chain name]
├── NFT Contracts
│   ├── ERC1155 Contract: [Weapons/boosts]
│   └── ERC721 Contract: [1:1 skins]
```

### 2. Setup NFT Manager

```
NFTManager Inspector:
├── NFT Weapons
│   └── [Add NFTWeapon entries]
│       ├── Token ID: "1"
│       ├── Contract Address: "0x..."
│       ├── Weapon Name: "Legendary Bat"
│       ├── Weapon Prefab: [Assign]
│       └── Damage/Speed/Range: [Stats]
├── NFT Boosts
│   └── [Add NFTBoost entries]
│       ├── Token ID: "2"
│       ├── Boost Type: SpeedMultiplier
│       ├── Value: 1.5
│       └── Duration: 60
└── NFT Skins
    └── [Add NFTSkin entries]
        ├── Token ID: "1001"
        ├── Skin Name: "Gold Player"
        └── Player Model Prefab: [Assign]
```

### 3. WebGL JavaScript Setup

The `WalletConnectPlugin.jslib` is included. For production:
1. Replace mock implementations with real Web3 calls
2. Add your Infura/Alchemy API key
3. Configure WalletConnect project ID

---

## UI Setup

### 1. Fade Controller (Opening/Death Transitions)

1. Create Canvas (Screen Space - Overlay)
2. Create Image filling entire screen
3. Add `FadeController` component

```
FadeController Inspector:
├── Fade Settings
│   ├── Fade Duration: 1
│   ├── Start Faded In: ✓ (for opening)
│   └── Auto Fade Out On Start: ✓
├── Colors
│   ├── Fade In Color: Black (255,0,0,0 -> full)
│   └── Fade Out Color: Transparent
```

**Usage:**
- Opening: Starts black, fades to transparent
- Death: Call `FadeController.Instance.FadeIn()` then `FadeOut()` on respawn

### 2. Phone UI

1. Create phone model prefab
2. Add `PhoneUI` component
3. Create TMP texts for stats

```
PhoneUI Inspector:
├── Phone Display
│   ├── Phone Object: [Phone model]
│   └── Toggle Key: Tab
├── Stats Display
│   ├── Health Text: [TMP reference]
│   ├── Money Text: [TMP reference]
│   └── Day Text: [TMP reference]
├── Party Display
│   ├── Party List Container: [Transform]
│   └── Party Member Prefab: [UI prefab]
└── Inventory Display
    ├── Inventory Container: [Transform]
    └── Inventory Item Prefab: [UI prefab]
```

### 3. Main Menu UI

1. Create Menu Canvas
2. Add `MainMenuUI` component
3. Create panels for each menu state

```
MainMenuUI Inspector:
├── Panels
│   ├── Main Panel: [Play button panel]
│   ├── Lobby Options Panel: [Host/Join]
│   ├── Host Panel: [Public/Private options]
│   ├── Join Panel: [Code input]
│   ├── Lobby Panel: [Waiting room]
│   └── Inventory Panel: [Pre-run items]
├── Player Name
│   └── Player Name Input: [TMP_InputField]
├── Host Options
│   ├── Public Lobby Toggle: [Toggle]
│   └── Lobby Code Text: [TMP_Text]
```

### 4. Intro Video Controller

1. Create Camera for intro
2. Add `VideoPlayer` component
3. Add `IntroVideoController`

```
IntroVideoController Inspector:
├── Video Settings
│   ├── Intro Video: [VideoClip]
│   ├── Allow Skip: ✓
│   └── Skip Delay: 1
├── On Video End
│   ├── Objects To Disable: [Intro camera, etc.]
│   └── Objects To Enable: [Main menu, etc.]
```

---

## Animation System

The `AnimationEventHandler` lets you control animations from the inspector without setting up complex animator transitions.

### Setup

1. Add `AnimationEventHandler` to any animated object
2. Assign the Animator reference
3. Add animation mappings:

```
AnimationEventHandler Inspector:
├── Animator: [Animator component]
├── Animation Mappings
│   └── [0] Mapping
│       ├── Event Name: "Attack" (code name)
│       ├── Animation Name: "Melee_Attack_01" (animator name)
│       ├── Use Trigger: ✓
│       ├── Use Bool: ☐
│       └── Sound: [Optional clip]
│   └── [1] Mapping
│       ├── Event Name: "Walk"
│       ├── Animation Name: "Walking"
│       ├── Use Trigger: ☐
│       └── Use Bool: ✓ (for state animations)
└── Default Animations
    ├── Idle Animation: "Idle"
    └── Play Idle On Start: ✓
```

### Usage in Code

```csharp
// Trigger one-shot animation
animationHandler.TriggerAnimation("Attack");
animationHandler.TriggerAnimation("Jump");
animationHandler.TriggerAnimation("Hurt");

// Set state animation
animationHandler.SetAnimation("Walk", true);
animationHandler.SetAnimation("Walk", false);

// Direct play
animationHandler.PlayAnimation("StateName");
```

### Common Animation Names to Map

For Players:
- Idle, Walk, Run, Jump, Land
- Attack, Hurt, Death, Respawn
- Fart, GetUp (for ragdoll recovery)

For NPCs:
- Idle, Walk, Run
- Attack, Hurt, Death
- Greet (for vendors)

For Animals:
- Idle, Walk, Run
- Attack, Hurt, Faint, Captured

---

## Inspector Reference Guide

### Layer Setup

Create these layers:
- `Player` - Player characters
- `NPC` - All NPCs
- `Interactable` - Objects player can interact with
- `Vehicle` - Cars
- `Animal` - Capturable animals
- `Ground` - Terrain/floors

### Tag Setup

Create these tags:
- `Player`
- `Enemy`
- `Customer`
- `Vehicle`
- `Garbage`
- `Checkpoint`

### Spawner Setup Summary

| Spawner | Location | Key Settings |
|---------|----------|--------------|
| ZombieSpawner | GameManagers | Spawn points around map, max count, day scaling |
| CustomerSpawner | GameManagers | Store entrance, register, shelves |
| NPCCarSpawner | GameManagers | Road spawn points, waypoints |
| GarbageSystem | GameManagers | Garbage spawn points (trash locations) |

### Network Setup Checklist

1. [ ] Add `NetworkManager` from Netcode
2. [ ] Add `NetworkObject` to player prefab
3. [ ] Set player prefab in NetworkManager
4. [ ] Add `NetworkObject` to any synced objects
5. [ ] Test with ParrelSync or multiple builds

---

## Quick Start Checklist

1. [ ] Install required packages
2. [ ] Create GameManagers object with all manager components
3. [ ] Create and configure player prefab
4. [ ] Set up gas station interior (shelves, register, vending, Bob's bed)
5. [ ] Create zombie and customer prefabs
6. [ ] Set up spawn points around map
7. [ ] Configure day/night cycle
8. [ ] Create main menu UI
9. [ ] Create phone UI
10. [ ] Add fade controller canvas
11. [ ] Set up at least one vendor NPC
12. [ ] Set up at least one battler NPC with animals
13. [ ] Create car prefab (optional)
14. [ ] Configure wallet connector (for Web3)
15. [ ] Bake NavMesh for NPCs
16. [ ] Test in editor
17. [ ] Build for WebGL

---

## Troubleshooting

### NPCs Not Moving
- Check NavMesh is baked
- Verify NavMeshAgent is enabled
- Check spawn points are on NavMesh

### Interactions Not Working
- Check layer mask on PlayerController
- Verify objects have IInteractable component
- Check interaction range

### Network Issues
- Ensure NetworkManager is in scene
- Player prefab needs NetworkObject
- Check lobby code generation

### Web3 Not Connecting
- Check browser has wallet extension
- Verify chain ID matches
- Check console for JavaScript errors

### Animations Not Playing
- Verify Animator has the states
- Check trigger/bool names match exactly
- Ensure AnimationEventHandler has mappings

---

## Support

For questions about specific systems, refer to the XML comments in each script file. Each public method and inspector field is documented.

Good luck with Bob's Petroleum!
