using System.Collections;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Activities
{
    /// <summary>
    /// Treasure hunt: a clue chain pointing at region landmarks. Clues are revealed one at
    /// a time; reaching the final spot grants a rare collectible. Explore-driven by design.
    /// </summary>
    public sealed class TreasureHuntActivity : ActivityBase
    {
        public override string Id => "treasure_hunt";
        public override string DisplayName => "Treasure Hunt";

        World.PoiDefinition _targetPoi;

        protected override void OnStarted()
        {
            var catalog = Services.Get<World.RegionCatalogService>();
            var region = catalog?.ActiveRegion;
            _targetPoi = null;
            if (region != null)
                foreach (var poi in region.Pois)
                    if (poi.kind == World.PoiKind.Treasure) { _targetPoi = poi; break; }
            if (_targetPoi == null)
            {
                // no dedicated treasure POI in this region: fall back to a random landmark
                if (region != null && region.Pois.Count > 0) _targetPoi = region.Pois[Random.Range(0, region.Pois.Count)];
            }
        }

        protected override IEnumerator Run()
        {
            if (_targetPoi == null) { Fail("No treasure clues here..."); yield break; }
            var region = Services.Get<World.RegionCatalogService>()?.ActiveRegion;
            if (region == null) { Fail("Region unknown"); yield break; }

            GameEvents.Publish(new NotificationEvent
            {
                Title = "Treasure hunt",
                Body = "A clue: \"Where light meets stone, the past sleeps.\"",
                Duration = 5f
            });

            var worldTarget = region.WorldCenter + new Vector3(_targetPoi.x, 0f, _targetPoi.z);
            var streamer = Services.Get<World.WorldStreamer>();
            float timeout = 240f;
            while (timeout > 0f && State == ActivityState.Running)
            {
                timeout -= Time.deltaTime;
                if (streamer != null)
                {
                    var dist = (streamer.PlayerPosition - worldTarget).magnitude;
                    ObjectiveProgress = Mathf.Clamp01(1f - dist / 400f);
                    ObjectiveText = $"Follow the clue... {dist:F0}m";
                    if (dist < 6f)
                    {
                        Grant("item_treasure_chest", 1);
                        Grant("collect_ancient_artifact", 1);
                        Complete();
                        yield break;
                    }
                }
                yield return null;
            }
            if (State == ActivityState.Running) Fail("The trail went cold.");
        }
    }

    /// <summary>
    /// Puzzle: sequence of glowing pressure plates near cave/ruin POIs. Watch the sequence,
    /// repeat it with the action button while standing on plates.
    /// </summary>
    public sealed class PuzzleActivity : ActivityBase
    {
        public override string Id => "puzzle";
        public override string DisplayName => "Rune Puzzle";

        Transform _plateRoot;
        readonly Color[] _plateColors = { new Color(1f, 0.4f, 0.4f), new Color(0.4f, 0.7f, 1f), new Color(1f, 0.9f, 0.4f), new Color(0.6f, 1f, 0.5f) };
        readonly int[] _sequence = new int[4];

        protected override void OnStarted()
        {
            _plateRoot = new GameObject("~PuzzlePlates").transform;
            var region = Services.Get<World.RegionCatalogService>()?.ActiveRegion;
            var streamer = Services.Get<World.WorldStreamer>();
            if (region != null && streamer != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i / 4f * Mathf.PI * 2f;
                    var local = new Vector3(Mathf.Cos(a) * 6f, 0f, Mathf.Sin(a) * 6f);
                    var world = streamer.PlayerPosition + local;
                    world.y = streamer.SampleHeight(world.x, world.z) + 0.08f;
                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    plate.name = $"plate_{i}";
                    plate.transform.SetParent(_plateRoot, false);
                    plate.transform.position = world;
                    plate.transform.localScale = new Vector3(2.6f, 0.12f, 2.6f);
                    plate.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Emissive(_plateColors[i], 1.2f);
                }
            }
            for (int i = 0; i < 4; i++) _sequence[i] = Random.Range(0, 4);
        }

        protected override IEnumerator Run()
        {
            if (_plateRoot == null || _plateRoot.childCount < 4) { Fail("No puzzle site nearby"); yield break; }

            GameEvents.Publish(new NotificationEvent { Title = "Rune puzzle", Body = "Watch the runes, then repeat the pattern!", Duration = 4f });

            // show sequence
            for (int i = 0; i < _sequence.Length; i++)
            {
                var renderer = _plateRoot.GetChild(_sequence[i]).GetComponent<MeshRenderer>();
                var mat = renderer.sharedMaterial;
                mat = MaterialLibrary.Emissive(_plateColors[_sequence[i]], 2.6f);
                renderer.sharedMaterial = mat;
                Services.TryGet<Audio.AudioService>(out var audio);
                audio?.QueueSfx(Audio.ProceduralAudioLibrary.UiClick());
                yield return new WaitForSeconds(0.7f);
                renderer.sharedMaterial = MaterialLibrary.Emissive(_plateColors[_sequence[i]], 1.2f);
                yield return new WaitForSeconds(0.25f);
            }

            // player repeats
            int inputIndex = 0;
            float timeout = 30f;
            var streamer = Services.Get<World.WorldStreamer>();
            var input = Services.Get<Player.InputService>();
            while (timeout > 0f && State == ActivityState.Running)
            {
                timeout -= Time.deltaTime;
                ObjectiveProgress = inputIndex / 4f;
                ObjectiveText = $"Repeat the pattern: {inputIndex}/4";
                if (input != null && input.Active.ActionPressed && streamer != null)
                {
                    // which plate is the player standing on?
                    int standing = -1;
                    for (int p = 0; p < 4; p++)
                    {
                        var plate = _plateRoot.GetChild(p);
                        if ((streamer.PlayerPosition - plate.position).sqrMagnitude < 2.2f * 2.2f) { standing = p; break; }
                    }
                    if (standing < 0)
                    {
                        GameEvents.Publish(new NotificationEvent { Title = "Stand on a rune!", Body = "Step onto a glowing plate first.", Duration = 2f });
                        yield return new WaitForSeconds(0.4f);
                        continue;
                    }
                    var renderer = _plateRoot.GetChild(standing).GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = MaterialLibrary.Emissive(_plateColors[standing], 2.4f);
                    if (standing == _sequence[inputIndex])
                    {
                        inputIndex++;
                        Services.TryGet<Audio.AudioService>(out var audio);
                        audio?.QueueSfx(Audio.ProceduralAudioLibrary.CollectChime());
                        if (inputIndex >= 4)
                        {
                            Grant("collect_crystal_shard", 2);
                            Grant("item_rune_key", 1);
                            Complete();
                            Cleanup();
                            yield break;
                        }
                    }
                    else
                    {
                        Fail("The runes dim... wrong pattern.");
                        Cleanup();
                        yield break;
                    }
                    yield return new WaitForSeconds(0.35f);
                    renderer.sharedMaterial = MaterialLibrary.Emissive(_plateColors[standing], 1.2f);
                }
                yield return null;
            }
            if (State == ActivityState.Running) { Fail("The runes faded away."); Cleanup(); }
        }

        void Cleanup()
        {
            if (_plateRoot != null) Object.Destroy(_plateRoot.gameObject);
            _plateRoot = null;
        }

        protected override void OnCancelled() => Cleanup();
    }
}
