using System;

/// <summary>
/// Implementation of UAX #14: Unicode Line Breaking Algorithm
/// Determines line break opportunities in text.
/// </summary>
public sealed class LineBreakAlgorithm
{
    private readonly IUnicodeDataProvider dataProvider;

    public LineBreakAlgorithm(IUnicodeDataProvider dataProvider)
    {
        this.dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    /// <summary>
    /// Get break opportunities for a sequence of codepoints.
    /// Returns array of length codepoints.Length + 1 where:
    /// - breaks[0] = can break before first character (always false per LB2)
    /// - breaks[i] = can break between codepoints[i-1] and codepoints[i]
    /// - breaks[length] = can break after last character (always true per LB3)
    /// </summary>
    public void GetBreakOpportunities(ReadOnlySpan<int> codePoints, Span<bool> breaks)
    {
        int length = codePoints.Length;
        
        if (breaks.Length < length + 1)
            throw new ArgumentException($"breaks array must have length at least {length + 1}");

        if (length == 0)
        {
            breaks[0] = true; // Empty text: LB3
            return;
        }

        // LB2: Never break at the start of text
        breaks[0] = false;

        // LB3: Always break at the end of text
        breaks[length] = true;

        // Check breaks between characters
        for (int i = 0; i < length - 1; i++)
        {
            breaks[i + 1] = CanBreakBetween(codePoints, i);
        }
    }

    /// <summary>
    /// Simplified version that allocates the result array
    /// </summary>
    public bool[] GetBreakOpportunities(ReadOnlySpan<int> codePoints)
    {
        bool[] breaks = new bool[codePoints.Length + 1];
        GetBreakOpportunities(codePoints, breaks);
        return breaks;
    }

    /// <summary>
    /// Check if break is allowed at a specific position (after codepoint at index)
    /// </summary>
    public bool CanBreakAt(ReadOnlySpan<int> codePoints, int index)
    {
        if (index < 0)
            return false; // LB2
        if (index >= codePoints.Length)
            return true;  // LB3
        if (index == 0)
            return false; // LB2 - no break before first char

        return CanBreakBetween(codePoints, index - 1);
    }

    /// <summary>
    /// Determine if break is allowed between codePoints[index] and codePoints[index+1]
    /// </summary>
    private bool CanBreakBetween(ReadOnlySpan<int> codePoints, int index)
    {
        var beforeRaw = dataProvider.GetLineBreakClass(codePoints[index]);
        var afterRaw = dataProvider.GetLineBreakClass(codePoints[index + 1]);
        int afterCp = codePoints[index + 1];
        int beforeCp = codePoints[index];

        // LB9: Do not break a combining character sequence
        // EXCEPT: The SP, BK, CR, LF, NL, ZW, and ZWJ classes are excepted from LB9
        // Note: SA with General_Category=Mn/Mc should be treated as CM per LB1
        var afterGc = dataProvider.GetGeneralCategory(afterCp);
        bool afterIsCombining = afterRaw == LineBreakClass.CM || 
                                afterRaw == LineBreakClass.ZWJ ||
                                (afterRaw == LineBreakClass.SA && (afterGc == GeneralCategory.Mn || afterGc == GeneralCategory.Mc));
        
        if (afterIsCombining)
        {
            // LB9 exception: these classes allow break before combining marks
            bool beforeIsException = beforeRaw == LineBreakClass.SP || 
                                     beforeRaw == LineBreakClass.BK ||
                                     beforeRaw == LineBreakClass.CR || 
                                     beforeRaw == LineBreakClass.LF ||
                                     beforeRaw == LineBreakClass.NL || 
                                     beforeRaw == LineBreakClass.ZW ||
                                     beforeRaw == LineBreakClass.ZWJ;
            
            if (!beforeIsException)
            {
                // × [CM | ZWJ | SA(combining)]
                return false;
            }
            // After SP/BK/CR/LF/NL/ZW/ZWJ, combining marks are treated as AL (fall through to LB10)
        }

        // LB9 also says: Treat X (CM | ZWJ)* as if it were X
        // So if beforeRaw is CM/ZWJ or SA(combining), we need to look back to find the effective class
        // EXCEPTION: If we find SP, BK, CR, LF, NL, ZW, ZWJ - the CM/ZWJ is treated as AL
        var effectiveBeforeRaw = beforeRaw;
        int effectiveIndex = index;
        int effectiveCp = beforeCp;
        
        // Helper: Check if class+codepoint is effectively combining
        bool IsEffectivelyCombiningLocal(LineBreakClass cls, int cp)
        {
            if (cls == LineBreakClass.CM || cls == LineBreakClass.ZWJ)
                return true;
            if (cls == LineBreakClass.SA)
            {
                var gc = dataProvider.GetGeneralCategory(cp);
                return gc == GeneralCategory.Mn || gc == GeneralCategory.Mc;
            }
            return false;
        }
        
        while (effectiveIndex > 0 && IsEffectivelyCombiningLocal(effectiveBeforeRaw, effectiveCp))
        {
            effectiveIndex--;
            effectiveCp = codePoints[effectiveIndex];
            effectiveBeforeRaw = dataProvider.GetLineBreakClass(effectiveCp);
        }
        
        // If we're still on a combining mark at the start, treat as AL (LB10)
        if (IsEffectivelyCombiningLocal(effectiveBeforeRaw, effectiveCp))
            effectiveBeforeRaw = LineBreakClass.AL;
        
        // If effectiveBefore is in the LB9 exception list, CM/ZWJ should be treated as AL, not inherit from base
        bool baseIsLB9Exception = effectiveBeforeRaw == LineBreakClass.SP ||
                                   effectiveBeforeRaw == LineBreakClass.BK ||
                                   effectiveBeforeRaw == LineBreakClass.CR ||
                                   effectiveBeforeRaw == LineBreakClass.LF ||
                                   effectiveBeforeRaw == LineBreakClass.NL ||
                                   effectiveBeforeRaw == LineBreakClass.ZW ||
                                   effectiveBeforeRaw == LineBreakClass.ZWJ;
        
        if (baseIsLB9Exception && IsEffectivelyCombiningLocal(beforeRaw, beforeCp))
        {
            // CM/ZWJ after exception class is treated as AL, not inherited from base
            effectiveBeforeRaw = LineBreakClass.AL;
        }

        // LB10: Treat any remaining combining mark or ZWJ as AL
        var before = ResolveClass(effectiveBeforeRaw);
        var after = ResolveClass(afterRaw);

        // LB4: Always break after hard line breaks
        // BK !
        if (before == LineBreakClass.BK)
            return true;

        // LB5: Treat CR followed by LF, as well as CR, LF, and NL as hard line breaks
        // CR × LF
        if (before == LineBreakClass.CR && after == LineBreakClass.LF)
            return false;
        // CR !
        // LF !
        // NL !
        if (before == LineBreakClass.CR || before == LineBreakClass.LF || before == LineBreakClass.NL)
            return true;

        // LB6: Do not break before hard line breaks
        // × ( BK | CR | LF | NL )
        if (after == LineBreakClass.BK || after == LineBreakClass.CR || 
            after == LineBreakClass.LF || after == LineBreakClass.NL)
            return false;

        // LB7: Do not break before spaces or zero width space
        // × SP
        // × ZW
        if (after == LineBreakClass.SP || after == LineBreakClass.ZW)
            return false;

        // LB8: Break before any character following a zero-width space
        // ZW ÷
        if (before == LineBreakClass.ZW)
            return true;

        // LB8a: Do not break after a zero width joiner
        // ZWJ × (except as handled above)

        // LB11: Do not break before or after Word joiner
        // × WJ
        // WJ ×
        if (before == LineBreakClass.WJ || after == LineBreakClass.WJ)
            return false;

        // LB12: Do not break after NBSP and related characters
        // GL ×
        if (before == LineBreakClass.GL)
            return false;

        // LB12a: Do not break before NBSP and related characters,
        // except after spaces and hyphens
        // [^SP BA HY] × GL
        if (after == LineBreakClass.GL)
        {
            if (before != LineBreakClass.SP && before != LineBreakClass.BA && before != LineBreakClass.HY)
                return false;
        }

        // LB13: Do not break before ']' or '!' or ';' or '/'
        // × CL
        // × CP
        // × EX
        // × IS
        // × SY
        if (after == LineBreakClass.CL || after == LineBreakClass.CP || 
            after == LineBreakClass.EX || after == LineBreakClass.IS || after == LineBreakClass.SY)
            return false;

        // LB14: Do not break after '[', even after spaces
        // OP SP* ×
        if (before == LineBreakClass.OP)
            return false;
        // Handle OP SP* × by looking back (skip CM attached to base)
        if (before == LineBreakClass.SP)
        {
            for (int i = effectiveIndex - 1; i >= 0; i--)
            {
                var rawCls = dataProvider.GetLineBreakClass(codePoints[i]);
                // Skip combining marks
                if (rawCls == LineBreakClass.CM || rawCls == LineBreakClass.ZWJ)
                    continue;
                var cls = ResolveClass(rawCls);
                if (cls == LineBreakClass.OP)
                    return false;
                if (cls != LineBreakClass.SP)
                    break;
            }
        }

        // LB15: Do not break within '"[', even with intervening spaces
        // QU SP* × OP
        if (after == LineBreakClass.OP)
        {
            var prev = before;
            int i = effectiveIndex;
            while ((prev == LineBreakClass.SP || prev == LineBreakClass.AL) && i > 0)
            {
                i--;
                var rawCls = dataProvider.GetLineBreakClass(codePoints[i]);
                // Skip combining marks (they resolve to AL)
                if (rawCls == LineBreakClass.CM || rawCls == LineBreakClass.ZWJ)
                    continue;
                prev = ResolveClass(rawCls);
                if (prev != LineBreakClass.SP)
                    break;
            }
            if (prev == LineBreakClass.QU)
                return false;
        }

        // LB16: Do not break between closing punctuation and a nonstarter
        // (CL | CP) SP* × NS
        if (after == LineBreakClass.NS)
        {
            var prev = before;
            int i = effectiveIndex;
            while ((prev == LineBreakClass.SP || prev == LineBreakClass.AL) && i > 0)
            {
                i--;
                var rawCls = dataProvider.GetLineBreakClass(codePoints[i]);
                if (rawCls == LineBreakClass.CM || rawCls == LineBreakClass.ZWJ)
                    continue;
                prev = ResolveClass(rawCls);
                if (prev != LineBreakClass.SP)
                    break;
            }
            if (prev == LineBreakClass.CL || prev == LineBreakClass.CP)
                return false;
        }

        // LB17: Do not break within '——', even with intervening spaces
        // B2 SP* × B2
        if (after == LineBreakClass.B2)
        {
            var prev = before;
            int i = effectiveIndex;
            while ((prev == LineBreakClass.SP || prev == LineBreakClass.AL) && i > 0)
            {
                i--;
                var rawCls = dataProvider.GetLineBreakClass(codePoints[i]);
                if (rawCls == LineBreakClass.CM || rawCls == LineBreakClass.ZWJ)
                    continue;
                prev = ResolveClass(rawCls);
                if (prev != LineBreakClass.SP)
                    break;
            }
            if (prev == LineBreakClass.B2)
                return false;
        }

        // LB19: × QU and QU × — no break around quotation marks
        // Per LineBreakTest.txt:
        // - BA × QU_Pi = × [19.11] — no break before initial quote
        // - QU_Pf × AL = × [19.13] — no break after final quote
        
        // × QU — no break before any quote
        if (after == LineBreakClass.QU)
            return false;
        
        // QU × — no break after any quote (except before SP for line breaking)
        if (before == LineBreakClass.QU)
        {
            if (after != LineBreakClass.SP)
                return false;
            // QU × SP falls through to LB18
        }
        
        // QU SP* × — no break after quote followed by spaces
        if (before == LineBreakClass.SP)
        {
            // Look back through spaces to see if there's QU
            for (int i = effectiveIndex - 1; i >= 0; i--)
            {
                var rawCls = dataProvider.GetLineBreakClass(codePoints[i]);
                if (rawCls == LineBreakClass.CM || rawCls == LineBreakClass.ZWJ)
                    continue;
                var cls = ResolveClass(rawCls);
                if (cls == LineBreakClass.QU)
                    return false;  // QU SP* × — no break
                if (cls != LineBreakClass.SP)
                    break;
            }
        }

        // LB18: Break after spaces
        // SP ÷
        if (before == LineBreakClass.SP)
            return true;

        // LB20: Break before and after unresolved CB
        // ÷ CB
        // CB ÷
        if (before == LineBreakClass.CB || after == LineBreakClass.CB)
            return true;

        // LB21: Do not break before hyphen-minus, other hyphens, etc.
        // × BA
        // × HY
        // × NS
        // BB ×
        if (after == LineBreakClass.BA || after == LineBreakClass.HY || after == LineBreakClass.NS)
            return false;
        if (before == LineBreakClass.BB)
            return false;
        // Note: (BA | HY) × [^SP] rule is checked earlier (before LB15)

        // LB21a: Don't break after Hebrew + Hyphen
        // HL (HY | BA) ×
        if (index > 0 && (before == LineBreakClass.HY || before == LineBreakClass.BA))
        {
            var prev = ResolveClass(dataProvider.GetLineBreakClass(codePoints[index - 1]));
            if (prev == LineBreakClass.HL)
                return false;
        }

        // LB21b: Don't break between SY and HL
        // SY × HL
        if (before == LineBreakClass.SY && after == LineBreakClass.HL)
            return false;

        // LB22: Do not break before ellipsis
        // × IN
        if (after == LineBreakClass.IN)
            return false;

        // LB23: Do not break between digits and letters
        // (AL | HL) × NU
        // NU × (AL | HL)
        if ((before == LineBreakClass.AL || before == LineBreakClass.HL) && after == LineBreakClass.NU)
            return false;
        if (before == LineBreakClass.NU && (after == LineBreakClass.AL || after == LineBreakClass.HL))
            return false;

        // LB23a: Do not break between numeric prefixes and ID, or ID and numeric postfixes
        // PR × (ID | EB | EM)
        // (ID | EB | EM) × PO
        if (before == LineBreakClass.PR && 
            (after == LineBreakClass.ID || after == LineBreakClass.EB || after == LineBreakClass.EM))
            return false;
        if ((before == LineBreakClass.ID || before == LineBreakClass.EB || before == LineBreakClass.EM) && 
            after == LineBreakClass.PO)
            return false;

        // LB24: Do not break between numeric prefix/postfix and letters
        // (PR | PO) × (AL | HL)
        // (AL | HL) × (PR | PO)
        if ((before == LineBreakClass.PR || before == LineBreakClass.PO) && 
            (after == LineBreakClass.AL || after == LineBreakClass.HL))
            return false;
        if ((before == LineBreakClass.AL || before == LineBreakClass.HL) && 
            (after == LineBreakClass.PR || after == LineBreakClass.PO))
            return false;

        // LB25: Do not break between the following pairs of classes relevant to numbers
        // Note: These rules apply primarily in numeric context
        // NU × (PO | PR) - always applies when NU is before
        if (before == LineBreakClass.NU && (after == LineBreakClass.PO || after == LineBreakClass.PR))
            return false;
        // (PO | PR) × NU - always applies when NU is after
        if ((before == LineBreakClass.PO || before == LineBreakClass.PR) && after == LineBreakClass.NU)
            return false;
        // (HY | IS | SY) × NU - numeric separators before number
        if ((before == LineBreakClass.HY || before == LineBreakClass.IS || before == LineBreakClass.SY) &&
            after == LineBreakClass.NU)
            return false;
        // NU × NU - consecutive digits
        if (before == LineBreakClass.NU && after == LineBreakClass.NU)
            return false;
        // (CL | CP) × (PO | PR) - only in numeric context, check if NU is nearby
        // Simplified: only apply if there's NU before CL/CP
        // (PO | PR) × OP - only in numeric context
        // Simplified: only apply if there's NU after OP
        // These are complex rules that require lookahead/lookbehind for full compliance

        // LB26: Do not break a Korean syllable
        // JL × (JL | JV | H2 | H3)
        if (before == LineBreakClass.JL && 
            (after == LineBreakClass.JL || after == LineBreakClass.JV || 
             after == LineBreakClass.H2 || after == LineBreakClass.H3))
            return false;
        // (JV | H2) × (JV | JT)
        if ((before == LineBreakClass.JV || before == LineBreakClass.H2) &&
            (after == LineBreakClass.JV || after == LineBreakClass.JT))
            return false;
        // (JT | H3) × JT
        if ((before == LineBreakClass.JT || before == LineBreakClass.H3) &&
            after == LineBreakClass.JT)
            return false;

        // LB27: Treat a Korean Syllable Block the same as ID
        // (JL | JV | JT | H2 | H3) × PO
        if ((before == LineBreakClass.JL || before == LineBreakClass.JV || before == LineBreakClass.JT ||
             before == LineBreakClass.H2 || before == LineBreakClass.H3) && after == LineBreakClass.PO)
            return false;
        // PR × (JL | JV | JT | H2 | H3)
        if (before == LineBreakClass.PR && 
            (after == LineBreakClass.JL || after == LineBreakClass.JV || after == LineBreakClass.JT ||
             after == LineBreakClass.H2 || after == LineBreakClass.H3))
            return false;

        // LB28: Do not break between alphabetics
        // (AL | HL) × (AL | HL)
        if ((before == LineBreakClass.AL || before == LineBreakClass.HL) && 
            (after == LineBreakClass.AL || after == LineBreakClass.HL))
            return false;

        // LB28a: Do not break inside orthographic syllables of Brahmic scripts
        // Per UAX #14 and LineBreakTest.txt:
        // AP × (AK | AS) [28.1]
        if (before == LineBreakClass.AP && (after == LineBreakClass.AK || after == LineBreakClass.AS))
            return false;
        // (AK | AS) × (VF | VI) [28.2]
        if ((before == LineBreakClass.AK || before == LineBreakClass.AS) &&
            (after == LineBreakClass.VF || after == LineBreakClass.VI))
            return false;
        // VI × (AK | AS) [28.3]
        if (before == LineBreakClass.VI && (after == LineBreakClass.AK || after == LineBreakClass.AS))
            return false;
        // AP × ◌ (dotted circle after aksara prefix - no break) [28.11]
        // Note: AK ÷ 25CC is ALLOWED (break), only AP × 25CC is forbidden
        if (before == LineBreakClass.AP && afterCp == 0x25CC)
            return false;
        // ◌ (U+25CC) × (VF | VI) — dotted circle followed by virama
        // Use effectiveCp to handle CM sequences like ◌ + CM + VF
        if (effectiveCp == 0x25CC && (after == LineBreakClass.VF || after == LineBreakClass.VI))
            return false;

        // LB29: Do not break between numeric punctuation and alphabetics
        // IS × (AL | HL)
        if (before == LineBreakClass.IS && (after == LineBreakClass.AL || after == LineBreakClass.HL))
            return false;

        // LB30: Do not break between letters, numbers, or ordinary symbols and
        // opening or closing parentheses
        // (AL | HL | NU) × OP
        // But break before OP with East_Asian_Width=F/W (fullwidth/wide)
        if ((before == LineBreakClass.AL || before == LineBreakClass.HL || before == LineBreakClass.NU) &&
            after == LineBreakClass.OP)
        {
            // Allow break before East Asian Wide/Fullwidth OP characters
            int nextCp = codePoints[index + 1];
            var eaw = dataProvider.GetEastAsianWidth(nextCp);
            if (IsEastAsianWide(eaw))
            {
                // Allow break (fall through to LB31)
            }
            else
            {
                return false;
            }
        }
        // CP × (AL | HL | NU)
        if (before == LineBreakClass.CP &&
            (after == LineBreakClass.AL || after == LineBreakClass.HL || after == LineBreakClass.NU))
            return false;

        // LB30a: Break between two regional indicator symbols if and only if
        // there are an even number of regional indicators preceding the position
        // sot (RI RI)* RI × RI
        // [^RI] (RI RI)* RI × RI
        if (before == LineBreakClass.RI && after == LineBreakClass.RI)
        {
            // Count consecutive RI characters before and including current position
            int riCount = 1; // current 'before' is RI
            for (int i = index - 1; i >= 0; i--)
            {
                if (dataProvider.GetLineBreakClass(codePoints[i]) == LineBreakClass.RI)
                    riCount++;
                else
                    break;
            }
            // Break only if odd number of RI before (so total becomes even)
            return (riCount % 2) == 0;
        }

        // LB30b: Do not break between an emoji base and an emoji modifier
        // EB × EM
        // Also: Extended_Pictographic × EM (per UAX #14)
        if (after == LineBreakClass.EM)
        {
            if (before == LineBreakClass.EB)
                return false;
            // Check if before is Extended_Pictographic using data from emoji-data.txt
            // Use effectiveCp to handle CM sequences like ExtPict + CM × EM
            if (dataProvider.IsExtendedPictographic(effectiveCp))
                return false;
        }

        // LB31: Break everywhere else
        // ALL ÷
        // ÷ ALL
        return true;
    }

    /// <summary>
    /// LB1: Resolve some ambiguous classes
    /// LB10: Treat any remaining combining mark or ZWJ as AL
    /// </summary>
    private static LineBreakClass ResolveClass(LineBreakClass cls)
    {
        return cls switch
        {
            // LB10: Treat any remaining CM or ZWJ as AL
            LineBreakClass.CM => LineBreakClass.AL,
            LineBreakClass.ZWJ => LineBreakClass.AL,
            
            // LB1: Resolve AI, SG, XX to AL
            LineBreakClass.AI => LineBreakClass.AL,
            LineBreakClass.SG => LineBreakClass.AL,
            LineBreakClass.XX => LineBreakClass.AL,
            
            // LB1: Resolve SA to AL (simplified - full impl checks General_Category)
            LineBreakClass.SA => LineBreakClass.AL,
            
            // LB1: Resolve CJ to NS
            LineBreakClass.CJ => LineBreakClass.NS,
            
            _ => cls
        };
    }
    
    /// <summary>
    /// Check if a class is a combining class (CM, ZWJ)
    /// Note: SA is NOT automatically included - use IsSouthAsianCombining for SA codepoints
    /// </summary>
    private static bool IsCombiningClass(LineBreakClass cls)
    {
        return cls == LineBreakClass.CM || 
               cls == LineBreakClass.ZWJ;
    }
    
    /// <summary>
    /// Check if General_Category indicates initial punctuation (Pi)
    /// Used for LB15a: QU_Pi rules
    /// </summary>
    private static bool IsQuotePi(GeneralCategory gc)
    {
        return gc == GeneralCategory.Pi;
    }
    
    /// <summary>
    /// Check if General_Category indicates final punctuation (Pf)
    /// Used for LB15b: QU_Pf rules
    /// </summary>
    private static bool IsQuotePf(GeneralCategory gc)
    {
        return gc == GeneralCategory.Pf;
    }
    
    /// <summary>
    /// Check if East Asian Width indicates Wide or Fullwidth character
    /// Used for LB30 to detect East Asian context
    /// </summary>
    private static bool IsEastAsianWide(EastAsianWidth eaw)
    {
        return eaw == EastAsianWidth.W || eaw == EastAsianWidth.F;
    }
}