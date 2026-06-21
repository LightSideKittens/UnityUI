using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace LightSide.UIBenchmark
{
    /// <summary>Environment snapshot written into every report so A/B runs are comparable in context.</summary>
    public struct EnvInfo
    {
        public string label;
        public string unityVersion;
        public string platform;
        public string os;
        public string cpu;
        public string gpu;
        public string graphicsApi;
        public int memoryMB;
        public string backend;
        public bool isEditor;
        public int screenWidth;
        public int screenHeight;
        public int vSyncCount;
        public int targetFrameRate;
        public string configSummary;

        /// <summary>Captures the current environment. Call before applying any vsync/frame-rate overrides.</summary>
        public static EnvInfo Capture(string label, string configSummary)
        {
            return new EnvInfo
            {
                label = string.IsNullOrEmpty(label) ? "run" : label,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                os = SystemInfo.operatingSystem,
                cpu = SystemInfo.processorType,
                gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                memoryMB = SystemInfo.systemMemorySize,
                backend = ScriptingBackend(),
                isEditor = Application.isEditor,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate,
                configSummary = configSummary
            };
        }

        static string ScriptingBackend()
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#elif ENABLE_MONO
            return "Mono";
#else
            return "Unknown";
#endif
        }
    }

    /// <summary>Computed statistics for a single scenario.</summary>
    public struct ScenarioResult
    {
        public ScenarioId id;
        public int frames;
        public int graphics;
        public StatResult cpuMs;
        public StatResult mainMs;
        public StatResult renderMs;
        public StatResult gcBytes;
        public StatResult drawCalls;
        public StatResult setPass;
        public StatResult tris;
        public StatResult verts;
        public double gcTotalBytes;
        public int gcCollections;
    }

    /// <summary>
    /// Collects per-frame CPU, GC and render-pipeline metrics for the active scenario and renders
    /// reports. Wraps a set of <see cref="ProfilerRecorder"/>s defensively: any counter the running
    /// Unity build does not expose is reported as n/a instead of throwing.
    /// </summary>
    public sealed class BenchmarkMetrics : IDisposable
    {
        ProfilerRecorder _mainThread;
        ProfilerRecorder _renderThread;
        ProfilerRecorder _gcAlloc;
        ProfilerRecorder _drawCalls;
        ProfilerRecorder _setPass;
        ProfilerRecorder _tris;
        ProfilerRecorder _verts;

        readonly StatSeries _cpuMs;
        readonly StatSeries _mainMs;
        readonly StatSeries _renderMs;
        readonly StatSeries _gcBytes;
        readonly StatSeries _drawCallsS;
        readonly StatSeries _setPassS;
        readonly StatSeries _trisS;
        readonly StatSeries _vertsS;

        int _gcCollAtBegin;
        readonly bool _preferProfilerGc;

        public BenchmarkMetrics(int capacity, bool preferProfilerGc)
        {
            _preferProfilerGc = preferProfilerGc;
            _cpuMs = new StatSeries(capacity);
            _mainMs = new StatSeries(capacity);
            _renderMs = new StatSeries(capacity);
            _gcBytes = new StatSeries(capacity);
            _drawCallsS = new StatSeries(capacity);
            _setPassS = new StatSeries(capacity);
            _trisS = new StatSeries(capacity);
            _vertsS = new StatSeries(capacity);

            _mainThread = TryStart(ProfilerCategory.Internal, "Main Thread");
            _renderThread = TryStart(ProfilerCategory.Internal, "Render Thread");
            _gcAlloc = TryStart(ProfilerCategory.Memory, "GC Allocated In Frame");
            _drawCalls = TryStart(ProfilerCategory.Render, "Draw Calls Count");
            _setPass = TryStart(ProfilerCategory.Render, "SetPass Calls Count");
            _tris = TryStart(ProfilerCategory.Render, "Triangles Count");
            _verts = TryStart(ProfilerCategory.Render, "Vertices Count");
        }

        /// <summary>Last frame's main-thread time in milliseconds, or 0 if the counter is unavailable.</summary>
        public double LiveMainMs => _mainThread.Valid ? _mainThread.LastValue * 1e-6 : 0;
        /// <summary>Last frame's draw-call count, or 0 if the counter is unavailable.</summary>
        public long LiveDrawCalls => _drawCalls.Valid ? _drawCalls.LastValue : 0;
        /// <summary>Last frame's vertex count, or 0 if the counter is unavailable.</summary>
        public long LiveVerts => _verts.Valid ? _verts.LastValue : 0;

        public void Dispose()
        {
            Stop(ref _mainThread);
            Stop(ref _renderThread);
            Stop(ref _gcAlloc);
            Stop(ref _drawCalls);
            Stop(ref _setPass);
            Stop(ref _tris);
            Stop(ref _verts);
        }

        /// <summary>Resets all series and snapshots the GC collection count for delta accounting.</summary>
        public void BeginScenario()
        {
            _cpuMs.Reset();
            _mainMs.Reset();
            _renderMs.Reset();
            _gcBytes.Reset();
            _drawCallsS.Reset();
            _setPassS.Reset();
            _trisS.Reset();
            _vertsS.Reset();
            _gcCollAtBegin = GcCollectionCount();
        }

        /// <summary>
        /// Records one measured frame. <paramref name="cpuMs"/> is wall-clock frame time and
        /// <paramref name="gcDeltaBytes"/> the managed heap growth during the previous full frame (used unless the
        /// profiler GC counter is enabled and valid, in which case the precise per-frame counter wins).
        /// Profiler counters are read from their last completed sample.
        /// </summary>
        public void RecordFrame(double cpuMs, long gcDeltaBytes)
        {
            _cpuMs.Add(cpuMs);
            long gc = (_preferProfilerGc && _gcAlloc.Valid) ? _gcAlloc.LastValue : gcDeltaBytes;
            _gcBytes.Add(gc);

            if (_mainThread.Valid) _mainMs.Add(_mainThread.LastValue * 1e-6);
            if (_renderThread.Valid) _renderMs.Add(_renderThread.LastValue * 1e-6);
            if (_drawCalls.Valid) _drawCallsS.Add(_drawCalls.LastValue);
            if (_setPass.Valid) _setPassS.Add(_setPass.LastValue);
            if (_tris.Valid) _trisS.Add(_tris.LastValue);
            if (_verts.Valid) _vertsS.Add(_verts.LastValue);
        }

        /// <summary>Finalizes the active scenario into an immutable result.</summary>
        public ScenarioResult EndScenario(ScenarioId id, int graphics)
        {
            return new ScenarioResult
            {
                id = id,
                frames = _cpuMs.Count,
                graphics = graphics,
                cpuMs = _cpuMs.Compute(),
                mainMs = _mainMs.Compute(),
                renderMs = _renderMs.Compute(),
                gcBytes = _gcBytes.Compute(),
                drawCalls = _drawCallsS.Compute(),
                setPass = _setPassS.Compute(),
                tris = _trisS.Compute(),
                verts = _vertsS.Compute(),
                gcTotalBytes = _gcBytes.Sum(),
                gcCollections = Math.Max(0, GcCollectionCount() - _gcCollAtBegin)
            };
        }

        static int GcCollectionCount()
        {
            return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        }

        static ProfilerRecorder TryStart(ProfilerCategory category, string stat)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, stat);
            }
            catch
            {
                return default;
            }
        }

        static void Stop(ref ProfilerRecorder r)
        {
            if (r.Valid) r.Dispose();
            r = default;
        }

        /// <summary>Builds a CSV report: <c>#</c>-prefixed environment lines followed by one row per scenario.</summary>
        public static string BuildCsv(EnvInfo env, List<ScenarioResult> results)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(4096);
            sb.Append("# label,").Append(env.label).Append('\n');
            sb.Append("# unity,").Append(env.unityVersion).Append('\n');
            sb.Append("# platform,").Append(env.platform).Append('\n');
            sb.Append("# os,").Append(env.os).Append('\n');
            sb.Append("# cpu,").Append(env.cpu).Append('\n');
            sb.Append("# gpu,").Append(env.gpu).Append(" (").Append(env.graphicsApi).Append(")\n");
            sb.Append("# memoryMB,").Append(env.memoryMB.ToString(ci)).Append('\n');
            sb.Append("# backend,").Append(env.backend).Append(env.isEditor ? " (Editor)" : " (Player)").Append('\n');
            sb.Append("# screen,").Append(env.screenWidth.ToString(ci)).Append('x').Append(env.screenHeight.ToString(ci)).Append('\n');
            sb.Append("# vSyncCount,").Append(env.vSyncCount.ToString(ci)).Append('\n');
            sb.Append("# targetFrameRate,").Append(env.targetFrameRate.ToString(ci)).Append('\n');
            sb.Append("# config,").Append(env.configSummary).Append('\n');

            double idleCpu, idleGc;
            FindIdleBaseline(results, out idleCpu, out idleGc);

            sb.Append("scenario,frames,graphics,");
            sb.Append("cpu_mean_ms,cpu_p50_ms,cpu_p95_ms,cpu_p99_ms,cpu_max_ms,fps_from_cpu_mean,cpu_over_idle_ms,");
            sb.Append("main_mean_ms,main_p95_ms,render_mean_ms,");
            sb.Append("gc_mean_b,gc_max_b,gc_total_b,gc_over_idle_b,gc_collections,");
            sb.Append("draw_calls_mean,setpass_mean,tris_mean,verts_mean\n");

            foreach (var r in results)
            {
                double fps = r.cpuMs.mean > 0 ? 1000.0 / r.cpuMs.mean : 0;
                sb.Append(r.id.DisplayName()).Append(',');
                sb.Append(r.frames.ToString(ci)).Append(',');
                sb.Append(r.graphics.ToString(ci)).Append(',');
                sb.Append(F(r.cpuMs.mean)).Append(',');
                sb.Append(F(r.cpuMs.p50)).Append(',');
                sb.Append(F(r.cpuMs.p95)).Append(',');
                sb.Append(F(r.cpuMs.p99)).Append(',');
                sb.Append(F(r.cpuMs.max)).Append(',');
                sb.Append(F(fps)).Append(',');
                sb.Append(F(r.cpuMs.mean - idleCpu)).Append(',');
                sb.Append(F(r.mainMs.mean)).Append(',');
                sb.Append(F(r.mainMs.p95)).Append(',');
                sb.Append(F(r.renderMs.mean)).Append(',');
                sb.Append(F0(r.gcBytes.mean)).Append(',');
                sb.Append(F0(r.gcBytes.max)).Append(',');
                sb.Append(F0(r.gcTotalBytes)).Append(',');
                sb.Append(F0(r.gcBytes.mean - idleGc)).Append(',');
                sb.Append(r.gcCollections.ToString(ci)).Append(',');
                sb.Append(F0(r.drawCalls.mean)).Append(',');
                sb.Append(F0(r.setPass.mean)).Append(',');
                sb.Append(F0(r.tris.mean)).Append(',');
                sb.Append(F0(r.verts.mean)).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Writes a compact, aligned summary of the run to the Unity console.</summary>
        public static void LogToConsole(EnvInfo env, List<ScenarioResult> results)
        {
            var sb = new StringBuilder(2048);
            sb.Append("=== UI Stress Benchmark — ").Append(env.label).Append(" ===\n");
            sb.Append(env.unityVersion).Append(" | ").Append(env.platform).Append(" | ").Append(env.backend)
              .Append(env.isEditor ? " (Editor)" : " (Player)")
              .Append(" | ").Append(env.screenWidth).Append('x').Append(env.screenHeight)
              .Append(" | vSync=").Append(env.vSyncCount).Append('\n');
            sb.Append(env.gpu).Append(" (").Append(env.graphicsApi).Append(")\n");
            sb.Append(env.configSummary).Append("\n\n");

            double idleCpu, idleGc;
            FindIdleBaseline(results, out idleCpu, out idleGc);

            sb.Append(Pad("Scenario", 16)).Append(Pad("cpuMean", 10)).Append(Pad("Δidle", 10)).Append(Pad("p95", 9)).Append(Pad("p99", 9))
              .Append(Pad("gcΔidle", 11)).Append(Pad("gcColl", 7)).Append(Pad("draws", 9)).Append("verts\n");
            sb.Append(new string('-', 98)).Append('\n');

            foreach (var r in results)
            {
                sb.Append(Pad(r.id.DisplayName(), 16));
                sb.Append(Pad(F(r.cpuMs.mean) + "ms", 10));
                sb.Append(Pad(F(r.cpuMs.mean - idleCpu), 10));
                sb.Append(Pad(F(r.cpuMs.p95), 9));
                sb.Append(Pad(F(r.cpuMs.p99), 9));
                sb.Append(Pad(Bytes(r.gcBytes.mean - idleGc), 11));
                sb.Append(Pad(r.gcCollections.ToString(), 7));
                sb.Append(Pad(F0(r.drawCalls.mean), 9));
                sb.Append(F0(r.verts.mean)).Append('\n');
            }
            Debug.Log(sb.ToString());
        }

        static void FindIdleBaseline(List<ScenarioResult> results, out double idleCpu, out double idleGc)
        {
            idleCpu = 0;
            idleGc = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].id == ScenarioId.StaticIdle)
                {
                    idleCpu = results[i].cpuMs.mean;
                    idleGc = results[i].gcBytes.mean;
                    return;
                }
            }
        }

        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string F0(double v) => v.ToString("0", CultureInfo.InvariantCulture);

        static string Bytes(double b)
        {
            if (b >= 1024 * 1024) return (b / (1024 * 1024)).ToString("0.##", CultureInfo.InvariantCulture) + "MB";
            if (b >= 1024) return (b / 1024).ToString("0.##", CultureInfo.InvariantCulture) + "KB";
            return b.ToString("0", CultureInfo.InvariantCulture) + "B";
        }

        static string Pad(string s, int width)
        {
            if (s == null) s = "";
            if (s.Length >= width) return s + " ";
            return s + new string(' ', width - s.Length);
        }
    }
}
