# CI Guide — Building Love Quest with GitHub Actions + GameCI

This repo builds the game in the cloud for free. You never need Unity installed for the *build itself* — but the one-time license setup uses Unity Hub (which you likely already have if you run the game locally, see [UNITY_SETUP.md](UNITY_SETUP.md)).

The workflow is `.github/workflows/build.yml`.

## 1. One-time setup — get your free Personal license (5 minutes)

GameCI's current (v4) flow: you activate a **free Unity Personal license in Unity Hub on your own PC**, export the license file, and store it in GitHub secrets. The builder action then activates it on every CI run using your email + password.

1. **Install Unity Hub** → https://unity.com/download → sign in (or create a free Unity account).
2. In Unity Hub: **Preferences → Licenses → Add → Get a free personal license**. This writes a `.ulf` license file to your disk.
3. **Find the `.ulf` file** (folders may be hidden — enable "show hidden files"):

   | OS | Path |
   |---|---|
   | Windows | `C:\ProgramData\Unity\Unity_lic.ulf` |
   | macOS | `/Library/Application Support/Unity/Unity_lic.ulf` |
   | Linux | `~/.local/share/unity3d/Unity/Unity_lic.ulf` |

   > The license is not tied to a Unity version, OS or machine type — one file works for all CI builds.
4. Open the `.ulf` file in a text editor (it's XML) and copy **the whole contents**.
5. On GitHub: this repo → **Settings → Secrets and variables → Actions → New repository secret**, and add:

   | Secret name | Value |
   |---|---|
   | `UNITY_LICENSE` | the entire `.ulf` file contents |
   | `UNITY_EMAIL` | your Unity account email |
   | `UNITY_PASSWORD` | your Unity account password |

> If you can't get Unity Hub to show the "Get a free personal license" option, the GameCI community has workarounds (see https://game.ci/docs and their Discord). GameCI never sees or stores your credentials — they are only used by the builder to reactivate your license during CI.

## 2. Run a build

- **Actions → Build WebGL → Run workflow** → Run (on `main`), or just push to `main` — it builds automatically.
- Takes ~6–12 minutes. When finished, the run page shows the **LoveQuest-WebGL** artifact → download → extract `LoveQuest-WebGL.tar.gz` → open `index.html` in any browser.

## 3. Optional: playable link on GitHub Pages

GitHub Pages hosting is free, **but Pages for private repositories requires a paid GitHub plan**.

- If the repo is **public** (or your plan supports private Pages):
  1. **Settings → Pages → Source: GitHub Actions** (one time).
  2. **Actions → Build WebGL → Run workflow** → tick **deployPages** → Run.
  3. Your link appears in **Settings → Pages** (like `https://itz-subhu-jaat.github.io/Love-game/`).

## 4. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| Workflow "succeeds" with warning *Unity license not configured* | The `UNITY_LICENSE` secret is empty — do §1 and re-run |
| Build fails with "license is not valid / expired" | Personal licenses expire periodically — open Unity Hub, re-activate (Add license), update the `UNITY_LICENSE` secret with the fresh `.ulf` |
| Login errors in build logs | Wrong `UNITY_EMAIL` / `UNITY_PASSWORD`, or a password with special characters that the CLI mishandles — try resetting to an alphanumeric password |
| "image not found" / editor tag missing | The project pins **6000.0.83f1** (verified on Docker Hub). If you changed `ProjectVersion.txt`, pick a version listed at https://hub.docker.com/r/unityci/editor/tags |
| Compile errors in build log | The log's "Build project (WebGL)" step points at the exact script — check the repo integrity (`git status`) |
| Artifact storage warnings | Artifacts auto-expire after 7 days, each build ≈10–20 MB → a sliver of the free 500 MB quota. Delete old runs from the Actions page any time |

## 5. What the workflow does (annotated)

```
checkout
  → check UNITY_LICENSE secret   (skip build with a helpful warning if absent)
  → game-ci/unity-builder@v4     (activates license from env, builds WebGL,
                                  Unity version auto-read from ProjectVersion.txt)
  → tar.gz the build             (locate via index.html — layout-proof)
  → upload artifact              (7-day retention, ~10-20 MB)
  → [opt-in via deployPages] deploy to GitHub Pages
```

- **concurrency**: a newer push cancels the superseded run — saves minutes.
- **No Library cache** on purpose: caches count against the same 500 MB storage quota, and this project imports in ~1 minute anyway.
- The `build/` folder is git-ignored; only the compressed artifact leaves the runner.

## 6. Cost picture (private repo, free plan)

| Resource | Free allowance | This repo's usage |
|---|---|---|
| Actions minutes | 2000 min/month | ~10 min per build |
| Artifact storage | 500 MB | ≤ 20 MB live per kept run, 7-day expiry |
| Pages | free hosting | needs public repo (or paid plan) for private |
