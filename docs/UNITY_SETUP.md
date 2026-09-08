# Unity Setup Guide (LOVE GAME — Love Quest)

Everything about opening, running and building this project on your own PC.
Top section is a Hinglish quick-start, below that is the full English guide.

---

## 🇮🇳 Hinglish Quick-Start (1 minute me samjho)

**Ye repo kya hai?** Ek complete Unity 6 game project — "Love Quest".
Code, sprites, scenes, CI — sab kuch repo me ready hai. Unity ko kahi "install" karne ki zarurat nahi is repo ke liye — Unity tumhare **apne PC pe** chahiye sirf game ko khelne/test karne ke liye.

**Steps:**

1. **Unity Hub download karo** → https://unity.com/download (free hai).
2. Unity Hub kholo → **Installs** → **Install Editor** → **Unity 6 LTS (6000.0.x)** select karo. License wala step me **Personal** (free) choose karo — bina card details ke chal jata hai.
3. Ye repo apne PC pe **clone** karo (ya ZIP download karo):
   `git clone https://github.com/Itz-Subhu-Jaat/Love-game.git`
4. Unity Hub me **Add → Add project from disk** → `Love-game` folder select karo.
5. Pehli baar Unity project import karega (1–2 min, `Library/` folder banata hai jo git me nahi jata — ye normal hai).
6. **Play button** (▶) dabao → Main Menu aayega → **PLAY** → game chalu!

**Build kaise ho (apne PC pe):** `File → Build Profiles → WebGL → Build`.
**Ya bina Unity ke (cloud me):** GitHub Actions use karo → [CI_GUIDE.md](CI_GUIDE.md) dekho. Sirf 2 secrets add karne hain (Unity email + password) aur Actions tab se workflow run karo — build artifact download karke browser me khel sakte ho.

**Kya kaha dala (file map):**
- `Assets/_Scenes/` → game ke 2 scenes (00_MainMenu, 01_Game)
- `Assets/_Scripts/Core|Gameplay|UI|Audio/` → pura C# code (23 scripts)
- `Assets/Resources/Sprites/` → saari game images (hearts, cupid, arrows, backgrounds)
- `ProjectSettings/ProjectVersion.txt` → Unity version pinned (6000.0.83f1)
- `.github/workflows/build.yml` → cloud build (GameCI)

**Common problems:** Version mismatch warning aaye to Unity Hub se exact `6000.0.83f1` install karo (ya ProjectVersion.txt edit karke apna version likh do). Scene empty dikhe to tension mat lo — pehle Play dabao, sab kuch code se build hota hai (ye project ka design hai, bug nahi).

---

# Full English Guide

## 1. Installing Unity

| | |
|---|---|
| Editor | **Unity 6000.0.83f1** (pinned in `ProjectSettings/ProjectVersion.txt`) — any 6000.0.x LTS works, minor patch differences are auto-handled on open |
| Hub | [unity.com/download](https://unity.com/download) → install Unity Hub → sign in / create free account |
| License | **Personal** (free) — Hub will ask once; no credit card needed |
| Platforms | Everything in this repo builds for **WebGL** by default; add Windows/Android build support via Hub → Installs → *Add modules* if you want native builds |

## 2. Opening the project

1. Clone: `git clone https://github.com/Itz-Subhu-Jaat/Love-game.git`
2. Unity Hub → **Add → Add project from disk** → select the `Love-game` folder (the one containing `Assets/`).
3. First open takes 1–2 minutes: Unity imports sprites and compiles scripts into a local `Library/` folder. `Library/` is git-ignored — that's normal and expected.
4. Press **Play**.

### What you will see (and why it's fine)

- The **scenes look almost empty** — one camera and one `Boot` object. This is by design: `MainMenuBootstrap` / `GameBootstrap` construct the whole UI, world and audio at runtime via code (`UiFactory` + `Resources.Load`). Nothing serialized = nothing to break, no merge conflicts, identical behaviour in CI.
- The **Hierarchy** during Play fills up: `HUDCanvas`, `PopupCanvas`, `Player`, `BrokenHeart` objects, `MusicCore` (persistent) etc.
- All sprites live in `Assets/Resources/Sprites/` and are loaded by name at runtime.

## 3. Running

- **MainMenu** → logo, drifting hearts, PLAY / HOW TO PLAY, best score, sound toggle.
- **Game** → 10 waves of falling broken hearts, HUD (lives, wave, love meter, score, combo), pause menu (Esc), win/lose screens.

Controls: `A/D` or arrows to move, `Space` to fire (or mouse-drag + click, or touch).

## 4. Building locally

- **WebGL**: `File → Build Profiles → WebGL → Build` → pick an empty folder → open `index.html` in a browser.
- **Windows**: `File → Build Profiles → Windows → Build` (add the module in Hub first).
- Both scenes are already registered in `EditorBuildSettings.asset`, so the build menu needs no extra setup.

## 5. Project structure deep-dive

```
Assets/
  _Scenes/00_MainMenu.unity     Boot(MainMenuBootstrap) + Main Camera
  _Scenes/01_Game.unity         Boot(GameBootstrap)    + Main Camera
  _Scripts/Core/                GameConstants  - every tunable (speeds, waves, colors)
                                GameMath       - WaveFormulas + ScoreFormulas (pure logic)
                                GameEvents     - static event hub (decoupling)
                                GameManager    - run state machine (score/lives/waves/slowmo)
                                SaveSystem     - PlayerPrefs persistence
                                SceneFlow      - scene names + loaders
                                BootstrapBase  - camera + canvas guarantees
                                MainMenuBootstrap / GameBootstrap
  _Scripts/Gameplay/            PlayerController, ArrowProjectile, BrokenHeart,
                                HeartSpawner, Pickup, AutoMotion, GameObjectPool,
                                EffectBurst, FloatyText
  _Scripts/UI/                  UiFactory (code-built uGUI), HudController,
                                OverlayPanels (PauseMenu + EndScreen)
  _Scripts/Audio/               ProceduralAudio (synthesized clips), MusicPlayer
  Resources/Sprites/            11 PNGs, loaded via Resources.Load<Sprite>()
```

### Architecture notes

- **Event-driven**: gameplay raises `GameEvents` (ScoreChanged, WaveStarted, GameLost…), UI subscribes. `GameEvents.ClearAll()` runs on scene unload so no dangling delegates.
- **Pools**: arrows, sparkles and mini-hearts are recycled via `GameObjectPool`; pools reset each scene.
- **No physics**: collisions are plain circle-distance checks — no Physics2D settings, no layers, no rigids. Deterministic and cheap.
- **Zero audio assets**: `ProceduralAudio` synthesizes every SFX and the 16-second ambient pad (C–Am–F–G) via `AudioClip.Create`.

## 6. Making it yours

- **Balance**: edit `GameConstants.cs` (one file: speed, fire rate, waves, lives, drop chances, palette).
- **Difficulty curve**: `GameMath.cs`.
- **Art**: replace PNGs in `Assets/Resources/Sprites/` (keep the file names).
- **New pickup**: copy `Pickup.cs`'s pattern — add a `Kind`, wire the effect in `Collect()`.
- **More waves**: change `TotalWaves` in `GameConstants`.

## 7. Troubleshooting

| Symptom | Fix |
|---|---|
| "Project was created with a different Unity version" | Install the exact version from Hub, or open with your 6000.0.x and let it upgrade (safe for this project) |
| Scene looks empty in the editor | Expected — press Play; everything is code-built |
| Buttons don't respond | The EventSystem is created automatically; if you added your own UI, ensure exactly one EventSystem exists |
| No sound in WebGL browser | Browsers block audio until the first click — click anywhere once |
| Build fails on image tag in CI | See docs/CI_GUIDE.md §4 |
