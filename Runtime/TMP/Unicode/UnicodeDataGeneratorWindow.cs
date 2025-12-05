#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UnicodeDataGeneratorWindow : EditorWindow
{
    [Serializable]
    private class TestData
    {
        [SerializeField] private int maxFailuresToLog;
        [SerializeField] private TextAsset unicodeDataAsset;
        [SerializeField] private TextAsset bidiCharacterTestAsset;

        public void RunBidiCharacterTests()
        {
            if (unicodeDataAsset == null || bidiCharacterTestAsset == null)
            {
                Debug.LogError("Assign unicodeDataAsset and bidiCharacterTestAsset.");
                return;
            }

            try
            {
                BinaryUnicodeDataProvider provider = new BinaryUnicodeDataProvider(unicodeDataAsset.bytes);
                BidiEngine engine = new BidiEngine(provider);

                BidiConformanceRunner runner = new BidiConformanceRunner(engine);
                BidiConformanceSummary summary =
                    runner.RunBidiCharacterTests(bidiCharacterTestAsset.text, maxFailuresToLog);

                var log = $"BidiCharacterTest done. Passed={summary.passedTests}, " +
                          $"Failed={summary.failedTests}, Skipped={summary.skippedTests}." +
                          $"Sample Failures:\n{summary.sampleFailures}";

                Debug.Log(log);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "UnicodeTestResults.txt"), log);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while running BidiCharacterTest: {ex}");
            }
        }

        public void OnGui()
        {
            maxFailuresToLog = EditorGUILayout.IntField("Max Failures To Log", maxFailuresToLog);

            unicodeDataAsset = (TextAsset)EditorGUILayout.ObjectField(
                "UnicodeData.bytes",
                unicodeDataAsset,
                typeof(TextAsset),
                false);

            bidiCharacterTestAsset = (TextAsset)EditorGUILayout.ObjectField(
                "BidiCharacterTest.txt",
                bidiCharacterTestAsset,
                typeof(TextAsset),
                false);

            if (GUILayout.Button("Run Test"))
            {
                RunBidiCharacterTests();
            }
        }
    }

    [SerializeField] private TextAsset derivedBidiClassAsset;
    [SerializeField] private TextAsset derivedJoiningTypeAsset;
    [SerializeField] private TextAsset arabicShapingAsset;
    [SerializeField] private TextAsset bidiBracketsAsset;
    [SerializeField] private TextAsset bidiMirroringAsset;

    [SerializeField] private TestData testing = new();

    [SerializeField] private DefaultAsset outputFolder;

    [SerializeField] private string outputFileName = "UnicodeData.bytes";

    [MenuItem("Tools/RTL/Unicode Data Generator")]
    public static void ShowWindow()
    {
        UnicodeDataGeneratorWindow window = GetWindow<UnicodeDataGeneratorWindow>();
        window.titleContent = new GUIContent("Unicode Data Generator");
        window.minSize = new Vector2(450, 200);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unicode Data Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        derivedBidiClassAsset = (TextAsset)EditorGUILayout.ObjectField(
            "DerivedBidiClass.txt",
            derivedBidiClassAsset,
            typeof(TextAsset),
            false);

        derivedJoiningTypeAsset = (TextAsset)EditorGUILayout.ObjectField(
            "DerivedJoiningType.txt",
            derivedJoiningTypeAsset,
            typeof(TextAsset),
            false);

        arabicShapingAsset = (TextAsset)EditorGUILayout.ObjectField(
            "ArabicShaping.txt",
            arabicShapingAsset,
            typeof(TextAsset),
            false);

        bidiBracketsAsset = (TextAsset)EditorGUILayout.ObjectField(
            "BidiBrackets.txt",
            bidiBracketsAsset,
            typeof(TextAsset),
            false);

        bidiMirroringAsset = (TextAsset)EditorGUILayout.ObjectField(
            "BidiMirroring.txt",
            bidiMirroringAsset,
            typeof(TextAsset),
            false);


        EditorGUILayout.Space();

        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder (Assets/*)",
            outputFolder,
            typeof(DefaultAsset),
            false);

        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        if (string.IsNullOrWhiteSpace(outputFileName))
        {
            outputFileName = "UnicodeData.bin";
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginFoldoutHeaderGroup(true, "Testing");
        testing ??= new();
        testing.OnGui();
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        GUI.enabled = derivedBidiClassAsset != null &&
                      derivedJoiningTypeAsset != null &&
                      arabicShapingAsset != null &&
                      bidiBracketsAsset != null &&
                      bidiMirroringAsset != null;

        if (GUILayout.Button("Generate Unicode Binary", GUILayout.Height(30)))
        {
            Generate();
        }

        GUI.enabled = true;
    }

    private void Generate()
    {
        try
        {
            string projectRoot = GetProjectRootPath();

            string derivedBidiClassPath = GetAbsolutePathFromTextAsset(derivedBidiClassAsset, projectRoot);
            string derivedJoiningTypePath = GetAbsolutePathFromTextAsset(derivedJoiningTypeAsset, projectRoot);
            string arabicShapingPath = GetAbsolutePathFromTextAsset(arabicShapingAsset, projectRoot);

            string bidiBracketsPath = GetAbsolutePathFromTextAsset(bidiBracketsAsset, projectRoot);
            string bidiMirroringPath = GetAbsolutePathFromTextAsset(bidiMirroringAsset, projectRoot);

            if (string.IsNullOrEmpty(derivedBidiClassPath) ||
                string.IsNullOrEmpty(derivedJoiningTypePath) ||
                string.IsNullOrEmpty(arabicShapingPath) ||
                string.IsNullOrEmpty(bidiBracketsPath) ||
                string.IsNullOrEmpty(bidiMirroringPath))
            {
                Debug.LogError("UnicodeDataGenerator: one or more TextAssets do not have valid asset paths.");
                return;
            }


            string outputFolderAssetPath = GetOutputFolderAssetPath();
            if (string.IsNullOrEmpty(outputFolderAssetPath))
            {
                Debug.LogError("UnicodeDataGenerator: output folder is invalid.");
                return;
            }

            string outputAbsoluteFolder = Path.Combine(projectRoot, outputFolderAssetPath.Replace('\\', '/'));
            if (!Directory.Exists(outputAbsoluteFolder))
            {
                Directory.CreateDirectory(outputAbsoluteFolder);
            }

            string safeFileName = outputFileName.Trim();
            if (string.IsNullOrEmpty(safeFileName))
            {
                safeFileName = "UnicodeData.bin";
            }

            string outputPath = Path.Combine(outputAbsoluteFolder, safeFileName);

            UnicodeDataBuilder builder = new UnicodeDataBuilder();
            builder.LoadDerivedBidiClass(derivedBidiClassPath);
            builder.LoadDerivedJoiningType(derivedJoiningTypePath);
            builder.LoadArabicShaping(arabicShapingPath);

            List<RangeEntry> ranges = builder.BuildRangeEntries();

            List<MirrorEntry> mirrors = UnicodeDataBuilder.BuildMirrorEntries(bidiMirroringPath);
            List<BracketEntry> brackets = UnicodeDataBuilder.BuildBracketEntries(bidiBracketsPath);

            UnicodeBinaryWriter.WriteBinary(
                outputPath,
                ranges,
                mirrors,
                brackets,
                unicodeVersionRaw: 0);

            Debug.Log(
                $"UnicodeDataGenerator: binary generated.\n" +
                $"Output: {outputPath}\n" +
                $"Ranges: {ranges.Count}");

            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"UnicodeDataGenerator: failed to generate Unicode binary.\n{ex}");
        }
    }

    private static string GetProjectRootPath()
    {
        string dataPath = Application.dataPath.Replace('\\', '/');
        const string assetsFolderName = "Assets";

        if (dataPath.EndsWith(assetsFolderName, StringComparison.Ordinal))
        {
            return dataPath.Substring(0, dataPath.Length - assetsFolderName.Length);
        }

        return Directory.GetParent(dataPath)?.FullName?.Replace('\\', '/') ?? dataPath;
    }

    private static string GetAbsolutePathFromTextAsset(TextAsset asset, string projectRoot)
    {
        if (asset == null)
        {
            return string.Empty;
        }

        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        assetPath = assetPath.Replace('\\', '/');
        return Path.Combine(projectRoot, assetPath);
    }

    private string GetOutputFolderAssetPath()
    {
        if (outputFolder == null)
        {
            return "Assets";
        }

        string assetPath = AssetDatabase.GetAssetPath(outputFolder);
        if (string.IsNullOrEmpty(assetPath))
        {
            return "Assets";
        }

        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            Debug.LogWarning(
                $"UnicodeDataGenerator: selected output asset is not a folder, using 'Assets' instead. ({assetPath})");
            return "Assets";
        }

        return assetPath;
    }
}
#endif