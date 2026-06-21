using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// Drop-in UGUI stress benchmark. Self-bootstraps a canvas/event-system/camera rig, builds a heavy
    /// procedural UI and runs a queue of scenarios, recording CPU/GC/render metrics per scenario to the
    /// console and a CSV file. Uses only public UGUI API so the same component can A/B compare the stock
    /// package against a fork: set a distinct <see cref="buildLabel"/> per run and diff the CSVs.
    /// </summary>
    [AddComponentMenu("Light Side/UI Stress Benchmark")]
    [DisallowMultipleComponent]
    public sealed class UIBenchmark : MonoBehaviour
    {
        /// <summary>Lifecycle of the benchmark loop.</summary>
        public enum Phase { Idle, Warmup, Measure, ManualIdle, Finished }

        /// <summary>Auto runs the whole scenario queue then writes a report; Manual drives one scenario live from the HUD.</summary>
        [Header("Run")]
        public RunMode runMode = RunMode.Auto;

        /// <summary>Begin automatically when entering Play mode.</summary>
        public bool autoStartOnPlay = true;

        /// <summary>Tag written into the report (and CSV filename), e.g. "official-2.0.2" vs "fork-2.0.2".</summary>
        public string buildLabel = "";

        /// <summary>Scenarios included in an Auto run.</summary>
        public ScenarioSet scenarios = ScenarioSet.All;

        /// <summary>Scenario driven in Manual mode.</summary>
        public ScenarioId manualScenario = ScenarioId.GeometryDirty;

        /// <summary>UI size and per-frame load knobs.</summary>
        [Header("Config")]
        public BenchmarkConfig config = new BenchmarkConfig();

        /// <summary>Write a CSV report to persistentDataPath/UIBenchmark when an Auto run finishes.</summary>
        [Header("Output")]
        public bool writeCsv = true;

        /// <summary>Disable vSync and frame-rate cap during measurement so CPU cost is not hidden by the present wait.</summary>
        public bool overrideVSync = true;

        /// <summary>Keep the HUD visible during measurement. Off by default so HUD/IMGUI allocations stay out of the GC numbers.</summary>
        public bool showHudDuringMeasure = false;

        /// <summary>
        /// GC byte source. Off (default): per-frame managed heap growth via <c>GC.GetTotalMemory</c> — works without
        /// the profiler and keeps CPU timings clean, but is approximate (lumpy, can under-report). On: enables the
        /// profiler backend and reads the precise <c>GC Allocated In Frame</c> counter, at the cost of added CPU
        /// overhead and a possible fixed per-frame floor. The reliable verdict either way is <c>gc_collections</c>.
        /// </summary>
        public bool measureGcAllocations = false;

        Phase _phase = Phase.Idle;
        bool _running;
        int _phaseFrame;
        int _globalFrame;
        long _lastAlloc;

        Canvas _canvas;
        GameObject _createdCanvas;
        GameObject _createdEventSystem;
        GameObject _createdCamera;
        Font _font;
        ProceduralSprites _sprites;
        WidgetRegistry _reg;
        IStressDriver _driver;
        BenchmarkMetrics _metrics;

        readonly List<ScenarioId> _queue = new List<ScenarioId>();
        int _queueIndex;
        readonly List<ScenarioResult> _results = new List<ScenarioResult>();
        EnvInfo _env;

        bool _frameRateOverridden;
        int _savedVSync;
        int _savedTarget;
        bool _profilerForced;
        bool _savedProfilerEnabled;
        float _liveCpuMs;
        string _status = "Idle";

        /// <summary>Current lifecycle phase.</summary>
        public Phase CurrentPhase => _phase;
        /// <summary>True only while recording a scenario; the HUD hides itself in this phase unless asked not to.</summary>
        public bool IsMeasuring => _phase == Phase.Measure;
        /// <summary>True between begin and finish of a run.</summary>
        public bool IsRunning => _running;
        /// <summary>Scenario being built, warmed up, measured or driven live.</summary>
        public ScenarioId CurrentScenario { get; private set; }
        /// <summary>Index of the current scenario within an Auto queue.</summary>
        public int ScenarioIndex => _queueIndex;
        /// <summary>Total scenarios in the current Auto queue.</summary>
        public int ScenarioCount => _queue.Count;
        /// <summary>Graphic count of the built rig.</summary>
        public int TotalGraphics => _reg != null ? _reg.TotalGraphics : 0;
        /// <summary>Short human-readable state line for the HUD.</summary>
        public string Status => _status;
        /// <summary>Smoothed wall-clock frame time for the live HUD readout.</summary>
        public float LiveCpuMs => _liveCpuMs;
        /// <summary>Metrics collector, exposed so the HUD can read live profiler counters.</summary>
        public BenchmarkMetrics Metrics => _metrics;
        /// <summary>Completed scenario results so far.</summary>
        public IReadOnlyList<ScenarioResult> Results => _results;

        void Awake()
        {
            if (GetComponent<BenchmarkHUD>() == null) gameObject.AddComponent<BenchmarkHUD>();
        }

        void Start()
        {
            if (!autoStartOnPlay) return;
            if (runMode == RunMode.Auto) StartAuto();
            else StartManual();
        }

        void OnDestroy()
        {
            RestoreFrameRate();
            RestoreProfiler();
            if (_metrics != null) _metrics.Dispose();
            TeardownScene();
            DestroyRig();
        }

        /// <summary>Starts an automatic run over every selected scenario.</summary>
        public void StartAuto()
        {
            runMode = RunMode.Auto;
            BeginCommon();
            _queue.Clear();
            for (int i = 0; i < ScenarioSetExtensions.Count; i++)
            {
                var id = (ScenarioId)i;
                if (scenarios.Contains(id)) _queue.Add(id);
            }
            if (_queue.Count == 0) _queue.Add(ScenarioId.GeometryDirty);
            _queueIndex = 0;
            StartScenario(_queue[0]);
        }

        /// <summary>Builds the rig for <see cref="manualScenario"/> and drives it live until the HUD requests a measurement.</summary>
        public void StartManual()
        {
            runMode = RunMode.Manual;
            BeginCommon();
            _queue.Clear();
            CurrentScenario = manualScenario;
            BuildSceneAndDriver(manualScenario);
            _phase = Phase.ManualIdle;
            _phaseFrame = 0;
            ResetAlloc();
            _status = "Manual: " + manualScenario.DisplayName();
        }

        /// <summary>Runs warmup + measurement on the current scenario in Manual mode.</summary>
        public void RunManualMeasure()
        {
            if (!_running || runMode != RunMode.Manual || _phase != Phase.ManualIdle) return;
            _phase = Phase.Warmup;
            _phaseFrame = 0;
            ResetAlloc();
            _status = "Warmup: " + CurrentScenario.DisplayName();
        }

        /// <summary>Switches the live Manual scenario and rebuilds.</summary>
        public void CycleScenarioManual(int dir)
        {
            if (runMode != RunMode.Manual) return;
            int n = ScenarioSetExtensions.Count;
            int cur = (((int)CurrentScenario + dir) % n + n) % n;
            manualScenario = (ScenarioId)cur;
            CurrentScenario = manualScenario;
            BuildSceneAndDriver(CurrentScenario);
            ResetAlloc();
            _status = "Manual: " + CurrentScenario.DisplayName();
        }

        /// <summary>Cycles the size preset and rebuilds the rig.</summary>
        public void CyclePreset(int dir)
        {
            int n = 5;
            int cur = (((int)config.preset + dir) % n + n) % n;
            config.preset = (BenchmarkPreset)cur;
            config.ApplyPreset();
            RebuildSceneInternal();
            RefreshDriver();
            ResetAlloc();
            _status = "Preset: " + config.preset;
        }

        /// <summary>Tears down and rebuilds the rig with the current config.</summary>
        public void RebuildScene()
        {
            RebuildSceneInternal();
            RefreshDriver();
            ResetAlloc();
        }

        /// <summary>Logs the current results and writes a CSV immediately.</summary>
        public void WriteReportNow()
        {
            if (_results.Count == 0) return;
            BenchmarkMetrics.LogToConsole(_env, _results);
            WriteCsv();
        }

        void BeginCommon()
        {
            EnsureRig();
            config.ApplyPreset();
            _results.Clear();
            if (_metrics != null) _metrics.Dispose();
            _metrics = new BenchmarkMetrics(Mathf.Max(1, config.measureFrames), measureGcAllocations);
            _env = EnvInfo.Capture(buildLabel, ConfigSummary());
            if (overrideVSync) ApplyFrameRateOverride();
            if (measureGcAllocations) ApplyProfiler();
            _running = true;
            _globalFrame = 0;
            ResetAlloc();
        }

        void StartScenario(ScenarioId id)
        {
            CurrentScenario = id;
            BuildSceneAndDriver(id);
            _phase = Phase.Warmup;
            _phaseFrame = 0;
            ResetAlloc();
            _status = "Warmup: " + id.DisplayName() + " (" + (_queueIndex + 1) + "/" + _queue.Count + ")";
        }

        void BuildSceneAndDriver(ScenarioId id)
        {
            if (config.rebuildPerScenario || _reg == null) RebuildSceneInternal();
            if (_driver != null) _driver.Teardown();
            _driver = StressDriverFactory.Create(id);
            _driver.Setup(_reg, config);
        }

        void RefreshDriver()
        {
            if (_driver == null) return;
            _driver.Teardown();
            _driver.Setup(_reg, config);
        }

        void RebuildSceneInternal()
        {
            TeardownScene();
            if (_sprites == null) _sprites = ProceduralSprites.Create();
            if (config.includeText && _font == null) _font = LoadFont();
            _reg = UISceneBuilder.Build(_canvas, config, _sprites, _font);
            Canvas.ForceUpdateCanvases();
        }

        void TeardownScene()
        {
            if (_reg != null && _reg.Root != null) UnityEngine.Object.Destroy(_reg.Root);
            _reg = null;
        }

        void Update()
        {
            if (!_running) return;

            long now = GC.GetTotalMemory(false);
            long alloc = now - _lastAlloc;
            if (alloc < 0) alloc = 0;
            _lastAlloc = now;
            float dtMs = Time.unscaledDeltaTime * 1000f;
            _liveCpuMs = Mathf.Lerp(_liveCpuMs <= 0f ? dtMs : _liveCpuMs, dtMs, 0.1f);

            switch (_phase)
            {
                case Phase.Warmup:
                    _driver.Tick(_globalFrame);
                    if (++_phaseFrame >= Mathf.Max(0, config.warmupFrames))
                    {
                        _phase = Phase.Measure;
                        _phaseFrame = 0;
                        _metrics.BeginScenario();
                        _status = "Measuring: " + CurrentScenario.DisplayName();
                    }
                    break;

                case Phase.Measure:
                    _metrics.RecordFrame(dtMs, alloc);
                    _driver.Tick(_globalFrame);
                    if (++_phaseFrame >= Mathf.Max(1, config.measureFrames))
                    {
                        _results.Add(_metrics.EndScenario(CurrentScenario, _reg.TotalGraphics));
                        _driver.Teardown();
                        if (runMode == RunMode.Auto) AdvanceAuto();
                        else AfterManualMeasure();
                    }
                    break;

                case Phase.ManualIdle:
                    _driver.Tick(_globalFrame);
                    break;
            }

            _globalFrame++;
        }

        void AdvanceAuto()
        {
            _queueIndex++;
            if (_queueIndex < _queue.Count) StartScenario(_queue[_queueIndex]);
            else Finish();
        }

        void AfterManualMeasure()
        {
            var last = _results[_results.Count - 1];
            Debug.Log("[UIBenchmark] " + last.id.DisplayName()
                + " cpuMean=" + last.cpuMs.mean.ToString("0.###", CultureInfo.InvariantCulture) + "ms"
                + " p95=" + last.cpuMs.p95.ToString("0.###", CultureInfo.InvariantCulture)
                + " p99=" + last.cpuMs.p99.ToString("0.###", CultureInfo.InvariantCulture)
                + " gcMean=" + last.gcBytes.mean.ToString("0", CultureInfo.InvariantCulture) + "B"
                + " gcColl=" + last.gcCollections);
            _phase = Phase.ManualIdle;
            _phaseFrame = 0;
            BuildSceneAndDriver(CurrentScenario);
            ResetAlloc();
            _status = "Manual: " + CurrentScenario.DisplayName();
        }

        void Finish()
        {
            RestoreFrameRate();
            RestoreProfiler();
            _running = false;
            _phase = Phase.Finished;
            _status = "Finished — " + _results.Count + " scenarios";
            BenchmarkMetrics.LogToConsole(_env, _results);
            if (writeCsv) WriteCsv();
        }

        void WriteCsv()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "UIBenchmark");
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string file = Path.Combine(dir, _env.label + "_" + stamp + ".csv");
                File.WriteAllText(file, BenchmarkMetrics.BuildCsv(_env, _results));
                Debug.Log("[UIBenchmark] CSV written: " + file);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UIBenchmark] CSV write failed: " + e.Message);
            }
        }

        void EnsureRig()
        {
            if (Camera.main == null && _createdCamera == null)
            {
                _createdCamera = new GameObject("BenchCamera", typeof(Camera));
                _createdCamera.tag = "MainCamera";
                var cam = _createdCamera.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            }

            if (EventSystem.current == null && _createdEventSystem == null)
            {
                _createdEventSystem = new GameObject("BenchEventSystem", typeof(EventSystem));
            }

            if (_canvas == null)
            {
                _createdCanvas = new GameObject("BenchCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                _canvas = _createdCanvas.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = _createdCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        void DestroyRig()
        {
            if (_sprites != null)
            {
                _sprites.Destroy();
                _sprites = null;
            }
            if (_createdCanvas != null) UnityEngine.Object.Destroy(_createdCanvas);
            if (_createdEventSystem != null) UnityEngine.Object.Destroy(_createdEventSystem);
            if (_createdCamera != null) UnityEngine.Object.Destroy(_createdCamera);
        }

        void ApplyFrameRateOverride()
        {
            if (_frameRateOverridden) return;
            _savedVSync = QualitySettings.vSyncCount;
            _savedTarget = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            _frameRateOverridden = true;
        }

        void RestoreFrameRate()
        {
            if (!_frameRateOverridden) return;
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedTarget;
            _frameRateOverridden = false;
        }

        void ApplyProfiler()
        {
            if (_profilerForced) return;
            _savedProfilerEnabled = Profiler.enabled;
            Profiler.enabled = true;
            _profilerForced = true;
        }

        void RestoreProfiler()
        {
            if (!_profilerForced) return;
            Profiler.enabled = _savedProfilerEnabled;
            _profilerForced = false;
        }

        void ResetAlloc()
        {
            _lastAlloc = GC.GetTotalMemory(false);
        }

        string ConfigSummary()
        {
            return "preset=" + config.preset
                + " cards=" + config.cardPanels + "x" + config.cardsPerPanel
                + " layout=" + config.layoutColumns + "x" + config.layoutDepth + "x" + config.layoutLeavesPerColumn
                + " free=" + config.freeImages
                + " dirty=" + config.dirtyFraction.ToString("0.##", CultureInfo.InvariantCulture)
                + " ray=" + config.raycastsPerFrame
                + " churn=" + config.churnPerFrame
                + " warm=" + config.warmupFrames
                + " meas=" + config.measureFrames
                + " seed=" + config.seed
                + " gcProfiler=" + (measureGcAllocations ? "on" : "off");
        }

        static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f == null) { try { f = Font.CreateDynamicFontFromOSFont("Arial", 14); } catch { } }
            return f;
        }
    }
}
