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

            int rangeStart;
            int rangeEnd;

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
                continue;

            if (rangeEnd > MaxCodePoint)
                rangeEnd = MaxCodePoint;

            for (int cp = rangeStart; cp <= rangeEnd; cp++)
            {
                props[cp].bidiClass = bidiClass;
            }
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

            int rangeStart;
            int rangeEnd;

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
                continue;

            if (rangeEnd > MaxCodePoint)
                rangeEnd = MaxCodePoint;

            for (int cp = rangeStart; cp <= rangeEnd; cp++)
            {
                props[cp].joiningType = joiningType;
            }
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
        switch (value)
        {
            case "L": return BidiClass.LeftToRight;
            case "R": return BidiClass.RightToLeft;
            case "AL": return BidiClass.ArabicLetter;

            case "EN": return BidiClass.EuropeanNumber;
            case "ES": return BidiClass.EuropeanSeparator;
            case "ET": return BidiClass.EuropeanTerminator;
            case "AN": return BidiClass.ArabicNumber;
            case "CS": return BidiClass.CommonSeparator;
            case "NSM": return BidiClass.NonspacingMark;

            case "BN": return BidiClass.BoundaryNeutral;
            case "B": return BidiClass.ParagraphSeparator;
            case "S": return BidiClass.SegmentSeparator;
            case "WS": return BidiClass.WhiteSpace;
            case "ON": return BidiClass.OtherNeutral;

            case "LRE": return BidiClass.LeftToRightEmbedding;
            case "LRO": return BidiClass.LeftToRightOverride;
            case "RLE": return BidiClass.RightToLeftEmbedding;
            case "RLO": return BidiClass.RightToLeftOverride;
            case "PDF": return BidiClass.PopDirectionalFormat;

            case "LRI": return BidiClass.LeftToRightIsolate;
            case "RLI": return BidiClass.RightToLeftIsolate;
            case "FSI": return BidiClass.FirstStrongIsolate;
            case "PDI": return BidiClass.PopDirectionalIsolate;

            default:
                throw new InvalidDataException($"Unknown Bidi_Class value '{value}' in DerivedBidiClass.txt.");
        }
    }

    private static JoiningGroup ParseJoiningGroup(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException("Joining_Group value is empty in ArabicShaping.txt.");

        // Normalize: trim, split on spaces and underscores, build PascalCase name.
        string[] parts = value.Trim().Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            throw new InvalidDataException($"Joining_Group value '{value}' cannot be parsed (no tokens).");

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].ToLowerInvariant();
            if (part.Length == 0)
                continue;

            // First char upper, rest lower
            char first = char.ToUpperInvariant(part[0]);
            sb.Append(first);
            if (part.Length > 1)
                sb.Append(part, 1, part.Length - 1);
        }

        string enumName = sb.ToString();

        if (Enum.TryParse(enumName, ignoreCase: false, out JoiningGroup result))
            return result;

        throw new InvalidDataException(
            $"Unknown Joining_Group value '{value}' in ArabicShaping.txt (normalized to '{enumName}').");
    }


    static JoiningType ParseJoiningType(string value)
    {
        switch (value)
        {
            case "U": return JoiningType.NonJoining;
            case "T": return JoiningType.Transparent;
            case "C": return JoiningType.JoinCausing;
            case "L": return JoiningType.LeftJoining;
            case "R": return JoiningType.RightJoining;
            case "D": return JoiningType.DualJoining;

            default:
                throw new InvalidDataException($"Unknown Joining_Type value '{value}' in DerivedJoiningType.txt.");
        }
    }


    public List<RangeEntry> BuildRangeEntries()
    {
        var result = new List<RangeEntry>();

        int currentStart = 0;
        UnicodeProps current = props[0];

        for (int cp = 1; cp <= MaxCodePoint; cp++)
        {
            UnicodeProps p = props[cp];

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

            // Формат по UCD: "XXXX; YYYY"
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

        // Бинарный провайдер делает двоичный поиск по mirrors[], поэтому сортируем по codePoint
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

            // Формат по UCD:
            // Field 0: code point (hex)
            // Field 1: Bidi_Paired_Bracket (hex или <none>)
            // Field 2: Bidi_Paired_Bracket_Type: "o", "c", "n"
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

            // По TR44 / описанию BidiBrackets: при отсутствии пары поле может быть "<none>",
            // а значение Bidi_Paired_Bracket по умолчанию равно самому символу.
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

            BidiPairedBracketType bracketType;

            switch (typePart)
            {
                case "o":
                case "O":
                    bracketType = BidiPairedBracketType.Open;
                    break;

                case "c":
                case "C":
                    bracketType = BidiPairedBracketType.Close;
                    break;

                case "n":
                case "N":
                    bracketType = BidiPairedBracketType.None;
                    break;

                default:
                    // Строго: неизвестное значение bpt — считаем ошибкой входных данных UCD.
                    throw new InvalidDataException(
                        $"Unknown Bidi_Paired_Bracket_Type '{typePart}' in BidiBrackets.txt.");
            }

            result.Add(new BracketEntry(codePoint, pairedCodePoint, bracketType));
        }

        // Бинарный провайдер делает двоичный поиск по brackets[], поэтому сортируем по codePoint
        result.Sort((a, b) => a.codePoint.CompareTo(b.codePoint));

        return result;
    }
}

#endregion

#region Writer бинарного формата

static class UnicodeBinaryWriter
{
    const uint magic = 0x554C5452;
    const ushort formatVersion = 1;

    public static void WriteBinary(
        string outputPath,
        IReadOnlyList<RangeEntry> ranges,
        IReadOnlyList<MirrorEntry> mirrors,
        IReadOnlyList<BracketEntry> brackets,
        int unicodeVersionRaw)
    {
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);

        long headerPosition = stream.Position;

        writer.Write(magic);
        writer.Write((ushort)formatVersion);
        writer.Write((ushort)0);
        writer.Write((uint)unicodeVersionRaw);

        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)0);
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

        long rangeEndPos = stream.Position;
        uint rangeLength = (uint)(rangeEndPos - rangeOffset);

        long mirrorOffset = stream.Position;

        writer.Write((uint)mirrors.Count);
        foreach (var m in mirrors)
        {
            writer.Write((uint)m.codePoint);
            writer.Write((uint)m.mirroredCodePoint);
        }

        long mirrorEndPos = stream.Position;
        uint mirrorLength = (uint)(mirrorEndPos - mirrorOffset);

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

        long bracketEndPos = stream.Position;
        uint bracketLength = (uint)(bracketEndPos - bracketOffset);

        stream.Position = headerPosition;

        writer.Write(magic);
        writer.Write((ushort)formatVersion);
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