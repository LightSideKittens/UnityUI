using System;
using System.Collections.Generic;

public readonly struct BidiParagraph
{
    public readonly int startIndex;
    public readonly int endIndex;
    public readonly byte baseLevel;

    public BidiParagraph(int startIndex, int endIndex, byte baseLevel)
    {
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.baseLevel = baseLevel;
    }

    public BidiDirection Direction => (baseLevel & 1) == 0 ? BidiDirection.LeftToRight : BidiDirection.RightToLeft;
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
    private struct EmbeddingState
    {
        public byte level;
        public sbyte overrideStatus;
        public bool isIsolate;
    }

    private const int maxExplicitLevel = 125;

    private readonly BidiClass[] emptyBidiClassArray = Array.Empty<BidiClass>();

    private readonly EmbeddingState[] embeddingStackBuffer = new EmbeddingState[maxExplicitLevel + 2];

    private readonly IUnicodeDataProvider unicodeData;

    private BidiClass[] bidiClassesBuffer = Array.Empty<BidiClass>();

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

            bidiClassesBuffer[i] = unicodeData.GetBidiClass(cp);
        }

        List<BidiParagraph> paragraphList;
        if (forcedParagraphLevel.HasValue)
        {
            paragraphList = BuildParagraphsWithExplicitBaseLevel(
                bidiClassesBuffer,
                length,
                forcedParagraphLevel.Value);
        }
        else
        {
            paragraphList = BuildParagraphs(bidiClassesBuffer, length);
        }

        byte[] levels = new byte[length];

        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolveExplicitLevelsForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolveWeakTypesForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolvePairedBracketsForParagraph(
                codePoints,
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolveNeutralTypesForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                paragraph.baseLevel,
                bidiClassesBuffer,
                levels);
        }

        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolveImplicitLevelsForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                bidiClassesBuffer,
                levels);
        }

        BidiParagraph[] paragraphs = paragraphList.ToArray();
        return new BidiResult(levels, paragraphs);
    }

    private void ResolveNeutralTypesForParagraph(
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
        {
            return;
        }

        int i = start;
        while (i <= end)
        {
            if (!IsNeutralType(bidiClasses[i]))
            {
                i++;
                continue;
            }

            // Запоминаем уровень текущей серии нейтральных символов.
            // Символы внутри изолятов будут иметь более высокий уровень и не должны смешиваться с этой группой.
            byte runLevel = levels[i];

            int neutralStart = i;
            int neutralEnd = i;

            // Группируем нейтральные символы, но ТОЛЬКО те, что находятся на том же уровне.
            while (neutralEnd + 1 <= end && 
                   IsNeutralType(bidiClasses[neutralEnd + 1]) &&
                   levels[neutralEnd + 1] == runLevel)
            {
                neutralEnd++;
            }

            // 1. Ищем сильный тип СЛЕВА (sGroup)
            BidiClass sGroup = BidiClass.OtherNeutral;
            for (int j = neutralStart - 1; j >= start; j--)
            {
                // ПРОПУСКАЕМ символы с более высоким уровнем (содержимое изолятов)
                if (levels[j] > runLevel) continue;

                BidiClass t = bidiClasses[j];
                if (t == BidiClass.BoundaryNeutral) continue;

                BidiClass strong = MapToStrongTypeForNeutrals(t, levels[j]);
                if (strong != BidiClass.OtherNeutral)
                {
                    sGroup = strong;
                    break;
                }
            }
            
            // Fallback для sGroup: направление текущего уровня (SOS)
            if (sGroup == BidiClass.OtherNeutral)
            {
                sGroup = (runLevel & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;
            }

            // 2. Ищем сильный тип СПРАВА (eGroup)
            BidiClass eGroup = BidiClass.OtherNeutral;
            for (int j = neutralEnd + 1; j <= end; j++)
            {
                // ПРОПУСКАЕМ символы с более высоким уровнем (содержимое изолятов)
                if (levels[j] > runLevel) continue;

                BidiClass t = bidiClasses[j];
                if (t == BidiClass.BoundaryNeutral) continue;

                BidiClass strong = MapToStrongTypeForNeutrals(t, levels[j]);
                if (strong != BidiClass.OtherNeutral)
                {
                    eGroup = strong;
                    break;
                }
            }

            // Fallback для eGroup: направление текущего уровня (EOS)
            if (eGroup == BidiClass.OtherNeutral)
            {
                eGroup = (runLevel & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;
            }

            // 3. Разрешение типов
            BidiClass resolvedGroup;
            if (sGroup == eGroup)
            {
                resolvedGroup = sGroup;
            }
            else
            {
                // Если типы разные, используем направление текущего уровня вложения (не параграфа!)
                resolvedGroup = (runLevel & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;
            }

            BidiClass resolvedType = resolvedGroup == BidiClass.RightToLeft
                ? BidiClass.RightToLeft
                : BidiClass.LeftToRight;

            for (int j = neutralStart; j <= neutralEnd; j++)
            {
                // Присваиваем вычисленный тип (включая LRI/PDI, которые являются частью этой группы)
                bidiClasses[j] = resolvedType; 
            }

            i = neutralEnd + 1;
        }
    }

    private sealed class BracketPair
    {
        public int openIndex;
        public int closeIndex;

        public BracketPair(int openIndex, int closeIndex)
        {
            this.openIndex = openIndex;
            this.closeIndex = closeIndex;
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

        var openStack = new List<int>();
        var pairs = new List<BracketPair>();

        for (int i = start; i <= end; i++)
        {
            BidiClass bc = bidiClasses[i];

            if (bc != BidiClass.OtherNeutral)
                continue;

            int cp = codePoints[i];
            BidiPairedBracketType bracketType = unicodeData.GetBidiPairedBracketType(cp);

            if (bracketType == BidiPairedBracketType.None)
                continue;

            if (bracketType == BidiPairedBracketType.Open)
            {
                if (openStack.Count >= 63)
                {
                    openStack.Clear();
                    pairs.Clear();
                    break;
                }

                openStack.Add(i);
            }
            else if (bracketType == BidiPairedBracketType.Close)
            {
                for (int s = openStack.Count - 1; s >= 0; s--)
                {
                    int openIndex = openStack[s];
                    int openCp = codePoints[openIndex];

                    if (BracketsMatch(openCp, cp))
                    {
                        pairs.Add(new BracketPair(openIndex, i));
                        openStack.RemoveRange(s, openStack.Count - s); 
                        break;
                    }
                }
            }
        }

        if (pairs.Count == 0)
            return;

        pairs.Sort(static (a, b) => a.openIndex.CompareTo(b.openIndex));

        foreach (BracketPair pair in pairs)
        {
            int openIndex = pair.openIndex;
            int closeIndex = pair.closeIndex;

            if (openIndex < start || closeIndex > end || openIndex >= closeIndex)
                continue;

            BidiClass embeddingDir =
                (levels[openIndex] & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;

            bool hasAnyStrongInside = false;
            bool hasStrongMatchingEmbedding = false;

            for (int k = openIndex + 1; k < closeIndex; k++)
            {
                BidiClass strong = MapToStrongTypeForN0(bidiClasses[k]);
                if (strong != BidiClass.LeftToRight && strong != BidiClass.RightToLeft)
                    continue;

                hasAnyStrongInside = true;
                if (strong == embeddingDir)
                    hasStrongMatchingEmbedding = true;
            }

            if (!hasAnyStrongInside)
                continue;

            BidiClass resolvedDir;

            if (hasStrongMatchingEmbedding)
            {
                resolvedDir = embeddingDir;
            }
            else
            {
                BidiClass prevStrong = FindPreviousStrongForN0(
                    codePoints,
                    start,
                    openIndex,
                    paragraphBaseLevel,
                    bidiClasses,
                    levels);

                if (prevStrong != BidiClass.LeftToRight && prevStrong != BidiClass.RightToLeft)
                {
                    // N0c: use embedding direction
                    prevStrong = embeddingDir;
                }

                resolvedDir = (prevStrong != embeddingDir) ? prevStrong : embeddingDir;
            }

            bidiClasses[openIndex] = resolvedDir;
            bidiClasses[closeIndex] = resolvedDir;

            PropagateNsmAfterBracket(codePoints, end, openIndex, resolvedDir, bidiClasses);
            PropagateNsmAfterBracket(codePoints, end, closeIndex, resolvedDir, bidiClasses);
        }
    }

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

    private BidiClass FindPreviousStrongForN0(
        ReadOnlySpan<int> codePoints,
        int start,
        int openIndex,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        for (int i = openIndex - 1; i >= start; i--)
        {
            BidiClass strong = MapToStrongTypeForN0(bidiClasses[i]);
            if (strong == BidiClass.LeftToRight || strong == BidiClass.RightToLeft)
                return strong;
        }

        return BidiClass.OtherNeutral;
    }

    private void PropagateNsmAfterBracket(
        ReadOnlySpan<int> codePoints,
        int end,
        int bracketIndex,
        BidiClass resolvedDir,
        BidiClass[] bidiClasses)
    {
        int i = bracketIndex + 1;

        while (i <= end)
        {
            int cp = codePoints[i];

            BidiClass original = unicodeData.GetBidiClass(cp);

            if (original == BidiClass.NonspacingMark)
            {
                bidiClasses[i] = resolvedDir;
                i++;
                continue;
            }

            if (original == BidiClass.BoundaryNeutral)
            {
                i++;
                continue;
            }

            break;
        }
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

        BidiClass paragraphBaseType = GetParagraphBaseType(paragraphBaseLevel);

        // W1
        {
            BidiClass prevType = paragraphBaseType;

            for (int i = start; i <= end; i++)
            {
                BidiClass t = bidiClasses[i];

                if (t == BidiClass.BoundaryNeutral)
                    continue;

                if (t == BidiClass.NonspacingMark)
                {
                    if (IsIsolateInitiator(prevType) || prevType == BidiClass.PopDirectionalIsolate)
                    {
                        bidiClasses[i] = BidiClass.OtherNeutral;
                    }
                    else
                    {
                        bidiClasses[i] = prevType;
                    }

                    continue;
                }

                prevType = t;
            }
        }

        // W2: EN -> AN if last strong was AL
        {
            for (int i = start; i <= end; i++)
            {
                if (bidiClasses[i] != BidiClass.EuropeanNumber)
                    continue;

                int j = i - 1;
                while (j >= start)
                {
                    BidiClass t = bidiClasses[j];
                    if (t == BidiClass.BoundaryNeutral) { j--; continue; }

                    if (IsStrongType(t))
                    {
                        if (t == BidiClass.ArabicLetter)
                        {
                            bidiClasses[i] = BidiClass.ArabicNumber;
                        }
                        break;
                    }
                    j--;
                }
            }
        }

        // W3: AL -> R
        {
            for (int i = start; i <= end; i++)
            {
                if (bidiClasses[i] == BidiClass.ArabicLetter)
                {
                    bidiClasses[i] = BidiClass.RightToLeft;
                }
            }
        }

        // W4: Separators
        {
            for (int i = start; i <= end; i++)
            {
                BidiClass t = bidiClasses[i];

                if (t != BidiClass.EuropeanSeparator && t != BidiClass.CommonSeparator)
                    continue;

                int prevIndex = FindPreviousNonBoundaryIndex(start, i, bidiClasses);
                int nextIndex = FindNextNonBoundaryIndex(end, i, bidiClasses);

                if (prevIndex < 0 || nextIndex < 0)
                {
                    bidiClasses[i] = BidiClass.OtherNeutral;
                    continue;
                }

                BidiClass before = bidiClasses[prevIndex];
                BidiClass after = bidiClasses[nextIndex];

                if (before == BidiClass.EuropeanNumber && after == BidiClass.EuropeanNumber)
                {
                    bidiClasses[i] = BidiClass.EuropeanNumber;
                }
                else if (t == BidiClass.CommonSeparator &&
                         before == BidiClass.ArabicNumber && after == BidiClass.ArabicNumber)
                {
                    bidiClasses[i] = BidiClass.ArabicNumber;
                }
                else
                {
                    bidiClasses[i] = BidiClass.OtherNeutral;
                }
            }
        }

        // W5: ET
        {
            for (int i = start; i <= end; i++)
            {
                if (bidiClasses[i] != BidiClass.EuropeanTerminator)
                    continue;

                int prevIndex = FindPreviousNonBoundaryIndex(start, i, bidiClasses);
                int nextIndex = FindNextNonBoundaryIndex(end, i, bidiClasses);

                bool prevIsEn = prevIndex >= 0 && bidiClasses[prevIndex] == BidiClass.EuropeanNumber;
                bool nextIsEn = nextIndex >= 0 && bidiClasses[nextIndex] == BidiClass.EuropeanNumber;

                if (prevIsEn || nextIsEn)
                {
                    bidiClasses[i] = BidiClass.EuropeanNumber;
                }
            }
        }

        // W6: Separators/Terminators -> ON
        {
            for (int i = start; i <= end; i++)
            {
                BidiClass t = bidiClasses[i];

                if (t == BidiClass.EuropeanSeparator ||
                    t == BidiClass.EuropeanTerminator ||
                    t == BidiClass.CommonSeparator)
                {
                    bidiClasses[i] = BidiClass.OtherNeutral;
                }
            }
        }

        // W7: EN -> L if strong is L
        {
            BidiClass lastStrong = paragraphBaseType;

            for (int i = start; i <= end; i++)
            {
                BidiClass t = bidiClasses[i];

                if (IsStrongType(t))
                {
                    lastStrong = t;
                    continue;
                }

                if (t == BidiClass.EuropeanNumber && lastStrong == BidiClass.LeftToRight)
                {
                    bidiClasses[i] = BidiClass.LeftToRight;
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
                    BidiClass bc = bidiClasses[j];

                    switch (bc)
                    {
                        case BidiClass.LeftToRight:
                            break;

                        case BidiClass.RightToLeft:
                        case BidiClass.ArabicLetter:
                            levels[j] = (byte)(runLevel + 1);
                            break;

                        case BidiClass.EuropeanNumber:
                        case BidiClass.ArabicNumber:
                            levels[j] = (byte)(runLevel + 2);
                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                for (int j = runStart; j <= runEnd; j++)
                {
                    BidiClass bc = bidiClasses[j];

                    switch (bc)
                    {
                        case BidiClass.RightToLeft:
                        case BidiClass.ArabicLetter:
                            break;

                        case BidiClass.LeftToRight:
                        case BidiClass.EuropeanNumber:
                        case BidiClass.ArabicNumber:
                            levels[j] = (byte)(runLevel + 1);
                            break;

                        default:
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
                    bool isRtl = ResolveFirstStrongIsolateDirection(i, start, end, bidiClasses, baseLevel);
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
            newLevel = (byte)(((currentLevel + 1) | 1));
        }
        else
        {
            newLevel = (byte)(((currentLevel + 2) & ~1));
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
            newLevel = (byte)(((currentLevel + 1) | 1));
        }
        else
        {
            newLevel = (byte)(((currentLevel + 2) & ~1));
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
    }

    private static List<BidiParagraph> BuildParagraphsWithExplicitBaseLevel(
        BidiClass[] bidiClasses,
        int length,
        byte givenBaseLevel)
    {
        var paragraphs = new List<BidiParagraph>();

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

        return paragraphs;
    }

    private static bool IsStrongType(BidiClass bc)
    {
        return bc == BidiClass.LeftToRight ||
               bc == BidiClass.RightToLeft ||
               bc == BidiClass.ArabicLetter;
    }

    private static bool IsIsolateInitiator(BidiClass bc)
    {
        return bc == BidiClass.LeftToRightIsolate ||
               bc == BidiClass.RightToLeftIsolate ||
               bc == BidiClass.FirstStrongIsolate;
    }

    private static int FindPreviousNonBoundaryIndex(int start, int index, BidiClass[] classes)
    {
        for (int i = index - 1; i >= start; i--)
        {
            BidiClass bc = classes[i];
            if (bc != BidiClass.BoundaryNeutral)
                return i;
        }

        return -1;
    }

    private static int FindNextNonBoundaryIndex(int end, int index, BidiClass[] classes)
    {
        for (int i = index + 1; i <= end; i++)
        {
            BidiClass bc = classes[i];
            if (bc != BidiClass.BoundaryNeutral)
                return i;
        }

        return -1;
    }

    private static bool IsNeutralType(BidiClass bc)
    {
        switch (bc)
        {
            case BidiClass.WhiteSpace:
            case BidiClass.OtherNeutral:
            case BidiClass.ParagraphSeparator:
            case BidiClass.SegmentSeparator:
            case BidiClass.LeftToRightIsolate:
            case BidiClass.RightToLeftIsolate:
            case BidiClass.FirstStrongIsolate:
            case BidiClass.PopDirectionalIsolate:
            // Добавлено: эти типы тоже участвуют в цепочках нейтральных символов
            case BidiClass.BoundaryNeutral:
            case BidiClass.PopDirectionalFormat:
                return true;

            default:
                return false;
        }
    }


    private static BidiClass GetParagraphBaseType(byte paragraphBaseLevel)
    {
        return (paragraphBaseLevel & 1) == 0
            ? BidiClass.LeftToRight
            : BidiClass.RightToLeft;
    }

    private static BidiClass MapToStrongTypeForNeutrals(BidiClass bc, byte level)
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

    private static BidiClass MapToStrongTypeForN0(BidiClass t)
    {
        switch (t)
        {
            case BidiClass.LeftToRight:
                return BidiClass.LeftToRight;

            case BidiClass.RightToLeft:
            case BidiClass.ArabicLetter:
            // Вернули цифры для N0
            case BidiClass.EuropeanNumber:
            case BidiClass.ArabicNumber:
                return BidiClass.RightToLeft;

            default:
                return BidiClass.OtherNeutral;
        }
    }

    private static bool ResolveFirstStrongIsolateDirection(
        int fsiIndex,
        int paragraphStart,
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

    private static List<BidiParagraph> BuildParagraphs(BidiClass[] bidiClasses, int length)
    {
        var paragraphs = new List<BidiParagraph>();

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

        return paragraphs;
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
}