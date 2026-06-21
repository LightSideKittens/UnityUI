using System;
using UnityEngine;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// How the benchmark drives its scenario queue once Play starts.
    /// <para>Auto runs every enabled scenario back-to-back (warmup then measure) and writes a CSV report.
    /// Manual builds the scene and waits for the on-screen HUD to start a single scenario.</para>
    /// </summary>
    public enum RunMode
    {
        Auto,
        Manual
    }

    /// <summary>
    /// Coarse size knob for the generated UI. Picks element counts and nesting depth.
    /// <see cref="BenchmarkPreset.Custom"/> keeps the explicit fields on <see cref="BenchmarkConfig"/> untouched.
    /// </summary>
    public enum BenchmarkPreset
    {
        Light,
        Medium,
        Heavy,
        Insane,
        Custom
    }

    /// <summary>
    /// Identity of a single benchmark scenario. The numeric order is the canonical run order
    /// and must match the bit order of <see cref="ScenarioSet"/>.
    /// </summary>
    public enum ScenarioId
    {
        StaticIdle = 0,
        GeometryDirty = 1,
        LayoutThrash = 2,
        TransformMove = 3,
        MaterialToggle = 4,
        Churn = 5,
        RaycastStorm = 6,
        Mixed = 7
    }

    /// <summary>
    /// Inspector-friendly multi-select of the scenarios to run. Each bit maps to the
    /// matching <see cref="ScenarioId"/> via <c>1 &lt;&lt; (int)id</c>.
    /// </summary>
    [Flags]
    public enum ScenarioSet
    {
        None = 0,
        StaticIdle = 1 << 0,
        GeometryDirty = 1 << 1,
        LayoutThrash = 1 << 2,
        TransformMove = 1 << 3,
        MaterialToggle = 1 << 4,
        Churn = 1 << 5,
        RaycastStorm = 1 << 6,
        Mixed = 1 << 7,
        All = StaticIdle | GeometryDirty | LayoutThrash | TransformMove | MaterialToggle | Churn | RaycastStorm | Mixed
    }

    /// <summary>
    /// Helpers for translating between <see cref="ScenarioId"/> and <see cref="ScenarioSet"/>.
    /// </summary>
    public static class ScenarioSetExtensions
    {
        /// <summary>Total number of distinct scenarios.</summary>
        public const int Count = 8;

        /// <summary>True when the set selects the given scenario.</summary>
        public static bool Contains(this ScenarioSet set, ScenarioId id)
        {
            return (set & (ScenarioSet)(1 << (int)id)) != 0;
        }

        /// <summary>Human-readable scenario name for HUD and reports.</summary>
        public static string DisplayName(this ScenarioId id)
        {
            switch (id)
            {
                case ScenarioId.StaticIdle: return "Static Idle";
                case ScenarioId.GeometryDirty: return "Geometry Dirty";
                case ScenarioId.LayoutThrash: return "Layout Thrash";
                case ScenarioId.TransformMove: return "Transform Move";
                case ScenarioId.MaterialToggle: return "Material Toggle";
                case ScenarioId.Churn: return "Churn";
                case ScenarioId.RaycastStorm: return "Raycast Storm";
                case ScenarioId.Mixed: return "Mixed";
                default: return id.ToString();
            }
        }
    }

    /// <summary>
    /// All knobs that shape the generated UI and the measurement loop. Serialized on
    /// <see cref="UIBenchmark"/>; the explicit count fields are only authoritative when
    /// <see cref="preset"/> is <see cref="BenchmarkPreset.Custom"/>.
    /// </summary>
    [Serializable]
    public class BenchmarkConfig
    {
        /// <summary>Coarse size selector. Anything other than Custom overwrites the count fields below at build time.</summary>
        [Header("Size")]
        public BenchmarkPreset preset = BenchmarkPreset.Medium;

        /// <summary>Number of card panels arranged in the visual grid section.</summary>
        [Header("Card grid (mesh / material / raycast bulk)")]
        public int cardPanels = 36;
        /// <summary>Cards laid out inside each panel by a GridLayoutGroup.</summary>
        public int cardsPerPanel = 25;
        /// <summary>Fraction of cards wrapped in a clipping region (alternating RectMask2D / Mask).</summary>
        [Range(0f, 1f)] public float maskedCardFraction = 0.15f;

        /// <summary>Number of independent nested-layout columns whose leaves drive the LayoutThrash scenario.</summary>
        [Header("Deep layout section (LayoutRebuilder stress)")]
        public int layoutColumns = 24;
        /// <summary>Nesting depth of alternating vertical/horizontal groups per column.</summary>
        public int layoutDepth = 4;
        /// <summary>Leaf elements at the bottom of each layout column.</summary>
        public int layoutLeavesPerColumn = 6;

        /// <summary>Absolutely positioned images outside any layout group, moved by TransformMove.</summary>
        [Header("Free layer (native re-batch stress)")]
        public int freeImages = 500;

        /// <summary>Fraction of the relevant element pool touched each measured frame (geometry/material/layout/move).</summary>
        [Header("Per-frame load")]
        [Range(0f, 1f)] public float dirtyFraction = 0.25f;
        /// <summary>Raycasts issued per frame in the RaycastStorm scenario.</summary>
        public int raycastsPerFrame = 64;
        /// <summary>Widgets created and an equal number destroyed each frame in the Churn scenario.</summary>
        public int churnPerFrame = 32;

        /// <summary>Frames driven but not recorded, to let pools, JIT and the GC settle.</summary>
        [Header("Measurement")]
        public int warmupFrames = 60;
        /// <summary>Frames recorded per scenario.</summary>
        public int measureFrames = 300;
        /// <summary>Seed for the deterministic dirty-index schedule so both A/B runs perform identical work.</summary>
        public int seed = 12345;

        /// <summary>Rebuild the whole scene before each scenario for clean, independent state.</summary>
        [Header("Options")]
        public bool rebuildPerScenario = true;
        /// <summary>Add a few legacy Text labels for realism. Off by default to keep mesh/layout numbers free of font subsystem noise.</summary>
        public bool includeText = false;

        /// <summary>
        /// Overwrites the count fields from <see cref="preset"/> unless the preset is
        /// <see cref="BenchmarkPreset.Custom"/>. Call once before building.
        /// </summary>
        public void ApplyPreset()
        {
            switch (preset)
            {
                case BenchmarkPreset.Light:
                    cardPanels = 16; cardsPerPanel = 16;
                    layoutColumns = 12; layoutDepth = 3; layoutLeavesPerColumn = 5;
                    freeImages = 200;
                    break;
                case BenchmarkPreset.Medium:
                    cardPanels = 36; cardsPerPanel = 25;
                    layoutColumns = 24; layoutDepth = 4; layoutLeavesPerColumn = 6;
                    freeImages = 500;
                    break;
                case BenchmarkPreset.Heavy:
                    cardPanels = 64; cardsPerPanel = 36;
                    layoutColumns = 40; layoutDepth = 4; layoutLeavesPerColumn = 8;
                    freeImages = 1000;
                    break;
                case BenchmarkPreset.Insane:
                    cardPanels = 100; cardsPerPanel = 49;
                    layoutColumns = 64; layoutDepth = 5; layoutLeavesPerColumn = 10;
                    freeImages = 2000;
                    break;
                case BenchmarkPreset.Custom:
                    break;
            }
        }
    }
}
