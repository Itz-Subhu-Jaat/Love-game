# Game Design — Love Quest

## Premise

The night is lonely and broken hearts fall from the sky. You are Cupid —
a winged heart with a golden bow. Shoot love arrows to mend hearts before
they crash into you. Ten waves, one night, endless love.

## Core loop (second-to-second)

1. Broken hearts spawn at the top and drift down with a sinusoidal wobble.
2. You slide horizontally along the bottom line and fire arrows upward.
3. Arrow + heart = **MEND**: heart turns pink, gets a bandage, floats away
   with sparkles; score + combo tick up.
4. Heart reaches you = **−1 life** + 1.6 s invulnerability flicker.
5. Heart falls past the screen = combo breaker.

## Progression (wave-to-wave)

| Wave | Hearts | Fall speed | Spawn gap | Heart scale |
|---|---|---|---|---|
| 1 | 8 | 1.70 u/s | 1.05 s | 1.00 |
| 4 | 14 | 2.36 u/s | 0.87 s | 0.94 |
| 7 | 20 | 3.02 u/s | 0.69 s | 0.88 |
| 10 | 26 | 3.68 u/s | 0.51 s | 0.82 |

All values derive from `WaveFormulas` (capped: speed 4.6, gap 0.32 s, scale 0.8).

## Scoring

- Mend: **25 × multiplier**. Multiplier = 1 + (combo−1)/4, capped ×4
  (combo 5 → ×2, 9 → ×3, 13+ → ×4).
- Rose (lives full): **+100**. Rose (heals): +50.
- Missed heart / getting hit: combo resets to 0.
- Best score persists via `PlayerPrefs`.

## Power-ups

- **Rose** — 12% of spawn slots. Heals +1 life (max 5), else +100 points.
- **Ice crystal** — 5% chance, max one per wave. `Time.timeScale = 0.45` for
  4 real-time seconds (player, arrows and hearts all slow — a genuine
  tactical save).

## Win / lose

- **Win**: clear wave 10 → "LOVE PREVAILS!" panel + stats.
- **Lose**: 3 lives (up to 5) gone → "HEARTS FALTERED" panel.
- Both show score, best (with NEW BEST badge), wave reached, hearts mended,
  and Play Again / Main Menu.

## Feel & feedback

- HUD: lives hearts, wave banner (fades in/out), love meter fill (overall
  progress), score pop, combo pulse, red hurt flash, blue slow-mo tint.
- World: floating "+points" texts, sparkle bursts, mended hearts fly up.
- Audio (all synthesized at runtime, zero files): arrow pew (down-sweep),
  mend ding (two-tone), hurt buzz, heal/power arpeggios, wave chime,
  win/lose jingles, and a 16 s looping ambient pad (C–Am–F–G, detuned sines).

## Art direction

Romantic night palette — deep purples (#12082B) through rose dusk (#6B2D5E),
pink hearts (#FF5C8A), gold accents (#FFD168). All sprites are procedurally
drawn (parametric heart curve) so the whole game ships ~300 KB of PNGs.

## Constraints embraced

- No physics engine — circle-distance collisions.
- No particle system — pooled sprite bursts (`AutoMotion`).
- No audio files — `AudioClip.Create` synthesis.
- No serialized scene content — code-built UI (`UiFactory`).
Every constraint is a deliberate robustness choice: fewer moving parts,
nothing to corrupt, tiny repo, deterministic CI.
