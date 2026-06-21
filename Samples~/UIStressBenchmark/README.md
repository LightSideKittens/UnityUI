# UI Stress Benchmark

A procedural, heavy-load benchmark for Unity UI (UGUI). It self-bootstraps a canvas/event-system/camera
rig, builds a deep, dense UI from code, then runs a queue of scenarios that each stress one subsystem —
mesh generation, layout, batching, raycasting — and reports CPU / GC / render metrics to the console and a
CSV file.

It uses **only public UGUI API**, so the exact same component can be run against the stock `com.unity.ugui`
package and against a fork to compare them apples-to-apples.

## Run it

1. Import this sample (Package Manager → Unity UI → Samples → *UI Stress Benchmark* → Import).
2. `Tools ▸ Light Side ▸ UI Benchmark ▸ Create Benchmark Scene` (or add the `UIBenchmark` component to any GameObject).
3. Set **Build Label** on the component (e.g. `official-2.0.2` or `fork-2.0.2`).
4. Press **Play**.

In **Auto** mode it runs every selected scenario (warmup → measure), prints a summary, and writes a CSV to
`persistentDataPath/UIBenchmark/<label>_<timestamp>.csv`. In **Manual** mode it builds one scenario and drives
it live; use the on-screen HUD to measure, switch scenario/preset, rebuild, or write a CSV.

## A/B comparison workflow

The sample is copied into `Assets/` on import, so it survives package swaps:

1. Import once while the fork is active.
2. Run with `Build Label = fork-...` → CSV.
3. Swap `com.unity.ugui` back to the official package (the imported sample keeps compiling — it only uses public API).
4. Run with `Build Label = official-...` → CSV.
5. Diff the two CSVs.

For the cleanest numbers, build a **Standalone Player** (Mono or IL2CPP) rather than measuring in the Editor —
the Editor adds overhead and caps the loop. `Override VSync` is on by default so frame time reflects CPU cost
rather than the present wait.

## Scenarios

| Scenario | Stresses | Notes |
|---|---|---|
| Static Idle | engine idle cost for a heavy built rig | nothing is dirtied; surfaces per-frame overhead |
| Geometry Dirty | mesh generation for every Image type (Simple/Sliced/Tiled/Filled) | color + fillAmount churn → `SetVerticesDirty` |
| Layout Thrash | `LayoutRebuilder` over nested layout groups + `ContentSizeFitter` | leaf `preferredHeight` changes propagate up |
| Transform Move | the native canvas re-batch, isolated | moves free-layer rects; no mesh regen, no layout |
| Material Toggle | `SetMaterialDirty` / batch breaking | alternates material on a fraction of images |
| Churn | registry + GC under instantiate/destroy | creates and destroys widgets every frame |
| Raycast Storm | `GraphicRaycaster` + registry sort | many `RaycastAll` calls per frame |
| Mixed | a realistic blend of the above at reduced load | holistic frame |

## Metrics (per scenario)

- **CPU** — wall-clock frame time, plus profiler `Main Thread` / `Render Thread` time: mean, p50, p95, p99, max.
- **GC** — managed bytes per frame, max, total, and GC collection count. Bytes are approximate by default (managed heap growth); `gc_collections` is the reliable verdict. The CSV also reports `cpu_over_idle_ms` and `gc_over_idle_b` (each metric minus the Static Idle baseline), which cancel fixed editor/measurement overhead.
- **Render** — `Draw Calls Count`, `SetPass Calls Count`, `Triangles Count`, `Vertices Count`.

Counters a given Unity build does not expose are reported as `0`/blank rather than failing. Per-frame GC bytes
come from the profiler's `GC Allocated In Frame` counter, which only reports while the profiler backend is
active — so `Measure Gc Allocations` enables it for the run (small, consistent CPU overhead). The reliable
zero-GC verdict is `gc_collections`: how many garbage collections happened during the measured window.

## Config

- **Preset**: Light / Medium / Heavy / Insane / Custom. Start with **Medium**; Insane builds tens of thousands of graphics and rebuilds per scenario, which is slow to set up (not measured, but be patient).
- **Dirty Fraction / Raycasts Per Frame / Churn Per Frame**: per-frame load.
- **Warmup / Measure Frames**: measurement window. **Seed**: makes both A/B runs perform identical work.
- **Include Text** is off by default to keep mesh/layout numbers free of font-subsystem noise.

The benchmark harness itself is allocation-free in steady state, and the HUD hides during measurement, so the
reported GC numbers reflect the UI system under test — not the tooling.
