# WORLD DESIGN — Love Game

## One world, not a map list

Regions sit on a 750 m world grid (700 m playable + seams). The player never picks a "map"
from a menu: they walk, drive, sail or fast-travel between neighbors. Border regions
(ocean/harbor/highway) are shaped to feel like the edge of somewhere, not a wall.

## The 24 launch regions

| # | Region | Biome | Hook |
|---|---|---|---|
| 1 | Azure Haven | Tropical | Home base: beach house, marina, waterfalls, hidden cove |
| 2 | Sunset Bay | Coastal | Romance: lighthouse, boardwalk, bonfire beach |
| 3 | Dreamfall Isles | Fantasy islands | Floating islands, glowing groves, sky views |
| 4 | Neon Abyss City | Cyberpunk | Vertical city, underground district, neon race |
| 5 | Nova Coast | Futuristic resort | Glass villas, observation tower, hover pad |
| 6 | Wildheart Highlands | Mountain | Peaks, hot springs, wildlife valley |
| 7 | Moonlight Forest | Night forest | Fireflies, treehouse, hidden lake, fairy ring |
| 8 | Crystal Caverns | Cave | Crystal cathedral, rune puzzles, treasure vault |
| 9 | Skyline Metropolis | Modern city | Shopping street, rooftop restaurant, waterfront |
| 10 | Coral Reef Archipelago | Ocean | Reefs, shipwreck, island hopping, diving |
| 11 | Frostpeak | Snow | Frozen lake, cabins, aurora stargazing |
| 12 | Golden Desert | Desert | Dunes, oasis, buried temple, dune race |
| 13 | Verdant Valley | Countryside | Farms, windmills, riverside picnics |
| 14 | Starlight Island | Luxury island | Private villa, yacht, moonlit gardens |
| 15 | Sunken City | Underwater | Drowned streets, tunnels, treasury |
| 16 | Technova District | Tech | Labs, robotics bay, elevated trams |
| 17 | Rainbow Meadows | Meadows | Flower fields, fairy falls, petal picnics |
| 18 | Lost Kingdom | Ruins | Castle, crypt, sealed puzzle gates |
| 19 | Ocean Drive | Highway | Coastal sprint, tunnels, great sea bridge |
| 20 | Lumen City | Bright futuristic | Grand plaza, garden towers, walkways |
| 21 | Willow Lake | Lake | Docks, boats, hidden island, lakeside camp |
| 22 | Volcanic Isle | Volcano | Crater rim, lava tubes, hot springs |
| 23 | Cloud Gardens | Sky | Temples above clouds, sky bridges |
| 24 | Old Harbor | Historic harbor | Fish market, old lighthouse, seafood house |

All definitions live in `Assets/Resources/Data/regions.json` (biome, palette, weather weights,
POIs, fast travel, vehicle spawns, description). Adding region #25 = one JSON entry (plus
optional ScriptableObject asset via the editor generator). The architecture targets 50–100+
regions: streaming is budgeted, NPC/wildlife tier by distance, nothing scales with total
region count.

## Content mix per region (rule)

Every region must contain: **landmarks** (identity), **traversal** (roads/paths),
**activity locations** (at least one interactable POI), **secrets** (treasure/collectible),
**scenic viewpoints**, **social areas** (picnic/stargaze/shop), **NPC zones** (inhabited
regions), **wildlife**, **vehicles** (where it makes sense) and **environmental storytelling**
(signs, ruins, abandoned structures). Empty terrain is a bug — the EditMode test suite
enforces POI presence.

## Terrain recipe (procedural blockouts)

`RegionContentBuilder`: seeded simplex FBM base + mountain ring + biome profile
(islands sink below water at the border, cities flatten, caves raise a rim, sky regions make
plateaus) + center flattening for the town/spawn + road network (hub-star + ring + POI loops)
that levels and paints the terrain. Vertex colors blend sand/grass/rock/snow by height and
slope; water planes sit at each region's water level. Props scatter by biome palette into
60 m cells for distance culling, then static-batch.

## Time, weather, ambience

24-minute default day (configurable), sunrise/dusk sky blending, stars at night, moon
light at night. Weather rolls per-region by weight (desert 75% clear, stormy at Volcanic
Isle, foggy in Cloud Gardens) with 15 s transitions, rain/snow particles following the
player, thunder SFX in storms. Each region's audio bed switches on entry.
