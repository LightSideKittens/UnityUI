using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class ArabicShaper
{
    private enum JoiningType : byte
    {
        U,
        R,
        L,
        D,
        C,
        T
    }

    private enum ArabicForm : byte
    {
        None = 0,
        Isolated,
        Final,
        Initial,
        Medial
    }

    private struct FormEntry
    {
        public readonly int isolated;
        public readonly int final;
        public readonly int initial;
        public readonly int medial;

        public FormEntry(int isolated, int final, int initial, int medial)
        {
            this.isolated = isolated;
            this.final    = final;
            this.initial  = initial;
            this.medial   = medial;
        }

        public int GetCodepoint(ArabicForm form)
        {
            switch (form)
            {
                case ArabicForm.Isolated:
                    return isolated;
                case ArabicForm.Final:
                    return final != 0 ? final : isolated;
                case ArabicForm.Initial:
                    return initial != 0 ? initial : isolated;
                case ArabicForm.Medial:
                    return medial != 0 ? medial : isolated;
                default:
                    return isolated;
            }
        }
    }

    private static readonly object initLock = new();
    private static bool initialized;

    private static Dictionary<int, JoiningType> joiningTypes;

    private static Dictionary<int, FormEntry> formEntries;

    static ArabicShaper()
    {
        lock (initLock)
        {
            if (initialized)
                return;

            joiningTypes = ParseDerivedJoiningTypes(Resources.Load<TextAsset>("DerivedJoiningType.txt").text);
            formEntries  = ParseArabicContextForms(Resources.Load<TextAsset>("ArabicContextForms.txt").text);

            initialized = true;
        }
    }

    public static string Do(string input, out int[] indexMap)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var codepoints = BiDi.StringToCodepoints(input);
        var shaped     = Do(codepoints, out indexMap);
        return BiDi.CodepointsToString(shaped);
    }

    public static int[] Do(int[] codepoints, out int[] indexMap)
    {
        if (codepoints == null)
            throw new ArgumentNullException(nameof(codepoints));
        
        int length = codepoints.Length;
        var output = new int[length];
        indexMap   = new int[length];

        if (length == 0)
            return output;

        for (int i = 0; i < length; i++)
        {
            output[i]   = codepoints[i];
            indexMap[i] = i;
        }

        bool hasArabic = false;
        for (int i = 0; i < length; i++)
        {
            if (IsArabicScript(codepoints[i]))
            {
                hasArabic = true;
                break;
            }
        }

        if (!hasArabic)
            return output;

        var joiningTypes = new JoiningType[length];
        var joinPrev     = new bool[length];
        var joinNext     = new bool[length];

        for (int i = 0; i < length; i++)
        {
            joiningTypes[i] = GetJoiningType(codepoints[i]);
        }

        int prevIndex = -1;
        for (int i = 0; i < length; i++)
        {
            JoiningType jt = joiningTypes[i];
            if (jt == JoiningType.T)
                continue;

            if (prevIndex >= 0)
            {
                JoiningType prevJt = joiningTypes[prevIndex];
                if (IsJoinPair(prevJt, jt))
                {
                    joinNext[prevIndex] = true;
                    joinPrev[i]         = true;
                }
            }

            prevIndex = i;
        }

        for (int i = 0; i < length; i++)
        {
            int cp = codepoints[i];

            if (!IsArabicScript(cp))
                continue;

            if (!formEntries.TryGetValue(cp, out var forms))
                continue;

            ArabicForm form;
            if (!joinPrev[i] && !joinNext[i])
                form = ArabicForm.Isolated;
            else if (!joinPrev[i] && joinNext[i])
                form = ArabicForm.Initial;
            else if (joinPrev[i] && !joinNext[i])
                form = ArabicForm.Final;
            else
                form = ArabicForm.Medial;

            int shapedCp = forms.GetCodepoint(form);
            output[i] = shapedCp;
        }

        return output;
    }

    private static bool IsArabicScript(int cp)
    {
        if ((cp >= 0x0600 && cp <= 0x06FF) ||
            (cp >= 0x0750 && cp <= 0x077F) ||
            (cp >= 0x08A0 && cp <= 0x08FF) ||
            (cp >= 0xFB50 && cp <= 0xFDFF) ||
            (cp >= 0xFE70 && cp <= 0xFEFF))
        {
            return true;
        }

        return false;
    }

    private static JoiningType GetJoiningType(int cp)
    {
        if (joiningTypes != null && joiningTypes.TryGetValue(cp, out var jt))
            return jt;

        return JoiningType.U;
    }

    /// <summary>
    /// Определяем, образуют ли два соседних символа (prev, curr)
    /// соединённую пару в логическом порядке.
    /// </summary>
    private static bool IsJoinPair(JoiningType prev, JoiningType curr)
    {
        if (prev == JoiningType.U || curr == JoiningType.U)
            return false;

        if (prev == JoiningType.T || curr == JoiningType.T)
            return false;

        bool prevJoinsToNext =
            prev == JoiningType.D ||
            prev == JoiningType.L ||
            prev == JoiningType.C;

        bool currJoinsToPrev =
            curr == JoiningType.D ||
            curr == JoiningType.R ||
            curr == JoiningType.C;

        return prevJoinsToNext && currJoinsToPrev;
    }

    private static Dictionary<int, JoiningType> ParseDerivedJoiningTypes(string text)
    {
        var dict = new Dictionary<int, JoiningType>();

        using (var reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                int hashIndex = line.IndexOf('#');
                if (hashIndex >= 0)
                    line = line.Substring(0, hashIndex);

                if (line.Length == 0)
                    continue;

                var parts = line.Split(';');
                if (parts.Length < 2)
                    continue;

                string rangePart = parts[0].Trim();
                string typePart  = parts[1].Trim();

                if (string.IsNullOrEmpty(rangePart) || string.IsNullOrEmpty(typePart))
                    continue;

                JoiningType jt;
                switch (typePart)
                {
                    case "R": jt = JoiningType.R; break;
                    case "L": jt = JoiningType.L; break;
                    case "D": jt = JoiningType.D; break;
                    case "C": jt = JoiningType.C; break;
                    case "T": jt = JoiningType.T; break;
                    case "U": jt = JoiningType.U; break;
                    default:
                        continue;
                }

                int start, end;
                int dotDot = rangePart.IndexOf("..", StringComparison.Ordinal);
                if (dotDot >= 0)
                {
                    string startHex = rangePart.Substring(0, dotDot);
                    string endHex   = rangePart.Substring(dotDot + 2);

                    if (!int.TryParse(startHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out start))
                        continue;
                    if (!int.TryParse(endHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out end))
                        continue;
                }
                else
                {
                    if (!int.TryParse(rangePart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out start))
                        continue;
                    end = start;
                }

                for (int cp = start; cp <= end; cp++)
                {
                    dict[cp] = jt;
                }
            }
        }

        return dict;
    }

    private static Dictionary<int, FormEntry> ParseArabicContextForms(string text)
    {
        var dict = new Dictionary<int, FormEntry>();

        using (var reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                var parts = line.Split(';');
                if (parts.Length < 5)
                    continue;

                if (!TryParseHex(parts[0], out int baseCp))
                    continue;

                TryParseHex(parts[1], out int isoCp);
                TryParseHex(parts[2], out int finCp);
                TryParseHex(parts[3], out int initCp);
                TryParseHex(parts[4], out int medCp);

                var entry = new FormEntry(isoCp, finCp, initCp, medCp);
                dict[baseCp] = entry;
            }
        }

        return dict;
    }

    private static bool TryParseHex(string s, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(s))
            return false;

        s = s.Trim();
        if (s == "0" || s == "0000")
        {
            value = 0;
            return true;
        }

        return int.TryParse(s,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out value);
    }
}
