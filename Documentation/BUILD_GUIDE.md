# BUILD GUIDE — Love Game

## CI (recommended: zero local setup)

### Prerequisites (one-time, manual)
1. **Unity license** (free Personal works):
   - Install Unity Hub → sign in → activate a Personal license (Hub → Preferences → Licenses).
   - Grab the license file:
     - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
     - macOS: `~/Library/Application Support/Unity/Unity_lic.ulf`
     - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
   - GitHub → repo → **Settings → Secrets and variables → Actions** → add:
     | Secret | Value |
     |---|---|
     | `UNITY_LICENSE` | full contents of the `.ulf` file |
     | `UNITY_EMAIL` | Unity account email |
     | `UNITY_PASSWORD` | Unity account password |
2. Unity 6000.0.83f1 is pulled automatically by GameCI (`unityVersion: auto` reads ProjectVersion.txt).

### Workflows
| Workflow | Trigger | What it does | License needed |
|---|---|---|---|
| `Validate Project` | every push/PR | static validation (GUIDs, scenes, asmdefs, C#/shaders, catalogs, secret scan) + Unity tests | no (tests skip with a warning) |
| `Build Android APK` | manual dispatch or `v*` tags | IL2CPP/ARM64 APK via `CiBuilder.BuildAndroid` | yes |
| `Release Build (AAB)` | manual dispatch | Play Store AAB via `CiBuilder.BuildAndroidAab` | yes |
| `Content Packs` | manual dispatch | remote content pipeline (prepared) | yes |

### Quota-friendly defaults
- APK/AAB artifacts: **5-day retention** (0.5 GB free storage quota — the reason this repo
  keeps artifacts small and short-lived)
- Unity builds do NOT run on every push — only tags/manual runs; the license-free lint does

### Verifying a build
1. Actions → run → green check
2. Artifacts section → `LoveGame-APK` → download zip → extract `LoveGame.apk`
3. Copy to phone → install (allow unknown sources) → open "Love Game"
4. Expected first-boot: menu → Begin Journey → Azure Haven generates (~2–4 s) → walk with joysticks

### Reading build failures
- **License step fails**: `UNITY_LICENSE` secret missing/invalid — re-copy the `.ulf` content
  (it is XML; include everything, no truncation).
- **Compile errors**: the run log lists `CS####` errors with file/line — fix, push, re-run.
  `CiBuilder` exits non-zero on errors so failed builds are never reported as success.
- **Tests fail**: `Validate Project` → unity-tests job → artifact `test-results`.

## Local builds

1. Unity Hub → install **6000.0.83f1** (+ Android Build Support, OpenJDK, Android SDK/NDK).
2. Open this project folder. First import runs the auto setup wizard (URP assets, quality
   levels, runtime materials, Android player settings). Re-run anytime: menu **Love Game →
   Run Project Setup**.
3. Play `02_World` directly, or build via File → Build Settings (scenes are pre-listed).

## Version pins

| Component | Version | Why |
|---|---|---|
| Unity | 6000.0.83f1 | LTS patch with a published GameCI docker image |
| URP | 17.0.3 | verified 6000.0.x-compatible in production GameCI projects |
| Addressables | 2.11.1 | 6000.0-verified release on packages.unity.com |
| GameCI actions | unity-builder@v4 / unity-test-runner@v4 | current stable, env-based licensing |

Changing the Unity version: update `ProjectSettings/ProjectVersion.txt`, the README table,
and confirm a `unityci/editor:ubuntu-<version>` image exists (hub.docker.com/r/unityci/editor).
