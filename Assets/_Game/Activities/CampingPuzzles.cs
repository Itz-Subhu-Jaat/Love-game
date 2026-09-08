using System.Collections;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Activities
{
    /// <summary>Picnic date: sit together, enjoy the ambience, gain a shared memory.</summary>
    public sealed class PicnicActivity : ActivityBase
    {
        public override string Id => "picnic";
        public override string DisplayName => "Picnic Date";

        protected override IEnumerator Run()
        {
            var couple = Services.Get<Interaction.CoupleInteractionManager>();
            if (couple != null && couple.IsRunning) couple.Cancel();

            GameEvents.Publish(new NotificationEvent { Title = "Picnic", Body = "A lovely spot for two...", Duration = 3f });
            float duration = 20f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                ObjectiveProgress = timer / duration;
                ObjectiveText = "Enjoying the picnic...";
                yield return null;
            }
            Services.TryGet<Inventory.MemoryService>(out var memories);
            memories?.Record("picnic", "Picnic date", "activity", "A quiet afternoon together.", Services.Get<World.RegionCatalogService>()?.ActiveRegion?.Id);
            Grant("food_sandwich", 2);
            Grant("item_memory_charm", 1);
            Complete();
        }
    }

    /// <summary>Camping: campfire ambience, rest, advances time to morning.</summary>
    public sealed class CampingActivity : ActivityBase
    {
        public override string Id => "camping";
        public override string DisplayName => "Camping";

        protected override IEnumerator Run()
        {
            GameEvents.Publish(new NotificationEvent { Title = "Camp", Body = "Resting by the fire...", Duration = 3f });
            float duration = 12f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                ObjectiveProgress = timer / duration;
                ObjectiveText = "Resting by the campfire...";
                yield return null;
            }
            var dayNight = Services.Get<World.DayNightCycle>();
            if (dayNight != null) dayNight.Hour = 7f; // wake at sunrise
            Services.TryGet<Inventory.MemoryService>(out var memories);
            memories?.Record("camping", "A night under the stars", "activity", "We camped beneath a sky full of stars.", Services.Get<World.RegionCatalogService>()?.ActiveRegion?.Id);
            Grant("food_toasted_marshmallow", 2);
            Complete();
        }
    }

    /// <summary>Stargazing: night-gated couple activity with a shooting-star moment.</summary>
    public sealed class StargazingActivity : ActivityBase
    {
        readonly World.DayNightCycle _dayNight;
        public override string Id => "stargazing";
        public override string DisplayName => "Stargazing";

        public StargazingActivity(ActivityService service, World.DayNightCycle dayNight)
        {
            _dayNight = dayNight;
            SetService(service);
        }

        public override bool CanStart()
        {
            if (!base.CanStart()) return false;
            bool night = _dayNight != null && _dayNight.IsNight;
            if (!night)
                GameEvents.Publish(new NotificationEvent { Title = "Too bright", Body = "Come back after sunset to stargaze.", Duration = 3f });
            return night;
        }

        protected override IEnumerator Run()
        {
            GameEvents.Publish(new NotificationEvent { Title = "Stargazing", Body = "Make a wish...", Duration = 3f });
            float duration = 25f;
            float timer = 0f;
            bool shootingStarShown = false;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                ObjectiveProgress = timer / duration;
                ObjectiveText = "Watching the stars...";
                if (!shootingStarShown && timer > duration * 0.6f)
                {
                    shootingStarShown = true;
                    GameEvents.Publish(new NotificationEvent { Title = "A shooting star!", Body = "Quick, make a wish.", Duration = 2.5f });
                }
                yield return null;
            }
            Services.TryGet<Inventory.MemoryService>(out var memories);
            memories?.Record("stargazing", "A shooting star", "activity", "We wished on a shooting star together.", Services.Get<World.RegionCatalogService>()?.ActiveRegion?.Id);
            Grant("collect_star_fragment", 1);
            Complete();
        }
    }

    /// <summary>Target mini-game: floating heart targets pop up; hit them with taps for score.</summary>
    public sealed class TargetMiniGameActivity : ActivityBase
    {
        public override string Id => "target_minigame";
        public override string DisplayName => "Heart Targets";
        int _score;

        protected override void OnStarted() => _score = 0;

        protected override IEnumerator Run()
        {
            var streamer = Services.Get<World.WorldStreamer>();
            var input = Services.Get<Player.InputService>();
            if (streamer == null || input == null) { Fail("setup missing"); yield break; }

            var root = new GameObject("~Targets");
            var rng = new Rng(Random.Range(int.MinValue, int.MaxValue));
            var heartMat = MaterialLibrary.Emissive(new Color(1f, 0.35f, 0.5f), 2.2f);
            var targets = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 6; i++)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.Destroy(target.GetComponent<Collider>());
                target.name = $"heart_{i}";
                target.transform.SetParent(root.transform, false);
                var local = rng.InsideUnitCircle() * 14f;
                var world = streamer.PlayerPosition + new Vector3(local.x, 0f, local.y);
                world.y = streamer.SampleHeight(world.x, world.z) + rng.Range(1.5f, 3.5f);
                target.transform.position = world;
                target.transform.localScale = Vector3.one * 0.9f;
                target.GetComponent<MeshRenderer>().sharedMaterial = heartMat;
                targets.Add(target);
            }

            GameEvents.Publish(new NotificationEvent { Title = "Heart targets", Body = "Tap the action button near hearts to pop them!", Duration = 4f });

            float timeout = 60f;
            while (timeout > 0f && State == ActivityState.Running && _score < 6)
            {
                timeout -= Time.deltaTime;
                ObjectiveProgress = _score / 6f;
                ObjectiveText = $"Hearts popped: {_score}/6";

                if (input.Active.ActionPressed)
                {
                    for (int t = targets.Count - 1; t >= 0; t--)
                    {
                        if (targets[t] == null) continue;
                        var dist = (streamer.PlayerPosition - targets[t].transform.position).magnitude;
                        if (dist < 4f)
                        {
                            Object.Destroy(targets[t]);
                            targets.RemoveAt(t);
                            _score++;
                            Services.TryGet<Audio.AudioService>(out var audio);
                            audio?.QueueSfx(Audio.ProceduralAudioLibrary.CollectChime());
                        }
                    }
                }
                // gentle bobbing so targets feel alive
                for (int t = 0; t < targets.Count; t++)
                    if (targets[t] != null) targets[t].transform.position += Vector3.up * Mathf.Sin(Time.time * 2f + t) * 0.003f;
                yield return null;
            }

            Object.Destroy(root);
            if (_score >= 6)
            {
                Grant("item_carnival_ticket", 2);
                Complete();
            }
            else if (State == ActivityState.Running) Fail("Time is up!");
        }
    }
}
