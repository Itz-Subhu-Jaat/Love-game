using System.Collections;
using LoveGame.Core;
using UnityEngine;

namespace LoveGame.Activities
{
    /// <summary>
    /// Fishing: cast -> wait for bite (exclamation) -> tap to hook -> catch roll.
    /// Yields fish items used by gifts/cooking later. All state machine timing in one coroutine.
    /// </summary>
    public sealed class FishingActivity : ActivityBase
    {
        readonly World.WorldStreamer _world;
        public override string Id => "fishing";
        public override string DisplayName => "Fishing";
        Vector3 _spot;

        public FishingActivity(ActivityService service, World.WorldStreamer world)
        {
            _world = world;
            SetService(service);
        }

        protected override IEnumerator Run()
        {
            _spot = _world != null ? _world.PlayerPosition : Vector3.zero;
            ObjectiveText = "Wait for a bite...";
            GameEvents.Publish(new NotificationEvent { Title = "Fishing", Body = "Cast your line...", Duration = 2.5f });
            yield return new WaitForSeconds(1.2f);

            float waitTime = Random.Range(3f, 7f);
            float timer = 0f;
            while (timer < waitTime)
            {
                timer += Time.deltaTime;
                ObjectiveProgress = timer / waitTime;
                // leaving the spot cancels quietly
                if (_world != null && (_world.PlayerPosition - _spot).sqrMagnitude > 6f * 6f)
                {
                    Cancel();
                    yield break;
                }
                yield return null;
            }

            // bite window
            ObjectiveText = "A bite! Quick!";
            Services.TryGet<Audio.AudioService>(out var audio);
            audio?.QueueSfx(Audio.ProceduralAudioLibrary.FishSplash());
            float window = 1.6f;
            bool hooked = false;
            float windowTimer = 0f;
            while (windowTimer < window)
            {
                windowTimer += Time.deltaTime;
                var input = Services.Get<Player.InputService>();
                if (input != null && input.Active.ActionPressed) { hooked = true; break; }
                yield return null;
            }

            if (!hooked)
            {
                Fail("The fish got away...");
                yield break;
            }

            // catch roll
            yield return new WaitForSeconds(0.8f);
            string[] fish = { "fish_clownfish", "fish_bluefin", "fish_angelfish", "fish_moonfish" };
            var caught = fish[Random.Range(0, fish.Length)];
            Grant(caught, 1);
            ObjectiveText = $"Caught: {caught}";
            Complete();
        }
    }

    /// <summary>
    /// Racing: checkpoint circuit generated around the region center, lap timer,
    /// best time saved per region. Leaderboard-ready architecture via the timer event flow.
    /// </summary>
    public sealed class RacingActivity : ActivityBase
    {
        public override string Id => "racing";
        public override string DisplayName => "Sunset Circuit Race";

        readonly Vector3[] _checkpoints = new Vector3[4];
        int _nextCheckpoint;
        float _elapsed;
        float _bestTime = -1f;

        public float BestTime => _bestTime;

        protected override void OnStarted()
        {
            var streamer = Services.Get<World.WorldStreamer>();
            var region = Services.Get<World.RegionCatalogService>()?.ActiveRegion;
            if (region != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i / 4f * Mathf.PI * 2f;
                    var local = new Vector3(Mathf.Cos(a) * region.Size * 0.25f, 0f, Mathf.Sin(a) * region.Size * 0.25f);
                    var world = region.WorldCenter + local;
                    if (streamer != null) world.y = streamer.SampleHeight(world.x, world.z) + 1f;
                    _checkpoints[i] = world;
                }
            }
            _nextCheckpoint = 0;
            _elapsed = 0f;
        }

        protected override IEnumerator Run()
        {
            ObjectiveText = "Reach checkpoint 1/4";
            GameEvents.Publish(new NotificationEvent { Title = "Race started", Body = "Follow the checkpoints!", Duration = 3f });
            float timeout = 180f;

            while (State == ActivityState.Running && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                _elapsed += Time.deltaTime;

                var player = Services.Get<Player.InputService>();
                var streamer = Services.Get<World.WorldStreamer>();
                if (streamer == null) { Fail("world unavailable"); yield break; }

                var target = _checkpoints[_nextCheckpoint];
                ObjectiveProgress = _nextCheckpoint / 4f;
                ObjectiveText = $"Checkpoint {_nextCheckpoint + 1}/4 - {_elapsed:F1}s";

                if ((streamer.PlayerPosition - target).sqrMagnitude < 22f * 22f)
                {
                    _nextCheckpoint++;
                    Services.TryGet<Audio.AudioService>(out var audio);
                    audio?.QueueSfx(Audio.ProceduralAudioLibrary.CollectChime());
                    if (_nextCheckpoint >= 4)
                    {
                        if (_bestTime < 0f || _elapsed < _bestTime) _bestTime = _elapsed;
                        Grant("item_race_trophy", 1);
                        Complete();
                        yield break;
                    }
                }
                yield return null;
            }
            if (State == ActivityState.Running) Fail("Time ran out");
        }
    }
}
