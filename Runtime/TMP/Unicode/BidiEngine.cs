using System;
using System.Collections.Generic;

public readonly struct BidiParagraph
{
    public readonly int startIndex;      // inclusive, index in logical text (code point index)
    public readonly int endIndex;        // inclusive
    public readonly byte baseLevel;      // 0 for LTR, 1 for RTL

    public BidiParagraph(int startIndex, int endIndex, byte baseLevel)
    {
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.baseLevel = baseLevel;
    }

    public BidiDirection Direction
    {
        get { return (baseLevel & 1) == 0 ? BidiDirection.LeftToRight : BidiDirection.RightToLeft; }
    }
}

public readonly struct BidiResult
{
    /// <summary>
    /// Embedding level for each logical code point.
    /// Length is equal to the input logical length.
    /// On this step, levels reflect only paragraph base levels (P2/P3).
    /// Later we will refine them with explicit embeddings, weak/neutral resolution, and implicit levels.
    /// </summary>
    public readonly byte[] levels;

    /// <summary>
    /// Paragraph information: logical ranges and base embedding levels.
    /// </summary>
    public readonly BidiParagraph[] paragraphs;

    public BidiResult(byte[] levels, BidiParagraph[] paragraphs)
    {
        this.levels = levels ?? throw new ArgumentNullException(nameof(levels));
        this.paragraphs = paragraphs ?? throw new ArgumentNullException(nameof(paragraphs));
    }
}

public sealed class BidiEngine
{
    private readonly IUnicodeDataProvider unicodeData;

    // Reused buffers to reduce allocations; they grow as needed.
    private BidiClass[] bidiClassesBuffer = Array.Empty<BidiClass>();

    public BidiEngine(IUnicodeDataProvider unicodeData)
    {
        this.unicodeData = unicodeData ?? throw new ArgumentNullException(nameof(unicodeData));
    }

    /// <summary>
    /// Processes a logical sequence of Unicode scalar values (code points)
    /// and returns paragraph information and initial levels according to UAX #9 P1-P3.
    /// This does NOT yet perform the full bidi algorithm; it only:
    /// - determines paragraph boundaries (P1),
    /// - determines base embedding level for each paragraph (P2-P3),
    /// - assigns base level to each character in the paragraph.
    /// </summary>
    /// <param name="codePoints">Logical code points (not UTF-16 code units).</param>
    /// <returns>BidiResult with base levels and paragraph info.</returns>
    public BidiResult Process(ReadOnlySpan<int> codePoints)
    {
        int length = codePoints.Length;
        if (length == 0)
        {
            return new BidiResult(Array.Empty<byte>(), Array.Empty<BidiParagraph>());
        }

        EnsureBidiClassesCapacity(length);

        // Step 1: classify each code point by Bidi_Class using Unicode data.
        for (int i = 0; i < length; i++)
        {
            int cp = codePoints[i];
            // We assume codePoints contain valid scalar values (0..0x10FFFF),
            // but clamp defensively for robustness.
            if ((uint)cp > 0x10FFFFU)
                cp = 0xFFFD; // replacement character

            bidiClassesBuffer[i] = unicodeData.GetBidiClass(cp);
        }

        // Step 2: determine paragraph boundaries and base embedding levels (UAX #9 P1-P3).
        List<BidiParagraph> paragraphList = BuildParagraphs(bidiClassesBuffer, length);

        // Step 3: assign base level to each character according to its paragraph.
        byte[] levels = new byte[length];

        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];
            byte baseLevel = paragraph.baseLevel;

            for (int i = paragraph.startIndex; i <= paragraph.endIndex; i++)
            {
                levels[i] = baseLevel;
            }
        }

        BidiParagraph[] paragraphs = paragraphList.ToArray();
        return new BidiResult(levels, paragraphs);
    }

    private void EnsureBidiClassesCapacity(int length)
    {
        if (bidiClassesBuffer.Length < length)
        {
            bidiClassesBuffer = new BidiClass[length];
        }
    }

    /// <summary>
    /// Implements paragraph detection and base level computation according to UAX #9 P1-P3.
    /// P1: Split text into paragraphs at B type characters (Paragraph_Separator).
    /// P2/P3: Base embedding level per paragraph:
    ///   - If the first strong character is L => base level 0.
    ///   - If the first strong is R or AL => base level 1.
    ///   - If no strong character is found => default base level 0 (can be customized later).
    /// </summary>
    private static List<BidiParagraph> BuildParagraphs(BidiClass[] bidiClasses, int length)
    {
        var paragraphs = new List<BidiParagraph>();

        int paraStart = 0;
        for (int i = 0; i < length; i++)
        {
            if (bidiClasses[i] == BidiClass.ParagraphSeparator)
            {
                // Paragraph ends at i-1 (can be empty if i == paraStart)
                int paraEnd = i - 1;
                if (paraEnd >= paraStart)
                {
                    byte baseLevel = ComputeParagraphBaseLevel(bidiClasses, paraStart, paraEnd);
                    paragraphs.Add(new BidiParagraph(paraStart, paraEnd, baseLevel));
                }

                // UAX #9: The paragraph separator itself is not part of the paragraph.
                paraStart = i + 1;
            }
        }

        // Last paragraph (if any characters remain after the final B)
        if (paraStart < length)
        {
            int paraEnd = length - 1;
            byte baseLevel = ComputeParagraphBaseLevel(bidiClasses, paraStart, paraEnd);
            paragraphs.Add(new BidiParagraph(paraStart, paraEnd, baseLevel));
        }

        return paragraphs;
    }

    /// <summary>
    /// Computes base embedding level for a paragraph range [start, end] according to UAX #9 P2/P3.
    /// P2: If the first strong character is L => level 0.
    /// P3: If the first strong is R or AL => level 1.
    /// If no strong character is found, we default to 0 (LTR). This behavior can be customized if needed.
    /// </summary>
    private static byte ComputeParagraphBaseLevel(BidiClass[] bidiClasses, int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            BidiClass bc = bidiClasses[i];
            switch (bc)
            {
                case BidiClass.LeftToRight:
                    return 0; // LTR paragraph

                case BidiClass.RightToLeft:
                case BidiClass.ArabicLetter:
                    return 1; // RTL paragraph
            }
        }

        // No strong character found: per UAX #9 default can be chosen.
        // Most engines use 0 (LTR) as a default; if you want to follow locale, this is the place to change.
        return 0;
    }
}
