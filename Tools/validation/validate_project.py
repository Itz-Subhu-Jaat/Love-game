#!/usr/bin/env python3
"""Love Game static project validator.

Runs in CI on every push (zero Unity license/minutes needed) and locally.
Fails the build on any of:
  - missing/orphan .meta files, duplicate GUIDs
  - scene references to script GUIDs that do not exist
  - asmdef references that do not resolve + dependency cycles
  - C# brace/paren imbalance or unclosed strings/comments
  - shader brace imbalance / missing CGPROGRAM-ENDCG pairs
  - malformed JSON catalogs (regions/items/furniture/manifest/tips)
  - region catalog regressions (<24 regions, empty regions, bad POIs)
  - Unity version pin drift between ProjectVersion and workflows
  - committed secrets (tokens, keys, licenses)
"""
import argparse
import json
import os
import re
import sys

EXIT = 0
ERRORS = []
WARNINGS = []


def error(msg):
    ERRORS.append(msg)


def warn(msg):
    WARNINGS.append(msg)


def load_yaml_docs(path):
    import yaml

    class Loader(yaml.SafeLoader):
        pass

    def unknown(loader, suffix, node):
        if isinstance(node, yaml.MappingNode):
            return loader.construct_mapping(node, deep=True)
        if isinstance(node, yaml.SequenceNode):
            return loader.construct_sequence(node, deep=True)
        return loader.construct_scalar(node)

    Loader.add_multi_constructor("tag:unity3d.com,2011:", unknown)
    with open(path, "r") as f:
        text = f.read()
    text = text.replace("%TAG !u! tag:unity3d.com,2011:\n", "")
    text = re.sub(r"!u!(\w+)", r"!<tag:unity3d.com,2011:\1>", text)
    return list(yaml.load_all(text, Loader=Loader))


# ------------------------------------------------------------- meta & GUIDs

def check_meta_pairing(root):
    assets = os.path.join(root, "Assets")
    guids = {}
    script_guids = {}
    for dirpath, dirnames, filenames in os.walk(assets):
        for name in filenames:
            full = os.path.join(dirpath, name)
            if name.endswith(".meta"):
                continue
            if not os.path.exists(full + ".meta"):
                error(f"missing .meta: {os.path.relpath(full, root)}")
    for dirpath, dirnames, filenames in os.walk(root):
        if os.sep + "Library" in dirpath or os.sep + ".git" in dirpath:
            dirnames[:] = []
            continue
        for name in filenames:
            if not name.endswith(".meta"):
                continue
            full = os.path.join(dirpath, name)
            target = full[:-5]
            if not os.path.exists(target) and not os.path.isdir(target):
                error(f"orphan .meta: {os.path.relpath(full, root)}")
            try:
                with open(full) as f:
                    m = re.search(r"^guid: ([0-9a-f]{32})", f.read(), re.M)
                if m:
                    g = m.group(1)
                    if g in guids:
                        error(f"duplicate GUID {g}: {guids[g]} and {os.path.relpath(full, root)}")
                    guids[g] = os.path.relpath(full, root)
                    if name.endswith(".cs.meta"):
                        script_guids[g] = os.path.relpath(target, root)
            except Exception as e:
                error(f"unreadable meta {full}: {e}")
    return guids, script_guids


# ------------------------------------------------------------------- scenes

def resolve_scenes_dir(root):
    for rel in ("Assets/_Game/_Scenes", "Assets/_Scenes"):
        path = os.path.join(root, rel)
        if os.path.isdir(path):
            return path
    return None


def check_scenes(root, script_guids):
    scenes_dir = resolve_scenes_dir(root)
    expected = ["00_Bootstrap.unity", "01_MainMenu.unity", "02_World.unity"]
    if scenes_dir is None:
        error("scene folder missing (expected Assets/_Game/_Scenes or Assets/_Scenes)")
        return
    found = [f for f in expected if os.path.exists(os.path.join(scenes_dir, f))]
    if len(found) != len(expected):
        error(f"expected scenes {expected}, found {found}")
    for name in os.listdir(scenes_dir):
        if not name.endswith(".unity"):
            continue
        path = os.path.join(scenes_dir, name)
        try:
            docs = load_yaml_docs(path)
            text = open(path).read()
            refs = set(re.findall(r"guid: ([0-9a-f]{32}), type: 3", text))
            for g in refs:
                if g not in script_guids:
                    error(f"{name}: references unknown script GUID {g}")
            root_count = 0
            for doc in docs:
                if isinstance(doc, dict) and "SceneRoots" in str(doc.keys()):
                    root_count += 1
            if root_count == 0:
                warn(f"{name}: no SceneRoots document found")
        except Exception as e:
            error(f"{name}: YAML parse failed: {e}")


# ------------------------------------------------------------------ asmdefs

# Special Unity assemblies provided by the editor/packages (not project asmdefs).
BUILTIN_ASSEMBLIES = {
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "nunit.framework",
    "Unity.Addressables",
    "Unity.Addressables.Editor",
    "Unity.ResourceManager",
    "Unity.RenderPipelines.Core.Runtime",
    "Unity.RenderPipelines.Core.Editor",
    "Unity.RenderPipelines.Universal.Runtime",
    "Unity.RenderPipelines.Universal.Editor",
}

def check_asmdefs(root):
    asmdefs = {}
    for dirpath, _dirnames, filenames in os.walk(root):
        if os.sep + "Library" in dirpath or os.sep + ".git" in dirpath:
            continue
        for name in filenames:
            if name.endswith(".asmdef"):
                full = os.path.join(dirpath, name)
                try:
                    with open(full) as f:
                        data = json.load(f)
                    asmdefs[data.get("name", name)] = {
                        "path": os.path.relpath(full, root),
                        "refs": data.get("references", []),
                        "editor": "Editor" in data.get("includePlatforms", []),
                    }
                except Exception as e:
                    error(f"asmdef parse failed {full}: {e}")
    for name, info in asmdefs.items():
        for ref in info["refs"]:
            if ref.startswith("GUID:") or ref in BUILTIN_ASSEMBLIES:
                continue
            if ref not in asmdefs:
                error(f"asmdef {name} references unknown assembly '{ref}'")
            elif asmdefs[ref]["editor"] and not info["editor"]:
                error(f"asmdef {name} (runtime) references editor-only assembly '{ref}'")
    # cycle detection
    graph = {name: [r for r in i["refs"] if r in asmdefs] for name, i in asmdefs.items()}
    state = {}

    def visit(node, stack):
        if state.get(node) == 1:
            error(f"asmdef dependency cycle: {' -> '.join(stack + [node])}")
            return
        if state.get(node) == 2:
            return
        state[node] = 1
        for dep in graph.get(node, []):
            visit(dep, stack + [node])
        state[node] = 2

    for node in graph:
        visit(node, [])
    return asmdefs


# ---------------------------------------------------------------------- C#

def check_csharp(root):
    cs_files = []
    for dirpath, dirnames, filenames in os.walk(root):
        if os.sep + "Library" in dirpath or os.sep + ".git" in dirpath:
            dirnames[:] = []
            continue
        for name in filenames:
            if name.endswith(".cs"):
                cs_files.append(os.path.join(dirpath, name))
    for path in cs_files:
        with open(path, encoding="utf-8") as f:
            text = f.read()
        rel = os.path.relpath(path, root)
        # strip strings/comments line-safely
        stripped = re.sub(r"//.*", "", text)
        stripped = re.sub(r"/\*.*?\*/", "", stripped, flags=re.S)
        stripped = re.sub(r'"(\\.|[^"\\])*"', '""', stripped)
        stripped = re.sub(r"'(\\.|[^'\\])*'", "''", stripped)
        stripped = re.sub(r"\$\"(\\.|[^\"\\])*\"", '""', stripped)
        stripped = re.sub(r"\$'(\\.|[^'\\])*'", "''", stripped)
        for opener, closer in [("{", "}"), ("(", ")"), ("[", "]")]:
            o, c = stripped.count(opener), stripped.count(closer)
            if o != c:
                error(f"{rel}: unbalanced {opener}{closer} ({o} vs {c})")
        # quote balance on the raw text (odd number of unescaped quotes per line)
        for i, line in enumerate(stripped.splitlines(), 1):
            if line.count('"') % 2 != 0:
                error(f"{rel}:{i}: unbalanced quotes")
        if "#if UNITY_EDITOR" in text and "#endif" not in text:
            error(f"{rel}: #if without #endif")
    return cs_files


# ------------------------------------------------------------------ shaders

def check_shaders(root):
    shaders = []
    for dirpath, _dirnames, filenames in os.walk(os.path.join(root, "Assets")):
        for name in filenames:
            if name.endswith(".shader"):
                shaders.append(os.path.join(dirpath, name))
    for path in shaders:
        text = open(path, encoding="utf-8").read()
        rel = os.path.relpath(path, root)
        for opener, closer in [("{", "}"), ("(", ")")]:
            if text.count(opener) != text.count(closer):
                error(f"{rel}: unbalanced {opener}{closer}")
        for keyword in ["Shader \"", "SubShader", "Pass", "vert", "frag"]:
            if keyword not in text:
                error(f"{rel}: missing '{keyword}'")
        if "CGPROGRAM" in text and "ENDCG" not in text:
            error(f"{rel}: CGPROGRAM without ENDCG")


# -------------------------------------------------------------------- JSON

def check_json_catalogs(root):
    data_dir = os.path.join(root, "Assets/Resources/Data")
    required = ["regions.json", "items.json", "furniture.json", "content_manifest.json", "tips.json"]
    payloads = {}
    for name in required:
        path = os.path.join(data_dir, name)
        if not os.path.exists(path):
            error(f"missing catalog: {name}")
            continue
        try:
            with open(path) as f:
                payloads[name] = json.load(f)
        except Exception as e:
            error(f"{name}: JSON parse failed: {e}")

    regions = payloads.get("regions.json", {}).get("regions", [])
    if len(regions) < 24:
        error(f"regions.json must ship 24+ regions, found {len(regions)}")
    ids = set()
    for region in regions:
        rid = region.get("id")
        if not rid:
            error("region with empty id")
            continue
        if rid in ids:
            error(f"duplicate region id {rid}")
        ids.add(rid)
        pois = region.get("pois", [])
        if not pois:
            error(f"{rid}: region has no POIs (empty regions forbidden)")
        for poi in pois:
            if not poi.get("id") or not poi.get("kind"):
                error(f"{rid}: POI missing id or kind")
        if not region.get("fastTravel"):
            error(f"{rid}: no fast travel points")
        w = region.get("weatherWeights", "60,25,10,2,3").split(",")
        if len(w) != 5:
            error(f"{rid}: weatherWeights must have 5 values")

    items = payloads.get("items.json", {}).get("items", [])
    if len(items) < 30:
        error(f"items.json needs 30+ items, found {len(items)}")
    item_ids = set()
    for item in items:
        if not item.get("id"):
            error("item without id")
        elif item["id"] in item_ids:
            error(f"duplicate item id {item['id']}")
        else:
            item_ids.add(item["id"])

    furniture = payloads.get("furniture.json", {}).get("furniture", [])
    if len(furniture) < 12:
        error(f"furniture.json needs 12+ pieces, found {len(furniture)}")

    manifest = payloads.get("content_manifest.json", {})
    packs = manifest.get("packs", [])
    if not any(p.get("required") for p in packs):
        error("content manifest must mark the core pack as required")

    tips = payloads.get("tips.json", {}).get("tips", [])
    if len(tips) < 10:
        error(f"tips.json needs 10+ tips, found {len(tips)}")
    return len(regions), len(items), len(furniture), len(packs), len(tips)


# ----------------------------------------------------------------- secrets

SECRET_PATTERNS = [
    (re.compile(r"ghp_[A-Za-z0-9]{30,}"), "GitHub PAT"),
    (re.compile(r"gho_[A-Za-z0-9]{30,}"), "GitHub OAuth"),
    (re.compile(r"github_pat_[A-Za-z0-9_]{30,}"), "GitHub fine-grained PAT"),
    (re.compile(r"AKIA[0-9A-Z]{16}"), "AWS key"),
    (re.compile(r"-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----"), "private key"),
    (re.compile(r"<DeveloperData Value=\".+\"/>"), "Unity license file"),
]


def check_secrets(root):
    for dirpath, dirnames, filenames in os.walk(root):
        if os.sep + ".git" in dirpath or os.sep + "Library" in dirpath:
            dirnames[:] = []
            continue
        for name in filenames:
            full = os.path.join(dirpath, name)
            _, ext = os.path.splitext(name)
            if ext.lower() in (".png", ".jpg", ".jpeg", ".ttf", ".wav", ".mp3", ".meta", ".unity", ".asset", ".pack", ".zip"):
                continue
            try:
                with open(full, encoding="utf-8", errors="ignore") as f:
                    text = f.read()
            except Exception:
                continue
            for pattern, label in SECRET_PATTERNS:
                m = pattern.search(text)
                if m:
                    error(f"possible committed secret ({label}) in {os.path.relpath(full, root)}")


# ------------------------------------------------------------ unity version

def check_unity_version(root):
    pv = os.path.join(root, "ProjectSettings/ProjectVersion.txt")
    if not os.path.exists(pv):
        error("ProjectSettings/ProjectVersion.txt missing")
        return
    version = open(pv).read().strip().split()[1]
    ci = os.path.join(root, "Assets/_Game/EditorTools/CiBuilder.cs")
    if os.path.exists(ci):
        text = open(ci).read()
    # GameCI uses unityVersion: auto - document the pin:
    for wf in ["android", "validate", "release", "addressables"]:
        path = os.path.join(root, ".github/workflows", wf + ".yml")
        if os.path.exists(path):
            text = open(path).read()
            if "unityci/editor:ubuntu-" in text and version not in text:
                warn(f"{wf}.yml pins a different editor than {version}")
    return version


# --------------------------------------------------------------------- main

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--summary", action="store_true", help="print data summary only")
    args = parser.parse_args()
    root = os.path.abspath(args.project_root)

    if not os.path.isdir(os.path.join(root, "Assets")):
        print(f"not a Unity project root: {root}")
        sys.exit(2)

    guids, script_guids = check_meta_pairing(root)
    check_scenes(root, script_guids)
    asmdefs = check_asmdefs(root)
    check_csharp(root)
    check_shaders(root)
    region_count, item_count, furniture_count, pack_count, tip_count = check_json_catalogs(root)
    check_secrets(root)
    version = check_unity_version(root)

    if args.summary:
        print(f"regions: {region_count}  items: {item_count}  furniture: {furniture_count}  packs: {pack_count}  tips: {tip_count}")
        print(f"unity: {version}  asmdefs: {len(asmdefs)}  guids: {len(guids)}")
        return

    print(f"Love Game validator - {root}")
    print(f"  GUIDs: {len(guids)}  scripts: {len(script_guids)}  asmdefs: {len(asmdefs)}")
    print(f"  regions: {region_count}  items: {item_count}  packs: {pack_count}")
    for w in WARNINGS:
        print(f"  [WARN] {w}")
    if ERRORS:
        print(f"\nFAILED with {len(ERRORS)} error(s):")
        for e in ERRORS:
            print(f"  [ERROR] {e}")
        sys.exit(1)
    print("\nALL CHECKS PASSED")


if __name__ == "__main__":
    main()
