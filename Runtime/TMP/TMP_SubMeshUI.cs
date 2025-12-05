using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

#pragma warning disable 0414

namespace TMPro
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class TMP_SubMeshUI : MaskableGraphic
    {
        public TMP_FontAsset fontAsset
        {
            get => m_fontAsset;
            set => m_fontAsset = value;
        }

        [SerializeField] private TMP_FontAsset m_fontAsset;


        public override Texture mainTexture
        {
            get
            {
                if (sharedMaterial != null)
                    return sharedMaterial.GetTexture(ShaderUtilities.ID_MainTex);

                return null;
            }
        }


        public override Material material
        {
            get => GetMaterial(m_sharedMaterial);

            set
            {
                if (m_sharedMaterial != null && m_sharedMaterial.GetInstanceID() == value.GetInstanceID())
                    return;

                m_sharedMaterial = m_material = value;

                m_padding = GetPaddingForMaterial();

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        [SerializeField] private Material m_material;


        public Material sharedMaterial
        {
            get => m_sharedMaterial;
            set => SetSharedMaterial(value);
        }

        [SerializeField] private Material m_sharedMaterial;


        public Material fallbackMaterial
        {
            get => m_fallbackMaterial;
            set
            {
                if (m_fallbackMaterial == value) return;

                if (m_fallbackMaterial != null && m_fallbackMaterial != value)
                    TMP_MaterialManager.ReleaseFallbackMaterial(m_fallbackMaterial);

                m_fallbackMaterial = value;
                TMP_MaterialManager.AddFallbackMaterialReference(m_fallbackMaterial);

                SetSharedMaterial(m_fallbackMaterial);
            }
        }

        private Material m_fallbackMaterial;


        public Material fallbackSourceMaterial
        {
            get => m_fallbackSourceMaterial;
            set => m_fallbackSourceMaterial = value;
        }

        private Material m_fallbackSourceMaterial;


        public override Material materialForRendering =>
            TMP_MaterialManager.GetMaterialForRendering(this, m_sharedMaterial);


        public bool isDefaultMaterial
        {
            get => m_isDefaultMaterial;
            set => m_isDefaultMaterial = value;
        }

        [SerializeField] private bool m_isDefaultMaterial;


        public float padding
        {
            get => m_padding;
            set => m_padding = value;
        }

        [SerializeField] private float m_padding;


        public Mesh mesh
        {
            get
            {
                if (m_mesh == null)
                {
                    m_mesh = new();
                    m_mesh.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_mesh;
            }
            set => m_mesh = value;
        }

        private Mesh m_mesh;


        public TMP_Text textComponent
        {
            get
            {
                if (m_TextComponent == null)
                    m_TextComponent = GetComponentInParent<TextMeshProUGUI>();

                return m_TextComponent;
            }
        }

        [SerializeField] private TMP_Text m_TextComponent;


        [System.NonSerialized] private bool m_isRegisteredForEvents;
        private bool m_materialDirty;


        public static TMP_SubMeshUI AddSubTextObject(TMP_Text textComponent, MaterialReference materialReference)
        {
            GameObject go = new();
            go.hideFlags = TMP_Settings.hideSubTextObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave;

            go.transform.SetParent(textComponent.transform, false);
            go.transform.SetAsFirstSibling();
            go.layer = textComponent.gameObject.layer;

#if UNITY_EDITOR
            go.name = materialReference.material == null
                ? "TMP SubMesh"
                : "TMP SubMesh [" + materialReference.material.name + "]";
#endif

            RectTransform rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = textComponent.rectTransform.pivot;

            LayoutElement layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            TMP_SubMeshUI subMesh = go.AddComponent<TMP_SubMeshUI>();
            subMesh.m_TextComponent = textComponent;
            subMesh.m_fontAsset = materialReference.fontAsset;
            subMesh.m_isDefaultMaterial = materialReference.isDefaultMaterial;
            subMesh.maskable = textComponent.maskable;
            subMesh.SetSharedMaterial(materialReference.material);

            return subMesh;
        }


        protected override void OnEnable()
        {
            if (!m_isRegisteredForEvents)
            {
#if UNITY_EDITOR
                TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Add(ON_MATERIAL_PROPERTY_CHANGED);
                TMPro_EventManager.FONT_PROPERTY_EVENT.Add(ON_FONT_PROPERTY_CHANGED);
                TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Add(ON_DRAG_AND_DROP_MATERIAL);
                TMPro_EventManager.SPRITE_ASSET_PROPERTY_EVENT.Add(ON_SPRITE_ASSET_PROPERTY_CHANGED);
#endif

                m_isRegisteredForEvents = true;
            }

            if (hideFlags != HideFlags.DontSave)
                hideFlags = HideFlags.DontSave;

            m_ShouldRecalculateStencil = true;
            RecalculateClipping();
            RecalculateMasking();
        }


        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_fallbackMaterial != null)
            {
                TMP_MaterialManager.ReleaseFallbackMaterial(m_fallbackMaterial);
                m_fallbackMaterial = null;
            }
        }


        protected override void OnDestroy()
        {
            if (m_mesh != null) DestroyImmediate(m_mesh);

            if (m_MaskMaterial != null)
                TMP_MaterialManager.ReleaseStencilMaterial(m_MaskMaterial);

            if (m_fallbackMaterial != null)
            {
                TMP_MaterialManager.ReleaseFallbackMaterial(m_fallbackMaterial);
                m_fallbackMaterial = null;
            }

#if UNITY_EDITOR
            TMPro_EventManager.MATERIAL_PROPERTY_EVENT.Remove(ON_MATERIAL_PROPERTY_CHANGED);
            TMPro_EventManager.FONT_PROPERTY_EVENT.Remove(ON_FONT_PROPERTY_CHANGED);
            TMPro_EventManager.DRAG_AND_DROP_MATERIAL_EVENT.Remove(ON_DRAG_AND_DROP_MATERIAL);
            TMPro_EventManager.SPRITE_ASSET_PROPERTY_EVENT.Remove(ON_SPRITE_ASSET_PROPERTY_CHANGED);
#endif

            m_isRegisteredForEvents = false;

            RecalculateClipping();

            if (m_TextComponent != null)
            {
                m_TextComponent.havePropertiesChanged = true;
                m_TextComponent.SetAllDirty();
            }
        }


#if UNITY_EDITOR
        private void ON_MATERIAL_PROPERTY_CHANGED(bool isChanged, Material mat)
        {
            if (m_sharedMaterial == null)
                return;

            int targetMaterialID = mat.GetInstanceID();
            int sharedMaterialID = m_sharedMaterial.GetInstanceID();
            int maskingMaterialID = m_MaskMaterial == null ? 0 : m_MaskMaterial.GetInstanceID();
            int fallbackSourceMaterialID =
                m_fallbackSourceMaterial == null ? 0 : m_fallbackSourceMaterial.GetInstanceID();

            bool hasCullModeProperty = m_sharedMaterial.HasProperty(ShaderUtilities.ShaderTag_CullMode);
            float cullMode = 0;

            if (hasCullModeProperty)
            {
                cullMode = textComponent.fontSharedMaterial.GetFloat(ShaderUtilities.ShaderTag_CullMode);
                m_sharedMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
            }

            if (m_fallbackMaterial != null && fallbackSourceMaterialID == targetMaterialID &&
                TMP_Settings.matchMaterialPreset)
            {
                TMP_MaterialManager.CopyMaterialPresetProperties(mat, m_fallbackMaterial);

                m_fallbackMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
            }

            if (m_MaskMaterial != null)
            {
                UnityEditor.Undo.RecordObject(m_MaskMaterial, "Material Property Changes");
                UnityEditor.Undo.RecordObject(m_sharedMaterial, "Material Property Changes");

                if (targetMaterialID == sharedMaterialID)
                {
                    float stencilID = m_MaskMaterial.GetFloat(ShaderUtilities.ID_StencilID);
                    float stencilComp = m_MaskMaterial.GetFloat(ShaderUtilities.ID_StencilComp);
                    m_MaskMaterial.CopyPropertiesFromMaterial(mat);
                    m_MaskMaterial.shaderKeywords = mat.shaderKeywords;

                    m_MaskMaterial.SetFloat(ShaderUtilities.ID_StencilID, stencilID);
                    m_MaskMaterial.SetFloat(ShaderUtilities.ID_StencilComp, stencilComp);
                }
                else if (targetMaterialID == maskingMaterialID)
                {
                    GetPaddingForMaterial(mat);

                    m_sharedMaterial.CopyPropertiesFromMaterial(mat);
                    m_sharedMaterial.shaderKeywords = mat.shaderKeywords;
                    m_sharedMaterial.SetFloat(ShaderUtilities.ID_StencilID, 0);
                    m_sharedMaterial.SetFloat(ShaderUtilities.ID_StencilComp, 8);
                }
                else if (fallbackSourceMaterialID == targetMaterialID)
                {
                    float stencilID = m_MaskMaterial.GetFloat(ShaderUtilities.ID_StencilID);
                    float stencilComp = m_MaskMaterial.GetFloat(ShaderUtilities.ID_StencilComp);
                    m_MaskMaterial.CopyPropertiesFromMaterial(m_fallbackMaterial);
                    m_MaskMaterial.shaderKeywords = m_fallbackMaterial.shaderKeywords;

                    m_MaskMaterial.SetFloat(ShaderUtilities.ID_StencilID, stencilID);
                    m_MaskMaterial.SetFloat(ShaderUtilities.ID_StencilComp, stencilComp);
                }

                if (hasCullModeProperty)
                    m_MaskMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
            }

            m_padding = GetPaddingForMaterial();

            SetVerticesDirty();
            m_ShouldRecalculateStencil = true;
            RecalculateClipping();
            RecalculateMasking();
        }


        private void ON_DRAG_AND_DROP_MATERIAL(GameObject obj, Material currentMaterial, Material newMaterial)
        {
            if (obj == gameObject || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == obj)
            {
                if (!m_isDefaultMaterial) return;

                UnityEditor.Undo.RecordObject(this, "Material Assignment");
                UnityEditor.Undo.RecordObject(canvasRenderer, "Material Assignment");

                SetSharedMaterial(newMaterial);
                m_TextComponent.havePropertiesChanged = true;
            }
        }

        private void ON_SPRITE_ASSET_PROPERTY_CHANGED(bool isChanged, Object obj)
        {
            if (m_TextComponent != null)
            {
                m_TextComponent.havePropertiesChanged = true;
            }
        }

        private void ON_FONT_PROPERTY_CHANGED(bool isChanged, Object fontAsset)
        {
            if (m_fontAsset != null && fontAsset != null && fontAsset.GetInstanceID() == m_fontAsset.GetInstanceID())
            {
                if (m_fallbackMaterial != null)
                {
                    if (TMP_Settings.matchMaterialPreset)
                    {
                        TMP_MaterialManager.ReleaseFallbackMaterial(m_fallbackMaterial);
                        TMP_MaterialManager.CleanupFallbackMaterials();
                    }
                }
            }
        }

        private void ON_TMP_SETTINGS_CHANGED()
        {
        }
#endif

        protected override void OnTransformParentChanged()
        {
            if (!IsActive())
                return;

            m_ShouldRecalculateStencil = true;
            RecalculateClipping();
            RecalculateMasking();
        }


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
                var maskMat = StencilMaterial.Add(mat, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal,
                    ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                mat = m_MaskMaterial;
            }

            return mat;
        }


        public float GetPaddingForMaterial()
        {
            float padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_TextComponent.extraPadding,
                m_TextComponent.isUsingBold);

            return padding;
        }


        public float GetPaddingForMaterial(Material mat)
        {
            float padding = ShaderUtilities.GetPadding(mat, m_TextComponent.extraPadding, m_TextComponent.isUsingBold);

            return padding;
        }


        public void UpdateMeshPadding(bool isExtraPadding, bool isUsingBold)
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, isExtraPadding, isUsingBold);
        }


        public override void SetAllDirty()
        {
        }


        public override void SetVerticesDirty()
        {
        }


        public override void SetLayoutDirty()
        {
        }


        public override void SetMaterialDirty()
        {
            m_materialDirty = true;

            UpdateMaterial();

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }


        public void SetPivotDirty()
        {
            if (!IsActive())
                return;

            rectTransform.pivot = m_TextComponent.rectTransform.pivot;
        }

        private Transform GetRootCanvasTransform()
        {
            if (m_RootCanvasTransform == null)
                m_RootCanvasTransform = m_TextComponent.canvas.rootCanvas.transform;

            return m_RootCanvasTransform;
        }

        private Transform m_RootCanvasTransform;


        public override void Cull(Rect clipRect, bool validRect)
        {
        }


        protected override void UpdateGeometry()
        {
        }


        public override void Rebuild(CanvasUpdate update)
        {
            if (update == CanvasUpdate.PreRender)
            {
                if (!m_materialDirty) return;

                UpdateMaterial();
                m_materialDirty = false;
            }
        }


        public void RefreshMaterial()
        {
            UpdateMaterial();
        }


        protected override void UpdateMaterial()
        {
            if (m_sharedMaterial == null)
                return;

            if (m_sharedMaterial.HasProperty(ShaderUtilities.ShaderTag_CullMode) &&
                textComponent.fontSharedMaterial != null)
            {
                float cullMode = textComponent.fontSharedMaterial.GetFloat(ShaderUtilities.ShaderTag_CullMode);
                m_sharedMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
            }

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);

#if UNITY_EDITOR
            if (m_sharedMaterial != null && gameObject.name != "TMP SubMeshUI [" + m_sharedMaterial.name + "]")
                gameObject.name = "TMP SubMeshUI [" + m_sharedMaterial.name + "]";
#endif
        }


        public override void RecalculateClipping()
        {
            base.RecalculateClipping();
        }


        private Material GetMaterial()
        {
            return m_sharedMaterial;
        }


        private Material GetMaterial(Material mat)
        {
            if (m_material == null || m_material.GetInstanceID() != mat.GetInstanceID())
                m_material = CreateMaterialInstance(mat);

            m_sharedMaterial = m_material;

            m_padding = GetPaddingForMaterial();

            SetVerticesDirty();
            SetMaterialDirty();

            return m_sharedMaterial;
        }


        private Material CreateMaterialInstance(Material source)
        {
            Material mat = new(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";

            return mat;
        }


        private Material GetSharedMaterial()
        {
            return canvasRenderer.GetMaterial();
        }


        private void SetSharedMaterial(Material mat)
        {
            m_sharedMaterial = mat;
            m_Material = m_sharedMaterial;

            m_padding = GetPaddingForMaterial();

            SetMaterialDirty();

#if UNITY_EDITOR
#endif
        }
    }
}