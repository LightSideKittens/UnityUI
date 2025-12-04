using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public readonly struct BidiParagraph
{
    public readonly int startIndex;
    public readonly int endIndex;
    public readonly byte baseLevel;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BidiParagraph(int startIndex, int endIndex, byte baseLevel)
    {
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.baseLevel = baseLevel;
    }

    public BidiDirection Direction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (baseLevel & 1) == 0 ? BidiDirection.LeftToRight : BidiDirection.RightToLeft;
    }
}

public readonly struct BidiResult
{
    public readonly byte[] levels;
    public readonly BidiParagraph[] paragraphs;

    public BidiResult(byte[] levels, BidiParagraph[] paragraphs)
    {
        this.levels = levels ?? throw new ArgumentNullException(nameof(levels));
        this.paragraphs = paragraphs ?? throw new ArgumentNullException(nameof(paragraphs));
    }
}

public sealed class BidiEngine
{
    private readonly struct LevelRun
    {
        public readonly int startIndex;
        public readonly int endIndex;
        public readonly byte level;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LevelRun(int startIndex, int endIndex, byte level)
        {
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.level = level;
        }
    }

    private readonly struct IsolatingRunSequence
    {
        public readonly int[] indexMap;
        public readonly byte level;
        public readonly BidiClass sos;
        public readonly BidiClass eos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IsolatingRunSequence(int[] indexMap, byte level, BidiClass sos, BidiClass eos)
        {
            this.indexMap = indexMap;
            this.level = level;
            this.sos = sos;
            this.eos = eos;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => indexMap.Length;
        }

        public int this[int sequenceIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => indexMap[sequenceIndex];
        }
    }

    private struct EmbeddingState
    {
        public byte level;
        public sbyte overrideStatus;
        public bool isIsolate;
    }

    private readonly struct BracketPair
    {
        public readonly int openIndex;
        public readonly int closeIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BracketPair(int openIndex, int closeIndex)
        {
            this.openIndex = openIndex;
            this.closeIndex = closeIndex;
        }
    }

    private const int maxExplicitLevel = 125;

    private readonly EmbeddingState[] embeddingStackBuffer = new EmbeddingState[maxExplicitLevel + 2];

    private readonly IUnicodeDataProvider unicodeData;

    private BidiClass[] bidiClassesBuffer = Array.Empty<BidiClass>();
    private BidiClass[] originalBidiClassesBuffer = Array.Empty<BidiClass>();

    private List<BidiParagraph> paragraphListBuffer = new(4);
    private List<IsolatingRunSequence> sequencesBuffer = new(16);
    private List<int> seqIndicesBuffer = new(128);
    private List<BracketPair> bracketPairsBuffer = new(32);
    private List<int> openStackBuffer = new(32);
    private List<int> isolatePairStackBuffer = new(32);
    private Stack<int> isolateStackBuffer = new(16);

    public BidiEngine(IUnicodeDataProvider unicodeData)
    {
        this.unicodeData = unicodeData ?? throw new ArgumentNullException(nameof(unicodeData));
    }

    public static void ReorderLine(byte[] levels, int start, int end, int[] indexMap)
    {
        int length = end - start + 1;
        if (length <= 0)
            return;

        if (indexMap == null)
            throw new ArgumentNullException(nameof(indexMap));

        if (indexMap.Length < length)
            throw new ArgumentException("indexMap length is less than line length.", nameof(indexMap));

        for (int i = 0; i < length; i++)
        {
            indexMap[i] = start + i;
        }

        byte maxLevel = 0;
        byte minOddLevel = byte.MaxValue;

        for (int i = start; i <= end; i++)
        {
            byte level = levels[i];

            if (level > maxLevel)
                maxLevel = level;

            if ((level & 1) != 0 && level < minOddLevel)
                minOddLevel = level;
        }

        if (minOddLevel == byte.MaxValue)
            return;

        for (byte level = maxLevel; level >= minOddLevel; level--)
        {
            int i = 0;
            while (i < length)
            {
                int logicalIndex = indexMap[i];

                if (levels[logicalIndex] >= level)
                {
                    int runStart = i;
                    int runEnd = i + 1;

                    while (runEnd < length && levels[indexMap[runEnd]] >= level)
                    {
                        runEnd++;
                    }

                    int left = runStart;
                    int right = runEnd - 1;

                    while (left < right)
                    {
                        (indexMap[left], indexMap[right]) = (indexMap[right], indexMap[left]);
                        left++;
                        right--;
                    }

                    i = runEnd;
                }
                else
                {
                    i++;
                }
            }

            if (level == 0)
                break;
        }
    }

    public BidiResult Process(ReadOnlySpan<int> codePoints)
    {
        return ProcessInternal(codePoints, forcedParagraphLevel: null);
    }

    public BidiResult Process(ReadOnlySpan<int> codePoints, int paragraphDirection)
    {
        byte? forcedParagraphLevel;

        switch (paragraphDirection)
        {
            case 0:
                forcedParagraphLevel = 0;
                break;
            case 1:
                forcedParagraphLevel = 1;
                break;
            case 2:
                forcedParagraphLevel = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(paragraphDirection),
                    "Paragraph direction must be 0 (LTR), 1 (RTL), or 2 (auto).");
        }

        return ProcessInternal(codePoints, forcedParagraphLevel);
    }

    private BidiResult ProcessInternal(ReadOnlySpan<int> codePoints, byte? forcedParagraphLevel)
    {
        int length = codePoints.Length;
        if (length == 0)
        {
            return new BidiResult(Array.Empty<byte>(), Array.Empty<BidiParagraph>());
        }

        EnsureBidiClassesCapacity(length);

        for (int i = 0; i < length; i++)
        {
            int cp = codePoints[i];
            if ((uint)cp > 0x10FFFFU)
                cp = 0xFFFD;

            BidiClass bc = unicodeData.GetBidiClass(cp);
            bidiClassesBuffer[i] = bc;
            originalBidiClassesBuffer[i] = bc;
        }

        paragraphListBuffer.Clear();
        if (forcedParagraphLevel.HasValue)
        {
            BuildParagraphsWithExplicitBaseLevel(
                bidiClassesBuffer,
                length,
                forcedParagraphLevel.Value,
                paragraphListBuffer);
        }
        else
        {
            BuildParagraphs(bidiClassesBuffer, length, paragraphListBuffer);
        }

        byte[] levels = new byte[length];
        int paragraphCount = paragraphListBuffer.Count;

        for (int pIndex = 0; pIndex < paragraphCount; pIndex++)
        {
            BidiParagraph paragraph = paragraphListBuffer[pIndex];

            ResolveExplicitLevelsForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphCount; pIndex++)
        {
            BidiParagraph paragraph = paragraphListBuffer[pIndex];

            ResolveWeakTypesForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphCount; pIndex++)
        {
            BidiParagraph paragraph = paragraphListBuffer[pIndex];

            ResolvePairedBracketsForParagraph(
                codePoints,
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphCount; pIndex++)
        {
            BidiParagraph paragraph = paragraphListBuffer[pIndex];

            ResolveNeutralTypesForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphCount; pIndex++)
        {
            BidiParagraph paragraph = paragraphListBuffer[pIndex];

            ResolveImplicitLevelsForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphCount; pIndex++)
        {
            BidiParagraph paragraph = paragraphListBuffer[pIndex];

            ApplyLineBreakRuleL1ForParagraph(
                codePoints,
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                levels);
        }

        BidiParagraph[] paragraphs = paragraphListBuffer.ToArray();
        return new BidiResult(levels, paragraphs);
    }


    private List<IsolatingRunSequence> BuildIsolatingRunSequences(
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        sequencesBuffer.Clear();

        if (start > end)
            return sequencesBuffer;

        int length = end - start + 1;

        int[] isolateToPdi = ArrayPool<int>.Shared.Rent(length);
        int[] pdiToIsolate = ArrayPool<int>.Shared.Rent(length);
        
        try
        {
            for (int i = 0; i < length; i++)
            {
                isolateToPdi[i] = -1;
                pdiToIsolate[i] = -1;
            }

            isolateStackBuffer.Clear();

            for (int index = start; index <= end; index++)
            {
                BidiClass bc = bidiClasses[index];

                if (bc == BidiClass.LeftToRightIsolate ||
                    bc == BidiClass.RightToLeftIsolate ||
                    bc == BidiClass.FirstStrongIsolate)
                {
                    isolateStackBuffer.Push(index);
                }
                else if (bc == BidiClass.PopDirectionalIsolate)
                {
                    if (isolateStackBuffer.Count > 0)
                    {
                        int open = isolateStackBuffer.Pop();
                        isolateToPdi[open - start] = index;
                        pdiToIsolate[index - start] = open;
                    }
                }
            }

            var levelRuns = new List<(int runStart, int runEnd, byte level)>(32);

            {
                int runStart = -1;
                byte runLevel = 0;

                for (int i = start; i <= end; i++)
                {
                    if (bidiClasses[i] == BidiClass.BoundaryNeutral)
                        continue;

                    byte l = levels[i];
                    
                    if (runStart == -1)
                    {
                        runStart = i;
                        runLevel = l;
                    }
                    else if (l != runLevel)
                    {
                        levelRuns.Add((runStart, FindLastNonBnBefore(i, start, bidiClasses), runLevel));
                        runStart = i;
                        runLevel = l;
                    }
                }

                if (runStart != -1)
                {
                    levelRuns.Add((runStart, FindLastNonBnBefore(end + 1, start, bidiClasses), runLevel));
                }
            }

            int levelRunsCount = levelRuns.Count;
            
            for (int r = 0; r < levelRunsCount; r++)
            {
                var run = levelRuns[r];
                int firstIndex = run.runStart;
                BidiClass firstBc = bidiClasses[firstIndex];

                if (firstBc == BidiClass.PopDirectionalIsolate &&
                    pdiToIsolate[firstIndex - start] != -1)
                {
                    continue;
                }

                seqIndicesBuffer.Clear();
                int currentRunIndex = r;

                while (true)
                {
                    var currentRun = levelRuns[currentRunIndex];

                    for (int i = currentRun.runStart; i <= currentRun.runEnd; i++)
                    {
                        seqIndicesBuffer.Add(i);
                    }

                    int lastIndex = currentRun.runEnd;
                    BidiClass lastBc = bidiClasses[lastIndex];

                    bool isIsolateInitiator =
                        lastBc == BidiClass.LeftToRightIsolate ||
                        lastBc == BidiClass.RightToLeftIsolate ||
                        lastBc == BidiClass.FirstStrongIsolate;

                    if (!isIsolateInitiator)
                    {
                        break;
                    }

                    int pdiIndex = isolateToPdi[lastIndex - start];
                    if (pdiIndex < 0)
                    {
                        break;
                    }

                    int nextRunIndex = -1;
                    for (int i = 0; i < levelRunsCount; i++)
                    {
                        var r2 = levelRuns[i];
                        if (r2.runStart <= pdiIndex && pdiIndex <= r2.runEnd)
                        {
                            nextRunIndex = i;
                            break;
                        }
                    }

                    if (nextRunIndex < 0 || nextRunIndex == currentRunIndex)
                    {
                        break;
                    }

                    currentRunIndex = nextRunIndex;
                }

                int seqCount = seqIndicesBuffer.Count;
                if (seqCount == 0)
                    continue;

                int[] seqArray = new int[seqCount];
                for (int i = 0; i < seqCount; i++)
                    seqArray[i] = seqIndicesBuffer[i];

                byte seqLevel = levels[seqArray[0]];

                BidiClass sos = ComputeSequenceBoundaryType(
                    start,
                    end,
                    paragraphBaseLevel,
                    seqArray[0],
                    isStart: true,
                    bidiClasses,
                    levels,
                    isolateToPdi);

                BidiClass eos = ComputeSequenceBoundaryType(
                    start,
                    end,
                    paragraphBaseLevel,
                    seqArray[seqArray.Length - 1],
                    isStart: false,
                    bidiClasses,
                    levels,
                    isolateToPdi);

                sequencesBuffer.Add(new IsolatingRunSequence(seqArray, seqLevel, sos, eos));
            }

            return sequencesBuffer;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(isolateToPdi);
            ArrayPool<int>.Shared.Return(pdiToIsolate);
        }
    }

    private void ResolveNeutralTypesForParagraph(
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
            return;

        List<IsolatingRunSequence> sequences = BuildIsolatingRunSequences(
            start,
            end,
            paragraphBaseLevel,
            bidiClasses,
            levels);

        if (sequences.Count == 0)
            return;

        foreach (IsolatingRunSequence seq in sequences)
        {
            int[] indices = seq.indexMap;
            int seqLen = indices.Length;
            if (seqLen == 0)
                continue;

            int k = 0;

            while (k < seqLen)
            {
                int idx = indices[k];
                BidiClass bc = bidiClasses[idx];

                if (!IsNeutralType(bc))
                {
                    k++;
                    continue;
                }

                int neutralStartPos = k;
                int neutralEndPos = k;

                while (neutralEndPos + 1 < seqLen &&
                       IsNeutralType(bidiClasses[indices[neutralEndPos + 1]]))
                {
                    neutralEndPos++;
                }

                BidiClass sGroup = BidiClass.OtherNeutral;
                bool sGroupFromActualChar = false;

                for (int pos = neutralStartPos - 1; pos >= 0; pos--)
                {
                    int j = indices[pos];
                    BidiClass t = bidiClasses[j];

                    if (t == BidiClass.BoundaryNeutral)
                        continue;

                    BidiClass strong = MapToStrongTypeForNeutrals(t);
                    if (strong != BidiClass.OtherNeutral)
                    {
                        sGroup = strong;
                        sGroupFromActualChar = true;
                        break;
                    }
                }

                if (sGroup == BidiClass.OtherNeutral)
                {
                    sGroup = MapToStrongTypeForNeutrals(seq.sos);
                }

                BidiClass eGroup = BidiClass.OtherNeutral;
                bool eGroupFromActualChar = false;

                for (int pos = neutralEndPos + 1; pos < seqLen; pos++)
                {
                    int j = indices[pos];
                    BidiClass t = bidiClasses[j];

                    if (t == BidiClass.BoundaryNeutral)
                        continue;

                    BidiClass strong = MapToStrongTypeForNeutrals(t);
                    if (strong != BidiClass.OtherNeutral)
                    {
                        eGroup = strong;
                        eGroupFromActualChar = true;
                        break;
                    }
                }

                if (eGroup == BidiClass.OtherNeutral)
                {
                    eGroup = MapToStrongTypeForNeutrals(seq.eos);
                }

                BidiClass resolvedType;
                if (sGroup != BidiClass.OtherNeutral &&
                    sGroup == eGroup)
                {
                    resolvedType = sGroup;
                }
                else
                {
                    resolvedType = (seq.level & 1) == 0
                        ? BidiClass.LeftToRight
                        : BidiClass.RightToLeft;
                }

                bool isolateControlsCanChange = sGroupFromActualChar && eGroupFromActualChar;

                for (int pos = neutralStartPos; pos <= neutralEndPos; pos++)
                {
                    int j = indices[pos];
                    BidiClass t = bidiClasses[j];

                    if (t == BidiClass.BoundaryNeutral)
                        continue;

                    if (!isolateControlsCanChange && IsIsolateControl(t))
                        continue;

                    if (IsNeutralType(t))
                    {
                        bidiClasses[j] = resolvedType;
                    }
                }

                k = neutralEndPos + 1;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIsolateControl(BidiClass bc)
    {
        return bc == BidiClass.LeftToRightIsolate ||
               bc == BidiClass.RightToLeftIsolate ||
               bc == BidiClass.FirstStrongIsolate ||
               bc == BidiClass.PopDirectionalIsolate;
    }
    
    private void ApplyLineBreakRuleL1ForParagraph(
        ReadOnlySpan<int> codePoints,
        int start,
        int end,
        byte paragraphBaseLevel,
        byte[] levels)
    {
        if (start > end)
            return;

        int lineStart = start;
        int lineEnd = end;

        for (int i = lineStart; i <= lineEnd; i++)
        {
            BidiClass cls = unicodeData.GetBidiClass(codePoints[i]);

            if (cls == BidiClass.SegmentSeparator || cls == BidiClass.ParagraphSeparator)
            {
                levels[i] = paragraphBaseLevel;

                int j = i - 1;
                while (j >= lineStart)
                {
                    BidiClass prev = unicodeData.GetBidiClass(codePoints[j]);

                    if (prev == BidiClass.WhiteSpace ||
                        prev == BidiClass.LeftToRightIsolate ||
                        prev == BidiClass.RightToLeftIsolate ||
                        prev == BidiClass.FirstStrongIsolate ||
                        prev == BidiClass.PopDirectionalIsolate ||
                        prev == BidiClass.LeftToRightEmbedding ||
                        prev == BidiClass.RightToLeftEmbedding ||
                        prev == BidiClass.LeftToRightOverride ||
                        prev == BidiClass.RightToLeftOverride ||
                        prev == BidiClass.BoundaryNeutral)
                    {
                        levels[j] = paragraphBaseLevel;
                        j--;
                        continue;
                    }

                    break;
                }
            }
        }

        int k = lineEnd;
        while (k >= lineStart)
        {
            BidiClass cls = unicodeData.GetBidiClass(codePoints[k]);

            if (cls == BidiClass.WhiteSpace ||
                cls == BidiClass.LeftToRightIsolate ||
                cls == BidiClass.RightToLeftIsolate ||
                cls == BidiClass.FirstStrongIsolate ||
                cls == BidiClass.PopDirectionalIsolate ||
                cls == BidiClass.LeftToRightEmbedding ||
                cls == BidiClass.RightToLeftEmbedding ||
                cls == BidiClass.LeftToRightOverride ||
                cls == BidiClass.RightToLeftOverride ||
                cls == BidiClass.BoundaryNeutral)
            {
                levels[k] = paragraphBaseLevel;
                k--;
                continue;
            }

            break;
        }
    }


    private void ResolvePairedBracketsForParagraph(
        ReadOnlySpan<int> codePoints,
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
            return;

        List<LevelRun> levelRuns = new List<LevelRun>();
        List<IsolatingRunSequence> sequences = new List<IsolatingRunSequence>();

        int[] matchingIsolate = new int[codePoints.Length];
        int[] runIndexByPosition = new int[codePoints.Length];

        BuildIsolatingRunSequencesForParagraph(
            start,
            end,
            paragraphBaseLevel,
            bidiClasses,
            levels,
            levelRuns,
            sequences,
            matchingIsolate,
            runIndexByPosition);

        for (int s = 0; s < sequences.Count; s++)
        {
            ResolvePairedBracketsForSequence(
                codePoints,
                sequences[s],
                bidiClasses);
        }
    }

    private void ResolvePairedBracketsForSequence(
        ReadOnlySpan<int> codePoints,
        IsolatingRunSequence sequence,
        BidiClass[] bidiClasses)
    {
        const int MaxPairingDepth = 63;

        openStackBuffer.Clear();
        bracketPairsBuffer.Clear();

        int seqLen = sequence.Length;

        for (int k = 0; k < seqLen; k++)
        {
            int index = sequence[k];

            if (bidiClasses[index] != BidiClass.OtherNeutral)
                continue;

            int cp = codePoints[index];
            BidiPairedBracketType bt = unicodeData.GetBidiPairedBracketType(cp);

            if (bt == BidiPairedBracketType.Open)
            {
                if (openStackBuffer.Count >= MaxPairingDepth)
                {
                    bracketPairsBuffer.Clear();
                    openStackBuffer.Clear();
                    break;
                }

                openStackBuffer.Add(index);
            }
            else if (bt == BidiPairedBracketType.Close)
            {
                for (int s = openStackBuffer.Count - 1; s >= 0; s--)
                {
                    int openIndex = openStackBuffer[s];
                    int openCp = codePoints[openIndex];

                    if (BracketsMatch(openCp, cp))
                    {
                        bracketPairsBuffer.Add(new BracketPair(openIndex, index));

                        openStackBuffer.RemoveRange(s, openStackBuffer.Count - s);
                        break;
                    }
                }
            }
        }

        if (bracketPairsBuffer.Count == 0)
            return;

        bracketPairsBuffer.Sort((a, b) => a.openIndex.CompareTo(b.openIndex));

        BidiClass embeddingDir = GetStrongTypeFromLevel(sequence.level);

        int pairsCount = bracketPairsBuffer.Count;
        for (int p = 0; p < pairsCount; p++)
        {
            int openIndex = bracketPairsBuffer[p].openIndex;
            int closeIndex = bracketPairsBuffer[p].closeIndex;

            BidiClass innerMatch = BidiClass.OtherNeutral;
            BidiClass innerOpposite = BidiClass.OtherNeutral;

            for (int k = 0; k < seqLen; k++)
            {
                int idx = sequence[k];

                if (idx <= openIndex || idx >= closeIndex)
                    continue;

                BidiClass strong = MapToStrongTypeForN0(bidiClasses[idx]);

                if (strong != BidiClass.LeftToRight && strong != BidiClass.RightToLeft)
                    continue;

                if (strong == embeddingDir)
                {
                    innerMatch = embeddingDir;
                    break;
                }

                innerOpposite = strong;
            }

            if (innerMatch == embeddingDir)
            {
                bidiClasses[openIndex] = embeddingDir;
                bidiClasses[closeIndex] = embeddingDir;
                continue;
            }

            if (innerOpposite == BidiClass.LeftToRight || innerOpposite == BidiClass.RightToLeft)
            {
                BidiClass preceding = BidiClass.OtherNeutral;

                int openSeqPos = 0;
                for (; openSeqPos < seqLen; openSeqPos++)
                {
                    if (sequence[openSeqPos] == openIndex)
                        break;
                }

                for (int k = openSeqPos - 1; k >= 0; k--)
                {
                    int idx = sequence[k];

                    if (bidiClasses[idx] == BidiClass.BoundaryNeutral)
                        continue;

                    BidiClass strong = MapToStrongTypeForN0(bidiClasses[idx]);
                    if (strong == BidiClass.LeftToRight || strong == BidiClass.RightToLeft)
                    {
                        preceding = strong;
                        break;
                    }
                }

                if (preceding != BidiClass.LeftToRight && preceding != BidiClass.RightToLeft)
                {
                    preceding = sequence.sos;
                }

                bidiClasses[openIndex] = preceding;
                bidiClasses[closeIndex] = preceding;
                continue;
            }
        }

        for (int p = 0; p < pairsCount; p++)
        {
            int openIndex = bracketPairsBuffer[p].openIndex;
            int closeIndex = bracketPairsBuffer[p].closeIndex;

            BidiClass pairType = bidiClasses[openIndex];
            if (pairType != BidiClass.LeftToRight && pairType != BidiClass.RightToLeft)
                continue;

            int openSeqPos = 0;
            for (; openSeqPos < seqLen; openSeqPos++)
            {
                if (sequence[openSeqPos] == openIndex)
                    break;
            }

            int closeSeqPos = 0;
            for (; closeSeqPos < seqLen; closeSeqPos++)
            {
                if (sequence[closeSeqPos] == closeIndex)
                    break;
            }

            int kPos = openSeqPos + 1;
            while (kPos < seqLen)
            {
                int idx = sequence[kPos];
                BidiClass original = unicodeData.GetBidiClass(codePoints[idx]);

                if (original == BidiClass.BoundaryNeutral)
                {
                    kPos++;
                    continue;
                }

                if (original != BidiClass.NonspacingMark)
                    break;

                bidiClasses[idx] = pairType;
                kPos++;
            }

            kPos = closeSeqPos + 1;
            while (kPos < seqLen)
            {
                int idx = sequence[kPos];
                BidiClass original = unicodeData.GetBidiClass(codePoints[idx]);

                if (original == BidiClass.BoundaryNeutral)
                {
                    kPos++;
                    continue;
                }

                if (original != BidiClass.NonspacingMark)
                    break;

                bidiClasses[idx] = pairType;
                kPos++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool BracketsMatch(int openCp, int closeCp)
    {
        BidiPairedBracketType openType = unicodeData.GetBidiPairedBracketType(openCp);
        if (openType != BidiPairedBracketType.None)
        {
            int paired = unicodeData.GetBidiPairedBracket(openCp);
            if (paired == closeCp)
                return true;

            if (openCp == 0x2329 && (closeCp == 0x232A || closeCp == 0x3009))
                return true;
            if (openCp == 0x3008 && (closeCp == 0x232A || closeCp == 0x3009))
                return true;

            return false;
        }

        return openCp == 0x0028 && closeCp == 0x0029;
    }

    private void ResolveWeakTypesForParagraph(
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
            return;

        List<IsolatingRunSequence> sequences = BuildIsolatingRunSequences(
            start,
            end,
            paragraphBaseLevel,
            bidiClasses,
            levels);

        if (sequences.Count == 0)
            return;

        foreach (IsolatingRunSequence seq in sequences)
        {
            ResolveWeakTypesForSequence(seq, bidiClasses);
        }
    }

    private void ResolveWeakTypesForSequence(
        IsolatingRunSequence seq,
        BidiClass[] bidiClasses)
    {
        int[] indices = seq.indexMap;
        int length = indices.Length;

        if (length == 0)
            return;

        {
            BidiClass prevType = seq.sos;

            for (int i = 0; i < length; i++)
            {
                int idx = indices[i];
                BidiClass t = bidiClasses[idx];

                if (t == BidiClass.BoundaryNeutral)
                    continue;

                if (t == BidiClass.NonspacingMark)
                {
                    if (IsIsolateInitiator(prevType) ||
                        prevType == BidiClass.PopDirectionalIsolate)
                    {
                        bidiClasses[idx] = BidiClass.OtherNeutral;
                    }
                    else
                    {
                        bidiClasses[idx] = prevType;
                    }

                    t = bidiClasses[idx];
                }

                prevType = t;
            }
        }

        {
            for (int i = 0; i < length; i++)
            {
                int idx = indices[i];
                if (bidiClasses[idx] != BidiClass.EuropeanNumber)
                    continue;

                BidiClass strong = FindPrevStrongTypeForW2(seq, bidiClasses, indices, i);

                if (strong == BidiClass.ArabicLetter)
                {
                    bidiClasses[idx] = BidiClass.ArabicNumber;
                }
            }
        }

        {
            for (int i = 0; i < length; i++)
            {
                int idx = indices[i];
                if (bidiClasses[idx] == BidiClass.ArabicLetter)
                {
                    bidiClasses[idx] = BidiClass.RightToLeft;
                }
            }
        }

        {
            for (int i = 0; i < length; i++)
            {
                int idx = indices[i];
                BidiClass t = bidiClasses[idx];

                if (t != BidiClass.EuropeanSeparator &&
                    t != BidiClass.CommonSeparator)
                {
                    continue;
                }

                BidiClass before = GetTypeBeforeInSequence(seq, bidiClasses, indices, i);
                BidiClass after = GetTypeAfterInSequence(seq, bidiClasses, indices, i);

                if (t == BidiClass.EuropeanSeparator)
                {
                    if (before == BidiClass.EuropeanNumber &&
                        after == BidiClass.EuropeanNumber)
                    {
                        bidiClasses[idx] = BidiClass.EuropeanNumber;
                    }
                }
                else
                {
                    if (before == BidiClass.EuropeanNumber &&
                        after == BidiClass.EuropeanNumber)
                    {
                        bidiClasses[idx] = BidiClass.EuropeanNumber;
                    }
                    else if (before == BidiClass.ArabicNumber &&
                             after == BidiClass.ArabicNumber)
                    {
                        bidiClasses[idx] = BidiClass.ArabicNumber;
                    }
                }
            }
        }

        {
            int i = 0;
            while (i < length)
            {
                int idx = indices[i];
                if (bidiClasses[idx] != BidiClass.EuropeanTerminator)
                {
                    i++;
                    continue;
                }

                int runStart = i;
                int runEnd = i;

                while (runEnd + 1 < length &&
                       bidiClasses[indices[runEnd + 1]] == BidiClass.EuropeanTerminator)
                {
                    runEnd++;
                }

                BidiClass before = GetTypeBeforeInSequence(seq, bidiClasses, indices, runStart);
                BidiClass after = GetTypeAfterInSequence(seq, bidiClasses, indices, runEnd);

                bool beforeIsEn = before == BidiClass.EuropeanNumber;
                bool afterIsEn = after == BidiClass.EuropeanNumber;

                if (beforeIsEn || afterIsEn)
                {
                    for (int p = runStart; p <= runEnd; p++)
                    {
                        bidiClasses[indices[p]] = BidiClass.EuropeanNumber;
                    }
                }

                i = runEnd + 1;
            }
        }

        {
            for (int i = 0; i < length; i++)
            {
                int idx = indices[i];
                BidiClass t = bidiClasses[idx];

                if (t == BidiClass.EuropeanSeparator ||
                    t == BidiClass.CommonSeparator ||
                    t == BidiClass.EuropeanTerminator)
                {
                    bidiClasses[idx] = BidiClass.OtherNeutral;
                }
            }
        }

        {
            for (int i = 0; i < length; i++)
            {
                int idx = indices[i];
                if (bidiClasses[idx] != BidiClass.EuropeanNumber)
                    continue;

                BidiClass strong = FindPrevStrongTypeForW7(seq, bidiClasses, indices, i);

                if (strong == BidiClass.LeftToRight)
                {
                    bidiClasses[idx] = BidiClass.LeftToRight;
                }
            }
        }
    }

    private void ResolveImplicitLevelsForParagraph(
        int start,
        int end,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        int i = start;

        while (i <= end)
        {
            byte runLevel = levels[i];
            int runStart = i;
            int runEnd = i;

            while (runEnd + 1 <= end && levels[runEnd + 1] == runLevel)
            {
                runEnd++;
            }

            bool isEvenLevel = (runLevel & 1) == 0;

            if (isEvenLevel)
            {
                for (int j = runStart; j <= runEnd; j++)
                {
                    switch (bidiClasses[j])
                    {
                        case BidiClass.RightToLeft:
                        case BidiClass.ArabicLetter:
                            levels[j] = (byte)(runLevel + 1);
                            break;

                        case BidiClass.EuropeanNumber:
                        case BidiClass.ArabicNumber:
                            levels[j] = (byte)(runLevel + 2);
                            break;
                    }
                }
            }
            else
            {
                for (int j = runStart; j <= runEnd; j++)
                {
                    switch (bidiClasses[j])
                    {
                        case BidiClass.LeftToRight:
                        case BidiClass.EuropeanNumber:
                        case BidiClass.ArabicNumber:
                            levels[j] = (byte)(runLevel + 1);
                            break;
                    }
                }
            }

            i = runEnd + 1;
        }
    }


    private void ResolveExplicitLevelsForParagraph(
        int start,
        int end,
        byte baseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        int stackDepth = 1;
        embeddingStackBuffer[0].level = baseLevel;
        embeddingStackBuffer[0].overrideStatus = 0;
        embeddingStackBuffer[0].isIsolate = false;

        byte currentLevel = baseLevel;
        sbyte overrideStatus = 0;

        for (int i = start; i <= end; i++)
        {
            BidiClass bc = bidiClasses[i];

            levels[i] = currentLevel;

            switch (bc)
            {
                case BidiClass.LeftToRightEmbedding:
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: false, overrideClass: 0);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.RightToLeftEmbedding:
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: true, overrideClass: 0);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.LeftToRightOverride:
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: false, overrideClass: 1);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.RightToLeftOverride:
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: true, overrideClass: 2);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.PopDirectionalFormat:
                    PopEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus);
                    levels[i] = currentLevel;
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.LeftToRightIsolate:
                    PushIsolate(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: false);
                    break;

                case BidiClass.RightToLeftIsolate:
                    PushIsolate(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: true);
                    break;

                case BidiClass.FirstStrongIsolate:
                {
                    bool isRtl = ResolveFirstStrongIsolateDirection(i, end, bidiClasses, baseLevel);
                    PushIsolate(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl);
                    break;
                }

                case BidiClass.PopDirectionalIsolate:
                    PopIsolate(ref stackDepth, ref currentLevel, ref overrideStatus);
                    levels[i] = currentLevel;
                    break;

                default:
                    if (overrideStatus == 1)
                    {
                        if (bc != BidiClass.BoundaryNeutral)
                            bidiClasses[i] = BidiClass.LeftToRight;
                    }
                    else if (overrideStatus == 2)
                    {
                        if (bc != BidiClass.BoundaryNeutral)
                            bidiClasses[i] = BidiClass.RightToLeft;
                    }

                    break;
            }
        }
    }
    
    private void PushEmbedding(
        ref int stackDepth,
        ref byte currentLevel,
        ref sbyte overrideStatus,
        bool isRtl,
        sbyte overrideClass)
    {
        byte newLevel;

        if (isRtl)
        {
            newLevel = (byte)((currentLevel + 1) | 1);
        }
        else
        {
            newLevel = (byte)((currentLevel + 2) & ~1);
        }

        if (newLevel > maxExplicitLevel)
        {
            return;
        }

        if (stackDepth >= embeddingStackBuffer.Length)
        {
            return;
        }

        embeddingStackBuffer[stackDepth].level = newLevel;
        embeddingStackBuffer[stackDepth].overrideStatus = overrideClass;
        embeddingStackBuffer[stackDepth].isIsolate = false;
        stackDepth++;

        currentLevel = newLevel;
        overrideStatus = overrideClass;
    }

    private void PopEmbedding(
        ref int stackDepth,
        ref byte currentLevel,
        ref sbyte overrideStatus)
    {
        if (stackDepth <= 1)
            return;

        int topIndex = stackDepth - 1;

        if (embeddingStackBuffer[topIndex].isIsolate)
            return;

        stackDepth--;

        EmbeddingState state = embeddingStackBuffer[stackDepth - 1];
        currentLevel = state.level;
        overrideStatus = state.overrideStatus;
    }

    private void PushIsolate(
        ref int stackDepth,
        ref byte currentLevel,
        ref sbyte overrideStatus,
        bool isRtl)
    {
        byte newLevel;

        if (isRtl)
        {
            newLevel = (byte)((currentLevel + 1) | 1);
        }
        else
        {
            newLevel = (byte)((currentLevel + 2) & ~1);
        }

        if (newLevel > maxExplicitLevel)
        {
            return;
        }

        if (stackDepth >= embeddingStackBuffer.Length)
        {
            return;
        }

        embeddingStackBuffer[stackDepth].level = newLevel;
        embeddingStackBuffer[stackDepth].overrideStatus = 0;
        embeddingStackBuffer[stackDepth].isIsolate = true;
        stackDepth++;

        currentLevel = newLevel;
        overrideStatus = 0;
    }

    private void PopIsolate(ref int stackDepth, ref byte currentLevel, ref sbyte overrideStatus)
    {
        if (stackDepth <= 1) return;

        int isolateIndex = -1;
        for (int i = stackDepth - 1; i >= 1; i--)
        {
            if (embeddingStackBuffer[i].isIsolate)
            {
                isolateIndex = i;
                break;
            }
        }

        if (isolateIndex == -1) return;

        stackDepth = isolateIndex;
        EmbeddingState state = embeddingStackBuffer[stackDepth - 1];
        currentLevel = state.level;
        overrideStatus = state.overrideStatus;
    }

    private void EnsureBidiClassesCapacity(int length)
    {
        if (bidiClassesBuffer.Length < length)
        {
            bidiClassesBuffer = new BidiClass[length];
        }

        if (originalBidiClassesBuffer.Length < length)
        {
            originalBidiClassesBuffer = new BidiClass[length];
        }
    }

    private List<IsolatingRunSequence> BuildIsolatingRunSequencesForParagraph(
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels,
        List<LevelRun> levelRuns,
        List<IsolatingRunSequence> sequences,
        int[] matchingIsolate,
        int[] runIndexByPosition)
    {
        sequences.Clear();

        if (start > end)
            return sequences;

        ComputeIsolatePairs(start, end, bidiClasses, matchingIsolate);

        BuildLevelRunsForParagraph(start, end, levels, bidiClasses, levelRuns);
        int runCount = levelRuns.Count;

        if (runCount == 0)
            return sequences;

        for (int i = start; i <= end; i++)
            runIndexByPosition[i] = -1;

        for (int r = 0; r < runCount; r++)
        {
            LevelRun run = levelRuns[r];
            for (int i = run.startIndex; i <= run.endIndex; i++)
                runIndexByPosition[i] = r;
        }

        int[] nextRun = ArrayPool<int>.Shared.Rent(runCount);
        bool[] hasPredecessor = ArrayPool<bool>.Shared.Rent(runCount);
        bool[] visited = ArrayPool<bool>.Shared.Rent(runCount);
        
        try
        {
            for (int r = 0; r < runCount; r++)
            {
                nextRun[r] = -1;
                hasPredecessor[r] = false;
                visited[r] = false;
            }

            for (int r = 0; r < runCount; r++)
            {
                LevelRun run = levelRuns[r];

                int lastIndex = run.endIndex;
                BidiClass lastType = bidiClasses[lastIndex];

                if (lastType == BidiClass.LeftToRightIsolate ||
                    lastType == BidiClass.RightToLeftIsolate ||
                    lastType == BidiClass.FirstStrongIsolate)
                {
                    int mate = matchingIsolate[lastIndex];
                    if (mate >= 0)
                    {
                        int mateRun = runIndexByPosition[mate];
                        if (mateRun >= 0)
                        {
                            nextRun[r] = mateRun;
                        }
                    }
                }
            }

            for (int r = 0; r < runCount; r++)
            {
                int succ = nextRun[r];
                if (succ >= 0 && succ < runCount)
                    hasPredecessor[succ] = true;
            }

            void AddSequenceFromRun(int startRunIndex)
            {
                int currentRun = startRunIndex;

                seqIndicesBuffer.Clear();

                LevelRun firstRun = levelRuns[currentRun];
                byte sequenceLevel = firstRun.level;

                while (true)
                {
                    visited[currentRun] = true;

                    LevelRun run = levelRuns[currentRun];
                    for (int i = run.startIndex; i <= run.endIndex; i++)
                        seqIndicesBuffer.Add(i);

                    int succ = nextRun[currentRun];
                    if (succ < 0 || visited[succ])
                        break;

                    currentRun = succ;
                }

                int indicesCount = seqIndicesBuffer.Count;
                if (indicesCount == 0)
                    return;

                int sequenceFirstIndex = seqIndicesBuffer[0];
                int sequenceLastIndex = seqIndicesBuffer[indicesCount - 1];

                ComputeSosEosForSequence(
                    paragraphStart: start,
                    paragraphEnd: end,
                    sequenceFirstIndex: sequenceFirstIndex,
                    sequenceLastIndex: sequenceLastIndex,
                    paragraphBaseLevel: paragraphBaseLevel,
                    sequenceLevel: sequenceLevel,
                    bidiClasses: bidiClasses,
                    levels: levels,
                    matchingIsolate: matchingIsolate,
                    out BidiClass sos,
                    out BidiClass eos);

                int[] seqArray = new int[indicesCount];
                for (int i = 0; i < indicesCount; i++)
                    seqArray[i] = seqIndicesBuffer[i];

                sequences.Add(
                    new IsolatingRunSequence(seqArray, sequenceLevel, sos, eos));
            }

            for (int r = 0; r < runCount; r++)
            {
                if (!hasPredecessor[r] && !visited[r])
                    AddSequenceFromRun(r);
            }

            for (int r = 0; r < runCount; r++)
            {
                if (!visited[r])
                    AddSequenceFromRun(r);
            }

            return sequences;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(nextRun);
            ArrayPool<bool>.Shared.Return(hasPredecessor);
            ArrayPool<bool>.Shared.Return(visited);
        }
    }

    private void ComputeIsolatePairs(
        int start,
        int end,
        BidiClass[] bidiClasses,
        int[] matchingIsolate)
    {
        for (int i = start; i <= end; i++)
            matchingIsolate[i] = -1;

        isolatePairStackBuffer.Clear();

        for (int i = start; i <= end; i++)
        {
            BidiClass bc = bidiClasses[i];

            if (bc == BidiClass.LeftToRightIsolate ||
                bc == BidiClass.RightToLeftIsolate ||
                bc == BidiClass.FirstStrongIsolate)
            {
                isolatePairStackBuffer.Add(i);
            }
            else if (bc == BidiClass.PopDirectionalIsolate)
            {
                int count = isolatePairStackBuffer.Count;
                if (count == 0)
                    continue;

                int openIndex = isolatePairStackBuffer[count - 1];
                isolatePairStackBuffer.RemoveAt(count - 1);

                matchingIsolate[openIndex] = i;
                matchingIsolate[i] = openIndex;
            }
        }
    }
    
    
    private static BidiClass ComputeSequenceBoundaryType(
        int start,
        int end,
        byte paragraphBaseLevel,
        int boundaryIndex,
        bool isStart,
        BidiClass[] bidiClasses,
        byte[] levels,
        int[] isolateToPdiRelative)
    {
        byte levelHere = levels[boundaryIndex];
        byte otherLevel;

        if (isStart)
        {
            int i = boundaryIndex - 1;
            while (i >= start)
            {
                if (bidiClasses[i] != BidiClass.BoundaryNeutral)
                {
                    otherLevel = levels[i];
                    goto Compute;
                }

                i--;
            }

            otherLevel = paragraphBaseLevel;
        }
        else
        {
            bool unmatchedIsolate =
                IsIsolateInitiator(bidiClasses[boundaryIndex]) &&
                isolateToPdiRelative[boundaryIndex - start] < 0;

            if (unmatchedIsolate)
            {
                otherLevel = paragraphBaseLevel;
            }
            else
            {
                int i = boundaryIndex + 1;
                while (i <= end)
                {
                    if (bidiClasses[i] != BidiClass.BoundaryNeutral)
                    {
                        otherLevel = levels[i];
                        goto Compute;
                    }

                    i++;
                }

                otherLevel = paragraphBaseLevel;
            }
        }

        Compute:
        byte higher = levelHere >= otherLevel ? levelHere : otherLevel;
        return (higher & 1) == 0
            ? BidiClass.LeftToRight
            : BidiClass.RightToLeft;
    }
    
    private static void BuildParagraphsWithExplicitBaseLevel(
        BidiClass[] bidiClasses,
        int length,
        byte givenBaseLevel,
        List<BidiParagraph> paragraphs)
    {
        int paraStart = 0;
        for (int i = 0; i < length; i++)
        {
            if (bidiClasses[i] == BidiClass.ParagraphSeparator)
            {
                int paraEnd = i - 1;
                if (paraEnd >= paraStart)
                {
                    paragraphs.Add(new BidiParagraph(paraStart, paraEnd, givenBaseLevel));
                }

                paraStart = i + 1;
            }
        }

        if (paraStart < length)
        {
            int paraEnd = length - 1;
            paragraphs.Add(new BidiParagraph(paraStart, paraEnd, givenBaseLevel));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIsolateInitiator(BidiClass bc)
    {
        return bc == BidiClass.LeftToRightIsolate ||
               bc == BidiClass.RightToLeftIsolate ||
               bc == BidiClass.FirstStrongIsolate;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindLastNonBnBefore(int beforeIndex, int start, BidiClass[] bidiClasses)
    {
        for (int i = beforeIndex - 1; i >= start; i--)
        {
            if (bidiClasses[i] != BidiClass.BoundaryNeutral)
                return i;
        }
        return start;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNeutralType(BidiClass bc)
    {
        switch (bc)
        {
            case BidiClass.WhiteSpace:
            case BidiClass.SegmentSeparator:
            case BidiClass.OtherNeutral:
            case BidiClass.BoundaryNeutral:
            case BidiClass.LeftToRightIsolate:
            case BidiClass.RightToLeftIsolate:
            case BidiClass.FirstStrongIsolate:
            case BidiClass.PopDirectionalIsolate:
                return true;

            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BidiClass MapToStrongTypeForNeutrals(BidiClass bc)
    {
        switch (bc)
        {
            case BidiClass.LeftToRight:
                return BidiClass.LeftToRight;

            case BidiClass.RightToLeft:
                return BidiClass.RightToLeft;

            case BidiClass.EuropeanNumber:
            case BidiClass.ArabicNumber:
                return BidiClass.RightToLeft;

            default:
                return BidiClass.OtherNeutral;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BidiClass MapToStrongTypeForN0(BidiClass t)
    {
        switch (t)
        {
            case BidiClass.LeftToRight:
                return BidiClass.LeftToRight;

            case BidiClass.RightToLeft:
            case BidiClass.ArabicLetter:
            case BidiClass.EuropeanNumber:
            case BidiClass.ArabicNumber:
                return BidiClass.RightToLeft;

            default:
                return BidiClass.OtherNeutral;
        }
    }

    private static bool ResolveFirstStrongIsolateDirection(
        int fsiIndex,
        int paragraphEnd,
        BidiClass[] bidiClasses,
        byte paragraphBaseLevel)
    {
        int depth = 1;

        for (int i = fsiIndex + 1; i <= paragraphEnd; i++)
        {
            BidiClass bc = bidiClasses[i];

            switch (bc)
            {
                case BidiClass.LeftToRightIsolate:
                case BidiClass.RightToLeftIsolate:
                case BidiClass.FirstStrongIsolate:
                    depth++;
                    break;

                case BidiClass.PopDirectionalIsolate:
                    depth--;
                    if (depth == 0)
                    {
                        goto EndScan;
                    }

                    break;
            }

            if (depth < 1)
                break;

            if (bc == BidiClass.LeftToRight)
                return false;

            if (bc == BidiClass.RightToLeft || bc == BidiClass.ArabicLetter)
                return true;
        }

        EndScan:
        return (paragraphBaseLevel & 1) == 1;
    }

    private static void BuildParagraphs(BidiClass[] bidiClasses, int length, List<BidiParagraph> paragraphs)
    {
        int paraStart = 0;
        for (int i = 0; i < length; i++)
        {
            if (bidiClasses[i] == BidiClass.ParagraphSeparator)
            {
                int paraEnd = i - 1;
                if (paraEnd >= paraStart)
                {
                    byte baseLevel = ComputeParagraphBaseLevel(bidiClasses, paraStart, paraEnd);
                    paragraphs.Add(new BidiParagraph(paraStart, paraEnd, baseLevel));
                }

                paraStart = i + 1;
            }
        }

        if (paraStart < length)
        {
            int paraEnd = length - 1;
            byte baseLevel = ComputeParagraphBaseLevel(bidiClasses, paraStart, paraEnd);
            paragraphs.Add(new BidiParagraph(paraStart, paraEnd, baseLevel));
        }
    }

    private static byte ComputeParagraphBaseLevel(BidiClass[] bidiClasses, int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            BidiClass bc = bidiClasses[i];
            switch (bc)
            {
                case BidiClass.LeftToRight:
                    return 0;

                case BidiClass.RightToLeft:
                case BidiClass.ArabicLetter:
                    return 1;
            }
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BidiClass GetStrongTypeFromLevel(byte level)
    {
        return (level & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;
    }

    private static BidiClass GetTypeBeforeInSequence(
        IsolatingRunSequence seq,
        BidiClass[] classes,
        int[] indices,
        int position)
    {
        for (int i = position - 1; i >= 0; i--)
        {
            BidiClass t = classes[indices[i]];
            if (t == BidiClass.BoundaryNeutral)
                continue;

            return t;
        }

        return seq.sos;
    }

    private static BidiClass GetTypeAfterInSequence(
        IsolatingRunSequence seq,
        BidiClass[] classes,
        int[] indices,
        int position)
    {
        for (int i = position + 1; i < indices.Length; i++)
        {
            BidiClass t = classes[indices[i]];
            if (t == BidiClass.BoundaryNeutral)
                continue;

            return t;
        }

        return seq.eos;
    }

    private static BidiClass FindPrevStrongTypeForW2(
        IsolatingRunSequence seq,
        BidiClass[] classes,
        int[] indices,
        int position)
    {
        for (int i = position - 1; i >= 0; i--)
        {
            BidiClass t = classes[indices[i]];

            if (t == BidiClass.BoundaryNeutral)
                continue;

            if (t == BidiClass.LeftToRight ||
                t == BidiClass.RightToLeft ||
                t == BidiClass.ArabicLetter)
            {
                return t;
            }
        }

        return seq.sos;
    }

    private static BidiClass FindPrevStrongTypeForW7(
        IsolatingRunSequence seq,
        BidiClass[] classes,
        int[] indices,
        int position)
    {
        for (int i = position - 1; i >= 0; i--)
        {
            BidiClass t = classes[indices[i]];

            if (IsNeutralType(t))
                continue;

            if (t == BidiClass.LeftToRight ||
                t == BidiClass.RightToLeft)
            {
                return t;
            }
        }

        return seq.sos;
    }

    private static void BuildLevelRunsForParagraph(
        int start,
        int end,
        byte[] levels,
        BidiClass[] bidiClasses,
        List<LevelRun> levelRuns)
    {
        levelRuns.Clear();

        if (start > end)
            return;

        int runStart = -1;
        byte currentLevel = 0;

        for (int i = start; i <= end; i++)
        {
            if (bidiClasses[i] == BidiClass.BoundaryNeutral)
                continue;

            byte level = levels[i];
            
            if (runStart == -1)
            {
                runStart = i;
                currentLevel = level;
            }
            else if (level != currentLevel)
            {
                levelRuns.Add(new LevelRun(runStart, FindLastNonBnBefore(i, start, bidiClasses), currentLevel));
                runStart = i;
                currentLevel = level;
            }
        }

        if (runStart != -1)
        {
            levelRuns.Add(new LevelRun(runStart, FindLastNonBnBefore(end + 1, start, bidiClasses), currentLevel));
        }
    }

    private static void ComputeSosEosForSequence(
        int paragraphStart,
        int paragraphEnd,
        int sequenceFirstIndex,
        int sequenceLastIndex,
        byte paragraphBaseLevel,
        byte sequenceLevel,
        BidiClass[] bidiClasses,
        byte[] levels,
        int[] matchingIsolate,
        out BidiClass sos,
        out BidiClass eos)
    {
        byte leftLevel;

        int prevIndex = sequenceFirstIndex - 1;
        while (prevIndex >= paragraphStart && bidiClasses[prevIndex] == BidiClass.BoundaryNeutral)
            prevIndex--;

        if (prevIndex >= paragraphStart)
            leftLevel = levels[prevIndex];
        else
            leftLevel = paragraphBaseLevel;

        byte maxLeft = leftLevel >= sequenceLevel ? leftLevel : sequenceLevel;
        sos = GetStrongTypeFromLevel(maxLeft);

        int lastNonBn = sequenceLastIndex;
        while (lastNonBn >= sequenceFirstIndex && bidiClasses[lastNonBn] == BidiClass.BoundaryNeutral)
            lastNonBn--;

        bool lastIsIsolateInitiatorWithoutMatch = false;
        if (lastNonBn >= sequenceFirstIndex)
        {
            BidiClass lastType = bidiClasses[lastNonBn];
            if (lastType == BidiClass.LeftToRightIsolate ||
                lastType == BidiClass.RightToLeftIsolate ||
                lastType == BidiClass.FirstStrongIsolate)
            {
                int mate = matchingIsolate[lastNonBn];
                if (mate < 0)
                    lastIsIsolateInitiatorWithoutMatch = true;
            }
        }

        byte rightLevel;
        int nextIndex = sequenceLastIndex + 1;

        while (nextIndex <= paragraphEnd && bidiClasses[nextIndex] == BidiClass.BoundaryNeutral)
            nextIndex++;

        if (nextIndex <= paragraphEnd && !lastIsIsolateInitiatorWithoutMatch)
            rightLevel = levels[nextIndex];
        else
            rightLevel = paragraphBaseLevel;

        byte maxRight = rightLevel >= sequenceLevel ? rightLevel : sequenceLevel;
        eos = GetStrongTypeFromLevel(maxRight);
    }
}