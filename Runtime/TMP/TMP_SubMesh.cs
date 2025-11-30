using UnityEngine;
using System;
using System.Collections;
using Object = UnityEngine.Object;

#pragma warning disable 0109

namespace TMPro
{
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteAlways]
    public class TMP_SubMesh : MonoBehaviour
    {
        /// <summary>
        /// The TMP Font Asset assigned to this sub text object.
        /// </summary>
        public TMP_FontAsset fontAsset
        {
            get { return m_fontAsset; }
            set { m_fontAsset = value; }
        }
        [SerializeField]
        private TMP_FontAsset m_fontAsset;


        /// <summary>
        /// The TMP Sprite Asset assigned to this sub text object.
        /// </summary>
        public TMP_SpriteAsset spriteAsset
        {
            get { return m_spriteAsset; }
            set { m_spriteAsset = value; }
        }
        [SerializeField]
        private TMP_SpriteAsset m_spriteAsset;


        /// <summary>
        /// The material to be assigned to this object. Returns an instance of the material.
        /// </summary>
        public Material material
        {
            get { return GetMaterial(m_sharedMaterial); }

            set
            {
                if (m_sharedMaterial.GetInstanceID() == value.GetInstanceID())
                    return;

                m_sharedMaterial = m_material = value;

                m_padding = GetPaddingForMaterial();

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
        [SerializeField]
        private Material m_material;


        /// <summary>
        /// The material to be assigned to this text object.
        /// </summary>
        public Material sharedMaterial
        {
            get { return m_sharedMaterial; }
            set { SetSharedMaterial(value); }
        }
        [SerializeField]
        private Material m_sharedMaterial;


        /// <summary>
        /// The fallback material created from the properties of the fallback source material.
        /// </summary>
        public Material fallbackMaterial
        {
            get { return m_fallbackMaterial; }
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


        /// <summary>
        /// The source material used by the fallback font
        /// </summary>
        public Material fallbackSourceMaterial
        {
            get { return m_fallbackSourceMaterial; }
            set { m_fallbackSourceMaterial = value; }
        }
        private Material m_fallbackSourceMaterial;


        /// <summary>
        /// Is the text object using the default font asset material.
        /// </summary>
        public bool isDefaultMaterial
        {
            get { return m_isDefaultMaterial; }
            set { m_isDefaultMaterial = value; }
        }
        [SerializeField]
        private bool m_isDefaultMaterial;


        /// <summary>
        /// Padding value resulting for the property settings on the material.
        /// </summary>
        public float padding
        {
            get { return m_padding; }
            set { m_padding = value; }
        }
        [SerializeField]
        private float m_padding;


        /// <summary>
        /// The Mesh Renderer of this text sub object.
        /// </summary>
        public new Renderer renderer
        {
            get { if (m_renderer == null) m_renderer = GetComponent<Renderer>();

                return m_renderer;
            }
        }
        [SerializeField]
        private Renderer m_renderer;


        /// <summary>
        /// The MeshFilter of this text sub object.
        /// </summary>
        public MeshFilter meshFilter
        {
            get
            {
                if (m_meshFilter == null)
                {
                    m_meshFilter = GetComponent<MeshFilter>();

                    if (m_meshFilter == null)
                    {
                        m_meshFilter = gameObject.AddComponent<MeshFilter>();
                        m_meshFilter.hideFlags = HideFlags.HideInInspector | HideFlags.HideAndDontSave;
                    }
                }

                return m_meshFilter;
            }
        }
        private MeshFilter m_meshFilter;


        /// <summary>
        /// The Mesh of this text sub object.
        /// </summary>
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
            set { m_mesh = value; }
        }
        private Mesh m_mesh;

        /// <summary>
        ///
        /// </summary>


        /// <summary>
        /// Reference to the parent Text Component.
        /// </summary>
        public TMP_Text textComponent
        {
            get
            {
                if (m_TextComponent == null)
                    m_TextComponent = GetComponentInParent<TextMeshPro>();

                return m_TextComponent;
            }
        }
        [SerializeField]
        private TextMeshPro m_TextComponent;

        [NonSerialized]
        private bool m_isRegisteredForEvents;


        public static TMP_SubMesh AddSubTextObject(TextMeshPro textComponent, MaterialReference materialReference)
        {
            GameObject go = new();
            go.hideFlags = TMP_Settings.hideSubTextObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave;
            go.transform.SetParent(textComponent.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = textComponent.gameObject.layer;

            #if UNITY_EDITOR
            go.name = materialReference.material == null ? "TMP SubMesh" : "TMP SubMesh [" + materialReference.material.name + "]";
            #endif

            TMP_SubMesh subMesh = go.AddComponent<TMP_SubMesh>();
            subMesh.m_TextComponent = textComponent;
            subMesh.m_fontAsset = materialReference.fontAsset;
            subMesh.m_spriteAsset = materialReference.spriteAsset;
            subMesh.m_isDefaultMaterial = materialReference.isDefaultMaterial;
            subMesh.SetSharedMaterial(materialReference.material);

            subMesh.renderer.sortingLayerID = textComponent.renderer.sortingLayerID;
            subMesh.renderer.sortingOrder = textComponent.renderer.sortingOrder;

            return subMesh;
        }


        private void OnEnable()
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

            meshFilter.sharedMesh = mesh;

            if (m_sharedMaterial != null)
                m_sharedMaterial.SetVector(ShaderUtilities.ID_ClipRect, new(-32767, -32767, 32767, 32767));
        }


        private void OnDisable()
        {
            m_meshFilter.sharedMesh = null;

            if (m_fallbackMaterial != null)
            {
                TMP_MaterialManager.ReleaseFallbackMaterial(m_fallbackMaterial);
                m_fallbackMaterial = null;
            }
        }


        private void OnDestroy()
        {
            if (m_mesh != null) DestroyImmediate(m_mesh);

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
            int fallbackSourceMaterialID = m_fallbackSourceMaterial == null ? 0 : m_fallbackSourceMaterial.GetInstanceID();

            bool hasCullModeProperty = m_sharedMaterial.HasProperty(ShaderUtilities.ShaderTag_CullMode);
            float cullMode = 0;

            if (hasCullModeProperty)
            {
                cullMode = textComponent.fontSharedMaterial.GetFloat(ShaderUtilities.ShaderTag_CullMode);
                m_sharedMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
            }

            if (targetMaterialID != sharedMaterialID)
            {
                if (m_fallbackMaterial != null && fallbackSourceMaterialID == targetMaterialID && TMP_Settings.matchMaterialPreset)
                {
                    TMP_MaterialManager.CopyMaterialPresetProperties(mat, m_fallbackMaterial);

                    if (hasCullModeProperty)
                        m_fallbackMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
                }
                else
                    return;
            }

            m_padding = GetPaddingForMaterial();

            m_TextComponent.havePropertiesChanged = true;
            m_TextComponent.SetVerticesDirty();
        }


        private void ON_DRAG_AND_DROP_MATERIAL(GameObject obj, Material currentMaterial, Material newMaterial)
        {
            if (obj == gameObject || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == obj)
            {
                if (!m_isDefaultMaterial) return;

                if (m_renderer == null) m_renderer = GetComponent<Renderer>();

                UnityEditor.Undo.RecordObject(this, "Material Assignment");
                UnityEditor.Undo.RecordObject(m_renderer, "Material Assignment");

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

        /// <summary>
        /// Event received when the TMP Settings are changed.
        /// </summary>
        private void ON_TMP_SETTINGS_CHANGED()
        {
        }
        #endif


        public void DestroySelf()
        {
            Destroy(gameObject, 1f);
        }

        private Material GetMaterial(Material mat)
        {
            if (m_renderer == null)
                m_renderer = GetComponent<Renderer>();

            if (m_material == null || m_material.GetInstanceID() != mat.GetInstanceID())
                m_material = CreateMaterialInstance(mat);

            m_sharedMaterial = m_material;

            m_padding = GetPaddingForMaterial();

            SetVerticesDirty();
            SetMaterialDirty();

            return m_sharedMaterial;
        }


        /// <summary>
        /// Method used to create an instance of the material
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private Material CreateMaterialInstance(Material source)
        {
            Material mat = new(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";

            return mat;
        }


        /// <summary>
        /// Method returning the shared material assigned to the text object.
        /// </summary>
        /// <returns></returns>
        private Material GetSharedMaterial()
        {
            if (m_renderer == null)
                m_renderer = GetComponent<Renderer>();

            return m_renderer.sharedMaterial;
        }


        /// <summary>
        /// Method to set the shared material.
        /// </summary>
        /// <param name="mat"></param>
        private void SetSharedMaterial(Material mat)
        {
            m_sharedMaterial = mat;

            m_padding = GetPaddingForMaterial();

            SetMaterialDirty();

            #if UNITY_EDITOR
            if (m_sharedMaterial != null)
                gameObject.name = "TMP SubMesh [" + m_sharedMaterial.name + "]";
            #endif
        }


        /// <summary>
        /// Function called when the padding value for the material needs to be re-calculated.
        /// </summary>
        /// <returns></returns>
        public float GetPaddingForMaterial()
        {
            float padding = ShaderUtilities.GetPadding(m_sharedMaterial, m_TextComponent.extraPadding, m_TextComponent.isUsingBold);

            return padding;
        }


        /// <summary>
        /// Function to update the padding values of the object.
        /// </summary>
        /// <param name="isExtraPadding"></param>
        /// <param name="isBold"></param>
        public void UpdateMeshPadding(bool isExtraPadding, bool isUsingBold)
        {
            m_padding = ShaderUtilities.GetPadding(m_sharedMaterial, isExtraPadding, isUsingBold);
        }


        /// <summary>
        ///
        /// </summary>
        public void SetVerticesDirty()
        {
        }


        /// <summary>
        ///
        /// </summary>
        public void SetMaterialDirty()
        {
            UpdateMaterial();
        }


        /// <summary>
        ///
        /// </summary>
        protected void UpdateMaterial()
        {
            if (renderer == null || m_sharedMaterial == null) return;

            m_renderer.sharedMaterial = m_sharedMaterial;

            if (m_sharedMaterial.HasProperty(ShaderUtilities.ShaderTag_CullMode) && textComponent.fontSharedMaterial != null)
            {
                float cullMode = textComponent.fontSharedMaterial.GetFloat(ShaderUtilities.ShaderTag_CullMode);
                m_sharedMaterial.SetFloat(ShaderUtilities.ShaderTag_CullMode, cullMode);
            }

            #if UNITY_EDITOR
            if (m_sharedMaterial != null && gameObject.name != "TMP SubMesh [" + m_sharedMaterial.name + "]")
                gameObject.name = "TMP SubMesh [" + m_sharedMaterial.name + "]";
            #endif
        }

        /// <summary>
        ///
        /// </summary>
    }
}
