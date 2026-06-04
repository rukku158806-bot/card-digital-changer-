# HexBattle — Unity 6 Setup Guide

## 1. Copy Scripts into the Project

1. Open your Unity 6 project (2D template, URP recommended for orb glow).
2. In the **Project** window, create the folder path:
   `Assets/HexBattle/Scripts/`
3. Copy every `.cs` file from `unity/Scripts/` into that folder.
4. Unity will auto-compile. Resolve any package errors first (see §7).

---

## 2. Required Prefabs

Create each prefab in `Assets/HexBattle/Prefabs/`.

### 2a. Tile Prefabs (×3)

| Prefab Name      | Required Components                     | Notes                                      |
|------------------|-----------------------------------------|--------------------------------------------|
| `NeutralTile`    | `SpriteRenderer`, `HexCell`             | Grey hex sprite; middle column             |
| `PlayerTile`     | `SpriteRenderer`, `HexCell`             | Blue-tinted hex sprite; columns 0–1        |
| `EnemyTile`      | `SpriteRenderer`, `HexCell`             | Red-tinted hex sprite; columns 3–4         |

Each tile needs a hex-shaped sprite. Use Unity's default Hexagon sprite or import your own.

### 2b. Player Prefab

| Prefab Name | Required Components                               |
|-------------|---------------------------------------------------|
| `Player`    | `SpriteRenderer` (glowing blue cube sprite)       |
|             | `PlayerController`                                |
|             | `OrbSystem`                                       |

- Sorting Order: 2
- Tag: `Player`

### 2c. Enemy Prefabs (×5)

| Prefab Name   | Tag        | EnemyType enum | Suggested sprite shape |
|---------------|------------|----------------|------------------------|
| `GruntEnemy`  | `Triangle` | Grunt          | Triangle               |
| `SupportEnemy`| `Circle`   | Support        | Circle                 |
| `CrystalEnemy`| `Diamond`  | Crystal        | Diamond                |
| `GolemEnemy`  | `Pentagon` | Golem          | Pentagon               |
| `VoidEnemy`   | `Star`     | Void           | Star                   |

Each enemy prefab must have:
- `SpriteRenderer`
- `EnemyUnit` component (set **Enemy Type**, **Max Hp**, **Damage** in Inspector)

The world-space HP bar is built automatically at runtime; no additional setup needed.

### 2d. DamagePopup Prefab

| Prefab Name    | Required Components              |
|----------------|----------------------------------|
| `DamagePopup`  | `DamagePopup` script             |
|                | `Canvas` (World Space, Order 20) |
|                | Child: `TextMeshProUGUI`         |

- Assign the `TextMeshProUGUI` child to the `label` field in the Inspector.
- Alternatively, leave `label` null — the script will build it at runtime.

### 2e. Heart Icon Prefab

| Prefab Name | Required Components  |
|-------------|----------------------|
| `HeartIcon` | `Image` (heart sprite)|

Used by `UIManager` to rebuild the lives panel.

---

## 3. Scene Setup

### 3a. Create GameObjects in the Scene Hierarchy

```
Scene
├── HexGrid          ← Empty GameObject
├── Player           ← Instantiate Player prefab
├── BattleManager    ← Empty GameObject
├── UIManager        ← Empty GameObject
└── UI Canvas        ← Screen Space - Overlay Canvas
    ├── TopBar
    │   ├── HPBar         (Slider, green fill)
    │   ├── EnergyBar     (Slider, blue fill)
    │   ├── LivesPanel    (Horizontal Layout Group)
    │   └── ScoreText     (TextMeshProUGUI)
    ├── WaveLabel         (TextMeshProUGUI — top centre)
    ├── WaveAnnouncement  (TextMeshProUGUI — centre screen, start disabled)
    ├── LeftSidebar
    │   ├── AttackBtn     (Button + TextMeshProUGUI "Attack")
    │   ├── CannonBtn     (Button + TextMeshProUGUI "Cannon")
    │   ├── ComboBtn      (Button + TextMeshProUGUI "Combo")
    │   └── OKBtn         (Button + TextMeshProUGUI "OK")
    ├── ComboChartPanel   (Image panel, start disabled)
    └── GameOverPanel     (Image panel, start disabled)
        └── GameOverScoreText (TextMeshProUGUI)
```

### 3b. Attach Components

| GameObject    | Component(s) to Attach          |
|---------------|----------------------------------|
| HexGrid       | `HexGrid`                        |
| Player        | `PlayerController`, `OrbSystem`  |
| BattleManager | `BattleManager`                  |
| UIManager     | `UIManager`                      |

---

## 4. Inspector Reference Wiring

### HexGrid
| Field               | Assign                          |
|---------------------|---------------------------------|
| Neutral Tile Prefab | `NeutralTile` prefab            |
| Player Tile Prefab  | `PlayerTile` prefab             |
| Enemy Tile Prefab   | `EnemyTile` prefab              |
| Cols                | 5                               |
| Rows                | 4                               |
| Hex Width           | 1.1                             |
| Hex Height          | 1.0                             |

### PlayerController
| Field     | Assign                          |
|-----------|---------------------------------|
| Hex Grid  | drag `HexGrid` GameObject       |

### OrbSystem
| Field       | Assign (optional)               |
|-------------|---------------------------------|
| Orb Prefab  | Leave null for procedural orbs  |
| Orbit Radius| 1.5                             |
| Orbit Speed | 90                              |

### BattleManager
| Field           | Assign                          |
|-----------------|---------------------------------|
| Hex Grid        | `HexGrid` GameObject            |
| Player          | `Player` GameObject             |
| Ui Manager      | `UIManager` GameObject          |
| Grunt Prefab    | `GruntEnemy` prefab             |
| Support Prefab  | `SupportEnemy` prefab           |
| Crystal Prefab  | `CrystalEnemy` prefab           |
| Golem Prefab    | `GolemEnemy` prefab             |
| Void Prefab     | `VoidEnemy` prefab              |

### UIManager
| Field                   | Assign                                      |
|-------------------------|---------------------------------------------|
| HP Bar                  | `HPBar` Slider in Canvas                    |
| Energy Bar              | `EnergyBar` Slider in Canvas                |
| Lives Panel             | `LivesPanel` Transform                      |
| Score Text              | `ScoreText` TMP object                      |
| Wave Label              | `WaveLabel` TMP object                      |
| Attack Btn              | `AttackBtn` Button                          |
| Cannon Btn              | `CannonBtn` Button                          |
| Combo Btn               | `ComboBtn` Button                           |
| OK Btn                  | `OKBtn` Button                              |
| Combo Chart Panel       | `ComboChartPanel` GameObject                |
| Game Over Panel         | `GameOverPanel` GameObject                  |
| Game Over Score Text    | `GameOverScoreText` TMP inside panel        |
| Wave Announcement Text  | `WaveAnnouncement` TMP object               |
| Damage Popup Prefab     | `DamagePopup` prefab                        |
| Heart Icon Prefab       | `HeartIcon` prefab                          |

---

## 5. Layer & Tag Setup

### Tags  *(Edit → Project Settings → Tags and Layers)*

Add these custom tags:

| Tag       | Used by         |
|-----------|-----------------|
| `Player`  | Player prefab   |
| `Triangle`| GruntEnemy      |
| `Circle`  | SupportEnemy    |
| `Diamond` | CrystalEnemy    |
| `Pentagon`| GolemEnemy      |
| `Star`    | VoidEnemy       |

### Layers

No custom layers are strictly required. If you want separate camera culling for the UI vs. world, add:

| Layer Name  | Usage               |
|-------------|---------------------|
| `HexGrid`   | Tile GameObjects    |
| `Units`     | Player + Enemies    |
| `HexUI`     | World-space HP bars |

Set the `Culling Mask` on the Main Camera to include all layers you use.

---

## 6. Camera Setup

- Camera: **Orthographic**, Size ≈ 3–4 (adjust to frame the 5×4 grid).
- Position the camera so the grid is centred in the view.
- Suggested position: X = 2.75, Y = 1.5, Z = -10 (tweak as needed).

---

## 7. Package Dependencies

The scripts depend on:

| Package                           | Why                             |
|-----------------------------------|---------------------------------|
| **TextMeshPro**                   | `TMP_Text` / `TextMeshProUGUI`  |
| **Universal Render Pipeline (URP)**| `Light2D` for orb glow          |

Import both via *Window → Package Manager*.

If URP is not used, the `Light2D` addition in `OrbSystem.CreateOrbGO()` is wrapped in a
`try/catch` and will silently skip — orbs will still appear without the glow effect.

---

## 8. Combo Reference Chart

To populate the `ComboChartPanel`, add a `TextMeshProUGUI` child with this text:

```
ELEMENTAL COMBOS
Dark + Void        → Shadow Collapse  ×3.20
Crystal + Light    → Prismatic Burst  ×2.75
Nature + Fire      → Wildfire Bloom   ×2.25
Water + Lightning  → Storm Surge      ×2.15
Nature + Earth     → Terra Growth     ×2.10
(other pairs)      → Basic Attack     ×1.00

Base damage: 20   Cannon: 10
```

---

## 9. Play Mode Quick Test

1. Press **Play**.
2. The 5×4 grid should appear with blue (player zone) and red (enemy zone) tiles.
3. Three enemies should spawn on the enemy side.
4. Click an adjacent blue tile to move the player.
5. Press **Attack** → combo fires → damage popup appears.
6. Press **OK** → enemies move and attack.
7. Defeat all enemies → Wave 2 announcement → harder wave spawns.
