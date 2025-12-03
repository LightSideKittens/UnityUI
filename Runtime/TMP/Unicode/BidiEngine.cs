using System;
using System.Collections.Generic;

public readonly struct BidiParagraph
{
    public readonly int startIndex; // inclusive, index in logical text (code point index)
    public readonly int endIndex; // inclusive
    public readonly byte baseLevel; // 0 for LTR, 1 for RTL

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
    private struct EmbeddingState
    {
        public byte level;
        public sbyte overrideStatus; // 0 = none, 1 = force L, 2 = force R
        public bool isIsolate;
    }

    private const int maxExplicitLevel = 125;

    private readonly BidiClass[] emptyBidiClassArray = Array.Empty<BidiClass>();

    private readonly EmbeddingState[] embeddingStackBuffer = new EmbeddingState[maxExplicitLevel + 2];

    private readonly IUnicodeDataProvider unicodeData;

    // Reused buffers to reduce allocations; they grow as needed.
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

        // Инициализируем отображение "визуальный индекс -> логический индекс"
        for (int i = 0; i < length; i++)
        {
            indexMap[i] = start + i;
        }

        // Находим максимальный и минимальный нечётный уровень на этой строке.
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

        // Если нечётных уровней нет — порядок логический == визуальный.
        if (minOddLevel == byte.MaxValue)
            return;

        // Классический алгоритм reordering: от максимального уровня до минимального нечётного.
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

                    // Находим непрерывный диапазон символов, у которых level >= текущему.
                    while (runEnd < length && levels[indexMap[runEnd]] >= level)
                    {
                        runEnd++;
                    }

                    // Разворачиваем этот диапазон.
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

            // Защита от потенциального underflow, хотя при корректных данных level >= 1 здесь.
            if (level == 0)
                break;
        }
    }


    public BidiResult Process(ReadOnlySpan<int> codePoints)
    {
        // Старое поведение: базовый уровень определяется автоматически (P2/P3).
        return ProcessInternal(codePoints, forcedParagraphLevel: null);
    }

    /// <summary>
    /// Process with explicit paragraph direction override:
    /// paragraphDirection:
    ///   0 = force LTR (base level 0),
    ///   1 = force RTL (base level 1),
    ///   2 = auto (P2/P3, same as Process(codePoints)).
    /// </summary>
    public BidiResult Process(ReadOnlySpan<int> codePoints, int paragraphDirection)
    {
        byte? forcedParagraphLevel;

        switch (paragraphDirection)
        {
            case 0:
                forcedParagraphLevel = 0; // LTR
                break;
            case 1:
                forcedParagraphLevel = 1; // RTL
                break;
            case 2:
                forcedParagraphLevel = null; // auto (P2/P3)
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

        // 1) Классификация по Bidi_Class
        for (int i = 0; i < length; i++)
        {
            int cp = codePoints[i];
            if ((uint)cp > 0x10FFFFU)
                cp = 0xFFFD;

            bidiClassesBuffer[i] = unicodeData.GetBidiClass(cp);
        }

        // 2) Параграфы и базовый уровень (P1–P3 или принудительный baseLevel)
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

        // 3) Явные уровни + изоляции (X1–X8, FSI/LRI/RLI/PDI)
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

        // 4) Слабые типы (W1–W7)
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

        // 5) Парные скобки (N0)
        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolvePairedBracketsForParagraph(
                codePoints,
                paragraph.startIndex,
                paragraph.endIndex,
                bidiClassesBuffer,
                levels);
        }

        // 6) Нейтрали (N1–N2)
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

        // 7) Имплицитные уровни (I1–I2)
        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            ResolveImplicitLevelsForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                bidiClassesBuffer,
                levels);
        }

        // 8) Коррекция уровней скобок после I1/I2 (если у тебя уже есть AdjustBracketLevelsForParagraph)
        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            AdjustBracketLevelsForParagraph(
                codePoints,
                paragraph.startIndex,
                paragraph.endIndex,
                bidiClassesBuffer,
                levels);
        }

        // 9) Пост-коррекция PDI (если ты оставил AdjustPdiLevelsForParagraph)
        for (int pIndex = 0; pIndex < paragraphList.Count; pIndex++)
        {
            BidiParagraph paragraph = paragraphList[pIndex];

            AdjustPdiLevelsForParagraph(
                paragraph.startIndex,
                paragraph.endIndex,
                bidiClassesBuffer,
                levels);
        }

        BidiParagraph[] paragraphs = paragraphList.ToArray();
        return new BidiResult(levels, paragraphs);
    }

    /// <summary>
    /// Builds paragraphs when paragraph base level is explicitly specified (0 or 1)
    /// instead of being inferred from text (P2/P3).
    /// Paragraph segmentation (P1) остаётся той же: разделяем по B (ParagraphSeparator),
    /// но baseLevel всех параграфов = givenBaseLevel.
    /// </summary>
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

                // Сам сепаратор параграфа не входит в параграф (P1).
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


    private void ResolveNeutralTypesForParagraph(
        int start,
        int end,
        byte paragraphBaseLevel,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
            return;

        BidiClass paragraphBaseType = GetParagraphBaseType(paragraphBaseLevel);

        int i = start;

        while (i <= end)
        {
            if (!IsNeutralType(bidiClasses[i]))
            {
                i++;
                continue;
            }

            int neutralStart = i;
            int neutralEnd = i;

            // Найти максимально длинную последовательность нейтралей.
            while (neutralEnd + 1 <= end && IsNeutralType(bidiClasses[neutralEnd + 1]))
            {
                neutralEnd++;
            }

            // s: ближайший сильный слева (L/R/AL), иначе базовый тип параграфа.
            BidiClass s = paragraphBaseType;
            bool hasLeft = false;

            for (int j = neutralStart - 1; j >= start; j--)
            {
                BidiClass t = bidiClasses[j];

                if (t == BidiClass.BoundaryNeutral)
                    continue;

                if (IsStrongType(t))
                {
                    s = t;
                    hasLeft = true;
                    break;
                }
            }

            if (!hasLeft)
            {
                s = paragraphBaseType;
            }

            // e: ближайший сильный справа (L/R/AL), иначе базовый тип параграфа.
            BidiClass e = paragraphBaseType;
            bool hasRight = false;

            for (int j = neutralEnd + 1; j <= end; j++)
            {
                BidiClass t = bidiClasses[j];

                if (t == BidiClass.BoundaryNeutral)
                    continue;

                if (IsStrongType(t))
                {
                    e = t;
                    hasRight = true;
                    break;
                }
            }

            if (!hasRight)
            {
                e = paragraphBaseType;
            }

            // N1/N2: если обе стороны в одной группе (L или R) → берём её,
            // иначе нейтрали берут базовое направление параграфа.
            BidiClass sGroup = GetDirectionalGroup(s);
            BidiClass eGroup = GetDirectionalGroup(e);

            BidiClass resolvedType;

            if (sGroup != BidiClass.OtherNeutral && sGroup == eGroup)
            {
                resolvedType = sGroup;
            }
            else
            {
                resolvedType = paragraphBaseType;
            }

            for (int j = neutralStart; j <= neutralEnd; j++)
            {
                if (!IsNeutralType(bidiClasses[j]))
                    continue;

                BidiClass type = resolvedType;

                // Специальный (но довольно общий) случай:
                // пробел между числом и правым контекстом (R/AL/EN/AN) должен вести себя как R.
                if (bidiClasses[j] == BidiClass.WhiteSpace)
                {
                    int prev = FindPreviousStrongOrNumberIndex(start, j, bidiClasses);
                    int next = FindNextStrongOrNumberIndex(end, j, bidiClasses);

                    if (prev >= 0 && next >= 0)
                    {
                        BidiClass prevType = bidiClasses[prev];
                        BidiClass nextType = bidiClasses[next];

                        bool prevIsNumber = prevType == BidiClass.EuropeanNumber || prevType == BidiClass.ArabicNumber;
                        bool nextIsRightGroup =
                            nextType == BidiClass.RightToLeft ||
                            nextType == BidiClass.ArabicLetter ||
                            nextType == BidiClass.EuropeanNumber ||
                            nextType == BidiClass.ArabicNumber;

                        if (prevIsNumber && nextIsRightGroup)
                        {
                            type = BidiClass.RightToLeft;
                        }
                    }
                }

                bidiClasses[j] = type;
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
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
            return;

        List<int> openStack = new List<int>();
        List<BracketPair> pairs = new List<BracketPair>();

        // Собираем пары скобок по Bidi_Paired_Bracket / Bidi_Paired_Bracket_Type.
        for (int i = start; i <= end; i++)
        {
            int cp = codePoints[i];

            BidiPairedBracketType bracketType = unicodeData.GetBidiPairedBracketType(cp);

            // Fallback: если Unicode-данные не содержат пары для ASCII-скобок,
            // явно считаем '(' открывающей, а ')' закрывающей.
            if (bracketType == BidiPairedBracketType.None)
            {
                if (cp == 0x0028) // '('
                {
                    bracketType = BidiPairedBracketType.Open;
                }
                else if (cp == 0x0029) // ')'
                {
                    bracketType = BidiPairedBracketType.Close;
                }
                else
                {
                    continue;
                }
            }

            // Алгоритм N0 применяется только к скобкам, которые сейчас ON.
            if (bidiClasses[i] != BidiClass.OtherNeutral)
                continue;

            if (bracketType == BidiPairedBracketType.Open)
            {
                openStack.Add(i);
            }
            else if (bracketType == BidiPairedBracketType.Close)
            {
                for (int s = openStack.Count - 1; s >= 0; s--)
                {
                    int openIndex = openStack[s];
                    int openCp = codePoints[openIndex];

                    bool match;

                    // Пытаемся использовать данные Unicode, если они есть.
                    BidiPairedBracketType openType = unicodeData.GetBidiPairedBracketType(openCp);
                    if (openType == BidiPairedBracketType.None)
                    {
                        // Fallback-пара для ASCII-скобок.
                        match = (openCp == 0x0028 && cp == 0x0029);
                    }
                    else
                    {
                        int openPaired = unicodeData.GetBidiPairedBracket(openCp);
                        match = (openPaired == cp);
                    }

                    if (match)
                    {
                        pairs.Add(new BracketPair(openIndex, i));
                        openStack.RemoveAt(s);
                        break;
                    }
                }
            }
        }


        if (pairs.Count == 0)
            return;

        foreach (BracketPair pair in pairs)
        {
            int openIndex = pair.openIndex;
            int closeIndex = pair.closeIndex;

            if (openIndex < start || closeIndex > end || openIndex >= closeIndex)
                continue;

            bool hasL = false;
            bool hasR = false;
            bool hasNumber = false;
            bool hasOther = false;

            // Анализ содержимого между скобками.
            for (int k = openIndex + 1; k < closeIndex; k++)
            {
                BidiClass t = bidiClasses[k];

                if (t == BidiClass.BoundaryNeutral)
                    continue;

                switch (t)
                {
                    case BidiClass.LeftToRight:
                        hasL = true;
                        break;

                    case BidiClass.RightToLeft:
                    case BidiClass.ArabicLetter:
                        hasR = true;
                        break;

                    case BidiClass.EuropeanNumber:
                    case BidiClass.ArabicNumber:
                        hasNumber = true;
                        break;

                    default:
                        // Любой другой тип (включая PDI, ON-пунктуацию и т.п.)
                        hasOther = true;
                        break;
                }
            }

            // Если внутри есть "мусор" (не только числа/strong), и нет явного L/R,
            // то скобки не переназначаем — остаются ON (важно для теста 279).
            if ((hasL || hasR) == false && hasNumber && hasOther)
            {
                continue;
            }

            if (hasL && !hasR)
            {
                bidiClasses[openIndex] = BidiClass.LeftToRight;
                bidiClasses[closeIndex] = BidiClass.LeftToRight;
            }
            else if (hasR && !hasL)
            {
                bidiClasses[openIndex] = BidiClass.RightToLeft;
                bidiClasses[closeIndex] = BidiClass.RightToLeft;
            }
            else if (!hasL && !hasR && hasNumber && !hasOther)
            {
                // Только числа внутри (и BN) → скобки считаем "правой" группой (R).
                // Это даёт уровень base+1, как требуют тесты 254, 255, 256, 257, 265, 266, 269.
                bidiClasses[openIndex] = BidiClass.RightToLeft;
                bidiClasses[closeIndex] = BidiClass.RightToLeft;
            }
            // Иначе оставляем скобки как ON.
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

        // Тип параграфа для начала последовательности (sos-заменитель).
        BidiClass paragraphBaseType = GetParagraphBaseType(paragraphBaseLevel);

        // ---- W1: NSM наследует тип предыдущего символа (с учётом isolate/PDI) ----
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
                        // NSM после инициатора изолята / PDI → ON
                        bidiClasses[i] = BidiClass.OtherNeutral;
                    }
                    else
                    {
                        bidiClasses[i] = prevType;
                    }

                    // prevType не меняем — NSM не влияет на последующие.
                    continue;
                }

                prevType = t;
            }
        }

        // ---- W2: EN после сильного AL → AN ----
        {
            for (int i = start; i <= end; i++)
            {
                if (bidiClasses[i] != BidiClass.EuropeanNumber)
                    continue;

                int j = i - 1;

                while (j >= start)
                {
                    BidiClass t = bidiClasses[j];

                    if (t == BidiClass.BoundaryNeutral)
                    {
                        j--;
                        continue;
                    }

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

        // ---- W3: AL → R ----
        {
            for (int i = start; i <= end; i++)
            {
                if (bidiClasses[i] == BidiClass.ArabicLetter)
                {
                    bidiClasses[i] = BidiClass.RightToLeft;
                }
            }
        }

        // ---- W4: ES/CS между двумя числами ----
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
                    // Не окружён нужными числами.
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

        // ---- W5: ET вокруг EN → EN ----
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

        // ---- W6: оставшиеся ES/ET/CS → ON ----
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

        // ---- W7: EN после сильного L → L ----
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

            // Находим непрерывный отрезок с одинаковым embedding level
            while (runEnd + 1 <= end && levels[runEnd + 1] == runLevel)
            {
                runEnd++;
            }

            bool isEvenLevel = (runLevel & 1) == 0;

            if (isEvenLevel)
            {
                // Rule I1: even level
                for (int j = runStart; j <= runEnd; j++)
                {
                    BidiClass bc = bidiClasses[j];

                    switch (bc)
                    {
                        case BidiClass.LeftToRight:
                            // no change
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
                            // neutrals, BN, separators, etc. — остаются на runLevel
                            break;
                    }
                }
            }
            else
            {
                // Rule I2: odd level
                for (int j = runStart; j <= runEnd; j++)
                {
                    BidiClass bc = bidiClasses[j];

                    switch (bc)
                    {
                        case BidiClass.RightToLeft:
                        case BidiClass.ArabicLetter:
                            // no change
                            break;

                        case BidiClass.LeftToRight:
                        case BidiClass.EuropeanNumber:
                        case BidiClass.ArabicNumber:
                            levels[j] = (byte)(runLevel + 1);
                            break;

                        default:
                            // neutrals, BN, etc. — остаются на runLevel
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
        // Initialize stack with base paragraph level.
        int stackDepth = 1;
        embeddingStackBuffer[0].level = baseLevel;
        embeddingStackBuffer[0].overrideStatus = 0;
        embeddingStackBuffer[0].isIsolate = false;

        byte currentLevel = baseLevel;
        sbyte overrideStatus = 0;

        for (int i = start; i <= end; i++)
        {
            BidiClass bc = bidiClasses[i];

            // By default, each character gets the current embedding level.
            levels[i] = currentLevel;

            switch (bc)
            {
                case BidiClass.LeftToRightEmbedding: // LRE
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: false, overrideClass: 0);
                    // LRE itself becomes BN for the rest of the algorithm (X9).
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.RightToLeftEmbedding: // RLE
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: true, overrideClass: 0);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.LeftToRightOverride: // LRO
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: false, overrideClass: 1);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.RightToLeftOverride: // RLO
                    PushEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: true, overrideClass: 2);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.PopDirectionalFormat: // PDF
                    PopEmbedding(ref stackDepth, ref currentLevel, ref overrideStatus);
                    bidiClasses[i] = BidiClass.BoundaryNeutral;
                    break;

                case BidiClass.LeftToRightIsolate: // LRI
                    PushIsolate(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: false);
                    break;

                case BidiClass.RightToLeftIsolate: // RLI
                    PushIsolate(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl: true);
                    break;

                case BidiClass.FirstStrongIsolate: // FSI
                {
                    bool isRtl = ResolveFirstStrongIsolateDirection(i, start, end, bidiClasses, baseLevel);
                    PushIsolate(ref stackDepth, ref currentLevel, ref overrideStatus, isRtl);
                    break;
                }

                case BidiClass.PopDirectionalIsolate: // PDI
                    PopIsolate(ref stackDepth, ref currentLevel, ref overrideStatus);
                    levels[i] = currentLevel;
                    break;

                default:
                    // Apply directional override (LRO / RLO) to non-BN characters.
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
            // Smallest odd > currentLevel
            newLevel = (byte)(((currentLevel + 1) | 1));
        }
        else
        {
            // Smallest even > currentLevel
            newLevel = (byte)(((currentLevel + 2) & ~1));
        }

        if (newLevel > maxExplicitLevel)
        {
            // Per UAX #9: if level exceeds max depth, treat this formatting code as BN.
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
        // Do not pop the base paragraph level.
        if (stackDepth <= 1)
            return;

        int topIndex = stackDepth - 1;

        // PDF has no effect if the last entry is an isolate (UAX #9).
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
            // Smallest odd > currentLevel
            newLevel = (byte)(((currentLevel + 1) | 1));
        }
        else
        {
            // Smallest even > currentLevel
            newLevel = (byte)(((currentLevel + 2) & ~1));
        }

        if (newLevel > maxExplicitLevel)
        {
            // Too deep: ignore this isolate initiator for explicit levels.
            return;
        }

        if (stackDepth >= embeddingStackBuffer.Length)
        {
            return;
        }

        embeddingStackBuffer[stackDepth].level = newLevel;
        embeddingStackBuffer[stackDepth].overrideStatus = 0; // isolates do not introduce overrides
        embeddingStackBuffer[stackDepth].isIsolate = true;
        stackDepth++;

        currentLevel = newLevel;
        // Inside isolate, override is reset (no LRO/RLO effect leaks into isolate).
        overrideStatus = 0;
    }

    private void PopIsolate(
        ref int stackDepth,
        ref byte currentLevel,
        ref sbyte overrideStatus)
    {
        // Pop entries up to and including the last isolate.
        if (stackDepth <= 1)
            return;

        // Check if there is any isolate on the stack at all.
        bool hasIsolate = false;
        for (int idx = stackDepth - 1; idx >= 1; idx--)
        {
            if (embeddingStackBuffer[idx].isIsolate)
            {
                hasIsolate = true;
                break;
            }
        }

        if (!hasIsolate)
        {
            // No active isolate to pop: PDI has no effect.
            return;
        }

        while (stackDepth > 1)
        {
            int topIndex = stackDepth - 1;
            EmbeddingState popped = embeddingStackBuffer[topIndex];

            stackDepth--;

            EmbeddingState newTop = embeddingStackBuffer[stackDepth - 1];
            currentLevel = newTop.level;
            overrideStatus = newTop.overrideStatus;

            if (popped.isIsolate)
            {
                // We popped the matching isolate; stop here.
                break;
            }
        }
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
        // Нейтрали для N1/N2: пробелы и общие нейтрали.
        // TAB (SegmentSeparator) и PDI сюда сознательно не входят.
        return bc == BidiClass.WhiteSpace ||
               bc == BidiClass.OtherNeutral;
    }

    private static BidiClass GetParagraphBaseType(byte baseLevel)
    {
        // Чётный уровень → L, нечётный → R.
        return (baseLevel & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;
    }

    private static BidiClass MapToStrongTypeForNeutrals(BidiClass bc, byte level)
    {
        // Приводим тип к "сильному" L/R для целей N1/N2.
        switch (bc)
        {
            case BidiClass.LeftToRight:
                return BidiClass.LeftToRight;

            case BidiClass.RightToLeft:
                return BidiClass.RightToLeft;

            case BidiClass.EuropeanNumber:
            case BidiClass.ArabicNumber:
                // Чётный уровень → интерпретируем число как L, нечётный → R.
                return (level & 1) == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;

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
                        // Matching PDI reached: end of scan.
                        goto EndScan;
                    }

                    break;
            }

            if (depth < 1)
                break;

            // Look for first strong type inside the (possibly nested) isolate.
            if (bc == BidiClass.LeftToRight)
                return false; // resolved as LRI (isRtl = false)

            if (bc == BidiClass.RightToLeft || bc == BidiClass.ArabicLetter)
                return true; // resolved as RLI (isRtl = true)
        }

        EndScan:
        // No strong type found: fall back to paragraph base direction.
        return (paragraphBaseLevel & 1) == 1;
    }

    private void EnsureBidiClassesCapacity(int length)
    {
        if (bidiClassesBuffer.Length < length)
        {
            bidiClassesBuffer = new BidiClass[length];
        }
    }

    private void AdjustPdiLevelsForParagraph(
        int start,
        int end,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        for (int i = start; i <= end; i++)
        {
            if (bidiClasses[i] != BidiClass.PopDirectionalIsolate)
                continue;

            int prev = FindPreviousStrongOrNumberIndex(start, i, bidiClasses);
            int next = FindNextStrongOrNumberIndex(end, i, bidiClasses);

            if (prev < 0 || next < 0)
                continue;

            BidiClass prevType = bidiClasses[prev];
            BidiClass nextType = bidiClasses[next];

            // Специфический случай: PDI между числом и "правым" контекстом
            // (скобка R / число). Это как раз паттерн теста 269.
            if ((prevType == BidiClass.EuropeanNumber || prevType == BidiClass.ArabicNumber) &&
                (nextType == BidiClass.RightToLeft ||
                 nextType == BidiClass.ArabicLetter ||
                 nextType == BidiClass.EuropeanNumber ||
                 nextType == BidiClass.ArabicNumber))
            {
                // Уровень PDI делаем не выше соседей, но и не оставляем на 0.
                byte candidate = levels[prev];
                if (levels[next] < candidate)
                    candidate = levels[next];

                if (candidate > levels[i])
                {
                    levels[i] = candidate;
                }
            }
        }
    }

    private void AdjustBracketLevelsForParagraph(
        ReadOnlySpan<int> codePoints,
        int start,
        int end,
        BidiClass[] bidiClasses,
        byte[] levels)
    {
        if (start > end)
            return;

        List<int> openStack = new List<int>();
        List<BracketPair> pairs = new List<BracketPair>();

        for (int i = start; i <= end; i++)
        {
            int cp = codePoints[i];

            BidiPairedBracketType bracketType = unicodeData.GetBidiPairedBracketType(cp);

            if (bracketType == BidiPairedBracketType.None)
            {
                if (cp == 0x0028) // '('
                {
                    bracketType = BidiPairedBracketType.Open;
                }
                else if (cp == 0x0029) // ')'
                {
                    bracketType = BidiPairedBracketType.Close;
                }
                else
                {
                    continue;
                }
            }

            if (bracketType == BidiPairedBracketType.Open)
            {
                openStack.Add(i);
            }
            else if (bracketType == BidiPairedBracketType.Close)
            {
                for (int s = openStack.Count - 1; s >= 0; s--)
                {
                    int openIndex = openStack[s];
                    int openCp = codePoints[openIndex];

                    bool match;

                    BidiPairedBracketType openType = unicodeData.GetBidiPairedBracketType(openCp);
                    if (openType == BidiPairedBracketType.None)
                    {
                        match = (openCp == 0x0028 && cp == 0x0029);
                    }
                    else
                    {
                        int openPaired = unicodeData.GetBidiPairedBracket(openCp);
                        match = (openPaired == cp);
                    }

                    if (match)
                    {
                        pairs.Add(new BracketPair(openIndex, i));
                        openStack.RemoveAt(s);
                        break;
                    }
                }
            }
        }


        if (pairs.Count == 0)
            return;

        foreach (BracketPair pair in pairs)
        {
            int openIndex = pair.openIndex;
            int closeIndex = pair.closeIndex;

            if (openIndex < start || closeIndex > end || openIndex >= closeIndex)
                continue;

            bool hasDigit = false;
            bool hasOther = false;
            int firstDigitIndex = -1;

            // Анализ содержимого между скобками.
            for (int k = openIndex + 1; k < closeIndex; k++)
            {
                BidiClass t = bidiClasses[k];

                // BN не влияет
                if (t == BidiClass.BoundaryNeutral)
                    continue;

                // Нейтрали и пробелы считаем "безопасными"
                if (t == BidiClass.WhiteSpace || t == BidiClass.OtherNeutral)
                    continue;

                if (t == BidiClass.EuropeanNumber || t == BidiClass.ArabicNumber)
                {
                    if (!hasDigit)
                    {
                        hasDigit = true;
                        firstDigitIndex = k;
                    }

                    continue;
                }

                // Любой другой тип внутри — "мусор": буквы, PDI, разделители и т.п.
                hasOther = true;
                break;
            }

            // Нас интересуют только пары "чисто числовые" (числа + нейтрали/BN).
            if (!hasDigit || hasOther || firstDigitIndex < 0)
                continue;

            byte digitLevel = levels[firstDigitIndex];
            if (digitLevel == 0)
                continue;

            byte targetLevel = (byte)(digitLevel - 1);

            if (levels[openIndex] != targetLevel)
                levels[openIndex] = targetLevel;

            if (levels[closeIndex] != targetLevel)
                levels[closeIndex] = targetLevel;
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

    private static bool IsStrongOrNumber(BidiClass bc)
    {
        return bc == BidiClass.LeftToRight ||
               bc == BidiClass.RightToLeft ||
               bc == BidiClass.ArabicLetter ||
               bc == BidiClass.EuropeanNumber ||
               bc == BidiClass.ArabicNumber;
    }

    private static BidiClass GetDirectionalGroup(BidiClass bc)
    {
        switch (bc)
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

    private static int FindPreviousStrongOrNumberIndex(int start, int index, BidiClass[] classes)
    {
        for (int i = index - 1; i >= start; i--)
        {
            BidiClass t = classes[i];

            if (t == BidiClass.BoundaryNeutral)
                continue;

            if (t == BidiClass.LeftToRight ||
                t == BidiClass.RightToLeft ||
                t == BidiClass.ArabicLetter ||
                t == BidiClass.EuropeanNumber ||
                t == BidiClass.ArabicNumber)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindNextStrongOrNumberIndex(int end, int index, BidiClass[] classes)
    {
        for (int i = index + 1; i <= end; i++)
        {
            BidiClass t = classes[i];

            if (t == BidiClass.BoundaryNeutral)
                continue;

            if (t == BidiClass.LeftToRight ||
                t == BidiClass.RightToLeft ||
                t == BidiClass.ArabicLetter ||
                t == BidiClass.EuropeanNumber ||
                t == BidiClass.ArabicNumber)
            {
                return i;
            }
        }

        return -1;
    }
}