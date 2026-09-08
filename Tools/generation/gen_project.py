#!/usr/bin/env python3
"""Love Game (3D) - Unity project file generator.

Walks Assets/, writes deterministic GUID .meta files for every folder/asset,
generates the three scene files (camera/light/boot script references),
ProjectSettings, Packages/manifest.json, .gitignore and .gitattributes.
Validates every Unity YAML file it produces.
"""
import os
import re
import sys
import uuid
import json

import yaml

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import unity_templates_3d as T

ROOT = "/home/z/my-project/Love-game"
ASSETS = os.path.join(ROOT, "Assets")
NS = uuid.UUID("b7f2d4e1-6a3c-4c5e-9a2b-8d4f1e7c3a02")  # new namespace for the 3D project
REGISTRY_PATH = "/home/z/my-project/scripts/guid_registry.json"

registry = {}

def guid_for(relpath):
    if relpath not in registry:
        registry[relpath] = uuid.uuid5(NS, relpath.replace(os.sep, "/")).hex
    return registry[relpath]

def write(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="\n") as f:
        f.write(content)

def meta_for(path, relpath):
    ext = os.path.splitext(path)[1].lower()
    g = guid_for(relpath)
    if ext == ".cs":
        return T.META_SCRIPT.format(guid=g)
    if ext == ".shader":
        return T.META_SHADER.format(guid=g)
    if ext in (".png", ".jpg", ".jpeg"):
        return T.META_TEXTURE.format(guid=g)
    if ext == ".json":
        return T.META_TEXT.format(guid=g)
    if ext == ".asmdef":
        return T.META_TEXT.format(guid=g)
    return T.META_DEFAULT.format(guid=g)

def generate_asset_metas():
    count = 0
    write(os.path.join(ROOT, "Assets.meta"), T.META_FOLDER.format(guid=guid_for("Assets")))
    count += 1
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        rel_dir = os.path.relpath(dirpath, ROOT)
        for d in sorted(dirnames):
            rel = os.path.join(rel_dir, d).replace(os.sep, "/")
            write(os.path.join(dirpath, d + ".meta"), T.META_FOLDER.format(guid=guid_for(rel)))
            count += 1
        for fn in sorted(filenames):
            if fn.endswith(".meta"):
                continue
            rel = os.path.join(rel_dir, fn).replace(os.sep, "/")
            write(os.path.join(dirpath, fn + ".meta"), meta_for(os.path.join(dirpath, fn), rel))
            count += 1
    return count

def generate_scenes():
    scenes = [
        ("00_Bootstrap", "Assets/_Game/Core/GameBootstrap.cs", False),
        ("01_MainMenu", "Assets/_Game/Root/MainMenuBootstrap.cs", False),
        ("02_World", "Assets/_Game/Root/WorldSystems.cs", True),
    ]
    for name, boot_script, with_light in scenes:
        content = T.SCENE.format(
            boot_guid=guid_for(boot_script),
            boot_go="GameBootstrap" if "GameBootstrap" in boot_script else ("MenuBoot" if "MainMenu" in boot_script else "WorldRoot"))
        # Unity 6 scenes end with a SceneRoots document that lists root objects
        # .format() with no fields unescapes the doubled YAML braces {{ }} -> { }
        content += (T.SCENE_LIGHT if with_light else T.SCENE_ROOTS_NO_LIGHT).format()
        path = os.path.join(ASSETS, "_Scenes", name + ".unity")
        write(path, content)
        rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
        write(path + ".meta", T.META_DEFAULT.format(guid=guid_for(rel)))
    return len(scenes)

def generate_project_settings():
    ps_dir = os.path.join(ROOT, "ProjectSettings")
    write(os.path.join(ps_dir, "ProjectVersion.txt"),
          f"m_EditorVersion: {T.UNITY_VERSION}\n"
          f"m_EditorVersionWithRevision: {T.UNITY_VERSION} (9f8ff13a3a1b)\n")

    product_guid = uuid.uuid5(NS, "product3d").hex
    write(os.path.join(ps_dir, "ProjectSettings.asset"), T.PROJECT_SETTINGS.format(product_guid=product_guid))
    write(os.path.join(ps_dir, "EditorBuildSettings.asset"), T.EDITOR_BUILD_SETTINGS.format(
        s0=guid_for("Assets/_Scenes/00_Bootstrap.unity"),
        s1=guid_for("Assets/_Scenes/01_MainMenu.unity"),
        s2=guid_for("Assets/_Scenes/02_World.unity")))
    write(os.path.join(ps_dir, "TagManager.asset"), T.TAG_MANAGER.format())
    write(os.path.join(ps_dir, "TimeManager.asset"), T.TIME_MANAGER.format())
    write(os.path.join(ps_dir, "AudioManager.asset"), T.AUDIO_MANAGER.format())
    write(os.path.join(ps_dir, "InputManager.asset"), T.INPUT_MANAGER.format())
    write(os.path.join(ps_dir, "PhysicsManager.asset"), T.PHYSICS_MANAGER.format())
    write(os.path.join(ps_dir, "GraphicsSettings.asset"), T.GRAPHICS_SETTINGS.format())
    write(os.path.join(ps_dir, "QualitySettings.asset"), T.QUALITY_SETTINGS.format())

def generate_packages_and_git():
    write(os.path.join(ROOT, "Packages", "manifest.json"), T.MANIFEST)
    write(os.path.join(ROOT, ".gitignore"), T.GITIGNORE)
    write(os.path.join(ROOT, ".gitattributes"), T.GITATTRIBUTES)

# ------------------------------------------------------------- validation --

def unity_yaml_load(text):
    class Loader(yaml.SafeLoader):
        pass

    def unknown(loader, suffix, node):
        if isinstance(node, yaml.MappingNode):
            return loader.construct_mapping(node, deep=True)
        if isinstance(node, yaml.SequenceNode):
            return loader.construct_sequence(node, deep=True)
        return loader.construct_scalar(node)

    Loader.add_multi_constructor("tag:unity3d.com,2011:", unknown)
    text = text.replace("%TAG !u! tag:unity3d.com,2011:\n", "")
    text = re.sub(r"!u!(\w+)", r"!<tag:unity3d.com,2011:\1>", text)
    return list(yaml.load_all(text, Loader=Loader))

def validate_all():
    checked, errors = 0, []
    for base in (ASSETS, os.path.join(ROOT, "ProjectSettings")):
        for dirpath, _dirnames, filenames in os.walk(base):
            for fn in filenames:
                if not fn.endswith((".meta", ".unity", ".asset")):
                    continue
                p = os.path.join(dirpath, fn)
                try:
                    with open(p, "r") as f:
                        unity_yaml_load(f.read())
                    checked += 1
                except Exception as e:
                    errors.append(f"{p}: {e}")
    return checked, errors

def main():
    print("Love Game 3D - Unity project generator")
    print("Unity version:", T.UNITY_VERSION)
    n = generate_asset_metas()
    print(f"meta files written:      {n}")
    print(f"scenes written:          {generate_scenes()}")
    generate_project_settings()
    print("project settings written: 10")
    generate_packages_and_git()
    print("manifest/git files:      ok")

    with open(REGISTRY_PATH, "w") as f:
        json.dump(registry, f, indent=2, sort_keys=True)
    print(f"guid registry:           {len(registry)} entries")

    checked, errors = validate_all()
    print(f"yaml files validated:    {checked}")
    if errors:
        print("YAML ERRORS:")
        for e in errors:
            print("  -", e)
        sys.exit(1)
    print("ALL YAML VALID")

if __name__ == "__main__":
    main()
