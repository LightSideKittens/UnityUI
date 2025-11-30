using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;

public static class BiDi
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
    
    [DllImport(DLL_NAME,
        EntryPoint = "fribidi_unity_has_rtl",
        CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fribidi_unity_has_rtl(
        [In] int[] logicalUtf32,
        int length);
    
    private static string WrapAndReorder(
        string logicalText,
        TMP_Text text,
        out int[] logicalToVisualMap,
        Direction targetDirection)
    {
        var info = text.textInfo;
        var lines = info.lineInfo;

        var sb = new StringBuilder();
        var logicalToVisualMapList = new List<int>();
        
        for (int i = 0; i < info.lineCount; i++)
        {
            var line = lines[i];
            int start = line.firstCharacterIndex;
            var length = line.characterCount;
            int end = start + length;
            var part = logicalText[start..end];
            
            var logicalSlice = StringToCodepoints(part);
            
            string visualLineText = DoBiDi(
                logicalSlice,
                out int[] localMap, targetDirection);

            if (localMap == null || localMap.Length != length)
            {
                localMap = BuildIdentityMap(length);
            }

            if (i > 0)
            {
                sb.Append('\n');
            }
            
            for (int j = visualLineText.Length - 1; j >= 0; j--)
            {
                logicalToVisualMapList.Add(localMap[j]);
                sb.Append(visualLineText[j]);
            }
        }

        logicalToVisualMap = logicalToVisualMapList.ToArray();
        return sb.ToString();
    }
    
    public static string Do(TMP_Text text,
        out int[] logicalToVisualMap,
        out Direction resultDirection,
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
        Debug.Log(string.Join(", ", cps));
        resultDirection = DetectDirection(cps);
        
        if (ContainsRtl(cps))
        {
            if (text.textWrappingMode == TextWrappingModes.NoWrap)
            { 
                logicalText = DoBiDi(cps, out logicalToVisualMap, targetDirection, true);
            }
            else
            {
                if (targetDirection == Direction.Auto)
                {
                    targetDirection = resultDirection;
                }
            
                logicalText = WrapAndReorder(logicalText, text, out logicalToVisualMap, targetDirection);
            }
            
            return logicalText;
        }
        
        logicalToVisualMap = null;
        return logicalText;
    }
    
    private static bool ContainsRtl(int[] codepoints)
    {
        if (codepoints == null || codepoints.Length == 0)
            return false;

        int result = fribidi_unity_has_rtl(codepoints, codepoints.Length);
        return result != 0;
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

