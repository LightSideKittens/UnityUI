using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LightSide.UIBenchmark.Editor
{
    /// <summary>
    /// Editor entry points that spin up the benchmark. The rig (canvas, event system, camera) and the
    /// heavy UI are built at runtime by <see cref="UIBenchmark"/>, so the created object is intentionally
    /// bare in edit mode; press Play to see and measure it.
    /// </summary>
    public static class BenchmarkSceneCreator
    {
        const string Menu = "Tools/Light Side/UI Benchmark/";

        [MenuItem(Menu + "Create Benchmark Scene")]
        static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("UIBenchmark");
            go.AddComponent<UIBenchmark>();
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(scene);

            EditorUtility.DisplayDialog(
                "UI Stress Benchmark",
                "Empty benchmark scene created with a UIBenchmark object.\n\nPress Play to run. Set 'Build Label' on the component before each run, then diff the CSVs written to persistentDataPath/UIBenchmark.\n\nSave the scene if you want to keep it.",
                "OK");
        }

        [MenuItem(Menu + "Add Benchmark To Current Scene")]
        static void AddToCurrentScene()
        {
            var go = new GameObject("UIBenchmark");
            go.AddComponent<UIBenchmark>();
            Undo.RegisterCreatedObjectUndo(go, "Create UIBenchmark");
            Selection.activeGameObject = go;
        }

        [MenuItem(Menu + "Open Reports Folder")]
        static void OpenReports()
        {
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "UIBenchmark");
            System.IO.Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }
    }
}
