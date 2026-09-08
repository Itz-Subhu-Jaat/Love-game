#if UNITY_EDITOR
using System.IO;
using LoveGame.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LoveGame.EditorTools
{
    /// <summary>
    /// One-click (and automatic-on-load) project configuration:
    /// - creates URP pipeline + renderer assets with mobile-friendly settings per quality level
    /// - assigns GraphicsSettings/QualitySettings (4 levels: Low/Medium/High/Ultra)
    /// - creates Lit/Particle material assets under Resources/Materials so runtime-created
    ///   visuals are guaranteed shader-included in builds
    /// - configures Android player settings (IL2CPP/ARM64, orientation, bundle id)
    /// Idempotent: running twice converges to the same result.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetupWizard
    {
        const string SettingsFolder = "Assets/Settings";
        const string MaterialsFolder = "Assets/Resources/Materials";

        static ProjectSetupWizard()
        {
            // deferred so the AssetDatabase is ready after domain reload
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("LoveGame.SetupRan", false))
                {
                    SessionState.SetBool("LoveGame.SetupRan", true);
                    try { Run(); }
                    catch (System.Exception e) { Debug.LogError($"[LoveGame Setup] {e.Message}\n{e.StackTrace}"); }
                }
            };
        }

        [MenuItem("Love Game/Run Project Setup")]
        public static void RunFromMenu() => Run();

        public static void Run()
        {
            Directory.CreateDirectory(SettingsFolder);
            Directory.CreateDirectory(MaterialsFolder);

            EnsureUrPConfiguration();
            EnsureRuntimeMaterials();
            ConfigurePlayerSettings();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[LoveGame Setup] project configured (URP + quality levels + runtime materials)");
        }

        // ---------------------------------------------------------------- URP

        static void EnsureUrPConfiguration()
        {
            var renderer = LoadOrCreateRenderer();
            var names = new[] { "Low", "Medium", "High", "Ultra" };
            var renderScales = new[] { 0.7f, 0.8f, 1.0f, 1.0f };
            var msaa = new[] { 0, 2, 2, 4 };
            var shadowDistances = new[] { 60f, 100f, 150f, 200f };
            var hq = new[] { false, false, true, true };

            QualitySettings.SetQualityLevel(1, false);
            for (int i = 0; i < 4; i++)
            {
                var assetPath = $"{SettingsFolder}/URP_{names[i]}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                ConfigurePipelineAsset(asset, renderer, renderScales[i], msaa[i], shadowDistances[i], hq[i]);
                EditorUtility.SetDirty(asset);
            }

            // assign quality-level render pipeline assets (QualitySettings API)
            for (int i = 0; i < 4; i++)
            {
                var assetPath = $"{SettingsFolder}/URP_{names[i]}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
                QualitySettings.SetQualityLevel(i, false);
                so_setRenderPipeline(i, asset);
            }

            // default pipeline for the project
            var defaultAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>($"{SettingsFolder}/URP_Medium.asset");
            if (defaultAsset != null) GraphicsSettings.defaultRenderPipeline = defaultAsset;

            // quality names
            ApplyQualityNames(names);
        }

        static void ApplyQualityNames(string[] names)
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = so.FindProperty("m_QualitySettings");
            for (int i = 0; i < names.Length && i < levels.arraySize; i++)
            {
                var entry = levels.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("name").stringValue = names[i];
                entry.FindPropertyRelative("pixelLightCount").intValue = i < 2 ? 2 : 4;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void so_setRenderPipeline(int level, UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = so.FindProperty("m_QualitySettings");
            if (level < levels.arraySize)
            {
                var entry = levels.GetArrayElementAtIndex(level);
                var prop = entry.FindPropertyRelative("customRenderPipeline");
                prop.objectReferenceValue = asset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static UniversalRendererData LoadOrCreateRenderer()
        {
            const string path = $"{SettingsFolder}/URP_Renderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (renderer != null) return renderer;
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, path);
            return renderer;
        }

        static void ConfigurePipelineAsset(UniversalRenderPipelineAsset asset, UniversalRendererData renderer,
            float renderScale, int msaa, float shadowDistance, bool highQuality)
        {
            var so = new SerializedObject(asset);
            // renderer list
            var renderers = so.FindProperty("m_RendererDataList");
            if (renderers != null)
            {
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }
            SetFloat(so, "m_RenderScale", renderScale);
            SetInt(so, "m_MSAA", msaa);
            SetBool(so, "m_SupportsHDR", highQuality);
            SetInt(so, "m_MainLightRenderingMode", 1);           // per-pixel
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            SetInt(so, "m_AdditionalLightsRenderingMode", 0);    // disabled (mobile)
            SetFloat(so, "m_ShadowDistance", shadowDistance);
            SetInt(so, "m_ShadowCascadeCount", 1);
            SetBool(so, "m_PostProcessing", true);
            SetInt(so, "m_ColorGradingMode", highQuality ? 1 : 0); // HDR/Linear
            SetFloat(so, "m_Exposure", 1f);
            SetInt(so, "m_AntialiasingMode", 0);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(SerializedObject so, string field, float value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.floatValue = value;
            else Debug.LogWarning($"[LoveGame Setup] URP field missing: {field} (version drift - using default)");
        }

        static void SetInt(SerializedObject so, string field, int value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.intValue = value;
            else Debug.LogWarning($"[LoveGame Setup] URP field missing: {field} (version drift - using default)");
        }

        static void SetBool(SerializedObject so, string field, bool value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.boolValue = value;
            else Debug.LogWarning($"[LoveGame Setup] URP field missing: {field} (version drift - using default)");
        }

        // ---------------------------------------------------- runtime materials

        static void EnsureRuntimeMaterials()
        {
            EnsureMaterial($"{MaterialsFolder}/Lit.mat", "Universal Render Pipeline/Lit", "Standard");
            EnsureMaterial($"{MaterialsFolder}/Particle.mat", "Universal Render Pipeline/Particles/Unlit", "Particles/Standard Unlit");
        }

        static void EnsureMaterial(string path, string urpShader, string fallbackShader)
        {
            if (File.Exists(path)) return;
            var shader = Shader.Find(urpShader);
            if (shader == null) shader = Shader.Find(fallbackShader);
            if (shader == null)
            {
                Debug.LogWarning($"[LoveGame Setup] shader not found: {urpShader}");
                return;
            }
            var mat = new Material(shader);
            mat.name = Path.GetFileNameWithoutExtension(path);
            if (shader.name.Contains("Particle"))
            {
                mat.SetFloat("_Mode", 2); // legacy fade
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            AssetDatabase.CreateAsset(mat, path);
        }

        // ---------------------------------------------------- player settings

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "LoveGame Studio";
            PlayerSettings.productName = "Love Game";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lovegamestudio.lovegame");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.Arm64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.graphicsJobs = false;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.accelerometerFrequency = 0; // battery friendly
        }

        static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Scenes/00_Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/_Scenes/01_MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Scenes/02_World.unity", true),
            };
        }
    }
}
#endif
