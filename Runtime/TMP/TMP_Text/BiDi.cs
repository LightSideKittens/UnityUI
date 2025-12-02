using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;

public static class Bidi
{
    public enum Direction
    {
        Auto = 0,
        LeftToRight = 1,
        RightToLeft = 2
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
    
    static string WrapAndReorder(
        string logicalText,
        TMP_Text text,
        out int[][] logicalToVisualMap,
        out Direction[] resultDirections,
        Direction targetDirection)
    {
        var info = text.textInfo;
        var lines = info.lineInfo;

        Debug.Log(logicalText.Count(x => x == '\n'));
        var sb = new StringBuilder(logicalText.Length);
        logicalToVisualMap = new int[info.lineCount][];
        resultDirections = new Direction[info.lineCount];

        bool isAuto = targetDirection == Direction.Auto;
        Debug.Log(info.lineCount);
        for (int i = 0; i < info.lineCount; i++)
        {
            var line = lines[i];

            int start = line.firstCharacterIndex;
            int length = line.characterCount;
            int end = start + length;

            string part = logicalText.Substring(start, length);

            bool hasTrailingNewline =
                part.Length > 0 &&
                part[^1] == '\n';

            string core = hasTrailingNewline ? part[..^1] : part;

            int[] logicalSlice = StringToCodepoints(core);

            var lineDirection = DetectDirection(logicalSlice);
            resultDirections[i] = lineDirection;

            string visualLineText = DoBiDi(
                logicalSlice,
                out int[] localMap,
                targetDirection);

            if (localMap == null || localMap.Length != logicalSlice.Length)
            {
                localMap = BuildIdentityMap(logicalSlice.Length);
            }

            logicalToVisualMap[i] = localMap;

            sb.Append(visualLineText);

            if (hasTrailingNewline)
            {
                sb.Append('\n');
            }
        }

        var result = sb.ToString();
        Debug.Log(result.Count(x => x == '\n'));
        return result;
    }

    
    public static string Do(TMP_Text text,
        out int[][] logicalToVisualMap,
        out Direction[] resultDirections,
        Direction targetDirection = Direction.Auto)
    {
        var info = text.textInfo;
        var characters = info.characterInfo;
        var sb = new StringBuilder(info.characterCount);
        
        for (int i = 0; i < info.characterCount; i++)
        {
            var ch = characters[i];
            sb.Append(ch.character);
        }
        
        var logicalText = sb.ToString();
        var cps = StringToCodepoints(logicalText);
        
        if (ContainsRtl(cps))
        {
            if (text.textWrappingMode == TextWrappingModes.NoWrap)
            {
                resultDirections = new[] { DetectDirection(cps) };
                logicalText = DoBiDi(cps, out var logicalToVisualMapOneLine, targetDirection, true);
                logicalToVisualMap = new[] { logicalToVisualMapOneLine };
            }
            else
            {
                resultDirections = new[] { DetectDirection(cps) };
                logicalText = WrapAndReorder(logicalText, text, out logicalToVisualMap, out resultDirections, targetDirection);
            }
            
            return logicalText;
        }
        
        logicalToVisualMap = null;
        resultDirections = null;
        return logicalText;
    }
    
    static bool ContainsRtl(int[] cps)
    {
        if (cps == null || cps.Length == 0)
            return false;

        for (int i = 0; i < cps.Length; i++)
        {
            int cp = cps[i];

            if ((cp >= 0x0590 && cp <= 0x05FF) || // Hebrew
                (cp >= 0x0600 && cp <= 0x06FF) || // Arabic
                (cp >= 0x0700 && cp <= 0x074F) || // Syriac
                (cp >= 0x0750 && cp <= 0x077F) || // Arabic Supplement
                (cp >= 0x0780 && cp <= 0x07BF) || // Thaana
                (cp >= 0x07C0 && cp <= 0x07FF) || // N'Ko
                (cp >= 0x0800 && cp <= 0x083F) || // Samaritan
                (cp >= 0x0840 && cp <= 0x085F) || // Mandaic
                (cp >= 0x08A0 && cp <= 0x08FF))   // Arabic Extended
            {
                return true;
            }
        }

        return false;
    }
    
    private static Direction DetectDirection(int[] cps)
    {
        int rtlFlag = fribidi_unity_detect_base_direction(cps, cps.Length);
        
        return rtlFlag == 1
            ? Direction.RightToLeft
            : Direction.LeftToRight;
    }
    
    private static string DoBiDi(int[] logical,
        out int[] logicalToVisualMap,
        Direction direction = Direction.Auto,
        bool reverse = false)
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

    private static int[] BuildIdentityMap(int length)
    {
        var map = new int[length];
        for (int i = 0; i < length; i++)
            map[i] = i;
        return map;
    }

    public static string CodepointsToString(int[] cps, bool reverse = false)
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

    public static int[] StringToCodepoints(string s)
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
}

