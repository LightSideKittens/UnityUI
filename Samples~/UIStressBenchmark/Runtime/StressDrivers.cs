using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// Applies one scenario's per-frame load. <see cref="Tick"/> runs inside the measured window and
    /// must stay allocation-free (except <see cref="ScenarioId.Churn"/>, whose churn is the point).
    /// </summary>
    public interface IStressDriver
    {
        ScenarioId Id { get; }
        void Setup(WidgetRegistry reg, BenchmarkConfig cfg);
        void Tick(int frame);
        void Teardown();
    }

    /// <summary>Creates the driver for a scenario id.</summary>
    public static class StressDriverFactory
    {
        public static IStressDriver Create(ScenarioId id)
        {
            switch (id)
            {
                case ScenarioId.StaticIdle: return new StaticIdleDriver();
                case ScenarioId.GeometryDirty: return new GeometryDirtyDriver();
                case ScenarioId.LayoutThrash: return new LayoutThrashDriver();
                case ScenarioId.TransformMove: return new TransformMoveDriver();
                case ScenarioId.MaterialToggle: return new MaterialToggleDriver();
                case ScenarioId.Churn: return new ChurnDriver();
                case ScenarioId.RaycastStorm: return new RaycastStormDriver();
                case ScenarioId.Mixed: return new MixedDriver();
                default: return new StaticIdleDriver();
            }
        }
    }

    /// <summary>Shared schedule helpers. A shuffled index order spreads the per-frame dirty window evenly across the pool.</summary>
    public abstract class StressDriverBase : IStressDriver
    {
        protected WidgetRegistry reg;
        protected BenchmarkConfig cfg;

        /// <summary>Multiplier applied to the dirty fraction, used by Mixed to run sub-drivers at reduced load.</summary>
        public float loadScale = 1f;

        public abstract ScenarioId Id { get; }

        public virtual void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            reg = registry;
            cfg = config;
        }

        public abstract void Tick(int frame);

        public virtual void Teardown() { }

        protected static int[] BuildOrder(int n, int seed)
        {
            var order = new int[n < 0 ? 0 : n];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            var rng = new System.Random(seed);
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }
            return order;
        }

        protected int DirtyCount(int n)
        {
            return Mathf.Clamp(Mathf.RoundToInt(n * cfg.dirtyFraction * loadScale), 0, n);
        }
    }

    /// <summary>Drives nothing; measures the engine's steady-state idle cost for the built rig.</summary>
    public sealed class StaticIdleDriver : StressDriverBase
    {
        public override ScenarioId Id => ScenarioId.StaticIdle;
        public override void Tick(int frame) { }
    }

    /// <summary>Marks a fraction of images vertex-dirty each frame, exercising mesh generation for every Image type.</summary>
    public sealed class GeometryDirtyDriver : StressDriverBase
    {
        int[] _order;
        int _cursor;
        int[] _fillOrder;
        int _fillCursor;

        public override ScenarioId Id => ScenarioId.GeometryDirty;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            _order = BuildOrder(reg.AllImages.Length, cfg.seed);
            _fillOrder = BuildOrder(reg.FilledImages.Length, cfg.seed + 1);
            _cursor = 0;
            _fillCursor = 0;
        }

        public override void Tick(int frame)
        {
            int n = _order.Length;
            if (n > 0)
            {
                int count = DirtyCount(n);
                for (int i = 0; i < count; i++)
                {
                    int idx = _order[(_cursor + i) % n];
                    var img = reg.AllImages[idx];
                    if (img == null) continue;
                    float t = ((frame + idx) & 255) * (1f / 255f);
                    img.color = new Color(t, 1f - t, 0.5f, 1f);
                }
                _cursor = (_cursor + count) % n;
            }

            int fn = _fillOrder.Length;
            if (fn > 0)
            {
                int fcount = DirtyCount(fn);
                for (int i = 0; i < fcount; i++)
                {
                    int idx = _fillOrder[(_fillCursor + i) % fn];
                    var img = reg.FilledImages[idx];
                    if (img == null) continue;
                    img.fillAmount = 0.5f + 0.5f * Mathf.Sin((frame + idx) * 0.1f);
                }
                _fillCursor = (_fillCursor + fcount) % fn;
            }
        }
    }

    /// <summary>Changes leaf preferred sizes inside nested layout columns, forcing LayoutRebuilder to run up the tree.</summary>
    public sealed class LayoutThrashDriver : StressDriverBase
    {
        int[] _order;
        int _cursor;

        public override ScenarioId Id => ScenarioId.LayoutThrash;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            _order = BuildOrder(reg.LayoutLeaves.Length, cfg.seed + 2);
            _cursor = 0;
        }

        public override void Tick(int frame)
        {
            int n = _order.Length;
            if (n == 0) return;
            int count = DirtyCount(n);
            for (int i = 0; i < count; i++)
            {
                int idx = _order[(_cursor + i) % n];
                var le = reg.LayoutLeaves[idx];
                if (le == null) continue;
                le.preferredHeight = 12f + 10f * (0.5f + 0.5f * Mathf.Sin((frame + idx) * 0.15f));
            }
            _cursor = (_cursor + count) % n;
        }

        public override void Teardown()
        {
            if (reg == null || reg.LayoutLeaves == null) return;
            Canvas.ForceUpdateCanvases();
            ulong h = 1469598103934665603UL;
            for (int i = 0; i < reg.LayoutLeaves.Length; i++)
            {
                var le = reg.LayoutLeaves[i];
                if (le == null) continue;
                var rt = le.transform as RectTransform;
                if (rt == null) continue;
                var r = rt.rect;
                var p = rt.anchoredPosition;
                h = HashFloat(h, r.width);
                h = HashFloat(h, r.height);
                h = HashFloat(h, p.x);
                h = HashFloat(h, p.y);
            }
            Debug.Log("[LayoutThrash] layout checksum = " + h.ToString("X16")
                + " (" + reg.LayoutLeaves.Length + " leaves) — must be identical before/after layout optimizations");
        }

        static ulong HashFloat(ulong h, float value)
        {
            var bytes = System.BitConverter.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                h *= 1099511628211UL;
            }
            return h;
        }
    }

    /// <summary>Moves free-layer rects each frame: a pure native re-batch with no mesh regen or layout.</summary>
    public sealed class TransformMoveDriver : StressDriverBase
    {
        int[] _order;
        int _cursor;
        Vector2[] _base;

        public override ScenarioId Id => ScenarioId.TransformMove;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            int n = reg.FreeRects.Length;
            _order = BuildOrder(n, cfg.seed + 3);
            _base = new Vector2[n];
            for (int i = 0; i < n; i++)
                if (reg.FreeRects[i] != null) _base[i] = reg.FreeRects[i].anchoredPosition;
            _cursor = 0;
        }

        public override void Tick(int frame)
        {
            int n = _order.Length;
            if (n == 0) return;
            int count = DirtyCount(n);
            for (int i = 0; i < count; i++)
            {
                int idx = _order[(_cursor + i) % n];
                var rt = reg.FreeRects[idx];
                if (rt == null) continue;
                float p = (frame + idx) * 0.2f;
                rt.anchoredPosition = _base[idx] + new Vector2(Mathf.Sin(p) * 12f, Mathf.Cos(p) * 12f);
            }
            _cursor = (_cursor + count) % n;
        }
    }

    /// <summary>Toggles materials on a fraction of images each frame, exercising the SetMaterialDirty / re-batch path.</summary>
    public sealed class MaterialToggleDriver : StressDriverBase
    {
        int[] _order;
        int _cursor;
        Material _alt;

        public override ScenarioId Id => ScenarioId.MaterialToggle;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            _order = BuildOrder(reg.MaterialTargets.Length, cfg.seed + 4);
            var shader = Shader.Find("UI/Default");
            if (shader != null) _alt = new Material(shader) { name = "BenchAltUI" };
            _cursor = 0;
        }

        public override void Tick(int frame)
        {
            int n = _order.Length;
            if (n == 0) return;
            int count = DirtyCount(n);
            for (int i = 0; i < count; i++)
            {
                int idx = _order[(_cursor + i) % n];
                var img = reg.MaterialTargets[idx];
                if (img == null) continue;
                img.material = ((frame + idx) & 1) == 0 ? _alt : null;
            }
            _cursor = (_cursor + count) % n;
        }

        public override void Teardown()
        {
            if (reg != null && reg.MaterialTargets != null)
            {
                for (int i = 0; i < reg.MaterialTargets.Length; i++)
                    if (reg.MaterialTargets[i] != null) reg.MaterialTargets[i].material = null;
            }
            if (_alt != null) Object.Destroy(_alt);
            _alt = null;
        }
    }

    /// <summary>Instantiates and destroys a fixed number of images each frame to stress registry churn and GC.</summary>
    public sealed class ChurnDriver : StressDriverBase
    {
        GameObject[] _ring;
        int _head;

        public override ScenarioId Id => ScenarioId.Churn;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            int alive = Mathf.Max(1, cfg.churnPerFrame) * 8;
            _ring = new GameObject[alive];
            _head = 0;
        }

        public override void Tick(int frame)
        {
            int per = Mathf.Max(0, cfg.churnPerFrame);
            for (int i = 0; i < per; i++)
            {
                if (_ring[_head] != null) Object.Destroy(_ring[_head]);

                var go = new GameObject("Churn", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(reg.ChurnParent, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(24f, 24f);
                rt.anchoredPosition = new Vector2(((frame * 31 + i * 17) % 600) - 300, ((frame * 13 + i * 7) % 400) - 200);
                var img = go.AddComponent<Image>();
                img.sprite = reg.ChurnSprite;
                img.raycastTarget = false;

                _ring[_head] = go;
                _head = (_head + 1) % _ring.Length;
            }
        }

        public override void Teardown()
        {
            if (_ring == null) return;
            for (int i = 0; i < _ring.Length; i++)
            {
                if (_ring[i] != null) Object.Destroy(_ring[i]);
                _ring[i] = null;
            }
        }
    }

    /// <summary>Issues many graphic raycasts per frame across the screen, stressing GraphicRaycaster and the registry sort.</summary>
    public sealed class RaycastStormDriver : StressDriverBase
    {
        Vector2[] _points;
        int _cursor;
        PointerEventData _ped;
        readonly List<RaycastResult> _results = new List<RaycastResult>(64);
        long _totalHits;
        long _totalCasts;

        public override ScenarioId Id => ScenarioId.RaycastStorm;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            var rng = new System.Random(cfg.seed + 5);
            _points = new Vector2[1024];
            for (int i = 0; i < _points.Length; i++)
                _points[i] = new Vector2(rng.Next(0, Mathf.Max(1, Screen.width)), rng.Next(0, Mathf.Max(1, Screen.height)));
            _cursor = 0;
            if (EventSystem.current != null) _ped = new PointerEventData(EventSystem.current);
        }

        public override void Tick(int frame)
        {
            var es = EventSystem.current;
            if (es == null) return;
            if (_ped == null) _ped = new PointerEventData(es);

            int per = Mathf.Max(0, cfg.raycastsPerFrame);
            int n = _points.Length;
            for (int i = 0; i < per; i++)
            {
                _ped.position = _points[_cursor];
                _cursor = (_cursor + 1) % n;
                es.RaycastAll(_ped, _results);
                _totalHits += _results.Count;
                _totalCasts++;
            }
        }

        public override void Teardown()
        {
            if (_totalCasts > 0)
                Debug.Log("[RaycastStorm] avg hits/cast = "
                    + (_totalHits / (double)_totalCasts).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                    + " (" + _totalHits + " hits / " + _totalCasts + " casts) — should stay >0 and stable across optimizations");
        }
    }

    /// <summary>Runs geometry, layout, transform and raycast drivers together at reduced load for a holistic frame.</summary>
    public sealed class MixedDriver : StressDriverBase
    {
        readonly IStressDriver[] _subs = new IStressDriver[4];

        public override ScenarioId Id => ScenarioId.Mixed;

        public override void Setup(WidgetRegistry registry, BenchmarkConfig config)
        {
            base.Setup(registry, config);
            _subs[0] = Scaled(new GeometryDirtyDriver(), 0.3f);
            _subs[1] = Scaled(new LayoutThrashDriver(), 0.3f);
            _subs[2] = Scaled(new TransformMoveDriver(), 0.3f);
            _subs[3] = new RaycastStormDriver();
            for (int i = 0; i < _subs.Length; i++) _subs[i].Setup(registry, config);
        }

        IStressDriver Scaled(StressDriverBase d, float scale)
        {
            d.loadScale = scale;
            return d;
        }

        public override void Tick(int frame)
        {
            for (int i = 0; i < _subs.Length; i++) _subs[i].Tick(frame);
        }

        public override void Teardown()
        {
            for (int i = 0; i < _subs.Length; i++) _subs[i].Teardown();
        }
    }
}
