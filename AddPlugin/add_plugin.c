#include <stdint.h>
#include <stdlib.h>
#include <fribidi/fribidi.h>

#include <SheenBidi/SheenBidi.h>

#ifdef __cplusplus
extern "C" {
#endif

    __declspec(dllexport)
        int __cdecl fribidi_unity_reorder_utf32(
            const uint32_t* logical,
            int             length,
            int             baseDirCode,
            uint32_t* visualOut,
            int* l2vOut
        )
    {
        if (!logical || !visualOut || !l2vOut || length <= 0)
            return 0;

        /* UTF-32: length = количество uint32_t, а SheenBidi ждёт длину
           в кодовых единицах → умножаем на sizeof(uint32_t). */
        SBCodepointSequence seq = (SBCodepointSequence){
            SBStringEncodingUTF32,
            (const void*)logical,
            (SBUInteger)(length * (int)sizeof(uint32_t))
        };

        SBAlgorithmRef algorithm = SBAlgorithmCreate(&seq);
        if (!algorithm)
            return 0;

        SBLevel baseLevel;
        switch (baseDirCode)
        {
        case 1: baseLevel = SBLevelDefaultLTR; break;
        case 2: baseLevel = SBLevelDefaultRTL; break;
        default: baseLevel = SBLevelDefaultLTR; break; /* auto, но с LTR по умолчанию */
        }

        /* Параграф на весь текст, как в примере: suggestedLength = INT32_MAX */
        SBParagraphRef paragraph =
            SBAlgorithmCreateParagraph(algorithm, 0, INT32_MAX, baseLevel);
        if (!paragraph)
        {
            SBAlgorithmRelease(algorithm);
            return 0;
        }

        /* Длина параграфа в кодовых единицах (для UTF-32 — в uint32_t) */
        SBUInteger paraLen = SBParagraphGetLength(paragraph);
        if (paraLen > (SBUInteger)length)
            paraLen = (SBUInteger)length;

        SBLineRef line = SBParagraphCreateLine(paragraph, 0, paraLen);
        if (!line)
        {
            SBParagraphRelease(paragraph);
            SBAlgorithmRelease(algorithm);
            return 0;
        }

        SBUInteger runCount = SBLineGetRunCount(line);
        const SBRun* runs = SBLineGetRunsPtr(line);

        if (runCount == 0)
        {
            SBLineRelease(line);
            SBParagraphRelease(paragraph);
            SBAlgorithmRelease(algorithm);
            return 0;
        }

        /* Инициализируем карту logical->visual тождественно */
        for (int i = 0; i < length; ++i)
            l2vOut[i] = i;

        SBUInteger visualIndex = 0;

        for (SBUInteger r = 0; r < runCount; ++r)
        {
            const SBRun* run = &runs[r];
            SBUInteger   off = run->offset;
            SBUInteger   len = run->length;
            SBLevel      lvl = run->level;

            if (off >= paraLen)
                continue;
            if (off + len > paraLen)
                len = paraLen - off;

            if ((lvl & 1) == 0)
            {
                /* LTR-run */
                for (SBUInteger i = 0; i < len; ++i)
                {
                    if (visualIndex >= (SBUInteger)length)
                        break;

                    SBUInteger logicalIndex = off + i;
                    l2vOut[logicalIndex] = (int)visualIndex;
                    visualOut[visualIndex] = logical[logicalIndex];
                    ++visualIndex;
                }
            }
            else
            {
                /* RTL-run: визуально справа налево */
                for (SBUInteger i = 0; i < len; ++i)
                {
                    if (visualIndex >= (SBUInteger)length)
                        break;

                    SBUInteger logicalIndex = off + (len - 1 - i);
                    l2vOut[logicalIndex] = (int)visualIndex;
                    visualOut[visualIndex] = logical[logicalIndex];
                    ++visualIndex;
                }
            }
        }

        /* Зеркалирование скобок и т.п. */
        SBMirrorLocatorRef mirrorLocator = SBMirrorLocatorCreate();
        if (mirrorLocator)
        {
            SBMirrorLocatorLoadLine(mirrorLocator, line, (void*)logical);
            const SBMirrorAgent* agent = SBMirrorLocatorGetAgent(mirrorLocator);

            while (SBMirrorLocatorMoveNext(mirrorLocator))
            {
                SBUInteger logicalIndex = agent->index;
                if (logicalIndex >= paraLen)
                    continue;

                int v = l2vOut[logicalIndex];
                if (v >= 0 && v < length)
                {
                    visualOut[v] = (uint32_t)agent->mirror;
                }
            }

            SBMirrorLocatorRelease(mirrorLocator);
        }

        SBLineRelease(line);
        SBParagraphRelease(paragraph);
        SBAlgorithmRelease(algorithm);

        return 1;
    }

    __declspec(dllexport)
        int __cdecl fribidi_unity_detect_base_direction(
            const uint32_t* logical,
            int             length
        )
    {
        if (!logical || length <= 0)
            return 0;

        SBCodepointSequence seq = (SBCodepointSequence){
            SBStringEncodingUTF32,
            (const void*)logical,
            (SBUInteger)(length * (int)sizeof(uint32_t))
        };

        SBAlgorithmRef algorithm = SBAlgorithmCreate(&seq);
        if (!algorithm)
            return 0;

        SBParagraphRef paragraph =
            SBAlgorithmCreateParagraph(algorithm, 0, INT32_MAX, SBLevelDefaultLTR);
        if (!paragraph)
        {
            SBAlgorithmRelease(algorithm);
            return 0;
        }

        SBUInteger paraLen = SBParagraphGetLength(paragraph);
        if (paraLen > (SBUInteger)length)
            paraLen = (SBUInteger)length;

        SBLineRef line = SBParagraphCreateLine(paragraph, 0, paraLen);
        if (!line)
        {
            SBParagraphRelease(paragraph);
            SBAlgorithmRelease(algorithm);
            return 0;
        }

        SBUInteger   runCount = SBLineGetRunCount(line);
        const SBRun* runs = SBLineGetRunsPtr(line);

        int isRTL = 0;
        if (runCount > 0)
        {
            SBLevel baseLevel = runs[0].level;
            isRTL = (baseLevel & 1) ? 1 : 0;
        }

        SBLineRelease(line);
        SBParagraphRelease(paragraph);
        SBAlgorithmRelease(algorithm);

        return isRTL; /* 1 = RTL, 0 = LTR */
    }

    __declspec(dllexport)
        int __cdecl fribidi_unity_has_rtl(
            const uint32_t* logical,
            int             length
        )
    {
        if (!logical || length <= 0)
            return 0;

        FriBidiStrIndex len = (FriBidiStrIndex)length;

        FriBidiCharType* bidi_types = (FriBidiCharType*)
            malloc(len * sizeof(FriBidiCharType));

        if (!bidi_types)
            return 0;

        fribidi_get_bidi_types((const FriBidiChar*)logical, len, bidi_types);

        int has_rtl = 0;

        for (FriBidiStrIndex i = 0; i < len; ++i)
        {
            FriBidiCharType t = bidi_types[i];

            if (FRIBIDI_IS_RTL(t) || t == FRIBIDI_TYPE_AN)
            {
                has_rtl = 1;
                break;
            }
        }

        free(bidi_types);
        return has_rtl;
    }

#ifdef __cplusplus
}
#endif
