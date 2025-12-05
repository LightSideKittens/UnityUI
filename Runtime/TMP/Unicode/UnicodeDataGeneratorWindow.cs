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
        [SerializeField] private TextAsset lineBreakTestAsset;
        [SerializeField] private TextAsset scriptsAsset;

        public void RunBidiCharacterTests()
        {
            if (unicodeDataAsset == null || bidiCharacterTestAsset == null)
            {
                Debug.LogError("Assign unicodeDataAsset and bidiCharacterTestAsset.");
                return;
            }

            try
            {
                var provider = new BinaryUnicodeDataProvider(unicodeDataAsset.bytes);
                var engine = new BidiEngine(provider);
                var runner = new BidiConformanceRunner(engine);
                var summary = runner.RunBidiCharacterTests(bidiCharacterTestAsset.text, maxFailuresToLog);

                LogSummary("BidiCharacterTest", summary.passedTests, summary.failedTests, 
                    summary.skippedTests, summary.sampleFailures);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while running BidiCharacterTest: {ex}");
            }
        }

        public void RunLineBreakTests()
        {
            if (unicodeDataAsset == null || lineBreakTestAsset == null)
            {
                Debug.LogError("Assign unicodeDataAsset and lineBreakTestAsset.");
                return;
            }

            try
            {
                var provider = new BinaryUnicodeDataProvider(unicodeDataAsset.bytes);
                var runner = new LineBreakConformanceRunner(provider);
                var summary = runner.RunTests(lineBreakTestAsset.text, maxFailuresToLog);

                LogSummary("LineBreakTest", summary.passedTests, summary.failedTests,
                    summary.skippedTests, summary.sampleFailures);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while running LineBreakTest: {ex}");
            }
        }

        public void RunScriptTests()
        {
            if (unicodeDataAsset == null || scriptsAsset == null)
            {
                Debug.LogError("Assign unicodeDataAsset and scriptsAsset.");
                return;
            }

            try
            {
                var provider = new BinaryUnicodeDataProvider(unicodeDataAsset.bytes);
                var runner = new ScriptConformanceRunner(provider);
                var summary = runner.RunTests(scriptsAsset.text, maxFailuresToLog);

                LogSummary("ScriptTest", summary.passedTests, summary.failedTests,
                    summary.skippedTests, summary.sampleFailures);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while running ScriptTest: {ex}");
            }
        }

        public void RunAllTests()
        {
            if (unicodeDataAsset == null)
            {
                Debug.LogError("Assign unicodeDataAsset first.");
                return;
            }

            Debug.Log("=== Running All Unicode Conformance Tests ===");

            if (bidiCharacterTestAsset != null)
                RunBidiCharacterTests();
            else
                Debug.LogWarning("Skipping BiDi tests: bidiCharacterTestAsset not assigned");

            if (lineBreakTestAsset != null)
                RunLineBreakTests();
            else
                Debug.LogWarning("Skipping LineBreak tests: lineBreakTestAsset not assigned");

            if (scriptsAsset != null)
                RunScriptTests();
            else
                Debug.LogWarning("Skipping Script tests: scriptsAsset not assigned");

            Debug.Log("=== All Tests Complete ===");
        }

        private void LogSummary(string testName, int passed, int failed, int skipped, string? sampleFailures)
        {
            string log = $"{testName}: Passed={passed}, Failed={failed}, Skipped={skipped}";
            
            if (failed > 0 && !string.IsNullOrEmpty(sampleFailures))
            {
                log += $"\nSample Failures:\n{sampleFailures}";
                Debug.LogWarning(log);
            }
            else
            {
                Debug.Log(log);
            }

            // Save to file
            string filePath = Path.Combine(Application.persistentDataPath, $"{testName}Results.txt");
            File.WriteAllText(filePath, log);
        }

        public void OnGui()
        {
            maxFailuresToLog = EditorGUILayout.IntField("Max Failures To Log", maxFailuresToLog);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Data Assets", EditorStyles.miniBoldLabel);

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

            lineBreakTestAsset = (TextAsset)EditorGUILayout.ObjectField(
                "LineBreakTest.txt",
                lineBreakTestAsset,
                typeof(TextAsset),
                false);

            scriptsAsset = (TextAsset)EditorGUILayout.ObjectField(
                "Scripts.txt",
                scriptsAsset,
                typeof(TextAsset),
                false);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Run All Tests"))
                RunAllTests();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(unicodeDataAsset == null || bidiCharacterTestAsset == null);
            if (GUILayout.Button("BiDi"))
                RunBidiCharacterTests();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(unicodeDataAsset == null || lineBreakTestAsset == null);
            if (GUILayout.Button("LineBreak"))
                RunLineBreakTests();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(unicodeDataAsset == null || scriptsAsset == null);
            if (GUILayout.Button("Script"))
                RunScriptTests();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
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
    [SerializeField] private TextAsset emojiDataAsset;
    [SerializeField] private TextAsset generalCategoryAsset;
    [SerializeField] private TextAsset eastAsianWidthAsset;

    [Header("Output")]
    [SerializeField] private DefaultAsset outputFolder;
    [SerializeField] private string outputFileName = "UnicodeData.bytes";
    [SerializeField] private bool useFormatV4 = true;

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
        EditorGUILayout.LabelField("Source Files (Format V2+)", EditorStyles.boldLabel);

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
        EditorGUILayout.LabelField("Source Files (Format V3+)", EditorStyles.boldLabel);

        emojiDataAsset = (TextAsset)EditorGUILayout.ObjectField(
            "emoji-data.txt",
            emojiDataAsset,
            typeof(TextAsset),
            false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Source Files (Format V4)", EditorStyles.boldLabel);

        generalCategoryAsset = (TextAsset)EditorGUILayout.ObjectField(
            "DerivedGeneralCategory.txt",
            generalCategoryAsset,
            typeof(TextAsset),
            false);

        eastAsianWidthAsset = (TextAsset)EditorGUILayout.ObjectField(
            "EastAsianWidth.txt",
            eastAsianWidthAsset,
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
        useFormatV4 = EditorGUILayout.Toggle("Use Format V4 (Full Unicode Properties)", useFormatV4);

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

        if (useFormatV4)
        {
            canGenerate = canGenerate && scriptsAsset != null && lineBreakAsset != null && 
                         emojiDataAsset != null && generalCategoryAsset != null && eastAsianWidthAsset != null;
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
                useFormatV4 
                    ? "Assign all required source files including DerivedGeneralCategory.txt and EastAsianWidth.txt and output folder to generate."
                    : "Assign all required source files (excluding Scripts.txt, LineBreak.txt, emoji-data.txt, etc.) and output folder.",
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

            if (useFormatV4 && scriptsAsset != null && lineBreakAsset != null && 
                emojiDataAsset != null && generalCategoryAsset != null && eastAsianWidthAsset != null)
            {
                string scriptsPath = SaveTempFile(tempDir, "Scripts.txt", scriptsAsset);
                string lineBreakPath = SaveTempFile(tempDir, "LineBreak.txt", lineBreakAsset);
                string emojiDataPath = SaveTempFile(tempDir, "emoji-data.txt", emojiDataAsset);
                string generalCategoryPath = SaveTempFile(tempDir, "DerivedGeneralCategory.txt", generalCategoryAsset);
                string eastAsianWidthPath = SaveTempFile(tempDir, "EastAsianWidth.txt", eastAsianWidthAsset);

                builder.LoadScripts(scriptsPath);
                builder.LoadLineBreak(lineBreakPath);
                builder.LoadEmojiData(emojiDataPath);
                builder.LoadGeneralCategory(generalCategoryPath);
                builder.LoadEastAsianWidth(eastAsianWidthPath);

                var scripts = builder.BuildScriptRangeEntries();
                var lineBreaks = builder.BuildLineBreakRangeEntries();
                var extendedPictographics = builder.BuildExtendedPictographicRangeEntries();
                var generalCategories = builder.BuildGeneralCategoryRangeEntries();
                var eastAsianWidths = builder.BuildEastAsianWidthRangeEntries();

                UnicodeBinaryWriter.WriteBinary(
                    outputPath, ranges, mirrors, brackets, scripts, lineBreaks, 
                    extendedPictographics, generalCategories, eastAsianWidths, unicodeVersion);

                Debug.Log($"Generated Unicode data (Format V4) with {ranges.Count} ranges, " +
                          $"{mirrors.Count} mirrors, {brackets.Count} brackets, " +
                          $"{scripts.Count} script ranges, {lineBreaks.Count} line break ranges, " +
                          $"{extendedPictographics.Count} Extended_Pictographic ranges, " +
                          $"{generalCategories.Count} GeneralCategory ranges, " +
                          $"{eastAsianWidths.Count} EastAsianWidth ranges.");
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
