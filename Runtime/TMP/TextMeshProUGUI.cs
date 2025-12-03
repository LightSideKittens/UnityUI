using System;
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
    public class TextMeshProUGUI : TMP_Text, ILayoutElement
    {
        public override Material materialForRendering => TMP_MaterialManager.GetMaterialForRendering(this, m_sharedMaterial);


        public override bool autoSizeTextContainer
        {
            get => m_autoSizeTextContainer;

            set { if (m_autoSizeTextContainer == value) return; m_autoSizeTextContainer = value; if (m_autoSizeTextContainer) { CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this); SetLayoutDirty(); } }
        }


        public override Mesh mesh => m_mesh;

        private bool m_isRebuildingLayout = false;

        public void CalculateLayoutInputHorizontal() { }


        public void CalculateLayoutInputVertical()
        {
            isBidiProcessing = false;
            OnPreRenderCanvas();

            if (!m_isMaterialDirty) return;

            UpdateMaterial();
            
            m_isMaterialDirty = false;
        }


        public override void SetVerticesDirty()
        {
            if (this == null || !IsActive())
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
        }


        public override void SetLayoutDirty()
        {
            if (this == null || !IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            m_isLayoutDirty = true;

            if (m_OnDirtyLayoutCallback != null)
                m_OnDirtyLayoutCallback();
        }


        public override void SetMaterialDirty()
        {
            if (this == null || !IsActive())
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics())
                return;

            m_isMaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }


        public override void SetAllDirty()
        {
            SetLayoutDirty();
            SetVerticesDirty();
            SetMaterialDirty();
        }


        private void UpdateSubObjectPivot()
        {
            if (m_textInfo == null) return;

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                m_subTextObjects[i].SetPivotDirty();
            }
        }


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


        protected override void UpdateMaterial()
        {
            if (m_sharedMaterial == null || canvasRenderer == null) return;

            m_canvasRenderer.materialCount = 1;
            m_canvasRenderer.SetMaterial(materialForRendering, 0);
        }


        public Vector4 maskOffset
        {
            get => m_maskOffset;
            set { m_maskOffset = value; UpdateMask(); _havePropertiesChanged = true; }
        }




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
        
        private Rect m_ClipRect;
        private bool m_ValidRect;

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


        /// <param name="ignoreActiveState">Ignore Active State of text objects. Inactive objects are ignored by default.</param>
        public override void ForceMeshUpdate(bool ignoreActiveState = false)
        {
            _havePropertiesChanged = true;
            m_ignoreActiveState = ignoreActiveState;

            if (m_canvas == null)
                m_canvas = GetComponentInParent<Canvas>();

            OnPreRenderCanvas();
        }

        public override void ClearMesh()
        {
            m_canvasRenderer.SetMesh(null);

            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
                m_subTextObjects[i].canvasRenderer.SetMesh(null);
        }


        public override event Action<TMP_TextInfo> OnPreRenderText;


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


        public void UpdateFontAsset()
        {
            LoadFontAsset();
        }

        #region TMPro_UGUI_Private
        [SerializeField]
        private bool m_hasFontAssetChanged;
        
        private bool m_isFirstAllocation;

        private int m_max_characters = 8;

        [SerializeField]
        private Material m_baseMaterial;

        private bool m_isScrollRegionSet;

        [SerializeField]
        private Vector4 m_maskOffset;

        private Matrix4x4 m_EnvMapMatrix;


        [NonSerialized]
        private bool m_isRegisteredForEvents;

        private static ProfilerMarker k_SetArraySizesMarker = new("TMP.SetArraySizes");


        protected override void Awake()
        {
#if UNITY_EDITOR
            if (TMP_Settings.instance == null)
            {
                if (!m_isWaitingOnResourceLoad)
                    TMPro_EventManager.RESOURCE_LOAD_EVENT.Add(ON_RESOURCES_LOADED);

                m_isWaitingOnResourceLoad = true;
                return;
            }
#endif
            
            m_canvas = canvas;

            m_isOrthographic = true;

            m_rectTransform = gameObject.GetComponent<RectTransform>();
            if (m_rectTransform == null)
                m_rectTransform = gameObject.AddComponent<RectTransform>();

            m_canvasRenderer = GetComponent<CanvasRenderer>();
            if (m_canvasRenderer == null)
                m_canvasRenderer = gameObject.AddComponent<CanvasRenderer> ();

            if (m_mesh == null)
            {
                m_mesh = new();
                m_mesh.hideFlags = HideFlags.HideAndDontSave;
                #if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_mesh.name = "TextMeshPro UI Mesh";
                #endif
                m_textInfo = new(this);
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
            if (!m_isAwake)
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

            ComputeMarginSize();

            SetAllDirty();

            RecalculateClipping();
            RecalculateMasking();
        }


        protected override void OnDisable()
        {
            if (!m_isAwake)
                return;

            GraphicRegistry.UnregisterGraphicForCanvas(m_canvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

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
            if (!m_isAwake)
                return;

            LoadDefaultSettings();
            LoadFontAsset();

            _havePropertiesChanged = true;
        }


        protected override void OnValidate()
        {
            if (!m_isAwake)
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
                
            SetAllDirty();
        }


        /// <param name="go">The affected GameObject</param>
        private void OnPrefabInstanceUpdate(GameObject go)
        {
            if (this == null)
            {
                UnityEditor.PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdate;
                return;
            }

            if (go == gameObject)
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


        /// <param name="textObject"></param>
        private void ON_COLOR_GRADIENT_CHANGED(Object gradient)
        {
            _havePropertiesChanged = true;
            SetVerticesDirty();
        }


        private void ON_TMP_SETTINGS_CHANGED()
        {
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

        private void ValidateEnvMapProperty()
        {
            if (m_sharedMaterial != null)
                m_hasEnvMapProperty = m_sharedMaterial.HasProperty(ShaderUtilities.ID_EnvMap) && m_sharedMaterial.GetTexture(ShaderUtilities.ID_EnvMap) != null;
            else
                m_hasEnvMapProperty = false;
        }

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

                Vector4 mask = new(center.x, center.y, width, height);


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

        private Dictionary<int, int> materialIndexPairs = new();

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

            m_materialReferenceStack.SetDefault(new(m_currentMaterialIndex, m_currentFontAsset, m_currentMaterial, m_padding));

            m_materialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_currentMaterial, m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);

            if (m_textInfo == null)
                m_textInfo = new(m_InternalTextProcessingArraySize);
            else if (m_textInfo.characterInfo.Length < m_InternalTextProcessingArraySize)
                TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_InternalTextProcessingArraySize, false);

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

            for (int i = 0; i < textProcessingArray.Length && textProcessingArray[i].unicode != 0; i++)
            {
                if (m_textInfo.characterInfo == null || m_totalCharacterCount >= m_textInfo.characterInfo.Length)
                    TMP_TextInfo.Resize(ref m_textInfo.characterInfo, m_totalCharacterCount + 1, true);

                uint unicode = textProcessingArray[i].unicode;

                #region PARSE XML TAGS
                if (m_isRichText && unicode == 60)
                {
                    int prev_MaterialIndex = m_currentMaterialIndex;

                    if (ValidateHtmlTag(textProcessingArray, i + 1, out var endTagIndex))
                    {
                        int tagStartIndex = textProcessingArray[i].stringIndex;
                        i = endTagIndex;

                        if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)
                            m_isUsingBold = true;

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
                #endregion

                #region LOOKUP GLYPH
                TMP_TextElement character = null;

                uint nextCharacter = i + 1 < textProcessingArray.Length ? textProcessingArray[i + 1].unicode : 0;

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
                            ? string.Format("The character with Unicode value \\U{0:X8} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4} in text object [{3}].", srcGlyph, m_fontAsset.name, character.unicode, name)
                            : string.Format("The character with Unicode value \\u{0:X4} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4} in text object [{3}].", srcGlyph, m_fontAsset.name, character.unicode, name);

                        Debug.LogWarning(formattedWarning, this);
                    }
                }
                #endregion

                ref var chInfo = ref m_textInfo.characterInfo[m_totalCharacterCount];
                chInfo.alternativeGlyph = null;
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
                            chInfo.alternativeGlyph = glyph;
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
                                chInfo.alternativeGlyph = glyph;

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
                #endregion

                chInfo.textElement = character;
                chInfo.isUsingAlternateTypeface = isUsingAlternativeTypeface;
                chInfo.character = (char)unicode;
                chInfo.index = textProcessingArray[i].stringIndex;
                chInfo.stringLength = textProcessingArray[i].length;
                chInfo.fontAsset = m_currentFontAsset;

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
                            int fallbackMaterialIndex = MaterialReference.AddMaterialReference(new(m_currentMaterial), m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                            materialIndexPairs[m_currentMaterialIndex] = fallbackMaterialIndex;
                            m_currentMaterialIndex = fallbackMaterialIndex;
                        }

                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    }
                    else
                    {
                        m_currentMaterialIndex = MaterialReference.AddMaterialReference(new(m_currentMaterial), m_currentFontAsset, ref m_materialReferences, m_materialReferenceIndexLookup);
                        m_materialReferences[m_currentMaterialIndex].referenceCount += 1;
                    }
                }

                chInfo.material = m_currentMaterial;
                chInfo.materialReferenceIndex = m_currentMaterialIndex;
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
                            m_textInfo.meshInfo[i] = new(m_mesh, referenceCount + 1);
                        else
                            m_textInfo.meshInfo[i] = new(m_subTextObjects[i].mesh, referenceCount + 1);
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

        public override void ComputeMarginSize()
        {
            Debug.Log($"ComputeMarginSize {rectTransform != null}");
            if (rectTransform != null)
            {
                Rect rect = m_rectTransform.rect;
                Debug.Log($"ComputeMarginSize {rect}");

                m_marginWidth = rect.width - m_margin.x - m_margin.z;
                m_marginHeight = rect.height - m_margin.y - m_margin.w;

                m_PreviousRectTransformSize = rect.size;
                m_PreviousPivotPosition = m_rectTransform.pivot;

                m_RectTransformCorners = GetTextContainerLocalCorners();
            }
        }


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
        }


        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_canvas = canvas;

            ComputeMarginSize();
            _havePropertiesChanged = true;
        }


        protected override void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            bool hasCanvasScaleFactorChanged = false;
            if (m_canvas != null && !Mathf.Approximately(m_CanvasScaleFactor, m_canvas.scaleFactor))
            {
                m_CanvasScaleFactor = m_canvas.scaleFactor;
                hasCanvasScaleFactorChanged = true;
            }

            if (!hasCanvasScaleFactorChanged &&
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


        internal override void InternalUpdate()
        {
            if (!_havePropertiesChanged)
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

            UpdateEnvMapMatrix();
        }
        
        /// <returns></returns>
        protected Vector3[] GetTextContainerLocalCorners()
        {
            if (m_rectTransform == null) m_rectTransform = rectTransform;

            m_rectTransform.GetLocalCorners(m_RectTransformCorners);

            return m_RectTransformCorners;
        }


        /// <param name="state"></param>
        protected void SetActiveSubMeshes(bool state)
        {
            for (int i = 1; i < m_subTextObjects.Length && m_subTextObjects[i] != null; i++)
            {
                if (m_subTextObjects[i].enabled != state)
                    m_subTextObjects[i].enabled = state;
            }
        }
        

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
            return new(center, size);
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

            return new(position + compoundBounds.min * lossyScale, compoundBounds.size * lossyScale);
        }


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
