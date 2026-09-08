# CI Guide — Building Love Quest with GitHub Actions + GameCI

This repo builds the game in the cloud for free. You never need Unity installed.
The workflow is `.github/workflows/build.yml`.

## 1. One-time setup (2 secrets)

1. Create a **free Unity account**: https://id.unity.com/account/edit (Personal license, no cost).
2. Open this repo on GitHub → **Settings → Secrets and variables → Actions → New repository secret**:

| Secret name | Value |
|---|---|
| `UNITY_EMAIL` | your Unity account email |
| `UNITY_PASSWORD` | your Unity account password |
| `UNITY_TOTP` | *(optional)* your 2FA/TOTP secret — only if you enabled 2FA on the Unity account |

> **TOTP secret?** If you enabled two-factor auth on your Unity account, the login needs a rotating code. The secret (the base32 string shown when you set up 2FA) goes into `UNITY_TOTP`. If you never enabled 2FA, skip this secret entirely.
>
> **Tip:** if your password contains special characters, try a Unity account with an alphanumeric-only password to rule out escaping issues.

## 2. Run a build

- **Actions → Build WebGL → Run workflow → Run** (button on the `main` branch).
- Also runs automatically on every push to `main`.
- Takes ~6–12 minutes. When done, the run page shows the **LoveQuest-WebGL** artifact → download → extract `LoveQuest-WebGL.tar.gz` → open `index.html` in any browser.

## 3. Optional: playable link on GitHub Pages

GitHub Pages hosting is free, **but Pages for private repositories requires a paid GitHub plan**.

- If the repo is **public** (or your plan supports private Pages):
  1. **Actions → Build WebGL → Run workflow** → tick **deployPages** → Run.
  2. First run: enable Pages once — repo **Settings → Pages → Source: GitHub Actions**.
  3. Your link appears in Settings → Pages (something like `https://itz-subhu-jaat.github.io/Love-game/`).

## 4. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| Workflow succeeded with a warning "Unity license not configured" | `UNITY_EMAIL`/`UNITY_PASSWORD` secrets are missing — add them (§1) and re-run |
| License step fails with auth error | Wrong email/password, or 2FA enabled without `UNITY_TOTP`, or account locked — log in at unity.com once in a browser, then retry |
| "image not found" / editor version tag missing | GameCI's Docker image for `6000.0.83f1` is verified on Docker Hub. If you changed `ProjectVersion.txt`, pick a version that has a tag at https://hub.docker.com/r/unityci/editor/tags |
| Build step fails | Open the "Build project (WebGL)" log — compile errors would point at the script; check the repo is intact (`git status`) |
| Artifact storage warnings | Artifacts auto-expire after 7 days and each build is ~10–20 MB — this uses a tiny slice of the free 500 MB quota. Old runs can also be deleted any time from the Actions page |

### Classic license fallback (manual ULF)

If the `game-ci/unity-license@v3` flow ever misbehaves (rare), the older
per-`.ulf` flow still works with `game-ci/unity-builder@v4`:

1. Temporarily add a `unity-activate.yml` workflow using `game-ci/unity-activate@v1` with `requestManualActivationFile: true`.
2. Download the `.alf` artifact, upload it at https://license.unity3d.com/manual (choose Personal), get the `.ulf`.
3. Put the full `.ulf` **contents** into a secret named `UNITY_LICENSE`, and change the builder step's env to `UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}` (remove the license-acquire step).

## 5. What the workflow does (annotated)

```
checkout → check secrets → game-ci/unity-license@v3 (Personal license)
        → game-ci/unity-builder@v4  (WebGL, project version auto-read)
        → tar.gz the build → upload artifact (7-day retention)
        → [opt-in] deploy to GitHub Pages
```

- **concurrency**: a newer push cancels the superseded run — saves minutes.
- **No Library cache** on purpose: caches count against the same 500 MB storage quota and this project imports in ~1 minute anyway.
- The build folder itself is git-ignored; only the compressed artifact leaves the runner.

## 6. Cost picture (private repo, free plan)

| Resource | Free allowance | This repo's usage |
|---|---|---|
| Actions minutes | 2000 min/month | ~10 min per build |
| Artifact storage | 500 MB | ≤ 20 MB live for 7 days per kept run |
| Pages | free hosting | needs public repo (or paid plan) for private |
