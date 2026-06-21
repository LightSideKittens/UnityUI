using System.Globalization;
using UnityEngine;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// IMGUI overlay for <see cref="UIBenchmark"/>: live CPU/GC/render readout plus run controls.
    /// Hides itself during the measured window (unless <see cref="UIBenchmark.showHudDuringMeasure"/>)
    /// so its own IMGUI allocations never contaminate the GC metric. Added automatically by the benchmark.
    /// </summary>
    [RequireComponent(typeof(UIBenchmark))]
    public sealed class BenchmarkHUD : MonoBehaviour
    {
        UIBenchmark _b;
        GUIStyle _box;
        GUIStyle _label;
        GUIStyle _title;

        void Awake()
        {
            _b = GetComponent<UIBenchmark>();
        }

        void EnsureStyles()
        {
            if (_box != null) return;
            _box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 10, 10) };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (_b == null) return;
            if (_b.IsMeasuring && !_b.showHudDuringMeasure) return;

            EnsureStyles();
            GUILayout.BeginArea(new Rect(10, 10, 460, Screen.height - 20));
            GUILayout.BeginVertical(_box, GUILayout.Width(440));

            GUILayout.Label("UI Stress Benchmark", _title);
            GUILayout.Label(_b.Status, _label);

            float cpu = _b.LiveCpuMs;
            float fps = cpu > 0.0001f ? 1000f / cpu : 0f;
            GUILayout.Label("frame: " + F(cpu) + " ms   (" + F0(fps) + " fps)", _label);

            if (_b.Metrics != null)
            {
                GUILayout.Label("main: " + F(_b.Metrics.LiveMainMs) + " ms   draws: " + _b.Metrics.LiveDrawCalls
                    + "   verts: " + _b.Metrics.LiveVerts, _label);
            }
            GUILayout.Label("graphics: " + _b.TotalGraphics + "   phase: " + _b.CurrentPhase, _label);

            GUILayout.Space(6);
            DrawControls();

            if (_b.Results.Count > 0)
            {
                GUILayout.Space(8);
                GUILayout.Label("Results", _title);
                for (int i = 0; i < _b.Results.Count; i++)
                {
                    var r = _b.Results[i];
                    GUILayout.Label(Pad(r.id.DisplayName(), 16) + " " + F(r.cpuMs.mean) + "ms  p95 " + F(r.cpuMs.p95)
                        + "  gc " + Bytes(r.gcBytes.mean), _label);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void DrawControls()
        {
            bool idle = !_b.IsRunning
                || _b.CurrentPhase == UIBenchmark.Phase.Idle
                || _b.CurrentPhase == UIBenchmark.Phase.Finished;

            if (idle)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Run Auto")) _b.StartAuto();
                if (GUILayout.Button("Run Manual")) _b.StartManual();
                if (_b.Results.Count > 0 && GUILayout.Button("Write CSV")) _b.WriteReportNow();
                GUILayout.EndHorizontal();
                return;
            }

            if (_b.runMode == RunMode.Manual && _b.CurrentPhase == UIBenchmark.Phase.ManualIdle)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Measure")) _b.RunManualMeasure();
                if (GUILayout.Button("Rebuild")) _b.RebuildScene();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("scenario:", _label, GUILayout.Width(70));
                if (GUILayout.Button("<")) _b.CycleScenarioManual(-1);
                GUILayout.Label(_b.CurrentScenario.DisplayName(), _label);
                if (GUILayout.Button(">")) _b.CycleScenarioManual(1);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("preset:", _label, GUILayout.Width(70));
                if (GUILayout.Button("<")) _b.CyclePreset(-1);
                GUILayout.Label(_b.config.preset.ToString(), _label);
                if (GUILayout.Button(">")) _b.CyclePreset(1);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Switch to Auto")) _b.StartAuto();
                if (_b.Results.Count > 0 && GUILayout.Button("Write CSV")) _b.WriteReportNow();
                GUILayout.EndHorizontal();
                return;
            }

            GUILayout.Label("running…", _label);
        }

        static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
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
            if (s.Length >= width) return s;
            return s + new string(' ', width - s.Length);
        }
    }
}
