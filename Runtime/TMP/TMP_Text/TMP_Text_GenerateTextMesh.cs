using System;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace TMPro
{
    public abstract partial class TMP_Text
    {
        private static ProfilerMarker k_GenerateTextMarker = new("TMP.GenerateText");
        private static ProfilerMarker k_GenerateTextPhaseIMarker = new("TMP GenerateText - Phase I");
        private static ProfilerMarker k_ParseMarkupTextMarker = new("TMP Parse Markup Text");
        private static ProfilerMarker k_CharacterLookupMarker = new("TMP Lookup Character & Glyph Data");
        private static ProfilerMarker k_HandleGPOSFeaturesMarker = new("TMP Handle GPOS Features");
        private static ProfilerMarker k_CalculateVerticesPositionMarker = new("TMP Calculate Vertices Position");
        private static ProfilerMarker k_ComputeTextMetricsMarker = new("TMP Compute Text Metrics");
        private static ProfilerMarker k_HandleVisibleCharacterMarker = new("TMP Handle Visible Character");
        private static ProfilerMarker k_HandleWhiteSpacesMarker = new("TMP Handle White Space & Control Character");
        private static ProfilerMarker k_HandleHorizontalLineBreakingMarker = new("TMP Handle Horizontal Line Breaking");
        private static ProfilerMarker k_HandleVerticalLineBreakingMarker = new("TMP Handle Vertical Line Breaking");
        private static ProfilerMarker k_SaveGlyphVertexDataMarker = new("TMP Save Glyph Vertex Data");
        private static ProfilerMarker k_ComputeCharacterAdvanceMarker = new("TMP Compute Character Advance");
        private static ProfilerMarker k_HandleCarriageReturnMarker = new("TMP Handle Carriage Return");
        private static ProfilerMarker k_HandleLineTerminationMarker = new("TMP Handle Line Termination");
        private static ProfilerMarker k_SaveTextExtentMarker = new("TMP Save Text Extent");
        private static ProfilerMarker k_SaveProcessingStatesMarker = new("TMP Save Processing States");
        private static ProfilerMarker k_GenerateTextPhaseIIMarker = new("TMP GenerateText - Phase II");
        private static ProfilerMarker k_GenerateTextPhaseIIIMarker = new("TMP GenerateText - Phase III");
        
        protected TMP_SubMeshUI[] m_subTextObjects = new TMP_SubMeshUI[8];

        protected float m_previousLossyScaleY = -1;
        protected Vector3[] m_RectTransformCorners = new Vector3[4];
        protected CanvasRenderer m_canvasRenderer;
        protected Canvas m_canvas;
        protected float m_CanvasScaleFactor;
        protected bool m_ShouldUpdateCulling;
        
        private string preprocessedText = string.Empty;
        private TextRenderFlags lastRenderMode;
        protected bool isBidiProcessing;
        protected Bidi.Direction[] directions;
        
        public new CanvasRenderer canvasRenderer
        {
            get
            {
                if (m_canvasRenderer == null) m_canvasRenderer = GetComponent<CanvasRenderer>();

                return m_canvasRenderer;
            }
        }
        
        protected void OnBeforeMeshRender()
        {
            if(isBidiProcessing) return;
            lastRenderMode = m_renderMode;
            m_renderMode = TextRenderFlags.DontRender;
        }

        protected void OnAfterMeshRender()
        {
            if(isBidiProcessing) return;
            var input = Bidi.Do(this, out var logicalToVisualMap, out directions);
            if (logicalToVisualMap != null)
            { 
                Debug.Log(string.Join(',', logicalToVisualMap.SelectMany(x => x)));
            }
            preprocessedText = input;
            m_renderMode = lastRenderMode;
            isBidiProcessing = true;
            ForceMeshUpdate();
            isBidiProcessing = false;
        }
        
        protected void OnPreRenderCanvas()
        {
            if (!m_isAwake || (!IsActive() && !m_ignoreActiveState))
                return;

            if (m_canvas == null) { m_canvas = canvas; if (m_canvas == null) return; }


            if (_havePropertiesChanged || m_isLayoutDirty)
            {
                if (m_fontAsset == null)
                {
                    Debug.LogWarning("Please assign a Font Asset to this " + transform.name + " gameobject.", this);
                    return;
                }

                OnBeforeMeshRender();
                if (checkPaddingRequired)
                    UpdateMeshPadding();
                
                ParseInputText();
                TMP_FontAsset.UpdateFontAssetsInUpdateQueue();

                if (m_enableAutoSizing)
                    m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax);

                m_maxFontSize = m_fontSizeMax;
                m_minFontSize = m_fontSizeMin;
                m_lineSpacingDelta = 0;
                m_charWidthAdjDelta = 0;

                m_isTextTruncated = false;

                _havePropertiesChanged = false;
                m_isLayoutDirty = false;
                m_ignoreActiveState = false;

                m_IsAutoSizePointSizeSet = false;
                m_AutoSizeIterationCount = 0;

                while (!m_IsAutoSizePointSizeSet)
                {
                    GenerateTextMesh();
                    m_AutoSizeIterationCount += 1;
                }
                OnAfterMeshRender();
                SetTextBounds();
            }
        }
        
        private TextBackingContainer m_TextBackingArray = new(4);
        
        protected void ParseInputText()
        {
            Debug.Log($"{GetInstanceID()} ParseInputText Parsing...");
            k_ParseTextMarker.Begin();

            var input = m_text;
            
            if (!isBidiProcessing)
            {
                if (m_TextPreprocessor != null)
                {
                    input = m_TextPreprocessor.PreprocessText(input);
                    PreprocessedText = input;
                }

                input = Normalize();
                input = ArabicShaper.Do(input, out _);
                preprocessedText = input;
                PopulateTextArrays(input);
            }
            else
            {
                PopulateTextArrays(preprocessedText);
            }

            k_ParseTextMarker.End();
            
            string Normalize()
            {
                StringBuilder sb = null;

                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];

                    if (c == '\r' ||
                        (c == '<' &&
                         i + 3 < input.Length &&
                         input[i + 1] == 'b' &&
                         input[i + 2] == 'r' &&
                         input[i + 3] == '>'))
                    {
                        if (sb == null)
                        {
                            sb = new(input.Length);
                            sb.Append(input, 0, i);
                        }

                        if (c == '\r')
                        {
                            if (i + 1 < input.Length && input[i + 1] == '\n')
                                i++;

                            sb.Append('\n');
                        }
                        else
                        {
                            sb.Append('\n');
                            i += 3;
                        }
                    }
                    else
                    {
                        sb?.Append(c);
                    }
                }

                return sb?.ToString() ?? input;
            }
        }

        private void PopulateTextArrays(string input)
        {
            PopulateTextBackingArray(input);
            PopulateTextProcessingArray();
            SetArraySizes(m_TextProcessingArray);
        }
        
        /// <param name="unicodeChars"></param>
        /// <returns></returns>
        internal virtual int SetArraySizes(TextProcessingElement[] unicodeChars) { return 0; }

        /// <param name="sourceText">Source text to be converted</param>
        private void PopulateTextBackingArray(string sourceText)
        {
            int srcLength = sourceText?.Length ?? 0;

            PopulateTextBackingArray(sourceText, 0, srcLength);
        }

        /// <param name="sourceText">string containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        private void PopulateTextBackingArray(string sourceText, int start, int length)
        {
            int readIndex;
            int writeIndex = 0;

            if (sourceText == null)
            {
                readIndex = 0;
                length = 0;
            }
            else
            {
                readIndex = Mathf.Clamp(start, 0, sourceText.Length);
                length = Mathf.Clamp(length, 0, start + length < sourceText.Length ? length : sourceText.Length - start);
            }

            if (length >= m_TextBackingArray.Capacity)
                m_TextBackingArray.Resize((length));

            int end = readIndex + length;
            for (; readIndex < end; readIndex++)
            {
                m_TextBackingArray[writeIndex] = sourceText[readIndex];
                writeIndex += 1;
            }

            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;
        }
        

        private void PopulateTextProcessingArray()
        {
            TMP_TextProcessingStack<int>.SetDefault(m_TextStyleStacks, 0);

            int srcLength = m_TextBackingArray.Count;
            int requiredCapacity = srcLength + (textStyle.styleOpeningDefinition?.Length ?? 0);
            if (m_TextProcessingArray.Length < requiredCapacity)
                ResizeInternalArray(ref m_TextProcessingArray, requiredCapacity);

            m_TextStyleStackDepth = 0;
            int writeIndex = 0;

            if (textStyle.hashCode != (int)MarkupTag.NORMAL)
                InsertOpeningStyleTag(m_TextStyle, ref m_TextProcessingArray, ref writeIndex);

            tag_NoParsing = false;

            int readIndex = 0;
            for (; readIndex < srcLength; readIndex++)
            {
                uint c = m_TextBackingArray[readIndex];

                if (c == 0)
                    break;

                if (c == '\\' && readIndex < srcLength - 1)
                {
                    switch (m_TextBackingArray[readIndex + 1])
                    {
                        case 92:
                            if (!m_parseCtrlCharacters) break;

                            readIndex += 1;
                            break;
                        case 110:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 10 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 114:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 13 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 116:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 9 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 118:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 11 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 117:
                            if (srcLength > readIndex + 5 && IsValidUTF16(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = GetUTF16(m_TextBackingArray, readIndex + 2) };
                                readIndex += 5;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                        case 85:
                            if (srcLength > readIndex + 9 && IsValidUTF32(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 10, unicode = GetUTF32(m_TextBackingArray, readIndex + 2) };
                                readIndex += 9;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                    }
                }

                if (c >= CodePoint.HIGH_SURROGATE_START && c <= CodePoint.HIGH_SURROGATE_END && srcLength > readIndex + 1 && m_TextBackingArray[readIndex + 1] >= CodePoint.LOW_SURROGATE_START && m_TextBackingArray[readIndex + 1] <= CodePoint.LOW_SURROGATE_END)
                {
                    m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 2, unicode = TMP_TextParsingUtilities.ConvertToUTF32(c, m_TextBackingArray[readIndex + 1]) };
                    readIndex += 1;
                    writeIndex += 1;
                    continue;
                }

                if (c == '<' && m_isRichText)
                {
                    int hashCode = GetMarkupTagHashCode(m_TextBackingArray, readIndex + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.NO_PARSE:
                            tag_NoParsing = true;
                            break;
                        case MarkupTag.SLASH_NO_PARSE:
                            tag_NoParsing = false;
                            break;
                        case MarkupTag.BR:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 10 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.CR:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 13 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0xA0 };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0x200B };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWJ:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0x200D };
                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.SHY:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0xAD };
                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.A:
                            if (m_TextBackingArray.Count > readIndex + 4 && m_TextBackingArray[readIndex + 3] == 'h' && m_TextBackingArray[readIndex + 4] == 'r')
                                InsertOpeningTextStyle(GetStyle((int)MarkupTag.A), ref m_TextProcessingArray, ref writeIndex);
                            break;
                        case MarkupTag.STYLE:
                            if (tag_NoParsing) break;

                            int openWriteIndex = writeIndex;
                            if (ReplaceOpeningStyleTag(ref m_TextBackingArray, readIndex, out int srcOffset, ref m_TextProcessingArray, ref writeIndex))
                            {
                                for (; openWriteIndex < writeIndex; openWriteIndex++)
                                {
                                    m_TextProcessingArray[openWriteIndex].stringIndex = readIndex;
                                    m_TextProcessingArray[openWriteIndex].length = (srcOffset - readIndex) + 1;
                                }

                                readIndex = srcOffset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_A:
                            InsertClosingTextStyle(GetStyle((int)MarkupTag.A), ref m_TextProcessingArray, ref writeIndex);
                            break;
                        case MarkupTag.SLASH_STYLE:
                            if (tag_NoParsing) break;

                            int closeWriteIndex = writeIndex;
                            ReplaceClosingStyleTag(ref m_TextProcessingArray, ref writeIndex);

                            for (; closeWriteIndex < writeIndex; closeWriteIndex++)
                            {
                                m_TextProcessingArray[closeWriteIndex].stringIndex = readIndex;
                                m_TextProcessingArray[closeWriteIndex].length = 8;
                            }

                            readIndex += 7;
                            continue;
                    }
                }

                if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                m_TextProcessingArray[writeIndex] = new() { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = c };

                writeIndex += 1;
            }

            m_TextStyleStackDepth = 0;

            if (textStyle.hashCode != (int)MarkupTag.NORMAL)
                InsertClosingStyleTag(ref m_TextProcessingArray, ref writeIndex);

            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

            m_TextProcessingArray[writeIndex].unicode = 0;
            m_InternalTextProcessingArraySize = writeIndex;
        }
        
        protected virtual void GenerateTextMesh()
        {
            k_GenerateTextMarker.Begin();

            if (m_fontAsset == null || m_fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " + GetInstanceID());
                m_IsAutoSizePointSizeSet = true;
                k_GenerateTextMarker.End();
                return;
            }

            if (m_textInfo != null)
                m_textInfo.Clear();

            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == 0)
            {
                ClearMesh();
                
                TMPro_EventManager.ON_TEXT_CHANGED(this);
                m_IsAutoSizePointSizeSet = true;
                k_GenerateTextMarker.End();
                return;
            }

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;
            m_materialReferenceStack.SetDefault(new(m_currentMaterialIndex, m_currentFontAsset, m_currentMaterial, m_padding));

            int totalCharacterCount = m_totalCharacterCount;

            float baseScale = (m_fontSize / m_fontAsset.m_FaceInfo.pointSize * m_fontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
            float currentElementScale = baseScale;
            float currentEmScale = m_fontSize * 0.01f * (m_isOrthographic ? 1 : 0.1f);
            m_fontScaleMultiplier = 1;

            m_currentFontSize = m_fontSize;
            m_sizeStack.SetDefault(m_currentFontSize);
            float fontSizeDelta = 0;

            uint charCode = 0;

            m_FontStyleInternal = m_fontStyle;
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? FontWeight.Bold : m_fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);
            m_fontStyleStack.Clear();

            m_lineJustification = m_HorizontalAlignment;
            m_lineJustificationStack.SetDefault(m_lineJustification);

            float padding = 0;

            m_baselineOffset = 0;
            m_baselineOffsetStack.Clear();

            bool beginUnderline = false;
            Vector3 underline_start = Vector3.zero;
            Vector3 underline_end = Vector3.zero;

            bool beginStrikethrough = false;
            Vector3 strikethrough_start = Vector3.zero;
            Vector3 strikethrough_end = Vector3.zero;

            bool beginHighlight = false;
            Vector3 highlight_start = Vector3.zero;
            Vector3 highlight_end = Vector3.zero;

            m_fontColor32 = m_fontColor;
            m_htmlColor = m_fontColor32;
            m_underlineColor = m_htmlColor;
            m_strikethroughColor = m_htmlColor;

            m_colorStack.SetDefault(m_htmlColor);
            m_underlineColorStack.SetDefault(m_htmlColor);
            m_strikethroughColorStack.SetDefault(m_htmlColor);
            m_HighlightStateStack.SetDefault(new(m_htmlColor, TMP_Offset.zero));

            m_colorGradientPreset = null;
            m_colorGradientStack.SetDefault(null);

            m_ItalicAngle = m_currentFontAsset.italicStyle;
            m_ItalicAngleStack.SetDefault(m_ItalicAngle);

            m_actionStack.Clear();

            m_FXScale = Vector3.one;
            m_FXRotation = Quaternion.identity;

            m_lineOffset = 0;
            m_lineHeight = TMP_Math.FLOAT_UNSET;
            float lineGap = m_currentFontAsset.m_FaceInfo.lineHeight - (m_currentFontAsset.m_FaceInfo.ascentLine - m_currentFontAsset.m_FaceInfo.descentLine);

            m_cSpacing = 0;
            m_monoSpacing = 0;
            m_xAdvance = 0;

            tag_LineIndent = 0;
            tag_Indent = 0;
            m_indentStack.SetDefault(0);
            tag_NoParsing = false;

            m_characterCount = 0;

            m_firstCharacterOfLine = m_firstVisibleCharacter;
            m_lastCharacterOfLine = 0;
            m_firstVisibleCharacterOfLine = 0;
            m_lastVisibleCharacterOfLine = 0;
            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;
            m_lineNumber = 0;
            m_startOfLineAscender = 0;
            m_startOfLineDescender = 0;
            m_lineVisibleCharacterCount = 0;
            m_lineVisibleSpaceCount = 0;
            bool isStartOfNewLine = true;
            m_IsDrivenLineSpacing = false;
            m_firstOverflowCharacterIndex = -1;
            m_LastBaseGlyphIndex = int.MinValue;

            bool kerning = m_ActiveFontFeatures.Contains(OTL_FeatureTag.kern);
            bool markToBase = m_ActiveFontFeatures.Contains(OTL_FeatureTag.mark);
            bool markToMark = m_ActiveFontFeatures.Contains(OTL_FeatureTag.mkmk);

            Vector4 margins = m_margin;
            float marginWidth = m_marginWidth > 0 ? m_marginWidth : 0;
            float marginHeight = m_marginHeight > 0 ? m_marginHeight : 0;
            m_marginLeft = 0;
            m_marginRight = 0;
            m_width = -1;
            float widthOfTextArea = marginWidth + 0.0001f;

            m_meshExtents.min = k_LargePositiveVector2;
            m_meshExtents.max = k_LargeNegativeVector2;

            m_textInfo.ClearLineInfo();

            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            bool isFirstWordOfLine = true;
            m_isNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;
            int lastSoftLineBreak = 0;

            CharacterSubstitution characterToSubstitute = new(-1, 0);
            bool isSoftHyphenIgnored = false;

            SaveWordWrappingState(ref m_SavedWordWrapState, -1, -1);
            SaveWordWrappingState(ref m_SavedLineState, -1, -1);
            SaveWordWrappingState(ref m_SavedEllipsisState, -1, -1);
            SaveWordWrappingState(ref m_SavedLastValidState, -1, -1);
            SaveWordWrappingState(ref m_SavedSoftLineBreakState, -1, -1);

            m_EllipsisInsertionCandidateStack.Clear();

            int restoreCount = 0;

            k_GenerateTextPhaseIMarker.Begin();

            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                charCode = m_TextProcessingArray[i].unicode;

                if (restoreCount > 5)
                {
                    Debug.LogError("Line breaking recursion max threshold hit... Character [" + charCode + "] index: " + i);
                    characterToSubstitute.index = m_characterCount;
                    characterToSubstitute.unicode = 0x03;
                }

                if (charCode == 0x1A)
                    continue;

                #region Parse Rich Text Tag

                ref var chInfo = ref m_textInfo.characterInfo[m_characterCount];
                if (m_isRichText && charCode == '<')
                {
                    k_ParseMarkupTextMarker.Begin();

                    m_isTextLayoutPhase = true;

                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out var endTagIndex))
                    {
                        i = endTagIndex;
                        k_ParseMarkupTextMarker.End();
                        continue;
                    }
                    k_ParseMarkupTextMarker.End();
                }
                else
                {
                    m_currentMaterialIndex = chInfo.materialReferenceIndex;
                    m_currentFontAsset = chInfo.fontAsset;
                }
                #endregion End Parse Rich Text Tag

                int previousMaterialIndex = m_currentMaterialIndex;
                bool isUsingAltTypeface = chInfo.isUsingAlternateTypeface;

                m_isTextLayoutPhase = false;

                #region Character Substitutions
                bool isInjectedCharacter = false;

                if (characterToSubstitute.index == m_characterCount)
                {
                    charCode = characterToSubstitute.unicode;
                    isInjectedCharacter = true;

                    switch (charCode)
                    {
                        case 0x03:
                            chInfo.textElement = m_currentFontAsset.characterLookupTable[0x03];
                            m_isTextTruncated = true;
                            break;
                        case 0x2D:
                            break;
                        case 0x2026:
                            chInfo.textElement = m_Ellipsis.character;
                            chInfo.fontAsset = m_Ellipsis.fontAsset;
                            chInfo.material = m_Ellipsis.material;
                            chInfo.materialReferenceIndex = m_Ellipsis.materialIndex;

                            m_materialReferences[m_Underline.materialIndex].referenceCount += 1;

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
                    chInfo.isVisible = false;
                    chInfo.character = (char)0x200B;
                    chInfo.lineNumber = 0;
                    m_characterCount += 1;
                    continue;
                }
                #endregion


                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

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
                #endregion


                #region Look up Character Data
                k_CharacterLookupMarker.Begin();

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;

                    m_cached_TextElement = chInfo.textElement;
                    if (m_cached_TextElement == null)
                    {
                        k_CharacterLookupMarker.End();
                        continue;
                    }

                    m_currentFontAsset = chInfo.fontAsset;
                    m_currentMaterial = chInfo.material;
                    m_currentMaterialIndex = chInfo.materialReferenceIndex;

                    float adjustedScale;
                    if (isInjectedCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_characterCount != m_firstCharacterOfLine)
                        adjustedScale = m_textInfo.characterInfo[m_characterCount - 1].pointSize * smallCapsMultiplier / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_currentFontSize * smallCapsMultiplier / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);

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

                    currentElementScale = adjustedScale * m_fontScaleMultiplier * m_cached_TextElement.m_Scale * m_cached_TextElement.m_Glyph.scale;
                    baselineOffset = m_currentFontAsset.m_FaceInfo.baseline * adjustedScale * m_fontScaleMultiplier * m_currentFontAsset.m_FaceInfo.scale;

                    chInfo.scale = currentElementScale;

                    padding = m_currentMaterialIndex == 0 ? m_padding : m_subTextObjects[m_currentMaterialIndex].padding;
                
                k_CharacterLookupMarker.End();
                #endregion


                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == 0xAD || charCode == 0x03)
                    currentElementScale = 0;
                #endregion


                chInfo.character = (char)charCode;
                chInfo.pointSize = m_currentFontSize;
                chInfo.underlineColor = m_underlineColor;
                chInfo.strikethroughColor = m_strikethroughColor;
                chInfo.highlightState = m_HighlightState;
                chInfo.style = m_FontStyleInternal;

                Glyph altGlyph = chInfo.alternativeGlyph;
                GlyphMetrics currentGlyphMetrics = altGlyph == null ? m_cached_TextElement.m_Glyph.metrics : altGlyph.metrics;

                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                #region Handle Kerning
                GlyphValueRecord glyphAdjustments = new();
                float characterSpacingAdjustment = m_characterSpacing;
                if (kerning)
                {
                    k_HandleGPOSFeaturesMarker.Begin();

                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_cached_TextElement.m_GlyphIndex;

                    if (m_characterCount < totalCharacterCount - 1)
                    {
                        uint nextGlyphIndex = m_textInfo.characterInfo[m_characterCount + 1].textElement.m_GlyphIndex;
                        uint key = nextGlyphIndex << 16 | baseGlyphIndex;

                        if (m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments = adjustmentPair.firstAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments) == UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    if (m_characterCount >= 1)
                    {
                        uint previousGlyphIndex = m_textInfo.characterInfo[m_characterCount - 1].textElement.m_GlyphIndex;
                        uint key = baseGlyphIndex << 16 | previousGlyphIndex;

                        if (m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments) == UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    k_HandleGPOSFeaturesMarker.End();
                }

                chInfo.adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
                #endregion


                #region Handle Diacritical Marks
                bool isBaseGlyph = TMP_TextParsingUtilities.IsBaseGlyph(charCode);

                if (isBaseGlyph)
                    m_LastBaseGlyphIndex = m_characterCount;

                if (m_characterCount > 0 && !isBaseGlyph)
                {
                    if (markToBase && m_LastBaseGlyphIndex != int.MinValue && m_LastBaseGlyphIndex == m_characterCount - 1)
                    {
                        Glyph baseGlyph = m_textInfo.characterInfo[m_LastBaseGlyphIndex].textElement.glyph;
                        uint baseGlyphIndex = baseGlyph.index;
                        uint markGlyphIndex = m_cached_TextElement.glyphIndex;
                        uint key = markGlyphIndex << 16 | baseGlyphIndex;

                        if (m_currentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                        {
                            float advanceOffset = (m_textInfo.characterInfo[m_LastBaseGlyphIndex].origin - m_xAdvance) / currentElementScale;

                            glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                            glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                            characterSpacingAdjustment = 0;
                        }
                    }
                    else
                    {
                        bool wasLookupApplied = false;

                        if (markToMark)
                        {
                            for (int characterLookupIndex = m_characterCount - 1; characterLookupIndex >= 0 && characterLookupIndex != m_LastBaseGlyphIndex; characterLookupIndex--)
                            {
                                Glyph baseMarkGlyph = m_textInfo.characterInfo[characterLookupIndex].textElement.glyph;
                                uint baseGlyphIndex = baseMarkGlyph.index;
                                uint combiningMarkGlyphIndex = m_cached_TextElement.glyphIndex;
                                uint key = combiningMarkGlyphIndex << 16 | baseGlyphIndex;

                                if (m_currentFontAsset.fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.TryGetValue(key, out MarkToMarkAdjustmentRecord glyphAdjustmentRecord))
                                {
                                    float baseMarkOrigin = (m_textInfo.characterInfo[characterLookupIndex].origin - m_xAdvance) / currentElementScale;
                                    float currentBaseline = baselineOffset - m_lineOffset + m_baselineOffset;
                                    float baseMarkBaseline = (m_textInfo.characterInfo[characterLookupIndex].baseLine - currentBaseline) / currentElementScale;

                                    glyphAdjustments.xPlacement = baseMarkOrigin + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.xPositionAdjustment;
                                    glyphAdjustments.yPlacement = baseMarkBaseline + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.yPositionAdjustment;

                                    characterSpacingAdjustment = 0;
                                    wasLookupApplied = true;
                                    break;
                                }
                            }
                        }

                        if (markToBase && m_LastBaseGlyphIndex != int.MinValue && !wasLookupApplied)
                        {
                            Glyph baseGlyph = m_textInfo.characterInfo[m_LastBaseGlyphIndex].textElement.glyph;
                            uint baseGlyphIndex = baseGlyph.index;
                            uint markGlyphIndex = m_cached_TextElement.glyphIndex;
                            uint key = markGlyphIndex << 16 | baseGlyphIndex;

                            if (m_currentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float advanceOffset = (m_textInfo.characterInfo[m_LastBaseGlyphIndex].origin - m_xAdvance) / currentElementScale;

                                glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                            }
                        }
                    }
                }

                elementAscentLine += glyphAdjustments.yPlacement;
                elementDescentLine += glyphAdjustments.yPlacement;
                #endregion

                float xAdvanceBeforeChar = m_xAdvance;


                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_monoSpacing != 0)
                {
                    if (m_duoSpace && (charCode == '.' || charCode == ':' || charCode == ','))
                        monoAdvance = (m_monoSpacing / 4 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_charWidthAdjDelta);
                    else
                        monoAdvance = (m_monoSpacing / 2 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_charWidthAdjDelta);

                    m_xAdvance += monoAdvance;
                }
                #endregion


                #region Handle Style Padding
                float boldSpacingAdjustment;
                float style_padding;
                if (!isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold))
                {
                    if (m_currentMaterial != null && m_currentMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
                    {
                        float gradientScale = m_currentMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                        style_padding = m_currentFontAsset.boldStyle / 4.0f * gradientScale * m_currentMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                        if (style_padding + padding > gradientScale)
                            padding = gradientScale - style_padding;
                    }
                    else
                        style_padding = 0;

                    boldSpacingAdjustment = m_currentFontAsset.boldSpacing;
                }
                else
                {
                    if (m_currentMaterial != null && m_currentMaterial.HasProperty(ShaderUtilities.ID_GradientScale) && m_currentMaterial.HasProperty(ShaderUtilities.ID_ScaleRatio_A))
                    {
                        float gradientScale = m_currentMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                        style_padding = m_currentFontAsset.normalStyle / 4.0f * gradientScale * m_currentMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                        if (style_padding + padding > gradientScale)
                            padding = gradientScale - style_padding;
                    }
                    else
                        style_padding = 0;

                    boldSpacingAdjustment = 0;
                }
                #endregion Handle Style Padding


                #region Calculate Vertices Position
                k_CalculateVerticesPositionMarker.Begin();
                Vector3 top_left;
                top_left.x = m_xAdvance + ((currentGlyphMetrics.horizontalBearingX * m_FXScale.x - padding - style_padding + glyphAdjustments.xPlacement) * currentElementScale * (1 - m_charWidthAdjDelta));
                top_left.y = baselineOffset + (currentGlyphMetrics.horizontalBearingY + padding + glyphAdjustments.yPlacement) * currentElementScale - m_lineOffset + m_baselineOffset;
                top_left.z = 0;

                Vector3 bottom_left;
                bottom_left.x = top_left.x;
                bottom_left.y = top_left.y - ((currentGlyphMetrics.height + padding * 2) * currentElementScale);
                bottom_left.z = 0;

                Vector3 top_right;
                top_right.x = bottom_left.x + ((currentGlyphMetrics.width * m_FXScale.x + padding * 2 + style_padding * 2) * currentElementScale * (1 - m_charWidthAdjDelta));
                top_right.y = top_left.y;
                top_right.z = 0;

                Vector3 bottom_right;
                bottom_right.x = top_right.x;
                bottom_right.y = bottom_left.y;
                bottom_right.z = 0;

                k_CalculateVerticesPositionMarker.End();
                #endregion


                #region Handle Italic & Shearing
                if (!isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic))
                {
                    float shear_value = m_ItalicAngle * 0.01f;
                    float midPoint = ((m_currentFontAsset.m_FaceInfo.capLine - (m_currentFontAsset.m_FaceInfo.baseline + m_baselineOffset)) / 2) * m_fontScaleMultiplier * m_currentFontAsset.m_FaceInfo.scale;
                    Vector3 topShear = new(shear_value * ((currentGlyphMetrics.horizontalBearingY + padding + style_padding - midPoint) * currentElementScale), 0, 0);
                    Vector3 bottomShear = new(shear_value * (((currentGlyphMetrics.horizontalBearingY - currentGlyphMetrics.height - padding - style_padding - midPoint)) * currentElementScale), 0, 0);

                    top_left += topShear;
                    bottom_left += bottomShear;
                    top_right += topShear;
                    bottom_right += bottomShear;
                }
                #endregion Handle Italics & Shearing


                #region Handle Character FX Rotation
                if (m_FXRotation != Quaternion.identity)
                {
                    Matrix4x4 rotationMatrix = Matrix4x4.Rotate(m_FXRotation);
                    Vector3 positionOffset = (top_right + bottom_left) / 2;

                    top_left = rotationMatrix.MultiplyPoint3x4(top_left - positionOffset) + positionOffset;
                    bottom_left = rotationMatrix.MultiplyPoint3x4(bottom_left - positionOffset) + positionOffset;
                    top_right = rotationMatrix.MultiplyPoint3x4(top_right - positionOffset) + positionOffset;
                    bottom_right = rotationMatrix.MultiplyPoint3x4(bottom_right - positionOffset) + positionOffset;
                }
                #endregion


                chInfo.bottomLeft = bottom_left;
                chInfo.topLeft = top_left;
                chInfo.topRight = top_right;
                chInfo.bottomRight = bottom_right;
                
                chInfo.origin = m_xAdvance + glyphAdjustments.xPlacement * currentElementScale;
                chInfo.baseLine = (baselineOffset - m_lineOffset + m_baselineOffset) + glyphAdjustments.yPlacement * currentElementScale;
                chInfo.aspectRatio = (top_right.x - bottom_left.x) / (top_left.y - bottom_left.y);

                #region Compute Ascender & Descender values
                k_ComputeTextMetricsMarker.Begin();
                float elementAscender = elementAscentLine * currentElementScale / smallCapsMultiplier + m_baselineOffset;
                float elementDescender = elementDescentLine * currentElementScale / smallCapsMultiplier + m_baselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                bool isFirstCharacterOfLine = m_characterCount == m_firstCharacterOfLine;
                if (isFirstCharacterOfLine || !isWhiteSpace)
                {
                    if (m_baselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max((elementAscender - m_baselineOffset) / m_fontScaleMultiplier, adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_baselineOffset) / m_fontScaleMultiplier, adjustedDescender);
                    }

                    m_maxLineAscender = Mathf.Max(adjustedAscender, m_maxLineAscender);
                    m_maxLineDescender = Mathf.Min(adjustedDescender, m_maxLineDescender);
                }

                if (isFirstCharacterOfLine || !isWhiteSpace)
                {
                    chInfo.adjustedAscender = adjustedAscender;
                    chInfo.adjustedDescender = adjustedDescender;

                    m_ElementAscender = chInfo.ascender = elementAscender - m_lineOffset;
                    m_ElementDescender = chInfo.descender = elementDescender - m_lineOffset;
                }
                else
                {
                    chInfo.adjustedAscender = m_maxLineAscender;
                    chInfo.adjustedDescender = m_maxLineDescender;

                    m_ElementAscender = chInfo.ascender = m_maxLineAscender - m_lineOffset;
                    m_ElementDescender = chInfo.descender = m_maxLineDescender - m_lineOffset;
                }

                if (m_lineNumber == 0)
                {
                    if (isFirstCharacterOfLine || !isWhiteSpace)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight, m_currentFontAsset.m_FaceInfo.capLine * currentElementScale / smallCapsMultiplier);
                    }
                }
                
                k_ComputeTextMetricsMarker.End();
                #endregion


                chInfo.isVisible = false;

                bool isJustifiedOrFlush = (m_lineJustification & HorizontalAlignmentOptions.Flush) == HorizontalAlignmentOptions.Flush || (m_lineJustification & HorizontalAlignmentOptions.Justified) == HorizontalAlignmentOptions.Justified;

                #region Handle Visible Characters

                ref var lineInfo = ref m_textInfo.lineInfo[m_lineNumber];
                if (charCode == 9 
                    || ((m_TextWrappingMode == TextWrappingModes.PreserveWhitespace 
                         || m_TextWrappingMode == TextWrappingModes.PreserveWhitespaceNoWrap) 
                        && (isWhiteSpace || charCode == 0x200B)) 
                    || (!isWhiteSpace && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) 
                    || (charCode == 0xAD && !isSoftHyphenIgnored))
                {
                    k_HandleVisibleCharacterMarker.Begin();

                    chInfo.isVisible = true;

                    float marginLeft = m_marginLeft;
                    float marginRight = m_marginRight;

                    if (isInjectedCharacter)
                    {
                        marginLeft = lineInfo.marginLeft;
                        marginRight = lineInfo.marginRight;
                    }

                    widthOfTextArea = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    float textWidth = Mathf.Abs(m_xAdvance) + (currentGlyphMetrics.horizontalAdvance) * (1 - m_charWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);
                    float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && !m_IsDrivenLineSpacing ? m_maxLineAscender - m_startOfLineAscender : 0);

                    int testedCharacterCount = m_characterCount;

                    #region Current Line Vertical Bounds Check
                    if (textHeight > marginHeight + 0.0001f)
                    {
                        k_HandleVerticalLineBreakingMarker.Begin();

                        if (m_firstOverflowCharacterIndex == -1)
                            m_firstOverflowCharacterIndex = m_characterCount;

                        if (m_enableAutoSizing)
                        {
                            #region Line Spacing Adjustments
                            if (m_lineSpacingDelta > m_lineSpacingMax && m_lineOffset > 0 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                float adjustmentDelta = (marginHeight - textHeight) / m_lineNumber;

                                m_lineSpacingDelta = Mathf.Max(m_lineSpacingDelta + adjustmentDelta / baseScale, m_lineSpacingMax);

                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                k_GenerateTextPhaseIMarker.End();
                                k_GenerateTextMarker.End();
                                return;
                            }
                            #endregion


                            #region Text Auto-Sizing (Text greater than vertical bounds)
                            if (m_fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                m_maxFontSize = m_fontSize;

                                float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                m_fontSize -= sizeDelta;
                                m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                k_GenerateTextPhaseIMarker.End();
                                k_GenerateTextMarker.End();
                                return;
                            }
                            #endregion Text Auto-Sizing
                        }

                        switch (m_overflowMode)
                        {
                            case TextOverflowModes.Overflow:
                                break;

                            case TextOverflowModes.Truncate:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState);

                                characterToSubstitute.index = testedCharacterCount;
                                characterToSubstitute.unicode = 0x03;
                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;

                            case TextOverflowModes.Ellipsis:
                                if (m_EllipsisInsertionCandidateStack.Count == 0)
                                {
                                    i = -1;
                                    m_characterCount = 0;
                                    characterToSubstitute.index = 0;
                                    characterToSubstitute.unicode = 0x03;
                                    m_firstCharacterOfLine = 0;
                                    k_HandleVerticalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                                }

                                var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                i = RestoreWordWrappingState(ref ellipsisState);

                                i -= 1;
                                m_characterCount -= 1;
                                characterToSubstitute.index = m_characterCount;
                                characterToSubstitute.unicode = 0x2026;

                                restoreCount += 1;
                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                        }

                        k_HandleVerticalLineBreakingMarker.End();
                    }
                    #endregion


                    #region Current Line Horizontal Bounds Check

                    Debug.Log((textWidth, widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f), isJustifiedOrFlush, widthOfTextArea));
                    
                    if (isBaseGlyph && textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        k_HandleHorizontalLineBreakingMarker.Begin();

                        if (m_TextWrappingMode != TextWrappingModes.NoWrap && m_TextWrappingMode != TextWrappingModes.PreserveWhitespaceNoWrap && m_characterCount != m_firstCharacterOfLine)
                        {
                            i = RestoreWordWrappingState(ref m_SavedWordWrapState);

                            float lineOffsetDelta = 0;
                            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                            {
                                float ascender = chInfo.adjustedAscender;
                                lineOffsetDelta = (m_lineOffset > 0 && !m_IsDrivenLineSpacing ? m_maxLineAscender - m_startOfLineAscender : 0) - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                            }
                            else
                            {
                                lineOffsetDelta = m_lineHeight + m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            float newTextHeight = m_maxTextAscender + lineOffsetDelta + m_lineOffset - chInfo.adjustedDescender;

                            #region Handle Soft Hyphenation
                            if (m_textInfo.characterInfo[m_characterCount - 1].character == 0xAD && !isSoftHyphenIgnored)
                            {
                                if (m_overflowMode == TextOverflowModes.Overflow || newTextHeight < marginHeight + 0.0001f)
                                {
                                    characterToSubstitute.index = m_characterCount - 1;
                                    characterToSubstitute.unicode = 0x2D;

                                    i -= 1;
                                    m_characterCount -= 1;
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                                }
                            }

                            isSoftHyphenIgnored = false;

                            if (chInfo.character == 0xAD)
                            {
                                isSoftHyphenIgnored = true;
                                k_HandleHorizontalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                            }
                            #endregion

                            if (m_enableAutoSizing && isFirstWordOfLine)
                            {
                                #region Character Width Adjustments
                                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    if (m_charWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion

                                #region Text Auto-Sizing (Text greater than vertical bounds)
                                if (m_fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_maxFontSize = m_fontSize;

                                    float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                    m_fontSize -= sizeDelta;
                                    m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion Text Auto-Sizing
                            }


                            int savedSoftLineBreakingSpace = m_SavedSoftLineBreakState.previous_WordBreak;
                            if (isFirstWordOfLine && savedSoftLineBreakingSpace != -1)
                            {
                                if (savedSoftLineBreakingSpace != lastSoftLineBreak)
                                {
                                    i = RestoreWordWrappingState(ref m_SavedSoftLineBreakState);
                                    lastSoftLineBreak = savedSoftLineBreakingSpace;

                                    if (m_textInfo.characterInfo[m_characterCount - 1].character == 0xAD)
                                    {
                                        characterToSubstitute.index = m_characterCount - 1;
                                        characterToSubstitute.unicode = 0x2D;

                                        i -= 1;
                                        m_characterCount -= 1;
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;
                                    }
                                }
                            }

                            if (newTextHeight > marginHeight + 0.0001f)
                            {
                                k_HandleVerticalLineBreakingMarker.Begin();

                                if (m_firstOverflowCharacterIndex == -1)
                                    m_firstOverflowCharacterIndex = m_characterCount;

                                if (m_enableAutoSizing)
                                {
                                    #region Line Spacing Adjustments
                                    if (m_lineSpacingDelta > m_lineSpacingMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustmentDelta = (marginHeight - newTextHeight) / (m_lineNumber + 1);

                                        m_lineSpacingDelta = Mathf.Max(m_lineSpacingDelta + adjustmentDelta / baseScale, m_lineSpacingMax);

                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        k_GenerateTextPhaseIMarker.End();
                                        k_GenerateTextMarker.End();
                                        return;
                                    }
                                    #endregion

                                    #region Character Width Adjustments
                                    if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustedTextWidth = textWidth;

                                        if (m_charWidthAdjDelta > 0)
                                            adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                        float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                        m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                        m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        k_GenerateTextPhaseIMarker.End();
                                        k_GenerateTextMarker.End();
                                        return;
                                    }
                                    #endregion

                                    #region Text Auto-Sizing (Text greater than vertical bounds)
                                    if (m_fontSize > m_fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        m_maxFontSize = m_fontSize;

                                        float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                        m_fontSize -= sizeDelta;
                                        m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        k_GenerateTextPhaseIMarker.End();
                                        k_GenerateTextMarker.End();
                                        return;
                                    }
                                    #endregion Text Auto-Sizing
                                }

                                switch (m_overflowMode)
                                {
                                    case TextOverflowModes.Overflow:
                                        InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender);
                                        isStartOfNewLine = true;
                                        isFirstWordOfLine = true;
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;
                                    
                                    case TextOverflowModes.Truncate:
                                        i = RestoreWordWrappingState(ref m_SavedLastValidState);

                                        characterToSubstitute.index = testedCharacterCount;
                                        characterToSubstitute.unicode = 0x03;
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;

                                    case TextOverflowModes.Ellipsis:
                                        if (m_EllipsisInsertionCandidateStack.Count == 0)
                                        {
                                            i = -1;
                                            m_characterCount = 0;
                                            characterToSubstitute.index = 0;
                                            characterToSubstitute.unicode = 0x03;
                                            m_firstCharacterOfLine = 0;
                                            k_HandleVerticalLineBreakingMarker.End();
                                            k_HandleHorizontalLineBreakingMarker.End();
                                            k_HandleVisibleCharacterMarker.End();
                                            continue;
                                        }

                                        var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                        i = RestoreWordWrappingState(ref ellipsisState);

                                        i -= 1;
                                        m_characterCount -= 1;
                                        characterToSubstitute.index = m_characterCount;
                                        characterToSubstitute.unicode = 0x2026;

                                        restoreCount += 1;
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;
                                }
                            }
                            else
                            {
                                InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender);
                                isStartOfNewLine = true;
                                isFirstWordOfLine = true;
                                k_HandleHorizontalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                            }
                        }
                        else
                        {
                            if (m_enableAutoSizing && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                #region Character Width Adjustments
                                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100)
                                {
                                    float adjustedTextWidth = textWidth;

                                    if (m_charWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_charWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_charWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_charWidthAdjDelta = Mathf.Min(m_charWidthAdjDelta, m_charWidthMaxAdj / 100);

                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion

                                #region Text Exceeds Horizontal Bounds - Reducing Point Size
                                if (m_fontSize > m_fontSizeMin)
                                {
                                    m_maxFontSize = m_fontSize;

                                    float sizeDelta = Mathf.Max((m_fontSize - m_minFontSize) / 2, 0.05f);
                                    m_fontSize -= sizeDelta;
                                    m_fontSize = Mathf.Max((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMin);

                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    k_GenerateTextPhaseIMarker.End();
                                    k_GenerateTextMarker.End();
                                    return;
                                }
                                #endregion

                            }

                            switch (m_overflowMode)
                            {
                                case TextOverflowModes.Overflow:
                                    break;
                                
                                case TextOverflowModes.Truncate:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState);

                                    characterToSubstitute.index = testedCharacterCount;
                                    characterToSubstitute.unicode = 0x03;
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;

                                case TextOverflowModes.Ellipsis:
                                    if (m_EllipsisInsertionCandidateStack.Count == 0)
                                    {
                                        i = -1;
                                        m_characterCount = 0;
                                        characterToSubstitute.index = 0;
                                        characterToSubstitute.unicode = 0x03;
                                        m_firstCharacterOfLine = 0;
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;
                                    }

                                    var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                    i = RestoreWordWrappingState(ref ellipsisState);

                                    i -= 1;
                                    m_characterCount -= 1;
                                    characterToSubstitute.index = m_characterCount;
                                    characterToSubstitute.unicode = 0x2026;

                                    restoreCount += 1;
                                    k_HandleHorizontalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                                
                            }

                        }

                        k_HandleHorizontalLineBreakingMarker.End();
                    }
                    #endregion


                    if (isWhiteSpace)
                    {
                        chInfo.isVisible = false;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                        m_lineVisibleSpaceCount = lineInfo.spaceCount += 1;
                        lineInfo.marginLeft = marginLeft;
                        lineInfo.marginRight = marginRight;
                        m_textInfo.spaceCount += 1;

                        if (charCode == 0xA0)
                            lineInfo.controlCharacterCount += 1;
                    }
                    else if (charCode == 0xAD)
                    {
                        chInfo.isVisible = false;
                    }
                    else
                    {
                        Color32 vertexColor;
                        if (m_overrideHtmlColors)
                            vertexColor = m_fontColor32;
                        else
                            vertexColor = m_htmlColor;

                        k_SaveGlyphVertexDataMarker.Begin();
                        SaveGlyphVertexInfo(padding, style_padding, vertexColor);
                        k_SaveGlyphVertexDataMarker.End();

                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_firstVisibleCharacterOfLine = m_characterCount;
                        }

                        m_lineVisibleCharacterCount += 1;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                        lineInfo.marginLeft = marginLeft;
                        lineInfo.marginRight = marginRight;
                    }

                    k_HandleVisibleCharacterMarker.End();
                }
                else
                {
                    k_HandleWhiteSpacesMarker.Begin();

                    if ((charCode == 10 || charCode == 11 || charCode == 0xA0 || charCode == 0x2007 || charCode == 0x2028 || charCode == 0x2029 || char.IsSeparator((char)charCode)) && charCode != 0xAD && charCode != 0x200B && charCode != 0x2060)
                    {
                        lineInfo.spaceCount += 1;
                        m_textInfo.spaceCount += 1;
                    }

                    if (charCode == 0xA0)
                        lineInfo.controlCharacterCount += 1;

                    k_HandleWhiteSpacesMarker.End();
                }
                #endregion Handle Visible Characters


                #region Track Potential Insertion Location for Ellipsis
                if (m_overflowMode == TextOverflowModes.Ellipsis && (!isInjectedCharacter || charCode == 0x2D))
                {
                    float fontScale = m_currentFontSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                    float scale = fontScale * m_fontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                    float marginLeft = m_marginLeft;
                    float marginRight = m_marginRight;

                    if (charCode == 0x0A && m_characterCount != m_firstCharacterOfLine)
                    {
                        fontScale = m_textInfo.characterInfo[m_characterCount - 1].pointSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                        scale = fontScale * m_fontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                        marginLeft = lineInfo.marginLeft;
                        marginRight = lineInfo.marginRight;
                    }

                    float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && !m_IsDrivenLineSpacing ? m_maxLineAscender - m_startOfLineAscender : 0);
                    float textWidth = Mathf.Abs(m_xAdvance) + (m_Ellipsis.character.m_Glyph.metrics.horizontalAdvance) * (1 - m_charWidthAdjDelta) * scale;
                    float widthOfTextAreaForEllipsis = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    if (textWidth < widthOfTextAreaForEllipsis * (isJustifiedOrFlush ? 1.05f : 1.0f) && textHeight < marginHeight + 0.0001f)
                    {
                        SaveWordWrappingState(ref m_SavedEllipsisState, i, m_characterCount);
                        m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                    }
                }
                #endregion


                #region Store Character Data
                chInfo.lineNumber = m_lineNumber;

                if (charCode != 10 && charCode != 11 && charCode != 13 && !isInjectedCharacter || lineInfo.characterCount == 1)
                    lineInfo.alignment = m_lineJustification;
                #endregion Store Character Data


                #region XAdvance, Tabulation & Stops
                k_ComputeCharacterAdvanceMarker.Begin();
                if (charCode == 9)
                {
                    float tabSize = m_currentFontAsset.m_FaceInfo.tabWidth * m_currentFontAsset.tabSize * currentElementScale;
                    float tabs = Mathf.Ceil(m_xAdvance / tabSize) * tabSize;
                    m_xAdvance = tabs > m_xAdvance ? tabs : m_xAdvance + tabSize;
                }
                else if (m_monoSpacing != 0)
                {
                    float monoAdjustment;
                    if (m_duoSpace && (charCode == '.' || charCode == ':' || charCode == ','))
                        monoAdjustment = m_monoSpacing / 2 - monoAdvance;
                    else
                        monoAdjustment = m_monoSpacing - monoAdvance;

                    m_xAdvance += (monoAdjustment + ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment) * currentEmScale) + m_cSpacing) * (1 - m_charWidthAdjDelta);

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }
                else
                {
                    m_xAdvance += ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }

                chInfo.xAdvance = m_xAdvance;
                k_ComputeCharacterAdvanceMarker.End();
                #endregion Tabulation & Stops
                
                float glyphAdvance = m_xAdvance - xAdvanceBeforeChar;

                if (glyphAdvance < 0f)
                {
                    glyphAdvance = -glyphAdvance;
                }

                if (charCode == 10 || charCode == 11 || charCode == 13 || charCode == 0x2028 || charCode == 0x2029)
                {
                    glyphAdvance = 0f;
                }

                chInfo.glyphAdvance = glyphAdvance;
                
                #region Carriage Return
                if (charCode == 13)
                {
                    k_HandleCarriageReturnMarker.Begin();
                    m_xAdvance = 0 + tag_Indent;
                    k_HandleCarriageReturnMarker.End();
                }
                #endregion Carriage Return


                #region Check for Line Feed and Last Character
                if (charCode == 10 || charCode == 11 || charCode == 0x03 || charCode == 0x2028 || charCode == 0x2029 || (charCode == 0x2D && isInjectedCharacter) || m_characterCount == totalCharacterCount - 1)
                {
                    k_HandleLineTerminationMarker.Begin();

                    float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                    if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && !m_IsDrivenLineSpacing)
                    {
                        AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, baselineAdjustmentDelta);
                        m_ElementDescender -= baselineAdjustmentDelta;
                        m_lineOffset += baselineAdjustmentDelta;

                        if (m_SavedEllipsisState.lineNumber == m_lineNumber)
                        {
                            m_SavedEllipsisState = m_EllipsisInsertionCandidateStack.Pop();
                            m_SavedEllipsisState.startOfLineAscender += baselineAdjustmentDelta;
                            m_SavedEllipsisState.lineOffset += baselineAdjustmentDelta;
                            m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                        }
                    }

                    float lineAscender = m_maxLineAscender - m_lineOffset;
                    float lineDescender = m_maxLineDescender - m_lineOffset;

                    m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
                    if (!isMaxVisibleDescenderSet)
                        maxVisibleDescender = m_ElementDescender;

                    if (m_useMaxVisibleDescender && (m_characterCount >= m_maxVisibleCharacters || m_lineNumber >= m_maxVisibleLines))
                        isMaxVisibleDescenderSet = true;

                    lineInfo.firstCharacterIndex = m_firstCharacterOfLine;
                    lineInfo.firstVisibleCharacterIndex = m_firstVisibleCharacterOfLine = m_firstCharacterOfLine > m_firstVisibleCharacterOfLine ? m_firstCharacterOfLine : m_firstVisibleCharacterOfLine;
                    lineInfo.lastCharacterIndex = m_lastCharacterOfLine = m_characterCount;
                    lineInfo.lastVisibleCharacterIndex = m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

                    lineInfo.characterCount = lineInfo.lastCharacterIndex - lineInfo.firstCharacterIndex + 1;
                    lineInfo.visibleCharacterCount = m_lineVisibleCharacterCount;
                    lineInfo.visibleSpaceCount = (lineInfo.lastVisibleCharacterIndex + 1 - lineInfo.firstCharacterIndex) - m_lineVisibleCharacterCount;
                    lineInfo.lineExtents.min = new(m_textInfo.characterInfo[m_firstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
                    lineInfo.lineExtents.max = new(m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].topRight.x, lineAscender);
                    lineInfo.length = lineInfo.lineExtents.max.x - (padding * currentElementScale);
                    lineInfo.width = widthOfTextArea;

                    if (lineInfo.characterCount == 1)
                        lineInfo.alignment = m_lineJustification;

                    float maxAdvanceOffset = ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);
                    if (m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].isVisible)
                        lineInfo.maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance + (-maxAdvanceOffset);
                    else
                        lineInfo.maxAdvance = m_textInfo.characterInfo[m_lastCharacterOfLine].xAdvance + (-maxAdvanceOffset);

                    lineInfo.baseline = 0 - m_lineOffset;
                    lineInfo.ascender = lineAscender;
                    lineInfo.descender = lineDescender;
                    lineInfo.lineHeight = lineAscender - lineDescender + lineGap * baseScale;

                    if (charCode == 10 || charCode == 11 || (charCode == 0x2D && isInjectedCharacter) || charCode == 0x2028 || charCode == 0x2029)
                    {
                        SaveWordWrappingState(ref m_SavedLineState, i, m_characterCount);

                        m_lineNumber += 1;
                        isStartOfNewLine = true;
                        ignoreNonBreakingSpace = false;
                        isFirstWordOfLine = true;

                        m_firstCharacterOfLine = m_characterCount + 1;
                        m_lineVisibleCharacterCount = 0;
                        m_lineVisibleSpaceCount = 0;

                        if (m_lineNumber >= m_textInfo.lineInfo.Length)
                            ResizeLineExtents(m_lineNumber);

                        float lastVisibleAscender = chInfo.adjustedAscender;

                        if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                        {
                            float lineOffsetDelta = 0 - m_maxLineDescender + lastVisibleAscender + (lineGap + m_lineSpacingDelta) * baseScale + (m_lineSpacing + (charCode == 10 || charCode == 0x2029 ? m_paragraphSpacing : 0)) * currentEmScale;
                            m_lineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_lineOffset += m_lineHeight + (m_lineSpacing + (charCode == 10 || charCode == 0x2029 ? m_paragraphSpacing : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_maxLineAscender = k_LargeNegativeFloat;
                        m_maxLineDescender = k_LargePositiveFloat;
                        m_startOfLineAscender = lastVisibleAscender;

                        m_xAdvance = 0 + tag_LineIndent + tag_Indent;

                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_characterCount);
                        SaveWordWrappingState(ref m_SavedLastValidState, i, m_characterCount);

                        m_characterCount += 1;

                        k_HandleLineTerminationMarker.End();

                        continue;
                    }

                    if (charCode == 0x03)
                        i = m_TextProcessingArray.Length;

                    k_HandleLineTerminationMarker.End();
                }
                #endregion Check for Linefeed or Last Character


                #region Track Text Extents
                k_SaveTextExtentMarker.Begin();
                if (chInfo.isVisible)
                {
                    m_meshExtents.min.x = Mathf.Min(m_meshExtents.min.x, chInfo.bottomLeft.x);
                    m_meshExtents.min.y = Mathf.Min(m_meshExtents.min.y, chInfo.bottomLeft.y);

                    m_meshExtents.max.x = Mathf.Max(m_meshExtents.max.x, chInfo.topRight.x);
                    m_meshExtents.max.y = Mathf.Max(m_meshExtents.max.y, chInfo.topRight.y);
                }
                k_SaveTextExtentMarker.End();
                #endregion Track Text Extents


                #region Save Word Wrapping State
                if ((m_TextWrappingMode != TextWrappingModes.NoWrap && m_TextWrappingMode != TextWrappingModes.PreserveWhitespaceNoWrap) || m_overflowMode == TextOverflowModes.Truncate || m_overflowMode == TextOverflowModes.Ellipsis)
                {
                    k_SaveProcessingStatesMarker.Begin();

                    bool shouldSaveHardLineBreak = false;
                    bool shouldSaveSoftLineBreak = false;

                    if ((isWhiteSpace || charCode == 0x200B || charCode == 0x2D || charCode == 0xAD) && (!m_isNonBreakingSpace || ignoreNonBreakingSpace) && charCode != 0xA0 && charCode != 0x2007 && charCode != 0x2011 && charCode != 0x202F && charCode != 0x2060)
                    {
                        if (!(charCode == 0x2D && m_characterCount > 0 && char.IsWhiteSpace(m_textInfo.characterInfo[m_characterCount - 1].character) && m_textInfo.characterInfo[m_characterCount - 1].lineNumber == m_lineNumber))
                        {
                            isFirstWordOfLine = false;
                            shouldSaveHardLineBreak = true;

                            m_SavedSoftLineBreakState.previous_WordBreak = -1;
                        }
                    }
                    else if (!m_isNonBreakingSpace && (TMP_TextParsingUtilities.IsHangul(charCode) && !TMP_Settings.useModernHangulLineBreakingRules || TMP_TextParsingUtilities.IsCJK(charCode)))
                    {
                        bool isCurrentLeadingCharacter = TMP_Settings.linebreakingRules.leadingCharacters.Contains(charCode);
                        bool isNextFollowingCharacter = m_characterCount < totalCharacterCount - 1 && TMP_Settings.linebreakingRules.followingCharacters.Contains(m_textInfo.characterInfo[m_characterCount + 1].character);

                        if (!isCurrentLeadingCharacter)
                        {
                            if (!isNextFollowingCharacter)
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
                    else if (!m_isNonBreakingSpace && m_characterCount + 1 < totalCharacterCount && TMP_TextParsingUtilities.IsCJK(m_textInfo.characterInfo[m_characterCount + 1].character))
                    {
                        shouldSaveHardLineBreak = true;
                    }
                    else if (isFirstWordOfLine)
                    {
                        if (isWhiteSpace && charCode != 0xA0 || (charCode == 0xAD && !isSoftHyphenIgnored))
                            shouldSaveSoftLineBreak = true;

                        shouldSaveHardLineBreak = true;
                    }

                    if (shouldSaveHardLineBreak)
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_characterCount);

                    if (shouldSaveSoftLineBreak)
                        SaveWordWrappingState(ref m_SavedSoftLineBreakState, i, m_characterCount);

                    k_SaveProcessingStatesMarker.End();
                }
                #endregion Save Word Wrapping State

                SaveWordWrappingState(ref m_SavedLastValidState, i, m_characterCount);

                m_characterCount += 1;
            }

            #region Check Auto-Sizing (Upper Font Size Bounds)
            fontSizeDelta = m_maxFontSize - m_minFontSize;
            if (m_enableAutoSizing && fontSizeDelta > 0.051f && m_fontSize < m_fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                if (m_charWidthAdjDelta < m_charWidthMaxAdj / 100)
                    m_charWidthAdjDelta = 0;

                m_minFontSize = m_fontSize;

                float sizeDelta = Mathf.Max((m_maxFontSize - m_fontSize) / 2, 0.05f);
                m_fontSize += sizeDelta;
                m_fontSize = Mathf.Min((int)(m_fontSize * 20 + 0.5f) / 20f, m_fontSizeMax);

                k_GenerateTextPhaseIMarker.End();
                k_GenerateTextMarker.End();
                return;
            }
            #endregion End Auto-sizing Check

            m_IsAutoSizePointSizeSet = true;

            if (m_AutoSizeIterationCount >= m_AutoSizeMaxIterationCount)
                Debug.Log("Auto Size Iteration Count: " + m_AutoSizeIterationCount + ". Final Point Size: " + m_fontSize);

            if (m_characterCount == 0 || (m_characterCount == 1 && charCode == 0x03))
            {
                ClearMesh();

                TMPro_EventManager.ON_TEXT_CHANGED(this);
                k_GenerateTextPhaseIMarker.End();
                k_GenerateTextMarker.End();
                return;
            }

            k_GenerateTextPhaseIMarker.End();

            k_GenerateTextPhaseIIMarker.Begin();
            int last_vert_index = m_materialReferences[m_Underline.materialIndex].referenceCount * 4;

            m_textInfo.meshInfo[0].Clear(false);

            #region Text Vertical Alignment
            Vector3 anchorOffset = Vector3.zero;
            Vector3[] corners = m_RectTransformCorners;

            switch (m_VerticalAlignment)
            {
                case VerticalAlignmentOptions.Top:
                    anchorOffset = corners[1] + new Vector3(0 + margins.x, 0 - m_maxTextAscender - margins.y, 0);
                    break;

                case VerticalAlignmentOptions.Middle:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_maxTextAscender + margins.y + maxVisibleDescender - margins.w) / 2, 0);
                    break;

                case VerticalAlignmentOptions.Bottom:
                    anchorOffset = corners[0] + new Vector3(0 + margins.x, 0 - maxVisibleDescender + margins.w, 0);
                    break;

                case VerticalAlignmentOptions.Baseline:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0, 0);
                    break;

                case VerticalAlignmentOptions.Geometry:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_meshExtents.max.y + margins.y + m_meshExtents.min.y - margins.w) / 2, 0);
                    break;

                case VerticalAlignmentOptions.Capline:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_maxCapHeight - margins.y - margins.w) / 2, 0);
                    break;
            }
            #endregion

            Vector3 justificationOffset = Vector3.zero;
            Vector3 offset = Vector3.zero;

            int wordCount = 0;
            int lineCount = 0;
            int lastLine = 0;
            bool isFirstSeperator = false;

            bool isStartOfWord = false;
            int wordFirstChar = 0;
            int wordLastChar = 0;

            bool isCameraAssigned = m_canvas.worldCamera == null ? false : true;
            float lossyScale = m_previousLossyScaleY = transform.lossyScale.y;
            RenderMode canvasRenderMode = m_canvas.renderMode;
            float canvasScaleFactor = m_canvas.scaleFactor;

            Color32 underlineColor = Color.white;
            Color32 strikethroughColor = Color.white;
            HighlightState highlightState = new(new(255, 255, 0, 64), TMP_Offset.zero);
            float xScale = 0;
            float xScaleMax = 0;
            float underlineStartScale = 0;
            float underlineEndScale = 0;
            float underlineMaxScale = 0;
            float underlineBaseLine = k_LargePositiveFloat;

            float strikethroughPointSize = 0;
            float strikethroughScale = 0;
            float strikethroughBaseline = 0;
            int lastLineNumber = -1;

            TMP_CharacterInfo[] characterInfos = m_textInfo.characterInfo;
            #region Handle Line Justification & UV Mapping & Character Visibility & More

            Debug.Log($"Count: {m_characterCount}");
            for (int i = 0; i < m_characterCount; i++)
            {
                ref var chInfo = ref characterInfos[i];
                ref var BL = ref chInfo.vertex_BL;
                ref var TL = ref chInfo.vertex_TL;
                ref var TR = ref chInfo.vertex_TR;
                ref var BR = ref chInfo.vertex_BR;
                TMP_FontAsset currentFontAsset = chInfo.fontAsset;

                char unicode = chInfo.character;
                bool isWhiteSpace = char.IsWhiteSpace(unicode);

                int currentLine = chInfo.lineNumber;
                ref var currLineInfo = ref m_textInfo.lineInfo[currentLine];
                TMP_LineInfo lineInfo = currLineInfo;
                lineCount = currentLine + 1;

                #region Handle Line Justification

                if (lastLineNumber != currentLine)
                {
                    lastLineNumber = currentLine;
                    HorizontalAlignmentOptions lineAlignment = lineInfo.alignment;

                    if (directions != null)
                    {
                        if (autoHorizontalAlignment)
                        {
                            var dir = Bidi.Direction.LeftToRight;
                            if (currentLine < directions.Length)
                            {
                                dir = directions[currentLine];
                            }
                            
                            if (dir == Bidi.Direction.LeftToRight)
                            {
                                lineAlignment = HorizontalAlignmentOptions.Left;
                            }
                            else
                            {
                                lineAlignment = HorizontalAlignmentOptions.Right;
                            }
                        }
                    }

                    switch (lineAlignment)
                    {
                        case HorizontalAlignmentOptions.Left: 
                            justificationOffset = new(0 + lineInfo.marginLeft, 0, 0);
                            break;

                        case HorizontalAlignmentOptions.Center:
                            justificationOffset = new(lineInfo.marginLeft + lineInfo.width / 2 - lineInfo.maxAdvance / 2, 0, 0);
                            break;

                        case HorizontalAlignmentOptions.Geometry:
                            justificationOffset =
                                new(
                                    lineInfo.marginLeft + lineInfo.width / 2 -
                                    (lineInfo.lineExtents.min.x + lineInfo.lineExtents.max.x) / 2, 0, 0);
                            break;

                        case HorizontalAlignmentOptions.Right: 
                            justificationOffset = new(lineInfo.marginLeft + lineInfo.width - lineInfo.maxAdvance, 0, 0); 
                            break;

                        case HorizontalAlignmentOptions.Justified:
                        case HorizontalAlignmentOptions.Flush:
                            if (i > lineInfo.lastVisibleCharacterIndex || unicode == 0x0A || unicode == 0xAD ||
                                unicode == 0x200B || unicode == 0x2060 || unicode == 0x03) break;

                            char lastCharOfCurrentLine = characterInfos[lineInfo.lastCharacterIndex].character;

                            bool isFlush = (lineAlignment & HorizontalAlignmentOptions.Flush) ==
                                           HorizontalAlignmentOptions.Flush;

                            if (char.IsControl(lastCharOfCurrentLine) == false && currentLine < m_lineNumber ||
                                isFlush || lineInfo.maxAdvance > lineInfo.width)
                            {
                                if (currentLine != lastLine || i == 0 || i == m_firstVisibleCharacter)
                                { 
                                    justificationOffset = new(lineInfo.marginLeft, 0, 0);
                                    
                                    if (char.IsSeparator(unicode))
                                        isFirstSeperator = true;
                                    else
                                        isFirstSeperator = false;
                                }
                                else
                                {
                                    float gap = lineInfo.width - lineInfo.maxAdvance;
                                    int visibleCount = lineInfo.visibleCharacterCount - 1 +
                                                       lineInfo.controlCharacterCount;
                                    int spaces = lineInfo.visibleSpaceCount - lineInfo.controlCharacterCount;

                                    if (isFirstSeperator)
                                    {
                                        spaces -= 1;
                                        visibleCount += 1;
                                    }

                                    float ratio = spaces > 0 ? m_wordWrappingRatios : 1;

                                    if (spaces < 1) spaces = 1;

                                    if (unicode != 0xA0 && (unicode == 9 || char.IsSeparator(unicode)))
                                    {
                                        justificationOffset += new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                    }
                                    else
                                    {
                                        justificationOffset += new Vector3(gap * ratio / visibleCount, 0, 0);
                                    }
                                }
                            }
                            else
                            {
                                justificationOffset = new(lineInfo.marginLeft, 0, 0);
                            }

                            break;
                    }
                }

                #endregion End Text Justification

                offset = anchorOffset + justificationOffset;

                #region Handling of UV2 mapping & Scale packing
                bool isCharacterVisible = chInfo.isVisible;
                if (isCharacterVisible)
                {
                    Extents lineExtents = lineInfo.lineExtents;
                    float uvOffset = (m_uvLineOffset * currentLine) % 1;

                    #region Handle UV Mapping Options

                    switch (m_horizontalMapping)
                    {
                        case TextureMappingOptions.Character:
                            BL.uv2.x = 0;
                            TL.uv2.x = 0;
                            TR.uv2.x = 1;
                            BR.uv2.x = 1;
                            break;

                        case TextureMappingOptions.Line:
                            if (m_textAlignment != TextAlignmentOptions.Justified)
                            {
                                BL.uv2.x =
                                    (BL.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) +
                                    uvOffset;
                                TL.uv2.x =
                                    (TL.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) +
                                    uvOffset;
                                TR.uv2.x =
                                    (TR.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) +
                                    uvOffset;
                                BR.uv2.x =
                                    (BR.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) +
                                    uvOffset;
                                break;
                            }
                            else
                            {
                                BL.uv2.x = (BL.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                    (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                TL.uv2.x = (TL.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                    (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                TR.uv2.x = (TR.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                    (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                BR.uv2.x = (BR.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                    (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                break;
                            }

                        case TextureMappingOptions.Paragraph:
                            BL.uv2.x = (BL.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                            TL.uv2.x = (TL.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                            TR.uv2.x = (TR.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                            BR.uv2.x = (BR.position.x + justificationOffset.x - m_meshExtents.min.x) /
                                (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                            break;

                        case TextureMappingOptions.MatchAspect:

                            switch (m_verticalMapping)
                            {
                                case TextureMappingOptions.Character:
                                    BL.uv2.y = 0;
                                    TL.uv2.y = 1;
                                    TR.uv2.y = 0;
                                    BR.uv2.y = 1;
                                    break;

                                case TextureMappingOptions.Line:
                                    BL.uv2.y = (BL.position.y - lineExtents.min.y) /
                                        (lineExtents.max.y - lineExtents.min.y) + uvOffset;
                                    TL.uv2.y = (TL.position.y - lineExtents.min.y) /
                                        (lineExtents.max.y - lineExtents.min.y) + uvOffset;
                                    TR.uv2.y = BL.uv2.y;
                                    BR.uv2.y = TL.uv2.y;
                                    break;

                                case TextureMappingOptions.Paragraph:
                                    BL.uv2.y = (BL.position.y - m_meshExtents.min.y) /
                                        (m_meshExtents.max.y - m_meshExtents.min.y) + uvOffset;
                                    TL.uv2.y = (TL.position.y - m_meshExtents.min.y) /
                                        (m_meshExtents.max.y - m_meshExtents.min.y) + uvOffset;
                                    TR.uv2.y = BL.uv2.y;
                                    BR.uv2.y = TL.uv2.y;
                                    break;

                                case TextureMappingOptions.MatchAspect:
                                    Debug.Log("ERROR: Cannot Match both Vertical & Horizontal.");
                                    break;
                            }

                            float xDelta = (1 - ((BL.uv2.y + TL.uv2.y) * chInfo.aspectRatio)) / 2;

                            BL.uv2.x = (BL.uv2.y * chInfo.aspectRatio) + xDelta + uvOffset;
                            TL.uv2.x = BL.uv2.x;
                            TR.uv2.x = (TL.uv2.y * chInfo.aspectRatio) + xDelta + uvOffset;
                            BR.uv2.x = TR.uv2.x;
                            break;
                    }

                    switch (m_verticalMapping)
                    {
                        case TextureMappingOptions.Character:
                            BL.uv2.y = 0;
                            TL.uv2.y = 1;
                            TR.uv2.y = 1;
                            BR.uv2.y = 0;
                            break;

                        case TextureMappingOptions.Line:
                            BL.uv2.y = (BL.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender);
                            TL.uv2.y = (TL.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender);
                            TR.uv2.y = TL.uv2.y;
                            BR.uv2.y = BL.uv2.y;
                            break;

                        case TextureMappingOptions.Paragraph:
                            BL.uv2.y = (BL.position.y - m_meshExtents.min.y) /
                                       (m_meshExtents.max.y - m_meshExtents.min.y);
                            TL.uv2.y = (TL.position.y - m_meshExtents.min.y) /
                                       (m_meshExtents.max.y - m_meshExtents.min.y);
                            TR.uv2.y = TL.uv2.y;
                            BR.uv2.y = BL.uv2.y;
                            break;

                        case TextureMappingOptions.MatchAspect:
                            float yDelta = (1 - ((BL.uv2.x + TR.uv2.x) / chInfo.aspectRatio)) / 2;

                            BL.uv2.y = yDelta + (BL.uv2.x / chInfo.aspectRatio);
                            TL.uv2.y = yDelta + (TR.uv2.x / chInfo.aspectRatio);
                            BR.uv2.y = BL.uv2.y;
                            TR.uv2.y = TL.uv2.y;
                            break;
                    }

                    #endregion

                    #region Pack Scale into UV2

                    xScale = chInfo.scale * (1 - m_charWidthAdjDelta);
                    if (!chInfo.isUsingAlternateTypeface && (chInfo.style & FontStyles.Bold) == FontStyles.Bold)
                        xScale *= -1;

                    switch (canvasRenderMode)
                    {
                        case RenderMode.ScreenSpaceOverlay:
                            xScale *= Mathf.Abs(lossyScale) / canvasScaleFactor;
                            break;
                        case RenderMode.ScreenSpaceCamera:
                            xScale *= isCameraAssigned ? Mathf.Abs(lossyScale) : 1;
                            break;
                        case RenderMode.WorldSpace:
                            xScale *= Mathf.Abs(lossyScale);
                            break;
                    }

                    BL.uv.w = xScale;
                    TL.uv.w = xScale;
                    TR.uv.w = xScale;
                    BR.uv.w = xScale;

                    #endregion

                    #region Handle maxVisibleCharacters / maxVisibleLines / Page Mode

                    if (i < m_maxVisibleCharacters && wordCount < m_maxVisibleWords && currentLine < m_maxVisibleLines)
                    {
                        BL.position += offset;
                        TL.position += offset;
                        TR.position += offset;
                        BR.position += offset;
                    }
                    else
                    {
                        BL.position = Vector3.zero;
                        TL.position = Vector3.zero;
                        TR.position = Vector3.zero;
                        BR.position = Vector3.zero;
                        chInfo.isVisible = false;
                    }

                    #endregion


                    FillCharacterVertexBuffers(i);
                }

                #endregion

                chInfo.bottomLeft += offset;
                chInfo.topLeft += offset;
                chInfo.topRight += offset;
                chInfo.bottomRight += offset;

                chInfo.origin += offset.x;
                chInfo.xAdvance += offset.x;

                chInfo.ascender += offset.y;
                chInfo.descender += offset.y;
                chInfo.baseLine += offset.y;

                #region Adjust lineExtents resulting from alignment offset
                if (currentLine != lastLine || i == m_characterCount - 1)
                {
                    if (currentLine != lastLine)
                    {
                        ref var lastLineInfo = ref m_textInfo.lineInfo[lastLine];
                        lastLineInfo.baseline += offset.y;
                        lastLineInfo.ascender += offset.y;
                        lastLineInfo.descender += offset.y;

                        lastLineInfo.maxAdvance += offset.x;

                        lastLineInfo.lineExtents.min = new(m_textInfo.characterInfo[lastLineInfo.firstCharacterIndex].bottomLeft.x, lastLineInfo.descender);
                        lastLineInfo.lineExtents.max = new(m_textInfo.characterInfo[lastLineInfo.lastVisibleCharacterIndex].topRight.x, lastLineInfo.ascender);
                    }

                    if (i == m_characterCount - 1)
                    {
                        currLineInfo.baseline += offset.y;
                        currLineInfo.ascender += offset.y;
                        currLineInfo.descender += offset.y;

                        currLineInfo.maxAdvance += offset.x;

                        currLineInfo.lineExtents.min = new(m_textInfo.characterInfo[currLineInfo.firstCharacterIndex].bottomLeft.x, currLineInfo.descender);
                        currLineInfo.lineExtents.max = new(m_textInfo.characterInfo[currLineInfo.lastVisibleCharacterIndex].topRight.x, currLineInfo.ascender);
                    }
                }
                #endregion


                #region Track Word Count
                if (char.IsLetterOrDigit(unicode) || unicode == 0x2D || unicode == 0xAD || unicode == 0x2010 || unicode == 0x2011)
                {
                    if (!isStartOfWord)
                    {
                        isStartOfWord = true;
                        wordFirstChar = i;
                    }

                    if (isStartOfWord && i == m_characterCount - 1)
                    {
                        int size = m_textInfo.wordInfo.Length;
                        int index = m_textInfo.wordCount;

                        if (m_textInfo.wordCount + 1 > size)
                            TMP_TextInfo.Resize(ref m_textInfo.wordInfo, size + 1);

                        wordLastChar = i;

                        ref var wordInfo = ref m_textInfo.wordInfo[index];
                        wordInfo.firstCharacterIndex = wordFirstChar;
                        wordInfo.lastCharacterIndex = wordLastChar;
                        wordInfo.characterCount = wordLastChar - wordFirstChar + 1;
                        wordInfo.textComponent = this;

                        wordCount += 1;
                        m_textInfo.wordCount += 1;
                        currLineInfo.wordCount += 1;
                    }
                }
                else if (isStartOfWord || i == 0 && (!char.IsPunctuation(unicode) || isWhiteSpace || unicode == 0x200B || i == m_characterCount - 1))
                {
                    if (i > 0 && i < characterInfos.Length - 1 && i < m_characterCount && (unicode == 39 || unicode == 8217) && char.IsLetterOrDigit(characterInfos[i - 1].character) && char.IsLetterOrDigit(characterInfos[i + 1].character))
                    {

                    }
                    else
                    {
                        wordLastChar = i == m_characterCount - 1 && char.IsLetterOrDigit(unicode) ? i : i - 1;
                        isStartOfWord = false;

                        int size = m_textInfo.wordInfo.Length;
                        int index = m_textInfo.wordCount;

                        if (m_textInfo.wordCount + 1 > size)
                            TMP_TextInfo.Resize(ref m_textInfo.wordInfo, size + 1);

                        ref var wordInfo = ref m_textInfo.wordInfo[index];
                        wordInfo.firstCharacterIndex = wordFirstChar;
                        wordInfo.lastCharacterIndex = wordLastChar;
                        wordInfo.characterCount = wordLastChar - wordFirstChar + 1;
                        wordInfo.textComponent = this;

                        wordCount += 1;
                        m_textInfo.wordCount += 1;
                        currLineInfo.wordCount += 1;
                    }
                }
                #endregion


                #region Underline

                bool isUnderline = (m_textInfo.characterInfo[i].style & FontStyles.Underline) == FontStyles.Underline;
                if (isUnderline)
                {
                    bool isUnderlineVisible = !(i > m_maxVisibleCharacters || currentLine > m_maxVisibleLines);

                    if (!isWhiteSpace && unicode != 0x200B)
                    {
                        underlineMaxScale = Mathf.Max(underlineMaxScale, m_textInfo.characterInfo[i].scale);
                        xScaleMax = Mathf.Max(xScaleMax, Mathf.Abs(xScale));
                        underlineBaseLine = Mathf.Min(k_LargePositiveFloat, m_textInfo.characterInfo[i].baseLine + font.m_FaceInfo.underlineOffset * underlineMaxScale);
                    }

                    if (!beginUnderline && isUnderlineVisible && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginUnderline = true;
                            underlineStartScale = m_textInfo.characterInfo[i].scale;
                            if (underlineMaxScale == 0)
                            {
                                underlineMaxScale = underlineStartScale;
                                xScaleMax = xScale;
                            }
                            underline_start = new(m_textInfo.characterInfo[i].bottomLeft.x, underlineBaseLine, 0);
                            underlineColor = m_textInfo.characterInfo[i].underlineColor;
                        }
                    }

                    if (beginUnderline && m_characterCount == 1)
                    {
                        beginUnderline = false;
                        underline_end = new(m_textInfo.characterInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = m_textInfo.characterInfo[i].scale;

                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                    else if (beginUnderline && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        if (isWhiteSpace || unicode == 0x200B)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            underline_end = new(m_textInfo.characterInfo[lastVisibleCharacterIndex].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = m_textInfo.characterInfo[lastVisibleCharacterIndex].scale;
                        }
                        else
                        {
                            underline_end = new(m_textInfo.characterInfo[i].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = m_textInfo.characterInfo[i].scale;
                        }

                        beginUnderline = false;
                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                    else if (beginUnderline && !isUnderlineVisible)
                    {
                        beginUnderline = false;
                        underline_end = new(m_textInfo.characterInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = m_textInfo.characterInfo[i - 1].scale;

                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                    else if (beginUnderline && i < m_characterCount - 1 && !underlineColor.Compare(m_textInfo.characterInfo[i + 1].underlineColor))
                    {
                        beginUnderline = false;
                        underline_end = new(m_textInfo.characterInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = m_textInfo.characterInfo[i].scale;

                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                }
                else
                {
                    if (beginUnderline)
                    {
                        beginUnderline = false;
                        underline_end = new(m_textInfo.characterInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = m_textInfo.characterInfo[i - 1].scale;

                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                }
                #endregion


                #region Strikethrough

                bool isStrikethrough = (m_textInfo.characterInfo[i].style & FontStyles.Strikethrough) == FontStyles.Strikethrough;
                float strikethroughOffset = currentFontAsset.m_FaceInfo.strikethroughOffset;

                if (isStrikethrough)
                {
                    bool isStrikeThroughVisible = !(i > m_maxVisibleCharacters || currentLine > m_maxVisibleLines);

                    if (!beginStrikethrough && isStrikeThroughVisible && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginStrikethrough = true;
                            strikethroughPointSize = m_textInfo.characterInfo[i].pointSize;
                            strikethroughScale = m_textInfo.characterInfo[i].scale;
                            strikethrough_start = new(m_textInfo.characterInfo[i].bottomLeft.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                            strikethroughColor = m_textInfo.characterInfo[i].strikethroughColor;
                            strikethroughBaseline = m_textInfo.characterInfo[i].baseLine;
                        }
                    }

                    if (beginStrikethrough && m_characterCount == 1)
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && i == lineInfo.lastCharacterIndex)
                    {
                        if (isWhiteSpace || unicode == 0x200B)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            strikethrough_end = new(m_textInfo.characterInfo[lastVisibleCharacterIndex].topRight.x, m_textInfo.characterInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }
                        else
                        {
                            strikethrough_end = new(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }

                        beginStrikethrough = false;
                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && i < m_characterCount && (m_textInfo.characterInfo[i + 1].pointSize != strikethroughPointSize || !TMP_Math.Approximately(m_textInfo.characterInfo[i + 1].baseLine + offset.y, strikethroughBaseline)))
                    {
                        beginStrikethrough = false;

                        int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                        if (i > lastVisibleCharacterIndex)
                            strikethrough_end = new(m_textInfo.characterInfo[lastVisibleCharacterIndex].topRight.x, m_textInfo.characterInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        else
                            strikethrough_end = new(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && i < m_characterCount && currentFontAsset.GetInstanceID() != characterInfos[i + 1].fontAsset.GetInstanceID())
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && !isStrikeThroughVisible)
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new(m_textInfo.characterInfo[i - 1].topRight.x, m_textInfo.characterInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                }
                else
                {
                    if (beginStrikethrough)
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new(m_textInfo.characterInfo[i - 1].topRight.x, m_textInfo.characterInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                }
                #endregion


                #region Text Highlighting
                bool isHighlight = (m_textInfo.characterInfo[i].style & FontStyles.Highlight) == FontStyles.Highlight;
                if (isHighlight)
                {
                    bool isHighlightVisible = !(i > m_maxVisibleCharacters || currentLine > m_maxVisibleLines); 

                    if (!beginHighlight && isHighlightVisible && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginHighlight = true;
                            highlight_start = k_LargePositiveVector2;
                            highlight_end = k_LargeNegativeVector2;
                            highlightState = m_textInfo.characterInfo[i].highlightState;
                        }
                    }

                    if (beginHighlight)
                    {
                        TMP_CharacterInfo currentCharacter = m_textInfo.characterInfo[i];
                        HighlightState currentState = currentCharacter.highlightState;

                        bool isColorTransition = false;

                        if (highlightState != currentState)
                        {
                            if (isWhiteSpace)
                                highlight_end.x = (highlight_end.x - highlightState.padding.right + currentCharacter.origin) / 2;
                            else
                                highlight_end.x = (highlight_end.x - highlightState.padding.right + currentCharacter.bottomLeft.x) / 2;

                            highlight_start.y = Mathf.Min(highlight_start.y, currentCharacter.descender);
                            highlight_end.y = Mathf.Max(highlight_end.y, currentCharacter.ascender);

                            DrawTextHighlight(highlight_start, highlight_end, ref last_vert_index, highlightState.color);

                            beginHighlight = true;
                            highlight_start = new Vector2(highlight_end.x, currentCharacter.descender - currentState.padding.bottom);

                            if (isWhiteSpace)
                                highlight_end = new Vector2(currentCharacter.xAdvance + currentState.padding.right, currentCharacter.ascender + currentState.padding.top);
                            else
                                highlight_end = new Vector2(currentCharacter.topRight.x + currentState.padding.right, currentCharacter.ascender + currentState.padding.top);

                            highlightState = currentState;

                            isColorTransition = true;
                        }

                        if (!isColorTransition)
                        {
                            if (isWhiteSpace)
                            {
                                highlight_start.x = Mathf.Min(highlight_start.x, currentCharacter.origin - highlightState.padding.left);
                                highlight_end.x = Mathf.Max(highlight_end.x, currentCharacter.xAdvance + highlightState.padding.right);
                            }
                            else
                            {
                                highlight_start.x = Mathf.Min(highlight_start.x, currentCharacter.bottomLeft.x - highlightState.padding.left);
                                highlight_end.x = Mathf.Max(highlight_end.x, currentCharacter.topRight.x + highlightState.padding.right);
                            }

                            highlight_start.y = Mathf.Min(highlight_start.y, currentCharacter.descender - highlightState.padding.bottom);
                            highlight_end.y = Mathf.Max(highlight_end.y, currentCharacter.ascender + highlightState.padding.top);
                        }
                    }

                    if (beginHighlight && m_characterCount == 1)
                    {
                        beginHighlight = false;

                        DrawTextHighlight(highlight_start, highlight_end, ref last_vert_index, highlightState.color);
                    }
                    else if (beginHighlight && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlight_start, highlight_end, ref last_vert_index, highlightState.color);
                    }
                    else if (beginHighlight && !isHighlightVisible)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlight_start, highlight_end, ref last_vert_index, highlightState.color);
                    }
                }
                else
                {
                    if (beginHighlight)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlight_start, highlight_end, ref last_vert_index, highlightState.color);
                    }
                }
                #endregion

                lastLine = currentLine;
            }
            #endregion

            m_textInfo.meshInfo[m_Underline.materialIndex].vertexCount = last_vert_index;

            m_textInfo.characterCount = m_characterCount;
            m_textInfo.spriteCount = m_spriteCount;
            m_textInfo.lineCount = lineCount;
            m_textInfo.wordCount = wordCount != 0 && m_characterCount > 0 ? wordCount : 1;

            k_GenerateTextPhaseIIMarker.End();

            k_GenerateTextPhaseIIIMarker.Begin();
            Debug.Log(m_textInfo.lineCount);
            if (m_renderMode == TextRenderFlags.Render && IsActive())
            {
                OnPreRenderText?.Invoke(m_textInfo);

                if (m_canvas.additionalShaderChannels != (AdditionalCanvasShaderChannels)25)
                    m_canvas.additionalShaderChannels |= (AdditionalCanvasShaderChannels)25;

                if (m_geometrySortingOrder != VertexSortingOrder.Normal)
                    m_textInfo.meshInfo[0].SortGeometry(VertexSortingOrder.Reverse);

                m_mesh.MarkDynamic();
                m_mesh.vertices = m_textInfo.meshInfo[0].vertices;
                m_mesh.SetUVs(0, m_textInfo.meshInfo[0].uvs0);
                m_mesh.uv2 = m_textInfo.meshInfo[0].uvs2;
                m_mesh.colors32 = m_textInfo.meshInfo[0].colors32;

                m_mesh.RecalculateBounds();

                m_canvasRenderer.SetMesh(m_mesh);

                Color parentBaseColor = m_canvasRenderer.GetColor();

                bool isCullTransparentMeshEnabled = m_canvasRenderer.cullTransparentMesh;

                for (int i = 1; i < m_textInfo.materialCount; i++)
                {
                    m_textInfo.meshInfo[i].ClearUnusedVertices();

                    if (m_subTextObjects[i] == null) continue;

                    if (m_geometrySortingOrder != VertexSortingOrder.Normal)
                        m_textInfo.meshInfo[i].SortGeometry(VertexSortingOrder.Reverse);

                    m_subTextObjects[i].mesh.vertices = m_textInfo.meshInfo[i].vertices;
                    m_subTextObjects[i].mesh.SetUVs(0, m_textInfo.meshInfo[i].uvs0);
                    m_subTextObjects[i].mesh.uv2 = m_textInfo.meshInfo[i].uvs2;
                    m_subTextObjects[i].mesh.colors32 = m_textInfo.meshInfo[i].colors32;

                    m_subTextObjects[i].mesh.RecalculateBounds();

                    m_subTextObjects[i].canvasRenderer.SetMesh(m_subTextObjects[i].mesh);

                    m_subTextObjects[i].canvasRenderer.SetColor(parentBaseColor);

                    m_subTextObjects[i].canvasRenderer.cullTransparentMesh = isCullTransparentMeshEnabled;

                    m_subTextObjects[i].raycastTarget = raycastTarget;
                }
            }

            if (m_ShouldUpdateCulling)
                UpdateCulling();

            TMPro_EventManager.ON_TEXT_CHANGED(this);

            k_GenerateTextPhaseIIIMarker.End();
            k_GenerateTextMarker.End();
        }
    }
}