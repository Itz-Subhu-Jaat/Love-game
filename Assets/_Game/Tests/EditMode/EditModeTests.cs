using System.Collections.Generic;
using System.Linq;
using LoveGame.Core;
using LoveGame.World;
using NUnit.Framework;
using UnityEngine;

namespace LoveGame.Tests.EditMode
{
    public class RegionCatalogTests
    {
        [Test]
        public void Catalog_Loads_All_24_Regions()
        {
            var catalog = new RegionCatalogService();
            catalog.Load();
            Assert.GreaterOrEqual(catalog.Count, 24, "the initial world must ship all 24 regions");
        }

        [Test]
        public void Catalog_Has_Unique_Ids()
        {
            var catalog = new RegionCatalogService();
            catalog.Load();
            var ids = new HashSet<string>(catalog.Regions.Select(r => r.Id));
            Assert.AreEqual(catalog.Count, ids.Count, "region ids must be unique");
        }

        [Test]
        public void Catalog_Validation_Passes()
        {
            var catalog = new RegionCatalogService();
            catalog.Load();
            var errors = catalog.Validate();
            Assert.IsEmpty(errors, "validation errors: " + string.Join("; ", errors));
        }

        [Test]
        public void Every_Region_Has_Meaningful_Content()
        {
            var catalog = new RegionCatalogService();
            catalog.Load();
            foreach (var region in catalog.Regions)
            {
                Assert.IsNotEmpty(region.Pois, $"{region.Id} must contain POIs (no empty regions)");
                Assert.Greater(region.Size, 300f, $"{region.Id} must be explorable");
                Assert.IsNotNull(region.Description, $"{region.Id} needs a description");
            }
        }

        [Test]
        public void Every_Region_Has_A_Spawn_And_FastTravel()
        {
            var catalog = new RegionCatalogService();
            catalog.Load();
            foreach (var region in catalog.Regions)
            {
                Assert.IsNotEmpty(region.FastTravel, $"{region.Id} must have at least one fast travel point");
            }
        }
    }

    public class DeterminismTests
    {
        [Test]
        public void Rng_Is_Deterministic_For_Same_Seed()
        {
            var a = new Rng(1234);
            var b = new Rng(1234);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextInt(), b.NextInt(), "same seed must produce identical sequences");
        }

        [Test]
        public void Noise_Is_Deterministic()
        {
            var a = new Noise(77);
            var b = new Noise(77);
            for (int i = 0; i < 200; i++)
            {
                float x = i * 0.37f, y = i * -1.13f;
                Assert.AreEqual(a.Sample(x, y), b.Sample(x, y), 1e-6f);
            }
        }

        [Test]
        public void Noise_Stays_In_Range()
        {
            var noise = new Noise(9);
            for (int i = 0; i < 500; i++)
            {
                float v = noise.Sample(i * 0.71f, i * 0.13f);
                Assert.That(v, Is.InRange(-1.2f, 1.2f), "simplex output should stay roughly in [-1,1]");
            }
        }

        [Test]
        public void Rng_Range_Respects_Bounds()
        {
            var rng = new Rng(55);
            for (int i = 0; i < 500; i++)
            {
                int v = rng.Range(3, 10);
                Assert.That(v, Is.InRange(3, 9));
            }
        }
    }

    public class MathUtilTests
    {
        [Test]
        public void Remap_Maps_Correctly()
        {
            Assert.AreEqual(0.5f, CoreMath.Remap(5f, 0f, 10f, 0f, 1f), 1e-5f);
            Assert.AreEqual(20f, CoreMath.Remap(2f, 0f, 10f, 0f, 100f), 1e-5f);
        }

        [Test]
        public void HexToColor_Parses_And_Falls_Back()
        {
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), CoreMath.HexToColor("#FF0000"));
            Assert.AreEqual(Color.white, CoreMath.HexToColor("not-a-color"));
        }

        [Test]
        public void Damp_Converges()
        {
            float value = 0f;
            for (int i = 0; i < 600; i++) value = CoreMath.Damp(value, 1f, 0.001f, Time.fixedDeltaTime);
            Assert.Greater(value, 0.95f);
        }
    }
}
