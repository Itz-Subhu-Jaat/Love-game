#!/usr/bin/env python3
"""Generates Assets/Resources/Data/*.json for Love Game.

24 regions from the master prompt's world design, each with terrain/sky/weather
profiles and POI sets that map to real gameplay hooks (fishing, picnic, race...).
Also: item catalog, furniture catalog, content manifest, loading tips.
"""
import json
import os

ROOT = "/home/z/my-project/Love-game"
DATA = os.path.join(ROOT, "Assets/Resources/Data")

# ----------------------------------------------------------------- regions
# (id, name, biome, seed, gridX, gridZ, desc, pois, fastTravel, vehicles, overrides)
# Poi: (id, name, kind, x, z, radius)

def poi(pid, name, kind, x, z, radius=8.0):
    return {"id": pid, "name": name, "kind": kind, "x": x, "z": z, "radius": radius}

def ft(pid, name, x, z):
    return {"id": pid, "name": name, "x": x, "z": z}

def veh(kind, x, z):
    return {"kind": kind, "x": x, "z": z}

REGIONS = [
    dict(id="azure_haven", name="Azure Haven", biome="Tropical", seed=101, gx=1, gz=1,
         desc="Main tropical home region: sandy beaches, turquoise ocean, palm forests and the couple's beach house.",
         water=2.5, amp=12, mtn=38, mtnr=210, colors=dict(low="#E8D8A8", mid="#6DBA5A", high="#8A8A8A", road="#D9C9A3", water="#2EC4B6"),
         sky=dict(top="#3FA9E8", hor="#BDE8F5", bot="#E8D8A8", sun="#FFF0C9", amb="#9FB8C8", fog="#CFE8F0", fogd=0.0016),
         weather="62,24,9,2,3", ambience="ocean", density=1.15,
         pois=[
             poi("beach_house", "Beach House", "Home", -70, 40, 10),
             poi("town_market", "Haven Market", "Shop", 20, 60, 12),
             poi("sunset_grill", "Sunset Grill", "Restaurant", 55, 30, 10),
             poi("marina", "Azure Marina", "Marina", 140, -40, 14),
             poi("picnic_grove", "Palm Picnic Grove", "Picnic", -40, -60, 9),
             poi("cliff_falls", "Cliffside Waterfall", "Landmark", -170, -120, 12),
             poi("fishing_dock", "Old Fishing Dock", "Fishing", 160, 60, 8),
             poi("view_south", "South Beach View", "Viewpoint", 0, -240, 10),
             poi("hidden_beach", "Hidden Cove", "Beach", 210, 180, 10),
             poi("cave_mouth", "Grotto Entrance", "Cave", -210, 150, 8),
         ],
         fts=[ft("ft_haven_plaza", "Haven Plaza", 10, 10), ft("ft_marina", "Azure Marina", 140, -30)],
         vehicles=[veh("Car", 120, 90), veh("Motorcycle", 30, -100), veh("Boat", 150, -60)]),

    dict(id="sunset_bay", name="Sunset Bay", biome="Coastal", seed=202, gx=2, gz=1,
         desc="Romantic coastal region with a lighthouse, boardwalk, bonfire beach and the best sunset on the coast.",
         water=2.5, amp=10, mtn=26, mtnr=180, colors=dict(low="#F2DCA8", mid="#7AB86A", high="#9A8F86", road="#D8C2A0", water="#FF8E6E"),
         sky=dict(top="#5C4FA8", hor="#FFA36E", bot="#FFD9B0", sun="#FF9D5C", amb="#B49BC8", fog="#F2C4A8", fogd=0.0019),
         weather="55,25,10,4,6", ambience="ocean", density=1.0,
         pois=[
             poi("lighthouse", "Sunset Lighthouse", "Lighthouse", -180, -200, 12),
             poi("beach_cafe", "Bay Cafe", "Restaurant", 60, 70, 10),
             poi("bonfire", "Bonfire Beach", "Camp", 20, -140, 10),
             poi("photo_arch", "Lovers' Arch", "PhotoSpot", -90, 90, 8),
             poi("boardwalk", "Sunset Boardwalk", "Boardwalk", 120, 40, 14),
             poi("hidden_cave", "Tide Cave", "Cave", 190, -190, 8),
             poi("view_cliff", "Cliff Viewpoint", "Viewpoint", -150, 200, 10),
             poi("fishing_pier", "Bay Pier", "Fishing", 150, -80, 8),
         ],
         fts=[ft("ft_bay_beach", "Sunset Beach", 0, 0), ft("ft_lighthouse", "Lighthouse", -170, -180)],
         vehicles=[veh("Car", 50, 20), veh("Boat", 160, -70)]),

    dict(id="dreamfall_isles", name="Dreamfall Isles", biome="FantasyIslands", seed=303, gx=3, gz=1,
         desc="Fantasy floating islands: giant trees, glowing vegetation and waterfalls falling into the sky.",
         water=-30, amp=8, mtn=20, mtnr=150, colors=dict(low="#C9B8E8", mid="#8FD8A8", high="#B8A8E0", road="#D8C8F0", water="#6ED8F0"),
         sky=dict(top="#7B9AE8", hor="#C8D8F5", bot="#E8D8F2", sun="#FFF5D8", amb="#B8C8E8", fog="#D8E0F5", fogd=0.0022),
         weather="50,25,8,3,14", ambience="wind", density=0.9,
         pois=[
             poi("giant_tree", "The Elder Tree", "Landmark", 40, 30, 16),
             poi("float_market", "Isle Village", "Shop", -80, 60, 12),
             poi("sky_view", "Skyview Peak", "Viewpoint", 150, -120, 10),
             poi("ruins_float", "Floating Ruins", "Ruins", -160, -100, 12),
             poi("glow_grove", "Glowing Grove", "PhotoSpot", 90, 150, 10),
             poi("bridge_span", "Dream Bridge", "Landmark", 0, -60, 10),
             poi("cave_isle", "Sky Cave", "Cave", -200, 160, 8),
         ],
         fts=[ft("ft_dreamfall", "Isle Arrival", 0, 0)],
         vehicles=[veh("Hover", -30, -30)]),

    dict(id="neon_abyss", name="Neon Abyss City", biome="CyberCity", seed=404, gx=4, gz=1,
         desc="Vertical cyberpunk megacity: neon streets, elevated roads, rooftops and an underground district.",
         water=-40, amp=2, mtn=6, mtnr=100, colors=dict(low="#3A3A4A", mid="#4A4A5A", high="#2A2A35", road="#2E2E3E", water="#16203A"),
         sky=dict(top="#0A0A2E", hor="#3A1A5A", bot="#1A0A2A", sun="#FF5CA8", amb="#5A4A7A", fog="#2A1A3A", fogd=0.0032),
         weather="35,30,20,10,5", ambience="city", density=1.3,
         pois=[
             poi("neon_plaza", "Neon Plaza", "Plaza", 0, 60, 16),
             poi("noodle_alley", "Noodle Alley", "Restaurant", -120, 40, 10),
             poi("tech_market", "Abyss Market", "Shop", 130, 90, 12),
             poi("rooftop_bar", "Skyline Rooftop Bar", "Restaurant", 180, -180, 10),
             poi("metro_abyss", "Abyss Metro", "Metro", 60, -120, 10),
             poi("underground", "Underground District", "Landmark", -160, -60, 12),
             poi("race_grid", "Neon Circuit", "Race", 200, 120, 12),
             poi("drone_pad", "Drone Pad", "Tower", -200, 200, 10),
         ],
         fts=[ft("ft_neon_plaza", "Neon Plaza", 0, 50), ft("ft_metro", "Abyss Metro", 60, -110)],
         vehicles=[veh("Hover", 30, 100), veh("Motorcycle", -60, 120), veh("Car", 90, -40)]),

    dict(id="nova_coast", name="Nova Coast", biome="Resort", seed=505, gx=5, gz=1,
         desc="Futuristic tropical resort: glass villas, landing pads and an observation tower over ocean cliffs.",
         water=2.0, amp=8, mtn=30, mtnr=190, colors=dict(low="#E8E0C8", mid="#5AC8B8", high="#A8B8C0", road="#D8D8E0", water="#20C8D8"),
         sky=dict(top="#48C8F0", hor="#C8E8F5", bot="#E8E8F0", sun="#FFF8E8", amb="#A8C8D8", fog="#D8E8F0", fogd=0.0014),
         weather="60,22,10,2,6", ambience="ocean", density=0.9,
         pois=[
             poi("villa_row", "Glass Villas", "Home", -100, 80, 14),
             poi("landing_pad", "Hover Landing Pad", "Landmark", 60, -100, 12),
             poi("research_lab", "Coastal Research Facility", "Landmark", 180, 100, 12),
             poi("obs_tower", "Nova Observation Tower", "Tower", -180, -160, 12),
             poi("resort_pool", "Infinity Pool", "Beach", 20, 150, 10),
             poi("marina_nova", "Nova Marina", "Marina", 150, -50, 12),
             poi("view_cliffs", "Cliff Boardwalk", "Boardwalk", -140, 40, 10),
         ],
         fts=[ft("ft_nova_resort", "Nova Resort", 0, 60)],
         vehicles=[veh("Hover", 70, -80), veh("Boat", 160, -40)]),

    dict(id="wildheart", name="Wildheart Highlands", biome="Mountain", seed=606, gx=0, gz=2,
         desc="Mountainous wilderness: deep valleys, rivers, dense forests and high-altitude trails.",
         water=1.0, amp=22, mtn=64, mtnr=230, colors=dict(low="#C8B89A", mid="#4E8A3E", high="#9A9A98", road="#C0B49A", water="#4890C8"),
         sky=dict(top="#3A8AD8", hor="#B8D8E8", bot="#E8E0D0", sun="#FFF2D8", amb="#98A8B8", fog="#C8D8E0", fogd=0.0021),
         weather="40,28,18,6,8", ambience="forest", density=1.2,
         pois=[
             poi("lookout_tower", "Eagle Lookout Tower", "Tower", 160, -180, 12),
             poi("mountain_lodge", "Highlands Lodge", "Restaurant", -80, 60, 12),
             poi("valley_camp", "Valley Campsite", "Camp", 100, 140, 12),
             poi("waterfall_falls", "Silverthread Falls", "Landmark", -180, -80, 12),
             poi("hiking_trail", "Summit Trailhead", "Viewpoint", 40, -220, 10),
             poi("hot_spring", "Mountain Hot Spring", "Spring", 200, 90, 10),
             poi("cave_deep", "Deepstone Cave", "Cave", -150, 170, 8),
             poi("wild_zone", "Wildlife Valley", "WildlifeZone", 0, 200, 30),
         ],
         fts=[ft("ft_wildheart", "Highlands Gate", 10, 10), ft("ft_summit", "Summit Trailhead", 40, -210)],
         vehicles=[veh("Motorcycle", -50, 90)]),

    dict(id="moonlight_forest", name="Moonlight Forest", biome="NightForest", seed=707, gx=1, gz=2,
         desc="Magical night forest: glowing plants, fireflies, a moonlit clearing and a hidden lake.",
         water=1.5, amp=10, mtn=22, mtnr=170, colors=dict(low="#3A3A5A", mid="#1E4A2E", high="#5A5A6A", road="#4A4A5A", water="#2858A8"),
         sky=dict(top="#0A0A2E", hor="#1A2A4A", bot="#0E1A2E", sun="#B8C8F5", amb="#3A4A68", fog="#1A2438", fogd=0.0028),
         weather="45,20,18,5,12", ambience="forest", density=1.3,
         pois=[
             poi("moon_clearing", "Moonlit Clearing", "Stargaze", 0, 0, 14),
             poi("hidden_lake", "Hidden Lake", "Fishing", 150, 130, 10),
             poi("treehouse", "Old Treehouse", "Landmark", -120, -80, 10),
             poi("ancient_ruins", "Forest Ruins", "Ruins", 90, -160, 12),
             poi("glow_path", "Glowing Path", "PhotoSpot", -60, 120, 8),
             poi("fairy_ring", "Fairy Ring", "Treasure", 180, -40, 8),
             poi("npc_grove", "Grove Keepers", "NpcZone", 40, 80, 14),
         ],
         fts=[ft("ft_moonlight", "Forest Gate", 0, -40)],
         vehicles=[]),

    dict(id="crystal_caverns", name="Crystal Caverns", biome="Cave", seed=808, gx=2, gz=2,
         desc="Underground crystal network: glowing formations, underground lakes and puzzle chambers.",
         water=0.5, amp=6, mtn=34, mtnr=240, colors=dict(low="#3A3A4E", mid="#4A4A66", high="#5A5A78", road="#3E3E52", water="#38B8D8"),
         sky=dict(top="#1A1A2E", hor="#2A2A44", bot="#141422", sun="#7AC8E8", amb="#3A4A6A", fog="#222A3E", fogd=0.0055),
         weather="70,10,15,0,5", ambience="cave", density=1.25,
         pois=[
             poi("cathedral", "Crystal Cathedral", "Landmark", 0, 60, 16),
             poi("under_lake", "Mirror Lake", "Fishing", 120, -60, 12),
             poi("puzzle_chamber", "Rune Chamber", "Cave", -130, 90, 12),
             poi("treasure_vault", "Treasure Vault", "Treasure", 170, 160, 10),
             poi("gem_market", "Deep Market", "Shop", -60, -140, 12),
             poi("echo_hall", "Echo Hall", "PhotoSpot", 200, -120, 10),
         ],
         fts=[ft("ft_caverns", "Cavern Gate", 0, 200)]),

    dict(id="skyline_metropolis", name="Skyline Metropolis", biome="ModernCity", seed=909, gx=3, gz=2,
         desc="Modern premium city: shopping streets, rooftop restaurants, parks and a waterfront plaza.",
         water=0.0, amp=2, mtn=4, mtnr=90, colors=dict(low="#B8B2A8", mid="#8A9A9E", high="#7A8A9A", road="#5A5A62", water="#3878B8"),
         sky=dict(top="#4A90D8", hor="#C8D8E8", bot="#E8E8E2", sun="#FFF8E0", amb="#B0BEC8", fog="#D0D8E0", fogd=0.0018),
         weather="50,28,14,4,4", ambience="city", density=1.2,
         pois=[
             poi("shopping_street", "Grand Shopping Street", "Shop", 0, 120, 16),
             poi("central_park", "Central Park", "Picnic", -120, -60, 14),
             poi("rooftop_rest", "Skyline Rooftop Restaurant", "Restaurant", 160, 40, 10),
             poi("waterfront", "Waterfront Plaza", "Plaza", 60, -170, 16),
             poi("metro_central", "Central Metro", "Metro", -180, 100, 10),
             poi("art_museum", "Museum of Light", "Landmark", 200, -80, 12),
             poi("photo_roof", "Rooftop Photo Spot", "PhotoSpot", -90, 170, 8),
         ],
         fts=[ft("ft_skyline", "Metro Central", -170, 90)],
         vehicles=[veh("Car", 30, 60), veh("Car", -100, 0), veh("Motorcycle", 120, -60)]),

    dict(id="coral_archipelago", name="Coral Reef Archipelago", biome="Ocean", seed=1010, gx=4, gz=2,
         desc="Island-hopping ocean region: coral reefs, a shipwreck and underwater treasure.",
         water=3.0, amp=9, mtn=14, mtnr=130, colors=dict(low="#F0E8D0", mid="#5AC8A8", high="#B8C8C0", road="#E0D8C0", water="#1EC8C0"),
         sky=dict(top="#2EA8E8", hor="#A8E0F0", bot="#E8E8D8", sun="#FFF8E8", amb="#98C8D8", fog="#C0E4EC", fogd=0.0013),
         weather="68,18,10,2,2", ambience="ocean", density=0.8,
         pois=[
             poi("shipwreck", "Old Shipwreck", "Landmark", 140, -140, 14),
             poi("reef_dive", "Coral Reef Dive", "Fishing", 0, 0, 12),
             poi("treasure_isle", "Treasure Isle", "Treasure", -180, 160, 10),
             poi("beach_club", "Isla Beach Club", "Restaurant", 80, 100, 12),
             poi("boat_route", "Boat Route Marker", "Marina", -60, -180, 10),
             poi("uw_cave", "Underwater Cave", "Cave", 190, 90, 10),
             poi("photo_reef", "Reef Photo Spot", "PhotoSpot", -140, -60, 8),
         ],
         fts=[ft("ft_coral", "Archipelago Dock", 60, 100)],
         vehicles=[veh("Boat", 70, 90), veh("Boat", -70, 60)]),

    dict(id="frostpeak", name="Frostpeak", biome="Snow", seed=1111, gx=5, gz=2,
         desc="Snow mountain region: frozen lake, cozy cabins, ski slopes and northern lights nights.",
         water=0.5, amp=20, mtn=70, mtnr=240, colors=dict(low="#E8EEF2", mid="#D8E4E8", high="#F2F6FA", road="#C8D4DC", water="#4878A8"),
         sky=dict(top="#6A90C8", hor="#C8D8E8", bot="#E8EEF4", sun="#F5F8FF", amb="#A8BEC8", fog="#D8E4EC", fogd=0.0024),
         weather="30,30,25,10,5", ambience="wind", density=1.0,
         pois=[
             poi("frozen_lake", "Frozen Lake", "Fishing", 0, 100, 14),
             poi("ski_lodge", "Ski Lodge", "Restaurant", -110, -60, 12),
             poi("cozy_cabin", "Cozy Cabin", "Home", 130, 60, 10),
             poi("ice_caves", "Ice Cave", "Cave", 180, -150, 10),
             poi("northern_view", "Aurora Viewpoint", "Stargaze", -180, 170, 12),
             poi("ski_lift", "Ski Area", "Activity", 60, -190, 12),
             poi("hotspring", "Steam Hot Spring", "Spring", -60, 20, 10),
         ],
         fts=[ft("ft_frostpeak", "Frostpeak Base", 10, -30), ft("ft_aurora", "Aurora Point", -170, 160)],
         vehicles=[veh("Car", 0, -60)]),

    dict(id="golden_desert", name="Golden Desert", biome="Desert", seed=1212, gx=0, gz=3,
         desc="Large desert biome: rolling dunes, an oasis village, ancient ruins and a buried temple.",
         water=-8, amp=7, mtn=26, mtnr=200, colors=dict(low="#E8C888", mid="#D8A860", high="#B08858", road="#D8B078", water="#38A8B8"),
         sky=dict(top="#5AB8E8", hor="#F0D8A8", bot="#F0C888", sun="#FFF0B8", amb="#D8B888", fog="#EFD9A8", fogd=0.0020),
         weather="75,10,5,2,8", ambience="desert", density=0.8,
         pois=[
             poi("oasis_village", "Oasis Village", "Shop", 60, 80, 14),
             poi("ancient_ruins", "Sunken Ruins", "Ruins", -150, -120, 14),
             poi("buried_temple", "Buried Temple", "Cave", 180, 140, 12),
             poi("canyon_overlook", "Canyon Overlook", "Viewpoint", -190, 170, 10),
             poi("night_camp", "Dune Camp", "Camp", 0, -200, 12),
             poi("treasure_dunes", "Lost Caravan", "Treasure", 120, -40, 10),
             poi("desert_race", "Dune Circuit", "Race", -60, 20, 12),
         ],
         fts=[ft("ft_oasis", "Oasis Village", 60, 70)],
         vehicles=[veh("Motorcycle", 0, 0), veh("Car", 80, 60)]),

    dict(id="verdant_valley", name="Verdant Valley", biome="Countryside", seed=1313, gx=1, gz=3,
         desc="Peaceful countryside: farms, rivers, windmills and a sleepy village.",
         water=1.0, amp=8, mtn=18, mtnr=160, colors=dict(low="#D8C898", mid="#7AB85A", high="#A8A890", road="#D0C0A0", water="#4890C8"),
         sky=dict(top="#4AA8E0", hor="#C0E0F0", bot="#E8E0C8", sun="#FFF4D8", amb="#A8B8A0", fog="#D0E0D8", fogd=0.0015),
         weather="55,25,14,2,4", ambience="forest", density=1.0,
         pois=[
             poi("farmstead", "Sunny Farmstead", "Landmark", -100, 60, 14),
             poi("windmill_hill", "Windmill Hill", "Landmark", 120, -80, 12),
             poi("village_square", "Verdant Village", "Shop", 60, 120, 14),
             poi("river_picnic", "Riverside Picnic", "Picnic", -30, -60, 10),
             poi("old_barn", "The Old Barn", "Restaurant", 170, 60, 10),
             poi("view_meadow", "Meadow Viewpoint", "Viewpoint", -180, -170, 10),
         ],
         fts=[ft("ft_verdant", "Village Square", 60, 110)],
         vehicles=[veh("Car", 30, 30)]),

    dict(id="starlight_island", name="Starlight Island", biome="LuxuryIsland", seed=1414, gx=2, gz=3,
         desc="Private luxury island: grand villa, infinity pool, yacht dock and starlit gardens.",
         water=2.5, amp=6, mtn=12, mtnr=120, colors=dict(low="#F0E4C8", mid="#6AB888", high="#B8C0C8", road="#E8E0D0", water="#18B8C8"),
         sky=dict(top="#283868", hor="#8898C8", bot="#E0D8C8", sun="#FFE8C8", amb="#8898B8", fog="#B0B8D8", fogd=0.0016),
         weather="50,20,12,3,15", ambience="ocean", density=0.85,
         pois=[
             poi("grand_villa", "Starlight Villa", "Home", -60, 80, 16),
             poi("infinity_pool", "Infinity Pool", "Beach", -20, 150, 12),
             poi("yacht_dock", "Yacht Dock", "Marina", 140, -80, 12),
             poi("garden_maze", "Moonlit Gardens", "PhotoSpot", 60, -30, 12),
             poi("obs_deck", "Observation Deck", "Stargaze", -160, -170, 12),
             poi("private_beach", "Private Beach", "Beach", 180, 170, 10),
         ],
         fts=[ft("ft_starlight", "Villa Dock", 140, -70)],
         vehicles=[veh("Boat", 150, -60)]),

    dict(id="sunken_city", name="Sunken City", biome="Underwater", seed=1515, gx=3, gz=3,
         desc="Submerged exploration: drowned streets, underwater tunnels and hidden treasure rooms.",
         water=24, amp=10, mtn=16, mtnr=180, colors=dict(low="#3A5A6A", mid="#2E4A5A", high="#4A6A78", road="#3E5868", water="#1878C8"),
         sky=dict(top="#1878A8", hor="#28A0C8", bot="#0A4A68", sun="#A8E0F0", amb="#2E5868", fog="#1878A8", fogd=0.0060),
         weather="80,5,10,0,5", ambience="underwater", density=1.1,
         pois=[
             poi("drowned_plaza", "Drowned Plaza", "Plaza", 0, 60, 16),
             poi("reef_market", "Pearl Diver's Market", "Shop", -120, 100, 12),
             poi("ship_graveyard", "Ship Graveyard", "Landmark", 150, -130, 14),
             poi("uw_tunnel", "Sunken Tunnel", "Cave", -160, -90, 10),
             poi("treasure_room", "Sunken Treasury", "Treasure", 190, 150, 10),
             poi("coral_garden", "Coral Garden", "PhotoSpot", 80, -40, 12),
         ],
         fts=[ft("ft_sunken", "Diver's Bell", 0, 180)]),

    dict(id="technova", name="Technova District", biome="Tech", seed=1616, gx=4, gz=3,
         desc="Advanced technology zone: laboratories, robotics bays and elevated transport platforms.",
         water=-20, amp=3, mtn=6, mtnr=100, colors=dict(low="#5A6A78", mid="#4A5A6A", high="#6A7A88", road="#485868", water="#2898C8"),
         sky=dict(top="#2E4868", hor="#6888A8", bot="#3E4E60", sun="#D8F0FF", amb="#5A7088", fog="#48607A", fogd=0.0026),
         weather="45,30,15,5,5", ambience="city", density=1.15,
         pois=[
             poi("lab_plaza", "Innovation Plaza", "Plaza", 0, 100, 16),
             poi("robotics_bay", "Robotics Bay", "Landmark", -140, -60, 14),
             poi("sky_tram", "Elevated Tram Station", "Metro", 120, 140, 10),
             poi("hidden_lab", "Classified Lab", "Cave", 180, -180, 10),
             poi("drone_zone", "Drone Zone", "Activity", -80, 180, 12),
             poi("tech_market", "Technova Market", "Shop", 60, -120, 12),
         ],
         fts=[ft("ft_technova", "Technova Gate", 10, 100)],
         vehicles=[veh("Hover", 0, 40)]),

    dict(id="rainbow_meadows", name="Rainbow Meadows", biome="Meadows", seed=1717, gx=5, gz=3,
         desc="Dreamy colorful landscape: flower fields, pastel streams and giant friendly trees.",
         water=1.5, amp=6, mtn=10, mtnr=140, colors=dict(low="#F8D8E8", mid="#A8E8B8", high="#E8C8F8", road="#F0E0F0", water="#78C8F0"),
         sky=dict(top="#8AC8F0", hor="#F8D8E8", bot="#FFF0F8", sun="#FFF8E0", amb="#D8C8E8", fog="#F0D8E8", fogd=0.0014),
         weather="65,18,8,1,8", ambience="wind", density=1.35,
         pois=[
             poi("flower_field", "Endless Flower Field", "PhotoSpot", 0, 0, 14),
             poi("pastel_stream", "Pastel Stream", "Fishing", 120, 90, 10),
             poi("giant_willow", "Grandmother Willow", "Landmark", -140, -110, 14),
             poi("petal_picnic", "Petal Picnic Lawn", "Picnic", 60, -160, 10),
             poi("fairy_falls", "Fairy Falls", "Landmark", -180, 130, 10),
             poi("meadow_stage", "Meadow Stage", "Activity", 170, 20, 12),
         ],
         fts=[ft("ft_meadows", "Meadow Gate", 0, 200)]),

    dict(id="lost_kingdom", name="Lost Kingdom", biome="Ruins", seed=1818, gx=0, gz=4,
         desc="Adventure fantasy region: an ancient castle, broken bridges and underground chambers.",
         water=0.5, amp=14, mtn=40, mtnr=220, colors=dict(low="#C8B898", mid="#5A9A4E", high="#A8A890", road="#C0B090", water="#3888C8"),
         sky=dict(top="#3E78B8", hor="#B8C8D8", bot="#D8D0B8", sun="#F0E8C8", amb="#8A98A0", fog="#B8C0C8", fogd=0.0022),
         weather="40,25,18,7,10", ambience="wind", density=1.05,
         pois=[
             poi("castle", "The Ancient Castle", "Landmark", 0, 100, 18),
             poi("broken_bridge", "Broken Bridge", "Landmark", -140, -80, 12),
             poi("under_chamber", "King's Chamber", "Cave", 150, -150, 12),
             poi("tower_ruin", "Watchtower Ruin", "Tower", -180, 180, 10),
             poi("treasure_crypt", "Royal Crypt", "Treasure", 90, -30, 10),
             poi("puzzle_gate", "Sealed Gate", "Cave", -60, 190, 10),
         ],
         fts=[ft("ft_kingdom", "Castle Approach", 0, 180)]),

    dict(id="ocean_drive", name="Ocean Drive", biome="Highway", seed=1919, gx=1, gz=4,
         desc="Coastal highway system: scenic roads, tunnels, bridges and service stations.",
         water=1.5, amp=5, mtn=20, mtnr=170, colors=dict(low="#D8CFA8", mid="#8A9A6A", high="#9A9888", road="#585860", water="#28A8C8"),
         sky=dict(top="#38A0E8", hor="#B0D8E8", bot="#E0D8C0", sun="#FFF6E0", amb="#A0B8C8", fog="#C0D8E0", fogd=0.0013),
         weather="60,22,10,3,5", ambience="ocean", density=0.7,
         pois=[
             poi("vista_point", "Cliff Vista Point", "Viewpoint", 0, -220, 12),
             poi("coast_tunnel", "Coastal Tunnel", "Landmark", -160, 0, 12),
             poi("service_stop", "Route 12 Service Stop", "Restaurant", 100, 80, 12),
             poi("long_bridge", "Great Sea Bridge", "Landmark", 190, -160, 14),
             poi("race_coast", "Coastal Sprint", "Race", 60, 40, 12),
             poi("rest_stop", "Palm Rest Stop", "Picnic", -80, 160, 10),
         ],
         fts=[ft("ft_drive", "Ocean Drive Start", 0, 190)],
         vehicles=[veh("Car", 0, 120), veh("Motorcycle", 40, 150), veh("Car", -40, 110)]),

    dict(id="lumen_city", name="Lumen City", biome="ModernCity", seed=2020, gx=2, gz=4,
         desc="Bright futuristic city: garden towers, water features and a grand central plaza.",
         water=0.0, amp=2, mtn=5, mtnr=90, colors=dict(low="#C0C8B8", mid="#7A9A8A", high="#88A898", road="#687870", water="#38A8D8"),
         sky=dict(top="#58B8E8", hor="#D0E8F0", bot="#E8F0EA", sun="#FFF8E8", amb="#B8C8C0", fog="#D8E8E4", fogd=0.0015),
         weather="55,25,12,3,5", ambience="city", density=1.15,
         pois=[
             poi("grand_plaza", "Grand Central Plaza", "Plaza", 0, 0, 20),
             poi("garden_tower", "Garden Tower", "Tower", -150, -140, 12),
             poi("water_promenade", "Water Promenade", "Boardwalk", 140, 90, 14),
             poi("entertainment", "Lumen Entertainment District", "Activity", 170, -70, 14),
             poi("lumen_market", "Lumen Market", "Shop", -100, 120, 12),
             poi("sky_walkway", "Elevated Walkway", "PhotoSpot", 60, -180, 10),
         ],
         fts=[ft("ft_lumen", "Grand Plaza", 0, 0)],
         vehicles=[veh("Car", 60, 60), veh("Hover", -60, 40)]),

    dict(id="willow_lake", name="Willow Lake", biome="Lake", seed=2121, gx=3, gz=4,
         desc="Peaceful lake region: wooden docks, lazy boats, hidden island and camping shores.",
         water=2.0, amp=8, mtn=16, mtnr=160, colors=dict(low="#D8CFA8", mid="#5A9A5E", high="#9AA89A", road="#C8BCA0", water="#2878B8"),
         sky=dict(top="#4A98D8", hor="#C0D8E0", bot="#E0E0CC", sun="#FFF4D0", amb="#A0B4B8", fog="#C8D8DC", fogd=0.0018),
         weather="50,25,16,3,6", ambience="forest", density=1.0,
         pois=[
             poi("main_dock", "Willow Dock", "Fishing", 60, 180, 12),
             poi("lake_rest", "Lakeside Restaurant", "Restaurant", -110, 150, 10),
             poi("hidden_isle", "Hidden Island", "Treasure", 0, -60, 12),
             poi("camp_shore", "Lakeside Camp", "Camp", 160, 60, 12),
             poi("boat_rental", "Boat Rental", "Marina", 100, 140, 10),
             poi("stargaze_lake", "Lake Stargazing Pier", "Stargaze", -160, -140, 12),
         ],
         fts=[ft("ft_willow", "Willow Dock", 60, 170)],
         vehicles=[veh("Boat", 90, 160)]),

    dict(id="volcanic_isle", name="Volcanic Isle", biome="Volcano", seed=2222, gx=4, gz=4,
         desc="Volcanic adventure: black-sand beaches, lava caves, hot springs and an abandoned research station.",
         water=1.0, amp=16, mtn=60, mtnr=210, colors=dict(low="#3A3430", mid="#4A4440", high="#2E2A28", road="#44403C", water="#1888A8"),
         sky=dict(top="#685858", hor="#C88868", bot="#483838", sun="#FFB088", amb="#887870", fog="#8A6858", fogd=0.0030),
         weather="40,25,20,12,3", ambience="wind", density=0.9,
         pois=[
             poi("volcano_rim", "Crater Rim", "Viewpoint", 0, -200, 14),
             poi("black_beach", "Black-Sand Beach", "Beach", 170, 170, 12),
             poi("lava_caves", "Lava Tubes", "Cave", -140, -60, 12),
             poi("research_station", "Abandoned Station", "Landmark", 160, -40, 12),
             poi("hot_springs", "Volcanic Hot Springs", "Spring", -60, 100, 12),
             poi("treasure_lava", "Forgotten Cache", "Treasure", 100, 60, 8),
         ],
         fts=[ft("ft_volcanic", "Isle Landing", 170, 160)]),

    dict(id="cloud_gardens", name="Cloud Gardens", biome="Cloud", seed=2323, gx=5, gz=4,
         desc="High-altitude fantasy: floating garden platforms, temples above the clouds and sky bridges.",
         water=-20, amp=6, mtn=18, mtnr=140, colors=dict(low="#E8E8F0", mid="#B8E0C8", high="#F0F0F8", road="#E0E0EA", water="#A8D8F0"),
         sky=dict(top="#7AB8F0", hor="#E8F0F8", bot="#F8F8FF", sun="#FFF8F0", amb="#C8D8E8", fog="#E0E8F0", fogd=0.0020),
         weather="45,25,8,2,20", ambience="wind", density=0.85,
         pois=[
             poi("sky_temple", "Temple of Air", "Landmark", 0, 100, 16),
             poi("cloud_bridge", "Cloud Bridge", "Landmark", -120, -80, 12),
             poi("sky_garden", "Hanging Gardens", "PhotoSpot", 140, 40, 12),
             poi("meditation_platform", "Meditation Platform", "Stargaze", -160, 160, 10),
             poi("waterfall_sky", "Skyfall Waterfall", "Landmark", 80, -170, 12),
             poi("cloud_market", "Cloud Market", "Shop", 180, 140, 10),
         ],
         fts=[ft("ft_cloud", "Ascension Gate", 0, 200)]),

    dict(id="old_harbor", name="Old Harbor", biome="Harbor", seed=2424, gx=0, gz=5,
         desc="Historic coastal town: cobbled streets, fish markets, warehouses and a proud old lighthouse.",
         water=2.0, amp=6, mtn=16, mtnr=160, colors=dict(low="#C8B898", mid="#8A9A78", high="#A8A098", road="#B0A898", water="#287898"),
         sky=dict(top="#5A9AD8", hor="#C8D8E0", bot="#E0D8C8", sun="#FFF2D8", amb="#A0B0B8", fog="#C0D0D8", fogd=0.0019),
         weather="45,25,18,6,6", ambience="ocean", density=1.05,
         pois=[
             poi("fish_market", "Old Fish Market", "Shop", 0, 120, 14),
             poi("harbor_front", "Harbor Front", "Marina", 60, -60, 14),
             poi("lighthouse_old", "The Old Lighthouse", "Lighthouse", -180, -190, 12),
             poi("cobbled_street", "Cobbled Street", "Landmark", -90, 40, 10),
             poi("warehouse_row", "Warehouse Row", "Landmark", 150, 100, 12),
             poi("seafood_house", "Grandma's Seafood House", "Restaurant", 40, 190, 10),
             poi("harbor_race", "Harbor Sprint", "Race", 170, -120, 12),
         ],
         fts=[ft("ft_harbor", "Harbor Front", 60, -50)],
         vehicles=[veh("Boat", 80, -70), veh("Car", -40, 80)]),
]

def build_region(r):
    size = 700
    spawn = {"x": 0.0, "z": 30.0}
    return {
        "id": r["id"], "name": r["name"], "description": r["desc"],
        "biome": r["biome"], "seed": r["seed"],
        "worldX": r["gx"], "worldZ": r["gz"], "size": size,
        "waterLevel": r.get("water", 2.0),
        "amplitude": r.get("amp", 10.0), "frequency": 1.0,
        "mountainHeight": r.get("mtn", 30.0), "mountainRadius": r.get("mtnr", 200.0),
        "flattenCenter": 60.0,
        "terrainLow": r["colors"]["low"], "terrainMid": r["colors"]["mid"], "terrainHigh": r["colors"]["high"],
        "roadColor": r["colors"]["road"], "waterColor": r["colors"]["water"],
        "skyTop": r["sky"]["top"], "skyHorizon": r["sky"]["hor"], "skyBottom": r["sky"]["bot"],
        "sunColor": r["sky"]["sun"], "sunIntensity": 1.1,
        "ambientColor": r["sky"]["amb"], "fogColor": r["sky"]["fog"], "fogDensity": r["sky"]["fogd"],
        "weatherWeights": r.get("weather", "60,25,10,2,3"),
        "ambience": r.get("ambience", "wind"),
        "propDensity": r.get("density", 1.0),
        "spawnX": spawn["x"], "spawnZ": spawn["z"],
        "pois": r["pois"],
        "fastTravel": r["fts"],
        "vehicleSpawns": r.get("vehicles", []),
    }

# ------------------------------------------------------------------ items
ITEMS = [
    # gifts
    ("gift_rose_bouquet", "Rose Bouquet", "A dozen fresh roses.", "Gift", 55, "#E8384F"),
    ("gift_chocolate_box", "Chocolate Box", "Handmade pralines.", "Gift", 40, "#8A5838"),
    ("gift_pearl_necklace", "Pearl Necklace", "Ocean-harvested pearls.", "Gift", 120, "#F0E8E0"),
    ("gift_stuffed_bear", "Stuffed Bear", "Soft and loyal.", "Gift", 30, "#B08858"),
    ("gift_music_box", "Music Box", "Plays your song.", "Gift", 80, "#C8B898"),
    ("gift_star_chart", "Star Chart", "Your night, mapped.", "Gift", 65, "#3848A8"),
    ("gift_sunset_painting", "Sunset Painting", "A memory on canvas.", "Gift", 90, "#F09858"),
    ("gift_friendship_bracelet", "Friendship Bracelet", "Woven together.", "Gift", 20, "#E8B84F"),
    # fish
    ("fish_clownfish", "Clownfish", "Cheerful reef dweller.", "Fish", 15, "#F08838"),
    ("fish_bluefin", "Bluefin Tuna", "Deep ocean fighter.", "Fish", 45, "#3858C8"),
    ("fish_angelfish", "Angelfish", "Graceful and shy.", "Fish", 20, "#E8D848"),
    ("fish_moonfish", "Moonfish", "Glows softly at night.", "Fish", 60, "#D8E8F0"),
    ("fish_sunfish", "Sunfish", "A gentle giant.", "Fish", 35, "#C89858"),
    # collectibles (region-tagged)
    ("collect_shell_conch", "Conch Shell", "Azure Haven", "Collectible", 10, "#F0E8D8", "azure_haven"),
    ("collect_shell_spiral", "Spiral Shell", "Sunset Bay", "Collectible", 10, "#E8C8A8", "sunset_bay"),
    ("collect_crystal_shard", "Crystal Shard", "Crystal Caverns", "Collectible", 25, "#78D8F8", "crystal_caverns"),
    ("collect_star_fragment", "Star Fragment", "Falls at night", "Collectible", 40, "#F8E8B8", None),
    ("collect_postcard_haven", "Postcard: Azure Haven", "Azure Haven", "Collectible", 5, "#88C8E8", "azure_haven"),
    ("collect_postcard_neon", "Postcard: Neon Abyss", "Neon Abyss City", "Collectible", 5, "#D858C8", "neon_abyss"),
    ("collect_postcard_frost", "Postcard: Frostpeak", "Frostpeak", "Collectible", 5, "#D8E8F8", "frostpeak"),
    ("collect_postcard_lumen", "Postcard: Lumen City", "Lumen City", "Collectible", 5, "#A8D8B8", "lumen_city"),
    ("collect_ancient_artifact", "Ancient Artifact", "Lost Kingdom", "Collectible", 80, "#C8B088", "lost_kingdom"),
    ("collect_pearl_black", "Black Pearl", "Sunken City", "Collectible", 100, "#383848", "sunken_city"),
    ("collect_coral_pink", "Pink Coral", "Coral Reef", "Collectible", 15, "#F8A8B8", "coral_archipelago"),
    ("collect_map_scrap", "Torn Map Piece", "Ocean Drive", "Collectible", 18, "#D8C8A8", "ocean_drive"),
    ("collect_dune_flower", "Desert Bloom", "Golden Desert", "Collectible", 12, "#E8B848", "golden_desert"),
    ("collect_cloud_feather", "Cloud Feather", "Cloud Gardens", "Collectible", 22, "#F0F0F8", "cloud_gardens"),
    # food
    ("food_sandwich", "Heart Sandwich", "Made with love.", "Food", 8, "#E8C888"),
    ("food_toasted_marshmallow", "Toasted Marshmallow", "Campfire classic.", "Food", 5, "#F8E8D8"),
    ("food_tropical_juice", "Tropical Juice", "Fresh from the islands.", "Food", 10, "#F8B838"),
    ("food_hot_cocoa", "Hot Cocoa", "For cold peaks.", "Food", 12, "#8A5838"),
    # tools / quest
    ("item_fishing_rod", "Travel Fishing Rod", "Fits in any bag.", "Tool", 30, "#A88858"),
    ("item_camera", "Instant Camera", "Cherish moments.", "Tool", 60, "#585868"),
    ("item_rune_key", "Rune Key", "Puzzle reward.", "Quest", 50, "#78D8F8"),
    ("item_treasure_chest", "Treasure Chest", "Contains the past.", "Quest", 150, "#C8A058"),
    ("item_memory_charm", "Memory Charm", "Glows near memories.", "Quest", 70, "#E8A8C8"),
    ("item_race_trophy", "Race Trophy", "For fastest lovers.", "Quest", 100, "#F8D858"),
    ("item_carnival_ticket", "Carnival Ticket", "Target game prize.", "Quest", 25, "#E87858"),
    # cosmetics
    ("cosmetic_scarf_red", "Red Scarf", "Cozy and bright.", "Cosmetic", 35, "#D84848"),
    ("cosmetic_hat_straw", "Straw Hat", "Beach essential.", "Cosmetic", 25, "#E8C888"),
]

def build_items():
    items = []
    for entry in ITEMS:
        item = {
            "id": entry[0], "name": entry[1], "description": entry[2], "category": entry[3],
            "stackable": entry[3] not in ("Tool", "Quest", "Cosmetic"),
            "maxStack": 99 if entry[3] in ("Gift", "Fish", "Food", "Collectible") else 1,
            "value": entry[4], "color": entry[5], "regionId": entry[6] if len(entry) > 6 else "",
        }
        items.append(item)
    return items

# -------------------------------------------------------------- furniture
FURNITURE = [
    ("bed_queen", "Queen Bed", "Bed", "#E8E0F0"), ("bed_double_pine", "Pine Double Bed", "Bed", "#D8C8A8"),
    ("sofa_corner", "Corner Sofa", "Sofa", "#B85878"), ("sofa_cream", "Cream Sofa", "Sofa", "#F0E8D8"),
    ("table_coffee", "Coffee Table", "Table", "#A87848"), ("table_dining", "Dining Table", "Table", "#8A6838"),
    ("chair_wood", "Wooden Chair", "Chair", "#B08858"), ("chair_pouf", "Cozy Pouf", "Chair", "#C87888"),
    ("lamp_floor", "Floor Lamp", "Lamp", "#F8E8B8"), ("lamp_string", "String Lights", "Lamp", "#F8D878"),
    ("plant_monstera", "Monstera", "Plant", "#4A8A5A"), ("plant_succulent", "Succulent", "Plant", "#7AB87A"),
    ("painting_sunset", "Sunset Painting", "Painting", "#F8A868"), ("painting_ocean", "Ocean Print", "Painting", "#88B8E8"),
    ("shelf_book", "Bookshelf", "Shelf", "#A88158"), ("shelf_display", "Display Shelf", "Shelf", "#C8B088"),
    ("electronics_tv", "Smart TV", "Electronics", "#585868"), ("electronics_music", "Music Station", "Electronics", "#8878A8"),
    ("rug_round", "Round Rug", "Rug", "#E8C8B8"), ("rug_star", "Star Pattern Rug", "Rug", "#B8C8E8"),
    ("deco_vase", "Ceramic Vase", "Decoration", "#E8D8C8"), ("deco_clock", "Wall Clock", "Decoration", "#C8C8C8"),
    ("outdoor_hammock", "Hammock", "Outdoor", "#D8B888"), ("outdoor_firebowl", "Fire Bowl", "Outdoor", "#8A5838"),
]

# --------------------------------------------------------------- manifest
def build_manifest():
    packs = [{
        "id": "core", "name": "Core Game", "description": "Bootstrap, systems and the Azure Haven starter region.",
        "version": "1.0.0", "sizeBytes": 15_000_000, "regionIds": ["azure_haven"], "required": True, "remoteUrl": "",
    }]
    for r in REGIONS:
        if r["id"] == "azure_haven":
            continue
        packs.append({
            "id": f"region_{r['id']}", "name": f"Region: {r['name']}",
            "description": r["desc"][:80],
            "version": "1.0.0", "sizeBytes": 8_000_000 + (r["seed"] % 5) * 1_500_000,
            "regionIds": [r["id"]], "required": False, "remoteUrl": f"packs/region_{r['id']}.pack",
        })
    return {"schemaVersion": 1, "remoteContentBaseUrl": "", "packs": packs}

# ------------------------------------------------------------------- tips
TIPS = [
    "Hold RUN to sprint - or take it slow and enjoy the view.",
    "Walk close together to unlock couple interactions.",
    "Discover fast travel points by walking up to them.",
    "Fishing rewards patience: hook the bite fast!",
    "Stargazing is best after sunset at a stargaze spot.",
    "Open the world map (MAP button) to travel between discovered regions.",
    "Gifts deepen your bond - check the inventory Gifts tab.",
    "Photo mode hides the HUD for perfect shots.",
    "Each region hides collectibles - find them all.",
    "Storms pass. Rainy picnics are still romantic.",
    "Watch the view together from a viewpoint bench.",
    "Vehicles appear at region spawn areas - look for cars, bikes and boats.",
    "Rune puzzles in caves reward crystal shards.",
    "The day lasts 24 minutes - sunsets are worth waiting for.",
    "Your home can be refurnished - visit the beach house.",
    "Treasure hunts start from treasure POIs in each region.",
    "Swimming is safe: buoyancy keeps you at the surface.",
    "Press ACT to pop heart targets in the mini-game.",
    "Memories are saved automatically in your journal.",
    "Change graphics quality in Settings for smoother play on older phones.",
]

def main():
    os.makedirs(DATA, exist_ok=True)

    regions_file = {"schemaVersion": 1, "regions": [build_region(r) for r in REGIONS]}
    write("regions.json", regions_file)

    items = build_items()
    write("items.json", {"items": items})
    print(f"items: {len(items)}")

    furniture = [{"id": f[0], "name": f[1], "category": f[2], "color": f[3], "footprint": 1.0} for f in FURNITURE]
    write("furniture.json", {"furniture": furniture})
    print(f"furniture: {len(furniture)}")

    write("content_manifest.json", build_manifest())

    write("tips.json", {"tips": TIPS})

    total_pois = sum(len(r["pois"]) for r in REGIONS)
    total_ft = sum(len(r["fts"]) for r in REGIONS)
    print(f"regions: {len(REGIONS)}, POIs: {total_pois}, fast travel points: {total_ft}")

def write(name, payload):
    path = os.path.join(DATA, name)
    with open(path, "w", newline="\n") as f:
        json.dump(payload, f, indent=2)
    print(f"wrote {path} ({os.path.getsize(path)} bytes)")

if __name__ == "__main__":
    main()
