using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Validates Script property implementation against Scripts.txt
/// Unlike BiDi and LineBreak, there's no separate conformance test file,
/// so we validate directly against the source data.
/// </summary>
public sealed class ScriptConformanceRunner
{
    private readonly IUnicodeDataProvider dataProvider;

    public ScriptConformanceRunner(IUnicodeDataProvider dataProvider)
    {
        this.dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    /// <summary>
    /// Validate Script property against Scripts.txt content
    /// Format: 0000..001F    ; Common # Cc  [32] <control-0000>..<control-001F>
    /// </summary>
    public ScriptConformanceSummary RunTests(string scriptsFileContent, int maxFailuresToLog = 20)
    {
        var summary = new ScriptConformanceSummary();

        if (string.IsNullOrEmpty(scriptsFileContent))
        {
            summary.sampleFailures = "Scripts.txt content is empty or null.";
            return summary;
        }

        var failures = new List<ScriptConformanceFailure>();

        using var reader = new System.IO.StringReader(scriptsFileContent);
        string? line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            // Strip comment
            int hashIndex = line.IndexOf('#');
            if (hashIndex >= 0)
                line = line.Substring(0, hashIndex);

            line = line.Trim();
            if (line.Length == 0)
                continue;

            // Parse line: XXXX..YYYY ; Script_Name or XXXX ; Script_Name
            string[] parts = line.Split(';');
            if (parts.Length < 2)
            {
                summary.skippedTests++;
                continue;
            }

            string rangePart = parts[0].Trim();
            string scriptPart = parts[1].Trim();

            if (!TryParseScript(scriptPart, out UnicodeScript expectedScript))
            {
                summary.skippedTests++;
                AddFailure(failures, lineNumber, line, $"Unknown script: {scriptPart}", maxFailuresToLog);
                continue;
            }

            if (!TryParseRange(rangePart, out int rangeStart, out int rangeEnd))
            {
                summary.skippedTests++;
                AddFailure(failures, lineNumber, line, $"Invalid range: {rangePart}", maxFailuresToLog);
                continue;
            }

            // Test sample points in the range (not all, for performance)
            int[] samplePoints = GetSamplePoints(rangeStart, rangeEnd);

            foreach (int cp in samplePoints)
            {
                summary.totalEvaluatedTests++;

                UnicodeScript actualScript;
                try
                {
                    actualScript = dataProvider.GetScript(cp);
                }
                catch (Exception ex)
                {
                    summary.failedTests++;
                    AddFailure(failures, lineNumber, line, 
                        $"Exception at U+{cp:X4}: {ex.Message}", maxFailuresToLog);
                    continue;
                }

                if (actualScript != expectedScript)
                {
                    summary.failedTests++;
                    AddFailure(failures, lineNumber, line,
                        $"U+{cp:X4}: expected {expectedScript}, got {actualScript}", maxFailuresToLog);
                    continue;
                }

                summary.passedTests++;
            }
        }

        summary.sampleFailures = BuildSampleFailuresText(failures, maxFailuresToLog);
        return summary;
    }

    private bool TryParseRange(string rangePart, out int rangeStart, out int rangeEnd)
    {
        rangeStart = 0;
        rangeEnd = 0;

        int dotsIndex = rangePart.IndexOf("..", StringComparison.Ordinal);
        if (dotsIndex >= 0)
        {
            string startHex = rangePart.Substring(0, dotsIndex);
            string endHex = rangePart.Substring(dotsIndex + 2);

            if (!int.TryParse(startHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rangeStart))
                return false;
            if (!int.TryParse(endHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rangeEnd))
                return false;
        }
        else
        {
            if (!int.TryParse(rangePart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rangeStart))
                return false;
            rangeEnd = rangeStart;
        }

        return rangeStart >= 0 && rangeEnd >= rangeStart;
    }

    private bool TryParseScript(string scriptPart, out UnicodeScript script)
    {
        // Convert to PascalCase (e.g., "Old_Italic" -> "OldItalic")
        string[] parts = scriptPart.Trim().Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (string part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1).ToLowerInvariant());
        }

        string enumName = sb.ToString();
        return Enum.TryParse(enumName, out script);
    }

    /// <summary>
    /// Get sample codepoints from a range (for large ranges, test boundaries + samples)
    /// </summary>
    private int[] GetSamplePoints(int start, int end)
    {
        int rangeSize = end - start + 1;

        if (rangeSize <= 10)
        {
            // Test all points
            int[] all = new int[rangeSize];
            for (int i = 0; i < rangeSize; i++)
                all[i] = start + i;
            return all;
        }

        // For large ranges, test boundaries + some samples
        var samples = new List<int>
        {
            start,          // First
            end,            // Last
            start + 1,      // Second
            end - 1,        // Second to last
        };

        // Add middle point
        samples.Add(start + rangeSize / 2);

        // Add some random-ish samples
        int step = rangeSize / 5;
        for (int i = 1; i < 5; i++)
        {
            int sample = start + i * step;
            if (!samples.Contains(sample))
                samples.Add(sample);
        }

        return samples.ToArray();
    }

    private static void AddFailure(List<ScriptConformanceFailure> failures, int lineNumber,
        string rawInput, string message, int maxFailuresToLog)
    {
        if (failures.Count >= maxFailuresToLog)
            return;

        failures.Add(new ScriptConformanceFailure
        {
            lineNumber = lineNumber,
            rawInput = rawInput,
            message = message
        });
    }

    private static string BuildSampleFailuresText(List<ScriptConformanceFailure> failures, int maxFailuresToLog)
    {
        if (failures == null || failures.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        int count = Math.Min(failures.Count, maxFailuresToLog);

        for (int i = 0; i < count; i++)
        {
            var failure = failures[i];
            sb.Append("- Line ").Append(failure.lineNumber).Append(": ").Append(failure.message).AppendLine();
            sb.Append("  Input: ").Append(failure.rawInput).AppendLine();
        }

        return sb.ToString();
    }
}

internal sealed class ScriptConformanceFailure
{
    public int lineNumber;
    public string? rawInput;
    public string? message;
}

public struct ScriptConformanceSummary
{
    public int totalEvaluatedTests;
    public int passedTests;
    public int failedTests;
    public int skippedTests;
    public string? sampleFailures;
}