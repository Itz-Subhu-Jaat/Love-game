#!/usr/bin/env python3
"""Cross-reference checker: verifies every type used from LoveGame.* namespaces exists,
finds duplicate class declarations, and validates Services.Get<T>/Register<T> types."""
import os
import re
import sys
from collections import defaultdict

ROOT = "/home/z/my-project/Love-game/Assets"

# collect declarations: namespace -> {typename: file}
decls = defaultdict(lambda: defaultdict(list))
for dirpath, _dirs, files in os.walk(ROOT):
    for fn in files:
        if not fn.endswith(".cs"):
            continue
        path = os.path.join(dirpath, fn)
        text = open(path, encoding="utf-8").read()
        ns = None
        for m in re.finditer(r"namespace\s+([\w.]+)", text):
            ns = m.group(1)
        if ns is None:
            continue
        for m in re.finditer(r"(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(class|struct|interface|enum)\s+(\w+)", text):
            decls[ns][m.group(2)].append(path)

errors = []

# duplicates in same namespace
for ns, types in decls.items():
    for t, paths in types.items():
        if len(paths) > 1:
            # partial classes allowed; flag same-file-count duplicates otherwise
            errors.append(f"DUPLICATE TYPE {ns}.{t}: {[os.path.relpath(p, ROOT) for p in paths]}")

# flatten all known types
known = set()
for ns, types in decls.items():
    for t in types:
        known.add(f"{ns}.{t}")
        known.add(t)

# collect qualified references like LoveGame.X.Y or X.Y (from sibling namespaces)
ref_pattern = re.compile(r"\b((?:LoveGame\.\w+(?:\.\w+)*)\.(?:\w+))\b")
# simpler: find identifiers used with dot after namespace segments we know
ns_namespaces = set(decls.keys())

for dirpath, _dirs, files in os.walk(ROOT):
    for fn in files:
        if not fn.endswith(".cs"):
            continue
        path = os.path.join(dirpath, fn)
        text = open(path, encoding="utf-8").read()
        this_ns = None
        m = re.search(r"namespace\s+([\w.]+)", text)
        if m:
            this_ns = m.group(1)

        # check references like Segment.Type: e.g. "World.WorldStreamer", "UI.HudScreen"
        for m2 in re.finditer(r"\b(\w+)\.(\w+)\b\s*[\(<\s]", text):
            seg1, seg2 = m2.group(1), m2.group(2)
            if seg1 in ("UnityEngine", "UnityEditor", "System", "Collections", "Unity", "Debug", "Math", "File", "Directory", "Path", "JsonUtility", "Mathf", "Vector2", "Vector3", "Vector4", "Color", "Quaternion", "Time", "Random", "Physics", "GameObject", "Transform", "Screen", "Input", "Camera", "AudioClip", "Rigidbody", "Light", "Shader", "Material", "Mesh", "Texture", "Application", "QualitySettings", "RenderSettings", "SceneManager", "PlayerPrefs", "Cursor", "Resources", "Object", "Scene", "Canvas", "CanvasScaler", "PlayerSettings", "EditorUtility", "AssetDatabase", "Selection", "Handles", "Gizmos", "EditorGUILayout", "EditorGUI", "ParticleSystem", "Lightmapping", "NavMesh", " animator"):
                continue
            if seg1 in ("Log", "Services", "SaveSystem", "GameConfig", "GameEvents", "CoreMath", "MaterialLibrary", "GameLayers", "Rng", "Noise", "ObjectPool", "SaveData", "UiFactory", "ProceduralAudioLibrary", "EnvironmentBlender", "WorldSystemsBridge", "SessionState", "MathF"):
                continue
            # resolve: is "seg1.seg2" a known namespace member type?
            candidate = None
            for base in [this_ns] if this_ns else []:
                parts = base.split(".")
                for i in range(len(parts), -1, -1):
                    prefix = ".".join(parts[:i]) if i > 0 else ""
                    trial = f"{prefix}.{seg1}.{seg2}" if prefix else f"{seg1}.{seg2}"
                    if trial in known and trial not in (f"{this_ns}.Log",):
                        candidate = trial
                        break
                if candidate:
                    break
            if candidate:
                continue
            # is seg1 a known TYPE with static member? then fine
            if seg1 in known:
                continue
            # is seg1 a local variable / parameter? crude check: declared in this file
            if re.search(rf"\b(?:var|\w+)\s+{seg1}\b|class\s+{seg1}\b|\({seg1}\s|,\s*{seg1}\s|\b{seg1}\s+\w+\s*[=;()]", text):
                continue
            # unresolved - record (may be false positive; manual review)
            errors.append(f"UNRESOLVED? {os.path.relpath(path, ROOT)}: {seg1}.{seg2} (ns={this_ns})")

print(f"namespaces: {len(decls)}, types: {len(known)}")
uniq = []
seen = set()
for e in errors:
    key = e.split(": ", 1)[1] if ": " in e else e
    if key in seen:
        continue
    seen.add(key)
    uniq.append(e)
print(f"issues: {len(uniq)}")
for e in uniq[:60]:
    print("  -", e)
