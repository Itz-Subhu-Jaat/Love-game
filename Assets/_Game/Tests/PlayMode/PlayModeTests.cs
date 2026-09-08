using System.Collections;
using LoveGame.Core;
using LoveGame.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoveGame.Tests.PlayMode
{
    /// <summary>Runtime behaviour tests. These validate real component behaviour, not mocks.</summary>
    public class PlayerTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            SaveSystem.Provider = new LocalSaveProvider();
        }

        [UnityTest]
        public IEnumerator Controller_Falls_And_Lands()
        {
            _go = new GameObject("player");
            _go.transform.position = new Vector3(0f, 5f, 0f);
            var controller = _go.AddComponent<ThirdPersonController>();
            var input = new KeyboardInputSource();
            controller.Bind(input, new FlatWorldQuery());
            controller.AllowControl = true;

            // wait for gravity to pull it onto the test floor
            float timeout = Time.realtimeSinceStartup + 5f;
            while (!controller.IsGrounded && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            Assert.IsTrue(controller.IsGrounded, "controller must land on the ground");
        }

        [UnityTest]
        public IEnumerator Controller_Detects_Water_And_Swims()
        {
            _go = new GameObject("swimmer");
            _go.transform.position = new Vector3(0f, -0.4f, 0f);
            var controller = _go.AddComponent<ThirdPersonController>();
            controller.Bind(new KeyboardInputSource(), new WaterWorldQuery());
            yield return null;
            yield return null;
            Assert.IsTrue(controller.IsSwimming, "body under a water surface must enter swimming state");
        }
    }

    public class CharacterTests
    {
        [UnityTest]
        public IEnumerator Character_Rig_Builds_With_Slots()
        {
            var go = new GameObject("char");
            var character = go.AddComponent<PlayerCharacter>();
            yield return null;
            Assert.NotNull(character.BodySlot, "modular body slot must exist");
            Assert.NotNull(character.HeadSlot, "modular head slot must exist");
            Assert.NotNull(character.RightHandSlot, "hand slot must exist for future accessories");
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Poses_Resolve_And_Play()
        {
            var go = new GameObject("char");
            var character = go.AddComponent<PlayerCharacter>();
            yield return null;
            Assert.IsTrue(character.HasPose("hug"), "couple interactions need the hug pose");
            Assert.IsTrue(character.HasPose("kiss"));
            Assert.IsTrue(character.HasPose("dance"));
            Assert.IsTrue(character.HasPose("sit"));
            character.PlayPose("dance");
            Assert.IsTrue(character.InPose);
            character.ClearPose();
            Assert.IsFalse(character.InPose);
            Object.Destroy(go);
        }
    }

    public class DayNightTests
    {
        [UnityTest]
        public IEnumerator Time_Advances_And_Publishes_Hours()
        {
            var service = new World.DayNightCycle { DayLengthMinutes = 0.1f };
            service.Initialize();
            int received = 0;
            GameEvents.Subscribe<TimeOfDayChangedEvent>(e => received++);
            service.Hour = 10.9f;
            for (int i = 0; i < 30; i++) { service.Tick(0.05f); yield return null; }
            Assert.Greater(received, 0, "hour changes must be published");
            Assert.Greater(service.Hour, 10.9f, "time must advance");
            GameEvents.Clear();
            service.Shutdown();
        }
    }

    public class VehicleTests
    {
        [UnityTest]
        public IEnumerator Vehicle_Enters_And_Exits()
        {
            var vehicleGo = new GameObject("car");
            var rb = vehicleGo.AddComponent<Rigidbody>();
            var vehicle = vehicleGo.AddComponent<LoveGame.Vehicles.VehicleBase>();
            var seats = new GameObject("seats");
            seats.transform.SetParent(vehicleGo.transform, false);
            var driver = new GameObject("driver");
            driver.transform.SetParent(seats.transform, false);
            Assert.NotNull(rb);

            var playerGo = new GameObject("p");
            var controller = playerGo.AddComponent<ThirdPersonController>();
            var cameraGo = new GameObject("cam");
            var camera = cameraGo.AddComponent<ThirdPersonCamera>();
            camera.SetTarget(playerGo.transform, true);

            vehicle.Bind(new KeyboardInputSource(), new FlatWorldQuery());
            vehicle.Enter(controller);
            Assert.IsTrue(vehicle.IsOccupied, "entering must mark the vehicle occupied");
            Assert.IsFalse(controller.AllowControl, "driver control transfers to the vehicle");
            vehicle.Exit(controller, camera);
            Assert.IsFalse(vehicle.IsOccupied);
            Assert.IsTrue(controller.AllowControl, "exiting restores player control");
            Object.Destroy(vehicleGo);
            Object.Destroy(playerGo);
            Object.Destroy(cameraGo);
            yield return null;
        }
    }

    // --------------------------------------------------------- test doubles

    /// <summary>Flat world at y=0 - deterministic ground for physics tests.</summary>
    public sealed class FlatWorldQuery : IWorldQuery
    {
        public float SampleHeight(float x, float z) => 0f;
        public bool IsWaterAt(Vector3 pos) => false;
        public float WaterSurfaceAt(float x, float z) => float.NaN;
        public string RegionIdAt(Vector3 pos) => "test";
    }

    /// <summary>Water surface at y=0 - swimming test world.</summary>
    public sealed class WaterWorldQuery : IWorldQuery
    {
        public float SampleHeight(float x, float z) => -5f;
        public bool IsWaterAt(Vector3 pos) => true;
        public float WaterSurfaceAt(float x, float z) => 0f;
        public string RegionIdAt(Vector3 pos) => "test";
    }
}
