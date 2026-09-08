# Love Game 🌴💞

**Private premium 3D open-world couple game for Android** — built with **Unity 6 LTS (6000.0.83f1) + URP**, streaming a **24-region world** (50+ ready), with couple interactions, vehicles, activities, home customization, photography and a full GameCI Android APK pipeline.

> Repo: `Itz-Subhu-Jaat/Love-game` (PRIVATE) · Language: English code + docs, Hinglish quick-start below.

---

## 🚀 Quick Start (Hinglish me, step by step)

### 1. Project kholna (sirf ek baar karna hai)
1. **Unity Hub** install karo → [unity.com/download](https://unity.com/download)
2. Hub me **Unity 6000.0.83f1** install karo (Installs → Install Editor → Archive/official link se exact version).
3. Ye repo download/clone karo, phir Unity Hub me **Add → project folder select karo** (`Love-game` folder jisme `Assets`, `ProjectSettings`, `Packages` ho).
4. Editor pehli baar kholega: sab scripts import honge aur **auto setup wizard** chalega (URP assets, quality levels, materials ban jayenge — `Assets/Settings/`, `Assets/Resources/Materials/` me).
5. `Assets/_Scenes/02_World.unity` kholo aur **Play** dabao — world generate hoga, explore karo!

### 2. Android APK banane ke liye (GitHub Actions se — bina PC pe build kiye)
1. Unity Hub me license activate karo (free Personal): Hub → Preferences → Licenses → Add → Sign in.
2. License file (.ulf) nikalo:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `~/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**:
   - `UNITY_LICENSE` = .ulf file ka **pura content** paste karo
   - `UNITY_EMAIL` = tumhara Unity account email
   - `UNITY_PASSWORD` = Unity account password
4. Repo → **Actions** tab → **Build Android APK** → **Run workflow** dabao.
5. ~30–60 min me run complete → artifacts me **LoveGame-APK** milega → download → phone me install (Unknown sources allow karke).

Bina license ke bhi **Validate Project** workflow har push pe chalta hai (lint + tests) — license sirf Unity builds ke liye chahiye. Detail: [Documentation/BUILD_GUIDE.md](Documentation/BUILD_GUIDE.md)

---

## 📁 Files kahan hain / kahan dalne hain (Unity structure)

```
Love-game/
├── Assets/                    ← SAB game content yahan
│   ├── _Game/                 ← C# code (12 assemblies)
│   │   ├── Core/              bootstrap, services, save, events, materials
│   │   ├── World/             regions, streaming, day/night, weather, NPC, wildlife
│   │   ├── Player/            controller, camera, character rig, partner AI
│   │   ├── Interaction/       interactables, couple interactions, emotes
│   │   ├── Vehicles/          car/bike/boat/hover framework
│   │   ├── Activities/        fishing, racing, photo, treasure, picnic...
│   │   ├── Home/              beach house + furniture system
│   │   ├── Inventory/         items, gifts, collectibles, memories
│   │   ├── Audio/             synthesized music/ambience (100% original)
│   │   ├── Content/           download manager + Addressables bridge
│   │   ├── UI/                HUD, map, menus, photo mode, debug (code-built uGUI)
│   │   ├── Root/              WorldSystems.cs + MainMenuBootstrap (scene boots)
│   │   ├── EditorTools/       setup wizard, CI builder, region generators
│   │   └── Tests/             EditMode + PlayMode test suites
│   ├── _Scenes/               00_Bootstrap, 01_MainMenu, 02_World
│   ├── Resources/
│   │   ├── Data/              regions.json (24 regions), items, furniture, manifest, tips
│   │   ├── Sprites/           UI sprites (runtime-tinted)
│   │   ├── Shaders/           skybox, water, terrain, emissive shaders
│   │   └── Materials/         (auto-generated on first open / CI)
├── Packages/manifest.json     URP 17.0.3 + Addressables + uGUI + TestFramework
├── ProjectSettings/           Unity 6000.0.83f1, quality levels, layers
├── Documentation/             status, architecture, build, content, testing guides
├── Tools/validation/          zero-license static validator (runs in CI)
└── .github/workflows/         validate.yml, android.yml, release.yml, addressables.yml
```

**Naye assets (art/models/music) dalne ka tarika:** `Assets/` ke andar koi bhi folder banao (e.g. `Assets/Art/Models/`) — Unity import karega. Rules: scale `1 unit = 1 meter`, forward `+Z`, pivot neeche-center. Poora guide: [Documentation/ASSET_IMPORT_GUIDE.md](Documentation/ASSET_IMPORT_GUIDE.md). **Kabhi bhi** `Library/`, `Temp/`, `build/` folders commit mat karo (`.gitignore` already handle karta hai).

---

## 🌍 World (24 regions, 50+ ready)

Azure Haven (home) · Sunset Bay · Dreamfall Isles · Neon Abyss City · Nova Coast · Wildheart Highlands · Moonlight Forest · Crystal Caverns · Skyline Metropolis · Coral Reef Archipelago · Frostpeak · Golden Desert · Verdant Valley · Starlight Island · Sunken City · Technova District · Rainbow Meadows · Lost Kingdom · Ocean Drive · Lumen City · Willow Lake · Volcanic Isle · Cloud Gardens · Old Harbor

Har region me: terrain + beaches/mountains, roads, POI beacons (fishing/picnic/stargaze/shops...), fast travel, NPCs, wildlife, vehicles, weather profile, collectibles. **Data-driven** — `Assets/Resources/Data/regions.json` edit karke naya region add karo, code change zero.

## 💑 Couple systems
Hug · kiss · high-five · hold hands · dance · sit together · wave · celebrate · watch scenery · picnic · stargazing — proximity + alignment + synchronized poses + cinematic camera + memory journal + relationship levels. Partner AI saath chalta hai; multiplayer-ready architecture (§ later phase).

## 🚗 Vehicles · 🎣 Activities
Car / motorcycle / boat / hover — enter/exit, driver+passenger, buoyancy boats. Fishing, racing (checkpoints+best time), treasure hunt, rune puzzles, picnic, camping (time skip), stargazing (night-gated), heart targets, photo mode (poses + PNG capture).

## 🏠 Home & Progression
Beach house interior, 24-piece furniture catalog (place/rotate/snap/save), inventory (40+ items), gifts → memories → bond levels, collectibles with region %.

## 🎨 Graphics (URP)
Gradient skybox + sun/moon/stars (custom shader), stylized animated water, vertex-colored biome terrain, emissive crystals/neon (bloom-ready), day/night cycle, 5 weather states with rain/snow particles, 4 quality levels (Low→Ultra) + FPS target setting. Placeholder visuals are procedural & **100% replaceable** without touching gameplay code.

## 🔊 Audio
Fully synthesized (original — zero licensing risk): romantic chord pad, ocean/wind/forest/city/cave ambience, rain, thunder, UI chimes, engine loop. Swap with licensed assets later behind the same `AudioService`.

## ✅ Quality & CI
- **Every push**: `Validate Project` workflow — meta/GUID pairing, scene refs, asmdef graph, C#/shader syntax, JSON catalogs, secret scan (no Unity license needed, ~1 min)
- **EditMode + PlayMode tests**: save roundtrip/migration, determinism, region validation, controller physics, camera, vehicles, couple poses
- **APK builds**: manual dispatch or version tags (IL2CPP/ARM64), 5-day artifact retention (free-tier storage friendly)

## 📄 License
MIT (code). All generated assets original. No third-party copyrighted content.
