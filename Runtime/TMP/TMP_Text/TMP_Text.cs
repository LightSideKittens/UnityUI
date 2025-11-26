#define TMP_PRESENT

using System;
using System.Text;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;


namespace TMPro
{
    public interface ITextElement
    {
        Material sharedMaterial { get; }

        void Rebuild(CanvasUpdate update);
        int GetInstanceID();
    }

    public enum TextAlignmentOptions
    {
        TopLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Top,
        Top = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Top,
        TopRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Top,
        TopJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Top,
        TopFlush = HorizontalAlignmentOptions.Flush | VerticalAlignmentOptions.Top,
        TopGeoAligned = HorizontalAlignmentOptions.Geometry | VerticalAlignmentOptions.Top,

        Left = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Middle,
        Center = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Middle,
        Right = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Middle,
        Justified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Middle,
        Flush = HorizontalAlignmentOptions.Flush | VerticalAlignmentOptions.Middle,
        CenterGeoAligned = HorizontalAlignmentOptions.Geometry | VerticalAlignmentOptions.Middle,

        BottomLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Bottom,
        Bottom = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Bottom,
        BottomRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Bottom,
        BottomJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Bottom,
        BottomFlush = HorizontalAlignmentOptions.Flush | VerticalAlignmentOptions.Bottom,
        BottomGeoAligned = HorizontalAlignmentOptions.Geometry | VerticalAlignmentOptions.Bottom,

        BaselineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Baseline,
        Baseline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Baseline,
        BaselineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Baseline,
        BaselineJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Baseline,
        BaselineFlush = HorizontalAlignmentOptions.Flush | VerticalAlignmentOptions.Baseline,
        BaselineGeoAligned = HorizontalAlignmentOptions.Geometry | VerticalAlignmentOptions.Baseline,

        MidlineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Geometry,
        Midline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Geometry,
        MidlineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Geometry,
        MidlineJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Geometry,
        MidlineFlush = HorizontalAlignmentOptions.Flush | VerticalAlignmentOptions.Geometry,
        MidlineGeoAligned = HorizontalAlignmentOptions.Geometry | VerticalAlignmentOptions.Geometry,

        CaplineLeft = HorizontalAlignmentOptions.Left | VerticalAlignmentOptions.Capline,
        Capline = HorizontalAlignmentOptions.Center | VerticalAlignmentOptions.Capline,
        CaplineRight = HorizontalAlignmentOptions.Right | VerticalAlignmentOptions.Capline,
        CaplineJustified = HorizontalAlignmentOptions.Justified | VerticalAlignmentOptions.Capline,
        CaplineFlush = HorizontalAlignmentOptions.Flush | VerticalAlignmentOptions.Capline,
        CaplineGeoAligned = HorizontalAlignmentOptions.Geometry | VerticalAlignmentOptions.Capline,

        Converted = 0xFFFF
    };

    /// <summary>
    /// Horizontal text alignment options.
    /// </summary>
    public enum HorizontalAlignmentOptions
    {
        Left = 0x1, Center = 0x2, Right = 0x4, Justified = 0x8, Flush = 0x10, Geometry = 0x20
    }

    /// <summary>
    /// Vertical text alignment options.
    /// </summary>
    public enum VerticalAlignmentOptions
    {
        Top = 0x100, Middle = 0x200, Bottom = 0x400, Baseline = 0x800, Geometry = 0x1000, Capline = 0x2000,
    }


    /// <summary>
    /// Flags controlling what vertex data gets pushed to the mesh.
    /// </summary>
    public enum TextRenderFlags
    {
        DontRender = 0x0,
        Render = 0xFF
    };

    public enum TMP_TextElementType { Character, Sprite };
    public enum MaskingTypes { MaskOff = 0, MaskHard = 1, MaskSoft = 2 };

    public enum TextOverflowModes { Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3, ScrollRect = 4, Page = 5, Linked = 6 };
    public enum TextWrappingModes { NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 };
    public enum MaskingOffsetMode { Percentage = 0, Pixel = 1 };
    public enum TextureMappingOptions { Character = 0, Line = 1, Paragraph = 2, MatchAspect = 3 };

    [Flags]
    public enum FontStyles { Normal = 0x0, Bold = 0x1, Italic = 0x2, Underline = 0x4, LowerCase = 0x8, UpperCase = 0x10, SmallCaps = 0x20, Strikethrough = 0x40, Superscript = 0x80, Subscript = 0x100, Highlight = 0x200 };
    public enum FontWeight { Thin = 100, ExtraLight = 200, Light = 300, Regular = 400, Medium = 500, SemiBold = 600, Bold = 700, Heavy = 800, Black = 900 };

    /// <summary>
    /// Base class which contains common properties and functions shared between the TextMeshPro and TextMeshProUGUI component.
    /// </summary>
    public abstract partial class TMP_Text : MaskableGraphic
    {
        /// <summary>
        /// Method which derived classes need to override to load Font Assets.
        /// </summary>
        protected virtual void LoadFontAsset() { }

        /// <summary>
        /// Function called internally when a new shared material is assigned via the fontSharedMaterial property.
        /// </summary>
        /// <param name="mat"></param>
        protected virtual void SetSharedMaterial(Material mat) { }

        /// <summary>
        /// Function called internally when a new material is assigned via the fontMaterial property.
        /// </summary>
        protected virtual Material GetMaterial(Material mat) { return null; }

        /// <summary>
        /// Function called internally when assigning a new base material.
        /// </summary>
        /// <param name="mat"></param>
        protected virtual void SetFontBaseMaterial(Material mat) { }

        /// <summary>
        /// Method which returns an array containing the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Material[] GetSharedMaterials() { return null; }

        /// <summary>
        ///
        /// </summary>
        protected virtual void SetSharedMaterials(Material[] materials) { }

        /// <summary>
        /// Method returning instances of the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Material[] GetMaterials(Material[] mats) { return null; }

        /// <summary>
        /// Method to set the materials of the text and sub text objects.
        /// </summary>
        /// <param name="mats"></param>

        /// <summary>
        /// Function used to create an instance of the material
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected virtual Material CreateMaterialInstance(Material source)
        {
            Material mat = new Material(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";

            return mat;
        }

        protected void SetVertexColorGradient(TMP_ColorGradient gradient)
        {
            if (gradient == null) return;

            m_fontColorGradient.bottomLeft = gradient.bottomLeft;
            m_fontColorGradient.bottomRight = gradient.bottomRight;
            m_fontColorGradient.topLeft = gradient.topLeft;
            m_fontColorGradient.topRight = gradient.topRight;

            SetVerticesDirty();
        }

        /// <summary>
        /// Function to control the sorting of the geometry of the text object.
        /// </summary>
        protected void SetTextSortingOrder(VertexSortingOrder order)
        {

        }

        /// <summary>
        /// Function to sort the geometry of the text object in accordance to the provided order.
        /// </summary>
        /// <param name="order"></param>
        protected void SetTextSortingOrder(int[] order)
        {

        }

        /// <summary>
        /// Function called internally to set the face color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected virtual void SetFaceColor(Color32 color) { }

        /// <summary>
        /// Function called internally to set the outline color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected virtual void SetOutlineColor(Color32 color) { }

        /// <summary>
        /// Function called internally to set the outline thickness property of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="thickness"></param>
        protected virtual void SetOutlineThickness(float thickness) { }

        /// <summary>
        /// Set the Render Queue and ZTest mode on the current material
        /// </summary>
        protected virtual void SetShaderDepth() { }

        /// <summary>
        /// Set the culling mode on the material.
        /// </summary>
        protected virtual void SetCulling() { }

        /// <summary>
        ///
        /// </summary>
        internal virtual void UpdateCulling() {}

        /// <summary>
        /// Get the padding value for the currently assigned material
        /// </summary>
        /// <returns></returns>
        protected virtual float GetPaddingForMaterial()
        {
            ShaderUtilities.GetShaderPropertyIDs();

            if (m_sharedMaterial == null) return 0;

            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
            m_isSDFShader = m_sharedMaterial.HasProperty(ShaderUtilities.ID_WeightNormal);

            return m_padding;
        }


        /// <summary>
        /// Get the padding value for the given material
        /// </summary>
        /// <returns></returns>
        protected virtual float GetPaddingForMaterial(Material mat)
        {
            if (mat == null)
                return 0;

            m_padding = ShaderUtilities.GetPadding(mat, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
            m_isSDFShader = mat.HasProperty(ShaderUtilities.ID_WeightNormal);

            return m_padding;
        }


        /// <summary>
        /// Method to return the local corners of the Text Container or RectTransform.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector3[] GetTextContainerLocalCorners() { return null; }


        protected bool m_ignoreActiveState;
        /// <summary>
        /// Function to force regeneration of the text object before its normal process time. This is useful when changes to the text object properties need to be applied immediately.
        /// </summary>
        /// <param name="ignoreActiveState">Ignore Active State of text objects. Inactive objects are ignored by default.</param>
        /// <param name="forceTextReparsing">Force re-parsing of the text.</param>
        public virtual void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }


        /// <summary>
        /// Function to update the geometry of the main and sub text objects.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="index"></param>
        public virtual void UpdateGeometry(Mesh mesh, int index) { }


        /// <summary>
        /// Function to push the updated vertex data into the mesh and renderer.
        /// </summary>
        public virtual void UpdateVertexData(TMP_VertexDataUpdateFlags flags) { }


        /// <summary>
        /// Function to push the updated vertex data into the mesh and renderer.
        /// </summary>
        public virtual void UpdateVertexData() { }


        /// <summary>
        /// Function to push a new set of vertices to the mesh.
        /// </summary>
        /// <param name="vertices"></param>
        public virtual void SetVertices(Vector3[] vertices) { }


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        public virtual void UpdateMeshPadding() { }


        /// <summary>
        ///
        /// </summary>


        /// <summary>
        /// Tweens the CanvasRenderer color associated with this Graphic.
        /// </summary>
        /// <param name="targetColor">Target color.</param>
        /// <param name="duration">Tween duration.</param>
        /// <param name="ignoreTimeScale">Should ignore Time.scale?</param>
        /// <param name="useAlpha">Should also Tween the alpha channel?</param>
        public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
        {
            base.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
            InternalCrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
        }


        /// <summary>
        /// Tweens the alpha of the CanvasRenderer color associated with this Graphic.
        /// </summary>
        /// <param name="alpha">Target alpha.</param>
        /// <param name="duration">Duration of the tween in seconds.</param>
        /// <param name="ignoreTimeScale">Should ignore Time.scale?</param>
        public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            base.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
            InternalCrossFadeAlpha(alpha, duration, ignoreTimeScale);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="targetColor"></param>
        /// <param name="duration"></param>
        /// <param name="ignoreTimeScale"></param>
        /// <param name="useAlpha"></param>
        /// <param name="useRGB"></param>
        protected virtual void InternalCrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }


        /// <summary>
        ///
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="duration"></param>
        /// <param name="ignoreTimeScale"></param>
        protected virtual void InternalCrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }

        /// <summary>
        ///
        /// </summary>
        private struct TextBackingContainer
        {
            public uint[] Text
            {
                get { return m_Array; }
            }

            public int Capacity
            {
                get { return m_Array.Length; }
            }

            public int Count
            {
                get { return m_Index; }
                set { m_Index = value; }
            }

            private uint[] m_Array;
            private int m_Index;

            public uint this[int index]
            {
                get { return m_Array[index]; }
                set
                {
                    if (index >= m_Array.Length)
                        Resize(index);

                    m_Array[index] = value;
                }
            }

            public TextBackingContainer(int size)
            {
                m_Array = new uint[size];
                m_Index = 0;
            }

            public void Resize(int size)
            {
                size = Mathf.NextPowerOfTwo(size + 1);

                Array.Resize(ref m_Array, size);
            }

        }

        /// <summary>
        /// Internal array containing the converted source text used in the text parsing process.
        /// </summary>
        private TextBackingContainer m_TextBackingArray = new TextBackingContainer(4);


        /// <summary>
        /// Method to parse the input text based on its source
        /// </summary>
        protected void ParseInputText()
        {
            k_ParseTextMarker.Begin();

            switch (m_inputSource)
            {
                case TextInputSources.TextString:
                case TextInputSources.TextInputBox:
                    PopulateTextBackingArray(m_TextPreprocessor == null ? m_text : m_TextPreprocessor.PreprocessText(m_text));
                    PopulateTextProcessingArray();
                    break;
                case TextInputSources.SetText:
                    break;
                case TextInputSources.SetTextArray:
                    break;
            }

            SetArraySizes(m_TextProcessingArray);

            k_ParseTextMarker.End();
        }


        /// <summary>
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">Source text to be converted</param>
        private void PopulateTextBackingArray(string sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            PopulateTextBackingArray(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Convert source text to uint and populate internal text backing array.
        /// </summary>
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

        /// <summary>
        /// Convert source text to uint and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">char array containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        private void PopulateTextBackingArray(StringBuilder sourceText, int start, int length)
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

        /// <summary>
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">char array containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        private void PopulateTextBackingArray(char[] sourceText, int start, int length)
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

        /// <summary>
        ///
        /// </summary>
        private void PopulateTextProcessingArray()
        {
            Debug.Log("PopulateTextProcessingArray");
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

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 10 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 114:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 13 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 116:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 9 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 118:
                            if (!m_parseCtrlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 11 };
                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 117:
                            if (srcLength > readIndex + 5 && IsValidUTF16(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = GetUTF16(m_TextBackingArray, readIndex + 2) };
                                readIndex += 5;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                        case 85:
                            if (srcLength > readIndex + 9 && IsValidUTF32(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 10, unicode = GetUTF32(m_TextBackingArray, readIndex + 2) };
                                readIndex += 9;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                    }
                }

                if (c >= CodePoint.HIGH_SURROGATE_START && c <= CodePoint.HIGH_SURROGATE_END && srcLength > readIndex + 1 && m_TextBackingArray[readIndex + 1] >= CodePoint.LOW_SURROGATE_START && m_TextBackingArray[readIndex + 1] <= CodePoint.LOW_SURROGATE_END)
                {
                    m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 2, unicode = TMP_TextParsingUtilities.ConvertToUTF32(c, m_TextBackingArray[readIndex + 1]) };
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

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 10 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.CR:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 13 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0xA0 };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0x200B };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWJ:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0x200D };
                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.SHY:
                            if (tag_NoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0xAD };
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

                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = c };

                writeIndex += 1;
            }

            m_TextStyleStackDepth = 0;

            if (textStyle.hashCode != (int)MarkupTag.NORMAL)
                InsertClosingStyleTag(ref m_TextProcessingArray, ref writeIndex);

            if (writeIndex == m_TextProcessingArray.Length) ResizeInternalArray(ref m_TextProcessingArray);

            m_TextProcessingArray[writeIndex].unicode = 0;
            m_InternalTextProcessingArraySize = writeIndex;
        }

        /// <summary>
        /// Function used in conjunction with GetPreferredValues
        /// </summary>
        /// <param name="sourceText"></param>
        private void SetTextInternal(string sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            PopulateTextBackingArray(sourceText, 0, srcLength);

            TextInputSources currentInputSource = m_inputSource;
            m_inputSource = TextInputSources.TextString;

            PopulateTextProcessingArray();

            m_inputSource = currentInputSource;
        }

        /// <summary>
        /// This function is the same as using the text property to set the text.
        /// </summary>
        /// <param name="sourceText">String containing the text.</param>
        public void SetText(string sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            PopulateTextBackingArray(sourceText, 0, srcLength);

            m_text = sourceText;

            m_inputSource = TextInputSources.TextString;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        public void SetText(string sourceText, float arg0)
        {
            SetText(sourceText, arg0, 0, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        public void SetText(string sourceText, float arg0, float arg1)
        {
            SetText(sourceText, arg0, arg1, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2)
        {
            SetText(sourceText, arg0, arg1, arg2, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, 0, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, arg4, 0, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        /// <param name="arg5">Sixth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, arg4, arg5, 0, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        /// <param name="arg5">Sixth float value.</param>
        /// <param name="arg6">Seventh float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6)
        {
            SetText(sourceText, arg0, arg1, arg2, arg3, arg4, arg5, arg6, 0);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>Ex. TMP_Text.SetText("A = {0}, B = {1:00}, C = {2:000.0}", 10.75f, 10.75f, 10.75f);</para>
        /// <para>Results "A = 10.75, B = 11, C = 010.8."</para>
        /// </summary>
        /// <param name="sourceText">String containing the pattern.</param>
        /// <param name="arg0">First float value.</param>
        /// <param name="arg1">Second float value.</param>
        /// <param name="arg2">Third float value.</param>
        /// <param name="arg3">Forth float value.</param>
        /// <param name="arg4">Fifth float value.</param>
        /// <param name="arg5">Sixth float value.</param>
        /// <param name="arg6">Seventh float value.</param>
        /// <param name="arg7">Eighth float value.</param>
        public void SetText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6, float arg7)
        {
            int argIndex = 0;
            int padding = 0;
            int decimalPrecision = 0;

            int readFlag = 0;

            int readIndex = 0;
            int writeIndex = 0;

            for (; readIndex < sourceText.Length; readIndex++)
            {
                char c = sourceText[readIndex];

                if (c == '{')
                {
                    readFlag = 1;
                    continue;
                }

                if (c == '}')
                {
                    switch (argIndex)
                    {
                        case 0:
                            AddFloatToInternalTextBackingArray(arg0, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 1:
                            AddFloatToInternalTextBackingArray(arg1, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 2:
                            AddFloatToInternalTextBackingArray(arg2, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 3:
                            AddFloatToInternalTextBackingArray(arg3, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 4:
                            AddFloatToInternalTextBackingArray(arg4, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 5:
                            AddFloatToInternalTextBackingArray(arg5, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 6:
                            AddFloatToInternalTextBackingArray(arg6, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 7:
                            AddFloatToInternalTextBackingArray(arg7, padding, decimalPrecision, ref writeIndex);
                            break;
                    }

                    argIndex = 0;
                    readFlag = 0;
                    padding = 0;
                    decimalPrecision = 0;
                    continue;
                }

                if (readFlag == 1)
                {
                    if (c >= '0' && c <= '8')
                    {
                        argIndex = c - 48;
                        readFlag = 2;
                        continue;
                    }
                }

                if (readFlag == 2)
                {
                    if (c == ':')
                        continue;

                    if (c == '.')
                    {
                        readFlag = 3;
                        continue;
                    }

                    if (c == '#')
                    {
                        continue;
                    }

                    if (c == '0')
                    {
                        padding += 1;
                        continue;
                    }

                    if (c == ',')
                    {
                        continue;
                    }

                    if (c >= '1' && c <= '9')
                    {
                        decimalPrecision = c - 48;
                        continue;
                    }
                }

                if (readFlag == 3)
                {
                    if (c == '0')
                    {
                        decimalPrecision += 1;
                        continue;
                    }
                }

                m_TextBackingArray[writeIndex] = c;
                writeIndex += 1;
            }

            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;

            m_IsTextBackingStringDirty = true;

            #if UNITY_EDITOR
            m_text = InternalTextBackingArrayToString();
            #endif

            m_inputSource = TextInputSources.SetText;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }

        /// <summary>
        /// Set the text using a StringBuilder object as the source.
        /// </summary>
        /// <description>
        /// Using a StringBuilder instead of concatenating strings prevents memory allocations with temporary objects.
        /// </description>
        /// <param name="sourceText">The StringBuilder object containing the source text.</param>
        public void SetText(StringBuilder sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            SetText(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Set the text using a StringBuilder object and specifying the starting character index and length.
        /// </summary>
        /// <param name="sourceText">The StringBuilder object containing the source text.</param>
        /// <param name="start">The index of the first character to read from in the array.</param>
        /// <param name="length">The number of characters in the array to be read.</param>
        private void SetText(StringBuilder sourceText, int start, int length)
        {
            PopulateTextBackingArray(sourceText, start, length);

            m_IsTextBackingStringDirty = true;

            #if UNITY_EDITOR
            m_text = InternalTextBackingArrayToString();
            #endif

            m_inputSource = TextInputSources.SetTextArray;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }

        /// <summary>
        /// Set the text using a char array.
        /// </summary>
        /// <param name="sourceText">Source char array containing the Unicode characters of the text.</param>
        public void SetText(char[] sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            SetCharArray(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Set the text using a char array and specifying the starting character index and length.
        /// </summary>
        /// <param name="sourceText">Source char array containing the Unicode characters of the text.</param>
        /// <param name="start">Index of the first character to read from in the array.</param>
        /// <param name="length">The number of characters in the array to be read.</param>
        public void SetText(char[] sourceText, int start, int length)
        {
            SetCharArray(sourceText, start, length);
        }

        /// <summary>
        /// Set the text using a char array.
        /// </summary>
        /// <param name="sourceText">Source char array containing the Unicode characters of the text.</param>
        public void SetCharArray(char[] sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            SetCharArray(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Set the text using a char array and specifying the starting character index and length.
        /// </summary>
        /// <param name="sourceText">Source char array containing the Unicode characters of the text.</param>
        /// <param name="start">The index of the first character to read from in the array.</param>
        /// <param name="length">The number of characters in the array to be read.</param>
        public void SetCharArray(char[] sourceText, int start, int length)
        {
            PopulateTextBackingArray(sourceText, start, length);

            m_IsTextBackingStringDirty = true;

            #if UNITY_EDITOR
            m_text = InternalTextBackingArrayToString();
            #endif

            m_inputSource = TextInputSources.SetTextArray;

            PopulateTextProcessingArray();

            m_havePropertiesChanged = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        private TMP_Style GetStyle(int hashCode)
        {
            TMP_Style style = null;

            if (m_StyleSheet != null)
            {
                style = m_StyleSheet.GetStyle(hashCode);

                if (style != null)
                    return style;
            }

            if (TMP_Settings.defaultStyleSheet != null)
                style = TMP_Settings.defaultStyleSheet.GetStyle(hashCode);

            return style;
        }

        private void InsertOpeningTextStyle(TMP_Style style, ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            m_TextStyleStackDepth += 1;

            m_TextStyleStacks[m_TextStyleStackDepth].Push(style.hashCode);

            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

            m_TextStyleStackDepth -= 1;
        }

        private void InsertClosingTextStyle(TMP_Style style, ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            m_TextStyleStackDepth += 1;

            m_TextStyleStacks[m_TextStyleStackDepth].Push(style.hashCode);

            uint[] styleDefinition = style.styleClosingTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

            m_TextStyleStackDepth -= 1;
        }

        private void InsertTextStyleInTextProcessingArray(ref TextProcessingElement[] charBuffer, ref int writeIndex, uint[] styleDefinition)
        {
            int styleLength = styleDefinition.Length;

            if (writeIndex + styleLength >= charBuffer.Length)
                ResizeInternalArray(ref charBuffer, writeIndex + styleLength);

            for (int i = 0; i < styleLength; i++)
            {
                uint c = styleDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (styleDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(styleDefinition, i + 2);

                                i += 5;
                            }

                            break;
                        case 'U':
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(styleDefinition, i + 2);

                                i += 9;
                            }

                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(styleDefinition, i + 1);

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

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (tag_NoParsing) break;

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (tag_NoParsing) break;

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (tag_NoParsing) break;

                            charBuffer[writeIndex].unicode = 0x200B;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWJ:
                            if (tag_NoParsing) break;

                            charBuffer[writeIndex].unicode = 0x200D;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.SHY:
                            if (tag_NoParsing) break;

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (tag_NoParsing) break;

                            if (ReplaceOpeningStyleTag(ref styleDefinition, i, out int offset, ref charBuffer, ref writeIndex))
                            {
                                int remainChar = styleLength - offset;
                                i = offset;

                                if ( writeIndex + remainChar >= charBuffer.Length)
                                    ResizeInternalArray(ref charBuffer, writeIndex + remainChar);

                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            if (tag_NoParsing) break;

                            ReplaceClosingStyleTag(ref charBuffer, ref writeIndex);

                            i += 7;
                            continue;
                    }
                }

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by opening style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="srcOffset"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        private bool ReplaceOpeningStyleTag(ref TextBackingContainer sourceText, int srcIndex, out int srcOffset, ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            int styleHashCode = GetStyleHashCode(ref sourceText, srcIndex + 7, out srcOffset);
            TMP_Style style = GetStyle(styleHashCode);

            if (style == null || srcOffset == 0) return false;

            m_TextStyleStackDepth += 1;

            m_TextStyleStacks[m_TextStyleStackDepth].Push(style.hashCode);

            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

            m_TextStyleStackDepth -= 1;

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by opening style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="srcOffset"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        private bool ReplaceOpeningStyleTag(ref uint[] sourceText, int srcIndex, out int srcOffset, ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            int styleHashCode = GetStyleHashCode(ref sourceText, srcIndex + 7, out srcOffset);
            TMP_Style style = GetStyle(styleHashCode);

            if (style == null || srcOffset == 0) return false;

            m_TextStyleStackDepth += 1;

            m_TextStyleStacks[m_TextStyleStackDepth].Push(style.hashCode);

            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

             m_TextStyleStackDepth -= 1;

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by closing style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        private void ReplaceClosingStyleTag(ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            int styleHashCode = m_TextStyleStacks[m_TextStyleStackDepth + 1].Pop();
            TMP_Style style = GetStyle(styleHashCode);

            if (style == null) return;

            m_TextStyleStackDepth += 1;

            uint[] styleDefinition = style.styleClosingTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

            m_TextStyleStackDepth -= 1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="style"></param>
        /// <param name="srcIndex"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        private void InsertOpeningStyleTag(TMP_Style style, ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            if (style == null) return;

            m_TextStyleStacks[0].Push(style.hashCode);

            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

            m_TextStyleStackDepth = 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        private void InsertClosingStyleTag(ref TextProcessingElement[] charBuffer, ref int writeIndex)
        {
            int styleHashCode = m_TextStyleStacks[0].Pop();
            TMP_Style style = GetStyle(styleHashCode);

            uint[] styleDefinition = style.styleClosingTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition);

            m_TextStyleStackDepth = 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="styleDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        private int GetMarkupTagHashCode(uint[] styleDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int styleDefinitionLength = styleDefinition.Length;

            for (; readIndex < maxReadIndex && readIndex < styleDefinitionLength; readIndex++)
            {
                uint c = styleDefinition[readIndex];

                if (c == '>' || c == '=' || c == ' ')
                    return hashCode;

                hashCode = ((hashCode << 5) + hashCode) ^ (int)TMP_TextParsingUtilities.ToUpperASCIIFast(c);
            }

            return hashCode;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="styleDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        private int GetMarkupTagHashCode(TextBackingContainer styleDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int styleDefinitionLength = styleDefinition.Capacity;

            for (; readIndex < maxReadIndex && readIndex < styleDefinitionLength; readIndex++)
            {
                uint c = styleDefinition[readIndex];

                if (c == '>' || c == '=' || c == ' ')
                    return hashCode;

                hashCode = ((hashCode << 5) + hashCode) ^ (int)TMP_TextParsingUtilities.ToUpperASCIIFast(c);
            }

            return hashCode;
        }

        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        private int GetStyleHashCode(ref uint[] text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Length; i++)
            {
                if (text[i] == 34) continue;

                if (text[i] == 62) { closeIndex = i; break; }

                hashCode = (hashCode << 5) + hashCode ^ TMP_TextParsingUtilities.ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }

        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        private int GetStyleHashCode(ref TextBackingContainer text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Capacity; i++)
            {
                if (text[i] == 34) continue;

                if (text[i] == 62) { closeIndex = i; break; }

                hashCode = (hashCode << 5) + hashCode ^ TMP_TextParsingUtilities.ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }

        /// <summary>
        ///
        /// </summary>
        private void ResizeInternalArray <T>(ref T[] array)
        {
            int size = Mathf.NextPowerOfTwo(array.Length + 1);

            System.Array.Resize(ref array, size);
        }

        private void ResizeInternalArray<T>(ref T[] array, int size)
        {
            size = Mathf.NextPowerOfTwo(size + 1);

            System.Array.Resize(ref array, size);
        }


        private readonly decimal[] k_Power = { 5e-1m, 5e-2m, 5e-3m, 5e-4m, 5e-5m, 5e-6m, 5e-7m, 5e-8m, 5e-9m, 5e-10m };


        private void AddFloatToInternalTextBackingArray(float value, int padding, int precision, ref int writeIndex)
        {
            if (value < 0)
            {
                m_TextBackingArray[writeIndex] = '-';
                writeIndex += 1;
                value = -value;
            }

            decimal valueD = (decimal)value;

            if (padding == 0 && precision == 0)
                precision = 9;
            else
                valueD += k_Power[Mathf.Min(9, precision)];

            long integer = (long)valueD;

            AddIntegerToInternalTextBackingArray(integer, padding, ref writeIndex);

            if (precision > 0)
            {
                valueD -= integer;

                if (valueD != 0)
                {
                    m_TextBackingArray[writeIndex++] = '.';

                    for (int p = 0; p < precision; p++)
                    {
                        valueD *= 10;
                        long d = (long)valueD;

                        m_TextBackingArray[writeIndex++] = (char)(d + 48);
                        valueD -= d;

                        if (valueD == 0)
                            p = precision;
                    }
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="number"></param>
        /// <param name="padding"></param>
        /// <param name="writeIndex"></param>
        private void AddIntegerToInternalTextBackingArray(double number, int padding, ref int writeIndex)
        {
            int integralCount = 0;
            int i = writeIndex;

            do
            {
                m_TextBackingArray[i++] = (char)(number % 10 + 48);
                number /= 10;
                integralCount += 1;
            } while (number > 0.999999999999999d || integralCount < padding);

            int lastIndex = i;

            while (writeIndex + 1 < i)
            {
                i -= 1;
                uint t = m_TextBackingArray[writeIndex];
                m_TextBackingArray[writeIndex] = m_TextBackingArray[i];
                m_TextBackingArray[i] = t;
                writeIndex += 1;
            }
            writeIndex = lastIndex;
        }


        private string InternalTextBackingArrayToString()
        {
            char[] array = new char[m_TextBackingArray.Count];

            for (int i = 0; i < m_TextBackingArray.Capacity; i++)
            {
                char c = (char)m_TextBackingArray[i];

                if (c == 0)
                    break;

                array[i] = c;
            }

            m_IsTextBackingStringDirty = false;

            return new string(array);
        }


        /// <summary>
        /// Method used to determine the number of visible characters and required buffer allocations.
        /// </summary>
        /// <param name="unicodeChars"></param>
        /// <returns></returns>
        internal virtual int SetArraySizes(TextProcessingElement[] unicodeChars) { return 0; }


        /// <summary>
        /// Function used to evaluate the length of a text string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public virtual TMP_TextInfo GetTextInfo(string text) { return null; }


        internal void InsertNewLine(int i, float baseScale, float currentElementScale, float currentEmScale, float boldSpacingAdjustment, float characterSpacingAdjustment, float width, float lineGap, ref bool isMaxVisibleDescenderSet, ref float maxVisibleDescender)
        {
            k_InsertNewLineMarker.Begin();

            float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
            if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_isNewPage)
            {
                AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, baselineAdjustmentDelta);
                m_ElementDescender -= baselineAdjustmentDelta;
                m_lineOffset += baselineAdjustmentDelta;
            }

            float lineAscender = m_maxLineAscender - m_lineOffset;
            float lineDescender = m_maxLineDescender - m_lineOffset;

            m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
            if (!isMaxVisibleDescenderSet)
                maxVisibleDescender = m_ElementDescender;

            if (m_useMaxVisibleDescender && (m_characterCount >= m_maxVisibleCharacters || m_lineNumber >= m_maxVisibleLines))
                isMaxVisibleDescenderSet = true;

            m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex = m_firstCharacterOfLine;
            m_textInfo.lineInfo[m_lineNumber].firstVisibleCharacterIndex = m_firstVisibleCharacterOfLine = m_firstCharacterOfLine > m_firstVisibleCharacterOfLine ? m_firstCharacterOfLine : m_firstVisibleCharacterOfLine;
            int lastCharacterIndex = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex = m_lastCharacterOfLine = m_characterCount - 1 > 0 ? m_characterCount - 1 : 0;
            m_textInfo.lineInfo[m_lineNumber].lastVisibleCharacterIndex = m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

            m_textInfo.lineInfo[m_lineNumber].characterCount = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex + 1;
            m_textInfo.lineInfo[m_lineNumber].visibleCharacterCount = m_lineVisibleCharacterCount;
            m_textInfo.lineInfo[m_lineNumber].visibleSpaceCount = (m_textInfo.lineInfo[m_lineNumber].lastVisibleCharacterIndex + 1 - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex) - m_lineVisibleCharacterCount;
            m_textInfo.lineInfo[m_lineNumber].lineExtents.min = new Vector2(m_textInfo.characterInfo[m_firstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
            m_textInfo.lineInfo[m_lineNumber].lineExtents.max = new Vector2(m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].topRight.x, lineAscender);
            m_textInfo.lineInfo[m_lineNumber].length = m_textInfo.lineInfo[m_lineNumber].lineExtents.max.x;
            m_textInfo.lineInfo[m_lineNumber].width = width;

            float glyphAdjustment = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].adjustedHorizontalAdvance;
            float maxAdvanceOffset = (glyphAdjustment * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);
            float adjustedHorizontalAdvance = m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance + (m_isRightToLeft ? maxAdvanceOffset : - maxAdvanceOffset);
            m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance = adjustedHorizontalAdvance;

            m_textInfo.lineInfo[m_lineNumber].baseline = 0 - m_lineOffset;
            m_textInfo.lineInfo[m_lineNumber].ascender = lineAscender;
            m_textInfo.lineInfo[m_lineNumber].descender = lineDescender;
            m_textInfo.lineInfo[m_lineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

            m_firstCharacterOfLine = m_characterCount;
            m_lineVisibleCharacterCount = 0;
            m_lineVisibleSpaceCount = 0;

            SaveWordWrappingState(ref m_SavedLineState, i, m_characterCount - 1);

            m_lineNumber += 1;

            if (m_lineNumber >= m_textInfo.lineInfo.Length)
                ResizeLineExtents(m_lineNumber);

            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
            {
                float ascender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;
                float lineOffsetDelta = 0 - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                m_lineOffset += lineOffsetDelta;

                m_startOfLineAscender = ascender;
            }
            else
            {
                m_lineOffset += m_lineHeight + m_lineSpacing * currentEmScale;
            }

            m_maxLineAscender = k_LargeNegativeFloat;
            m_maxLineDescender = k_LargePositiveFloat;

            m_xAdvance = 0 + tag_Indent;
            k_InsertNewLineMarker.End();
        }


        /// <summary>
        /// Save the State of various variables used in the mesh creation loop in conjunction with Word Wrapping
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        internal void SaveWordWrappingState(ref WordWrapState state, int index, int count)
        {
            state.currentFontAsset = m_currentFontAsset;
            state.currentSpriteAsset = m_currentSpriteAsset;
            state.currentMaterial = m_currentMaterial;
            state.currentMaterialIndex = m_currentMaterialIndex;

            state.previous_WordBreak = index;
            state.total_CharacterCount = count;
            state.visible_CharacterCount = m_lineVisibleCharacterCount;
            state.visibleSpaceCount = m_lineVisibleSpaceCount;
            state.visible_LinkCount = m_textInfo.linkCount;

            state.firstCharacterIndex = m_firstCharacterOfLine;
            state.firstVisibleCharacterIndex = m_firstVisibleCharacterOfLine;
            state.lastVisibleCharIndex = m_lastVisibleCharacterOfLine;

            state.fontStyle = m_FontStyleInternal;
            state.italicAngle = m_ItalicAngle;
            state.fontScaleMultiplier = m_fontScaleMultiplier;
            state.currentFontSize = m_currentFontSize;

            state.xAdvance = m_xAdvance;
            state.maxCapHeight = m_maxCapHeight;
            state.maxAscender = m_maxTextAscender;
            state.maxDescender = m_ElementDescender;
            state.startOfLineAscender = m_startOfLineAscender;
            state.maxLineAscender = m_maxLineAscender;
            state.maxLineDescender = m_maxLineDescender;
            state.pageAscender = m_PageAscender;

            state.preferredWidth = m_preferredWidth;
            state.preferredHeight = m_preferredHeight;
            state.renderedWidth = m_RenderedWidth;
            state.renderedHeight = m_RenderedHeight;
            state.meshExtents = m_meshExtents;

            state.lineNumber = m_lineNumber;
            state.lineOffset = m_lineOffset;
            state.baselineOffset = m_baselineOffset;
            state.isDrivenLineSpacing = m_IsDrivenLineSpacing;
            state.lastBaseGlyphIndex = m_LastBaseGlyphIndex;

            state.cSpace = m_cSpacing;
            state.mSpace = m_monoSpacing;

            state.horizontalAlignment = m_lineJustification;
            state.marginLeft = m_marginLeft;
            state.marginRight = m_marginRight;

            state.vertexColor = m_htmlColor;
            state.underlineColor = m_underlineColor;
            state.strikethroughColor = m_strikethroughColor;
            state.highlightState = m_HighlightState;

            state.isNonBreakingSpace = m_isNonBreakingSpace;
            state.tagNoParsing = tag_NoParsing;

            state.fxRotation = m_FXRotation;
            state.fxScale = m_FXScale;

            state.basicStyleStack = m_fontStyleStack;
            state.italicAngleStack = m_ItalicAngleStack;
            state.colorStack = m_colorStack;
            state.underlineColorStack = m_underlineColorStack;
            state.strikethroughColorStack = m_strikethroughColorStack;
            state.highlightStateStack = m_HighlightStateStack;
            state.colorGradientStack = m_colorGradientStack;
            state.sizeStack = m_sizeStack;
            state.indentStack = m_indentStack;
            state.fontWeightStack = m_FontWeightStack;

            state.baselineStack = m_baselineOffsetStack;
            state.actionStack = m_actionStack;
            state.materialReferenceStack = m_materialReferenceStack;
            state.lineJustificationStack = m_lineJustificationStack;

            state.spriteAnimationID = m_spriteAnimationID;

            if (m_lineNumber < m_textInfo.lineInfo.Length)
                state.lineInfo = m_textInfo.lineInfo[m_lineNumber];
        }


        /// <summary>
        /// Restore the State of various variables used in the mesh creation loop.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal int RestoreWordWrappingState(ref WordWrapState state)
        {
            int index = state.previous_WordBreak;

            m_currentFontAsset = state.currentFontAsset;
            m_currentSpriteAsset = state.currentSpriteAsset;
            m_currentMaterial = state.currentMaterial;
            m_currentMaterialIndex = state.currentMaterialIndex;

            m_characterCount = state.total_CharacterCount + 1;
            m_lineVisibleCharacterCount = state.visible_CharacterCount;
            m_lineVisibleSpaceCount = state.visibleSpaceCount;
            m_textInfo.linkCount = state.visible_LinkCount;

            m_firstCharacterOfLine = state.firstCharacterIndex;
            m_firstVisibleCharacterOfLine = state.firstVisibleCharacterIndex;
            m_lastVisibleCharacterOfLine = state.lastVisibleCharIndex;

            m_FontStyleInternal = state.fontStyle;
            m_ItalicAngle = state.italicAngle;
            m_fontScaleMultiplier = state.fontScaleMultiplier;
            m_currentFontSize = state.currentFontSize;

            m_xAdvance = state.xAdvance;
            m_maxCapHeight = state.maxCapHeight;
            m_maxTextAscender = state.maxAscender;
            m_ElementDescender = state.maxDescender;
            m_startOfLineAscender = state.startOfLineAscender;
            m_maxLineAscender = state.maxLineAscender;
            m_maxLineDescender = state.maxLineDescender;
            m_PageAscender = state.pageAscender;

            m_preferredWidth = state.preferredWidth;
            m_preferredHeight = state.preferredHeight;
            m_RenderedWidth = state.renderedWidth;
            m_RenderedHeight = state.renderedHeight;
            m_meshExtents = state.meshExtents;

            m_lineNumber = state.lineNumber;
            m_lineOffset = state.lineOffset;
            m_baselineOffset = state.baselineOffset;
            m_IsDrivenLineSpacing = state.isDrivenLineSpacing;
            m_LastBaseGlyphIndex = state.lastBaseGlyphIndex;

            m_cSpacing = state.cSpace;
            m_monoSpacing = state.mSpace;

            m_lineJustification = state.horizontalAlignment;
            m_marginLeft = state.marginLeft;
            m_marginRight = state.marginRight;

            m_htmlColor = state.vertexColor;
            m_underlineColor = state.underlineColor;
            m_strikethroughColor = state.strikethroughColor;
            m_HighlightState = state.highlightState;

            m_isNonBreakingSpace = state.isNonBreakingSpace;
            tag_NoParsing = state.tagNoParsing;

            m_FXRotation = state.fxRotation;
            m_FXScale = state.fxScale;

            m_fontStyleStack = state.basicStyleStack;
            m_ItalicAngleStack = state.italicAngleStack;
            m_colorStack = state.colorStack;
            m_underlineColorStack = state.underlineColorStack;
            m_strikethroughColorStack = state.strikethroughColorStack;
            m_HighlightStateStack = state.highlightStateStack;
            m_colorGradientStack = state.colorGradientStack;
            m_sizeStack = state.sizeStack;
            m_indentStack = state.indentStack;
            m_FontWeightStack = state.fontWeightStack;

            m_baselineOffsetStack = state.baselineStack;
            m_actionStack = state.actionStack;
            m_materialReferenceStack = state.materialReferenceStack;
            m_lineJustificationStack = state.lineJustificationStack;

            m_spriteAnimationID = state.spriteAnimationID;

            if (m_lineNumber < m_textInfo.lineInfo.Length)
                m_textInfo.lineInfo[m_lineNumber] = state.lineInfo;

            return index;
        }


        /// <summary>
        /// Store vertex information for each character.
        /// </summary>
        /// <param name="style_padding">Style_padding.</param>
        /// <param name="vertexColor">Vertex color.</param>
        protected virtual void SaveGlyphVertexInfo(float padding, float style_padding, Color32 vertexColor)
        {
            #region Setup Mesh Vertices
            m_textInfo.characterInfo[m_characterCount].vertex_BL.position = m_textInfo.characterInfo[m_characterCount].bottomLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.position = m_textInfo.characterInfo[m_characterCount].topLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.position = m_textInfo.characterInfo[m_characterCount].topRight;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.position = m_textInfo.characterInfo[m_characterCount].bottomRight;
            #endregion


            #region Setup Vertex Colors

            vertexColor.a = m_fontColor32.a < vertexColor.a ? m_fontColor32.a : vertexColor.a;

            #if TEXTCORE_FONT_ENGINE_1_5_OR_NEWER
            bool isColorGlyph = ((GlyphRasterModes)m_currentFontAsset.m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_COLOR) == GlyphRasterModes.RASTER_MODE_COLOR;
            #else
            bool isColorGlyph = false;
            #endif

            if (!m_enableVertexGradient || isColorGlyph)
            {
                vertexColor = isColorGlyph ? new Color32(255, 255, 255, vertexColor.a) : vertexColor;

                m_textInfo.characterInfo[m_characterCount].vertex_BL.color = vertexColor;
                m_textInfo.characterInfo[m_characterCount].vertex_TL.color = vertexColor;
                m_textInfo.characterInfo[m_characterCount].vertex_TR.color = vertexColor;
                m_textInfo.characterInfo[m_characterCount].vertex_BR.color = vertexColor;
            }
            else
            {
                if (!m_overrideHtmlColors && m_colorStack.index > 1)
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color = vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color = vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color = vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color = vertexColor;
                }
                else
                {
                    if (m_fontColorGradientPreset != null)
                    {
                        m_textInfo.characterInfo[m_characterCount].vertex_BL.color = m_fontColorGradientPreset.bottomLeft * vertexColor;
                        m_textInfo.characterInfo[m_characterCount].vertex_TL.color = m_fontColorGradientPreset.topLeft * vertexColor;
                        m_textInfo.characterInfo[m_characterCount].vertex_TR.color = m_fontColorGradientPreset.topRight * vertexColor;
                        m_textInfo.characterInfo[m_characterCount].vertex_BR.color = m_fontColorGradientPreset.bottomRight * vertexColor;
                    }
                    else
                    {
                        m_textInfo.characterInfo[m_characterCount].vertex_BL.color = m_fontColorGradient.bottomLeft * vertexColor;
                        m_textInfo.characterInfo[m_characterCount].vertex_TL.color = m_fontColorGradient.topLeft * vertexColor;
                        m_textInfo.characterInfo[m_characterCount].vertex_TR.color = m_fontColorGradient.topRight * vertexColor;
                        m_textInfo.characterInfo[m_characterCount].vertex_BR.color = m_fontColorGradient.bottomRight * vertexColor;
                    }
                }
            }

            if (m_colorGradientPreset != null && !isColorGlyph)
            {
                if (m_colorGradientPresetIsTinted)
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color *= m_colorGradientPreset.bottomLeft;
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color *= m_colorGradientPreset.topLeft;
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color *= m_colorGradientPreset.topRight;
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color *= m_colorGradientPreset.bottomRight;
                }
                else
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color = m_colorGradientPreset.bottomLeft.MinAlpha(vertexColor);
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color = m_colorGradientPreset.topLeft.MinAlpha(vertexColor);
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color = m_colorGradientPreset.topRight.MinAlpha(vertexColor);
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color = m_colorGradientPreset.bottomRight.MinAlpha(vertexColor);
                }
            }
            #endregion

            if (!m_isSDFShader)
                style_padding = 0f;


            #region Setup UVs

            Glyph altGlyph = m_textInfo.characterInfo[m_characterCount].alternativeGlyph;
            GlyphRect glyphRect = altGlyph == null ? m_cached_TextElement.m_Glyph.glyphRect : altGlyph.glyphRect;

            Vector2 uv0;
            uv0.x = (glyphRect.x - padding - style_padding) / m_currentFontAsset.m_AtlasWidth;
            uv0.y = (glyphRect.y - padding - style_padding) / m_currentFontAsset.m_AtlasHeight;

            Vector2 uv1;
            uv1.x = uv0.x;
            uv1.y = (glyphRect.y + padding + style_padding + glyphRect.height) / m_currentFontAsset.m_AtlasHeight;

            Vector2 uv2;
            uv2.x = (glyphRect.x + padding + style_padding + glyphRect.width) / m_currentFontAsset.m_AtlasWidth;
            uv2.y = uv1.y;

            Vector2 uv3;
            uv3.x = uv2.x;
            uv3.y = uv0.y;

            m_textInfo.characterInfo[m_characterCount].vertex_BL.uv = uv0;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.uv = uv1;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.uv = uv2;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.uv = uv3;
            #endregion Setup UVs


            #region Setup Normals & Tangents

            #endregion end Normals & Tangents
        }


        /// <summary>
        /// Store vertex information for each sprite.
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="style_padding"></param>
        /// <param name="vertexColor"></param>
        protected virtual void SaveSpriteVertexInfo(Color32 vertexColor)
        {
            #region Setup Mesh Vertices
            m_textInfo.characterInfo[m_characterCount].vertex_BL.position = m_textInfo.characterInfo[m_characterCount].bottomLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.position = m_textInfo.characterInfo[m_characterCount].topLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.position = m_textInfo.characterInfo[m_characterCount].topRight;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.position = m_textInfo.characterInfo[m_characterCount].bottomRight;
            #endregion

            if (m_tintAllSprites) m_tintSprite = true;
            Color32 spriteColor = m_tintSprite ? m_spriteColor.Multiply(vertexColor) : m_spriteColor;
            spriteColor.a = spriteColor.a < m_fontColor32.a ? spriteColor.a < vertexColor.a ? spriteColor.a : vertexColor.a : m_fontColor32.a;

            Color32 c0 = spriteColor;
            Color32 c1 = spriteColor;
            Color32 c2 = spriteColor;
            Color32 c3 = spriteColor;

            if (m_enableVertexGradient)
            {
                if (m_fontColorGradientPreset != null)
                {
                    c0 = m_tintSprite ? c0.Multiply(m_fontColorGradientPreset.bottomLeft) : c0;
                    c1 = m_tintSprite ? c1.Multiply(m_fontColorGradientPreset.topLeft) : c1;
                    c2 = m_tintSprite ? c2.Multiply(m_fontColorGradientPreset.topRight) : c2;
                    c3 = m_tintSprite ? c3.Multiply(m_fontColorGradientPreset.bottomRight) : c3;
                }
                else
                {
                    c0 = m_tintSprite ? c0.Multiply(m_fontColorGradient.bottomLeft) : c0;
                    c1 = m_tintSprite ? c1.Multiply(m_fontColorGradient.topLeft) : c1;
                    c2 = m_tintSprite ? c2.Multiply(m_fontColorGradient.topRight) : c2;
                    c3 = m_tintSprite ? c3.Multiply(m_fontColorGradient.bottomRight) : c3;
                }
            }

            if (m_colorGradientPreset != null)
            {
                c0 = m_tintSprite ? c0.Multiply(m_colorGradientPreset.bottomLeft) : c0;
                c1 = m_tintSprite ? c1.Multiply(m_colorGradientPreset.topLeft) : c1;
                c2 = m_tintSprite ? c2.Multiply(m_colorGradientPreset.topRight) : c2;
                c3 = m_tintSprite ? c3.Multiply(m_colorGradientPreset.bottomRight) : c3;
            }

            m_tintSprite = false;

            m_textInfo.characterInfo[m_characterCount].vertex_BL.color = c0;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.color = c1;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.color = c2;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.color = c3;


            #region Setup UVs
            GlyphRect glyphRect = m_cached_TextElement.m_Glyph.glyphRect;

            Vector2 uv0 = new Vector2((float)glyphRect.x / m_currentSpriteAsset.spriteSheet.width, (float)glyphRect.y / m_currentSpriteAsset.spriteSheet.height);
            Vector2 uv1 = new Vector2(uv0.x, (float)(glyphRect.y + glyphRect.height) / m_currentSpriteAsset.spriteSheet.height);
            Vector2 uv2 = new Vector2((float)(glyphRect.x + glyphRect.width) / m_currentSpriteAsset.spriteSheet.width, uv1.y);
            Vector2 uv3 = new Vector2(uv2.x, uv0.y);

            m_textInfo.characterInfo[m_characterCount].vertex_BL.uv = uv0;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.uv = uv1;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.uv = uv2;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.uv = uv3;
            #endregion Setup UVs


            #region Setup Normals & Tangents

            #endregion end Normals & Tangents

        }


        /// <summary>
        /// Store vertex attributes into the appropriate TMP_MeshInfo.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="index_X4"></param>
        protected virtual void FillCharacterVertexBuffers(int i)
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            int index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            if (index_X4 >= m_textInfo.meshInfo[materialIndex].vertices.Length)
                m_textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((index_X4 + 4) / 4));


            TMP_CharacterInfo[] characterInfoArray = m_textInfo.characterInfo;
            m_textInfo.characterInfo[i].vertexIndex = index_X4;

            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;


            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;


            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;


            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_BL.color.GammaToLinear() : characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_TL.color.GammaToLinear() : characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_TR.color.GammaToLinear() : characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_BR.color.GammaToLinear() : characterInfoArray[i].vertex_BR.color;

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + 4;
        }


        protected virtual void FillCharacterVertexBuffers(int i, bool isVolumetric)
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            int index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            if (index_X4 >= m_textInfo.meshInfo[materialIndex].vertices.Length)
                m_textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((index_X4 + (isVolumetric ? 8 : 4)) / 4));

            TMP_CharacterInfo[] characterInfoArray = m_textInfo.characterInfo;
            m_textInfo.characterInfo[i].vertexIndex = index_X4;

            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;

            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;

            if (isVolumetric)
            {
                m_textInfo.meshInfo[materialIndex].uvs0[4 + index_X4] = characterInfoArray[i].vertex_BL.uv;
                m_textInfo.meshInfo[materialIndex].uvs0[5 + index_X4] = characterInfoArray[i].vertex_TL.uv;
                m_textInfo.meshInfo[materialIndex].uvs0[6 + index_X4] = characterInfoArray[i].vertex_TR.uv;
                m_textInfo.meshInfo[materialIndex].uvs0[7 + index_X4] = characterInfoArray[i].vertex_BR.uv;
            }


            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;

            if (isVolumetric)
            {
                m_textInfo.meshInfo[materialIndex].uvs2[4 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
                m_textInfo.meshInfo[materialIndex].uvs2[5 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
                m_textInfo.meshInfo[materialIndex].uvs2[6 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
                m_textInfo.meshInfo[materialIndex].uvs2[7 + index_X4] = characterInfoArray[i].vertex_BR.uv2;
            }


            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = characterInfoArray[i].vertex_BR.color;

            if (isVolumetric)
            {
                Color32 backColor = new Color32(255, 255, 128, 255);
                m_textInfo.meshInfo[materialIndex].colors32[4 + index_X4] = backColor;
                m_textInfo.meshInfo[materialIndex].colors32[5 + index_X4] = backColor;
                m_textInfo.meshInfo[materialIndex].colors32[6 + index_X4] = backColor;
                m_textInfo.meshInfo[materialIndex].colors32[7 + index_X4] = backColor;
            }

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + (!isVolumetric ? 4 : 8);
        }


        /// <summary>
        /// Fill Vertex Buffers for Sprites
        /// </summary>
        /// <param name="i"></param>
        /// <param name="spriteIndex_X4"></param>
        protected virtual void FillSpriteVertexBuffers(int i)
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            int index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            if (index_X4 >= m_textInfo.meshInfo[materialIndex].vertices.Length)
                m_textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((index_X4 + 4) / 4));

            TMP_CharacterInfo[] characterInfoArray = m_textInfo.characterInfo;
            m_textInfo.characterInfo[i].vertexIndex = index_X4;

            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;


            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;


            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;


            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_BL.color.GammaToLinear() : characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_TL.color.GammaToLinear() : characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_TR.color.GammaToLinear() : characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = m_ConvertToLinearSpace ? characterInfoArray[i].vertex_BR.color.GammaToLinear() : characterInfoArray[i].vertex_BR.color;

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + 4;
        }


        /// <summary>
        /// Method to add the underline geometry.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="maxScale"></param>
        /// <param name="underlineColor"></param>
        protected virtual void DrawUnderlineMesh(Vector3 start, Vector3 end, ref int index, float startScale, float endScale, float maxScale, float sdfScale, Color32 underlineColor)
        {
            GetUnderlineSpecialCharacter(m_fontAsset);

            if (m_Underline.character == null)
            {
                if (!TMP_Settings.warningsDisabled)
                    Debug.LogWarning("Unable to add underline or strikethrough since the character [0x5F] used by these features is not present in the Font Asset assigned to this text object.", this);

                return;
            }

            int underlineMaterialIndex = m_Underline.materialIndex;

            int verticesCount = index + 12;

            if (verticesCount > m_textInfo.meshInfo[underlineMaterialIndex].vertices.Length)
            {
                m_textInfo.meshInfo[underlineMaterialIndex].ResizeMeshInfo(verticesCount / 4);
            }

            start.y = Mathf.Min(start.y, end.y);
            end.y = Mathf.Min(start.y, end.y);

            GlyphMetrics underlineGlyphMetrics = m_Underline.character.glyph.metrics;
            GlyphRect underlineGlyphRect = m_Underline.character.glyph.glyphRect;

            float segmentWidth = underlineGlyphMetrics.width / 2 * maxScale;

            if (end.x - start.x < underlineGlyphMetrics.width * maxScale)
            {
                segmentWidth = (end.x - start.x) / 2f;
            }

            float startPadding = m_padding * startScale / maxScale;
            float endPadding = m_padding * endScale / maxScale;

            float underlineThickness = m_Underline.fontAsset.faceInfo.underlineThickness;

            #region UNDERLINE VERTICES
            Vector3[] vertices = m_textInfo.meshInfo[underlineMaterialIndex].vertices;

            vertices[index + 0] = start + new Vector3(0, 0 - (underlineThickness + m_padding) * maxScale, 0);
            vertices[index + 1] = start + new Vector3(0, m_padding * maxScale, 0);
            vertices[index + 2] = vertices[index + 1] + new Vector3(segmentWidth, 0, 0);
            vertices[index + 3] = vertices[index + 0] + new Vector3(segmentWidth, 0, 0);

            vertices[index + 4] = vertices[index + 3];
            vertices[index + 5] = vertices[index + 2];
            vertices[index + 6] = end + new Vector3(-segmentWidth, m_padding * maxScale, 0);
            vertices[index + 7] = end + new Vector3(-segmentWidth, -(underlineThickness + m_padding) * maxScale, 0);

            vertices[index + 8] = vertices[index + 7];
            vertices[index + 9] = vertices[index + 6];
            vertices[index + 10] = end + new Vector3(0, m_padding * maxScale, 0);
            vertices[index + 11] = end + new Vector3(0, -(underlineThickness + m_padding) * maxScale, 0);

            #endregion

            #region HANDLE UV0
            Vector4[] uvs0 = m_textInfo.meshInfo[underlineMaterialIndex].uvs0;

            int atlasWidth = m_Underline.fontAsset.atlasWidth;
            int atlasHeight = m_Underline.fontAsset.atlasHeight;

            float xScale = Mathf.Abs(sdfScale);

            Vector4 uv0 = new Vector4((underlineGlyphRect.x - startPadding) / atlasWidth, (underlineGlyphRect.y - m_padding) / atlasHeight, 0, xScale);
            Vector4 uv1 = new Vector4(uv0.x, (underlineGlyphRect.y + underlineGlyphRect.height + m_padding) / atlasHeight, 0, xScale);
            Vector4 uv2 = new Vector4((underlineGlyphRect.x - startPadding + (float)underlineGlyphRect.width / 2) / atlasWidth, uv1.y, 0, xScale);
            Vector4 uv3 = new Vector4(uv2.x, uv0.y, 0, xScale);
            Vector4 uv4 = new Vector4((underlineGlyphRect.x + endPadding + (float)underlineGlyphRect.width / 2) / atlasWidth, uv1.y, 0, xScale);
            Vector4 uv5 = new Vector4(uv4.x, uv0.y, 0, xScale);
            Vector4 uv6 = new Vector4((underlineGlyphRect.x + endPadding + underlineGlyphRect.width) / atlasWidth, uv1.y, 0, xScale);
            Vector4 uv7 = new Vector4(uv6.x, uv0.y, 0, xScale);

            uvs0[0 + index] = uv0;
            uvs0[1 + index] = uv1;
            uvs0[2 + index] = uv2;
            uvs0[3 + index] = uv3;

            uvs0[4 + index] = new Vector4(uv2.x - uv2.x * 0.001f, uv0.y, 0, xScale);
            uvs0[5 + index] = new Vector4(uv2.x - uv2.x * 0.001f, uv1.y, 0, xScale);
            uvs0[6 + index] = new Vector4(uv2.x + uv2.x * 0.001f, uv1.y, 0, xScale);
            uvs0[7 + index] = new Vector4(uv2.x + uv2.x * 0.001f, uv0.y, 0, xScale);

            uvs0[8 + index] = uv5;
            uvs0[9 + index] = uv4;
            uvs0[10 + index] = uv6;
            uvs0[11 + index] = uv7;
            #endregion

            #region HANDLE UV2 - SDF SCALE

            float min_UvX = 0;
            float max_UvX = (vertices[index + 2].x - start.x) / (end.x - start.x);

            Vector2[] uvs2 = m_textInfo.meshInfo[underlineMaterialIndex].uvs2;

            uvs2[0 + index] = new Vector2 (0, 0);
            uvs2[1 + index] = new Vector2(0, 1);
            uvs2[2 + index] = new Vector2(max_UvX, 1);
            uvs2[3 + index] = new Vector2(max_UvX, 0);

            min_UvX = (vertices[index + 4].x - start.x) / (end.x - start.x);
            max_UvX = (vertices[index + 6].x - start.x) / (end.x - start.x);

            uvs2[4 + index] = new Vector2(min_UvX, 0);
            uvs2[5 + index] = new Vector2(min_UvX, 1);
            uvs2[6 + index] = new Vector2(max_UvX, 1);
            uvs2[7 + index] = new Vector2(max_UvX, 0);

            min_UvX = (vertices[index + 8].x - start.x) / (end.x - start.x);

            uvs2[8 + index] = new Vector2(min_UvX, 0);
            uvs2[9 + index] = new Vector2(min_UvX, 1);
            uvs2[10 + index] = new Vector2(1, 1);
            uvs2[11 + index] = new Vector2(1, 0);
            #endregion

            #region UNDERLINE VERTEX COLORS

            underlineColor.a = m_fontColor32.a < underlineColor.a ? m_fontColor32.a : underlineColor.a;

            Color32[] colors32 = m_textInfo.meshInfo[underlineMaterialIndex].colors32;
            colors32[0 + index] = underlineColor;
            colors32[1 + index] = underlineColor;
            colors32[2 + index] = underlineColor;
            colors32[3 + index] = underlineColor;

            colors32[4 + index] = underlineColor;
            colors32[5 + index] = underlineColor;
            colors32[6 + index] = underlineColor;
            colors32[7 + index] = underlineColor;

            colors32[8 + index] = underlineColor;
            colors32[9 + index] = underlineColor;
            colors32[10 + index] = underlineColor;
            colors32[11 + index] = underlineColor;
            #endregion

            index += 12;
        }


        protected virtual void DrawTextHighlight(Vector3 start, Vector3 end, ref int index, Color32 highlightColor)
        {
            if (m_Underline.character == null)
            {
                GetUnderlineSpecialCharacter(m_fontAsset);

                if (m_Underline.character == null)
                {
                    if (!TMP_Settings.warningsDisabled)
                        Debug.LogWarning("Unable to add highlight since the primary Font Asset doesn't contain the underline character.", this);

                    return;
                }
            }

            int underlineMaterialIndex = m_Underline.materialIndex;

            int verticesCount = index + 4;

            if (verticesCount > m_textInfo.meshInfo[underlineMaterialIndex].vertices.Length)
            {
                m_textInfo.meshInfo[underlineMaterialIndex].ResizeMeshInfo(verticesCount / 4);
            }

            #region HIGHLIGHT VERTICES
            Vector3[] vertices = m_textInfo.meshInfo[underlineMaterialIndex].vertices;

            vertices[index + 0] = start;
            vertices[index + 1] = new Vector3(start.x, end.y, 0);
            vertices[index + 2] = end;
            vertices[index + 3] = new Vector3(end.x, start.y, 0);

            #endregion

            #region HANDLE UV0
            Vector4[] uvs0 = m_textInfo.meshInfo[underlineMaterialIndex].uvs0;

            int atlasWidth = m_Underline.fontAsset.atlasWidth;
            int atlasHeight = m_Underline.fontAsset.atlasHeight;
            GlyphRect glyphRect = m_Underline.character.glyph.glyphRect;

            Vector2 uvGlyphCenter = new Vector2((glyphRect.x + (float)glyphRect.width / 2) / atlasWidth, (glyphRect.y + (float)glyphRect.height / 2) / atlasHeight);
            Vector2 uvTexelSize = new Vector2(1.0f / atlasWidth, 1.0f / atlasHeight);

            uvs0[index + 0] = uvGlyphCenter - uvTexelSize;
            uvs0[index + 1] = uvGlyphCenter + new Vector2(-uvTexelSize.x, uvTexelSize.y);
            uvs0[index + 2] = uvGlyphCenter + uvTexelSize;
            uvs0[index + 3] = uvGlyphCenter + new Vector2(uvTexelSize.x, -uvTexelSize.y);

            #endregion

            #region HANDLE UV2 - SDF SCALE
            Vector2[] uvs2 = m_textInfo.meshInfo[underlineMaterialIndex].uvs2;
            Vector2 customUV = new Vector2(0, 1);
            uvs2[index + 0] = customUV;
            uvs2[index + 1] = customUV;
            uvs2[index + 2] = customUV;
            uvs2[index + 3] = customUV;
            #endregion

            #region HIGHLIGHT VERTEX COLORS

            highlightColor.a = m_fontColor32.a < highlightColor.a ? m_fontColor32.a : highlightColor.a;

            Color32[] colors32 = m_textInfo.meshInfo[underlineMaterialIndex].colors32;
            colors32[index + 0] = highlightColor;
            colors32[index + 1] = highlightColor;
            colors32[index + 2] = highlightColor;
            colors32[index + 3] = highlightColor;
            #endregion

            index += 4;
        }


        /// <summary>
        /// Internal function used to load the default settings of text objects.
        /// </summary>
        protected void LoadDefaultSettings()
        {
            if (m_fontSize == -99 || m_isWaitingOnResourceLoad)
            {
                m_rectTransform = this.rectTransform;

                if (TMP_Settings.autoSizeTextContainer)
                {
                    autoSizeTextContainer = true;
                }
                else
                {
                    if (GetType() == typeof(TextMeshPro))
                    {
                        if (m_rectTransform.sizeDelta == new Vector2(100, 100))
                            m_rectTransform.sizeDelta = TMP_Settings.defaultTextMeshProTextContainerSize;
                    }
                    else
                    {
                        if (m_rectTransform.sizeDelta == new Vector2(100, 100))
                            m_rectTransform.sizeDelta = TMP_Settings.defaultTextMeshProUITextContainerSize;
                    }

                }

                m_TextWrappingMode = TMP_Settings.textWrappingMode;

                m_ActiveFontFeatures = new List<OTL_FeatureTag>(TMP_Settings.fontFeatures);

                m_enableExtraPadding = TMP_Settings.enableExtraPadding;
                m_tintAllSprites = TMP_Settings.enableTintAllSprites;
                m_parseCtrlCharacters = TMP_Settings.enableParseEscapeCharacters;
                m_fontSize = m_fontSizeBase = TMP_Settings.defaultFontSize;
                m_fontSizeMin = m_fontSize * TMP_Settings.defaultTextAutoSizingMinRatio;
                m_fontSizeMax = m_fontSize * TMP_Settings.defaultTextAutoSizingMaxRatio;
                m_isWaitingOnResourceLoad = false;
                raycastTarget = TMP_Settings.enableRaycastTarget;
                m_IsTextObjectScaleStatic = TMP_Settings.isTextObjectScaleStatic;
            }
            else
            {
                if ((int)m_textAlignment < 0xFF)
                    m_textAlignment = TMP_Compatibility.ConvertTextAlignmentEnumValues(m_textAlignment);

                if (m_ActiveFontFeatures.Count == 1 && m_ActiveFontFeatures[0] == 0)
                {
                    m_ActiveFontFeatures.Clear();

                    if (m_enableKerning)
                        m_ActiveFontFeatures.Add(OTL_FeatureTag.kern);
                }
            }

            if (m_textAlignment != TextAlignmentOptions.Converted)
            {
                m_HorizontalAlignment = (HorizontalAlignmentOptions)((int)m_textAlignment & 0xFF);
                m_VerticalAlignment = (VerticalAlignmentOptions)((int)m_textAlignment & 0xFF00);
                m_textAlignment = TextAlignmentOptions.Converted;
            }
        }


        /// <summary>
        /// Method used to find and cache references to the Underline and Ellipsis characters.
        /// </summary>
        /// <param name=""></param>
        protected void GetSpecialCharacters(TMP_FontAsset fontAsset)
        {
            GetEllipsisSpecialCharacter(fontAsset);

            GetUnderlineSpecialCharacter(fontAsset);
        }


        protected void GetEllipsisSpecialCharacter(TMP_FontAsset fontAsset)
        {
            bool isUsingAlternativeTypeface;

            TMP_Character character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(0x2026, fontAsset, false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

            if (character == null)
            {
                if (fontAsset.m_FallbackFontAssetTable != null && fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(0x2026, fontAsset, fontAsset.m_FallbackFontAssetTable, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
            }

            if (character == null)
            {
                if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(0x2026, fontAsset, TMP_Settings.fallbackFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
            }

            if (character == null)
            {
                if (TMP_Settings.defaultFontAsset != null)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(0x2026, TMP_Settings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
            }

            if (character != null)
                m_Ellipsis = new SpecialCharacter(character, 0);
        }


        protected void GetUnderlineSpecialCharacter(TMP_FontAsset fontAsset)
        {
            bool isUsingAlternativeTypeface;

            TMP_Character character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(0x5F, fontAsset, false, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);

            if (character != null)
                m_Underline = new SpecialCharacter(character, 0);
        }


        /// <summary>
        /// Replace a given number of characters (tag) in the array with a new character and shift subsequent characters in the array.
        /// </summary>
        /// <param name="chars">Array which contains the text.</param>
        /// <param name="insertionIndex">The index of where the new character will be inserted</param>
        /// <param name="tagLength">Length of the tag being replaced.</param>
        /// <param name="c">The replacement character.</param>
        protected void ReplaceTagWithCharacter(int[] chars, int insertionIndex, int tagLength, char c)
        {
            chars[insertionIndex] = c;

            for (int i = insertionIndex + tagLength; i < chars.Length; i++)
            {
                chars[i - 3] = chars[i];
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected TMP_FontAsset GetFontAssetForWeight(int fontWeight)
        {
            bool isItalic = (m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic || (m_fontStyle & FontStyles.Italic) == FontStyles.Italic;

            TMP_FontAsset fontAsset = null;

            int weightIndex = fontWeight / 100;

            if (isItalic)
                fontAsset = m_currentFontAsset.fontWeightTable[weightIndex].italicTypeface;
            else
                fontAsset = m_currentFontAsset.fontWeightTable[weightIndex].regularTypeface;

            return fontAsset;
        }

        internal TMP_TextElement GetTextElement(uint unicode, TMP_FontAsset fontAsset, FontStyles fontStyle, FontWeight fontWeight, out bool isUsingAlternativeTypeface)
        {
            TMP_Character character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight, isUsingAlternativeTypeface);

                return character;
            }

            if (fontAsset.instanceID != m_fontAsset.instanceID)
            {
                character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, m_fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight, isUsingAlternativeTypeface);

                    return character;
                }

                if (m_fontAsset.m_FallbackFontAssetTable != null && m_fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, m_fontAsset.m_FallbackFontAssetTable, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight, isUsingAlternativeTypeface);

                    return character;
                }
            }

            if (m_spriteAsset != null)
            {
                TMP_SpriteCharacter spriteCharacter = TMP_FontAssetUtilities.GetSpriteCharacterFromSpriteAsset(unicode, m_spriteAsset, true);

                if (spriteCharacter != null)
                    return spriteCharacter;
            }

            if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, TMP_Settings.fallbackFontAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight, isUsingAlternativeTypeface);

                return character;
            }

            if (TMP_Settings.defaultFontAsset != null)
                character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, TMP_Settings.defaultFontAsset, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                fontAsset.AddCharacterToLookupCache(unicode, character, fontStyle, fontWeight, isUsingAlternativeTypeface);

                return character;
            }

            if (TMP_Settings.defaultSpriteAsset != null)
            {
                TMP_SpriteCharacter spriteCharacter = TMP_FontAssetUtilities.GetSpriteCharacterFromSpriteAsset(unicode, TMP_Settings.defaultSpriteAsset, true);

                if (spriteCharacter != null)
                    return spriteCharacter;
            }

            if (fontStyle != FontStyles.Normal || fontWeight != FontWeight.Regular)
            {
                character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    fontAsset.AddCharacterToLookupCache(unicode, character, FontStyles.Normal, FontWeight.Regular, isUsingAlternativeTypeface);

                    return character;
                }

                if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, TMP_Settings.fallbackFontAssets, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    fontAsset.AddCharacterToLookupCache(unicode, character, FontStyles.Normal, FontWeight.Regular, isUsingAlternativeTypeface);

                    return character;
                }

                if (TMP_Settings.defaultFontAsset != null)
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, TMP_Settings.defaultFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    fontAsset.AddCharacterToLookupCache(unicode, character, FontStyles.Normal, FontWeight.Regular, isUsingAlternativeTypeface);

                    return character;
                }
            }

            return null;
        }


        /// <summary>
        /// Method to Enable or Disable child SubMesh objects.
        /// </summary>
        /// <param name="state"></param>
        protected virtual void SetActiveSubMeshes(bool state) { }


        /// <summary>
        /// Destroy Sub Mesh Objects.
        /// </summary>
        protected virtual void DestroySubMeshObjects() { }


        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        public virtual void ClearMesh() { }


        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        public virtual void ClearMesh(bool uploadGeometry) { }


        /// <summary>
        /// Function which returns the text after it has been parsed and rich text tags removed.
        /// </summary>
        /// <returns></returns>
        public virtual string GetParsedText()
        {
            if (m_textInfo == null)
                return string.Empty;

            int characterCount = m_textInfo.characterCount;

            char[] buffer = new char[characterCount];

            for (int i = 0; i < characterCount && i < m_textInfo.characterInfo.Length; i++)
            {
                buffer[i] = m_textInfo.characterInfo[i].character;
            }

            return new string(buffer);
        }


        internal bool IsSelfOrLinkedAncestor(TMP_Text targetTextComponent)
        {
            if (targetTextComponent == null)
                return true;

            if (parentLinkedComponent != null)
            {
                if (parentLinkedComponent.IsSelfOrLinkedAncestor(targetTextComponent))
                    return true;
            }

            if (this.GetInstanceID() == targetTextComponent.GetInstanceID())
                return true;

            return false;
        }

        internal void ReleaseLinkedTextComponent(TMP_Text targetTextComponent)
        {
            if (targetTextComponent == null)
                return;

            TMP_Text childLinkedComponent = targetTextComponent.linkedTextComponent;

            if (childLinkedComponent != null)
                ReleaseLinkedTextComponent(childLinkedComponent);

            targetTextComponent.text = string.Empty;
            targetTextComponent.firstVisibleCharacter = 0;
            targetTextComponent.linkedTextComponent = null;
            targetTextComponent.parentLinkedComponent = null;
        }

        protected void DoMissingGlyphCallback(int unicode, int stringIndex, TMP_FontAsset fontAsset)
        {
            OnMissingCharacter?.Invoke(unicode, stringIndex, m_text, fontAsset, this);
        }


        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>

        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        protected Vector2 PackUV(float x, float y, float scale)
        {
            Vector2 output;

            output.x = (int)(x * 511);
            output.y = (int)(y * 511);

            output.x = (output.x * 4096) + output.y;
            output.y = scale;

            return output;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected float PackUV(float x, float y)
        {
            double x0 = (int)(x * 511);
            double y0 = (int)(y * 511);

            return (float)((x0 * 4096) + y0);
        }


        /// <summary>
        /// Function used as a replacement for LateUpdate()
        /// </summary>
        internal virtual void InternalUpdate() { }


        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>


        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>


        /// <summary>
        /// Method to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        protected uint HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }

        private bool IsValidUTF16(TextBackingContainer text, int index)
        {
            for (int i = 0; i < 4; i++)
            {
                uint c = text[index + i];
                if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'))
                    return false;
            }

            return true;
        }

        private uint GetUTF16(uint[] text, int i)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        private uint GetUTF16(TextBackingContainer text, int i)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        private bool IsValidUTF32(TextBackingContainer text, int index)
        {
            for (int i = 0; i < 8; i++)
            {
                uint c = text[index + i];
                if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'))
                    return false;
            }

            return true;
        }

        private uint GetUTF32(uint[] text, int i)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[i]) << 28;
            unicode += HexToInt((char)text[i + 1]) << 24;
            unicode += HexToInt((char)text[i + 2]) << 20;
            unicode += HexToInt((char)text[i + 3]) << 16;
            unicode += HexToInt((char)text[i + 4]) << 12;
            unicode += HexToInt((char)text[i + 5]) << 8;
            unicode += HexToInt((char)text[i + 6]) << 4;
            unicode += HexToInt((char)text[i + 7]);
            return unicode;
        }

        private uint GetUTF32(TextBackingContainer text, int i)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[i]) << 28;
            unicode += HexToInt((char)text[i + 1]) << 24;
            unicode += HexToInt((char)text[i + 2]) << 20;
            unicode += HexToInt((char)text[i + 3]) << 16;
            unicode += HexToInt((char)text[i + 4]) << 12;
            unicode += HexToInt((char)text[i + 5]) << 8;
            unicode += HexToInt((char)text[i + 6]) << 4;
            unicode += HexToInt((char)text[i + 7]);
            return unicode;
        }


        /// <summary>
        /// Method to convert Hex color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="tagCount"></param>
        /// <returns></returns>
        protected Color32 HexCharsToColor(char[] hexChars, int tagCount)
        {
            if (tagCount == 4)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 5)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));
                byte a = (byte)(HexToInt(hexChars[4]) * 16 + HexToInt(hexChars[4]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 7)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 9)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));
                byte a = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 10)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[7]));
                byte g = (byte)(HexToInt(hexChars[8]) * 16 + HexToInt(hexChars[8]));
                byte b = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[9]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 11)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[7]));
                byte g = (byte)(HexToInt(hexChars[8]) * 16 + HexToInt(hexChars[8]));
                byte b = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[9]));
                byte a = (byte)(HexToInt(hexChars[10]) * 16 + HexToInt(hexChars[10]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 13)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 15)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));
                byte a = (byte)(HexToInt(hexChars[13]) * 16 + HexToInt(hexChars[14]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Method to convert Hex Color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected Color32 HexCharsToColor(char[] hexChars, int startIndex, int length)
        {
            if (length == 7)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));

                return new Color32(r, g, b, 255);
            }
            else if (length == 9)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));
                byte a = (byte)(HexToInt(hexChars[startIndex + 7]) * 16 + HexToInt(hexChars[startIndex + 8]));

                return new Color32(r, g, b, a);
            }

            return s_colorWhite;
        }


        /// <summary>
        /// Method which returns the number of parameters used in a tag attribute and populates an array with such values.
        /// </summary>
        /// <param name="chars">Char[] containing the tag attribute and data</param>
        /// <param name="startIndex">The index of the first char of the data</param>
        /// <param name="length">The length of the data</param>
        /// <param name="parameters">The number of parameters contained in the Char[]</param>
        /// <returns></returns>
        private int GetAttributeParameters(char[] chars, int startIndex, int length, ref float[] parameters)
        {
            int endIndex = startIndex;
            int attributeCount = 0;

            while (endIndex < startIndex + length)
            {
                parameters[attributeCount] = ConvertToFloat(chars, startIndex, length, out endIndex);

                length -= (endIndex - startIndex) + 1;
                startIndex = endIndex + 1;

                attributeCount += 1;
            }

            return attributeCount;
        }


        /// <summary>
        /// Extracts a float value from char[] assuming we know the position of the start, end and decimal point.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected float ConvertToFloat(char[] chars, int startIndex, int length)
        {
            int lastIndex;

            return ConvertToFloat(chars, startIndex, length, out lastIndex);
        }


        /// <summary>
        /// Extracts a float value from char[] given a start index and length.
        /// </summary>
        /// <param name="chars"></param> The Char[] containing the numerical sequence.
        /// <param name="startIndex"></param> The index of the start of the numerical sequence.
        /// <param name="length"></param> The length of the numerical sequence.
        /// <param name="lastIndex"></param> Index of the last character in the validated sequence.
        /// <returns></returns>
        protected float ConvertToFloat(char[] chars, int startIndex, int length, out int lastIndex)
        {
            if (startIndex == 0)
            {
                lastIndex = 0;
                return Int16.MinValue;
            }

            int endIndex = startIndex + length;

            bool isIntegerValue = true;
            float decimalPointMultiplier = 0;

            int valueSignMultiplier = 1;
            if (chars[startIndex] == '+')
            {
                valueSignMultiplier = 1;
                startIndex += 1;
            }
            else if (chars[startIndex] == '-')
            {
                valueSignMultiplier = -1;
                startIndex += 1;
            }

            float value = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                uint c = chars[i];

                if (c >= '0' && c <= '9' || c == '.')
                {
                    if (c == '.')
                    {
                        isIntegerValue = false;
                        decimalPointMultiplier = 0.1f;
                        continue;
                    }

                    if (isIntegerValue)
                        value = value * 10 + (c - 48) * valueSignMultiplier;
                    else
                    {
                        value = value + (c - 48) * decimalPointMultiplier * valueSignMultiplier;
                        decimalPointMultiplier *= 0.1f;
                    }

                    continue;
                }
                else if (c == ',')
                {
                    if (i + 1 < endIndex && chars[i + 1] == ' ')
                        lastIndex = i + 1;
                    else
                        lastIndex = i;

                    if (value > 32767)
                        return Int16.MinValue;

                    return value;
                }
            }

            lastIndex = endIndex;

            if (value > 32767)
                return Int16.MinValue;

            return value;
        }

        private void ClearMarkupTagAttributes()
        {
            int length = m_xmlAttribute.Length;
            for (int i = 0; i < length; i++)
                m_xmlAttribute[i] = new RichTextTagAttribute();
        }

        /// <summary>
        /// Function to identify and validate the rich tag. Returns the position of the > if the tag was valid.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        internal bool ValidateHtmlTag(TextProcessingElement[] chars, int startIndex, out int endIndex)
        {
            int tagCharCount = 0;
            byte attributeFlag = 0;

            int attributeIndex = 0;
            ClearMarkupTagAttributes();
            TagValueType tagValueType = TagValueType.None;
            TagUnitType tagUnitType = TagUnitType.Pixels;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;

            for (int i = startIndex; i < chars.Length && chars[i].unicode != 0 && tagCharCount < m_htmlTag.Length && chars[i].unicode != '<'; i++)
            {
                uint unicode = chars[i].unicode;

                if (unicode == '>')
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    m_htmlTag[tagCharCount] = (char)0;
                    break;
                }

                m_htmlTag[tagCharCount] = (char)unicode;
                tagCharCount += 1;

                if (attributeFlag == 1)
                {
                    if (tagValueType == TagValueType.None)
                    {
                        if (unicode == '+' || unicode == '-' || unicode == '.' || (unicode >= '0' && unicode <= '9'))
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.NumericalValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.ColorValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '"')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                        }
                        else
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_xmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueHashCode = (m_xmlAttribute[attributeIndex].valueHashCode << 5) + m_xmlAttribute[attributeIndex].valueHashCode ^ TMP_TextUtilities.ToUpperFast((char)unicode);
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (tagValueType == TagValueType.NumericalValue)
                        {
                            if (unicode == 'p' || unicode == 'e' || unicode == '%' || unicode == ' ')
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;

                                switch (unicode)
                                {
                                    case 'e':
                                        m_xmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.FontUnits;
                                        break;
                                    case '%':
                                        m_xmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Percentage;
                                        break;
                                    default:
                                        m_xmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Pixels;
                                        break;
                                }

                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;

                            }
                            else
                            {
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (tagValueType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                        else if (tagValueType == TagValueType.StringValue)
                        {
                            if (unicode != '"')
                            {
                                m_xmlAttribute[attributeIndex].valueHashCode = (m_xmlAttribute[attributeIndex].valueHashCode << 5) + m_xmlAttribute[attributeIndex].valueHashCode ^ TMP_TextUtilities.ToUpperFast((char)unicode);
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                    }
                }


                if (unicode == '=') attributeFlag = 1;

                if (attributeFlag == 0 && unicode == ' ')
                {
                    if (isTagSet) return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    tagValueType = TagValueType.None;
                    tagUnitType = TagUnitType.Pixels;
                    attributeIndex += 1;
                    m_xmlAttribute[attributeIndex].nameHashCode = 0;
                    m_xmlAttribute[attributeIndex].valueType = TagValueType.None;
                    m_xmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                    m_xmlAttribute[attributeIndex].valueHashCode = 0;
                    m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                    m_xmlAttribute[attributeIndex].valueLength = 0;
                }

                if (attributeFlag == 0)
                    m_xmlAttribute[attributeIndex].nameHashCode = (m_xmlAttribute[attributeIndex].nameHashCode << 5) + m_xmlAttribute[attributeIndex].nameHashCode ^ TMP_TextUtilities.ToUpperFast((char)unicode);

                if (attributeFlag == 2 && unicode == ' ')
                    attributeFlag = 0;

            }

            if (!isValidHtmlTag)
            {
                return false;
            }

            #region Rich Text Tag Processing
            #if !RICH_TEXT_ENABLED
            if (tag_NoParsing && (m_xmlAttribute[0].nameHashCode != (int)MarkupTag.SLASH_NO_PARSE))
                return false;

            if (m_xmlAttribute[0].nameHashCode == (int)MarkupTag.SLASH_NO_PARSE)
            {
                tag_NoParsing = false;
                return true;
            }

            if (m_htmlTag[0] == 35 && tagCharCount == 4)
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            else if (m_htmlTag[0] == 35 && tagCharCount == 5)
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            else if (m_htmlTag[0] == 35 && tagCharCount == 7)
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            else if (m_htmlTag[0] == 35 && tagCharCount == 9)
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            else
            {
                float value = 0;
                float fontScale;

                switch ((MarkupTag)m_xmlAttribute[0].nameHashCode)
                {
                    case MarkupTag.BOLD:
                        m_FontStyleInternal |= FontStyles.Bold;
                        m_fontStyleStack.Add(FontStyles.Bold);

                        m_FontWeightInternal = FontWeight.Bold;
                        return true;
                    case MarkupTag.SLASH_BOLD:
                        if ((m_fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.Bold) == 0)
                            {
                                m_FontStyleInternal &= ~FontStyles.Bold;
                                m_FontWeightInternal = m_FontWeightStack.Peek();
                            }
                        }
                        return true;
                    case MarkupTag.ITALIC:
                        m_FontStyleInternal |= FontStyles.Italic;
                        m_fontStyleStack.Add(FontStyles.Italic);

                        if (m_xmlAttribute[1].nameHashCode == (int)MarkupTag.ANGLE)
                        {
                            m_ItalicAngle = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);

                            if (m_ItalicAngle < -180 || m_ItalicAngle > 180) return false;
                        }
                        else
                            m_ItalicAngle = m_currentFontAsset.italicStyle;

                        m_ItalicAngleStack.Add(m_ItalicAngle);

                        return true;
                    case MarkupTag.SLASH_ITALIC:
                        if ((m_fontStyle & FontStyles.Italic) != FontStyles.Italic)
                        {
                            m_ItalicAngle = m_ItalicAngleStack.Remove();

                            if (m_fontStyleStack.Remove(FontStyles.Italic) == 0)
                                m_FontStyleInternal &= ~FontStyles.Italic;
                        }
                        return true;
                    case MarkupTag.STRIKETHROUGH:
                        m_FontStyleInternal |= FontStyles.Strikethrough;
                        m_fontStyleStack.Add(FontStyles.Strikethrough);

                        if (m_xmlAttribute[1].nameHashCode == (int)MarkupTag.COLOR)
                        {
                            m_strikethroughColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);
                            m_strikethroughColor.a = m_htmlColor.a < m_strikethroughColor.a ? (byte)(m_htmlColor.a) : (byte)(m_strikethroughColor .a);
                        }
                        else
                            m_strikethroughColor = m_htmlColor;

                        m_strikethroughColorStack.Add(m_strikethroughColor);

                        return true;
                    case MarkupTag.SLASH_STRIKETHROUGH:
                        if ((m_fontStyle & FontStyles.Strikethrough) != FontStyles.Strikethrough)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.Strikethrough) == 0)
                                m_FontStyleInternal &= ~FontStyles.Strikethrough;
                        }

                        m_strikethroughColor = m_strikethroughColorStack.Remove();
                        return true;
                    case MarkupTag.UNDERLINE:
                        m_FontStyleInternal |= FontStyles.Underline;
                        m_fontStyleStack.Add(FontStyles.Underline);

                        if (m_xmlAttribute[1].nameHashCode == (int)MarkupTag.COLOR)
                        {
                            m_underlineColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);
                            m_underlineColor.a = m_htmlColor.a < m_underlineColor.a ? (m_htmlColor.a) : (m_underlineColor.a);
                        }
                        else
                            m_underlineColor = m_htmlColor;

                        m_underlineColorStack.Add(m_underlineColor);

                        return true;
                    case MarkupTag.SLASH_UNDERLINE:
                        if ((m_fontStyle & FontStyles.Underline) != FontStyles.Underline)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.Underline) == 0)
                                m_FontStyleInternal &= ~FontStyles.Underline;
                        }

                        m_underlineColor = m_underlineColorStack.Remove();
                        return true;
                    case MarkupTag.MARK:
                        m_FontStyleInternal |= FontStyles.Highlight;
                        m_fontStyleStack.Add(FontStyles.Highlight);

                        Color32 highlightColor = new Color32(255, 255, 0, 64);
                        TMP_Offset highlightPadding = TMP_Offset.zero;

                        for (int i = 0; i < m_xmlAttribute.Length && m_xmlAttribute[i].nameHashCode != 0; i++)
                        {
                            switch ((MarkupTag)m_xmlAttribute[i].nameHashCode)
                            {
                                case MarkupTag.MARK:
                                    if (m_xmlAttribute[i].valueType == TagValueType.ColorValue)
                                        highlightColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);
                                    break;

                                case MarkupTag.COLOR:
                                    highlightColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength);
                                    break;

                                case MarkupTag.PADDING:
                                    int paramCount = GetAttributeParameters(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength, ref m_attributeParameterValues);
                                    if (paramCount != 4) return false;

                                    highlightPadding = new TMP_Offset(m_attributeParameterValues[0], m_attributeParameterValues[1], m_attributeParameterValues[2], m_attributeParameterValues[3]);
                                    highlightPadding *= m_fontSize * 0.01f * (m_isOrthographic ? 1 : 0.1f);
                                    break;
                            }
                        }

                        highlightColor.a = m_htmlColor.a < highlightColor.a ? (byte)(m_htmlColor.a) : (byte)(highlightColor.a);

                        m_HighlightState = new HighlightState(highlightColor, highlightPadding);
                        m_HighlightStateStack.Push(m_HighlightState);

                        return true;
                    case MarkupTag.SLASH_MARK:
                        if ((m_fontStyle & FontStyles.Highlight) != FontStyles.Highlight)
                        {
                            m_HighlightStateStack.Remove();
                            m_HighlightState = m_HighlightStateStack.current;

                            if (m_fontStyleStack.Remove(FontStyles.Highlight) == 0)
                                m_FontStyleInternal &= ~FontStyles.Highlight;
                        }
                        return true;
                    case MarkupTag.SUBSCRIPT:
                        m_fontScaleMultiplier *= m_currentFontAsset.faceInfo.subscriptSize > 0 ? m_currentFontAsset.faceInfo.subscriptSize : 1;
                        m_baselineOffsetStack.Push(m_baselineOffset);
                        m_materialReferenceStack.Push(m_materialReferences[m_currentMaterialIndex]);
                        fontScale = (m_currentFontSize / m_currentFontAsset.faceInfo.pointSize * m_currentFontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
                        m_baselineOffset += m_currentFontAsset.faceInfo.subscriptOffset * fontScale * m_fontScaleMultiplier;

                        m_fontStyleStack.Add(FontStyles.Subscript);
                        m_FontStyleInternal |= FontStyles.Subscript;
                        return true;
                    case MarkupTag.SLASH_SUBSCRIPT:
                        if ((m_FontStyleInternal & FontStyles.Subscript) == FontStyles.Subscript)
                        {
                            var previousFontAsset = m_materialReferenceStack.Pop().fontAsset;
                            if (m_fontScaleMultiplier < 1)
                            {
                                m_baselineOffset = m_baselineOffsetStack.Pop();
                                m_fontScaleMultiplier /= previousFontAsset.faceInfo.subscriptSize > 0 ? previousFontAsset.faceInfo.subscriptSize : 1;
                            }

                            if (m_fontStyleStack.Remove(FontStyles.Subscript) == 0)
                                m_FontStyleInternal &= ~FontStyles.Subscript;
                        }
                        return true;
                    case MarkupTag.SUPERSCRIPT:
                        m_fontScaleMultiplier *= m_currentFontAsset.faceInfo.superscriptSize > 0 ? m_currentFontAsset.faceInfo.superscriptSize : 1;
                        m_baselineOffsetStack.Push(m_baselineOffset);
                        m_materialReferenceStack.Push(m_materialReferences[m_currentMaterialIndex]);
                        fontScale = (m_currentFontSize / m_currentFontAsset.faceInfo.pointSize * m_currentFontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
                        m_baselineOffset += m_currentFontAsset.faceInfo.superscriptOffset * fontScale * m_fontScaleMultiplier;

                        m_fontStyleStack.Add(FontStyles.Superscript);
                        m_FontStyleInternal |= FontStyles.Superscript;
                        return true;
                    case MarkupTag.SLASH_SUPERSCRIPT:
                        if ((m_FontStyleInternal & FontStyles.Superscript) == FontStyles.Superscript)
                        {
                            var previousFontAsset = m_materialReferenceStack.Pop().fontAsset;
                            if (m_fontScaleMultiplier < 1)
                            {
                                m_baselineOffset = m_baselineOffsetStack.Pop();
                                m_fontScaleMultiplier /= previousFontAsset.faceInfo.superscriptSize > 0 ? previousFontAsset.faceInfo.superscriptSize : 1;
                            }

                            if (m_fontStyleStack.Remove(FontStyles.Superscript) == 0)
                                m_FontStyleInternal &= ~FontStyles.Superscript;
                        }
                        return true;
                    case MarkupTag.FONT_WEIGHT:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch ((int)value)
                        {
                            case 100:
                                m_FontWeightInternal = FontWeight.Thin;
                                break;
                            case 200:
                                m_FontWeightInternal = FontWeight.ExtraLight;
                                break;
                            case 300:
                                m_FontWeightInternal = FontWeight.Light;
                                break;
                            case 400:
                                m_FontWeightInternal = FontWeight.Regular;
                                break;
                            case 500:
                                m_FontWeightInternal = FontWeight.Medium;
                                break;
                            case 600:
                                m_FontWeightInternal = FontWeight.SemiBold;
                                break;
                            case 700:
                                m_FontWeightInternal = FontWeight.Bold;
                                break;
                            case 800:
                                m_FontWeightInternal = FontWeight.Heavy;
                                break;
                            case 900:
                                m_FontWeightInternal = FontWeight.Black;
                                break;
                        }

                        m_FontWeightStack.Add(m_FontWeightInternal);

                        return true;
                    case MarkupTag.SLASH_FONT_WEIGHT:
                        m_FontWeightStack.Remove();

                        if (m_FontStyleInternal == FontStyles.Bold)
                            m_FontWeightInternal = FontWeight.Bold;
                        else
                            m_FontWeightInternal = m_FontWeightStack.Peek();

                        return true;
                    case MarkupTag.POSITION:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_xAdvance = value * (m_isOrthographic ? 1.0f : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_xAdvance = value * m_currentFontSize * (m_isOrthographic ? 1.0f : 0.1f);
                                return true;
                            case TagUnitType.Percentage:
                                m_xAdvance = m_marginWidth * value / 100;
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_POSITION:
                        m_isIgnoringAlignment = false;
                        return true;
                    case MarkupTag.VERTICAL_OFFSET:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_baselineOffset = value * (m_isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_baselineOffset = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return false;
                    case MarkupTag.SLASH_VERTICAL_OFFSET:
                        m_baselineOffset = 0;
                        return true;
                    case MarkupTag.PAGE:
                        if (m_overflowMode == TextOverflowModes.Page)
                        {
                            m_xAdvance = 0 + tag_LineIndent + tag_Indent;
                            m_lineOffset = 0;
                            m_pageNumber += 1;
                            m_isNewPage = true;
                        }
                        return true;
                    case MarkupTag.NO_BREAK:
                        m_isNonBreakingSpace = true;
                        return true;
                    case MarkupTag.SLASH_NO_BREAK:
                        m_isNonBreakingSpace = false;
                        return true;
                    case MarkupTag.SIZE:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                if (m_htmlTag[5] == 43)
                                {
                                    m_currentFontSize = m_fontSize + value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    return true;
                                }
                                else if (m_htmlTag[5] == 45)
                                {
                                    m_currentFontSize = m_fontSize + value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    return true;
                                }
                                else
                                {
                                    m_currentFontSize = value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    return true;
                                }
                            case TagUnitType.FontUnits:
                                m_currentFontSize = m_fontSize * value;
                                m_sizeStack.Add(m_currentFontSize);
                                return true;
                            case TagUnitType.Percentage:
                                m_currentFontSize = m_fontSize * value / 100;
                                m_sizeStack.Add(m_currentFontSize);
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_SIZE:
                        m_currentFontSize = m_sizeStack.Remove();
                        return true;
                    case MarkupTag.FONT:
                        int fontHashCode = m_xmlAttribute[0].valueHashCode;
                        int materialAttributeHashCode = m_xmlAttribute[1].nameHashCode;
                        int materialHashCode = m_xmlAttribute[1].valueHashCode;

                        if (fontHashCode == (int)MarkupTag.DEFAULT)
                        {
                            m_currentFontAsset = m_materialReferences[0].fontAsset;
                            m_currentMaterial = m_materialReferences[0].material;
                            m_currentMaterialIndex = 0;

                            m_materialReferenceStack.Add(m_materialReferences[0]);

                            return true;
                        }

                        TMP_FontAsset tempFont;
                        Material tempMaterial;

                        MaterialReferenceManager.TryGetFontAsset(fontHashCode, out tempFont);

                        if (tempFont == null)
                        {
                            tempFont = OnFontAssetRequest?.Invoke(fontHashCode, new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));

                            if (tempFont == null)
                            {
                                tempFont = Resources.Load<TMP_FontAsset>(TMP_Settings.defaultFontAssetPath + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));
                            }

                            if (tempFont == null)
                                return false;

                            MaterialReferenceManager.AddFontAsset(tempFont);
                        }

                        if (materialAttributeHashCode == 0 && materialHashCode == 0)
                        {
                            m_currentMaterial = tempFont.material;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, ref m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        else if (materialAttributeHashCode == (int)MarkupTag.MATERIAL)
                        {
                            if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                            {
                                m_currentMaterial = tempMaterial;

                                m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, ref m_materialReferences, m_materialReferenceIndexLookup);

                                m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                            }
                            else
                            {
                                tempMaterial = Resources.Load<Material>(TMP_Settings.defaultFontAssetPath + new string(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength));

                                if (tempMaterial == null)
                                    return false;

                                MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                                m_currentMaterial = tempMaterial;

                                m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, ref m_materialReferences, m_materialReferenceIndexLookup);

                                m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                            }
                        }
                        else
                            return false;

                        m_currentFontAsset = tempFont;

                        return true;
                    case MarkupTag.SLASH_FONT:
                        {
                            MaterialReference materialReference = m_materialReferenceStack.Remove();

                            m_currentFontAsset = materialReference.fontAsset;
                            m_currentMaterial = materialReference.material;
                            m_currentMaterialIndex = materialReference.index;

                            return true;
                        }
                    case MarkupTag.MATERIAL:
                        materialHashCode = m_xmlAttribute[0].valueHashCode;

                        if (materialHashCode == (int)MarkupTag.DEFAULT)
                        {
                            m_currentMaterial = m_materialReferences[0].material;
                            m_currentMaterialIndex = 0;

                            m_materialReferenceStack.Add(m_materialReferences[0]);

                            return true;
                        }


                        if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                        {
                            m_currentMaterial = tempMaterial;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        else
                        {
                            tempMaterial = Resources.Load<Material>(TMP_Settings.defaultFontAssetPath + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));

                            if (tempMaterial == null)
                                return false;

                            MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                            m_currentMaterial = tempMaterial;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        return true;
                    case MarkupTag.SLASH_MATERIAL:
                        {
                            MaterialReference materialReference = m_materialReferenceStack.Remove();

                            m_currentMaterial = materialReference.material;
                            m_currentMaterialIndex = materialReference.index;

                            return true;
                        }
                    case MarkupTag.SPACE:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_xAdvance += value * (m_isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_xAdvance += value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return false;
                    case MarkupTag.ALPHA:
                        if (m_xmlAttribute[0].valueLength != 3) return false;

                        m_htmlColor.a = (byte)(HexToInt(m_htmlTag[7]) * 16 + HexToInt(m_htmlTag[8]));
                        return true;

                    case MarkupTag.A:
                        if (m_isTextLayoutPhase && !m_isCalculatingPreferredValues)
                        {
                            if (m_xmlAttribute[1].nameHashCode == (int)MarkupTag.HREF)
                            {
                                int index = m_textInfo.linkCount;

                                if (index + 1 > m_textInfo.linkInfo.Length)
                                    TMP_TextInfo.Resize(ref m_textInfo.linkInfo, index + 1);

                                m_textInfo.linkInfo[index].textComponent = this;
                                m_textInfo.linkInfo[index].hashCode = (int)MarkupTag.HREF;
                                m_textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_characterCount;
                                m_textInfo.linkInfo[index].SetLinkID(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);
                            }
                        }
                        return true;
                    case MarkupTag.SLASH_A:
                        if (m_isTextLayoutPhase && !m_isCalculatingPreferredValues)
                        {
                            int index = m_textInfo.linkCount;

                            m_textInfo.linkInfo[index].linkTextLength = m_characterCount - m_textInfo.linkInfo[index].linkTextfirstCharacterIndex;

                            m_textInfo.linkCount += 1;
                        }
                        return true;
                    case MarkupTag.LINK:
                        if (m_isTextLayoutPhase && !m_isCalculatingPreferredValues)
                        {
                            int index = m_textInfo.linkCount;

                            if (index + 1 > m_textInfo.linkInfo.Length)
                                TMP_TextInfo.Resize(ref m_textInfo.linkInfo, index + 1);

                            m_textInfo.linkInfo[index].textComponent = this;
                            m_textInfo.linkInfo[index].hashCode = m_xmlAttribute[0].valueHashCode;
                            m_textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_characterCount;

                            m_textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_xmlAttribute[0].valueStartIndex;
                            m_textInfo.linkInfo[index].linkIdLength = m_xmlAttribute[0].valueLength;
                            m_textInfo.linkInfo[index].SetLinkID(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);
                        }
                        return true;
                    case MarkupTag.SLASH_LINK:
                        if (m_isTextLayoutPhase && !m_isCalculatingPreferredValues)
                        {
                            if (m_textInfo.linkCount < m_textInfo.linkInfo.Length)
                            {
                                m_textInfo.linkInfo[m_textInfo.linkCount].linkTextLength = m_characterCount - m_textInfo.linkInfo[m_textInfo.linkCount].linkTextfirstCharacterIndex;

                                m_textInfo.linkCount += 1;
                            }
                        }
                        return true;
                    case MarkupTag.ALIGN:
                        switch ((MarkupTag)m_xmlAttribute[0].valueHashCode)
                        {
                            case MarkupTag.LEFT:
                                m_lineJustification = HorizontalAlignmentOptions.Left;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case MarkupTag.RIGHT:
                                m_lineJustification = HorizontalAlignmentOptions.Right;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case MarkupTag.CENTER:
                                m_lineJustification = HorizontalAlignmentOptions.Center;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case MarkupTag.JUSTIFIED:
                                m_lineJustification = HorizontalAlignmentOptions.Justified;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                            case MarkupTag.FLUSH:
                                m_lineJustification = HorizontalAlignmentOptions.Flush;
                                m_lineJustificationStack.Add(m_lineJustification);
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_ALIGN:
                        m_lineJustification = m_lineJustificationStack.Remove();
                        return true;
                    case MarkupTag.WIDTH:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_width = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                return false;
                            case TagUnitType.Percentage:
                                m_width = m_marginWidth * value / 100;
                                break;
                        }
                        return true;
                    case MarkupTag.SLASH_WIDTH:
                        m_width = -1;
                        return true;

                    case MarkupTag.COLOR:
                        if (m_htmlTag[6] == 35 && tagCharCount == 10)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
                        else if (m_htmlTag[6] == 35 && tagCharCount == 11)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }

                        if (m_htmlTag[6] == 35 && tagCharCount == 13)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
                        else if (m_htmlTag[6] == 35 && tagCharCount == 15)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }

                        switch (m_xmlAttribute[0].valueHashCode)
                        {
                            case (int)MarkupTag.RED:
                                m_htmlColor = Color.red;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.LIGHTBLUE:
                                m_htmlColor = new Color32(173, 216, 230, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.BLUE:
                                m_htmlColor = Color.blue;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.GREY:
                                m_htmlColor = new Color32(128, 128, 128, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.BLACK:
                                m_htmlColor = Color.black;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.GREEN:
                                m_htmlColor = Color.green;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.WHITE:
                                m_htmlColor = Color.white;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.ORANGE:
                                m_htmlColor = new Color32(255, 128, 0, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.PURPLE:
                                m_htmlColor = new Color32(160, 32, 240, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case (int)MarkupTag.YELLOW:
                                m_htmlColor = Color.yellow;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                        }
                        return false;

                    case MarkupTag.GRADIENT:
                        int gradientPresetHashCode = m_xmlAttribute[0].valueHashCode;
                        TMP_ColorGradient tempColorGradientPreset;

                        if (MaterialReferenceManager.TryGetColorGradientPreset(gradientPresetHashCode, out tempColorGradientPreset))
                        {
                            m_colorGradientPreset = tempColorGradientPreset;
                        }
                        else
                        {
                            if (tempColorGradientPreset == null)
                            {
                                tempColorGradientPreset = Resources.Load<TMP_ColorGradient>(TMP_Settings.defaultColorGradientPresetsPath + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));
                            }

                            if (tempColorGradientPreset == null)
                                return false;

                            MaterialReferenceManager.AddColorGradientPreset(gradientPresetHashCode, tempColorGradientPreset);
                            m_colorGradientPreset = tempColorGradientPreset;
                        }

                        m_colorGradientPresetIsTinted = false;

                        for (int i = 1; i < m_xmlAttribute.Length && m_xmlAttribute[i].nameHashCode != 0; i++)
                        {
                            int nameHashCode = m_xmlAttribute[i].nameHashCode;

                            switch ((MarkupTag)nameHashCode)
                            {
                                case MarkupTag.TINT:
                                    m_colorGradientPresetIsTinted = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength) != 0;
                                    break;
                            }
                        }

                        m_colorGradientStack.Add(m_colorGradientPreset);

                        return true;

                    case MarkupTag.SLASH_GRADIENT:
                        m_colorGradientPreset = m_colorGradientStack.Remove();
                        return true;

                    case MarkupTag.CHARACTER_SPACE:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_cSpacing = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_cSpacing = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;
                    case MarkupTag.SLASH_CHARACTER_SPACE:
                        if (!m_isTextLayoutPhase) return true;

                        if (m_characterCount > 0)
                        {
                            m_xAdvance -= m_cSpacing;
                            m_textInfo.characterInfo[m_characterCount - 1].xAdvance = m_xAdvance;
                        }
                        m_cSpacing = 0;
                        return true;
                    case MarkupTag.MONOSPACE:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (m_xmlAttribute[0].unitType)
                        {
                            case TagUnitType.Pixels:
                                m_monoSpacing = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_monoSpacing = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }

                        if (m_xmlAttribute[1].nameHashCode == (int)MarkupTag.DUOSPACE)
                            m_duoSpace = ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength) != 0;

                        return true;
                    case MarkupTag.SLASH_MONOSPACE:
                        m_monoSpacing = 0;
                        m_duoSpace = false;
                        return true;
                    case MarkupTag.CLASS:
                        return false;
                    case MarkupTag.SLASH_COLOR:
                        m_htmlColor = m_colorStack.Remove();
                        return true;
                    case MarkupTag.INDENT:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                tag_Indent = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                tag_Indent = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                tag_Indent = m_marginWidth * value / 100;
                                break;
                        }
                        m_indentStack.Add(tag_Indent);

                        m_xAdvance = tag_Indent;
                        return true;
                    case MarkupTag.SLASH_INDENT:
                        tag_Indent = m_indentStack.Remove();
                        return true;
                    case MarkupTag.LINE_INDENT:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                tag_LineIndent = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                tag_LineIndent = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                tag_LineIndent = m_marginWidth * value / 100;
                                break;
                        }

                        m_xAdvance += tag_LineIndent;
                        return true;
                    case MarkupTag.SLASH_LINE_INDENT:
                        tag_LineIndent = 0;
                        return true;
                    case MarkupTag.SPRITE:
                        int spriteAssetHashCode = m_xmlAttribute[0].valueHashCode;
                        TMP_SpriteAsset tempSpriteAsset;
                        m_spriteIndex = -1;

                        if (m_xmlAttribute[0].valueType == TagValueType.None || m_xmlAttribute[0].valueType == TagValueType.NumericalValue)
                        {
                            if (m_spriteAsset != null)
                            {
                                m_currentSpriteAsset = m_spriteAsset;
                            }
                            else if (m_defaultSpriteAsset != null)
                            {
                                m_currentSpriteAsset = m_defaultSpriteAsset;
                            }
                            else if (m_defaultSpriteAsset == null)
                            {
                                if (TMP_Settings.defaultSpriteAsset != null)
                                    m_defaultSpriteAsset = TMP_Settings.defaultSpriteAsset;
                                else
                                    m_defaultSpriteAsset = Resources.Load<TMP_SpriteAsset>("Sprite Assets/Default Sprite Asset");

                                m_currentSpriteAsset = m_defaultSpriteAsset;
                            }

                            if (m_currentSpriteAsset == null)
                                return false;
                        }
                        else
                        {
                            if (MaterialReferenceManager.TryGetSpriteAsset(spriteAssetHashCode, out tempSpriteAsset))
                            {
                                m_currentSpriteAsset = tempSpriteAsset;
                            }
                            else
                            {
                                if (tempSpriteAsset == null)
                                {
                                    tempSpriteAsset = OnSpriteAssetRequest?.Invoke(spriteAssetHashCode, new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));

                                    if (tempSpriteAsset == null)
                                        tempSpriteAsset = Resources.Load<TMP_SpriteAsset>(TMP_Settings.defaultSpriteAssetPath + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));
                                }

                                if (tempSpriteAsset == null)
                                    return false;

                                MaterialReferenceManager.AddSpriteAsset(spriteAssetHashCode, tempSpriteAsset);
                                m_currentSpriteAsset = tempSpriteAsset;
                            }
                        }

                        if (m_xmlAttribute[0].valueType == TagValueType.NumericalValue)
                        {
                            int index = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                            if (index == Int16.MinValue) return false;

                            if (index > m_currentSpriteAsset.spriteCharacterTable.Count - 1) return false;

                            m_spriteIndex = index;
                        }

                        m_spriteColor = s_colorWhite;
                        m_tintSprite = false;

                        for (int i = 0; i < m_xmlAttribute.Length && m_xmlAttribute[i].nameHashCode != 0; i++)
                        {
                            int nameHashCode = m_xmlAttribute[i].nameHashCode;
                            int index = 0;

                            switch ((MarkupTag)nameHashCode)
                            {
                                case MarkupTag.NAME:
                                    m_currentSpriteAsset = TMP_SpriteAsset.SearchForSpriteByHashCode(m_currentSpriteAsset, m_xmlAttribute[i].valueHashCode, true, out index);
                                    if (index == -1) return false;

                                    m_spriteIndex = index;
                                    break;
                                case MarkupTag.INDEX:
                                    index = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);

                                    if (index == Int16.MinValue) return false;

                                    if (index > m_currentSpriteAsset.spriteCharacterTable.Count - 1) return false;

                                    m_spriteIndex = index;
                                    break;
                                case MarkupTag.TINT:
                                    m_tintSprite = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength) != 0;
                                    break;
                                case MarkupTag.COLOR:
                                    m_spriteColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength);
                                    break;
                                case MarkupTag.ANIM:
                                    int paramCount = GetAttributeParameters(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength, ref m_attributeParameterValues);
                                    if (paramCount != 3) return false;

                                    m_spriteIndex = (int)m_attributeParameterValues[0];

                                    if (m_isTextLayoutPhase)
                                    {
                                        spriteAnimator.DoSpriteAnimation(m_characterCount, m_currentSpriteAsset, m_spriteIndex, (int)m_attributeParameterValues[1], (int)m_attributeParameterValues[2]);
                                    }

                                    break;

                                default:
                                    if (nameHashCode != (int)MarkupTag.SPRITE)
                                        return false;
                                    break;
                            }
                        }

                        if (m_spriteIndex == -1) return false;

                        m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentSpriteAsset.material, m_currentSpriteAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                        m_textElementType = TMP_TextElementType.Sprite;
                        return true;
                    case MarkupTag.LOWERCASE:
                        m_FontStyleInternal |= FontStyles.LowerCase;
                        m_fontStyleStack.Add(FontStyles.LowerCase);
                        return true;
                    case MarkupTag.SLASH_LOWERCASE:
                        if ((m_fontStyle & FontStyles.LowerCase) != FontStyles.LowerCase)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.LowerCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.LowerCase;
                        }
                        return true;
                    case MarkupTag.ALLCAPS:
                    case MarkupTag.UPPERCASE:
                        m_FontStyleInternal |= FontStyles.UpperCase;
                        m_fontStyleStack.Add(FontStyles.UpperCase);
                        return true;
                    case MarkupTag.SLASH_ALLCAPS:
                    case MarkupTag.SLASH_UPPERCASE:
                        if ((m_fontStyle & FontStyles.UpperCase) != FontStyles.UpperCase)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.UpperCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.UpperCase;
                        }
                        return true;
                    case MarkupTag.SMALLCAPS:
                        m_FontStyleInternal |= FontStyles.SmallCaps;
                        m_fontStyleStack.Add(FontStyles.SmallCaps);
                        return true;
                    case MarkupTag.SLASH_SMALLCAPS:
                        if ((m_fontStyle & FontStyles.SmallCaps) != FontStyles.SmallCaps)
                        {
                            if (m_fontStyleStack.Remove(FontStyles.SmallCaps) == 0)
                                m_FontStyleInternal &= ~FontStyles.SmallCaps;
                        }
                        return true;
                    case MarkupTag.MARGIN:
                        switch (m_xmlAttribute[0].valueType)
                        {
                            case TagValueType.NumericalValue:
                                value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                                if (value == Int16.MinValue) return false;

                                switch (tagUnitType)
                                {
                                    case TagUnitType.Pixels:
                                        m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f);
                                        break;
                                    case TagUnitType.FontUnits:
                                        m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                        break;
                                    case TagUnitType.Percentage:
                                        m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                        break;
                                }
                                m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                                m_marginRight = m_marginLeft;
                                return true;

                            case TagValueType.None:
                                for (int i = 1; i < m_xmlAttribute.Length && m_xmlAttribute[i].nameHashCode != 0; i++)
                                {
                                    int nameHashCode = m_xmlAttribute[i].nameHashCode;

                                    switch ((MarkupTag)nameHashCode)
                                    {
                                        case MarkupTag.LEFT:
                                            value = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength);

                                            if (value == Int16.MinValue) return false;

                                            switch (m_xmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                                            break;

                                        case MarkupTag.RIGHT:
                                            value = ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength);

                                            if (value == Int16.MinValue) return false;

                                            switch (m_xmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_marginRight = value * (m_isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_marginRight = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_marginRight = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_marginRight = m_marginRight >= 0 ? m_marginRight : 0;
                                            break;
                                    }
                                }
                                return true;
                        }

                        return false;
                    case MarkupTag.SLASH_MARGIN:
                        m_marginLeft = 0;
                        m_marginRight = 0;
                        return true;
                    case MarkupTag.MARGIN_LEFT:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_marginLeft = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                break;
                        }
                        m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                        return true;
                    case MarkupTag.MARGIN_RIGHT:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_marginRight = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_marginRight = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_marginRight = (m_marginWidth - (m_width != -1 ? m_width : 0)) * value / 100;
                                break;
                        }
                        m_marginRight = m_marginRight >= 0 ? m_marginRight : 0;
                        return true;
                    case MarkupTag.LINE_HEIGHT:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_lineHeight = value * (m_isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_lineHeight = value * (m_isOrthographic ? 1 : 0.1f) * m_currentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                fontScale = (m_currentFontSize / m_currentFontAsset.faceInfo.pointSize * m_currentFontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));
                                m_lineHeight = m_fontAsset.faceInfo.lineHeight * value / 100 * fontScale;
                                break;
                        }
                        return true;
                    case MarkupTag.SLASH_LINE_HEIGHT:
                        m_lineHeight = TMP_Math.FLOAT_UNSET;
                        return true;
                    case MarkupTag.NO_PARSE:
                        tag_NoParsing = true;
                        return true;
                    case MarkupTag.ACTION:
                        int actionID = m_xmlAttribute[0].valueHashCode;

                        if (m_isTextLayoutPhase)
                        {
                            m_actionStack.Add(actionID);

                            Debug.Log("Action ID: [" + actionID + "] First character index: " + m_characterCount);


                        }

                        return true;
                    case MarkupTag.SLASH_ACTION:
                        if (m_isTextLayoutPhase)
                        {
                            Debug.Log("Action ID: [" + m_actionStack.CurrentItem() + "] Last character index: " + (m_characterCount - 1));
                        }

                        m_actionStack.Remove();
                        return true;
                    case MarkupTag.SCALE:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        m_FXScale = new Vector3(value, 1, 1);

                        return true;
                    case MarkupTag.SLASH_SCALE:
                        m_FXScale = Vector3.one;
                        return true;
                    case MarkupTag.ROTATE:
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);

                        if (value == Int16.MinValue) return false;

                        m_FXRotation = Quaternion.Euler(0, 0, value);

                        return true;
                    case MarkupTag.SLASH_ROTATE:
                        m_FXRotation = Quaternion.identity;
                        return true;
                    case MarkupTag.TABLE:

                        return false;
                    case MarkupTag.SLASH_TABLE:
                        return false;
                    case MarkupTag.TR:
                        return false;
                    case MarkupTag.SLASH_TR:
                        return false;
                    case MarkupTag.TH:
                        return false;
                    case MarkupTag.SLASH_TH:
                        return false;
                    case MarkupTag.TD:

                        return false;
                    case MarkupTag.SLASH_TD:
                        return false;
                }
            }
            #endif
            #endregion

            return false;
        }
    }
}
