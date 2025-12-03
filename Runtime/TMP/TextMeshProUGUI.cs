using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Debug = UnityEngine.Debug;
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
    public class TextMeshProUGUI : TMPText
    {
        public override Material materialForRendering => TMP_MaterialManager.GetMaterialForRendering(this, m_sharedMaterial);

        public override Mesh mesh => m_mesh;

        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);

            if (update == CanvasUpdate.Prelayout)
            {
                var sw = new Stopwatch();
                sw.Start();
                OnPreRenderCanvas();
                sw.Stop();
                Debug.Log(sw.ElapsedTicks);
                
                if (!m_isMaterialDirty) return;

                UpdateMaterial();
            
                m_isMaterialDirty = false;
            }
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

            for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
            {
                MSubTextObjects[i].SetPivotDirty();
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
            if (m_sharedMaterial == null || CanvasRenderer == null) return;

            MCanvasRenderer.materialCount = 1;
            MCanvasRenderer.SetMaterial(materialForRendering, 0);
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
            MShouldUpdateCulling = false;

            if (m_isLayoutDirty)
            {
                MShouldUpdateCulling = true;
                m_ClipRect = clipRect;
                m_ValidRect = validRect;
                return;
            }

            Rect rect = GetCanvasSpaceClippingRect();

            var cull = !validRect || !clipRect.Overlaps(rect, true);
            if (MCanvasRenderer.cull != cull)
            {
                MCanvasRenderer.cull = cull;
                onCullStateChanged.Invoke(cull);
                OnCullingChanged();

                for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
                {
                    MSubTextObjects[i].canvasRenderer.cull = cull;
                }
            }
        }
        
        private Rect m_ClipRect;
        private bool m_ValidRect;

        internal override void UpdateCulling()
        {
            Rect rect = GetCanvasSpaceClippingRect();

            var cull = !m_ValidRect || !m_ClipRect.Overlaps(rect, true);
            if (MCanvasRenderer.cull != cull)
            {
                MCanvasRenderer.cull = cull;
                onCullStateChanged.Invoke(cull);
                OnCullingChanged();

                for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
                {
                    MSubTextObjects[i].canvasRenderer.cull = cull;
                }
            }

            MShouldUpdateCulling = false;
        }


        public override void UpdateMeshPadding()
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_enableExtraPadding, m_isUsingBold);
            m_isMaskingEnabled = ShaderUtilities.IsMaskingEnabled(m_sharedMaterial);
            _havePropertiesChanged = true;
            checkPaddingRequired = false;

            if (m_textInfo == null) return;

            for (int i = 1; i < m_textInfo.materialCount; i++)
                MSubTextObjects[i].UpdateMeshPadding(m_enableExtraPadding, m_isUsingBold);
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
                MSubTextObjects[i].CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
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
                MSubTextObjects[i].CrossFadeAlpha(alpha, duration, ignoreTimeScale);
            }
        }

        public override void ClearMesh()
        {
            MCanvasRenderer.SetMesh(null);

            for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
                MSubTextObjects[i].canvasRenderer.SetMesh(null);
        }


        public override event Action<TMP_TextInfo> OnPreRenderText;


        /// <param name="mesh"></param>
        /// <param name="index"></param>
        public override void UpdateGeometry(Mesh mesh, int index)
        {
            mesh.RecalculateBounds();

            if (index == 0)
            {
                MCanvasRenderer.SetMesh(mesh);
            }
            else
            {
                MSubTextObjects[index].canvasRenderer.SetMesh(mesh);
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
            
            MCanvas = canvas;

            m_isOrthographic = true;

            m_rectTransform = gameObject.GetComponent<RectTransform>();
            if (m_rectTransform == null)
                m_rectTransform = gameObject.AddComponent<RectTransform>();

            MCanvasRenderer = GetComponent<CanvasRenderer>();
            if (MCanvasRenderer == null)
                MCanvasRenderer = gameObject.AddComponent<CanvasRenderer> ();

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

            MCanvas = GetCanvas();

            SetActiveSubMeshes(true);

            GraphicRegistry.RegisterGraphicForCanvas(MCanvas, this);

            ComputeMarginSize();

            SetAllDirty();

            RecalculateClipping();
            RecalculateMasking();
        }


        protected override void OnDisable()
        {
            if (!m_isAwake)
                return;

            GraphicRegistry.UnregisterGraphicForCanvas(MCanvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (MCanvasRenderer != null)
                MCanvasRenderer.Clear();

            SetActiveSubMeshes(false);

            LayoutRebuilder.MarkLayoutForRebuild(m_rectTransform);
            RecalculateClipping();
            RecalculateMasking();
        }


        protected override void OnDestroy()
        {
            GraphicRegistry.UnregisterGraphicForCanvas(MCanvas, this);

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
            if (MCanvasRenderer == null)
            {
                MCanvasRenderer = gameObject.GetComponent<CanvasRenderer> ();
                if (MCanvasRenderer == null)
                {
                    MCanvasRenderer = gameObject.AddComponent<CanvasRenderer> ();
                }
            }
            
            if (!m_isAwake)
                return;

            if (m_fontAsset == null || m_hasFontAssetChanged)
            {
                LoadFontAsset();
                m_hasFontAssetChanged = false;
            }

            if (MCanvasRenderer == null || MCanvasRenderer.GetMaterial() == null || MCanvasRenderer.GetMaterial().GetTexture(ShaderUtilities.ID_MainTex) == null || m_fontAsset == null || m_fontAsset.atlasTexture.GetInstanceID() != MCanvasRenderer.GetMaterial().GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
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
                        MSubTextObjects[i + 1] = subTextObjects[i];
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

            if (MCanvasRenderer == null || MCanvasRenderer.GetMaterial() == null)
            {
                if (MCanvasRenderer == null) return;

                if (m_fontAsset != null)
                {
                    MCanvasRenderer.SetMaterial(m_fontAsset.material, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
                }
                else
                    Debug.LogWarning("No Font Asset assigned to " + name + ". Please assign a Font Asset.", this);
            }


            if (MCanvasRenderer.GetMaterial() != m_sharedMaterial && m_fontAsset == null)
            {
                m_sharedMaterial = MCanvasRenderer.GetMaterial();
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
                UnityEditor.Undo.RecordObject(MCanvasRenderer, "Material Assignment");

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
                    m_fontMaterials[i] = MSubTextObjects[i].material;
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
                    m_fontSharedMaterials[i] = MSubTextObjects[i].sharedMaterial;
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
                    if (materials[i].GetTexture(ShaderUtilities.ID_MainTex) == null || materials[i].GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() != MSubTextObjects[i].sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                        continue;

                    if (MSubTextObjects[i].isDefaultMaterial)
                        MSubTextObjects[i].sharedMaterial = m_fontSharedMaterials[i] = materials[i];
                }
            }
        }


        protected override void SetOutlineThickness(float thickness)
        {
            if (m_fontMaterial != null && m_sharedMaterial.GetInstanceID() != m_fontMaterial.GetInstanceID())
            {
                m_sharedMaterial = m_fontMaterial;
                MCanvasRenderer.SetMaterial(m_sharedMaterial, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
            }
            else if(m_fontMaterial == null)
            {
                m_fontMaterial = CreateMaterialInstance(m_sharedMaterial);
                m_sharedMaterial = m_fontMaterial;
                MCanvasRenderer.SetMaterial(m_sharedMaterial, m_sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex));
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

                for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
                {
                    mat = MSubTextObjects[i].materialForRendering;

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

                for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
                {
                    mat = MSubTextObjects[i].materialForRendering;

                    if (mat != null)
                    {
                        mat.SetFloat(ShaderUtilities.ShaderTag_CullMode, 0);
                    }
                }
            }
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

                MRectTransformCorners = GetTextContainerLocalCorners();
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
            MCanvas = canvas;
        }


        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            MCanvas = canvas;

            ComputeMarginSize();
            _havePropertiesChanged = true;
        }


        protected override void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            bool hasCanvasScaleFactorChanged = false;
            if (MCanvas != null && !Mathf.Approximately(MCanvasScaleFactor, MCanvas.scaleFactor))
            {
                MCanvasScaleFactor = MCanvas.scaleFactor;
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
        
        /// <returns></returns>
        protected Vector3[] GetTextContainerLocalCorners()
        {
            if (m_rectTransform == null) m_rectTransform = rectTransform;

            m_rectTransform.GetLocalCorners(MRectTransformCorners);

            return MRectTransformCorners;
        }


        /// <param name="state"></param>
        protected void SetActiveSubMeshes(bool state)
        {
            for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
            {
                if (MSubTextObjects[i].enabled != state)
                    MSubTextObjects[i].enabled = state;
            }
        }
        

        /// <returns></returns>
        protected override Bounds GetCompoundBounds()
        {
            Bounds mainBounds = m_mesh.bounds;
            Vector3 min = mainBounds.min;
            Vector3 max = mainBounds.max;

            for (int i = 1; i < MSubTextObjects.Length && MSubTextObjects[i] != null; i++)
            {
                Bounds subBounds = MSubTextObjects[i].mesh.bounds;
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
            if (MCanvas == null || MCanvas.rootCanvas == null || m_mesh == null)
                return Rect.zero;

            Transform rootCanvasTransform = MCanvas.rootCanvas.transform;
            Bounds compoundBounds = GetCompoundBounds();

            Vector2 position =  rootCanvasTransform.InverseTransformPoint(m_rectTransform.position);

            Vector2 canvasLossyScale = rootCanvasTransform.lossyScale;
            Vector2 lossyScale = m_rectTransform.lossyScale / canvasLossyScale;

            return new(position + compoundBounds.min * lossyScale, compoundBounds.size * lossyScale);
        }
        
        #endregion
    }
}
