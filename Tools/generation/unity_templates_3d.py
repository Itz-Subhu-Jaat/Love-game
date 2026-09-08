#!/usr/bin/env python3
"""YAML templates for the Love Game 3D Unity project (Unity 6000.0.83f1, URP)."""

UNITY_VERSION = "6000.0.83f1"

# ---------------------------------------------------------------- meta files

META_FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_SHADER = """fileFormatVersion: 2
guid: {guid}
ShaderImporter:
  externalObjects: []
  defaultTextures: []
  nonModifiableTextures: []
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_TEXT = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_DEFAULT = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

META_TEXTURE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  cubemapConvolutionSteps: 7
  cubemapConvolutionExponent: 1.5
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateSlicePhysics: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRowsAndColumns: {{x: 1, y: 1}}
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2Fallback: {{fileID: 0}}
  - serializedVersion: 3
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: 34
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2Fallback: {{fileID: 0}}
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    linkedTextures: []
  spritePackingTag:
  pSDRemoveMatte: 0
  pSDShowRemoveMatteTodo: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

# ---------------------------------------------------------------- scene file
# 3D scene: perspective camera (skybox), optional directional light, EventSystem created at runtime.
# Scene-referenced boot scripts live in asmdefs - referenced by GUID.

SCENE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneVisibilityIndex: 0
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 0
  m_FogColor: {{r: 0.5, g: 0.7, b: 0.85, a: 1}}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {{r: 0.55, g: 0.68, b: 0.78, a: 1}}
  m_AmbientEquatorColor: {{r: 0.5, g: 0.55, b: 0.6, a: 1}}
  m_AmbientGroundColor: {{r: 0.35, g: 0.38, b: 0.42, a: 1}}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
  m_SubtractiveShadowColor: {{r: 0.42, g: 0.48, b: 1, a: 1}}
  m_SkyboxMaterial: {{fileID: 0}}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {{fileID: 0}}
  m_SpotCookie: {{fileID: 0}}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {{fileID: 0}}
  m_Sun: {{fileID: 0}}
  m_IndirectSpecularColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 1
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOSamples: 5
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {{fileID: 0}}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherFiltering: 1
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVRCFiltering: 0
    m_PVRCFilteringMode: 1
    m_PVRCResolution: 0
    m_VectorsTickEveryNFrame: 0
  m_LightingDataAsset: {{fileID: 0}}
  m_LightingSettings: {{fileID: 0}}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {{fileID: 0}}
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 110000}}
  - component: {{fileID: 120000}}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {{fileID: 0}}
  m_IsActive: 1
--- !u!4 &110000
Transform:
  m_ObjectHideFlags: 0
  m_GameObject: {{fileID: 100000}}
  m_LocalRotation: {{x: 0.0871557, y: 0, z: 0, w: 0.9961947}}
  m_LocalPosition: {{x: 0, y: 8, z: -12}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 10, y: 0, z: 0}}
--- !u!20 &120000
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {{r: 0.25, g: 0.6, b: 0.85, a: 1}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_FocalLength: 50
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 3000
  field of view: 58
  orthographic: 0
  orthographic size: 10
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!1 &300000
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 310000}}
  - component: {{fileID: 1140000}}
  m_Layer: 0
  m_Name: {boot_go}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_IsActive: 1
--- !u!4 &310000
Transform:
  m_ObjectHideFlags: 0
  m_GameObject: {{fileID: 300000}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 2
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &1140000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 300000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {boot_guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
"""

# Optional directional light block appended for the world scene.
SCENE_LIGHT = """--- !u!1 &200000
GameObject:
  m_ObjectHideFlags: 0
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 210000}}
  - component: {{fileID: 220000}}
  m_Layer: 0
  m_Name: Directional Light
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_IsActive: 1
--- !u!4 &210000
Transform:
  m_ObjectHideFlags: 0
  m_GameObject: {{fileID: 200000}}
  m_LocalRotation: {{x: 0.40821788, y: 0.23456968, z: -0.10938163, w: 0.87542597}}
  m_LocalPosition: {{x: 0, y: 30, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 1
  m_LocalEulerAnglesHint: {{x: 48, y: 30, z: 0}}
--- !u!108 &220000
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  m_Enabled: 1
  serializedVersion: 11
  m_Type: 1
  m_Shape: 0
  m_Color: {{r: 1, g: 0.95, b: 0.84, a: 1}}
  m_Intensity: 1.1
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.80208
  m_CookieSize: 10
  m_Shadows:
    m_Type: 1
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrix:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
  m_Cookie: {{fileID: 0}}
  m_DrawHalo: 0
  m_Flare: {{fileID: 0}}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {{x: 1, y: 1}}
  m_BounceIntensity: 0
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {{x: 0, y: 0, z: 0, w: 0}}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_FadeDistance: 10000
  m_ShadowFadeDistance: 180
  m_VolumeFramework: 1
  m_ShadowRadius: 0
  m_ShadowAngle: 0
--- !u!1660057549 &990000
SceneRoots:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Roots:
  - {{fileID: 100000}}
  - {{fileID: 200000}}
  - {{fileID: 300000}}
"""

SCENE_ROOTS_NO_LIGHT = """--- !u!1660057549 &990000
SceneRoots:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Roots:
  - {{fileID: 100000}}
  - {{fileID: 300000}}
"""

# -------------------------------------------------------- ProjectSettings

PROJECT_SETTINGS = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 26
  productGUID: {product_guid}
  companyName: LoveGame Studio
  productName: Love Game
  defaultScreenWidth: 1920
  defaultScreenHeight: 1080
  defaultScreenWidthWeb: 960
  defaultScreenHeightWeb: 600
  runInBackground: 0
  resizableWindow: 1
  fullscreenMode: 3
  usePlayerLog: 1
  activeInputHandler: 0
  forceSIMD: 0
  gpuSkinning: 1
  xboxPixelTextureFormatOverride: 7
"""

EDITOR_BUILD_SETTINGS = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/_Scenes/00_Bootstrap.unity
    guid: {s0}
  - enabled: 1
    path: Assets/_Scenes/01_MainMenu.unity
    guid: {s1}
  - enabled: 1
    path: Assets/_Scenes/02_World.unity
    guid: {s2}
"""

TAG_MANAGER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!92 &1
TagManager:
  serializedVersion: 3
  tags:
  - Player
  - Interactable
  - Vehicle
  - NPC
  - Wildlife
  - POI
  layers:
  - Default
  - TransparentFX
  - Ignore Raycast
  -
  - Water
  - UI
  -
  -
  - Player
  - Interactable
  - Vehicle
  - Prop
  - NPC
  - Wildlife
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  -
  m_SortingLayers:
  - name: Default
    uniqueID: 0
    locked: 0
"""

TIME_MANAGER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!5 &1
TimeManager:
  m_ObjectHideFlags: 0
  Fixed Timestep: 0.02
  Maximum Allowed Timestep: 0.33333334
  m_TimeScale: 1
  Maximum Particle Timestep: 0.03
"""

AUDIO_MANAGER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!11 &1
AudioManager:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Volume: 1
  Rolloff Scale: 1
  Doppler Factor: 1
  Default Speaker Mode: 2
  m_SamplingRate: 0
  m_DisableAudio: 0
  m_VirtualVoiceCount: 512
  m_RealVoiceCount: 32
"""

INPUT_MANAGER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!13 &1
InputManager:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Axes:
  - serializedVersion: 3
    m_Name: Horizontal
    descriptiveName:
    descriptiveNegativeName:
    negativeButton: left
    positiveButton: right
    altNegativeButton: a
    altPositiveButton: d
    gravity: 1000
    dead: 0.001
    sensitivity: 1000
    type: 0
    axis: 0
    joyNum: 0
  - serializedVersion: 3
    m_Name: Vertical
    descriptiveName:
    descriptiveNegativeName:
    negativeButton: down
    positiveButton: up
    altNegativeButton: s
    altPositiveButton: w
    gravity: 1000
    dead: 0.001
    sensitivity: 1000
    type: 0
    axis: 0
    joyNum: 0
  - serializedVersion: 3
    m_Name: Mouse X
    descriptiveName:
    descriptiveNegativeName:
    negativeButton:
    positiveButton:
    altNegativeButton:
    altPositiveButton:
    gravity: 0
    dead: 0
    sensitivity: 0.1
    type: 1
    axis: 0
    joyNum: 0
  - serializedVersion: 3
    m_Name: Mouse Y
    descriptiveName:
    descriptiveNegativeName:
    negativeButton:
    positiveButton:
    altNegativeButton:
    altPositiveButton:
    gravity: 0
    dead: 0
    sensitivity: 0.1
    type: 1
    axis: 1
    joyNum: 0
  - serializedVersion: 3
    m_Name: Submit
    descriptiveName:
    descriptiveNegativeName:
    negativeButton:
    positiveButton: return
    altNegativeButton:
    altPositiveButton: space
    gravity: 1000
    dead: 0.001
    sensitivity: 1000
    type: 0
    axis: 0
    joyNum: 0
  - serializedVersion: 3
    m_Name: Cancel
    descriptiveName:
    descriptiveNegativeName:
    negativeButton:
    positiveButton: escape
    altNegativeButton:
    altPositiveButton:
    gravity: 1000
    dead: 0.001
    sensitivity: 1000
    type: 0
    axis: 0
    joyNum: 0
"""

PHYSICS_MANAGER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!55 &1
PhysicsManager:
  m_ObjectHideFlags: 0
  serializedVersion: 14
  m_Gravity: {{x: 0, y: -9.81, z: 0}}
  m_DefaultMaterial: {{fileID: 0}}
  m_BounceThreshold: 2
  m_SleepThreshold: 0.005
  m_DefaultContactOffset: 0.01
  m_DefaultSolverIterations: 6
  m_DefaultSolverVelocityIterations: 1
  m_QueriesHitBackfaces: 0
  m_QueriesHitTriggers: 1
  m_EnableAdaptiveForce: 0
  m_ClothInterCollisionDistance: 0
  m_ClothInterCollisionStiffness: 0
  m_LayerCollisionMatrix: ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff
"""

GRAPHICS_SETTINGS = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!30 &1
GraphicsSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_CustomRenderPipeline: {{fileID: 0}}
  m_TransparencySortMode: 0
  m_TransparencySortAxis: {{x: 0, y: 1, z: 0}}
  m_DefaultRenderingPath: 1
  m_DefaultMobileRenderingPath: 1
  m_TierSettings: []
  m_LightmapStripping: 0
  m_FogStripping: 0
  m_InstancingStripping: 0
  m_LightmapKeepPlain: 1
  m_LightmapKeepDirCombined: 1
  m_LightmapKeepDynamicPlain: 1
  m_LightmapKeepDynamicDirCombined: 1
  m_LightmapKeepShadowMask: 1
  m_LightmapBakeType: 1
  m_LightmapsBakeMode: 1
  m_ShadowmaskMode: 0
  m_AlwaysIncludedShaders:
  - {{fileID: 10770, guid: 0000000000000000f000000000000000, type: 0}}
  - {{fileID: 10782, guid: 0000000000000000f000000000000000, type: 0}}
  m_PreloadedShaders: []
  m_SRPDefaultSettings:
    m_Settings: []
"""

QUALITY_SETTINGS = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!47 &1
QualitySettings:
  m_ObjectHideFlags: 0
  serializedVersion: 5
  m_CurrentQuality: 1
  m_QualitySettings:
  - serializedVersion: 3
    name: Low
    pixelLightCount: 0
    shadows: 0
    shadowResolution: 0
    shadowProjection: 1
    shadowCascades: 1
    shadowDistance: 60
    shadowNearPlaneOffset: 2
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.06666667, y: 0.2, z: 0.46666667}}
    shadowmaskMode: 0
    skinWeights: 1
    globalTextureMipmapLimit: 1
    textureMipmapLimitSettings: []
    anisotropicTextures: 0
    antiAliasing: 0
    softParticles: 0
    softVegetation: 0
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 0
    useHDR: 0
    useDetailNormalMap: 0
    quality: 0
    customRenderPipeline: {{fileID: 0}}
    exclusionFlags: 0
    vSyncCount: 0
    realtimeGICPUUsage: 0
    realtimeGIBakingCPUUsage: 0
  - serializedVersion: 3
    name: Medium
    pixelLightCount: 2
    shadows: 1
    shadowResolution: 1
    shadowProjection: 1
    shadowCascades: 1
    shadowDistance: 100
    shadowNearPlaneOffset: 2
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.06666667, y: 0.2, z: 0.46666667}}
    shadowmaskMode: 0
    skinWeights: 2
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 1
    antiAliasing: 0
    softParticles: 0
    softVegetation: 0
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 0
    useHDR: 1
    useDetailNormalMap: 0
    quality: 1
    customRenderPipeline: {{fileID: 0}}
    exclusionFlags: 0
    vSyncCount: 0
    realtimeGICPUUsage: 0
    realtimeGIBakingCPUUsage: 0
  - serializedVersion: 3
    name: High
    pixelLightCount: 4
    shadows: 2
    shadowResolution: 2
    shadowProjection: 1
    shadowCascades: 1
    shadowDistance: 150
    shadowNearPlaneOffset: 2
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.06666667, y: 0.2, z: 0.46666667}}
    shadowmaskMode: 0
    skinWeights: 2
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 1
    antiAliasing: 0
    softParticles: 1
    softVegetation: 1
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 1
    useHDR: 1
    useDetailNormalMap: 1
    quality: 2
    customRenderPipeline: {{fileID: 0}}
    exclusionFlags: 0
    vSyncCount: 0
    realtimeGICPUUsage: 0
    realtimeGIBakingCPUUsage: 0
  - serializedVersion: 3
    name: Ultra
    pixelLightCount: 4
    shadows: 2
    shadowResolution: 3
    shadowProjection: 1
    shadowCascades: 2
    shadowDistance: 200
    shadowNearPlaneOffset: 2
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.06666667, y: 0.2, z: 0.46666667}}
    shadowmaskMode: 0
    skinWeights: 4
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 2
    antiAliasing: 0
    softParticles: 1
    softVegetation: 1
    realtimeReflectionProbes: 1
    billboardsFaceCameraPosition: 1
    useHDR: 1
    useDetailNormalMap: 1
    quality: 3
    customRenderPipeline: {{fileID: 0}}
    exclusionFlags: 0
    vSyncCount: 0
    realtimeGICPUUsage: 0
    realtimeGIBakingCPUUsage: 0
  m_TextureMipmapLimitGroupNames: []
"""

MANIFEST = """{
  "dependencies": {
    "com.unity.render-pipelines.universal": "17.0.3",
    "com.unity.addressables": "2.11.1",
    "com.unity.test-framework": "1.4.6",
    "com.unity.ugui": "2.0.0",
    "com.unity.modules.accessibility": "1.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
"""

GITIGNORE = """# Unity generated folders
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]serSettings/
MemoryCaptures/
Recordings/

# Unity generated files
*.pidb.meta
*.pdb.meta
*.mdb.meta
*.sysinfo.txt
*.apk
*.aab
*.unitypackage
*.app
*.pack

# IDE
.vs/
.idea/
*.csproj
*.sln
*.user
*.userprefs
*.booproj
*.orig
*.tmp

# OS
.DS_Store
Thumbs.db

# CI build output (GameCI writes here)
/build/
"""

GITATTRIBUTES = """* text=auto
*.cs diff=csharp text
*.shader text
*.meta text
*.unity text
*.asset text
*.prefab text
*.asmdef text
*.json text
*.md text
*.yaml text
*.yml text
*.py text
*.sh text
*.png binary
*.jpg binary
*.jpeg binary
*.gif binary
*.wav binary
*.mp3 binary
*.ttf binary
*.zip binary
*.tar.gz binary
*.apk binary
"""
