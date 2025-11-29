using System;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace TMPro
{
    public abstract partial class TMP_Text
    {
        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPreferredValues()
        {
            m_isPreferredWidthDirty = true;
            float preferredWidth = GetPreferredWidth();

            m_isPreferredHeightDirty = true;
            float preferredHeight = GetPreferredHeight();

            m_isPreferredWidthDirty = true;
            m_isPreferredHeightDirty = true;

            return new Vector2(preferredWidth, preferredHeight);
        }

        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object given the provided width and height.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPreferredValues(float width, float height)
        {
            m_isCalculatingPreferredValues = true;
            ParseInputText();

            Vector2 margin = new Vector2(width, height);

            float preferredWidth = GetPreferredWidth(margin);

            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }

        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object given a certain string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 GetPreferredValues(string text)
        {
            m_isCalculatingPreferredValues = true;

            SetTextInternal(text);
            SetArraySizes(m_TextProcessingArray);

            Vector2 margin = k_LargePositiveVector2;

            float preferredWidth = GetPreferredWidth(margin);

            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }

        /// <summary>
        ///  Function to Calculate the Preferred Width and Height of the text object given a certain string and size of text container.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 GetPreferredValues(string text, float width, float height)
        {
            m_isCalculatingPreferredValues = true;

            SetTextInternal(text);
            SetArraySizes(m_TextProcessingArray);

            Vector2 margin = new Vector2(width, height);

            float preferredWidth = GetPreferredWidth(margin, m_TextWrappingMode);

            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }

        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <returns></returns>
        protected float GetPreferredWidth()
        {
            if (TMP_Settings.instance == null) return 0;

            if (!m_isPreferredWidthDirty)
                return m_preferredWidth;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            Vector2 margin = k_LargePositiveVector2;

            m_isCalculatingPreferredValues = true;
            ParseInputText();

            m_AutoSizeIterationCount = 0;
            TextWrappingModes wrapMode =
                m_TextWrappingMode == TextWrappingModes.Normal || m_TextWrappingMode == TextWrappingModes.NoWrap
                    ? TextWrappingModes.NoWrap
                    : TextWrappingModes.PreserveWhitespaceNoWrap;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, wrapMode).x;

            m_isPreferredWidthDirty = false;

            return preferredWidth;
        }

        private float GetPreferredWidth(Vector2 margin)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            m_AutoSizeIterationCount = 0;
            TextWrappingModes wrapMode =
                m_TextWrappingMode == TextWrappingModes.Normal || m_TextWrappingMode == TextWrappingModes.NoWrap
                    ? TextWrappingModes.NoWrap
                    : TextWrappingModes.PreserveWhitespaceNoWrap;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, wrapMode).x;

            return preferredWidth;
        }

        private float GetPreferredWidth(Vector2 margin, TextWrappingModes wrapMode)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            m_AutoSizeIterationCount = 0;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, wrapMode).x;

            return preferredWidth;
        }

        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <returns></returns>
        protected float GetPreferredHeight()
        {
            if (TMP_Settings.instance == null) return 0;

            if (!m_isPreferredHeightDirty)
                return m_preferredHeight;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_marginWidth != 0 ? m_marginWidth : k_LargePositiveFloat,
                k_LargePositiveFloat);

            m_isCalculatingPreferredValues = true;
            ParseInputText();

            m_IsAutoSizePointSizeSet = false;
            m_AutoSizeIterationCount = 0;

            float preferredHeight = 0;

            while (m_IsAutoSizePointSizeSet == false)
            {
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, m_enableAutoSizing, m_TextWrappingMode)
                    .y;
                m_AutoSizeIterationCount += 1;
            }

            m_isPreferredHeightDirty = false;

            return preferredHeight;
        }

        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <param name="margin"></param>
        /// <returns></returns>
        private float GetPreferredHeight(Vector2 margin)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            m_IsAutoSizePointSizeSet = false;
            m_AutoSizeIterationCount = 0;

            float preferredHeight = 0;

            while (m_IsAutoSizePointSizeSet == false)
            {
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, m_enableAutoSizing, m_TextWrappingMode)
                    .y;
                m_AutoSizeIterationCount += 1;
            }

            return preferredHeight;
        }

        /// <summary>
        /// Method returning the rendered width and height of the text object.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetRenderedValues()
        {
            return GetTextBounds().size;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="onlyVisibleCharacters">Should returned value only factor in visible characters and exclude those greater than maxVisibleCharacters for instance.</param>
        /// <returns></returns>
        public Vector2 GetRenderedValues(bool onlyVisibleCharacters)
        {
            return GetTextBounds(onlyVisibleCharacters).size;
        }

        /// <summary>
        /// Method returning the rendered width of the text object.
        /// </summary>
        /// <returns></returns>
        private float GetRenderedWidth()
        {
            return GetRenderedValues().x;
        }

        /// <summary>
        /// Method returning the rendered width of the text object.
        /// </summary>
        /// <returns></returns>
        protected float GetRenderedWidth(bool onlyVisibleCharacters)
        {
            return GetRenderedValues(onlyVisibleCharacters).x;
        }

        /// <summary>
        /// Method returning the rendered height of the text object.
        /// </summary>
        /// <returns></returns>
        private float GetRenderedHeight()
        {
            return GetRenderedValues().y;
        }

        /// <summary>
        /// Method returning the rendered height of the text object.
        /// </summary>
        /// <returns></returns>
        protected float GetRenderedHeight(bool onlyVisibleCharacters)
        {
            return GetRenderedValues(onlyVisibleCharacters).y;
        }

        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize,
            bool isTextAutoSizingEnabled, TextWrappingModes textWrapMode)
        {
            if (m_fontAsset == null || m_fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " +
                                 GetInstanceID());

                m_IsAutoSizePointSizeSet = true;
                return Vector2.zero;
            }

            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 ||
                m_TextProcessingArray[0].unicode == (char)0)
            {
                m_IsAutoSizePointSizeSet = true;
                return Vector2.zero;
            }

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;
            m_materialReferenceStack.SetDefault(new MaterialReference(0, m_currentFontAsset, null, m_currentMaterial,
                m_padding));

            int totalCharacterCount = m_totalCharacterCount;

            if (m_internalCharacterInfo == null || totalCharacterCount > m_internalCharacterInfo.Length)
                m_internalCharacterInfo = new TMP_CharacterInfo[totalCharacterCount > 1024
                    ? totalCharacterCount + 256
                    : Mathf.NextPowerOfTwo(totalCharacterCount)];

            float baseScale = (fontSize / m_fontAsset.faceInfo.pointSize * m_fontAsset.faceInfo.scale *
                               (m_isOrthographic ? 1 : 0.1f));
            float currentElementScale = baseScale;
            float currentEmScale = fontSize * 0.01f * (m_isOrthographic ? 1 : 0.1f);
            m_fontScaleMultiplier = 1;

            m_currentFontSize = fontSize;
            m_sizeStack.SetDefault(m_currentFontSize);
            float fontSizeDelta = 0;

            m_FontStyleInternal = m_fontStyle;

            m_lineJustification =
                m_HorizontalAlignment;
            m_lineJustificationStack.SetDefault(m_lineJustification);

            m_baselineOffset = 0;
            m_baselineOffsetStack.Clear();

            m_FXScale = Vector3.one;

            m_lineOffset = 0;
            m_lineHeight = TMP_Math.FLOAT_UNSET;
            float lineGap = m_currentFontAsset.faceInfo.lineHeight -
                            (m_currentFontAsset.faceInfo.ascentLine - m_currentFontAsset.faceInfo.descentLine);

            m_cSpacing = 0;
            m_monoSpacing = 0;
            m_xAdvance = 0;

            tag_LineIndent = 0;
            tag_Indent = 0;
            m_indentStack.SetDefault(0);
            tag_NoParsing = false;

            m_characterCount = 0;


            m_firstCharacterOfLine = 0;
            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;
            m_lineNumber = 0;
            m_startOfLineAscender = 0;
            m_IsDrivenLineSpacing = false;
            m_LastBaseGlyphIndex = int.MinValue;

            float marginWidth = marginSize.x;
            float marginHeight = marginSize.y;
            m_marginLeft = 0;
            m_marginRight = 0;

            m_width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_marginLeft - m_marginRight;

            m_RenderedWidth = 0;
            m_RenderedHeight = 0;
            float textWidth = 0;
            m_isCalculatingPreferredValues = true;

            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            bool isFirstWordOfLine = true;
            m_isNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;

            WordWrapState internalWordWrapState = new WordWrapState();
            WordWrapState internalLineState = new WordWrapState();
            WordWrapState internalSoftLineBreak = new WordWrapState();

            m_AutoSizeIterationCount += 1;

            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                uint charCode = m_TextProcessingArray[i].unicode;

                if (charCode == 0x1A)
                    continue;

                #region Parse Rich Text Tag

                if (m_isRichText && charCode == 60)
                {
                    m_isTextLayoutPhase = true;
                    m_textElementType = TMP_TextElementType.Character;
                    int endTagIndex;

                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex))
                    {
                        i = endTagIndex;

                        if (m_textElementType == TMP_TextElementType.Character)
                            continue;
                    }
                }
                else
                {
                    m_textElementType = m_textInfo.characterInfo[m_characterCount].elementType;
                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;
                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;
                }

                #endregion End Parse Rich Text Tag

                int prev_MaterialIndex = m_currentMaterialIndex;
                bool isUsingAltTypeface = m_textInfo.characterInfo[m_characterCount].isUsingAlternateTypeface;

                m_isTextLayoutPhase = false;

                #region Character Substitutions

                bool isInjectedCharacter = false;

                if (characterToSubstitute.index == m_characterCount)
                {
                    charCode = characterToSubstitute.unicode;
                    m_textElementType = TMP_TextElementType.Character;
                    isInjectedCharacter = true;

                    switch (charCode)
                    {
                        case 0x03:
                            m_internalCharacterInfo[m_characterCount].textElement =
                                m_currentFontAsset.characterLookupTable[0x03];
                            m_isTextTruncated = true;
                            break;
                        case 0x2D:
                            break;
                        case 0x2026:
                            m_internalCharacterInfo[m_characterCount].textElement = m_Ellipsis.character;
                            ;
                            m_internalCharacterInfo[m_characterCount].elementType = TMP_TextElementType.Character;
                            m_internalCharacterInfo[m_characterCount].fontAsset = m_Ellipsis.fontAsset;
                            m_internalCharacterInfo[m_characterCount].material = m_Ellipsis.material;
                            m_internalCharacterInfo[m_characterCount].materialReferenceIndex = m_Ellipsis.materialIndex;

                            m_isTextTruncated = true;

                            characterToSubstitute.index = m_characterCount + 1;
                            characterToSubstitute.unicode = 0x03;
                            break;
                    }
                }

                #endregion


                #region Linked Text

                if (m_characterCount < m_firstVisibleCharacter && charCode != 0x03)
                {
                    m_internalCharacterInfo[m_characterCount].isVisible = false;
                    m_internalCharacterInfo[m_characterCount].character = (char)0x200B;
                    m_internalCharacterInfo[m_characterCount].lineNumber = 0;
                    m_characterCount += 1;
                    continue;
                }

                #endregion


                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

                if (m_textElementType == TMP_TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        if (char.IsLower((char)charCode))
                            charCode = char.ToUpper((char)charCode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        if (char.IsUpper((char)charCode))
                            charCode = char.ToLower((char)charCode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        if (char.IsLower((char)charCode))
                        {
                            smallCapsMultiplier = 0.8f;
                            charCode = char.ToUpper((char)charCode);
                        }
                    }
                }

                #endregion


                #region Look up Character Data

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                if (m_textElementType == TMP_TextElementType.Sprite)
                {
                    TMP_SpriteCharacter sprite =
                        (TMP_SpriteCharacter)m_textInfo.characterInfo[m_characterCount].textElement;
                    m_currentSpriteAsset = sprite.textAsset as TMP_SpriteAsset;
                    m_spriteIndex = (int)sprite.glyphIndex;

                    if (sprite == null) continue;

                    if (charCode == 60)
                        charCode = 57344 + (uint)m_spriteIndex;

                    if (m_currentSpriteAsset.faceInfo.pointSize > 0)
                    {
                        float spriteScale = (m_currentFontSize / m_currentSpriteAsset.faceInfo.pointSize *
                                             m_currentSpriteAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
                        currentElementScale = sprite.scale * sprite.glyph.scale * spriteScale;
                        elementAscentLine = m_currentSpriteAsset.faceInfo.ascentLine;
                        elementDescentLine = m_currentSpriteAsset.faceInfo.descentLine;
                    }
                    else
                    {
                        float spriteScale = (m_currentFontSize / m_currentFontAsset.faceInfo.pointSize *
                                             m_currentFontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
                        currentElementScale = m_currentFontAsset.faceInfo.ascentLine / sprite.glyph.metrics.height *
                                              sprite.scale * sprite.glyph.scale * spriteScale;
                        float scaleDelta = spriteScale / currentElementScale;
                        elementAscentLine = m_currentFontAsset.faceInfo.ascentLine * scaleDelta;
                        elementDescentLine = m_currentFontAsset.faceInfo.descentLine * scaleDelta;
                    }

                    m_cached_TextElement = sprite;

                    m_internalCharacterInfo[m_characterCount].elementType = TMP_TextElementType.Sprite;
                    m_internalCharacterInfo[m_characterCount].scale = currentElementScale;

                    m_currentMaterialIndex = prev_MaterialIndex;
                }
                else if (m_textElementType == TMP_TextElementType.Character)
                {
                    m_cached_TextElement = m_textInfo.characterInfo[m_characterCount].textElement;
                    if (m_cached_TextElement == null) continue;

                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;

                    float adjustedScale;
                    if (isInjectedCharacter && m_TextProcessingArray[i].unicode == 0x0A &&
                        m_characterCount != m_firstCharacterOfLine)
                        adjustedScale = m_textInfo.characterInfo[m_characterCount - 1].pointSize * smallCapsMultiplier /
                                        m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale *
                                        (m_isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_currentFontSize * smallCapsMultiplier /
                                        m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale *
                                        (m_isOrthographic ? 1 : 0.1f);

                    if (isInjectedCharacter && charCode == 0x2026)
                    {
                        elementAscentLine = 0;
                        elementDescentLine = 0;
                    }
                    else
                    {
                        elementAscentLine = m_currentFontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_currentFontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale * m_fontScaleMultiplier * m_cached_TextElement.scale;

                    m_internalCharacterInfo[m_characterCount].elementType = TMP_TextElementType.Character;
                }

                #endregion


                #region Handle Soft Hyphen

                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == 0xAD || charCode == 0x03)
                    currentElementScale = 0;

                #endregion


                m_internalCharacterInfo[m_characterCount].character = (char)charCode;

                Glyph altGlyph = m_textInfo.characterInfo[m_characterCount].alternativeGlyph;
                GlyphMetrics currentGlyphMetrics =
                    altGlyph == null ? m_cached_TextElement.m_Glyph.metrics : altGlyph.metrics;

                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);


                #region Handle Kerning

                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                float characterSpacingAdjustment = m_characterSpacing;
                if (m_enableKerning && m_textElementType == TMP_TextElementType.Character)
                {
                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_cached_TextElement.m_GlyphIndex;

                    if (m_characterCount < totalCharacterCount - 1 &&
                        m_textInfo.characterInfo[m_characterCount + 1].elementType == TMP_TextElementType.Character)
                    {
                        uint nextGlyphIndex = m_textInfo.characterInfo[m_characterCount + 1].textElement.m_GlyphIndex;
                        uint key = nextGlyphIndex << 16 | baseGlyphIndex;

                        if (m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key,
                                out adjustmentPair))
                        {
                            glyphAdjustments = adjustmentPair.firstAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment =
                                (adjustmentPair.featureLookupFlags & UnityEngine.TextCore.LowLevel
                                    .FontFeatureLookupFlags.IgnoreSpacingAdjustments) ==
                                UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments
                                    ? 0
                                    : characterSpacingAdjustment;
                        }
                    }

                    if (m_characterCount >= 1)
                    {
                        uint previousGlyphIndex =
                            m_textInfo.characterInfo[m_characterCount - 1].textElement.m_GlyphIndex;
                        uint key = baseGlyphIndex << 16 | previousGlyphIndex;

                        if (textInfo.characterInfo[m_characterCount - 1].elementType == TMP_TextElementType.Character &&
                            m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key,
                                out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment =
                                (adjustmentPair.featureLookupFlags & UnityEngine.TextCore.LowLevel
                                    .FontFeatureLookupFlags.IgnoreSpacingAdjustments) ==
                                UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments
                                    ? 0
                                    : characterSpacingAdjustment;
                        }
                    }

                    m_internalCharacterInfo[m_characterCount].adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
                }

                #endregion


                #region Handle Diacritical Marks

                bool isBaseGlyph = TMP_TextParsingUtilities.IsBaseGlyph((uint)charCode);

                if (isBaseGlyph)
                    m_LastBaseGlyphIndex = m_characterCount;

                if (m_characterCount > 0 && !isBaseGlyph)
                {
                    if (m_LastBaseGlyphIndex != int.MinValue && m_LastBaseGlyphIndex == m_characterCount - 1)
                    {
                        Glyph baseGlyph = m_textInfo.characterInfo[m_LastBaseGlyphIndex].textElement.glyph;
                        uint baseGlyphIndex = baseGlyph.index;
                        uint markGlyphIndex = m_cached_TextElement.glyphIndex;
                        uint key = markGlyphIndex << 16 | baseGlyphIndex;

                        if (m_currentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key,
                                out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                        {
                            float advanceOffset = (m_internalCharacterInfo[m_LastBaseGlyphIndex].origin - m_xAdvance) /
                                                  currentElementScale;

                            glyphAdjustments.xPlacement = advanceOffset +
                                                          glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate -
                                                          glyphAdjustmentRecord.markPositionAdjustment
                                                              .xPositionAdjustment;
                            glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate -
                                                          glyphAdjustmentRecord.markPositionAdjustment
                                                              .yPositionAdjustment;

                            characterSpacingAdjustment = 0;
                        }
                    }
                    else
                    {
                        bool wasLookupApplied = false;

                        for (int characterLookupIndex = m_characterCount - 1;
                             characterLookupIndex >= 0 && characterLookupIndex != m_LastBaseGlyphIndex;
                             characterLookupIndex--)
                        {
                            Glyph baseMarkGlyph = m_textInfo.characterInfo[characterLookupIndex].textElement.glyph;
                            uint baseGlyphIndex = baseMarkGlyph.index;
                            uint combiningMarkGlyphIndex = m_cached_TextElement.glyphIndex;
                            uint key = combiningMarkGlyphIndex << 16 | baseGlyphIndex;

                            if (m_currentFontAsset.fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.TryGetValue(key,
                                    out MarkToMarkAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float baseMarkOrigin =
                                    (m_textInfo.characterInfo[characterLookupIndex].origin - m_xAdvance) /
                                    currentElementScale;
                                float currentBaseline = baselineOffset - m_lineOffset + m_baselineOffset;
                                float baseMarkBaseline =
                                    (m_internalCharacterInfo[characterLookupIndex].baseLine - currentBaseline) /
                                    currentElementScale;

                                glyphAdjustments.xPlacement = baseMarkOrigin +
                                    glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord
                                        .combiningMarkPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = baseMarkBaseline +
                                    glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord
                                        .combiningMarkPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                                wasLookupApplied = true;
                                break;
                            }
                        }

                        if (m_LastBaseGlyphIndex != int.MinValue && !wasLookupApplied)
                        {
                            Glyph baseGlyph = m_textInfo.characterInfo[m_LastBaseGlyphIndex].textElement.glyph;
                            uint baseGlyphIndex = baseGlyph.index;
                            uint markGlyphIndex = m_cached_TextElement.glyphIndex;
                            uint key = markGlyphIndex << 16 | baseGlyphIndex;

                            if (m_currentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key,
                                    out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float advanceOffset =
                                    (m_internalCharacterInfo[m_LastBaseGlyphIndex].origin - m_xAdvance) /
                                    currentElementScale;

                                glyphAdjustments.xPlacement = advanceOffset +
                                    glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord
                                        .markPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate -
                                                              glyphAdjustmentRecord.markPositionAdjustment
                                                                  .yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                            }
                        }
                    }
                }

                elementAscentLine += glyphAdjustments.yPlacement;
                elementDescentLine += glyphAdjustments.yPlacement;

                #endregion


                #region Handle Right-to-Left

                #endregion


                #region Handle Mono Spacing

                float monoAdvance = 0;
                if (m_monoSpacing != 0)
                {
                    monoAdvance =
                        (m_monoSpacing / 2 -
                         (m_cached_TextElement.glyph.metrics.width / 2 +
                          m_cached_TextElement.glyph.metrics.horizontalBearingX) * currentElementScale) *
                        (1 - m_charWidthAdjDelta);
                    m_xAdvance += monoAdvance;
                }

                #endregion


                #region Handle Style Padding

                float boldSpacingAdjustment = 0;
                if (m_textElementType == TMP_TextElementType.Character && !isUsingAltTypeface &&
                    ((m_FontStyleInternal & FontStyles.Bold) ==
                     FontStyles.Bold))
                    boldSpacingAdjustment = m_currentFontAsset.boldSpacing;

                #endregion Handle Style Padding

                m_internalCharacterInfo[m_characterCount].origin =
                    m_xAdvance + glyphAdjustments.xPlacement * currentElementScale;
                m_internalCharacterInfo[m_characterCount].baseLine =
                    (baselineOffset - m_lineOffset + m_baselineOffset) +
                    glyphAdjustments.yPlacement * currentElementScale;

                #region Compute Ascender & Descender values

                float elementAscender = m_textElementType == TMP_TextElementType.Character
                    ? elementAscentLine * currentElementScale / smallCapsMultiplier + m_baselineOffset
                    : elementAscentLine * currentElementScale + m_baselineOffset;

                float elementDescender = m_textElementType == TMP_TextElementType.Character
                    ? elementDescentLine * currentElementScale / smallCapsMultiplier + m_baselineOffset
                    : elementDescentLine * currentElementScale + m_baselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                bool isFirstCharacterOfLine = m_characterCount == m_firstCharacterOfLine;
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    if (m_baselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max((elementAscender - m_baselineOffset) / m_fontScaleMultiplier,
                            adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_baselineOffset) / m_fontScaleMultiplier,
                            adjustedDescender);
                    }

                    m_maxLineAscender = Mathf.Max(adjustedAscender, m_maxLineAscender);
                    m_maxLineDescender = Mathf.Min(adjustedDescender, m_maxLineDescender);
                }

                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    m_internalCharacterInfo[m_characterCount].adjustedAscender = adjustedAscender;
                    m_internalCharacterInfo[m_characterCount].adjustedDescender = adjustedDescender;

                    m_ElementAscender = m_internalCharacterInfo[m_characterCount].ascender =
                        elementAscender - m_lineOffset;
                    m_ElementDescender = m_internalCharacterInfo[m_characterCount].descender =
                        elementDescender - m_lineOffset;
                }
                else
                {
                    m_internalCharacterInfo[m_characterCount].adjustedAscender = m_maxLineAscender;
                    m_internalCharacterInfo[m_characterCount].adjustedDescender = m_maxLineDescender;

                    m_ElementAscender = m_internalCharacterInfo[m_characterCount].ascender =
                        m_maxLineAscender - m_lineOffset;
                    m_ElementDescender = m_internalCharacterInfo[m_characterCount].descender =
                        m_maxLineDescender - m_lineOffset;
                }

                if (m_lineNumber == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight,
                            m_currentFontAsset.m_FaceInfo.capLine * currentElementScale / smallCapsMultiplier);
                    }
                }
                
                #endregion

                bool isJustifiedOrFlush =
                    (m_lineJustification & HorizontalAlignmentOptions.Flush) == HorizontalAlignmentOptions.Flush ||
                    (m_lineJustification & HorizontalAlignmentOptions.Justified) ==
                    HorizontalAlignmentOptions.Justified;

                #region Handle Visible Characters

                if (charCode == 9 ||
                    ((textWrapMode == TextWrappingModes.PreserveWhitespace ||
                      textWrapMode == TextWrappingModes.PreserveWhitespaceNoWrap) &&
                     (isWhiteSpace || charCode == 0x200B)) ||
                    (isWhiteSpace == false && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) ||
                    (charCode == 0xAD && isSoftHyphenIgnored == false) ||
                    m_textElementType == TMP_TextElementType.Sprite)
                {
                    widthOfTextArea = m_width != -1
                        ? Mathf.Min(marginWidth + 0.0001f - m_marginLeft - m_marginRight, m_width)
                        : marginWidth + 0.0001f - m_marginLeft - m_marginRight;

                    textWidth = Mathf.Abs(m_xAdvance) + currentGlyphMetrics.horizontalAdvance *
                        (1 - m_charWidthAdjDelta) *
                        (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);

                    int testedCharacterCount = m_characterCount;

                    #region Current Line Horizontal Bounds Check

                    if (isBaseGlyph && textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        if (textWrapMode != TextWrappingModes.NoWrap &&
                            textWrapMode != TextWrappingModes.PreserveWhitespaceNoWrap &&
                            m_characterCount != m_firstCharacterOfLine)
                        {
                            i = RestoreWordWrappingState(ref internalWordWrapState);

                            #region Handle Soft Hyphenation

                            if (m_internalCharacterInfo[m_characterCount - 1].character == 0xAD &&
                                isSoftHyphenIgnored == false && m_overflowMode == TextOverflowModes.Overflow)
                            {
                                characterToSubstitute.index = m_characterCount - 1;
                                characterToSubstitute.unicode = 0x2D;

                                i -= 1;
                                m_characterCount -= 1;
                                continue;
                            }

                            isSoftHyphenIgnored = false;

                            if (m_internalCharacterInfo[m_characterCount].character == 0xAD)
                            {
                                isSoftHyphenIgnored = true;
                                continue;
                            }

                            #endregion

                            #region Handle Text Auto Size (if word wrapping is no longer possible)

                            if (isTextAutoSizingEnabled && isFirstWordOfLine)
                            {
                                #region Character Width Adjustments

                                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100 &&
                                    m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    if (m_charWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) *
                                        (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                    return Vector2.zero;
                                }

                                #endregion

                                #region Text Auto-Sizing (Text greater than vertical bounds)

                                if (fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_maxFontSize = fontSize;

                                    float sizeDelta = Mathf.Max((fontSize - m_minFontSize) / 2, 0.05f);
                                    fontSize -= sizeDelta;
                                    fontSize = Mathf.Max((int)(fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                    return Vector2.zero;
                                }

                                #endregion Text Auto-Sizing
                            }

                            #endregion

                            float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                            if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f &&
                                m_IsDrivenLineSpacing == false)
                            {
                                m_ElementDescender -= baselineAdjustmentDelta;
                                m_lineOffset += baselineAdjustmentDelta;
                            }

                            float lineAscender = m_maxLineAscender - m_lineOffset;
                            float lineDescender = m_maxLineDescender - m_lineOffset;

                            m_ElementDescender =
                                m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
                            if (!isMaxVisibleDescenderSet)
                                maxVisibleDescender = m_ElementDescender;

                            if (m_useMaxVisibleDescender && (m_characterCount >= m_maxVisibleCharacters ||
                                                             m_lineNumber >= m_maxVisibleLines))
                                isMaxVisibleDescenderSet = true;

                            m_firstCharacterOfLine = m_characterCount;
                            m_lineVisibleCharacterCount = 0;

                            SaveWordWrappingState(ref internalLineState, i, m_characterCount - 1);

                            m_lineNumber += 1;

                            float ascender = m_internalCharacterInfo[m_characterCount].adjustedAscender;

                            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                            {
                                m_lineOffset += 0 - m_maxLineDescender + ascender +
                                                (lineGap + m_lineSpacingDelta) * baseScale +
                                                m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = false;
                            }
                            else
                            {
                                m_lineOffset += m_lineHeight + m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            m_maxLineAscender = k_LargeNegativeFloat;
                            m_maxLineDescender = k_LargePositiveFloat;
                            m_startOfLineAscender = ascender;

                            m_xAdvance = 0 + tag_Indent;
                            isFirstWordOfLine = true;
                            continue;
                        }
                    }

                    #endregion

                    m_RenderedWidth = Mathf.Max(m_RenderedWidth, textWidth + m_marginLeft + m_marginRight);
                    m_RenderedHeight = Mathf.Max(m_RenderedHeight, m_maxTextAscender - m_ElementDescender);
                }

                #endregion Handle Visible Characters


                #region Adjust Line Spacing

                if (m_lineOffset > 0 && !TMP_Math.Approximately(m_maxLineAscender, m_startOfLineAscender) &&
                    m_IsDrivenLineSpacing == false)
                {
                    float offsetDelta = m_maxLineAscender - m_startOfLineAscender;
                    m_ElementDescender -= offsetDelta;
                    m_lineOffset += offsetDelta;

                    m_startOfLineAscender += offsetDelta;
                    internalWordWrapState.lineOffset = m_lineOffset;
                    internalWordWrapState.startOfLineAscender = m_startOfLineAscender;
                }

                #endregion


                #region XAdvance, Tabulation & Stops

                if (charCode == 9)
                {
                    float tabSize = m_currentFontAsset.faceInfo.tabWidth * m_currentFontAsset.tabSize *
                                    currentElementScale;
                    float tabs = Mathf.Ceil(m_xAdvance / tabSize) * tabSize;
                    m_xAdvance = tabs > m_xAdvance ? tabs : m_xAdvance + tabSize;
                }
                else if (m_monoSpacing != 0)
                {
                    m_xAdvance +=
                        (m_monoSpacing - monoAdvance +
                         ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale) +
                         m_cSpacing) * (1 - m_charWidthAdjDelta);

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }
                else
                {
                    m_xAdvance +=
                        ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) *
                         currentElementScale +
                         (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment +
                          boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }

                #endregion Tabulation & Stops


                #region Carriage Return

                if (charCode == 13)
                {
                    m_xAdvance = 0 + tag_Indent;
                }

                #endregion Carriage Return


                #region Check for Line Feed and Last Character

                if (charCode == 10 || charCode == 11 || charCode == 0x03 || charCode == 0x2028 || charCode == 0x2029 ||
                    m_characterCount == totalCharacterCount - 1)
                {
                    float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                    if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f &&
                        m_IsDrivenLineSpacing == false)
                    {
                        m_ElementDescender -= baselineAdjustmentDelta;
                        m_lineOffset += baselineAdjustmentDelta;
                    }

                    float lineDescender = m_maxLineDescender - m_lineOffset;

                    m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;

                    if (charCode == 10 || charCode == 11 || (charCode == 0x2D && isInjectedCharacter) ||
                        charCode == 0x2028 || charCode == 0x2029)
                    {
                        SaveWordWrappingState(ref internalLineState, i, m_characterCount);
                        SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);

                        m_lineNumber += 1;
                        m_firstCharacterOfLine = m_characterCount + 1;

                        float ascender = m_internalCharacterInfo[m_characterCount].adjustedAscender;

                        if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                        {
                            float lineOffsetDelta = 0 - m_maxLineDescender + ascender +
                                                    (lineGap + m_lineSpacingDelta) * baseScale +
                                                    (m_lineSpacing + (charCode == 10 || charCode == 0x2029
                                                        ? m_paragraphSpacing
                                                        : 0)) * currentEmScale;
                            m_lineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_lineOffset += m_lineHeight +
                                            (m_lineSpacing + (charCode == 10 || charCode == 0x2029
                                                ? m_paragraphSpacing
                                                : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_maxLineAscender = k_LargeNegativeFloat;
                        m_maxLineDescender = k_LargePositiveFloat;
                        m_startOfLineAscender = ascender;

                        m_xAdvance = 0 + tag_LineIndent + tag_Indent;

                        m_characterCount += 1;
                        continue;
                    }

                    if (charCode == 0x03)
                        i = m_TextProcessingArray.Length;
                }

                #endregion Check for Linefeed or Last Character


                #region Save Word Wrapping State

                if ((textWrapMode != TextWrappingModes.NoWrap &&
                     textWrapMode != TextWrappingModes.PreserveWhitespaceNoWrap) ||
                    m_overflowMode == TextOverflowModes.Truncate || m_overflowMode == TextOverflowModes.Ellipsis)
                {
                    bool shouldSaveHardLineBreak = false;
                    bool shouldSaveSoftLineBreak = false;

                    if ((isWhiteSpace || charCode == 0x200B || charCode == 0x2D || charCode == 0xAD) &&
                        (!m_isNonBreakingSpace || ignoreNonBreakingSpace) && charCode != 0xA0 && charCode != 0x2007 &&
                        charCode != 0x2011 && charCode != 0x202F && charCode != 0x2060)
                    {
                        if (!(charCode == 0x2D && m_characterCount > 0 &&
                              char.IsWhiteSpace(m_textInfo.characterInfo[m_characterCount - 1].character) &&
                              m_textInfo.characterInfo[m_characterCount - 1].lineNumber == m_lineNumber))
                        {
                            isFirstWordOfLine = false;
                            shouldSaveHardLineBreak = true;

                            internalSoftLineBreak.previous_WordBreak = -1;
                        }
                    }
                    else if (m_isNonBreakingSpace == false &&
                             (TMP_TextParsingUtilities.IsHangul(charCode) &&
                              TMP_Settings.useModernHangulLineBreakingRules == false ||
                              TMP_TextParsingUtilities.IsCJK(charCode)))
                    {
                        bool isCurrentLeadingCharacter =
                            TMP_Settings.linebreakingRules.leadingCharacters.Contains(charCode);
                        bool isNextFollowingCharacter = m_characterCount < totalCharacterCount - 1 &&
                                                        TMP_Settings.linebreakingRules.followingCharacters.Contains(
                                                            m_internalCharacterInfo[m_characterCount + 1].character);

                        if (isCurrentLeadingCharacter == false)
                        {
                            if (isNextFollowingCharacter == false)
                            {
                                isFirstWordOfLine = false;
                                shouldSaveHardLineBreak = true;
                            }

                            if (isFirstWordOfLine)
                            {
                                if (isWhiteSpace)
                                    shouldSaveSoftLineBreak = true;

                                shouldSaveHardLineBreak = true;
                            }
                        }
                        else
                        {
                            if (isFirstWordOfLine && isFirstCharacterOfLine)
                            {
                                if (isWhiteSpace)
                                    shouldSaveSoftLineBreak = true;

                                shouldSaveHardLineBreak = true;
                            }
                        }
                    }
                    else if (m_isNonBreakingSpace == false && m_characterCount + 1 < totalCharacterCount &&
                             TMP_TextParsingUtilities.IsCJK(m_textInfo.characterInfo[m_characterCount + 1].character))
                    {
                        shouldSaveHardLineBreak = true;
                    }
                    else if (isFirstWordOfLine)
                    {
                        if (isWhiteSpace && charCode != 0xA0 || (charCode == 0xAD && isSoftHyphenIgnored == false))
                            shouldSaveSoftLineBreak = true;

                        shouldSaveHardLineBreak = true;
                    }

                    if (shouldSaveHardLineBreak)
                        SaveWordWrappingState(ref internalWordWrapState, i, m_characterCount);

                    if (shouldSaveSoftLineBreak)
                        SaveWordWrappingState(ref internalSoftLineBreak, i, m_characterCount);
                }

                #endregion Save Word Wrapping State

                m_characterCount += 1;
            }

            #region Check Auto-Sizing (Upper Font Size Bounds)

            fontSizeDelta = m_maxFontSize - m_minFontSize;
            if (isTextAutoSizingEnabled && fontSizeDelta > 0.051f && fontSize < m_fontSizeMax &&
                m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100)
                    m_charWidthAdjDelta = 0;

                m_minFontSize = fontSize;

                float sizeDelta = Mathf.Max((m_maxFontSize - fontSize) / 2, 0.05f);
                fontSize += sizeDelta;
                fontSize = Mathf.Min((int)(fontSize * 20 + 0.5f) / 20f, m_fontSizeMax);

                return Vector2.zero;
            }

            #endregion End Auto-sizing Check

            m_IsAutoSizePointSizeSet = true;

            m_isCalculatingPreferredValues = false;

            m_RenderedWidth += m_margin.x > 0 ? m_margin.x : 0;
            m_RenderedWidth += m_margin.z > 0 ? m_margin.z : 0;

            m_RenderedHeight += m_margin.y > 0 ? m_margin.y : 0;
            m_RenderedHeight += m_margin.w > 0 ? m_margin.w : 0;

            m_RenderedWidth = (int)(m_RenderedWidth * 100 + 1f) / 100f;
            m_RenderedHeight = (int)(m_RenderedHeight * 100 + 1f) / 100f;

            return new Vector2(m_RenderedWidth, m_RenderedHeight);
        }

        /// <summary>
        /// Method returning the compound bounds of the text object and child sub objects.
        /// </summary>
        /// <returns></returns>
        protected virtual Bounds GetCompoundBounds()
        {
            return new Bounds();
        }

        /// <summary>
        /// Method which returns the bounds of the text object;
        /// </summary>
        /// <returns></returns>
        protected Bounds GetTextBounds()
        {
            if (m_textInfo == null || m_textInfo.characterCount > m_textInfo.characterInfo.Length) return new Bounds();

            Extents extent = new Extents(k_LargePositiveVector2, k_LargeNegativeVector2);

            for (int i = 0; i < m_textInfo.characterCount && i < m_textInfo.characterInfo.Length; i++)
            {
                if (!m_textInfo.characterInfo[i].isVisible)
                    continue;

                extent.min.x = Mathf.Min(extent.min.x, m_textInfo.characterInfo[i].origin);
                extent.min.y = Mathf.Min(extent.min.y, m_textInfo.characterInfo[i].descender);

                extent.max.x = Mathf.Max(extent.max.x, m_textInfo.characterInfo[i].xAdvance);
                extent.max.y = Mathf.Max(extent.max.y, m_textInfo.characterInfo[i].ascender);
            }

            Vector2 size;
            size.x = extent.max.x - extent.min.x;
            size.y = extent.max.y - extent.min.y;

            Vector3 center = (extent.min + extent.max) / 2;

            return new Bounds(center, size);
        }

        /// <summary>
        /// Method which returns the bounds of the text object;
        /// </summary>
        /// <param name="onlyVisibleCharacters"></param>
        /// <returns></returns>
        protected Bounds GetTextBounds(bool onlyVisibleCharacters)
        {
            if (m_textInfo == null) return new Bounds();

            Extents extent = new Extents(k_LargePositiveVector2, k_LargeNegativeVector2);

            for (int i = 0; i < m_textInfo.characterCount; i++)
            {
                if ((i > maxVisibleCharacters || m_textInfo.characterInfo[i].lineNumber > m_maxVisibleLines) &&
                    onlyVisibleCharacters)
                    break;

                if (onlyVisibleCharacters && !m_textInfo.characterInfo[i].isVisible)
                    continue;

                extent.min.x = Mathf.Min(extent.min.x, m_textInfo.characterInfo[i].origin);
                extent.min.y = Mathf.Min(extent.min.y, m_textInfo.characterInfo[i].descender);

                extent.max.x = Mathf.Max(extent.max.x, m_textInfo.characterInfo[i].xAdvance);
                extent.max.y = Mathf.Max(extent.max.y, m_textInfo.characterInfo[i].ascender);
            }

            Vector2 size;
            size.x = extent.max.x - extent.min.x;
            size.y = extent.max.y - extent.min.y;

            Vector2 center = (extent.min + extent.max) / 2;

            return new Bounds(center, size);
        }

        /// <summary>
        /// Method to adjust line spacing as a result of using different fonts or font point size.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="offset"></param>
        protected void AdjustLineOffset(int startIndex, int endIndex, float offset)
        {
            Vector3 vertexOffset = new Vector3(0, offset, 0);

            for (int i = startIndex; i <= endIndex; i++)
            {
                m_textInfo.characterInfo[i].bottomLeft -= vertexOffset;
                m_textInfo.characterInfo[i].topLeft -= vertexOffset;
                m_textInfo.characterInfo[i].topRight -= vertexOffset;
                m_textInfo.characterInfo[i].bottomRight -= vertexOffset;

                m_textInfo.characterInfo[i].ascender -= vertexOffset.y;
                m_textInfo.characterInfo[i].baseLine -= vertexOffset.y;
                m_textInfo.characterInfo[i].descender -= vertexOffset.y;

                if (m_textInfo.characterInfo[i].isVisible)
                {
                    m_textInfo.characterInfo[i].vertex_BL.position -= vertexOffset;
                    m_textInfo.characterInfo[i].vertex_TL.position -= vertexOffset;
                    m_textInfo.characterInfo[i].vertex_TR.position -= vertexOffset;
                    m_textInfo.characterInfo[i].vertex_BR.position -= vertexOffset;
                }
            }
        }

        /// <summary>
        /// Function to increase the size of the Line Extents Array.
        /// </summary>
        /// <param name="size"></param>
        protected void ResizeLineExtents(int size)
        {
            size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size + 1);

            TMP_LineInfo[] temp_lineInfo = new TMP_LineInfo[size];
            for (int i = 0; i < size; i++)
            {
                if (i < m_textInfo.lineInfo.Length)
                    temp_lineInfo[i] = m_textInfo.lineInfo[i];
                else
                {
                    temp_lineInfo[i].lineExtents.min = k_LargePositiveVector2;
                    temp_lineInfo[i].lineExtents.max = k_LargeNegativeVector2;

                    temp_lineInfo[i].ascender = k_LargeNegativeFloat;
                    temp_lineInfo[i].descender = k_LargePositiveFloat;
                }
            }

            m_textInfo.lineInfo = temp_lineInfo;
        }

        protected static Vector2 k_LargePositiveVector2 = new Vector2(TMP_Math.INT_MAX, TMP_Math.INT_MAX);
        protected static Vector2 k_LargeNegativeVector2 = new Vector2(TMP_Math.INT_MIN, TMP_Math.INT_MIN);
        protected static float k_LargePositiveFloat = TMP_Math.FLOAT_MAX;
        protected static float k_LargeNegativeFloat = TMP_Math.FLOAT_MIN;

        /// <summary>
        /// Function to force an update of the margin size.
        /// </summary>
        public virtual void ComputeMarginSize()
        {
        }
    }
}