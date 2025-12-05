#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UnicodeDataGeneratorWindow : EditorWindow
{
    [Serializable]
    private class TestData
    {
        [SerializeField] private int maxFailuresToLog = 10;
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

    [Header("Source Files")]
    [SerializeField] private TextAsset derivedBidiClassAsset;
    [SerializeField] private TextAsset derivedJoiningTypeAsset;
    [SerializeField] private TextAsset arabicShapingAsset;
    [SerializeField] private TextAsset bidiBracketsAsset;
    [SerializeField] private TextAsset bidiMirroringAsset;
    [SerializeField] private TextAsset scriptsAsset;
    [SerializeField] private TextAsset lineBreakAsset;

    [Header("Output")]
    [SerializeField] private DefaultAsset outputFolder;
    [SerializeField] private string outputFileName = "UnicodeData.bytes";
    [SerializeField] private bool useFormatV2 = true;

    [Header("Testing")]
    [SerializeField] private TestData testing = new();

    [MenuItem("Tools/RTL/Unicode Data Generator")]
    public static void ShowWindow()
    {
        UnicodeDataGeneratorWindow window = GetWindow<UnicodeDataGeneratorWindow>();
        window.titleContent = new GUIContent("Unicode Data Generator");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unicode Data Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Source Files (Required)", EditorStyles.boldLabel);
        
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
        EditorGUILayout.LabelField("Source Files (Format V2)", EditorStyles.boldLabel);

        scriptsAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Scripts.txt",
            scriptsAsset,
            typeof(TextAsset),
            false);

        lineBreakAsset = (TextAsset)EditorGUILayout.ObjectField(
            "LineBreak.txt",
            lineBreakAsset,
            typeof(TextAsset),
            false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder",
            outputFolder,
            typeof(DefaultAsset),
            false);

        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);
        useFormatV2 = EditorGUILayout.Toggle("Use Format V2 (Script + LineBreak)", useFormatV2);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(this);
        }

        EditorGUILayout.Space();

        bool canGenerate = derivedBidiClassAsset != null &&
                          derivedJoiningTypeAsset != null &&
                          arabicShapingAsset != null &&
                          bidiBracketsAsset != null &&
                          bidiMirroringAsset != null &&
                          outputFolder != null;

        if (useFormatV2)
        {
            canGenerate = canGenerate && scriptsAsset != null && lineBreakAsset != null;
        }

        EditorGUI.BeginDisabledGroup(!canGenerate);
        if (GUILayout.Button("Generate Unicode Data", GUILayout.Height(30)))
        {
            GenerateUnicodeData();
        }
        EditorGUI.EndDisabledGroup();

        if (!canGenerate)
        {
            EditorGUILayout.HelpBox(
                useFormatV2 
                    ? "Assign all required source files and output folder to generate."
                    : "Assign all required source files (excluding Scripts.txt and LineBreak.txt) and output folder.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
        testing.OnGui();
    }

    private void GenerateUnicodeData()
    {
        try
        {
            string folderPath = AssetDatabase.GetAssetPath(outputFolder);
            string outputPath = Path.Combine(folderPath, outputFileName);

            // Create temp files
            string tempDir = Path.Combine(Application.temporaryCachePath, "UnicodeGen");
            Directory.CreateDirectory(tempDir);

            string derivedBidiPath = SaveTempFile(tempDir, "DerivedBidiClass.txt", derivedBidiClassAsset);
            string derivedJoiningPath = SaveTempFile(tempDir, "DerivedJoiningType.txt", derivedJoiningTypeAsset);
            string arabicShapingPath = SaveTempFile(tempDir, "ArabicShaping.txt", arabicShapingAsset);
            string bidiBracketsPath = SaveTempFile(tempDir, "BidiBrackets.txt", bidiBracketsAsset);
            string bidiMirroringPath = SaveTempFile(tempDir, "BidiMirroring.txt", bidiMirroringAsset);

            // Build data
            var builder = new UnicodeDataBuilder();
            builder.LoadDerivedBidiClass(derivedBidiPath);
            builder.LoadDerivedJoiningType(derivedJoiningPath);
            builder.LoadArabicShaping(arabicShapingPath);

            var ranges = builder.BuildRangeEntries();
            var mirrors = UnicodeDataBuilder.BuildMirrorEntries(bidiMirroringPath);
            var brackets = UnicodeDataBuilder.BuildBracketEntries(bidiBracketsPath);

            // Unicode version (17.0.0 = 0x110000)
            int unicodeVersion = 0x110000;

            if (useFormatV2 && scriptsAsset != null && lineBreakAsset != null)
            {
                string scriptsPath = SaveTempFile(tempDir, "Scripts.txt", scriptsAsset);
                string lineBreakPath = SaveTempFile(tempDir, "LineBreak.txt", lineBreakAsset);

                builder.LoadScripts(scriptsPath);
                builder.LoadLineBreak(lineBreakPath);

                var scripts = builder.BuildScriptRangeEntries();
                var lineBreaks = builder.BuildLineBreakRangeEntries();

                UnicodeBinaryWriter.WriteBinary(
                    outputPath, ranges, mirrors, brackets, scripts, lineBreaks, unicodeVersion);

                Debug.Log($"Generated Unicode data (Format V2) with {ranges.Count} ranges, " +
                          $"{mirrors.Count} mirrors, {brackets.Count} brackets, " +
                          $"{scripts.Count} script ranges, {lineBreaks.Count} line break ranges.");
            }
            else
            {
                UnicodeBinaryWriter.WriteBinaryV1(
                    outputPath, ranges, mirrors, brackets, unicodeVersion);

                Debug.Log($"Generated Unicode data (Format V1) with {ranges.Count} ranges, " +
                          $"{mirrors.Count} mirrors, {brackets.Count} brackets.");
            }

            // Cleanup temp files
            Directory.Delete(tempDir, true);

            AssetDatabase.Refresh();
            Debug.Log($"Unicode data saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to generate Unicode data: {ex}");
        }
    }

    private string SaveTempFile(string dir, string name, TextAsset asset)
    {
        string path = Path.Combine(dir, name);
        File.WriteAllText(path, asset.text);
        return path;
    }
}
#endif
