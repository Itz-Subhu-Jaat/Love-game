# TESTING — Love Game

## Suites

| Suite | Where | Runs | Focus |
|---|---|---|---|
| Static validator | `Tools/validation/validate_project.py` | every push (CI, no license) + locally | meta/GUID pairing, scene script refs, asmdef graph + cycles, C# brace/quote balance, shader structure, JSON catalog integrity (24 regions, 40 items, packs, tips), Unity version pin, secret scan |
| EditMode tests | `Assets/_Game/Tests/EditMode` | CI unity-tests job (needs license) + editor | region catalog (count/unique/validated/POIs/fast travel), determinism (RNG, noise), core math, save roundtrip + migration, item/furniture/manifest parsing, pooling |
| PlayMode tests | `Assets/_Game/Tests/PlayMode` | CI unity-tests job (needs license) + editor | controller gravity/landing, swim state detection, character rig + poses, day/night advance + events, vehicle enter/exit lifecycle |

Run locally: `Window → General → Test Runner` → EditMode/PlayMode tabs → Run All.
Run validator locally: `python3 Tools/validation/validate_project.py --project-root .`

## The loop (build → test → fix → repeat)

1. Change code.
2. Run the validator (fast, catches structural breakage).
3. PlayMode/EditMode suites in the editor or CI.
4. Fix everything the logs surface — warnings included (the project is written to compile
   warning-clean: no unused fields, FindObjectsByType instead of deprecated FindObjectOfType,
   Rigidbody.linearVelocity for Unity 6, named handlers for clean event unsubscription).
5. Re-run. Regression rule: when a system changes, retest its dependents
   (PlayerController → camera/interaction/vehicles; Streaming → map/fast travel/save;
   Interaction → NPC/couple/activities; Content → downloads/streaming).

## CI failure triage

- **lint job fails**: validator prints `[ERROR]` lines with file paths — these are structural
  (missing meta, bad JSON, unbalanced syntax) and fixable without Unity.
- **unity-tests fail**: download the `test-results` artifact; NUnit XML has full stack traces.
- **APK build fails**: check the `Build Android APK` run log for `CS####` compile errors
  (CiBuilder exits non-zero, so failures are loud, never silently green).

## Not yet covered / known gaps

- No device farm: touch ergonomics (joystick feel, button reach) need a physical phone.
- No visual regression tests (screenshot diffs) — future when rendering stabilizes.
- Performance budgets (frame time on mid-tier Android) measured once CI builds run.
