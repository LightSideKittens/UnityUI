using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;


#pragma warning disable 0414
#pragma warning disable 0618


namespace TMPro
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/TextMeshPro - Text (UI)", 11)]
    [ExecuteAlways]
    #if UNITY_2023_2_OR_NEWER
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/TextMeshPro/index.html")]
    #else
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2")]
    #endif
    public class TextMeshProUGUI : TMP_Text, ILayoutElement
    {
        /// <summary>
        /// Get the material that will be used for rendering.
        /// </summary>
        public override Material materialForRendering
        {
            get { return TMP_MaterialManager.GetMaterialForRendering(this, m_sharedMaterial); }
        }


        /// <summary>
        /// Determines if the size of the text container will be adjusted to fit the text object when it is first created.
        /// </summary>
        public override bool autoSizeTextContainer
        {
            get { return m_autoSizeTextContainer; }

            set { if (m_autoSizeTextContainer == value) return; m_autoSizeTextContainer = value; if (m_autoSizeTextContainer) { CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this); SetLayoutDirty(); } }
        }


        /// <summary>
        /// Reference to the Mesh used by the text object.
        /// </summary>
        public override Mesh mesh
        {
            get { return m_mesh; }
        }


        /// <summary>
        /// Reference to the CanvasRenderer used by the text object.
        /// </summary>
        public new CanvasRenderer canvasRenderer
        {
            get
            {
                if (m_canvasRenderer == null) m_canvasRenderer = GetComponent<CanvasRenderer>();

                return m_canvasRenderer;
            }
        }


        /// <summary>
        /// Anchor dampening prevents the anchor position from being adjusted unless the positional change exceeds about 40% of the width of the underline character. This essentially stabilizes the anchor position.
        /// </summary>

#if !UNITY_2019_3_OR_NEWER
        [SerializeField]
        private bool m_Maskable = true;
        #endif

        private bool m_isRebuildingLayout = false;
        private Coroutine m_DelayedGraphicRebuild;
        private Coroutine m_DelayedMaterialRebuild;

        /// <summary>
        /// Function called by Unity when the horizontal layout needs to be recalculated.
        /// </summary>
        public void CalculateLayoutInputHorizontal()
        {
        }


        /// <summary>
        /// Function called by Unity when the vertical layout needs to be recalculated.
        /// </summary>
        public void CalculateLayoutInputVertical()
        {
        }


        public override void SetVerticesDirty()
        {
            if (this == null || !this.IsActive())
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetLayoutDirty()
        {
            m_isPreferredWidthDirty = true;
            m_isPreferredHeightDirty = true;

            if (this == null || !this.IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);

            m_isLayoutDirty = true;

            if (m_OnDirtyLayoutCallback != null)
                m_OnDirtyLayoutCallback();
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetMaterialDirty()
        {
            if (this == null || !this.IsActive())
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;

            m_isMaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }


        /// <summary>
        ///
        /// </summary>
        public override void SetAllDirty()
        {
            SetLayoutDirty();
            SetVerticesDirty();
            SetMaterialDirty();
        }


        /// <summary>
        /// Delay registration of text object for graphic rebuild by one frame.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayedGraphicRebuild()
        {
            yield return null;

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();

            m_DelayedGraphicRebuild = null;
        }


        /// <summary>
        /// Delay registration of text object for graphic rebuild by one frame.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayedMaterialRebuild()
        {
            yield return null;

            m_isMaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();

            m_DelayedMaterialRebuild = null;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="update"></param>
        public override void Rebuild(CanvasUpdate update)
        {
            if (this == null) return;

            base.Rebuild(update);
            
            if (update == CanvasUpdate.PreRender)
            {
                OnPreRenderCanvas();

                if (!m_isMaterialDirty) return;

                UpdateMaterial();
                m_isMaterialDirty = false;
            }
        }


        /// <summary>
        /// Method to keep the pivot of the sub text objects in sync with the parent pivot.
        /// </summary>
        private void UpdateSubObjectPivot()
        {
            if (m_textInfo == null) return;

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                m_subTextObjects[i].SetPivotDirty();
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="baseMaterial"></param>
        /// <returns></returns>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            Material mat = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                m_StencilValue = maskable ? MaskUtilities.GetStencilDepth(transform, rootCanvas) : 0;
                m_ShouldRecalculateStencil = false;
            }

            if (m_StencilValue > 0)
            {
                var maskMat = StencilMaterial.Add(mat, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                mat = m_MaskMaterial;
            }

            return mat;
        }


        /// <summary>
        ///
        /// </summary>
        protected override void UpdateMaterial()
        {
            if (m_sharedMaterial == null || canvasRenderer == null) return;

            m_canvasRenderer.materialCount = 1;
            m_canvasRenderer.SetMaterial(materialForRendering, 0);
        }


        /// <summary>
        /// Sets the masking offset from the bounds of the object
        /// </summary>
        public Vector4 maskOffset
        {
            get { return m_maskOffset; }
            set { m_maskOffset = value; UpdateMask(); _havePropertiesChanged = true; }
        }


        /// <summary>
        /// Method called when the state of a parent changes.
        /// </summary>
        public override void RecalculateClipping()
        {
            base.RecalculateClipping();
        }


        /// <summary>
        /// Method called when Stencil Mask needs to be updated on this element and parents.
        /// </summary>


        /// <summary>
        /// Override of the Cull function to provide for the ability to override the culling of the text object.
        /// </summary>
        /// <param name="clipRect"></param>
        /// <param name="validRect"></param>
        public override void Cull(Rect clipRect, bool validRect)
        {
            m_ShouldUpdateCulling = false;

            if (m_isLayoutDirty)
            {
                m_ShouldUpdateCulling = true;
                m_ClipRect = clipRect;
                m_ValidRect = validRect;
                return;
            }

            Rect rect = GetCanvasSpaceClippingRect();

            var cull = !validRect || !clipRect.Overlaps(rect, true);
            if (m_canvasRenderer.cull != cull)
            {
                m_canvasRenderer.cull = cull;
                onCullStateChanged.Invoke(cull);
                OnCullingChanged();

                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    m_subTextObjects[i].canvasRenderer.cull = cull;
                }
            }
        }

        private bool m_ShouldUpdateCulling;
        private Rect m_ClipRect;
        private bool m_ValidRect;

        /// <summary>
        /// Internal function to allow delay of culling until the text geometry has been updated.
        /// </summary>
        internal override void UpdateCulling()
        {
            Rect rect = GetCanvasSpaceClippingRect();

            var cull = !m_ValidRect || !m_ClipRect.Overlaps(rect, true);
            if (m_canvasRenderer.cull != cull)
            {
                m_canvasRenderer.cull = cull;
                onCullStateChanged.Invoke(cull);
                OnCullingChanged();

                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    m_subTextObjects[i].canvasRenderer.cull = cull;
                }
            }

            m_ShouldUpdateCulling = false;
        }


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        public override void UpdateMeshPadding()
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
            _havePropertiesChanged = true;
            checkPaddingRequired = false;

            if (m_textInfo == null) return;

            for (int i = 1; i < m_textInfo.materialCount; i++)
                m_subTextObjects[i].UpdateMeshPadding(m_enableExtraPadding, m_isUsingBold);
        }


        /// <summary>
        /// Tweens the CanvasRenderer color associated with this Graphic.
        /// </summary>
        /// <param name="targetColor">Target color.</param>
        /// <param name="duration">Tween duration.</param>
        /// <param name="ignoreTimeScale">Should ignore Time.scale?</param>
        /// <param name="useAlpha">Should also Tween the alpha channel?</param>
        protected override void InternalCrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
        {
            if (m_textInfo == null)
                return;

            int materialCount = m_textInfo.materialCount;

            for (int i = 1; i < materialCount; i++)
            {
                m_subTextObjects[i].CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
            }
        }


        /// <summary>
        /// Tweens the alpha of the CanvasRenderer color associated with this Graphic.
        /// </summary>
        /// <param name="alpha">Target alpha.</param>
        /// <param name="duration">Duration of the tween in seconds.</param>
        /// <param name="ignoreTimeScale">Should ignore Time.scale?</param>
        protected override void InternalCrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            if (m_textInfo == null)
                return;

            int materialCount = m_textInfo.materialCount;

            for (int i = 1; i < materialCount; i++)
            {
                m_subTextObjects[i].CrossFadeAlpha(alpha, duration, ignoreTimeScale);
            }
        }


        /// <summary>
        /// Function to force regeneration of the text object before its normal process time. This is useful when changes to the text object properties need to be applied immediately.
        /// </summary>
        /// <param name="ignoreActiveState">Ignore Active State of text objects. Inactive objects are ignored by default.</param>
        public override void ForceMeshUpdate(bool ignoreActiveState = false)
        {
            _havePropertiesChanged = true;
            m_ignoreActiveState = ignoreActiveState;

            if (m_canvas == null)
                m_canvas = GetComponentInParent<Canvas>();

            OnPreRenderCanvas();
        }


        /// <summary>
        /// Function used to evaluate the length of a text string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public override TMP_TextInfo GetTextInfo(string text)
        {
            SetText(text);
            SetArraySizes(m_TextProcessingArray);

            m_renderMode = TextRenderFlags.DontRender;

            ComputeMarginSize();

            if (m_canvas == null) m_canvas = this.canvas;

            GenerateTextMesh();

            m_renderMode = TextRenderFlags.Render;

            return this.textInfo;
        }


        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        public override void ClearMesh()
        {
            m_canvasRenderer.SetMesh(null);

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                m_subTextObjects[i].canvasRenderer.SetMesh(null);
        }


        /// <summary>
        /// Event to allow users to modify the content of the text info before the text is rendered.
        /// </summary>
        public override event Action<TMP_TextInfo> OnPreRenderText;


        /// <summary>
        /// Function to update the geometry of the main and sub text objects.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="index"></param>
        public override void UpdateGeometry(Mesh mesh, int index)
        {
            mesh.RecalculateBounds();

            if (index == 0)
            {
                m_canvasRenderer.SetMesh(mesh);
            }
            else
            {
                m_subTextObjects[index].canvasRenderer.SetMesh(mesh);
            }
        }


        /// <summary>
        /// Function to upload the updated vertex data and renderer.
        /// </summary>
        public override void UpdateVertexData(TMP_VertexDataUpdateFlags flags)
        {
            int materialCount = m_textInfo.materialCount;

            for (int i = 0; i < materialCount; i++)
            {
                Mesh mesh;

                if (i == 0)
                    mesh = m_mesh;
                else
                {
                    mesh = m_subTextObjects[i].mesh;
                }

                if ((flags & TMP_VertexDataUpdateFlags.Vertices) == TMP_VertexDataUpdateFlags.Vertices)
                    mesh.vertices = m_textInfo.meshInfo[i].vertices;

                if ((flags & TMP_VertexDataUpdateFlags.Uv0) == TMP_VertexDataUpdateFlags.Uv0)
                    mesh.SetUVs(0, m_textInfo.meshInfo[i].uvs0);

                if ((flags & TMP_VertexDataUpdateFlags.Uv2) == TMP_VertexDataUpdateFlags.Uv2)
                    mesh.uv2 = m_textInfo.meshInfo[i].uvs2;

                if ((flags & TMP_VertexDataUpdateFlags.Colors32) == TMP_VertexDataUpdateFlags.Colors32)
                    mesh.colors32 = m_textInfo.meshInfo[i].colors32;

                mesh.RecalculateBounds();

                if (i == 0)
                    m_canvasRenderer.SetMesh(mesh);
                else
                    m_subTextObjects[i].canvasRenderer.SetMesh(mesh);
            }
        }


        /// <summary>
        /// Function to upload the updated vertex data and renderer.
        /// </summary>
        public override void UpdateVertexData()
        {
            int materialCount = m_textInfo.materialCount;

            for (int i = 0; i < materialCount; i++)
            {
                Mesh mesh;

                if (i == 0)
                    mesh = m_mesh;
                else
                {
                    m_textInfo.meshInfo[i].ClearUnusedVertices();

                    mesh = m_subTextObjects[i].mesh;
                }

                mesh.vertices = m_textInfo.meshInfo[i].vertices;
                mesh.SetUVs(0, m_textInfo.meshInfo[i].uvs0);
                mesh.uv2 = m_textInfo.meshInfo[i].uvs2;
                mesh.colors32 = m_textInfo.meshInfo[i].colors32;

                mesh.RecalculateBounds();

                if (i == 0)
                    m_canvasRenderer.SetMesh(mesh);
                else
                    m_subTextObjects[i].canvasRenderer.SetMesh(mesh);
            }
        }


        public void UpdateFontAsset()
        {
            LoadFontAsset();
        }

        #region TMPro_UGUI_Private
                [SerializeField]
        private bool m_hasFontAssetChanged = false;

        protected TMP_SubMeshUI[] m_subTextObjects = new TMP_SubMeshUI[8];

        private float m_previousLossyScaleY = -1;

        private Vector3[] m_RectTransformCorners = new Vector3[4];
        private CanvasRenderer m_canvasRenderer;
        private Canvas m_canvas;
        private float m_CanvasScaleFactor;


        private bool m_isFirstAllocation;

        private int m_max_characters = 8;

        [SerializeField]
        private Material m_baseMaterial;

        private bool m_isScrollRegionSet;

        [SerializeField]
        private Vector4 m_maskOffset;

        private Matrix4x4 m_EnvMapMatrix = new Matrix4x4();


        [NonSerialized]
        private bool m_isRegisteredForEvents;

        private static ProfilerMarker k_GenerateTextMarker = new ProfilerMarker("TMP.GenerateText");
        private static ProfilerMarker k_SetArraySizesMarker = new ProfilerMarker("TMP.SetArraySizes");
        private static ProfilerMarker k_GenerateTextPhaseIMarker = new ProfilerMarker("TMP GenerateText - Phase I");
        private static ProfilerMarker k_ParseMarkupTextMarker = new ProfilerMarker("TMP Parse Markup Text");
        private static ProfilerMarker k_CharacterLookupMarker = new ProfilerMarker("TMP Lookup Character & Glyph Data");
        private static ProfilerMarker k_HandleGPOSFeaturesMarker = new ProfilerMarker("TMP Handle GPOS Features");
        private static ProfilerMarker k_CalculateVerticesPositionMarker = new ProfilerMarker("TMP Calculate Vertices Position");
        private static ProfilerMarker k_ComputeTextMetricsMarker = new ProfilerMarker("TMP Compute Text Metrics");
        private static ProfilerMarker k_HandleVisibleCharacterMarker = new ProfilerMarker("TMP Handle Visible Character");
        private static ProfilerMarker k_HandleWhiteSpacesMarker = new ProfilerMarker("TMP Handle White Space & Control Character");
        private static ProfilerMarker k_HandleHorizontalLineBreakingMarker = new ProfilerMarker("TMP Handle Horizontal Line Breaking");
        private static ProfilerMarker k_HandleVerticalLineBreakingMarker = new ProfilerMarker("TMP Handle Vertical Line Breaking");
        private static ProfilerMarker k_SaveGlyphVertexDataMarker = new ProfilerMarker("TMP Save Glyph Vertex Data");
        private static ProfilerMarker k_ComputeCharacterAdvanceMarker = new ProfilerMarker("TMP Compute Character Advance");
        private static ProfilerMarker k_HandleCarriageReturnMarker = new ProfilerMarker("TMP Handle Carriage Return");
        private static ProfilerMarker k_HandleLineTerminationMarker = new ProfilerMarker("TMP Handle Line Termination");
        private static ProfilerMarker k_SavePageInfoMarker = new ProfilerMarker("TMP Save Page Info");
        private static ProfilerMarker k_SaveTextExtentMarker = new ProfilerMarker("TMP Save Text Extent");
        private static ProfilerMarker k_SaveProcessingStatesMarker = new ProfilerMarker("TMP Save Processing States");
        private static ProfilerMarker k_GenerateTextPhaseIIMarker = new ProfilerMarker("TMP GenerateText - Phase II");
        private static ProfilerMarker k_GenerateTextPhaseIIIMarker = new ProfilerMarker("TMP GenerateText - Phase III");


        protected override void Awake()
        {
#if UNITY_EDITOR
            if (TMP_Settings.instance == null)
            {
                if (m_isWaitingOnResourceLoad == false)
                    TMPro_EventManager.RESOURCE_LOAD_EVENT.Add(ON_RESOURCES_LOADED);

                m_isWaitingOnResourceLoad = true;
                return;
            }
            #endif

            m_canvas = this.canvas;

            m_isOrthographic = true;

            m_rectTransform = gameObject.GetComponent<RectTransform>();
            if (m_rectTransform == null)
                m_rectTransform = gameObject.AddComponent<RectTransform>();

            m_canvasRenderer = GetComponent<CanvasRenderer>();
            if (m_canvasRenderer == null)
                m_canvasRenderer = gameObject.AddComponent<CanvasRenderer> ();

            if (m_mesh == null)
            {
                m_mesh = new Mesh();
                m_mesh.hideFlags = HideFlags.HideAndDontSave;
                #if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_mesh.name = "TextMeshPro UI Mesh";
                #endif
                m_textInfo = new TMP_TextInfo(this);
            }

            LoadDefaultSettings();

#if UNITY_EDITOR
            if (!UnityEditor.BuildPipeline.isBuildingPlayer)
#endif
                LoadFontAsset();

            if (m_TextProcessingArray == null)
                m_TextProcessingArray = new TextProcessingElement[m_max_characters];

            m_cached_TextElement = new TMP_Character();
            m_isFirstAllocation = true;

            _havePropertiesChanged = true;

            m_isAwake = true;
        }


        protected override void OnEnable()
        {
            if (m_isAwake == false)
                return;

            if (!m_isRegisteredForEvents)
            {
#if UNITY_EDITOR
                TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Add(ON_MATERIAL_PROPERTY_CHANGED);
                TMPro_EventManager.FONT_PROPERTY_EVENT.Add(ON_FONT_PROPERTY_CHANGED);
                TMPro_EventManager.TEXTMESHPRO_UGUI_PROPERTY_EVENT.Add(ON_TEXTMESHPRO_UGUI_PROPERTY_CHANGED);
                TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Add(ON_DRAG_AND_DROP_MATERIAL);
                TMPro_EventManager.TEXT_STYLE_PROPERTY_EVENT.Add(ON_TEXT_STYLE_CHANGED);
                TMPro_EventManager.COLOR_GRADIENT_PROPERTY_EVENT.Add(ON_COLOR_GRADIENT_CHANGED);
                TMPro_EventManager.TMP_SETTINGS_PROPERTY_EVENT.Add(ON_TMP_SETTINGS_CHANGED);

                UnityEditor.PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
                #endif
                m_isRegisteredForEvents = true;
            }

            m_canvas = GetCanvas();

            SetActiveSubMeshes(true);

            GraphicRegistry.RegisterGraphicForCanvas(m_canvas, this);

            if (m_IsTextObjectScaleStatic == false)
                TMP_UpdateManager.RegisterTextObjectForUpdate(this);

            ComputeMarginSize();

            SetAllDirty();

            RecalculateClipping();
            RecalculateMasking();
        }


        protected override void OnDisable()
        {
            if (m_isAwake == false)
                return;

            GraphicRegistry.UnregisterGraphicForCanvas(m_canvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild((ICanvasElement)this);

            TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);

            if (m_canvasRenderer != null)
                m_canvasRenderer.Clear();

            SetActiveSubMeshes(false);

            LayoutRebuilder.MarkLayoutForRebuild(m_rectTransform);
            RecalculateClipping();
            RecalculateMasking();
        }


        protected override void OnDestroy()
        {
            GraphicRegistry.UnregisterGraphicForCanvas(m_canvas, this);

            TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);

            if (m_mesh != null)
                DestroyImmediate(m_mesh);

            if (m_MaskMaterial != null)
            {
                TMP_MaterialManager.ReleaseStencilMaterial(m_MaskMaterial);
                m_MaskMaterial = null;
            }

            #if UNITY_EDITOR
            TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Remove(ON_MATERIAL_PROPERTY_CHANGED);
            TMPro_EventManager.FONT_PROPERTY_EVENT.Remove(ON_FONT_PROPERTY_CHANGED);
            TMPro_EventManager.TEXTMESHPRO_UGUI_PROPERTY_EVENT.Remove(ON_TEXTMESHPRO_UGUI_PROPERTY_CHANGED);
            TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Remove(ON_DRAG_AND_DROP_MATERIAL);
            TMPro_EventManager.TEXT_STYLE_PROPERTY_EVENT.Remove(ON_TEXT_STYLE_CHANGED);
            TMPro_EventManager.COLOR_GRADIENT_PROPERTY_EVENT.Remove(ON_COLOR_GRADIENT_CHANGED);
            TMPro_EventManager.TMP_SETTINGS_PROPERTY_EVENT.Remove(ON_TMP_SETTINGS_CHANGED);
            TMPro_EventManager.RESOURCE_LOAD_EVENT.Remove(ON_RESOURCES_LOADED);

            UnityEditor.PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdate;
            #endif
            m_isRegisteredForEvents = false;
        }


        #if UNITY_EDITOR
        protected override void Reset()
        {
            if (m_isAwake == false)
                return;

            LoadDefaultSettings();
            LoadFontAsset();

            _havePropertiesChanged = true;
        }


        protected override void OnValidate()
        {
            if (m_isAwake == false)
                return;

            if (m_fontAsset == null || m_hasFontAssetChanged)
            {
                LoadFontAsset();
                m_hasFontAssetChanged = false;
            }

            if (m_canvasRenderer == null || m_canvasRenderer.GetMaterial() == null || m_canvasRenderer.GetMaterial().GetTexture(ShaderUtilities.ID_MainTex) == null || m_fontAsset == null || m_fontAsset.atlasTexture.GetInstanceID() != m_canvasRenderer.GetMaterial().GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
            {
                LoadFontAsset();
                m_hasFontAssetChanged = false;
            }

            m_padding = GetPaddingForMaterial();
            ComputeMarginSize();
            
            _havePropertiesChanged = true;
            m_isPreferredWidthDirty = true;
            m_isPreferredHeightDirty = true;

            SetAllDirty();
        }


        /// <summary>
        /// Callback received when Prefabs are updated.
        /// </summary>
        /// <param name="go">The affected GameObject</param>
        private void OnPrefabInstanceUpdate(GameObject go)
        {
            if (this == null)
            {
                UnityEditor.PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdate;
                return;
            }

            if (go == this.gameObject)
            {
                TMP_SubMeshUI[] subTextObjects = GetComponentsInChildren<TMP_SubMeshUI>();
                if (subTextObjects.Length > 0)
                {
                    for (int i = 0; i < subTextObjects.Length; i++)
                        m_subTextObjects[i + 1] = subTextObjects[i];
                }
            }
        }


        private void ON_RESOURCES_LOADED()
        {
            TMPro_EventManager.RESOURCE_LOAD_EVENT.Remove(ON_RESOURCES_LOADED);

            if (this == null)
                return;

            m_isWaitingOnResourceLoad = false;

            Awake();
            OnEnable();
        }


        private void ON_MATERIAL_PROPERTY_CHANGED(bool isChanged, Material mat)
        {
            ShaderUtilities.GetShaderPropertyIDs();

            int materialID = mat.GetInstanceID();
            int sharedMaterialID = m_sharedMaterial.GetInstanceID();
            int maskingMaterialID = m_MaskMaterial == null ? 0 : m_MaskMaterial.GetInstanceID();

            if (m_canvasRenderer == null || m_canvasRenderer.GetMaterial() == null)
            {
                if (m_canvasRenderer == null) return;

                if (m_fontAsset != null)
                {
                    m_canvasRenderer.SetMaterial(m_fontAsset.material, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
                }
                else
                    Debug.LogWarning("No Font Asset assigned to " + name + ". Please assign a Font Asset.", this);
            }


            if (m_canvasRenderer.GetMaterial() != m_sharedMaterial && m_fontAsset == null)
            {
                m_sharedMaterial = m_canvasRenderer.GetMaterial();
            }


            if (m_MaskMaterial != null)
            {
                UnityEditor.Undo.RecordObject(m_MaskMaterial, "Material Property Changes");
                UnityEditor.Undo.RecordObject(m_sharedMaterial, "Material Property Changes");

                if (materialID == sharedMaterialID)
                {
                    float stencilID = m_MaskMaterial.GetFloat(ShaderUtilities.ID_StencilID);
                    float stencilComp = m_MaskMaterial.GetFloat(ShaderUtilities.ID_StencilComp);

                    m_MaskMaterial.CopyPropertiesFromMaterial(mat);
                    m_MaskMaterial.shaderKeywords = mat.shaderKeywords;

                    m_MaskMaterial.SetFloat(ShaderUtilities.ID_StencilID, stencilID);
                    m_MaskMaterial.SetFloat(ShaderUtilities.ID_StencilComp, stencilComp);
                }
                else if (materialID == maskingMaterialID)
                {
                    GetPaddingForMaterial(mat);

                    m_sharedMaterial.CopyPropertiesFromMaterial(mat);
                    m_sharedMaterial.shaderKeywords = mat.shaderKeywords;
                    m_sharedMaterial.SetFloat(ShaderUtilities.ID_StencilID, 0);
                    m_sharedMaterial.SetFloat(ShaderUtilities.ID_StencilComp, 8);
                }

            }

            m_padding = GetPaddingForMaterial();
            ValidateEnvMapProperty();
            _havePropertiesChanged = true;
            SetVerticesDirty();
        }


        private void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object font)
        {
            {
                _havePropertiesChanged = true;

                UpdateMeshPadding();

                SetLayoutDirty();
                SetVerticesDirty();
            }
        }


        private void ON_TEXTMESHPRO_UGUI_PROPERTY_CHANGED(bool isChanged, Object obj)
        {
            if (obj == this)
            {
                _havePropertiesChanged = true;

                ComputeMarginSize();
                SetVerticesDirty();
            }
        }


        private void ON_DRAG_AND_DROP_MATERIAL(GameObject obj, Material currentMaterial, Material newMaterial)
        {
            if (obj == gameObject || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == obj)
            {
                UnityEditor.Undo.RecordObject(this, "Material Assignment");
                UnityEditor.Undo.RecordObject(m_canvasRenderer, "Material Assignment");

                m_sharedMaterial = newMaterial;

                m_padding = GetPaddingForMaterial();

                _havePropertiesChanged = true;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }


        private void ON_TEXT_STYLE_CHANGED(bool isChanged)
        {
            _havePropertiesChanged = true;
            SetVerticesDirty();
        }


        /// <summary>
        /// Event received when a Color Gradient Preset is modified.
        /// </summary>
        /// <param name="textObject"></param>
        private void ON_COLOR_GRADIENT_CHANGED(Object gradient)
        {
            _havePropertiesChanged = true;
            SetVerticesDirty();
        }


        /// <summary>
        /// Event received when the TMP Settings are changed.
        /// </summary>
        private void ON_TMP_SETTINGS_CHANGED()
        {
            m_defaultSpriteAsset = null;
            _havePropertiesChanged = true;
            SetAllDirty();
        }
        #endif


        protected override void LoadFontAsset()
        {
            ShaderUtilities.GetShaderPropertyIDs();

            if (m_fontAsset == null)
            {
                if (TMP_Settings.defaultFontAsset != null)
                    m_fontAsset = TMP_Settings.defaultFontAsset;

                if (m_fontAsset == null)
                {
                    Debug.LogWarning("The LiberationSans SDF Font Asset was not found. There is no Font Asset assigned to " + gameObject.name + ".", this);
                    return;
                }

                if (m_fontAsset.characterLookupTable == null)
                {
                    Debug.Log("Dictionary is Null!");
                }

                m_sharedMaterial = m_fontAsset.material;
            }
            else
            {
                if (m_fontAsset.characterLookupTable == null)
                    m_fontAsset.ReadFontAssetDefinition();

                if (m_sharedMaterial == null && m_baseMaterial != null)
                {
                    m_sharedMaterial = m_baseMaterial;
                    m_baseMaterial = null;
                }

                if (m_sharedMaterial == null || m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex) == null || m_fontAsset.atlasTexture.GetInstanceID() != m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                {
                    if (m_fontAsset.material == null)
                        Debug.LogWarning("The Font Atlas Texture of the Font Asset " + m_fontAsset.name + " assigned to " + gameObject.name + " is missing.", this);
                    else
                        m_sharedMaterial = m_fontAsset.material;
                }
            }

            ValidateEnvMapProperty();

            GetSpecialCharacters(m_fontAsset);

            m_padding = GetPaddingForMaterial();

            SetMaterialDirty();
        }


        /// <summary>
        /// Method to retrieve the parent Canvas.
        /// </summary>
        private Canvas GetCanvas()
        {
            Canvas canvas = null;
            var list = TMP_ListPool<Canvas>.Get();

            gameObject.GetComponentsInParent(false, list);
            if (list.Count > 0)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].isActiveAndEnabled)
                    {
                        canvas = list[i];
                        break;
                    }
                }
            }

            TMP_ListPool<Canvas>.Release(list);

            return canvas;
        }

        /// <summary>
        /// Method to check if the environment map property is valid.
        /// </summary>
        private void ValidateEnvMapProperty()
        {
            if (m_sharedMaterial != null)
                m_hasEnvMapProperty = m_sharedMaterial.HasProperty(ShaderUtilities.ID_EnvMap) && m_sharedMaterial.GetTexture(ShaderUtilities.ID_EnvMap) != null;
            else
                m_hasEnvMapProperty = false;
        }

        /// <summary>
        /// Method used when animating the Env Map on the material.
        /// </summary>
        private void UpdateEnvMapMatrix()
        {
            if (!m_hasEnvMapProperty)
                return;

            Vector3 rotation = m_sharedMaterial.GetVector(ShaderUtilities.ID_EnvMatrixRotation);
            #if !UNITY_EDITOR
            if (m_currentEnvMapRotation == rotation)
                return;
            #endif

            m_currentEnvMapRotation = rotation;
            m_EnvMapMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_currentEnvMapRotation), Vector3.one);

            m_sharedMaterial.SetMatrix(ShaderUtilities.ID_EnvMatrix, m_EnvMapMatrix);
        }


        private void EnableMasking()
        {
            if (m_fontMaterial == null)
            {
                m_fontMaterial = CreateMaterialInstance(m_sharedMaterial);
                m_canvasRenderer.SetMaterial(m_fontMaterial, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
            }

            m_sharedMaterial = m_fontMaterial;
            if (m_sharedMaterial.HasProperty(ShaderUtilities.ID_ClipRect))
            {
                m_sharedMaterial.EnableKeyword(ShaderUtilities.Keyword_MASK_SOFT);
                m_sharedMaterial.DisableKeyword(ShaderUtilities.Keyword_MASK_HARD);
                m_sharedMaterial.DisableKeyword(ShaderUtilities.Keyword_MASK_TEX);

                UpdateMask();
            }

            m_isMaskingEnabled = true;
        }


        private void DisableMasking()
        {
        }


        private void UpdateMask()
        {
            if (m_rectTransform != null)
            {
                if (!ShaderUtilities.isInitialized)
                    ShaderUtilities.GetShaderPropertyIDs();

                m_isScrollRegionSet = true;

                float softnessX = Mathf.Min(Mathf.Min(m_margin.x, m_margin.z), m_sharedMaterial.GetFloat(ShaderUtilities.ID_MaskSoftnessX));
                float softnessY = Mathf.Min(Mathf.Min(m_margin.y, m_margin.w), m_sharedMaterial.GetFloat(ShaderUtilities.ID_MaskSoftnessY));

                softnessX = softnessX > 0 ? softnessX : 0;
                softnessY = softnessY > 0 ? softnessY : 0;

                float width = (m_rectTransform.rect.width - Mathf.Max(m_margin.x, 0) - Mathf.Max(m_margin.z, 0)) / 2 + softnessX;
                float height = (m_rectTransform.rect.height - Mathf.Max(m_margin.y, 0) - Mathf.Max(m_margin.w, 0)) / 2 + softnessY;


                Vector2 center = m_rectTransform.localPosition + new Vector3((0.5f - m_rectTransform.pivot.x) * m_rectTransform.rect.width + (Mathf.Max(m_margin.x, 0) - Mathf.Max(m_margin.z, 0)) / 2, (0.5f - m_rectTransform.pivot.y) * m_rectTransform.rect.height + (-Mathf.Max(m_margin.y, 0) + Mathf.Max(m_margin.w, 0)) / 2);

                Vector4 mask = new Vector4(center.x, center.y, width, height);


                m_sharedMaterial.SetVector(ShaderUtilities.ID_ClipRect, mask);
            }
        }


        protected override Material GetMaterial(Material mat)
        {
            ShaderUtilities.GetShaderPropertyIDs();

            if (m_fontMaterial == null || m_fontMaterial.GetInstanceID() != mat.GetInstanceID())
                m_fontMaterial = CreateMaterialInstance(mat);

            m_sharedMaterial = m_fontMaterial;

            m_padding = GetPaddingForMaterial();

            m_ShouldRecalculateStencil = true;
            SetVerticesDirty();
            SetMaterialDirty();

            return m_sharedMaterial;
        }


        /// <summary>
        /// Method returning instances of the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected override Material[] GetMaterials(Material[] mats)
        {
            int materialCount = m_textInfo.materialCount;

            if (m_fontMaterials == null)
                m_fontMaterials = new Material[materialCount];
            else if (m_fontMaterials.Length != materialCount)
                TMP_TextInfo.Resize(ref m_fontMaterials, materialCount, false);

            for (int i = 0; i < materialCount; i++)
            {
                if (i == 0)
                    m_fontMaterials[i] = fontMaterial;
                else
                    m_fontMaterials[i] = m_subTextObjects[i].material;
            }

            m_fontSharedMaterials = m_fontMaterials;

            return m_fontMaterials;
        }


        protected override void SetSharedMaterial(Material mat)
        {
            m_sharedMaterial = mat;

            m_padding = GetPaddingForMaterial();

            SetMaterialDirty();
        }


        /// <summary>
        /// Method returning an array containing the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected override Material[] GetSharedMaterials()
        {
            int materialCount = m_textInfo.materialCount;

            if (m_fontSharedMaterials == null)
                m_fontSharedMaterials = new Material[materialCount];
            else if (m_fontSharedMaterials.Length != materialCount)
                TMP_TextInfo.Resize(ref m_fontSharedMaterials, materialCount, false);

            for (int i = 0; i < materialCount; i++)
            {
                if (i == 0)
                    m_fontSharedMaterials[i] = m_sharedMaterial;
                else
                    m_fontSharedMaterials[i] = m_subTextObjects[i].sharedMaterial;
            }

            return m_fontSharedMaterials;
        }


        /// <summary>
        /// Method used to assign new materials to the text and sub text objects.
        /// </summary>
        protected override void SetSharedMaterials(Material[] materials)
        {
            int materialCount = m_textInfo.materialCount;

            if (m_fontSharedMaterials == null)
                m_fontSharedMaterials = new Material[materialCount];
            else if (m_fontSharedMaterials.Length != materialCount)
                TMP_TextInfo.Resize(ref m_fontSharedMaterials, materialCount, false);

            for (int i = 0; i < materialCount; i++)
            {
                if (i == 0)
                {
                    if (materials[i].GetTexture(ShaderUtilities.ID_MainTex) == null || materials[i].GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() != m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                        continue;

                    m_sharedMaterial = m_fontSharedMaterials[i] = materials[i];
                    m_padding = GetPaddingForMaterial(m_sharedMaterial);
                }
                else
                {
                    if (materials[i].GetTexture(ShaderUtilities.ID_MainTex) == null || materials[i].GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() != m_subTextObjects[i].sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                        continue;

                    if (m_subTextObjects[i].isDefaultMaterial)
                        m_subTextObjects[i].sharedMaterial = m_fontSharedMaterials[i] = materials[i];
                }
            }
        }


        protected override void SetOutlineThickness(float thickness)
        {
            if (m_fontMaterial != null && m_sharedMaterial.GetInstanceID() != m_fontMaterial.GetInstanceID())
            {
                m_sharedMaterial = m_fontMaterial;
                m_canvasRenderer.SetMaterial(m_sharedMaterial, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
            }
            else if(m_fontMaterial == null)
            {
                m_fontMaterial = CreateMaterialInstance(m_sharedMaterial);
                m_sharedMaterial = m_fontMaterial;
                m_canvasRenderer.SetMaterial(m_sharedMaterial, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
            }

            thickness = Mathf.Clamp01(thickness);
            m_sharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, thickness);
            m_padding = GetPaddingForMaterial();
        }


        protected override void SetFaceColor(Color32 color)
        {
            if (m_fontMaterial == null)
                m_fontMaterial = CreateMaterialInstance(m_sharedMaterial);

            m_sharedMaterial = m_fontMaterial;
            m_padding = GetPaddingForMaterial();

            m_sharedMaterial.SetColor(ShaderUtilities.ID_FaceColor, color);
        }


        protected override void SetOutlineColor(Color32 color)
        {
            if (m_fontMaterial == null)
                m_fontMaterial = CreateMaterialInstance(m_sharedMaterial);

            m_sharedMaterial = m_fontMaterial;
            m_padding = GetPaddingForMaterial();

            m_sharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, color);
        }


        protected override void SetShaderDepth()
        {
            if (m_canvas == null || m_sharedMaterial == null)
                return;

            if (m_canvas.renderMode == RenderMode.ScreenSpaceOverlay || m_isOverlay)
            {
            }
            else
            {
            }
        }


        protected override void SetCulling()
        {
            if (m_isCullingEnabled)
            {
                Material mat = materialForRendering;

                if (mat != null)
                    mat.SetFloat("_CullMode", 2);

                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    mat = m_subTextObjects[i].materialForRendering;

                    if (mat != null)
                    {
                        mat.SetFloat(ShaderUtilities.ShaderTag_CullMode, 2);
                    }
                }
            }
            else
            {
                Material mat = materialForRendering;

                if (mat != null)
                    mat.SetFloat("_CullMode", 0);

                for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                {
                    mat = m_subTextObjects[i].materialForRendering;

                    if (mat != null)
                    {
                        mat.SetFloat(ShaderUtilities.ShaderTag_CullMode, 0);
                    }
                }
            }
        }


        private void SetPerspectiveCorrection()
        {
            if (m_isOrthographic)
                m_sharedMaterial.SetFloat(ShaderUtilities.ID_PerspectiveFilter, 0.0f);
            else
                m_sharedMaterial.SetFloat(ShaderUtilities.ID_PerspectiveFilter, 0.875f);
        }


        private void SetMeshArrays(int size)
        {
            m_textInfo.meshInfo[0].ResizeMeshInfo(size);

            m_canvasRenderer.SetMesh(m_textInfo.meshInfo[0].mesh);
        }


        private Dictionary<int, int> materialIndexPairs = new Dictionary<int, int>();

        internal override int SetArraySizes(TextProcessingElement[] textProcessingArray)
        {
            k_SetArraySizesMarker.Begin();

            int spriteCount = 0;

            m_totalCharacterCount = 0;
            m_isUsingBold = false;
            m_isTextLayoutPhase = false;
            tag_NoParsing = false;
            m_FontStyleInternal = m_fontStyle;
            m_fontStyleStack.Clear();

            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? FontWeight.Bold : m_fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;

            m_materialReferenceStack.SetDefault(new MaterialReference(m_currentMaterialIndex, m_currentFontAsset, null, m_currentMaterial, m_padding));

            m_materialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

            if (m_textInfo == null)
                m_textInfo = new TMP_TextInfo(m_InternalTextProcessingArraySize);
            else if (m_textInfo.characterInfo.Length < m_InternalTextProcessingArraySize)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_InternalTextProcessingArraySize, false);

            m_textElementType = TMP_TextElementType.Character;

            #region Setup Ellipsis Special Character
            if (m_overflowMode == TextOverflowModes.Ellipsis)
            {
                GetEllipsisSpecialCharacter(m_currentFontAsset);

                if (m_Ellipsis.character != null)
                {
                    if (m_Ellipsis.fontAsset.GetInstanceID() != m_currentFontAsset.GetInstanceID())
                    {
                        if (TMP_Settings.matchMaterialPreset && m_currentMaterial.GetInstanceID() != m_Ellipsis.fontAsset.material.GetInstanceID())
                            m_Ellipsis.material = TMP_MaterialManager.GetFallbackMaterial(m_currentMaterial, m_Ellipsis.fontAsset.material);
                        else
                            m_Ellipsis.material = m_Ellipsis.fontAsset.material;

                        m_Ellipsis.materialIndex = MaterialReference.AddMaterialReference(m_Ellipsis.material, m_Ellipsis.fontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                        m_materialReferences[m_Ellipsis.materialIndex].referenceCount = 0;
                    }
                }
                else
                {
                    m_overflowMode = TextOverflowModes.Truncate;

                    if (!TMP_Settings.warningsDisabled)
                        Debug.LogWarning("The character used for Ellipsis is not available in font asset [" + m_currentFontAsset.name + "] or any potential fallbacks. Switching Text Overflow mode to Truncate.", this);
                }
            }
            #endregion

            bool ligature = m_ActiveFontFeatures.Contains(OTL_FeatureTag.liga);

            if (m_overflowMode == TextOverflowModes.Linked && m_linkedTextComponent != null && !m_isCalculatingPreferredValues)
            {
                TMP_Text linkedComponent = m_linkedTextComponent;

                while (linkedComponent != null)
                {
                    linkedComponent.text = String.Empty;
                    linkedComponent.ClearMesh();
                    linkedComponent.textInfo.Clear();

                    linkedComponent = linkedComponent.linkedTextComponent;
                }
            }


            for (int i = 0; i < textProcessingArray.Length && textProcessingArray[i].unicode != 0; i++)
            {
                if (m_textInfo.characterInfo == null || m_totalCharacterCount >= m_textInfo.characterInfo.Length)
                    TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_totalCharacterCount + 1, true);

                uint unicode = textProcessingArray[i].unicode;

                #region PARSE XML TAGS
                if (m_isRichText && unicode == 60)
                {
                    int prev_MaterialIndex = m_currentMaterialIndex;
                    int endTagIndex;

                    if (ValidateHtmlTag(textProcessingArray, i + 1, out endTagIndex))
                    {
                        int tagStartIndex = textProcessingArray[i].stringIndex;
                        i = endTagIndex;

                        if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)
                            m_isUsingBold = true;

                        if (m_textElementType == TMP_TextElementType.Sprite)
                        {
                            m_materialReferences[m_currentMaterialIndex].referenceCount += 1;

                            m_textInfo.characterInfo[m_totalCharacterCount].character = (char)(57344 + m_spriteIndex);
                            m_textInfo.characterInfo[m_totalCharacterCount].fontAsset = m_currentFontAsset;
                            m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;
                            m_textInfo.characterInfo[m_totalCharacterCount].textElement = m_currentSpriteAsset.spriteCharacterTable[m_spriteIndex];
                            m_textInfo.characterInfo[m_totalCharacterCount].elementType = m_textElementType;
                            m_textInfo.characterInfo[m_totalCharacterCount].index = tagStartIndex;
                            m_textInfo.characterInfo[m_totalCharacterCount].stringLength = textProcessingArray[i].stringIndex - tagStartIndex + 1;

                            m_textElementType = TMP_TextElementType.Character;
                            m_currentMaterialIndex = prev_MaterialIndex;

                            spriteCount += 1;
                            m_totalCharacterCount += 1;
                        }

                        continue;
                    }
                }
                #endregion

                bool isUsingAlternativeTypeface = false;
                bool isUsingFallbackOrAlternativeTypeface = false;

                TMP_FontAsset prev_fontAsset = m_currentFontAsset;
                Material prev_material = m_currentMaterial;
                int prev_materialIndex = m_currentMaterialIndex;

                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles
                if (m_textElementType == TMP_TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        if (char.IsUpper((char)unicode))
                            unicode = char.ToLower((char)unicode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);
                    }
                }
                #endregion

                #region LOOKUP GLYPH
                TMP_TextElement character = null;

                uint nextCharacter = i + 1 < textProcessingArray.Length ? textProcessingArray[i + 1].unicode : 0;

                if (emojiFallbackSupport && ((TMP_TextParsingUtilities.IsEmojiPresentationForm(unicode) && nextCharacter != 0xFE0E) || (TMP_TextParsingUtilities.IsEmoji(unicode) && nextCharacter == 0xFE0F)))
                {
                    if (TMP_Settings.emojiFallbackTextAssets != null && TMP_Settings.emojiFallbackTextAssets.Count > 0)
                    {
                        character = TMP_FontAssetUtilities.GetTextElementFromTextAssets(unicode, m_currentFontAsset, TMP_Settings.emojiFallbackTextAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                        if (character != null)
                        {
                        }
                    }
                }

                if (character == null)
                    character = GetTextElement(unicode, m_currentFontAsset, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

                #region MISSING CHARACTER HANDLING

                if (character == null)
                {
                    DoMissingGlyphCallback((int)unicode, textProcessingArray[i].stringIndex, m_currentFontAsset);

                    uint srcGlyph = unicode;

                    unicode = textProcessingArray[i].unicode = (uint)TMP_Settings.missingGlyphCharacter == 0 ? 9633 : (uint)TMP_Settings.missingGlyphCharacter;

                    character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, m_currentFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);

                    if (character == null)
                    {
                        if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                            character = TMP_FontAssetUtilities.GetCharacterFromFontAssets(unicode, m_currentFontAsset, TMP_Settings.fallbackFontAssets, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        if (TMP_Settings.defaultFontAsset != null)
                            character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, TMP_Settings.defaultFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        unicode = textProcessingArray[i].unicode = 32;
                        character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, m_currentFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        unicode = textProcessingArray[i].unicode = 0x03;
                        character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, m_currentFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                    }

                    if (!TMP_Settings.warningsDisabled)
                    {
                        string formattedWarning = srcGlyph > 0xFFFF
                            ? string.Format("The character with Unicode value \\U{0:X8} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4} in text object [{3}].", srcGlyph, m_fontAsset.name, character.unicode, this.name)
                            : string.Format("The character with Unicode value \\u{0:X4} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4} in text object [{3}].", srcGlyph, m_fontAsset.name, character.unicode, this.name);

                        Debug.LogWarning(formattedWarning, this);
                    }
                }
                #endregion

                m_textInfo.characterInfo[m_totalCharacterCount].alternativeGlyph = null;

                if (character.elementType == TextElementType.Character)
                {
                    if (character.textAsset.instanceID != m_currentFontAsset.instanceID)
                    {
                        isUsingFallbackOrAlternativeTypeface = true;
                        m_currentFontAsset = character.textAsset as TMP_FontAsset;
                    }

                    #region VARIATION SELECTOR
                    if (nextCharacter >= 0xFE00 && nextCharacter <= 0xFE0F || nextCharacter >= 0xE0100 && nextCharacter <= 0xE01EF)
                    {
                        uint variantGlyphIndex = m_currentFontAsset.GetGlyphVariantIndex((uint)unicode, nextCharacter);

                        if (variantGlyphIndex != 0)
                        {
                            if (m_currentFontAsset.TryAddGlyphInternal(variantGlyphIndex, out Glyph glyph))
                            {
                                m_textInfo.characterInfo[m_totalCharacterCount].alternativeGlyph = glyph;
                            }
                        }

                        textProcessingArray[i + 1].unicode = 0x1A;
                        i += 1;
                    }
                    #endregion

                    #region LIGATURES
                    if (ligature && m_currentFontAsset.fontFeatureTable.m_LigatureSubstitutionRecordLookup.TryGetValue(character.glyphIndex, out List<LigatureSubstitutionRecord> records))
                    {
                        if (records == null)
                            break;

                        for (int j = 0; j < records.Count; j++)
                        {
                            LigatureSubstitutionRecord record = records[j];

                            int componentCount = record.componentGlyphIDs.Length;
                            uint ligatureGlyphID = record.ligatureGlyphID;

                            for (int k = 1; k < componentCount; k++)
                            {
                                uint componentUnicode = (uint)textProcessingArray[i + k].unicode;

                                uint glyphIndex = m_currentFontAsset.GetGlyphIndex(componentUnicode);

                                if (glyphIndex == record.componentGlyphIDs[k])
                                    continue;

                                ligatureGlyphID = 0;
                                break;
                            }

                            if (ligatureGlyphID != 0)
                            {
                                if (m_currentFontAsset.TryAddGlyphInternal(ligatureGlyphID, out Glyph glyph))
                                {
                                    m_textInfo.characterInfo[m_totalCharacterCount].alternativeGlyph = glyph;

                                    for (int c = 0; c < componentCount; c++)
                                    {
                                        if (c == 0)
                                        {
                                            textProcessingArray[i + c].length = componentCount;
                                            continue;
                                        }

                                        textProcessingArray[i + c].unicode = 0x1A;
                                    }

                                    i += componentCount - 1;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                m_textInfo.characterInfo[m_totalCharacterCount].elementType = TMP_TextElementType.Character;
                m_textInfo.characterInfo[m_totalCharacterCount].textElement = character;
                m_textInfo.characterInfo[m_totalCharacterCount].isUsingAlternateTypeface = isUsingAlternativeTypeface;
                m_textInfo.characterInfo[m_totalCharacterCount].character = (char)unicode;
                m_textInfo.characterInfo[m_totalCharacterCount].index = textProcessingArray[i].stringIndex;
                m_textInfo.characterInfo[m_totalCharacterCount].stringLength = textProcessingArray[i].length;
                m_textInfo.characterInfo[m_totalCharacterCount].fontAsset = m_currentFontAsset;

                if (character.elementType == TextElementType.Sprite)
                {
                    TMP_SpriteAsset spriteAssetRef = character.textAsset as TMP_SpriteAsset;
                    m_currentMaterialIndex = MaterialReference.AddMaterialReference(spriteAssetRef.material, spriteAssetRef, ref m_materialReferences, m_materialReferenceIndexLookup);
                    m_materialReferences[m_currentMaterialIndex].referenceCount += 1;

                    m_textInfo.characterInfo[m_totalCharacterCount].elementType = TMP_TextElementType.Sprite;
                    m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;

                    m_textElementType = TMP_TextElementType.Character;
                    m_currentMaterialIndex = prev_materialIndex;

                    spriteCount += 1;
                    m_totalCharacterCount += 1;

                    continue;
                }

                if (isUsingFallbackOrAlternativeTypeface && m_currentFontAsset.instanceID != m_fontAsset.instanceID)
                {
                    if (TMP_Settings.matchMaterialPreset)
                        m_currentMaterial = TMP_MaterialManager.GetFallbackMaterial(m_currentMaterial, m_currentFontAsset.material);
                    else
                        m_currentMaterial = m_currentFontAsset.material;

                    m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                }

                if (character != null && character.glyph.atlasIndex > 0)
                {
                    m_currentMaterial = TMP_MaterialManager.GetFallbackMaterial(m_currentFontAsset, m_currentMaterial, character.glyph.atlasIndex);

                    m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

                    isUsingFallbackOrAlternativeTypeface = true;
                }

                if (!char.IsWhiteSpace((char)unicode) && unicode != 0x200B)
                {
                    if (m_materialReferences[m_currentMaterialIndex].referenceCount < 16383)
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    else if (isUsingFallbackOrAlternativeTypeface)
                    {
                        if (materialIndexPairs.TryGetValue(m_currentMaterialIndex, out int prev_fallbackMaterialIndex) && m_materialReferences[prev_fallbackMaterialIndex].referenceCount < 16383)
                        {
                            m_currentMaterialIndex = prev_fallbackMaterialIndex;
                        }
                        else
                        {
                            int fallbackMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_currentMaterial), m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                            materialIndexPairs[m_currentMaterialIndex] = fallbackMaterialIndex;
                            m_currentMaterialIndex = fallbackMaterialIndex;
                        }

                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    }
                    else
                    {
                        m_currentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_currentMaterial), m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    }
                }

                m_textInfo.characterInfo[m_totalCharacterCount].material = m_currentMaterial;
                m_textInfo.characterInfo[m_totalCharacterCount].materialReferenceIndex = m_currentMaterialIndex;
                m_materialReferences[m_currentMaterialIndex].isFallbackMaterial = isUsingFallbackOrAlternativeTypeface;

                if (isUsingFallbackOrAlternativeTypeface)
                {
                    m_materialReferences[m_currentMaterialIndex].fallbackMaterial = prev_material;
                    m_currentFontAsset = prev_fontAsset;
                    m_currentMaterial = prev_material;
                    m_currentMaterialIndex = prev_materialIndex;
                }

                m_totalCharacterCount += 1;
            }

            if (m_isCalculatingPreferredValues)
            {
                m_isCalculatingPreferredValues = false;

                k_SetArraySizesMarker.End();
                return m_totalCharacterCount;
            }

            m_textInfo.spriteCount = spriteCount;
            int materialCount = m_textInfo.materialCount = m_materialReferenceIndexLookup.Count;

            if (materialCount > m_textInfo.meshInfo.Length)
                TMP_TextInfo.Resize(ref m_textInfo.meshInfo, materialCount, false);

            if (materialCount > m_subTextObjects.Length)
                TMP_TextInfo.Resize(ref m_subTextObjects, Mathf.NextPowerOfTwo(materialCount + 1));

            if (m_VertexBufferAutoSizeReduction && m_textInfo.characterInfo.Length - m_totalCharacterCount > 256)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, Mathf.Max(m_totalCharacterCount + 1, 256), true);


            for (int i = 0; i < materialCount; i++)
            {
                if (i > 0)
                {
                    if (m_subTextObjects[i] == null)
                    {
                        m_subTextObjects[i] = TMP_SubMeshUI.AddSubTextObject(this, m_materialReferences[i]);

                        m_textInfo.meshInfo[i].vertices = null;
                    }

                    if (m_rectTransform.pivot != m_subTextObjects[i].rectTransform.pivot)
                        m_subTextObjects[i].rectTransform.pivot = m_rectTransform.pivot;

                    if (m_subTextObjects[i].sharedMaterial == null || m_subTextObjects[i].sharedMaterial.GetInstanceID() != m_materialReferences[i].material.GetInstanceID())
                    {
                        m_subTextObjects[i].sharedMaterial = m_materialReferences[i].material;
                        m_subTextObjects[i].fontAsset = m_materialReferences[i].fontAsset;
                        m_subTextObjects[i].spriteAsset = m_materialReferences[i].spriteAsset;
                    }

                    if (m_materialReferences[i].isFallbackMaterial)
                    {
                        m_subTextObjects[i].fallbackMaterial = m_materialReferences[i].material;
                        m_subTextObjects[i].fallbackSourceMaterial = m_materialReferences[i].fallbackMaterial;
                    }
                }

                int referenceCount = m_materialReferences[i].referenceCount;

                if (m_textInfo.meshInfo[i].vertices == null || m_textInfo.meshInfo[i].vertices.Length < referenceCount * 4)
                {
                    if (m_textInfo.meshInfo[i].vertices == null)
                    {
                        if (i == 0)
                            m_textInfo.meshInfo[i] = new TMP_MeshInfo(m_mesh, referenceCount + 1);
                        else
                            m_textInfo.meshInfo[i] = new TMP_MeshInfo(m_subTextObjects[i].mesh, referenceCount + 1);
                    }
                    else
                        m_textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
                }
                else if (m_VertexBufferAutoSizeReduction && referenceCount > 0 && m_textInfo.meshInfo[i].vertices.Length / 4 - referenceCount > 256)
                {
                    m_textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount + 1));
                }

                m_textInfo.meshInfo[i].material = m_materialReferences[i].material;
            }

            for (int i = materialCount; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                if (i < m_textInfo.meshInfo.Length)
                {
                    m_subTextObjects[i].canvasRenderer.SetMesh(null);
                }
            }

            k_SetArraySizesMarker.End();
            return m_totalCharacterCount;
        }


        /// <summary>
        /// Update the margin width and height
        /// </summary>
        public override void ComputeMarginSize()
        {
            if (this.rectTransform != null)
            {
                Rect rect = m_rectTransform.rect;

                m_marginWidth = rect.width - m_margin.x - m_margin.z;
                m_marginHeight = rect.height - m_margin.y - m_margin.w;

                m_PreviousRectTransformSize = rect.size;
                m_PreviousPivotPosition = m_rectTransform.pivot;

                m_RectTransformCorners = GetTextContainerLocalCorners();
            }
        }


        /// <summary>
        ///
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
            _havePropertiesChanged = true;
            SetVerticesDirty();
            SetLayoutDirty();
        }


        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();

            m_canvas = canvas;

            if (!m_isAwake || !isActiveAndEnabled)
                return;

            if (m_canvas == null || m_canvas.enabled == false)
                TMP_UpdateManager.UnRegisterTextObjectForUpdate(this);
            else if (m_IsTextObjectScaleStatic == false)
                TMP_UpdateManager.RegisterTextObjectForUpdate(this);
        }


        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_canvas = this.canvas;

            ComputeMarginSize();
            _havePropertiesChanged = true;
        }


        protected override void OnRectTransformDimensionsChange()
        {
            if (!this.gameObject.activeInHierarchy)
                return;

            bool hasCanvasScaleFactorChanged = false;
            if (m_canvas != null && !Mathf.Approximately(m_CanvasScaleFactor, m_canvas.scaleFactor))
            {
                m_CanvasScaleFactor = m_canvas.scaleFactor;
                hasCanvasScaleFactorChanged = true;
            }

            if (hasCanvasScaleFactorChanged == false &&
                rectTransform != null &&
                Mathf.Abs(m_rectTransform.rect.width - m_PreviousRectTransformSize.x) < 0.0001f && Mathf.Abs(m_rectTransform.rect.height - m_PreviousRectTransformSize.y) < 0.0001f &&
                Mathf.Abs(m_rectTransform.pivot.x - m_PreviousPivotPosition.x) < 0.0001f && Mathf.Abs(m_rectTransform.pivot.y - m_PreviousPivotPosition.y) < 0.0001f)
            {
                return;
            }

            ComputeMarginSize();

            UpdateSubObjectPivot();

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Function used as a replacement for LateUpdate to check if the transform or scale of the text object has changed.
        /// </summary>
        internal override void InternalUpdate()
        {
            if (_havePropertiesChanged == false)
            {
                float lossyScaleY = m_rectTransform.lossyScale.y;

                if (lossyScaleY != m_previousLossyScaleY && m_TextProcessingArray[0].unicode != 0)
                {
                    float scaleDelta = lossyScaleY / m_previousLossyScaleY;

                    if (scaleDelta < 0.8f || scaleDelta > 1.25f)
                    {
                        UpdateSDFScale(scaleDelta);
                        m_previousLossyScaleY = lossyScaleY;
                    }
                }
            }

            if (m_isUsingLegacyAnimationComponent)
            {
                _havePropertiesChanged = true;
                OnPreRenderCanvas();
            }

            UpdateEnvMapMatrix();
        }


        /// <summary>
        /// Function called when the text needs to be updated.
        /// </summary>
        private void OnPreRenderCanvas()
        {
            if (!m_isAwake || (this.IsActive() == false && m_ignoreActiveState == false))
                return;

            if (m_canvas == null) { m_canvas = this.canvas; if (m_canvas == null) return; }


            if (_havePropertiesChanged || m_isLayoutDirty)
            {
                if (m_fontAsset == null)
                {
                    Debug.LogWarning("Please assign a Font Asset to this " + transform.name + " gameobject.", this);
                    return;
                }

                OnBeforeRebuild();
                if (checkPaddingRequired)
                    UpdateMeshPadding();

                needSetArraySizes = true;
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

                while (m_IsAutoSizePointSizeSet == false)
                {
                    GenerateTextMesh();
                    m_AutoSizeIterationCount += 1;
                }
                OnAfterRebuild();
            }
        }
        
        /// <summary>
        /// This is the main function that is responsible for creating / displaying the text.
        /// </summary>
        protected virtual void GenerateTextMesh()
        {
            k_GenerateTextMarker.Begin();

            if (m_fontAsset == null || m_fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " + this.GetInstanceID());
                m_IsAutoSizePointSizeSet = true;
                k_GenerateTextMarker.End();
                return;
            }

            if (m_textInfo != null)
                m_textInfo.Clear();

            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == 0)
            {
                ClearMesh();

                m_preferredWidth = 0;
                m_preferredHeight = 0;

                TMPro_EventManager.ON_TEXT_CHANGED(this);
                m_IsAutoSizePointSizeSet = true;
                k_GenerateTextMarker.End();
                return;
            }

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;
            m_materialReferenceStack.SetDefault(new MaterialReference(m_currentMaterialIndex, m_currentFontAsset, null, m_currentMaterial, m_padding));

            m_currentSpriteAsset = m_spriteAsset;

            if (m_spriteAnimator != null)
                m_spriteAnimator.StopAllAnimations();

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
            m_HighlightStateStack.SetDefault(new HighlightState(m_htmlColor, TMP_Offset.zero));

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

            m_pageNumber = 0;
            int pageToDisplay = Mathf.Clamp(m_pageToDisplay - 1, 0, m_textInfo.pageInfo.Length - 1);
            m_textInfo.ClearPageInfo();

            Vector4 margins = m_margin;
            float marginWidth = m_marginWidth > 0 ? m_marginWidth : 0;
            float marginHeight = m_marginHeight > 0 ? m_marginHeight : 0;
            m_marginLeft = 0;
            m_marginRight = 0;
            m_width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_marginLeft - m_marginRight;

            m_meshExtents.min = k_LargePositiveVector2;
            m_meshExtents.max = k_LargeNegativeVector2;

            m_textInfo.ClearLineInfo();

            m_maxCapHeight = 0;
            m_maxTextAscender = 0;
            m_ElementDescender = 0;
            m_PageAscender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;
            m_isNewPage = false;

            bool isFirstWordOfLine = true;
            m_isNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;
            int lastSoftLineBreak = 0;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
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
                if (m_isRichText && charCode == '<')
                {
                    k_ParseMarkupTextMarker.Begin();

                    m_isTextLayoutPhase = true;
                    m_textElementType = TMP_TextElementType.Character;
                    int endTagIndex;

                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex))
                    {
                        i = endTagIndex;

                        if (m_textElementType == TMP_TextElementType.Character)
                        {
                            k_ParseMarkupTextMarker.End();
                            continue;
                        }
                    }
                    k_ParseMarkupTextMarker.End();
                }
                else
                {
                    m_textElementType = m_textInfo.characterInfo[m_characterCount].elementType;
                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;
                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;
                }
                #endregion End Parse Rich Text Tag

                int previousMaterialIndex = m_currentMaterialIndex;
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
                            m_textInfo.characterInfo[m_characterCount].textElement = m_currentFontAsset.characterLookupTable[0x03];
                            m_isTextTruncated = true;
                            break;
                        case 0x2D:
                            break;
                        case 0x2026:
                            m_textInfo.characterInfo[m_characterCount].textElement = m_Ellipsis.character;
                            m_textInfo.characterInfo[m_characterCount].elementType = TMP_TextElementType.Character;
                            m_textInfo.characterInfo[m_characterCount].fontAsset = m_Ellipsis.fontAsset;
                            m_textInfo.characterInfo[m_characterCount].material = m_Ellipsis.material;
                            m_textInfo.characterInfo[m_characterCount].materialReferenceIndex = m_Ellipsis.materialIndex;

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
                    m_textInfo.characterInfo[m_characterCount].isVisible = false;
                    m_textInfo.characterInfo[m_characterCount].character = (char)0x200B;
                    m_textInfo.characterInfo[m_characterCount].lineNumber = 0;
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
                k_CharacterLookupMarker.Begin();

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                if (m_textElementType == TMP_TextElementType.Sprite)
                {
                    TMP_SpriteCharacter sprite = (TMP_SpriteCharacter)textInfo.characterInfo[m_characterCount].textElement;
                    m_currentSpriteAsset = sprite.textAsset as TMP_SpriteAsset;
                    m_spriteIndex = (int)sprite.glyphIndex;

                    if (sprite == null)
                    {
                        k_CharacterLookupMarker.End();
                        continue;
                    }

                    if (charCode == '<')
                        charCode = 57344 + (uint)m_spriteIndex;
                    else
                        m_spriteColor = s_colorWhite;

                    float fontScale = (m_currentFontSize / m_currentFontAsset.faceInfo.pointSize * m_currentFontAsset.faceInfo.scale * (m_isOrthographic ? 1 : 0.1f));

                    if (m_currentSpriteAsset.m_FaceInfo.pointSize > 0)
                    {
                        float spriteScale = m_currentFontSize / m_currentSpriteAsset.m_FaceInfo.pointSize * m_currentSpriteAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                        currentElementScale = sprite.m_Scale * sprite.m_Glyph.scale * spriteScale;
                        elementAscentLine = m_currentSpriteAsset.m_FaceInfo.ascentLine;
                        baselineOffset = m_currentSpriteAsset.m_FaceInfo.baseline * fontScale * m_fontScaleMultiplier * m_currentSpriteAsset.m_FaceInfo.scale;
                        elementDescentLine = m_currentSpriteAsset.m_FaceInfo.descentLine;
                    }
                    else
                    {
                        float spriteScale = m_currentFontSize / m_currentFontAsset.m_FaceInfo.pointSize * m_currentFontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                        currentElementScale = m_currentFontAsset.m_FaceInfo.ascentLine / sprite.m_Glyph.metrics.height * sprite.m_Scale * sprite.m_Glyph.scale * spriteScale;
                        float scaleDelta = spriteScale / currentElementScale;
                        elementAscentLine = m_currentFontAsset.m_FaceInfo.ascentLine * scaleDelta;
                        baselineOffset = m_currentFontAsset.m_FaceInfo.baseline * fontScale * m_fontScaleMultiplier * m_currentFontAsset.m_FaceInfo.scale;
                        elementDescentLine = m_currentFontAsset.m_FaceInfo.descentLine * scaleDelta;
                    }

                    m_cached_TextElement = sprite;

                    m_textInfo.characterInfo[m_characterCount].elementType = TMP_TextElementType.Sprite;
                    m_textInfo.characterInfo[m_characterCount].scale = currentElementScale;
                    m_textInfo.characterInfo[m_characterCount].fontAsset = m_currentFontAsset;
                    m_textInfo.characterInfo[m_characterCount].materialReferenceIndex = m_currentMaterialIndex;

                    m_currentMaterialIndex = previousMaterialIndex;

                    padding = 0;
                }
                else if (m_textElementType == TMP_TextElementType.Character)
                {
                    m_cached_TextElement = m_textInfo.characterInfo[m_characterCount].textElement;
                    if (m_cached_TextElement == null)
                    {
                        k_CharacterLookupMarker.End();
                        continue;
                    }

                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;
                    m_currentMaterial = m_textInfo.characterInfo[m_characterCount].material;
                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;

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

                    m_textInfo.characterInfo[m_characterCount].elementType = TMP_TextElementType.Character;
                    m_textInfo.characterInfo[m_characterCount].scale = currentElementScale;

                    padding = m_currentMaterialIndex == 0 ? m_padding : m_subTextObjects[m_currentMaterialIndex].padding;
                }
                k_CharacterLookupMarker.End();
                #endregion


                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == 0xAD || charCode == 0x03)
                    currentElementScale = 0;
                #endregion


                m_textInfo.characterInfo[m_characterCount].character = (char)charCode;
                m_textInfo.characterInfo[m_characterCount].pointSize = m_currentFontSize;
                m_textInfo.characterInfo[m_characterCount].color = m_htmlColor;
                m_textInfo.characterInfo[m_characterCount].underlineColor = m_underlineColor;
                m_textInfo.characterInfo[m_characterCount].strikethroughColor = m_strikethroughColor;
                m_textInfo.characterInfo[m_characterCount].highlightState = m_HighlightState;
                m_textInfo.characterInfo[m_characterCount].style = m_FontStyleInternal;

                Glyph altGlyph = m_textInfo.characterInfo[m_characterCount].alternativeGlyph;
                GlyphMetrics currentGlyphMetrics = altGlyph == null ? m_cached_TextElement.m_Glyph.metrics : altGlyph.metrics;

                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                #region Handle Kerning
                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                float characterSpacingAdjustment = m_characterSpacing;
                if (kerning && m_textElementType == TMP_TextElementType.Character)
                {
                    k_HandleGPOSFeaturesMarker.Begin();

                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_cached_TextElement.m_GlyphIndex;

                    if (m_characterCount < totalCharacterCount - 1 && textInfo.characterInfo[m_characterCount + 1].elementType == TMP_TextElementType.Character)
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

                        if (textInfo.characterInfo[m_characterCount - 1].elementType == TMP_TextElementType.Character && m_currentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments) == UnityEngine.TextCore.LowLevel.FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    k_HandleGPOSFeaturesMarker.End();
                }

                m_textInfo.characterInfo[m_characterCount].adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
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


                #region Handle Right-to-Left
                if (m_isRightToLeft)
                {
                    m_xAdvance -= currentGlyphMetrics.horizontalAdvance * (1 - m_charWidthAdjDelta) * currentElementScale;

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance -= m_wordSpacing * currentEmScale;
                }
                #endregion


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
                if (m_textElementType == TMP_TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold))
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
                if (m_textElementType == TMP_TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic))
                {
                    float shear_value = m_ItalicAngle * 0.01f;
                    float midPoint = ((m_currentFontAsset.m_FaceInfo.capLine - (m_currentFontAsset.m_FaceInfo.baseline + m_baselineOffset)) / 2) * m_fontScaleMultiplier * m_currentFontAsset.m_FaceInfo.scale;
                    Vector3 topShear = new Vector3(shear_value * ((currentGlyphMetrics.horizontalBearingY + padding + style_padding - midPoint) * currentElementScale), 0, 0);
                    Vector3 bottomShear = new Vector3(shear_value * (((currentGlyphMetrics.horizontalBearingY - currentGlyphMetrics.height - padding - style_padding - midPoint)) * currentElementScale), 0, 0);

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


                m_textInfo.characterInfo[m_characterCount].bottomLeft = bottom_left;
                m_textInfo.characterInfo[m_characterCount].topLeft = top_left;
                m_textInfo.characterInfo[m_characterCount].topRight = top_right;
                m_textInfo.characterInfo[m_characterCount].bottomRight = bottom_right;

                m_textInfo.characterInfo[m_characterCount].origin = m_xAdvance + glyphAdjustments.xPlacement * currentElementScale;
                m_textInfo.characterInfo[m_characterCount].baseLine = (baselineOffset - m_lineOffset + m_baselineOffset) + glyphAdjustments.yPlacement * currentElementScale;
                m_textInfo.characterInfo[m_characterCount].aspectRatio = (top_right.x - bottom_left.x) / (top_left.y - bottom_left.y);


                #region Compute Ascender & Descender values
                k_ComputeTextMetricsMarker.Begin();
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
                        adjustedAscender = Mathf.Max((elementAscender - m_baselineOffset) / m_fontScaleMultiplier, adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_baselineOffset) / m_fontScaleMultiplier, adjustedDescender);
                    }

                    m_maxLineAscender = Mathf.Max(adjustedAscender, m_maxLineAscender);
                    m_maxLineDescender = Mathf.Min(adjustedDescender, m_maxLineDescender);
                }

                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    m_textInfo.characterInfo[m_characterCount].adjustedAscender = adjustedAscender;
                    m_textInfo.characterInfo[m_characterCount].adjustedDescender = adjustedDescender;

                    m_ElementAscender = m_textInfo.characterInfo[m_characterCount].ascender = elementAscender - m_lineOffset;
                    m_ElementDescender = m_textInfo.characterInfo[m_characterCount].descender = elementDescender - m_lineOffset;
                }
                else
                {
                    m_textInfo.characterInfo[m_characterCount].adjustedAscender = m_maxLineAscender;
                    m_textInfo.characterInfo[m_characterCount].adjustedDescender = m_maxLineDescender;

                    m_ElementAscender = m_textInfo.characterInfo[m_characterCount].ascender = m_maxLineAscender - m_lineOffset;
                    m_ElementDescender = m_textInfo.characterInfo[m_characterCount].descender = m_maxLineDescender - m_lineOffset;
                }

                if (m_lineNumber == 0 || m_isNewPage)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_maxTextAscender = m_maxLineAscender;
                        m_maxCapHeight = Mathf.Max(m_maxCapHeight, m_currentFontAsset.m_FaceInfo.capLine * currentElementScale / smallCapsMultiplier);
                    }
                }

                if (m_lineOffset == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                k_ComputeTextMetricsMarker.End();
                #endregion


                m_textInfo.characterInfo[m_characterCount].isVisible = false;

                bool isJustifiedOrFlush = (m_lineJustification & HorizontalAlignmentOptions.Flush) == HorizontalAlignmentOptions.Flush || (m_lineJustification & HorizontalAlignmentOptions.Justified) == HorizontalAlignmentOptions.Justified;

                #region Handle Visible Characters
                if (charCode == 9 || ((m_TextWrappingMode == TextWrappingModes.PreserveWhitespace || m_TextWrappingMode == TextWrappingModes.PreserveWhitespaceNoWrap) && (isWhiteSpace || charCode == 0x200B)) || (isWhiteSpace == false && charCode != 0x200B && charCode != 0xAD && charCode != 0x03) || (charCode == 0xAD && isSoftHyphenIgnored == false) || m_textElementType == TMP_TextElementType.Sprite)
                {
                    k_HandleVisibleCharacterMarker.Begin();

                    m_textInfo.characterInfo[m_characterCount].isVisible = true;

                    float marginLeft = m_marginLeft;
                    float marginRight = m_marginRight;

                    if (isInjectedCharacter)
                    {
                        marginLeft = m_textInfo.lineInfo[m_lineNumber].marginLeft;
                        marginRight = m_textInfo.lineInfo[m_lineNumber].marginRight;
                    }

                    widthOfTextArea = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    float textWidth = Mathf.Abs(m_xAdvance) + (!m_isRightToLeft ? currentGlyphMetrics.horizontalAdvance : 0) * (1 - m_charWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);
                    float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0);

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
                            case TextOverflowModes.ScrollRect:
                            case TextOverflowModes.Masking:
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

                            case TextOverflowModes.Linked:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState);

                                if (m_linkedTextComponent != null)
                                {
                                    m_linkedTextComponent.text = text;
                                    m_linkedTextComponent.firstVisibleCharacter = m_characterCount;
                                    m_linkedTextComponent.ForceMeshUpdate();

                                    m_isTextTruncated = true;
                                }

                                characterToSubstitute.index = testedCharacterCount;
                                characterToSubstitute.unicode = 0x03;
                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;

                            case TextOverflowModes.Page:
                                if (i < 0 || testedCharacterCount == 0)
                                {
                                    i = -1;
                                    m_characterCount = 0;
                                    characterToSubstitute.index = 0;
                                    characterToSubstitute.unicode = 0x03;
                                    k_HandleVerticalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                                }
                                else if (m_maxLineAscender - m_maxLineDescender > marginHeight + 0.0001f)
                                {
                                    i = RestoreWordWrappingState(ref m_SavedLineState);

                                    characterToSubstitute.index = testedCharacterCount;
                                    characterToSubstitute.unicode = 0x03;
                                    k_HandleVerticalLineBreakingMarker.End();
                                    k_HandleVisibleCharacterMarker.End();
                                    continue;
                                }

                                i = RestoreWordWrappingState(ref m_SavedLineState);

                                m_isNewPage = true;
                                m_firstCharacterOfLine = m_characterCount;
                                m_maxLineAscender = k_LargeNegativeFloat;
                                m_maxLineDescender = k_LargePositiveFloat;
                                m_startOfLineAscender = 0;

                                m_xAdvance = 0 + tag_Indent;
                                m_lineOffset = 0;
                                m_maxTextAscender = 0;
                                m_PageAscender = 0;
                                m_lineNumber += 1;
                                m_pageNumber += 1;

                                k_HandleVerticalLineBreakingMarker.End();
                                k_HandleVisibleCharacterMarker.End();
                                continue;
                        }

                        k_HandleVerticalLineBreakingMarker.End();
                    }
                    #endregion


                    #region Current Line Horizontal Bounds Check
                    if (isBaseGlyph && textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        k_HandleHorizontalLineBreakingMarker.Begin();

                        if (m_TextWrappingMode != TextWrappingModes.NoWrap && m_TextWrappingMode != TextWrappingModes.PreserveWhitespaceNoWrap && m_characterCount != m_firstCharacterOfLine)
                        {
                            i = RestoreWordWrappingState(ref m_SavedWordWrapState);

                            float lineOffsetDelta = 0;
                            if (m_lineHeight == TMP_Math.FLOAT_UNSET)
                            {
                                float ascender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;
                                lineOffsetDelta = (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0) - m_maxLineDescender + ascender + (lineGap + m_lineSpacingDelta) * baseScale + m_lineSpacing * currentEmScale;
                            }
                            else
                            {
                                lineOffsetDelta = m_lineHeight + m_lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            float newTextHeight = m_maxTextAscender + lineOffsetDelta + m_lineOffset - m_textInfo.characterInfo[m_characterCount].adjustedDescender;

                            #region Handle Soft Hyphenation
                            if (m_textInfo.characterInfo[m_characterCount - 1].character == 0xAD && isSoftHyphenIgnored == false)
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

                            if (m_textInfo.characterInfo[m_characterCount].character == 0xAD)
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
                                    case TextOverflowModes.ScrollRect:
                                    case TextOverflowModes.Masking:
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

                                    case TextOverflowModes.Linked:
                                        if (m_linkedTextComponent != null)
                                        {
                                            m_linkedTextComponent.text = text;
                                            m_linkedTextComponent.firstVisibleCharacter = m_characterCount;
                                            m_linkedTextComponent.ForceMeshUpdate();

                                            m_isTextTruncated = true;
                                        }

                                        characterToSubstitute.index = m_characterCount;
                                        characterToSubstitute.unicode = 0x03;
                                        k_HandleVerticalLineBreakingMarker.End();
                                        k_HandleHorizontalLineBreakingMarker.End();
                                        k_HandleVisibleCharacterMarker.End();
                                        continue;

                                    case TextOverflowModes.Page:
                                        m_isNewPage = true;

                                        InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender);

                                        m_startOfLineAscender = 0;
                                        m_lineOffset = 0;
                                        m_maxTextAscender = 0;
                                        m_PageAscender = 0;
                                        m_pageNumber += 1;

                                        isStartOfNewLine = true;
                                        isFirstWordOfLine = true;
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
                                case TextOverflowModes.ScrollRect:
                                case TextOverflowModes.Masking:
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

                                case TextOverflowModes.Linked:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState);

                                    if (m_linkedTextComponent != null)
                                    {
                                        m_linkedTextComponent.text = text;
                                        m_linkedTextComponent.firstVisibleCharacter = m_characterCount;
                                        m_linkedTextComponent.ForceMeshUpdate();

                                        m_isTextTruncated = true;
                                    }

                                    characterToSubstitute.index = m_characterCount;
                                    characterToSubstitute.unicode = 0x03;
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
                        m_textInfo.characterInfo[m_characterCount].isVisible = false;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                        m_lineVisibleSpaceCount = m_textInfo.lineInfo[m_lineNumber].spaceCount += 1;
                        m_textInfo.lineInfo[m_lineNumber].marginLeft = marginLeft;
                        m_textInfo.lineInfo[m_lineNumber].marginRight = marginRight;
                        m_textInfo.spaceCount += 1;

                        if (charCode == 0xA0)
                            m_textInfo.lineInfo[m_lineNumber].controlCharacterCount += 1;
                    }
                    else if (charCode == 0xAD)
                    {
                        m_textInfo.characterInfo[m_characterCount].isVisible = false;
                    }
                    else
                    {
                        Color32 vertexColor;
                        if (m_overrideHtmlColors)
                            vertexColor = m_fontColor32;
                        else
                            vertexColor = m_htmlColor;

                        k_SaveGlyphVertexDataMarker.Begin();
                        if (m_textElementType == TMP_TextElementType.Character)
                        {
                            SaveGlyphVertexInfo(padding, style_padding, vertexColor);
                        }
                        else if (m_textElementType == TMP_TextElementType.Sprite)
                        {
                            SaveSpriteVertexInfo(vertexColor);
                        }
                        k_SaveGlyphVertexDataMarker.End();

                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_firstVisibleCharacterOfLine = m_characterCount;
                        }

                        m_lineVisibleCharacterCount += 1;
                        m_lastVisibleCharacterOfLine = m_characterCount;
                        m_textInfo.lineInfo[m_lineNumber].marginLeft = marginLeft;
                        m_textInfo.lineInfo[m_lineNumber].marginRight = marginRight;
                    }

                    k_HandleVisibleCharacterMarker.End();
                }
                else
                {
                    k_HandleWhiteSpacesMarker.Begin();

                    #region Check Vertical Bounds
                    if (m_overflowMode == TextOverflowModes.Linked && (charCode == 10 || charCode == 11))
                    {
                        float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0);

                        int testedCharacterCount = m_characterCount;

                        if (textHeight > marginHeight + 0.0001f)
                        {
                            if (m_firstOverflowCharacterIndex == -1)
                                m_firstOverflowCharacterIndex = m_characterCount;

                            i = RestoreWordWrappingState(ref m_SavedLastValidState);

                            if (m_linkedTextComponent != null)
                            {
                                m_linkedTextComponent.text = text;
                                m_linkedTextComponent.firstVisibleCharacter = m_characterCount;
                                m_linkedTextComponent.ForceMeshUpdate();

                                m_isTextTruncated = true;
                            }

                            characterToSubstitute.index = testedCharacterCount;
                            characterToSubstitute.unicode = 0x03;
                            k_HandleWhiteSpacesMarker.End();
                            continue;
                        }
                    }
                    #endregion

                    if ((charCode == 10 || charCode == 11 || charCode == 0xA0 || charCode == 0x2007 || charCode == 0x2028 || charCode == 0x2029 || char.IsSeparator((char)charCode)) && charCode != 0xAD && charCode != 0x200B && charCode != 0x2060)
                    {
                        m_textInfo.lineInfo[m_lineNumber].spaceCount += 1;
                        m_textInfo.spaceCount += 1;
                    }

                    if (charCode == 0xA0)
                        m_textInfo.lineInfo[m_lineNumber].controlCharacterCount += 1;

                    k_HandleWhiteSpacesMarker.End();
                }
                #endregion Handle Visible Characters


                #region Track Potential Insertion Location for Ellipsis
                if (m_overflowMode == TextOverflowModes.Ellipsis && (isInjectedCharacter == false || charCode == 0x2D))
                {
                    float fontScale = m_currentFontSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                    float scale = fontScale * m_fontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                    float marginLeft = m_marginLeft;
                    float marginRight = m_marginRight;

                    if (charCode == 0x0A && m_characterCount != m_firstCharacterOfLine)
                    {
                        fontScale = m_textInfo.characterInfo[m_characterCount - 1].pointSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale * (m_isOrthographic ? 1 : 0.1f);
                        scale = fontScale * m_fontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                        marginLeft = m_textInfo.lineInfo[m_lineNumber].marginLeft;
                        marginRight = m_textInfo.lineInfo[m_lineNumber].marginRight;
                    }

                    float textHeight = m_maxTextAscender - (m_maxLineDescender - m_lineOffset) + (m_lineOffset > 0 && m_IsDrivenLineSpacing == false ? m_maxLineAscender - m_startOfLineAscender : 0);
                    float textWidth = Mathf.Abs(m_xAdvance) + (!m_isRightToLeft ? m_Ellipsis.character.m_Glyph.metrics.horizontalAdvance : 0) * (1 - m_charWidthAdjDelta) * scale;
                    float widthOfTextAreaForEllipsis = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    if (textWidth < widthOfTextAreaForEllipsis * (isJustifiedOrFlush ? 1.05f : 1.0f) && textHeight < marginHeight + 0.0001f)
                    {
                        SaveWordWrappingState(ref m_SavedEllipsisState, i, m_characterCount);
                        m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                    }
                }
                #endregion


                #region Store Character Data
                m_textInfo.characterInfo[m_characterCount].lineNumber = m_lineNumber;
                m_textInfo.characterInfo[m_characterCount].pageNumber = m_pageNumber;

                if (charCode != 10 && charCode != 11 && charCode != 13 && isInjectedCharacter == false || m_textInfo.lineInfo[m_lineNumber].characterCount == 1)
                    m_textInfo.lineInfo[m_lineNumber].alignment = m_lineJustification;
                #endregion Store Character Data


                #region XAdvance, Tabulation & Stops
                k_ComputeCharacterAdvanceMarker.Begin();
                if (charCode == 9)
                {
                    float tabSize = m_currentFontAsset.m_FaceInfo.tabWidth * m_currentFontAsset.tabSize * currentElementScale;
                    if (m_isRightToLeft)
                    {
                        float tabs = Mathf.Floor(m_xAdvance / tabSize) * tabSize;
                        m_xAdvance = tabs < m_xAdvance ? tabs : m_xAdvance - tabSize;
                    }
                    else
                    {
                        float tabs = Mathf.Ceil(m_xAdvance / tabSize) * tabSize;
                        m_xAdvance = tabs > m_xAdvance ? tabs : m_xAdvance + tabSize;
                    }
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
                else if (m_isRightToLeft)
                {
                    m_xAdvance -= ((glyphAdjustments.xAdvance * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta));

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance -= m_wordSpacing * currentEmScale;
                }
                else
                {
                    m_xAdvance += ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) * currentElementScale + (m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);

                    if (isWhiteSpace || charCode == 0x200B)
                        m_xAdvance += m_wordSpacing * currentEmScale;
                }

                m_textInfo.characterInfo[m_characterCount].xAdvance = m_xAdvance;
                k_ComputeCharacterAdvanceMarker.End();
                #endregion Tabulation & Stops


                #region Carriage Return
                if (charCode == 13)
                {
                    k_HandleCarriageReturnMarker.Begin();
                    m_xAdvance = 0 + tag_Indent;
                    k_HandleCarriageReturnMarker.End();
                }
                #endregion Carriage Return


                #region Save PageInfo
                k_SavePageInfoMarker.Begin();
                if (m_overflowMode == TextOverflowModes.Page && charCode != 10 && charCode != 11 && charCode != 13 && charCode != 0x2028 && charCode != 0x2029)
                {
                    if (m_pageNumber + 1 > m_textInfo.pageInfo.Length)
                        TMP_TextInfo.Resize(ref m_textInfo.pageInfo, m_pageNumber + 1, true);

                    m_textInfo.pageInfo[m_pageNumber].ascender = m_PageAscender;
                    m_textInfo.pageInfo[m_pageNumber].descender = m_ElementDescender < m_textInfo.pageInfo[m_pageNumber].descender
                        ? m_ElementDescender
                        : m_textInfo.pageInfo[m_pageNumber].descender;

                    if (m_isNewPage)
                    {
                        m_isNewPage = false;
                        m_textInfo.pageInfo[m_pageNumber].firstCharacterIndex = m_characterCount;
                    }

                    m_textInfo.pageInfo[m_pageNumber].lastCharacterIndex = m_characterCount;
                }
                k_SavePageInfoMarker.End();
                #endregion Save PageInfo


                #region Check for Line Feed and Last Character
                if (charCode == 10 || charCode == 11 || charCode == 0x03 || charCode == 0x2028 || charCode == 0x2029 || (charCode == 0x2D && isInjectedCharacter) || m_characterCount == totalCharacterCount - 1)
                {
                    k_HandleLineTerminationMarker.Begin();

                    float baselineAdjustmentDelta = m_maxLineAscender - m_startOfLineAscender;
                    if (m_lineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_isNewPage)
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
                    m_isNewPage = false;

                    float lineAscender = m_maxLineAscender - m_lineOffset;
                    float lineDescender = m_maxLineDescender - m_lineOffset;

                    m_ElementDescender = m_ElementDescender < lineDescender ? m_ElementDescender : lineDescender;
                    if (!isMaxVisibleDescenderSet)
                        maxVisibleDescender = m_ElementDescender;

                    if (m_useMaxVisibleDescender && (m_characterCount >= m_maxVisibleCharacters || m_lineNumber >= m_maxVisibleLines))
                        isMaxVisibleDescenderSet = true;

                    m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex = m_firstCharacterOfLine;
                    m_textInfo.lineInfo[m_lineNumber].firstVisibleCharacterIndex = m_firstVisibleCharacterOfLine = m_firstCharacterOfLine > m_firstVisibleCharacterOfLine ? m_firstCharacterOfLine : m_firstVisibleCharacterOfLine;
                    m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex = m_lastCharacterOfLine = m_characterCount;
                    m_textInfo.lineInfo[m_lineNumber].lastVisibleCharacterIndex = m_lastVisibleCharacterOfLine = m_lastVisibleCharacterOfLine < m_firstVisibleCharacterOfLine ? m_firstVisibleCharacterOfLine : m_lastVisibleCharacterOfLine;

                    m_textInfo.lineInfo[m_lineNumber].characterCount = m_textInfo.lineInfo[m_lineNumber].lastCharacterIndex - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex + 1;
                    m_textInfo.lineInfo[m_lineNumber].visibleCharacterCount = m_lineVisibleCharacterCount;
                    m_textInfo.lineInfo[m_lineNumber].visibleSpaceCount = (m_textInfo.lineInfo[m_lineNumber].lastVisibleCharacterIndex + 1 - m_textInfo.lineInfo[m_lineNumber].firstCharacterIndex) - m_lineVisibleCharacterCount;
                    m_textInfo.lineInfo[m_lineNumber].lineExtents.min = new Vector2(m_textInfo.characterInfo[m_firstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
                    m_textInfo.lineInfo[m_lineNumber].lineExtents.max = new Vector2(m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].topRight.x, lineAscender);
                    m_textInfo.lineInfo[m_lineNumber].length = m_textInfo.lineInfo[m_lineNumber].lineExtents.max.x - (padding * currentElementScale);
                    m_textInfo.lineInfo[m_lineNumber].width = widthOfTextArea;

                    if (m_textInfo.lineInfo[m_lineNumber].characterCount == 1)
                        m_textInfo.lineInfo[m_lineNumber].alignment = m_lineJustification;

                    float maxAdvanceOffset = ((m_currentFontAsset.normalSpacingOffset + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_cSpacing) * (1 - m_charWidthAdjDelta);
                    if (m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].isVisible)
                        m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastVisibleCharacterOfLine].xAdvance + (m_isRightToLeft ? maxAdvanceOffset : - maxAdvanceOffset);
                    else
                        m_textInfo.lineInfo[m_lineNumber].maxAdvance = m_textInfo.characterInfo[m_lastCharacterOfLine].xAdvance + (m_isRightToLeft ? maxAdvanceOffset : - maxAdvanceOffset);

                    m_textInfo.lineInfo[m_lineNumber].baseline = 0 - m_lineOffset;
                    m_textInfo.lineInfo[m_lineNumber].ascender = lineAscender;
                    m_textInfo.lineInfo[m_lineNumber].descender = lineDescender;
                    m_textInfo.lineInfo[m_lineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

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

                        float lastVisibleAscender = m_textInfo.characterInfo[m_characterCount].adjustedAscender;

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
                if (m_textInfo.characterInfo[m_characterCount].isVisible)
                {
                    m_meshExtents.min.x = Mathf.Min(m_meshExtents.min.x, m_textInfo.characterInfo[m_characterCount].bottomLeft.x);
                    m_meshExtents.min.y = Mathf.Min(m_meshExtents.min.y, m_textInfo.characterInfo[m_characterCount].bottomLeft.y);

                    m_meshExtents.max.x = Mathf.Max(m_meshExtents.max.x, m_textInfo.characterInfo[m_characterCount].topRight.x);
                    m_meshExtents.max.y = Mathf.Max(m_meshExtents.max.y, m_textInfo.characterInfo[m_characterCount].topRight.y);
                }
                k_SaveTextExtentMarker.End();
                #endregion Track Text Extents


                #region Save Word Wrapping State
                if ((m_TextWrappingMode != TextWrappingModes.NoWrap && m_TextWrappingMode != TextWrappingModes.PreserveWhitespaceNoWrap) || m_overflowMode == TextOverflowModes.Truncate || m_overflowMode == TextOverflowModes.Ellipsis || m_overflowMode == TextOverflowModes.Linked)
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
                    else if (m_isNonBreakingSpace == false && (TMP_TextParsingUtilities.IsHangul(charCode) && TMP_Settings.useModernHangulLineBreakingRules == false || TMP_TextParsingUtilities.IsCJK(charCode)))
                    {
                        bool isCurrentLeadingCharacter = TMP_Settings.linebreakingRules.leadingCharacters.Contains(charCode);
                        bool isNextFollowingCharacter = m_characterCount < totalCharacterCount - 1 && TMP_Settings.linebreakingRules.followingCharacters.Contains(m_textInfo.characterInfo[m_characterCount + 1].character);

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
                    else if (m_isNonBreakingSpace == false && m_characterCount + 1 < totalCharacterCount && TMP_TextParsingUtilities.IsCJK(m_textInfo.characterInfo[m_characterCount + 1].character))
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
                    if (m_overflowMode != TextOverflowModes.Page)
                        anchorOffset = corners[1] + new Vector3(0 + margins.x, 0 - m_maxTextAscender - margins.y, 0);
                    else
                        anchorOffset = corners[1] + new Vector3(0 + margins.x, 0 - m_textInfo.pageInfo[pageToDisplay].ascender - margins.y, 0);
                    break;

                case VerticalAlignmentOptions.Middle:
                    if (m_overflowMode != TextOverflowModes.Page)
                        anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_maxTextAscender + margins.y + maxVisibleDescender - margins.w) / 2, 0);
                    else
                        anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_textInfo.pageInfo[pageToDisplay].ascender + margins.y + m_textInfo.pageInfo[pageToDisplay].descender - margins.w) / 2, 0);
                    break;

                case VerticalAlignmentOptions.Bottom:
                    if (m_overflowMode != TextOverflowModes.Page)
                        anchorOffset = corners[0] + new Vector3(0 + margins.x, 0 - maxVisibleDescender + margins.w, 0);
                    else
                        anchorOffset = corners[0] + new Vector3(0 + margins.x, 0 - m_textInfo.pageInfo[pageToDisplay].descender + margins.w, 0);
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
            float lossyScale = m_previousLossyScaleY = this.transform.lossyScale.y;
            RenderMode canvasRenderMode = m_canvas.renderMode;
            float canvasScaleFactor = m_canvas.scaleFactor;

            Color32 underlineColor = Color.white;
            Color32 strikethroughColor = Color.white;
            HighlightState highlightState = new HighlightState(new Color32(255, 255, 0, 64), TMP_Offset.zero);
            float xScale = 0;
            float xScaleMax = 0;
            float underlineStartScale = 0;
            float underlineEndScale = 0;
            float underlineMaxScale = 0;
            float underlineBaseLine = k_LargePositiveFloat;
            int lastPage = 0;

            float strikethroughPointSize = 0;
            float strikethroughScale = 0;
            float strikethroughBaseline = 0;

            TMP_CharacterInfo[] characterInfos = m_textInfo.characterInfo;
            #region Handle Line Justification & UV Mapping & Character Visibility & More
            for (int i = 0; i < m_characterCount; i++)
            {
                TMP_FontAsset currentFontAsset = characterInfos[i].fontAsset;

                char unicode = characterInfos[i].character;
                bool isWhiteSpace = char.IsWhiteSpace(unicode);

                int currentLine = characterInfos[i].lineNumber;
                TMP_LineInfo lineInfo = m_textInfo.lineInfo[currentLine];
                lineCount = currentLine + 1;

                HorizontalAlignmentOptions lineAlignment = lineInfo.alignment;

                #region Handle Line Justification
                switch (lineAlignment)
                {
                    case HorizontalAlignmentOptions.Left:
                        if (!m_isRightToLeft)
                            justificationOffset = new Vector3(0 + lineInfo.marginLeft, 0, 0);
                        else
                            justificationOffset = new Vector3(0 - lineInfo.maxAdvance, 0, 0);
                        break;

                    case HorizontalAlignmentOptions.Center:
                        justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width / 2 - lineInfo.maxAdvance / 2, 0, 0);
                        break;

                    case HorizontalAlignmentOptions.Geometry:
                        justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width / 2 - (lineInfo.lineExtents.min.x + lineInfo.lineExtents.max.x) / 2, 0, 0);
                        break;

                    case HorizontalAlignmentOptions.Right:
                        if (!m_isRightToLeft)
                            justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width - lineInfo.maxAdvance, 0, 0);
                        else
                            justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);
                        break;

                    case HorizontalAlignmentOptions.Justified:
                    case HorizontalAlignmentOptions.Flush:
                        if (i > lineInfo.lastVisibleCharacterIndex || unicode == 0x0A || unicode == 0xAD || unicode == 0x200B || unicode == 0x2060 || unicode == 0x03) break;

                        char lastCharOfCurrentLine = characterInfos[lineInfo.lastCharacterIndex].character;

                        bool isFlush = (lineAlignment & HorizontalAlignmentOptions.Flush) == HorizontalAlignmentOptions.Flush;

                        if (char.IsControl(lastCharOfCurrentLine) == false && currentLine < m_lineNumber || isFlush || lineInfo.maxAdvance > lineInfo.width)
                        {
                            if (currentLine != lastLine || i == 0 || i == m_firstVisibleCharacter)
                            {
                                if (!m_isRightToLeft)
                                    justificationOffset = new Vector3(lineInfo.marginLeft, 0, 0);
                                else
                                    justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);

                                if (char.IsSeparator(unicode))
                                    isFirstSeperator = true;
                                else
                                    isFirstSeperator = false;
                            }
                            else
                            {
                                float gap = !m_isRightToLeft ? lineInfo.width - lineInfo.maxAdvance : lineInfo.width + lineInfo.maxAdvance;
                                int visibleCount = lineInfo.visibleCharacterCount - 1 + lineInfo.controlCharacterCount;
                                int spaces = lineInfo.visibleSpaceCount - lineInfo.controlCharacterCount;

                                if (isFirstSeperator) { spaces -= 1; visibleCount += 1; }

                                float ratio = spaces > 0 ? m_wordWrappingRatios : 1;

                                if (spaces < 1) spaces = 1;

                                if (unicode != 0xA0 && (unicode == 9 || char.IsSeparator(unicode)))
                                {
                                    if (!m_isRightToLeft)
                                        justificationOffset += new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                    else
                                        justificationOffset -= new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                }
                                else
                                {
                                    if (!m_isRightToLeft)
                                        justificationOffset += new Vector3(gap * ratio / visibleCount, 0, 0);
                                    else
                                        justificationOffset -= new Vector3(gap * ratio / visibleCount, 0, 0);
                                }
                            }
                        }
                        else
                        {
                            if (!m_isRightToLeft)
                                justificationOffset = new Vector3(lineInfo.marginLeft, 0, 0);
                            else
                                justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);
                        }

                        break;
                }
                #endregion End Text Justification

                offset = anchorOffset + justificationOffset;

                #region Handling of UV2 mapping & Scale packing
                bool isCharacterVisible = characterInfos[i].isVisible;
                if (isCharacterVisible)
                {
                    TMP_TextElementType elementType = characterInfos[i].elementType;
                    switch (elementType)
                    {
                        case TMP_TextElementType.Character:
                            Extents lineExtents = lineInfo.lineExtents;
                            float uvOffset = (m_uvLineOffset * currentLine) % 1;

                            #region Handle UV Mapping Options
                            switch (m_horizontalMapping)
                            {
                                case TextureMappingOptions.Character:
                                    characterInfos[i].vertex_BL.uv2.x = 0;
                                    characterInfos[i].vertex_TL.uv2.x = 0;
                                    characterInfos[i].vertex_TR.uv2.x = 1;
                                    characterInfos[i].vertex_BR.uv2.x = 1;
                                    break;

                                case TextureMappingOptions.Line:
                                    if (m_textAlignment != TextAlignmentOptions.Justified)
                                    {
                                        characterInfos[i].vertex_BL.uv2.x = (characterInfos[i].vertex_BL.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        characterInfos[i].vertex_TL.uv2.x = (characterInfos[i].vertex_TL.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        characterInfos[i].vertex_TR.uv2.x = (characterInfos[i].vertex_TR.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        characterInfos[i].vertex_BR.uv2.x = (characterInfos[i].vertex_BR.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        break;
                                    }
                                    else
                                    {
                                        characterInfos[i].vertex_BL.uv2.x = (characterInfos[i].vertex_BL.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                        characterInfos[i].vertex_TL.uv2.x = (characterInfos[i].vertex_TL.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                        characterInfos[i].vertex_TR.uv2.x = (characterInfos[i].vertex_TR.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                        characterInfos[i].vertex_BR.uv2.x = (characterInfos[i].vertex_BR.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                        break;
                                    }

                                case TextureMappingOptions.Paragraph:
                                    characterInfos[i].vertex_BL.uv2.x = (characterInfos[i].vertex_BL.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                    characterInfos[i].vertex_TL.uv2.x = (characterInfos[i].vertex_TL.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                    characterInfos[i].vertex_TR.uv2.x = (characterInfos[i].vertex_TR.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                    characterInfos[i].vertex_BR.uv2.x = (characterInfos[i].vertex_BR.position.x + justificationOffset.x - m_meshExtents.min.x) / (m_meshExtents.max.x - m_meshExtents.min.x) + uvOffset;
                                    break;

                                case TextureMappingOptions.MatchAspect:

                                    switch (m_verticalMapping)
                                    {
                                        case TextureMappingOptions.Character:
                                            characterInfos[i].vertex_BL.uv2.y = 0;
                                            characterInfos[i].vertex_TL.uv2.y = 1;
                                            characterInfos[i].vertex_TR.uv2.y = 0;
                                            characterInfos[i].vertex_BR.uv2.y = 1;
                                            break;

                                        case TextureMappingOptions.Line:
                                            characterInfos[i].vertex_BL.uv2.y = (characterInfos[i].vertex_BL.position.y - lineExtents.min.y) / (lineExtents.max.y - lineExtents.min.y) + uvOffset;
                                            characterInfos[i].vertex_TL.uv2.y = (characterInfos[i].vertex_TL.position.y - lineExtents.min.y) / (lineExtents.max.y - lineExtents.min.y) + uvOffset;
                                            characterInfos[i].vertex_TR.uv2.y = characterInfos[i].vertex_BL.uv2.y;
                                            characterInfos[i].vertex_BR.uv2.y = characterInfos[i].vertex_TL.uv2.y;
                                            break;

                                        case TextureMappingOptions.Paragraph:
                                            characterInfos[i].vertex_BL.uv2.y = (characterInfos[i].vertex_BL.position.y - m_meshExtents.min.y) / (m_meshExtents.max.y - m_meshExtents.min.y) + uvOffset;
                                            characterInfos[i].vertex_TL.uv2.y = (characterInfos[i].vertex_TL.position.y - m_meshExtents.min.y) / (m_meshExtents.max.y - m_meshExtents.min.y) + uvOffset;
                                            characterInfos[i].vertex_TR.uv2.y = characterInfos[i].vertex_BL.uv2.y;
                                            characterInfos[i].vertex_BR.uv2.y = characterInfos[i].vertex_TL.uv2.y;
                                            break;

                                        case TextureMappingOptions.MatchAspect:
                                            Debug.Log("ERROR: Cannot Match both Vertical & Horizontal.");
                                            break;
                                    }

                                    float xDelta = (1 - ((characterInfos[i].vertex_BL.uv2.y + characterInfos[i].vertex_TL.uv2.y) * characterInfos[i].aspectRatio)) / 2;

                                    characterInfos[i].vertex_BL.uv2.x = (characterInfos[i].vertex_BL.uv2.y * characterInfos[i].aspectRatio) + xDelta + uvOffset;
                                    characterInfos[i].vertex_TL.uv2.x = characterInfos[i].vertex_BL.uv2.x;
                                    characterInfos[i].vertex_TR.uv2.x = (characterInfos[i].vertex_TL.uv2.y * characterInfos[i].aspectRatio) + xDelta + uvOffset;
                                    characterInfos[i].vertex_BR.uv2.x = characterInfos[i].vertex_TR.uv2.x;
                                    break;
                            }

                            switch (m_verticalMapping)
                            {
                                case TextureMappingOptions.Character:
                                    characterInfos[i].vertex_BL.uv2.y = 0;
                                    characterInfos[i].vertex_TL.uv2.y = 1;
                                    characterInfos[i].vertex_TR.uv2.y = 1;
                                    characterInfos[i].vertex_BR.uv2.y = 0;
                                    break;

                                case TextureMappingOptions.Line:
                                    characterInfos[i].vertex_BL.uv2.y = (characterInfos[i].vertex_BL.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender);
                                    characterInfos[i].vertex_TL.uv2.y = (characterInfos[i].vertex_TL.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender);
                                    characterInfos[i].vertex_TR.uv2.y = characterInfos[i].vertex_TL.uv2.y;
                                    characterInfos[i].vertex_BR.uv2.y = characterInfos[i].vertex_BL.uv2.y;
                                    break;

                                case TextureMappingOptions.Paragraph:
                                    characterInfos[i].vertex_BL.uv2.y = (characterInfos[i].vertex_BL.position.y - m_meshExtents.min.y) / (m_meshExtents.max.y - m_meshExtents.min.y);
                                    characterInfos[i].vertex_TL.uv2.y = (characterInfos[i].vertex_TL.position.y - m_meshExtents.min.y) / (m_meshExtents.max.y - m_meshExtents.min.y);
                                    characterInfos[i].vertex_TR.uv2.y = characterInfos[i].vertex_TL.uv2.y;
                                    characterInfos[i].vertex_BR.uv2.y = characterInfos[i].vertex_BL.uv2.y;
                                    break;

                                case TextureMappingOptions.MatchAspect:
                                    float yDelta = (1 - ((characterInfos[i].vertex_BL.uv2.x + characterInfos[i].vertex_TR.uv2.x) / characterInfos[i].aspectRatio)) / 2;

                                    characterInfos[i].vertex_BL.uv2.y = yDelta + (characterInfos[i].vertex_BL.uv2.x / characterInfos[i].aspectRatio);
                                    characterInfos[i].vertex_TL.uv2.y = yDelta + (characterInfos[i].vertex_TR.uv2.x / characterInfos[i].aspectRatio);
                                    characterInfos[i].vertex_BR.uv2.y = characterInfos[i].vertex_BL.uv2.y;
                                    characterInfos[i].vertex_TR.uv2.y = characterInfos[i].vertex_TL.uv2.y;
                                    break;
                            }
                            #endregion

                            #region Pack Scale into UV2
                            xScale = characterInfos[i].scale * (1 - m_charWidthAdjDelta);
                            if (!characterInfos[i].isUsingAlternateTypeface && (characterInfos[i].style & FontStyles.Bold) == FontStyles.Bold) xScale *= -1;

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

                            characterInfos[i].vertex_BL.uv.w = xScale;
                            characterInfos[i].vertex_TL.uv.w = xScale;
                            characterInfos[i].vertex_TR.uv.w = xScale;
                            characterInfos[i].vertex_BR.uv.w = xScale;
                            #endregion
                            break;

                        case TMP_TextElementType.Sprite:
                            break;
                    }

                    #region Handle maxVisibleCharacters / maxVisibleLines / Page Mode
                    if (i < m_maxVisibleCharacters && wordCount < m_maxVisibleWords && currentLine < m_maxVisibleLines && m_overflowMode != TextOverflowModes.Page)
                    {
                        characterInfos[i].vertex_BL.position += offset;
                        characterInfos[i].vertex_TL.position += offset;
                        characterInfos[i].vertex_TR.position += offset;
                        characterInfos[i].vertex_BR.position += offset;
                    }
                    else if (i < m_maxVisibleCharacters && wordCount < m_maxVisibleWords && currentLine < m_maxVisibleLines && m_overflowMode == TextOverflowModes.Page && characterInfos[i].pageNumber == pageToDisplay)
                    {
                        characterInfos[i].vertex_BL.position += offset;
                        characterInfos[i].vertex_TL.position += offset;
                        characterInfos[i].vertex_TR.position += offset;
                        characterInfos[i].vertex_BR.position += offset;
                    }
                    else
                    {
                        characterInfos[i].vertex_BL.position = Vector3.zero;
                        characterInfos[i].vertex_TL.position = Vector3.zero;
                        characterInfos[i].vertex_TR.position = Vector3.zero;
                        characterInfos[i].vertex_BR.position = Vector3.zero;
                        characterInfos[i].isVisible = false;
                    }
                    #endregion


                    if (elementType == TMP_TextElementType.Character)
                    {
                        FillCharacterVertexBuffers(i);
                    }
                    else if (elementType == TMP_TextElementType.Sprite)
                    {
                        FillSpriteVertexBuffers(i);
                    }
                }
                #endregion

                m_textInfo.characterInfo[i].bottomLeft += offset;
                m_textInfo.characterInfo[i].topLeft += offset;
                m_textInfo.characterInfo[i].topRight += offset;
                m_textInfo.characterInfo[i].bottomRight += offset;

                m_textInfo.characterInfo[i].origin += offset.x;
                m_textInfo.characterInfo[i].xAdvance += offset.x;

                m_textInfo.characterInfo[i].ascender += offset.y;
                m_textInfo.characterInfo[i].descender += offset.y;
                m_textInfo.characterInfo[i].baseLine += offset.y;

                if (isCharacterVisible)
                {
                }

                #region Adjust lineExtents resulting from alignment offset
                if (currentLine != lastLine || i == m_characterCount - 1)
                {
                    if (currentLine != lastLine)
                    {
                        m_textInfo.lineInfo[lastLine].baseline += offset.y;
                        m_textInfo.lineInfo[lastLine].ascender += offset.y;
                        m_textInfo.lineInfo[lastLine].descender += offset.y;

                        m_textInfo.lineInfo[lastLine].maxAdvance += offset.x;

                        m_textInfo.lineInfo[lastLine].lineExtents.min = new Vector2(m_textInfo.characterInfo[m_textInfo.lineInfo[lastLine].firstCharacterIndex].bottomLeft.x, m_textInfo.lineInfo[lastLine].descender);
                        m_textInfo.lineInfo[lastLine].lineExtents.max = new Vector2(m_textInfo.characterInfo[m_textInfo.lineInfo[lastLine].lastVisibleCharacterIndex].topRight.x, m_textInfo.lineInfo[lastLine].ascender);
                    }

                    if (i == m_characterCount - 1)
                    {
                        m_textInfo.lineInfo[currentLine].baseline += offset.y;
                        m_textInfo.lineInfo[currentLine].ascender += offset.y;
                        m_textInfo.lineInfo[currentLine].descender += offset.y;

                        m_textInfo.lineInfo[currentLine].maxAdvance += offset.x;

                        m_textInfo.lineInfo[currentLine].lineExtents.min = new Vector2(m_textInfo.characterInfo[m_textInfo.lineInfo[currentLine].firstCharacterIndex].bottomLeft.x, m_textInfo.lineInfo[currentLine].descender);
                        m_textInfo.lineInfo[currentLine].lineExtents.max = new Vector2(m_textInfo.characterInfo[m_textInfo.lineInfo[currentLine].lastVisibleCharacterIndex].topRight.x, m_textInfo.lineInfo[currentLine].ascender);
                    }
                }
                #endregion


                #region Track Word Count
                if (char.IsLetterOrDigit(unicode) || unicode == 0x2D || unicode == 0xAD || unicode == 0x2010 || unicode == 0x2011)
                {
                    if (isStartOfWord == false)
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

                        m_textInfo.wordInfo[index].firstCharacterIndex = wordFirstChar;
                        m_textInfo.wordInfo[index].lastCharacterIndex = wordLastChar;
                        m_textInfo.wordInfo[index].characterCount = wordLastChar - wordFirstChar + 1;
                        m_textInfo.wordInfo[index].textComponent = this;

                        wordCount += 1;
                        m_textInfo.wordCount += 1;
                        m_textInfo.lineInfo[currentLine].wordCount += 1;
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

                        m_textInfo.wordInfo[index].firstCharacterIndex = wordFirstChar;
                        m_textInfo.wordInfo[index].lastCharacterIndex = wordLastChar;
                        m_textInfo.wordInfo[index].characterCount = wordLastChar - wordFirstChar + 1;
                        m_textInfo.wordInfo[index].textComponent = this;

                        wordCount += 1;
                        m_textInfo.wordCount += 1;
                        m_textInfo.lineInfo[currentLine].wordCount += 1;
                    }
                }
                #endregion


                #region Underline

                bool isUnderline = (m_textInfo.characterInfo[i].style & FontStyles.Underline) == FontStyles.Underline;
                if (isUnderline)
                {
                    bool isUnderlineVisible = true;
                    int currentPage = m_textInfo.characterInfo[i].pageNumber;
                    m_textInfo.characterInfo[i].underlineVertexIndex = last_vert_index;

                    if (i > m_maxVisibleCharacters || currentLine > m_maxVisibleLines || (m_overflowMode == TextOverflowModes.Page && currentPage + 1 != m_pageToDisplay))
                        isUnderlineVisible = false;

                    if (!isWhiteSpace && unicode != 0x200B)
                    {
                        underlineMaxScale = Mathf.Max(underlineMaxScale, m_textInfo.characterInfo[i].scale);
                        xScaleMax = Mathf.Max(xScaleMax, Mathf.Abs(xScale));
                        underlineBaseLine = Mathf.Min(currentPage == lastPage ? underlineBaseLine : k_LargePositiveFloat, m_textInfo.characterInfo[i].baseLine + font.m_FaceInfo.underlineOffset * underlineMaxScale);
                        lastPage = currentPage;
                    }

                    if (beginUnderline == false && isUnderlineVisible == true && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
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
                            underline_start = new Vector3(m_textInfo.characterInfo[i].bottomLeft.x, underlineBaseLine, 0);
                            underlineColor = m_textInfo.characterInfo[i].underlineColor;
                        }
                    }

                    if (beginUnderline && m_characterCount == 1)
                    {
                        beginUnderline = false;
                        underline_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, underlineBaseLine, 0);
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
                            underline_end = new Vector3(m_textInfo.characterInfo[lastVisibleCharacterIndex].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = m_textInfo.characterInfo[lastVisibleCharacterIndex].scale;
                        }
                        else
                        {
                            underline_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, underlineBaseLine, 0);
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
                        underline_end = new Vector3(m_textInfo.characterInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = m_textInfo.characterInfo[i - 1].scale;

                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                    else if (beginUnderline && i < m_characterCount - 1 && !underlineColor.Compare(m_textInfo.characterInfo[i + 1].underlineColor))
                    {
                        beginUnderline = false;
                        underline_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = m_textInfo.characterInfo[i].scale;

                        DrawUnderlineMesh(underline_start, underline_end, ref last_vert_index, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = k_LargePositiveFloat;
                    }
                }
                else
                {
                    if (beginUnderline == true)
                    {
                        beginUnderline = false;
                        underline_end = new Vector3(m_textInfo.characterInfo[i - 1].topRight.x, underlineBaseLine, 0);
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
                    bool isStrikeThroughVisible = true;
                    m_textInfo.characterInfo[i].strikethroughVertexIndex = last_vert_index;

                    if (i > m_maxVisibleCharacters || currentLine > m_maxVisibleLines || (m_overflowMode == TextOverflowModes.Page && m_textInfo.characterInfo[i].pageNumber + 1 != m_pageToDisplay))
                        isStrikeThroughVisible = false;

                    if (beginStrikethrough == false && isStrikeThroughVisible && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginStrikethrough = true;
                            strikethroughPointSize = m_textInfo.characterInfo[i].pointSize;
                            strikethroughScale = m_textInfo.characterInfo[i].scale;
                            strikethrough_start = new Vector3(m_textInfo.characterInfo[i].bottomLeft.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                            strikethroughColor = m_textInfo.characterInfo[i].strikethroughColor;
                            strikethroughBaseline = m_textInfo.characterInfo[i].baseLine;
                        }
                    }

                    if (beginStrikethrough && m_characterCount == 1)
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && i == lineInfo.lastCharacterIndex)
                    {
                        if (isWhiteSpace || unicode == 0x200B)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            strikethrough_end = new Vector3(m_textInfo.characterInfo[lastVisibleCharacterIndex].topRight.x, m_textInfo.characterInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }
                        else
                        {
                            strikethrough_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }

                        beginStrikethrough = false;
                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && i < m_characterCount && (m_textInfo.characterInfo[i + 1].pointSize != strikethroughPointSize || !TMP_Math.Approximately(m_textInfo.characterInfo[i + 1].baseLine + offset.y, strikethroughBaseline)))
                    {
                        beginStrikethrough = false;

                        int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                        if (i > lastVisibleCharacterIndex)
                            strikethrough_end = new Vector3(m_textInfo.characterInfo[lastVisibleCharacterIndex].topRight.x, m_textInfo.characterInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        else
                            strikethrough_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && i < m_characterCount && currentFontAsset.GetInstanceID() != characterInfos[i + 1].fontAsset.GetInstanceID())
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new Vector3(m_textInfo.characterInfo[i].topRight.x, m_textInfo.characterInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                    else if (beginStrikethrough && !isStrikeThroughVisible)
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new Vector3(m_textInfo.characterInfo[i - 1].topRight.x, m_textInfo.characterInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                }
                else
                {
                    if (beginStrikethrough == true)
                    {
                        beginStrikethrough = false;
                        strikethrough_end = new Vector3(m_textInfo.characterInfo[i - 1].topRight.x, m_textInfo.characterInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethrough_start, strikethrough_end, ref last_vert_index, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor);
                    }
                }
                #endregion


                #region Text Highlighting
                bool isHighlight = (m_textInfo.characterInfo[i].style & FontStyles.Highlight) == FontStyles.Highlight;
                if (isHighlight)
                {
                    bool isHighlightVisible = true;
                    int currentPage = m_textInfo.characterInfo[i].pageNumber;

                    if (i > m_maxVisibleCharacters || currentLine > m_maxVisibleLines || (m_overflowMode == TextOverflowModes.Page && currentPage + 1 != m_pageToDisplay))
                        isHighlightVisible = false;

                    if (beginHighlight == false && isHighlightVisible == true && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
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
                    if (beginHighlight == true)
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
            m_textInfo.pageCount = m_pageNumber + 1;

            k_GenerateTextPhaseIIMarker.End();

            k_GenerateTextPhaseIIIMarker.Begin();
            
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

                    m_subTextObjects[i].raycastTarget = this.raycastTarget;
                }
            }

            if (m_ShouldUpdateCulling)
                UpdateCulling();

            TMPro_EventManager.ON_TEXT_CHANGED(this);

            k_GenerateTextPhaseIIIMarker.End();
            k_GenerateTextMarker.End();
        }


        /// <summary>
        /// Method to return the local corners of the Text Container or RectTransform.
        /// </summary>
        /// <returns></returns>
        protected Vector3[] GetTextContainerLocalCorners()
        {
            if (m_rectTransform == null) m_rectTransform = this.rectTransform;

            m_rectTransform.GetLocalCorners(m_RectTransformCorners);

            return m_RectTransformCorners;
        }


        /// <summary>
        /// Method to Enable or Disable child SubMesh objects.
        /// </summary>
        /// <param name="state"></param>
        protected void SetActiveSubMeshes(bool state)
        {
            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                if (m_subTextObjects[i].enabled != state)
                    m_subTextObjects[i].enabled = state;
            }
        }


        /// <summary>
        /// Destroy Sub Mesh Objects
        /// </summary>
        protected void DestroySubMeshObjects()
        {
            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                DestroyImmediate(m_subTextObjects[i]);
        }


        /// <summary>
        ///  Method returning the compound bounds of the text object and child sub objects.
        /// </summary>
        /// <returns></returns>
        protected override Bounds GetCompoundBounds()
        {
            Bounds mainBounds = m_mesh.bounds;
            Vector3 min = mainBounds.min;
            Vector3 max = mainBounds.max;

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                Bounds subBounds = m_subTextObjects[i].mesh.bounds;
                min.x = min.x < subBounds.min.x ? min.x : subBounds.min.x;
                min.y = min.y < subBounds.min.y ? min.y : subBounds.min.y;

                max.x = max.x > subBounds.max.x ? max.x : subBounds.max.x;
                max.y = max.y > subBounds.max.y ? max.y : subBounds.max.y;
            }

            Vector3 center = (min + max) / 2;
            Vector2 size = max - min;
            return new Bounds(center, size);
        }

        internal Rect GetCanvasSpaceClippingRect()
        {
            if (m_canvas == null || m_canvas.rootCanvas == null || m_mesh == null)
                return Rect.zero;

            Transform rootCanvasTransform = m_canvas.rootCanvas.transform;
            Bounds compoundBounds = GetCompoundBounds();

            Vector2 position =  rootCanvasTransform.InverseTransformPoint(m_rectTransform.position);

            Vector2 canvasLossyScale = rootCanvasTransform.lossyScale;
            Vector2 lossyScale = m_rectTransform.lossyScale / canvasLossyScale;

            return new Rect(position + compoundBounds.min * lossyScale, compoundBounds.size * lossyScale);
        }

        /// <summary>
        /// Method to Update Scale in UV2
        /// </summary>

        /// <summary>
        /// Method to update the SDF Scale in UV2.
        /// </summary>
        /// <param name="scaleDelta"></param>
        private void UpdateSDFScale(float scaleDelta)
        {
            if (scaleDelta == 0 || scaleDelta == float.PositiveInfinity || scaleDelta == float.NegativeInfinity)
            {
                _havePropertiesChanged = true;
                OnPreRenderCanvas();
                return;
            }

            for (int materialIndex = 0; materialIndex < m_textInfo.materialCount; materialIndex ++)
            {
                TMP_MeshInfo meshInfo = m_textInfo.meshInfo[materialIndex];

                for (int i = 0; i < meshInfo.uvs0.Length; i++)
                {
                    meshInfo.uvs0[i].w *= Mathf.Abs(scaleDelta);
                }
            }

            for (int i = 0; i < m_textInfo.materialCount; i++)
            {
                if (i == 0)
                {
                    m_mesh.SetUVs(0, m_textInfo.meshInfo[0].uvs0);
                    m_canvasRenderer.SetMesh(m_mesh);
                }
                else
                {
                    m_subTextObjects[i].mesh.SetUVs(0, m_textInfo.meshInfo[i].uvs0);
                    m_subTextObjects[i].canvasRenderer.SetMesh(m_subTextObjects[i].mesh);
                }
            }
        }
        #endregion
    }
}
