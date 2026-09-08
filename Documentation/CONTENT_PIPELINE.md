# CONTENT PIPELINE — Love Game

Goal: **small initial install** + optional downloadable region packs from a CDN
(Cloudflare R2 by default), with a polished in-game download manager.

## Current state

- **Content manifest** (`Assets/Resources/Data/content_manifest.json`): 1 required core pack
  (Azure Haven + bootstrap systems) + 23 optional region packs with sizes/versions/regionIds.
- **Download manager UI** (menu → Downloads): per-pack state, size, progress, retry, cancel,
  remove; cache size display; Wi-Fi-only policy toggle in Settings; storage checks.
- **ContentService** runtime: pack states (`NotDownloaded/Downloading/Downloaded/UpdateRequired/Error`),
  `UnityWebRequest` downloads with 3 retries + backoff, atomic cache writes
  (`persistentDataPath/ContentCache`), `IsRegionContentReady(regionId)` gate for streaming.
- **RemoteContentBaseUrl** lives in the manifest — configuration, never a credential.
  Empty = fully local (blockout mode, zero network use).

## Addressables bridge

`LoveGame.Content` carries a versionDefines entry: installing `com.unity.addressables`
defines `LOVEGAME_ADDRESSABLES` and compiles the bridge (`AddressablesBridge`):
region packs map 1:1 to Addressables groups/labels; `InitializeRegionAnchor` /
`ReleaseRegion` wrap instantiate/release. Without the package, the bridge compiles to a stub —
gameplay is identical either way. Enabling it later:

1. Open the project in the editor once (window: Package Manager → Addressables).
2. Create Addressables settings (Assets → Addressables → Create settings).
3. Group region prefabs/labels to match pack ids.
4. Switch `ContentService` provider to the bridge in `WorldSystems` (one line).

## CDN upload flow (Cloudflare R2 example)

```
build packs (addressables workflow or raw .pack files)
  → rclone/aws-cli upload to your R2 bucket
  → set remoteContentBaseUrl in content_manifest.json
     (e.g. https://cdn.example.com/lovegame)
  → players' download manager sees + installs packs
```

Secrets (R2 keys) go to GitHub Secrets only. The `addressables.yml` workflow is prepared to
drive the pack build once remote assets exist.

## Versioning & cache rules

- Pack version bumps → clients see `UpdateRequired`, redownload that pack only.
- Unchanged packs are never redownloaded (version check against cached manifest data).
- Optional content is only deleted when the user explicitly removes it (Settings/Downloads).
- Core pack is always present — the game can never brick itself out of the main menu.
