using System;
using System.Collections.Generic;
using System.Text;

public static class Bidi
{
    public enum Direction
    {
        Auto = 0,
        LeftToRight = 1,
        RightToLeft = 2
    }
    public static int[] StringToCodepoints(string s)
    {
        if (string.IsNullOrEmpty(s))
            return Array.Empty<int>();

        var list = new List<int>(s.Length);
        for (int i = 0; i < s.Length;)
        {
            int cp = char.ConvertToUtf32(s, i);
            list.Add(cp);
            i += char.IsSurrogatePair(s, i) ? 2 : 1;
        }

        return list.ToArray();
    }

    public static string CodepointsToString(int[] cps)
    {
        if (cps == null || cps.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(cps.Length);
        for (int i = 0; i < cps.Length; i++)
        {
            sb.Append(char.ConvertFromUtf32(cps[i]));
        }

        return sb.ToString();
    }
}
