#region Offline структуры

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

struct UnicodeProps
{
    public BidiClass bidiClass;
    public JoiningType joiningType;
    public JoiningGroup joiningGroup;
    public UnicodeScript script;
    public LineBreakClass lineBreakClass;
    public bool extendedPictographic;
    public GeneralCategory generalCategory;
    public EastAsianWidth eastAsianWidth;
}

public class UnicodeDataBuilder
{
    const int MaxCodePoint = 0x10FFFF;
    const int ScalarCount = MaxCodePoint + 1;

    readonly UnicodeProps[] props;

    public UnicodeDataBuilder()
    {
        props = new UnicodeProps[ScalarCount];
        InitializeDefaults();
    }

    void InitializeDefaults()
    {
        for (int cp = 0; cp < ScalarCount; cp++)
        {
            props[cp].bidiClass = BidiClass.LeftToRight;
            props[cp].joiningType = JoiningType.NonJoining;
            props[cp].joiningGroup = JoiningGroup.NoJoiningGroup;
            props[cp].script = UnicodeScript.Unknown;
            props[cp].lineBreakClass = LineBreakClass.XX;
            props[cp].generalCategory = GeneralCategory.Cn; // Not assigned
            props[cp].eastAsianWidth = EastAsianWidth.N;    // Neutral
        }
    }

    public void LoadDerivedBidiClass(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string classPart = semi[1].Trim();

            if (codeRangePart.Length == 0 || classPart.Length == 0)
                continue;

            BidiClass bidiClass = ParseBidiClass(classPart);

            ParseRangeAndApply(codeRangePart, cp => props[cp].bidiClass = bidiClass);
        }
    }

    public void LoadArabicShaping(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 4)
                continue;

            string codePart = semi[0].Trim();
            string joiningTypePart = semi[2].Trim();
            string joiningGroupPart = semi[3].Trim();

            if (codePart.Length == 0 || joiningTypePart.Length == 0 || joiningGroupPart.Length == 0)
                continue;

            int codePoint = ParseHexCodePoint(codePart);
            if (codePoint < 0 || codePoint > MaxCodePoint)
                continue;

            JoiningType joiningType = ParseJoiningType(joiningTypePart);
            JoiningGroup joiningGroup = ParseJoiningGroup(joiningGroupPart);

            props[codePoint].joiningType = joiningType;
            props[codePoint].joiningGroup = joiningGroup;
        }
    }


    public void LoadDerivedJoiningType(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string typePart = semi[1].Trim();

            if (codeRangePart.Length == 0 || typePart.Length == 0)
                continue;

            JoiningType joiningType = ParseJoiningType(typePart);

            ParseRangeAndApply(codeRangePart, cp => props[cp].joiningType = joiningType);
        }
    }

    public void LoadScripts(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string scriptPart = semi[1].Trim();

            if (codeRangePart.Length == 0 || scriptPart.Length == 0)
                continue;

            UnicodeScript script = ParseScript(scriptPart);

            ParseRangeAndApply(codeRangePart, cp => props[cp].script = script);
        }
    }

    public void LoadLineBreak(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string lbcPart = semi[1].Trim();

            if (codeRangePart.Length == 0 || lbcPart.Length == 0)
                continue;

            LineBreakClass lbc = ParseLineBreakClass(lbcPart);

            ParseRangeAndApply(codeRangePart, cp => props[cp].lineBreakClass = lbc);
        }
    }

    /// <summary>
    /// Load emoji-data.txt to extract Extended_Pictographic property
    /// </summary>
    public void LoadEmojiData(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string propertyPart = semi[1].Trim();

            if (codeRangePart.Length == 0 || propertyPart.Length == 0)
                continue;

            // Only interested in Extended_Pictographic property
            if (propertyPart != "Extended_Pictographic")
                continue;

            ParseRangeAndApply(codeRangePart, cp => props[cp].extendedPictographic = true);
        }
    }

    /// <summary>
    /// Load DerivedGeneralCategory.txt to extract General_Category property
    /// Format: 0000..001F    ; Cc # [32] <control-0000>..<control-001F>
    /// </summary>
    public void LoadGeneralCategory(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string gcPart = semi[1].Trim();

            if (codeRangePart.Length == 0 || gcPart.Length == 0)
                continue;

            GeneralCategory gc = ParseGeneralCategory(gcPart);
            ParseRangeAndApply(codeRangePart, cp => props[cp].generalCategory = gc);
        }
    }

    /// <summary>
    /// Load EastAsianWidth.txt to extract East_Asian_Width property
    /// Format: 0000..001F;N  # Cc    [32] <control-0000>..<control-001F>
    /// </summary>
    public void LoadEastAsianWidth(string path)
    {
        using var reader = new StreamReader(path);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] semi = line.Split(';');
            if (semi.Length < 2)
                continue;

            string codeRangePart = semi[0].Trim();
            string eawPart = semi[1].Trim();

            if (codeRangePart.Length == 0 || eawPart.Length == 0)
                continue;

            EastAsianWidth eaw = ParseEastAsianWidth(eawPart);
            ParseRangeAndApply(codeRangePart, cp => props[cp].eastAsianWidth = eaw);
        }
    }

    private static GeneralCategory ParseGeneralCategory(string value)
    {
        return value switch
        {
            "Lu" => GeneralCategory.Lu,
            "Ll" => GeneralCategory.Ll,
            "Lt" => GeneralCategory.Lt,
            "Lm" => GeneralCategory.Lm,
            "Lo" => GeneralCategory.Lo,
            "Mn" => GeneralCategory.Mn,
            "Mc" => GeneralCategory.Mc,
            "Me" => GeneralCategory.Me,
            "Nd" => GeneralCategory.Nd,
            "Nl" => GeneralCategory.Nl,
            "No" => GeneralCategory.No,
            "Pc" => GeneralCategory.Pc,
            "Pd" => GeneralCategory.Pd,
            "Ps" => GeneralCategory.Ps,
            "Pe" => GeneralCategory.Pe,
            "Pi" => GeneralCategory.Pi,
            "Pf" => GeneralCategory.Pf,
            "Po" => GeneralCategory.Po,
            "Sm" => GeneralCategory.Sm,
            "Sc" => GeneralCategory.Sc,
            "Sk" => GeneralCategory.Sk,
            "So" => GeneralCategory.So,
            "Zs" => GeneralCategory.Zs,
            "Zl" => GeneralCategory.Zl,
            "Zp" => GeneralCategory.Zp,
            "Cc" => GeneralCategory.Cc,
            "Cf" => GeneralCategory.Cf,
            "Cs" => GeneralCategory.Cs,
            "Co" => GeneralCategory.Co,
            "Cn" => GeneralCategory.Cn,
            _ => GeneralCategory.Cn
        };
    }

    private static EastAsianWidth ParseEastAsianWidth(string value)
    {
        return value switch
        {
            "N" => EastAsianWidth.N,
            "A" => EastAsianWidth.A,
            "H" => EastAsianWidth.H,
            "W" => EastAsianWidth.W,
            "F" => EastAsianWidth.F,
            "Na" => EastAsianWidth.Na,
            _ => EastAsianWidth.N
        };
    }

    private void ParseRangeAndApply(string codeRangePart, Action<int> apply)
    {
        int rangeStart, rangeEnd;

        int dotsIndex = codeRangePart.IndexOf("..", StringComparison.Ordinal);
        if (dotsIndex >= 0)
        {
            string startHex = codeRangePart.Substring(0, dotsIndex);
            string endHex = codeRangePart.Substring(dotsIndex + 2);

            rangeStart = ParseHexCodePoint(startHex);
            rangeEnd = ParseHexCodePoint(endHex);
        }
        else
        {
            rangeStart = ParseHexCodePoint(codeRangePart);
            rangeEnd = rangeStart;
        }

        if (rangeStart < 0 || rangeEnd < 0 || rangeStart > rangeEnd)
            return;

        if (rangeEnd > MaxCodePoint)
            rangeEnd = MaxCodePoint;

        for (int cp = rangeStart; cp <= rangeEnd; cp++)
        {
            apply(cp);
        }
    }

    static string StripComment(string line)
    {
        int hashIndex = line.IndexOf('#');
        if (hashIndex >= 0)
            line = line.Substring(0, hashIndex);
        return line.Trim();
    }

    static int ParseHexCodePoint(string hex)
    {
        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
            return value;
        return -1;
    }

    static BidiClass ParseBidiClass(string value)
    {
        return value switch
        {
            "L" => BidiClass.LeftToRight,
            "R" => BidiClass.RightToLeft,
            "AL" => BidiClass.ArabicLetter,
            "EN" => BidiClass.EuropeanNumber,
            "ES" => BidiClass.EuropeanSeparator,
            "ET" => BidiClass.EuropeanTerminator,
            "AN" => BidiClass.ArabicNumber,
            "CS" => BidiClass.CommonSeparator,
            "NSM" => BidiClass.NonspacingMark,
            "BN" => BidiClass.BoundaryNeutral,
            "B" => BidiClass.ParagraphSeparator,
            "S" => BidiClass.SegmentSeparator,
            "WS" => BidiClass.WhiteSpace,
            "ON" => BidiClass.OtherNeutral,
            "LRE" => BidiClass.LeftToRightEmbedding,
            "LRO" => BidiClass.LeftToRightOverride,
            "RLE" => BidiClass.RightToLeftEmbedding,
            "RLO" => BidiClass.RightToLeftOverride,
            "PDF" => BidiClass.PopDirectionalFormat,
            "LRI" => BidiClass.LeftToRightIsolate,
            "RLI" => BidiClass.RightToLeftIsolate,
            "FSI" => BidiClass.FirstStrongIsolate,
            "PDI" => BidiClass.PopDirectionalIsolate,
            _ => throw new InvalidDataException($"Unknown Bidi_Class value '{value}'.")
        };
    }

    static JoiningType ParseJoiningType(string value)
    {
        return value switch
        {
            "U" => JoiningType.NonJoining,
            "T" => JoiningType.Transparent,
            "C" => JoiningType.JoinCausing,
            "L" => JoiningType.LeftJoining,
            "R" => JoiningType.RightJoining,
            "D" => JoiningType.DualJoining,
            _ => JoiningType.NonJoining
        };
    }

    static JoiningGroup ParseJoiningGroup(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return JoiningGroup.NoJoiningGroup;

        // Convert to PascalCase
        string[] parts = value.Trim().Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();

        foreach (string part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1).ToLowerInvariant());
        }

        string enumName = sb.ToString();

        if (Enum.TryParse<JoiningGroup>(enumName, out var result))
            return result;

        return JoiningGroup.NoJoiningGroup;
    }

    static UnicodeScript ParseScript(string value)
    {
        // Convert to PascalCase (e.g., "Old_Italic" -> "OldItalic")
        string[] parts = value.Trim().Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();

        foreach (string part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1).ToLowerInvariant());
        }

        string enumName = sb.ToString();

        if (Enum.TryParse<UnicodeScript>(enumName, out var result))
            return result;

        return UnicodeScript.Unknown;
    }

    static LineBreakClass ParseLineBreakClass(string value)
    {
        if (Enum.TryParse<LineBreakClass>(value.Trim(), out var result))
            return result;

        return LineBreakClass.XX;
    }

    public List<RangeEntry> BuildRangeEntries()
    {
        var result = new List<RangeEntry>();

        int currentStart = 0;
        var current = props[0];

        for (int cp = 1; cp <= MaxCodePoint; cp++)
        {
            var p = props[cp];

            bool same =
                p.bidiClass == current.bidiClass &&
                p.joiningType == current.joiningType &&
                p.joiningGroup == current.joiningGroup;

            if (!same)
            {
                result.Add(new RangeEntry(
                    startCodePoint: currentStart,
                    endCodePoint: cp - 1,
                    bidiClass: current.bidiClass,
                    joiningType: current.joiningType,
                    joiningGroup: current.joiningGroup));

                currentStart = cp;
                current = p;
            }
        }

        result.Add(new RangeEntry(
            startCodePoint: currentStart,
            endCodePoint: MaxCodePoint,
            bidiClass: current.bidiClass,
            joiningType: current.joiningType,
            joiningGroup: current.joiningGroup));

        return result;
    }

    public List<ScriptRangeEntry> BuildScriptRangeEntries()
    {
        var result = new List<ScriptRangeEntry>();

        int currentStart = 0;
        var currentScript = props[0].script;

        for (int cp = 1; cp <= MaxCodePoint; cp++)
        {
            var script = props[cp].script;

            if (script != currentScript)
            {
                result.Add(new ScriptRangeEntry(currentStart, cp - 1, currentScript));
                currentStart = cp;
                currentScript = script;
            }
        }

        result.Add(new ScriptRangeEntry(currentStart, MaxCodePoint, currentScript));

        return result;
    }

    public List<LineBreakRangeEntry> BuildLineBreakRangeEntries()
    {
        var result = new List<LineBreakRangeEntry>();

        int currentStart = 0;
        var currentLbc = props[0].lineBreakClass;

        for (int cp = 1; cp <= MaxCodePoint; cp++)
        {
            var lbc = props[cp].lineBreakClass;

            if (lbc != currentLbc)
            {
                result.Add(new LineBreakRangeEntry(currentStart, cp - 1, currentLbc));
                currentStart = cp;
                currentLbc = lbc;
            }
        }

        result.Add(new LineBreakRangeEntry(currentStart, MaxCodePoint, currentLbc));

        return result;
    }

    /// <summary>
    /// Build range entries for Extended_Pictographic property.
    /// Only includes ranges where Extended_Pictographic=true.
    /// </summary>
    public List<ExtendedPictographicRangeEntry> BuildExtendedPictographicRangeEntries()
    {
        var result = new List<ExtendedPictographicRangeEntry>();

        int currentStart = -1;
        bool inRange = false;

        for (int cp = 0; cp <= MaxCodePoint; cp++)
        {
            bool ep = props[cp].extendedPictographic;

            if (ep && !inRange)
            {
                // Start a new range
                currentStart = cp;
                inRange = true;
            }
            else if (!ep && inRange)
            {
                // End current range
                result.Add(new ExtendedPictographicRangeEntry(currentStart, cp - 1));
                inRange = false;
            }
        }

        // Don't forget the last range if it extends to MaxCodePoint
        if (inRange)
        {
            result.Add(new ExtendedPictographicRangeEntry(currentStart, MaxCodePoint));
        }

        return result;
    }

    /// <summary>
    /// Build range entries for General_Category property.
    /// </summary>
    public List<GeneralCategoryRangeEntry> BuildGeneralCategoryRangeEntries()
    {
        var result = new List<GeneralCategoryRangeEntry>();

        int currentStart = 0;
        var currentGc = props[0].generalCategory;

        for (int cp = 1; cp <= MaxCodePoint; cp++)
        {
            var gc = props[cp].generalCategory;

            if (gc != currentGc)
            {
                result.Add(new GeneralCategoryRangeEntry(currentStart, cp - 1, currentGc));
                currentStart = cp;
                currentGc = gc;
            }
        }

        result.Add(new GeneralCategoryRangeEntry(currentStart, MaxCodePoint, currentGc));

        return result;
    }

    /// <summary>
    /// Build range entries for East_Asian_Width property.
    /// </summary>
    public List<EastAsianWidthRangeEntry> BuildEastAsianWidthRangeEntries()
    {
        var result = new List<EastAsianWidthRangeEntry>();

        int currentStart = 0;
        var currentEaw = props[0].eastAsianWidth;

        for (int cp = 1; cp <= MaxCodePoint; cp++)
        {
            var eaw = props[cp].eastAsianWidth;

            if (eaw != currentEaw)
            {
                result.Add(new EastAsianWidthRangeEntry(currentStart, cp - 1, currentEaw));
                currentStart = cp;
                currentEaw = eaw;
            }
        }

        result.Add(new EastAsianWidthRangeEntry(currentStart, MaxCodePoint, currentEaw));

        return result;
    }

    public static List<MirrorEntry> BuildMirrorEntries(string bidiMirroringPath)
    {
        if (string.IsNullOrEmpty(bidiMirroringPath))
            throw new ArgumentNullException(nameof(bidiMirroringPath));

        var result = new List<MirrorEntry>();

        using var reader = new StreamReader(bidiMirroringPath);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(';');
            if (parts.Length < 2)
                continue;

            string codePart = parts[0].Trim();
            string mirrorPart = parts[1].Trim();

            if (codePart.Length == 0 || mirrorPart.Length == 0)
                continue;

            int codePoint = ParseHexCodePoint(codePart);
            int mirrored = ParseHexCodePoint(mirrorPart);

            if (codePoint < 0 || codePoint > MaxCodePoint)
                continue;
            if (mirrored < 0 || mirrored > MaxCodePoint)
                continue;

            result.Add(new MirrorEntry(codePoint, mirrored));
        }

        result.Sort((a, b) => a.codePoint.CompareTo(b.codePoint));

        return result;
    }

    public static List<BracketEntry> BuildBracketEntries(string bidiBracketsPath)
    {
        if (string.IsNullOrEmpty(bidiBracketsPath))
            throw new ArgumentNullException(nameof(bidiBracketsPath));

        var result = new List<BracketEntry>();

        using var reader = new StreamReader(bidiBracketsPath);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = StripComment(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(';');
            if (parts.Length < 3)
                continue;

            string codePart = parts[0].Trim();
            string pairedPart = parts[1].Trim();
            string typePart = parts[2].Trim();

            if (codePart.Length == 0 || typePart.Length == 0)
                continue;

            int codePoint = ParseHexCodePoint(codePart);
            if (codePoint < 0 || codePoint > MaxCodePoint)
                continue;

            int pairedCodePoint;

            if (pairedPart.Length == 0 ||
                string.Equals(pairedPart, "<none>", StringComparison.OrdinalIgnoreCase))
            {
                pairedCodePoint = codePoint;
            }
            else
            {
                pairedCodePoint = ParseHexCodePoint(pairedPart);
                if (pairedCodePoint < 0 || pairedCodePoint > MaxCodePoint)
                    continue;
            }

            BidiPairedBracketType bracketType = typePart.ToUpperInvariant() switch
            {
                "O" => BidiPairedBracketType.Open,
                "C" => BidiPairedBracketType.Close,
                "N" => BidiPairedBracketType.None,
                _ => throw new InvalidDataException($"Unknown Bidi_Paired_Bracket_Type '{typePart}'.")
            };

            result.Add(new BracketEntry(codePoint, pairedCodePoint, bracketType));
        }

        result.Sort((a, b) => a.codePoint.CompareTo(b.codePoint));

        return result;
    }
}

#endregion

#region Writer бинарного формата

public static class UnicodeBinaryWriter
{
    const uint Magic = 0x554C5452; // "ULTR"
    const ushort FormatVersion4 = 4;

    /// <summary>
    /// Write format version 4 (with Script, LineBreak, Extended_Pictographic, GeneralCategory, EastAsianWidth)
    /// </summary>
    public static void WriteBinary(
        string outputPath,
        IReadOnlyList<RangeEntry> ranges,
        IReadOnlyList<MirrorEntry> mirrors,
        IReadOnlyList<BracketEntry> brackets,
        IReadOnlyList<ScriptRangeEntry> scripts,
        IReadOnlyList<LineBreakRangeEntry> lineBreaks,
        IReadOnlyList<ExtendedPictographicRangeEntry> extendedPictographics,
        IReadOnlyList<GeneralCategoryRangeEntry> generalCategories,
        IReadOnlyList<EastAsianWidthRangeEntry> eastAsianWidths,
        int unicodeVersionRaw)
    {
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);

        // Header placeholder
        long headerPosition = stream.Position;

        writer.Write(Magic);
        writer.Write(FormatVersion4);
        writer.Write((ushort)0); // Reserved
        writer.Write((uint)unicodeVersionRaw);

        // Section offsets placeholder (9 sections * 8 bytes)
        for (int i = 0; i < 18; i++)
            writer.Write((uint)0);

        // Write Range section
        long rangeOffset = stream.Position;
        writer.Write((uint)ranges.Count);
        foreach (var r in ranges)
        {
            writer.Write((uint)r.startCodePoint);
            writer.Write((uint)r.endCodePoint);
            writer.Write((byte)r.bidiClass);
            writer.Write((byte)r.joiningType);
            writer.Write((byte)r.joiningGroup);
            writer.Write((byte)0); // padding
        }
        uint rangeLength = (uint)(stream.Position - rangeOffset);

        // Write Mirror section
        long mirrorOffset = stream.Position;
        writer.Write((uint)mirrors.Count);
        foreach (var m in mirrors)
        {
            writer.Write((uint)m.codePoint);
            writer.Write((uint)m.mirroredCodePoint);
        }
        uint mirrorLength = (uint)(stream.Position - mirrorOffset);

        // Write Bracket section
        long bracketOffset = stream.Position;
        writer.Write((uint)brackets.Count);
        foreach (var b in brackets)
        {
            writer.Write((uint)b.codePoint);
            writer.Write((uint)b.pairedCodePoint);
            writer.Write((byte)b.bracketType);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }
        uint bracketLength = (uint)(stream.Position - bracketOffset);

        // Write Script section
        long scriptOffset = stream.Position;
        writer.Write((uint)scripts.Count);
        foreach (var s in scripts)
        {
            writer.Write((uint)s.startCodePoint);
            writer.Write((uint)s.endCodePoint);
            writer.Write((byte)s.script);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }
        uint scriptLength = (uint)(stream.Position - scriptOffset);

        // Write LineBreak section
        long lineBreakOffset = stream.Position;
        writer.Write((uint)lineBreaks.Count);
        foreach (var lb in lineBreaks)
        {
            writer.Write((uint)lb.startCodePoint);
            writer.Write((uint)lb.endCodePoint);
            writer.Write((byte)lb.lineBreakClass);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }
        uint lineBreakLength = (uint)(stream.Position - lineBreakOffset);

        // Write Extended_Pictographic section
        long extPictOffset = stream.Position;
        writer.Write((uint)extendedPictographics.Count);
        foreach (var ep in extendedPictographics)
        {
            writer.Write((uint)ep.startCodePoint);
            writer.Write((uint)ep.endCodePoint);
        }
        uint extPictLength = (uint)(stream.Position - extPictOffset);

        // Write GeneralCategory section
        long gcOffset = stream.Position;
        writer.Write((uint)generalCategories.Count);
        foreach (var gc in generalCategories)
        {
            writer.Write((uint)gc.startCodePoint);
            writer.Write((uint)gc.endCodePoint);
            writer.Write((byte)gc.generalCategory);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }
        uint gcLength = (uint)(stream.Position - gcOffset);

        // Write EastAsianWidth section
        long eawOffset = stream.Position;
        writer.Write((uint)eastAsianWidths.Count);
        foreach (var eaw in eastAsianWidths)
        {
            writer.Write((uint)eaw.startCodePoint);
            writer.Write((uint)eaw.endCodePoint);
            writer.Write((byte)eaw.eastAsianWidth);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }
        uint eawLength = (uint)(stream.Position - eawOffset);

        // Go back and write header with offsets
        stream.Position = headerPosition;

        writer.Write(Magic);
        writer.Write(FormatVersion4);
        writer.Write((ushort)0);
        writer.Write((uint)unicodeVersionRaw);

        writer.Write((uint)rangeOffset);
        writer.Write(rangeLength);
        writer.Write((uint)mirrorOffset);
        writer.Write(mirrorLength);
        writer.Write((uint)bracketOffset);
        writer.Write(bracketLength);
        writer.Write((uint)scriptOffset);
        writer.Write(scriptLength);
        writer.Write((uint)lineBreakOffset);
        writer.Write(lineBreakLength);
        writer.Write((uint)extPictOffset);
        writer.Write(extPictLength);
        writer.Write((uint)gcOffset);
        writer.Write(gcLength);
        writer.Write((uint)eawOffset);
        writer.Write(eawLength);

        writer.Flush();
    }

    /// <summary>
    /// Write format version 1 (backward compatible, no Script/LineBreak)
    /// </summary>
    public static void WriteBinaryV1(
        string outputPath,
        IReadOnlyList<RangeEntry> ranges,
        IReadOnlyList<MirrorEntry> mirrors,
        IReadOnlyList<BracketEntry> brackets,
        int unicodeVersionRaw)
    {
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);

        const ushort v1 = 1;

        long headerPosition = stream.Position;

        writer.Write(Magic);
        writer.Write(v1);
        writer.Write((ushort)0);
        writer.Write((uint)unicodeVersionRaw);

        // 3 sections
        for (int i = 0; i < 6; i++)
            writer.Write((uint)0);

        long rangeOffset = stream.Position;
        writer.Write((uint)ranges.Count);
        foreach (var r in ranges)
        {
            writer.Write((uint)r.startCodePoint);
            writer.Write((uint)r.endCodePoint);
            writer.Write((byte)r.bidiClass);
            writer.Write((byte)r.joiningType);
            writer.Write((byte)r.joiningGroup);
            writer.Write((byte)0);
        }
        uint rangeLength = (uint)(stream.Position - rangeOffset);

        long mirrorOffset = stream.Position;
        writer.Write((uint)mirrors.Count);
        foreach (var m in mirrors)
        {
            writer.Write((uint)m.codePoint);
            writer.Write((uint)m.mirroredCodePoint);
        }
        uint mirrorLength = (uint)(stream.Position - mirrorOffset);

        long bracketOffset = stream.Position;
        writer.Write((uint)brackets.Count);
        foreach (var b in brackets)
        {
            writer.Write((uint)b.codePoint);
            writer.Write((uint)b.pairedCodePoint);
            writer.Write((byte)b.bracketType);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }
        uint bracketLength = (uint)(stream.Position - bracketOffset);

        stream.Position = headerPosition;

        writer.Write(Magic);
        writer.Write(v1);
        writer.Write((ushort)0);
        writer.Write((uint)unicodeVersionRaw);

        writer.Write((uint)rangeOffset);
        writer.Write(rangeLength);
        writer.Write((uint)mirrorOffset);
        writer.Write(mirrorLength);
        writer.Write((uint)bracketOffset);
        writer.Write(bracketLength);

        writer.Flush();
    }
}

#endregion