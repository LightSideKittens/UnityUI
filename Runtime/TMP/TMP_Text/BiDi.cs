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
    
    
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int fribidi_unity_detect_base_direction(
        [In] int[] logicalUtf32,
        int length);
    
    public static IReadOnlyList<BidiWrappedLine> WrapAndReorder(
        string logicalText,
        TMP_Text text,
        Direction direction = Direction.Auto)
    {
        if (logicalText == null) throw new ArgumentNullException(nameof(logicalText));

        var buffer = GetCodepointBuffer(logicalText);
        var cps = buffer.cps;
        
        if (cps.Length == 0) return Array.Empty<BidiWrappedLine>();

        float[] widths = MeasureCodepointWidths(text, buffer);

        if (widths.Length != cps.Length) throw new InvalidOperationException("MeasureCodepointWidths must return width for each codepoint.");

        var maxWidth = text.rectTransform.rect.width;
        var lineRanges = SplitLogicalLinesByWidth(cps, widths, maxWidth);

        var result = new List<BidiWrappedLine>(lineRanges.Count);

        if (direction == Direction.Auto)
        {
            direction = DetectDirection(logicalText, buffer);
        }
        
        foreach (var (start, length) in lineRanges)
        {
            var logicalSlice = new int[length];
            Array.Copy(cps, start, logicalSlice, 0, length);
            
            string visualLineText = DoBiDi(
                logicalSlice,
                out int[] localMap, direction);

            if (localMap == null || localMap.Length != length)
            {
                localMap = BuildIdentityMap(length);
            }

            result.Add(new()
            {
                visualText = visualLineText,
                logicalToVisualMap = localMap,
                logicalStartCodepointIndex = start
            });
        }

        return result;
    }
    
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

    public static string DoBiDi(string logical,
        out int[] logicalToVisualMap,
        Direction direction = Direction.Auto)
    {
        var cps = StringToCodepoints(logical);
        return DoBiDi(cps, out logicalToVisualMap, direction, true);
    }

    private static Direction DetectDirection(string text, CodepointBuffer buffer)
    {
        if (string.IsNullOrEmpty(text)) return Direction.LeftToRight;

        var cps = buffer.cps;
        int rtlFlag = fribidi_unity_detect_base_direction(cps, cps.Length);
        
        return rtlFlag == 1
            ? Direction.RightToLeft
            : Direction.LeftToRight;
    }
    
    private static string DoBiDi(int[] logical,
        out int[] logicalToVisualMap,
        Direction direction = Direction.Auto, bool reverse = false)
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
            
            return CodepointsToString(logical, reverse);
        }

        return CodepointsToString(visual, reverse);
    }

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

                if (cp == 0x000A)
                {
                    if (i > lineStart)
                    {
                        int len = i - lineStart;
                        lines.Add((lineStart, len));
                    }

                    lineStart = i + 1;
                    goto NextLine;
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
                        int lineEnd = lastBreakIndex;
                        int len = lineEnd - lineStart + 1;
                        lines.Add((lineStart, len));
                        lineStart = lineEnd + 1;
                    }
                    else
                    {
                        int lineEnd = i;
                        int len = lineEnd - lineStart + 1;
                        lines.Add((lineStart, len));
                        lineStart = lineEnd + 1;
                    }

                    goto NextLine;
                }
            }

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
    
    private static bool CanBreakAfterCodepoint(int cp)
    {
        if (cp == 0x0020 || cp == 0x0009) return true;

        if (cp >= 0x2000 && cp <= 0x2006) return true;
        if (cp == 0x2008 || cp == 0x2009 || cp == 0x200A) return true;
        if (cp == 0x3000) return true;

        if (cp == 0x200B)
            return true;

        if (cp == 0x00AD) return true;

        switch (cp)
        {
            case 0x002D:
            case 0x2010:
            case 0x2012:
            case 0x2013:
            case 0x2014:
            case 0x058A:
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

    private static string CodepointsToString(int[] cps, bool reverse = false)
    {
        var sb = new StringBuilder(cps.Length);
        if (reverse)
        {
            for (int i = cps.Length - 1; i >= 0; i--)
            {
                int cp = cps[i];
                sb.Append(char.ConvertFromUtf32(cp));
            }
        }
        else
        {
            for (int i = 0; i < cps.Length; i++)
            {
                int cp = cps[i];
                sb.Append(char.ConvertFromUtf32(cp));
            }
        }
        
        return sb.ToString();
    }

    private struct CodepointBuffer
    {
        public int[] cps;
        public int[] cpIndexByStringIndex;
    }

    private static CodepointBuffer GetCodepointBuffer(string logicalText)
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

        return new()
        {
            cps           = cps.ToArray(),
            cpIndexByStringIndex = cpIndexByStringIndex
        };
    }
    
    private static int[] StringToCodepoints(string s)
    {
        var list = new List<int>(s.Length);
        for (int i = 0; i < s.Length;)
        {
            int cp = char.ConvertToUtf32(s, i);
            list.Add(cp);
            i += char.IsSurrogatePair(s, i) ? 2 : 1;
        }
        return list.ToArray();
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
            {
                continue;
            }

            int cpIndex = cpIndexByStrIndex[stringIndex];
            if (cpIndex < 0 || cpIndex >= widths.Length)
            {
                continue;
            }
            
            float width = ch.glyphAdvance;
            
            widths[cpIndex] += width;
        }
        
        for (int i = 0; i < cps.Length; i++)
        {
            if (cps[i] == 0x000A)
                widths[i] = 0f;
        }

        return widths;
    }

}

