# ASSET IMPORT GUIDE — Love Game

Placeholder visuals are 100% procedural and replaceable. This is the replacement contract.

## Conventions (all asset types)

| Rule | Value |
|---|---|
| Scale | 1 unit = 1 meter (human ≈ 1.8 units tall) |
| Orientation | forward = **+Z** (Unity convention) |
| Pivot | characters: feet; props: base-center; vehicles: base-center |
| Naming | `snake_case` ids, no spaces; materials `M_`, models `SM_`, textures `T_` |
| Colliders | included in the prefab where cheap (mesh/box); no wheel colliders (framework adds physics) |
| Materials | URP Lit (project uses URP); emissive items use `LoveGame/EmissiveUnlit` |
| Textures | PNG, power-of-two where possible; Android ETC2 compression handled by import settings |

## Drop-in slots

| System | Slot | How to replace |
|---|---|---|
| Player / Partner visuals | `PlayerCharacter.customModelPrefab` or the rig slots (Body/Head/Hair/Hands/Accessory) | assign a prefab; the procedural rig auto-hides. Poses/animation keep working; add an Animator + clips and map clip names to pose ids later |
| Vehicles | `VehicleService.Spawn` visuals | replace `BuildVisual()` output with a prefab; keep the `seats/driver` + `seats/passenger` child transforms and the `VehicleBase` component |
| Props (trees, rocks, crystals...) | `PropLibrary.Build` | swap the constructed GameObject for a prefab; the region builder only places anchors |
| Furniture | `FurnitureVisual.Build` | same pattern; catalog entry stays in `furniture.json` |
| Region art | `RegionDefinition` ScriptableObjects (menu: Love Game → Regions → Generate Region Assets from JSON) | edit the asset, then replace POI beacon visuals |
| Music / ambience | `AudioService` | replace synthesized clips with licensed AudioClips behind the same API (keep original-or-licensed sources only) |
| UI sprites | `Assets/Resources/Sprites/` | replace PNGs, keep names; everything tints at runtime |

## Import workflow (per asset)

1. Inspect source + license (document license/source in `Documentation/` when practical).
2. Drop the file into the intended `Assets/` folder.
3. Verify: scale (1.8 m human), orientation (+Z), pivot placement.
4. Configure importer: mesh read/write off, colliders as needed, texture compression.
5. If animated: configure rig + clips; map clip names to the system's ids (e.g. `hug`, `dance`).
6. Register with the system (catalog JSON / prefab slot / region asset).
7. Play the relevant scene and test (walk up, interact, drive).
8. Commit with a descriptive message.

## LOD & performance

- Final assets should ship LOD groups (LOD0/LOD1/LOD2 at ~10%/30%/60% screen heights).
  The region builder batches placeholder props statically; imported LOD assets slot into the
  same parents.
- Draw distance/fog per region already hides distant detail.
- Quality levels (Low→Ultra) scale prop count, NPC budget, render scale and shadows —
  respect them when authoring density.

## License safety

- Never commit assets ripped from commercial games.
- Unclear license → flag for review, do not ship.
- All current repo content (sprites, sounds, shaders, code) is original/generated — zero
  third-party copyright exposure.
