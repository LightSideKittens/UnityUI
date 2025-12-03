using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TMPro
{
    public class TMP_TextParsingUtilities
    {
        private static readonly TMP_TextParsingUtilities s_Instance = new();

        static TMP_TextParsingUtilities() { }


        public static TMP_TextParsingUtilities instance
        {
            get { return s_Instance; }
        }


        public static int GetHashCode(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ ToUpperASCIIFast(s[i]);

            return hashCode;
        }

        public static int GetHashCodeCaseSensitive(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ s[i];

            return hashCode;
        }


        private const string k_LookupStringL = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

        private const string k_LookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";


        public static char ToLowerASCIIFast(char c)
        {
            if (c > k_LookupStringL.Length - 1)
                return c;

            return k_LookupStringL[c];
        }


        public static char ToUpperASCIIFast(char c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[c];
        }


        public static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[(int)c];
        }


        public static uint ToLowerASCIIFast(uint c)
        {
            if (c > k_LookupStringL.Length - 1)
                return c;

            return k_LookupStringL[(int)c];
        }


        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsHighSurrogate(uint c)
        {
            return c > 0xD800 && c < 0xDBFF;
        }

        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsLowSurrogate(uint c)
        {
            return c > 0xDC00 && c < 0xDFFF;
        }

        /// <param name="highSurrogate"></param>
        /// <param name="lowSurrogate"></param>
        /// <returns></returns>
        internal static uint ConvertToUTF32(uint highSurrogate, uint lowSurrogate)
        {
            return ((highSurrogate - CodePoint.HIGH_SURROGATE_START) * 0x400) + ((lowSurrogate - CodePoint.LOW_SURROGATE_START) + CodePoint.UNICODE_PLANE01_START);
        }

        /// <param name="c"></param>
        /// <returns></returns>
        internal static bool IsDiacriticalMark(uint c)
        {
            return c >= 0x300 && c <= 0x36F || c >= 0x1AB0 && c <= 0x1AFF || c >= 0x1DC0 && c <= 0x1DFF || c >= 0x20D0 && c <= 0x20FF || c >= 0xFE20 && c <= 0xFE2F;
        }

        internal static bool IsBaseGlyph(uint c)
        {
            return !(c >= 0x300 && c <= 0x36F || c >= 0x1AB0 && c <= 0x1AFF || c >= 0x1DC0 && c <= 0x1DFF || c >= 0x20D0 && c <= 0x20FF || c >= 0xFE20 && c <= 0xFE2F || c == 0xE31 || c >= 0xE34 && c <= 0xE3A || c >= 0xE47 && c <= 0xE4E || c >= 0x591 && c <= 0x5BD || c == 0x5BF || c >= 0x5C1 && c <= 0x5C2 || c >= 0x5C4 && c <= 0x5C5 || c == 0x5C7 || c >= 0x610 && c <= 0x61A || c >= 0x64B && c <= 0x65F || c == 0x670 || c >= 0x6D6 && c <= 0x6DC || c >= 0x6DF && c <= 0x6E4 || c >= 0x6E7 && c <= 0x6E8 || c >= 0x6EA && c <= 0x6ED ||
                c >= 0x8D3 && c <= 0x8E1 || c >= 0x8E3 && c <= 0x8FF ||
                c >= 0xFBB2 && c <= 0xFBC1
                );
        }


        internal static bool IsHangul(uint c)
        {
            return c >= 0x1100 && c <= 0x11ff || c >= 0xA960 && c <= 0xA97F || c >= 0xD7B0 && c <= 0xD7FF || c >= 0x3130 && c <= 0x318F || c >= 0xFFA0 && c <= 0xFFDC || c >= 0xAC00 && c <= 0xD7AF;
        }

        internal static bool IsCJK(uint c)
        {
            return c >= 0x3100  && c <= 0x312F  || c >= 0x31A0  && c <= 0x31BF  || c >= 0x4E00  && c <= 0x9FFF  || c >= 0x3400  && c <= 0x4DBF  || c >= 0x20000 && c <= 0x2A6DF || c >= 0x2A700 && c <= 0x2B73F || c >= 0x2B740 && c <= 0x2B81F || c >= 0x2B820 && c <= 0x2CEAF || c >= 0x2CEB0 && c <= 0x2EBE0 || c >= 0x30000 && c <= 0x3134A || c >= 0xF900  && c <= 0xFAFF  || c >= 0x2F800 && c <= 0x2FA1F || c >= 0x2F00  && c <= 0x2FDF  || c >= 0x2E80  && c <= 0x2EFF  || c >= 0x31C0  && c <= 0x31EF  || c >= 0x2FF0  && c <= 0x2FFF  || c >= 0x3040  && c <= 0x309F  || c >= 0x1B100 && c <= 0x1B12F || c >= 0x1AFF0 && c <= 0x1AFFF || c >= 0x1B000 && c <= 0x1B0FF || c >= 0x1B130 && c <= 0x1B16F || c >= 0x3190  && c <= 0x319F  || c >= 0x30A0  && c <= 0x30FF  || c >= 0x31F0  && c <= 0x31FF  || c >= 0xFF65  && c <= 0xFF9F;
        }
    }
}
