# BUILD GUIDE — Love Game

## CI (recommended: zero local setup)

### Prerequisites — pick ONE of the two license paths

#### Path A — automated, no Unity Hub (recommended)

Everything runs inside GitHub Actions. Add these secrets
(**Settings → Secrets and variables → Actions**):

| Secret | Value | Required |
|---|---|---|
| `UNITY_EMAIL` | Unity account email | yes |
| `UNITY_PASSWORD` | Unity account password | yes |
| `UNITY_TOTP_KEY` | authenticator-app key (only if 2FA is enabled) | 2FA only |
| `ACCESS_TOKEN` | fine-grained PAT with **Secrets: write** on this repo (auto-stores the license) | for auto-store |

Then run the **Unity License Auto-Activation** workflow (Actions tab → select it →
Run workflow). It:

1. creates the `.alf` activation request with Unity in docker (batch-mode, no license needed),
2. signs into `license.unity3d.com` automatically (game-ci `unity-license-activate`, puppeteer),
   submits the `.alf`, picks the free Personal license and downloads the `.ulf`,
3. stores the `.ulf` as the `UNITY_LICENSE` secret via the GitHub API, then (optionally)
   verifies it by running the editmode test suite.

No `ACCESS_TOKEN`? The run still works — it uploads the `.ulf` as a 1-day artifact; paste its
contents into `UNITY_LICENSE` manually once, done. Re-run the workflow whenever the personal
license expires.

> 2FA notes: the authenticator-app flow is automated via `UNITY_TOTP_KEY`
> (get the key at id.unity.com → settings → two-factor authentication → `Can't scan the
> barcode?`). Email-code 2FA is not automated — prefer the authenticator app.

#### Path B — manual, official GameCI method (one-time, 5 minutes)

1. Install Unity Hub → sign in → activate a Personal license
   (Hub → Preferences → Licenses → Add → *Get a free personal license*).
2. Grab the license file:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `~/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. Add the secrets:
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
| `Unity License Auto-Activation` | manual dispatch | docker `.alf` → automated portal login → `.ulf` → stores `UNITY_LICENSE` secret (+ verification tests) | no — it *acquires* the license |
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
- **License step fails**: `UNITY_LICENSE` secret missing/invalid — run the
  **Unity License Auto-Activation** workflow (Path A above), or re-copy the `.ulf` content
  (it is XML; include everything, no truncation).
- **Auto-activation fails**: download the `license-activation-diagnostics` artifact —
  `error.png` is a full screenshot of the licensing portal at the failure point (wrong
  password, changed portal layout, 2FA prompt without `UNITY_TOTP_KEY`).
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
