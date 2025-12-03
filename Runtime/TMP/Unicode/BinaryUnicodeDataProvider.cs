using System;
using System.IO;


public readonly struct RangeEntry
{
    public readonly int startCodePoint; // inclusive
    public readonly int endCodePoint; // inclusive
    public readonly BidiClass bidiClass;
    public readonly JoiningType joiningType;
    public readonly JoiningGroup joiningGroup;

    public RangeEntry(
        int startCodePoint,
        int endCodePoint,
        BidiClass bidiClass,
        JoiningType joiningType,
        JoiningGroup joiningGroup)
    {
        this.startCodePoint = startCodePoint;
        this.endCodePoint = endCodePoint;
        this.bidiClass = bidiClass;
        this.joiningType = joiningType;
        this.joiningGroup = joiningGroup;
    }

    public bool Contains(int codePoint)
    {
        return codePoint >= startCodePoint && codePoint <= endCodePoint;
    }
}

public readonly struct MirrorEntry
{
    public readonly int codePoint;
    public readonly int mirroredCodePoint;

    public MirrorEntry(int codePoint, int mirroredCodePoint)
    {
        this.codePoint = codePoint;
        this.mirroredCodePoint = mirroredCodePoint;
    }
}

public readonly struct BracketEntry
{
    public readonly int codePoint;
    public readonly int pairedCodePoint;
    public readonly BidiPairedBracketType bracketType;

    public BracketEntry(int codePoint, int pairedCodePoint, BidiPairedBracketType bracketType)
    {
        this.codePoint = codePoint;
        this.pairedCodePoint = pairedCodePoint;
        this.bracketType = bracketType;
    }
}

public sealed class BinaryUnicodeDataProvider : IUnicodeDataProvider
{
    private const uint magic = 0x554C5452; // 'R','T','L','U' little-endian
    private const ushort supportedFormatVersion = 1;

    private readonly RangeEntry[] ranges;
    private readonly MirrorEntry[] mirrors;
    private readonly BracketEntry[] brackets;

    public int unicodeVersionRaw { get; }

    public BinaryUnicodeDataProvider(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        using var stream = new MemoryStream(data, writable: false);
        using var reader = new BinaryReader(stream);

        // Header
        uint fileMagic = reader.ReadUInt32();
        if (fileMagic != magic)
            throw new InvalidDataException("Invalid Unicode data blob: magic mismatch.");

        ushort formatVersion = reader.ReadUInt16();
        if (formatVersion != supportedFormatVersion)
            throw new InvalidDataException($"Unsupported Unicode data format version: {formatVersion}.");

        reader.ReadUInt16(); // reserved

        uint unicodeVersion = reader.ReadUInt32();
        unicodeVersionRaw = unchecked((int)unicodeVersion);

        uint rangeOffset = reader.ReadUInt32();
        uint rangeLength = reader.ReadUInt32();
        uint mirrorOffset = reader.ReadUInt32();
        uint mirrorLength = reader.ReadUInt32();
        uint bracketOffset = reader.ReadUInt32();
        uint bracketLength = reader.ReadUInt32();

        // Ranges section
        if (rangeOffset == 0 || rangeLength == 0)
            throw new InvalidDataException("Unicode data blob is missing Range section.");

        stream.Position = rangeOffset;
        uint rangeCount = reader.ReadUInt32();
        ranges = new RangeEntry[rangeCount];

        for (uint i = 0; i < rangeCount; i++)
        {
            uint start = reader.ReadUInt32();
            uint end = reader.ReadUInt32();
            byte bidi = reader.ReadByte();
            byte jt = reader.ReadByte();
            byte jg = reader.ReadByte();
            reader.ReadByte(); // padding

            ranges[i] = new RangeEntry(
                startCodePoint: unchecked((int)start),
                endCodePoint: unchecked((int)end),
                bidiClass: (BidiClass)bidi,
                joiningType: (JoiningType)jt,
                joiningGroup: (JoiningGroup)jg);
        }

        // Mirrors section
        if (mirrorOffset != 0 && mirrorLength != 0)
        {
            stream.Position = mirrorOffset;
            uint mirrorCount = reader.ReadUInt32();
            mirrors = new MirrorEntry[mirrorCount];

            for (uint i = 0; i < mirrorCount; i++)
            {
                uint cp = reader.ReadUInt32();
                uint mirrored = reader.ReadUInt32();

                mirrors[i] = new MirrorEntry(
                    codePoint: unchecked((int)cp),
                    mirroredCodePoint: unchecked((int)mirrored));
            }
        }
        else
        {
            mirrors = Array.Empty<MirrorEntry>();
        }

        // Brackets section
        if (bracketOffset != 0 && bracketLength != 0)
        {
            stream.Position = bracketOffset;
            uint bracketCount = reader.ReadUInt32();
            brackets = new BracketEntry[bracketCount];

            for (uint i = 0; i < bracketCount; i++)
            {
                uint cp = reader.ReadUInt32();
                uint paired = reader.ReadUInt32();
                byte bpt = reader.ReadByte();
                reader.ReadByte(); // reserved
                reader.ReadByte(); // reserved
                reader.ReadByte(); // reserved

                brackets[i] = new BracketEntry(
                    codePoint: unchecked((int)cp),
                    pairedCodePoint: unchecked((int)paired),
                    bracketType: (BidiPairedBracketType)bpt);
            }
        }
        else
        {
            brackets = Array.Empty<BracketEntry>();
        }
    }

    // IUnicodeDataProvider

    public BidiClass GetBidiClass(int codePoint)
    {
        var entry = FindRange(codePoint);
        return entry?.bidiClass ?? BidiClass.LeftToRight;
    }

    public bool IsBidiMirrored(int codePoint)
    {
        return FindMirror(codePoint) != null;
    }

    public int GetBidiMirroringGlyph(int codePoint)
    {
        var mirror = FindMirror(codePoint);
        return mirror?.mirroredCodePoint ?? codePoint;
    }

    public BidiPairedBracketType GetBidiPairedBracketType(int codePoint)
    {
        var bracket = FindBracket(codePoint);
        return bracket?.bracketType ?? BidiPairedBracketType.None;
    }

    public int GetBidiPairedBracket(int codePoint)
    {
        var bracket = FindBracket(codePoint);
        return bracket?.pairedCodePoint ?? codePoint;
    }

    public JoiningType GetJoiningType(int codePoint)
    {
        var entry = FindRange(codePoint);
        return entry?.joiningType ?? JoiningType.NonJoining;
    }

    public JoiningGroup GetJoiningGroup(int codePoint)
    {
        var entry = FindRange(codePoint);
        return entry?.joiningGroup ?? JoiningGroup.NoJoiningGroup;
    }

    // public lookup helpers

    private RangeEntry? FindRange(int codePoint)
    {
        int lo = 0;
        int hi = ranges.Length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var entry = ranges[mid];

            if (codePoint < entry.startCodePoint)
            {
                hi = mid - 1;
            }
            else if (codePoint > entry.endCodePoint)
            {
                lo = mid + 1;
            }
            else
            {
                return entry;
            }
        }

        return null;
    }

    private MirrorEntry? FindMirror(int codePoint)
    {
        int lo = 0;
        int hi = mirrors.Length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var entry = mirrors[mid];

            if (codePoint < entry.codePoint)
            {
                hi = mid - 1;
            }
            else if (codePoint > entry.codePoint)
            {
                lo = mid + 1;
            }
            else
            {
                return entry;
            }
        }

        return null;
    }

    private BracketEntry? FindBracket(int codePoint)
    {
        int lo = 0;
        int hi = brackets.Length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var entry = brackets[mid];

            if (codePoint < entry.codePoint)
            {
                hi = mid - 1;
            }
            else if (codePoint > entry.codePoint)
            {
                lo = mid + 1;
            }
            else
            {
                return entry;
            }
        }

        return null;
    }
}