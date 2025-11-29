using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;

public static class BiDi
{
    public enum Direction
    {
        Auto = 0,
        LeftToRight = 1,
        RightToLeft = 2
    }
    
    /// Одна визуальная строка после переноса и BiDi.
    public sealed class BidiWrappedLine
    {
        /// Готовый визуальный текст этой строки (то, что можно отдавать в TMP).
        public string visualText;

        /// Карта logical→visual внутри ЭТОЙ логической строки.
        /// Индексы – по Unicode codepoint'ам.
        public int[] logicalToVisualMap;

        /// Начальный индекс (по codepoint'ам) этой строки в исходном абзаце.
        public int logicalStartCodepointIndex;

        public int LogicalLength => logicalToVisualMap?.Length ?? 0;
    }
    
#if UNITY_IOS && !UNITY_EDITOR
    private const string DLL_NAME = "__Internal";
#else
    private const string DLL_NAME = "AddPlugin";
#endif

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int fribidi_unity_reorder_utf32(
        [In] int[] logical,
        int length,
        int baseDirCode,
        [Out] int[] visual,
        [Out] int[] logicalToVisual);
    
    /// Главный метод: логический текст → набор визуальных строк после переноса и BiDi.
    public static IReadOnlyList<BidiWrappedLine> WrapAndReorder(
        string logicalText,
        TMP_Text text,
        Direction direction = Direction.Auto)
    {
        if (logicalText == null)
            throw new ArgumentNullException(nameof(logicalText));

        // 1. Нормализуем переводы строк и конвертируем string → массив Unicode codepoint'ов.
        var buffer = StringToCodepoints(logicalText);
        var cps = buffer.cps;
        
        if (cps.Length == 0)
            return Array.Empty<BidiWrappedLine>();

        // 2. Ширина каждого codepoint’а – сюда ты подставишь TMP-логику.
        float[] widths = MeasureCodepointWidths(text, buffer);

        if (widths.Length != cps.Length)
            throw new InvalidOperationException("MeasureCodepointWidths must return width for each codepoint.");

        // 3. Разбиваем логический текст на строки по ширине (без BiDi).
        var maxWidth = text.rectTransform.rect.width;
        var lineRanges = SplitLogicalLinesByWidth(cps, widths, maxWidth);

        // 4. Для каждой логической строки делаем BiDi и собираем результат.
        var result = new List<BidiWrappedLine>(lineRanges.Count);

        foreach (var (start, length) in lineRanges)
        {
            // Логическая подстрока в виде codepoint’ов.
            var logicalSlice = new int[length];
            Array.Copy(cps, start, logicalSlice, 0, length);
            
            string visualLineText = DoBiDi(
                logicalSlice,
                out int[] localMap, direction);

            if (localMap == null || localMap.Length != length)
            {
                // На всякий случай не даём карте "поехать".
                localMap = BuildIdentityMap(length);
            }

            result.Add(new BidiWrappedLine
            {
                visualText = visualLineText,
                logicalToVisualMap = localMap,
                logicalStartCodepointIndex = start
            });
        }

        return result;
    }

    /// Удобный помощник: склеить строки в один текст с '\n' для TMP
    /// (при этом в TMP нужно отключить word wrapping).
    public static string BuildVisualTextWithNewlines(IReadOnlyList<BidiWrappedLine> lines)
    {
        if (lines == null || lines.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }

            var str = lines[i].visualText;
            for (int j = str.Length - 1; j >= 0; j--)
            { 
                sb.Append(str[j]);
            }
        }

        return sb.ToString();
    }
    
    private static string DoBiDi(int[] logical,
        out int[] logicalToVisualMap,
        Direction direction = Direction.Auto)
    {
        
        int len = logical.Length;

        int[] visual = new int[len];
        logicalToVisualMap = new int[len];

        int ok = fribidi_unity_reorder_utf32(
            logical,
            len,
            (int)direction,
            visual,
            logicalToVisualMap);

        if (ok == 0)
        {
            for (int i = 0; i < len; i++)
            {
                logicalToVisualMap[i] = i;
            }
            
            return CodepointsToString(logical);
        }

        return CodepointsToString(visual);
    }

    // --------------------------------------------------------------------
    //  ЛОГИЧЕСКОЕ РАЗБИТИЕ НА СТРОКИ ПО ШИРИНЕ (без BiDi)
    // --------------------------------------------------------------------

    private static List<(int start, int length)> SplitLogicalLinesByWidth(
        int[] cps,
        float[] widths,
        float maxWidth)
    {
        int n = cps.Length;
        var lines = new List<(int start, int length)>();

        int lineStart = 0;

        while (lineStart < n)
        {
            float currentWidth = 0f;
            int lastBreakIndex = -1;

            int i;
            for (i = lineStart; i < n; i++)
            {
                int cp = cps[i];

                // Жёсткий перенос строки: U+000A (LF)
                if (cp == 0x000A)
                {
                    if (i > lineStart)
                    {
                        int len = i - lineStart;
                        lines.Add((lineStart, len));
                    }

                    // Пропускаем сам \n
                    lineStart = i + 1;
                    goto NextLine; // переходим к следующей строке
                }

                currentWidth += widths[i];

                if (CanBreakAfterCodepoint(cp))
                {
                    lastBreakIndex = i;
                }

                if (currentWidth > maxWidth)
                {
                    if (lastBreakIndex >= lineStart)
                    {
                        // Переносим по последнему допустимому разрыву.
                        int lineEnd = lastBreakIndex;
                        int len = lineEnd - lineStart + 1;
                        lines.Add((lineStart, len));
                        lineStart = lineEnd + 1;
                    }
                    else
                    {
                        // "Длинное слово": форсированный перенос.
                        int lineEnd = i;
                        int len = lineEnd - lineStart + 1;
                        lines.Add((lineStart, len));
                        lineStart = lineEnd + 1;
                    }

                    goto NextLine;
                }
            }

            // Дошли до конца абзаца: остаток – последняя строка.
            if (lineStart < n)
            {
                int len = n - lineStart;
                lines.Add((lineStart, len));
            }

            break;

        NextLine:
            ;
        }

        return lines;
    }

    /// Возможен ли разрыв строки после данного codepoint (в логическом порядке).
    /// Здесь реализована "нормальная" word-wrap логика для языков с пробелами:
    ///   - пробелы и табы;
    ///   - различные типографские пробелы (кроме NBSP);
    ///   - мягкий перенос (SOFT HYPHEN);
    ///   - некоторые дефисы/тире.
    private static bool CanBreakAfterCodepoint(int cp)
    {
        // Явные пробелы / whitespace
        if (cp == 0x0020 || cp == 0x0009) // space, tab
            return true;

        // Разные типографские пробелы (но НЕ NBSP U+00A0 и узкий NBSP U+202F)
        if (cp >= 0x2000 && cp <= 0x2006) // EN QUAD..SIX-PER-EM SPACE
            return true;
        if (cp == 0x2008 || cp == 0x2009 || cp == 0x200A) // PUNCTUATION / THIN / HAIR SPACE
            return true;
        if (cp == 0x3000) // IDEOGRAPHIC SPACE
            return true;

        // Zero width space – явная точка переноса.
        if (cp == 0x200B)
            return true;

        // Мягкий перенос: символ отображается как точка разрыва.
        if (cp == 0x00AD) // SOFT HYPHEN
            return true;

        // Некоторые дефисы/тире, после которых перенос обычно допустим.
        switch (cp)
        {
            case 0x002D: // HYPHEN-MINUS
            case 0x2010: // HYPHEN
            case 0x2012: // FIGURE DASH
            case 0x2013: // EN DASH
            case 0x2014: // EM DASH
            case 0x058A: // ARMENIAN HYPHEN
                return true;
        }

        return false;
    }

    private static int[] BuildIdentityMap(int length)
    {
        var map = new int[length];
        for (int i = 0; i < length; i++)
            map[i] = i;
        return map;
    }

    // --------------------------------------------------------------------
    //  КОНВЕРТАЦИЯ string ↔ массив Unicode codepoint'ов (UTF-32)
    // --------------------------------------------------------------------
    
    /*private static string CodepointsToString(int[] codepoints)
{
    var sb = new StringBuilder(codepoints.Length);
    for (var i = codepoints.Length - 1; i >= 0; i--)
    {
        sb.Append(char.ConvertFromUtf32(codepoints[i]));

    }
    return sb.ToString();
}*/
    
    private static string CodepointsToString(int[] cps)
    {
        var sb = new StringBuilder(cps.Length);
        for (int i = 0; i < cps.Length; i++)
        {
            int cp = cps[i];
            sb.Append(char.ConvertFromUtf32(cp));
        }
        return sb.ToString();
    }

    public struct CodepointBuffer
    {
        public int[] cps;
        public int[] cpIndexByStringIndex;
    }

    public static CodepointBuffer StringToCodepoints(string logicalText)
    {
        var cps = new List<int>(logicalText.Length);
        var cpIndexByStringIndex = new int[logicalText.Length];
        for (int i = 0; i < cpIndexByStringIndex.Length; i++)
            cpIndexByStringIndex[i] = -1;

        int cpIndex = 0;

        for (int stringIndex = 0; stringIndex < logicalText.Length;)
        {
            int cp = char.ConvertToUtf32(logicalText, stringIndex);

            cps.Add(cp);
            cpIndexByStringIndex[stringIndex] = cpIndex;

            int delta = char.IsSurrogatePair(logicalText, stringIndex) ? 2 : 1;
            stringIndex += delta;
            cpIndex++;
        }

        return new CodepointBuffer
        {
            cps           = cps.ToArray(),
            cpIndexByStringIndex = cpIndexByStringIndex
        };
    }

    private static float[] MeasureCodepointWidths(TMP_Text text, CodepointBuffer buffer)
    {
        var cps               = buffer.cps;
        var cpIndexByStrIndex = buffer.cpIndexByStringIndex;
        var widths            = new float[cps.Length];

        var info = text.textInfo;
        int glyphCount = info.characterCount;

        for (int glyphIndex = 0; glyphIndex < glyphCount; glyphIndex++)
        {
            TMP_CharacterInfo ch = info.characterInfo[glyphIndex];

            int stringIndex = ch.index;
            if (stringIndex < 0 || stringIndex >= cpIndexByStrIndex.Length)
                continue;

            int cpIndex = cpIndexByStrIndex[stringIndex];
            if (cpIndex < 0 || cpIndex >= widths.Length)
            {
                // Этот глиф не соответствует началу codepoint'а (например, часть тега) – пропускаем.
                continue;
            }

            // Advance-ширина символа: сколько "пера" ушло вперёд после этого глифа.
            float width = ch.xAdvance - ch.origin;

            // На случай, если один codepoint даёт несколько глифов (лигатуры и т.п.),
            // аккумулируем сумму.
            widths[cpIndex] += width;
        }

        // Для перевода строки (U+000A) ширина нам не нужна — она обрабатывается отдельно.
        for (int i = 0; i < cps.Length; i++)
        {
            if (cps[i] == 0x000A)
                widths[i] = 0f;
        }

        return widths;
    }

}

