using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


namespace TMPro
{
    [AddComponentMenu("UI/TextMeshPro - Input Field", 11)]
        #if UNITY_2023_2_OR_NEWER
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/TextMeshPro/index.html")]
    #else
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2")]
    #endif
    public class TMP_InputField : Selectable,
        IUpdateSelectedHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler,
        ISubmitHandler,
        ICancelHandler,
        ICanvasElement,
        ILayoutElement,
        IScrollHandler
    {
        public enum ContentType
        {
            Standard,
            Autocorrected,
            IntegerNumber,
            DecimalNumber,
            Alphanumeric,
            Name,
            EmailAddress,
            Password,
            Pin,
            Custom
        }

        public enum InputType
        {
            Standard,
            AutoCorrect,
            Password,
        }

        public enum CharacterValidation
        {
            None,
            Digit,
            Integer,
            Decimal,
            Alphanumeric,
            Name,
            Regex,
            EmailAddress,
            CustomValidator
        }

        public enum LineType
        {
            SingleLine,
            MultiLineSubmit,
            MultiLineNewline
        }

        public delegate char OnValidateInput(string text, int charIndex, char addedChar);

        [Serializable]
        public class SubmitEvent : UnityEvent<string> { }

        [Serializable]
        public class OnChangeEvent : UnityEvent<string> { }

        [Serializable]
        public class SelectionEvent : UnityEvent<string> { }

        [Serializable]
        public class TextSelectionEvent : UnityEvent<string, int, int> { }

        [Serializable]
        public class TouchScreenKeyboardEvent : UnityEvent<TouchScreenKeyboard.Status> { }

        protected TouchScreenKeyboard m_SoftKeyboard;
        static private readonly char[] kSeparators = { ' ', '.', ',', '\t', '\r', '\n' };

    #if UNITY_ANDROID
        static private bool s_IsQuestDeviceEvaluated;
    #endif

        static private bool s_IsQuestDevice;

        #region Exposed properties

        protected RectTransform m_RectTransform;

        [SerializeField]
        protected RectTransform m_TextViewport;

        [SerializeField]
        protected TMPText m_TextComponent;

        [SerializeField]
        protected Graphic m_Placeholder;

        [SerializeField]
        protected Scrollbar m_VerticalScrollbar;
        
        private bool m_IsDrivenByLayoutComponents;
        [SerializeField]
        private LayoutGroup m_LayoutGroup;

        private IScrollHandler m_IScrollHandlerParent;

        private float m_ScrollPosition;

        [SerializeField]
        protected float m_ScrollSensitivity = 1.0f;

        [SerializeField]
        private ContentType m_ContentType = ContentType.Standard;

        [SerializeField]
        private InputType m_InputType = InputType.Standard;

        [SerializeField]
        private char m_AsteriskChar = '*';

        [SerializeField]
        private TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;

        [SerializeField]
        private LineType m_LineType = LineType.SingleLine;

        [SerializeField]
        private bool m_HideMobileInput;

        [SerializeField]
        private bool m_HideSoftKeyboard;

        [SerializeField]
        private CharacterValidation m_CharacterValidation = CharacterValidation.None;

        [SerializeField]
        private string m_RegexValue = string.Empty;

        [SerializeField]
        private float m_GlobalPointSize = 14;

        [SerializeField]
        private int m_CharacterLimit;

        [SerializeField]
        private SubmitEvent m_OnEndEdit = new();

        [SerializeField]
        private SubmitEvent m_OnSubmit = new();

        [SerializeField]
        private SelectionEvent m_OnSelect = new();

        [SerializeField]
        private SelectionEvent m_OnDeselect = new();

        [SerializeField]
        private TextSelectionEvent m_OnTextSelection = new();

        [SerializeField]
        private TextSelectionEvent m_OnEndTextSelection = new();

        [SerializeField]
        private OnChangeEvent m_OnValueChanged = new();

        [SerializeField]
        private TouchScreenKeyboardEvent m_OnTouchScreenKeyboardStatusChanged = new();

        [SerializeField]
        private OnValidateInput m_OnValidateInput;

        [SerializeField]
        private Color m_CaretColor = new(50f / 255f, 50f / 255f, 50f / 255f, 1f);

        [SerializeField]
        private bool m_CustomCaretColor;

        [SerializeField]
        private Color m_SelectionColor = new(168f / 255f, 206f / 255f, 255f / 255f, 192f / 255f);


        [SerializeField]
        [TextArea(5, 10)]
        protected string m_Text = string.Empty;

        [SerializeField]
        [Range(0f, 4f)]
        private float m_CaretBlinkRate = 0.85f;

        [SerializeField]
        [Range(1, 5)]
        private int m_CaretWidth = 1;

        [SerializeField]
        private bool m_ReadOnly;

        [SerializeField]
        private bool m_RichText = true;

        #endregion

        protected int m_StringPosition;
        protected int m_StringSelectPosition;
        protected int m_CaretPosition;
        protected int m_CaretSelectPosition;

        private RectTransform caretRectTrans;
        protected UIVertex[] m_CursorVerts;
        private CanvasRenderer m_CachedInputRenderer;
        private Vector2 m_LastPosition;

        [NonSerialized]
        protected Mesh m_Mesh;
        private bool m_AllowInput;
        private bool m_ShouldActivateNextUpdate;
        private bool m_UpdateDrag;
        private bool m_DragPositionOutOfBounds;
        private const float kHScrollSpeed = 0.05f;
        private const float kVScrollSpeed = 0.10f;
        protected bool m_CaretVisible;
        private Coroutine m_BlinkCoroutine;
        private float m_BlinkStartTime;
        private Coroutine m_DragCoroutine;
        private string m_OriginalText = "";
        private bool m_WasCanceled;
        private bool m_HasDoneFocusTransition;
        private WaitForSecondsRealtime m_WaitForSecondsRealtime;
        private bool m_PreventCallback;

        private bool m_TouchKeyboardAllowsInPlaceEditing;

        private bool m_IsTextComponentUpdateRequired;

        private bool m_HasTextBeenRemoved;
        private float m_PointerDownClickStartTime;
        private float m_KeyDownStartTime;
        private float m_DoubleClickDelay = 0.5f;

        private bool m_IsApplePlatform;

        private const string kEmailSpecialCharacters = "!#$%&'*+-/=?^_`{|}~";
        private const string kOculusQuestDeviceModel = "Oculus Quest";

        private BaseInput inputSystem
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        private string compositionString
        {
            get { return inputSystem != null ? inputSystem.compositionString : Input.compositionString; }
        }
        private bool m_IsCompositionActive;
        private bool m_ShouldUpdateIMEWindowPosition;
        private int m_PreviousIMEInsertionLine;

        private int compositionLength
        {
            get
            {
                if (m_ReadOnly)
                    return 0;

                return compositionString.Length;
            }
        }



        protected TMP_InputField()
        {
            SetTextComponentWrapMode();
        }

        protected Mesh mesh
        {
            get
            {
                if (m_Mesh == null)
                    m_Mesh = new();
                return m_Mesh;
            }
        }

        public virtual bool shouldActivateOnSelect
        {
            set
            {
                m_ShouldActivateOnSelect = value;
            }
            get
            {
                return m_ShouldActivateOnSelect && Application.platform != RuntimePlatform.tvOS;
            }
        }

        public bool shouldHideMobileInput
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    #if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
                    #endif
                        return m_HideMobileInput;
                    default:
                        return true;
                }
            }

            set
            {
                switch(Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    #if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
                    #endif
                        SetPropertyUtility.SetStruct(ref m_HideMobileInput, value);
                        break;
                    default:
                        m_HideMobileInput = true;
                        break;
                }
            }
        }

        public bool shouldHideSoftKeyboard
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    #if UNITY_XR_VISIONOS_SUPPORTED
                    case RuntimePlatform.VisionOS:
                    #endif
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
                    #if UNITY_2020_2_OR_NEWER
                    case RuntimePlatform.PS4:
                        #if !(UNITY_2020_2_1 || UNITY_2020_2_2)
                        case RuntimePlatform.PS5:
                        #endif
                    #endif
                    #if UNITY_2019_4_OR_NEWER
                    case RuntimePlatform.GameCoreXboxOne:
                    case RuntimePlatform.GameCoreXboxSeries:
                    #endif
                    case RuntimePlatform.Switch:
                    #if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
                    #endif
                        return m_HideSoftKeyboard;
                    default:
                        return true;
                }
            }

            set
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    #if UNITY_XR_VISIONOS_SUPPORTED
                    case RuntimePlatform.VisionOS:
                    #endif
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
                    #if UNITY_2020_2_OR_NEWER
                    case RuntimePlatform.PS4:
                        #if !(UNITY_2020_2_1 || UNITY_2020_2_2)
                        case RuntimePlatform.PS5:
                        #endif
                    #endif
                    #if UNITY_2019_4_OR_NEWER
                    case RuntimePlatform.GameCoreXboxOne:
                    case RuntimePlatform.GameCoreXboxSeries:
                    #endif
                    case RuntimePlatform.Switch:
                    #if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
                    #endif
                        SetPropertyUtility.SetStruct(ref m_HideSoftKeyboard, value);
                        break;
                    default:
                        m_HideSoftKeyboard = true;
                        break;
                }

                if (m_HideSoftKeyboard && m_SoftKeyboard != null && TouchScreenKeyboard.isSupported && m_SoftKeyboard.active)
                {
                    m_SoftKeyboard.active = false;
                    m_SoftKeyboard = null;
                }
            }
        }

        private bool isKeyboardUsingEvents()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return InPlaceEditing() && m_HideSoftKeyboard;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
                #if UNITY_XR_VISIONOS_SUPPORTED
                case RuntimePlatform.VisionOS:
                #endif
                    return m_HideSoftKeyboard;
                #if UNITY_2020_2_OR_NEWER
                case RuntimePlatform.PS4:
                    #if !(UNITY_2020_2_1 || UNITY_2020_2_2)
                    case RuntimePlatform.PS5:
                    #endif
                #endif
                #if UNITY_2019_4_OR_NEWER
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                #endif
                case RuntimePlatform.Switch:
                    return false;
                #if UNITY_2022_1_OR_NEWER
                case RuntimePlatform.WebGLPlayer:
                    return m_SoftKeyboard == null || !m_SoftKeyboard.active;
                #endif
                default:
                    return true;
            }
        }

        private bool isUWP()
        {
            return Application.platform == RuntimePlatform.WSAPlayerX86 || Application.platform == RuntimePlatform.WSAPlayerX64 || Application.platform == RuntimePlatform.WSAPlayerARM;
        }

        /// <remarks>
        /// Note that null is invalid value  for InputField.text.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public InputField mainInputField;
        ///
        ///     public void Start()
        ///     {
        ///         mainInputField.text = "Enter Text Here...";
        ///     }
        /// }
        /// </code>
        /// </example>
        public string text
        {
            get
            {
                return m_Text;
            }
            set
            {
                SetText(value);
            }
        }

        public void SetTextWithoutNotify(string input)
        {
            SetText(input, false);
        }

        private void SetText(string value, bool sendCallback = true)
        {
            if (text == value)
                return;

            if (value == null)
                value = "";

            value = value.Replace("\0", string.Empty);

            m_Text = value;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SendOnValueChangedAndUpdateLabel();
                return;
            }
            #endif

            if (m_SoftKeyboard != null)
                m_SoftKeyboard.text = m_Text;

            if (m_StringPosition > m_Text.Length)
                m_StringPosition = m_StringSelectPosition = m_Text.Length;
            else if (m_StringSelectPosition > m_Text.Length)
                m_StringSelectPosition = m_Text.Length;

            m_forceRectTransformAdjustment = true;

            m_IsTextComponentUpdateRequired = true;
            UpdateLabel();

            if (sendCallback)
                SendOnValueChanged();
        }


        public bool isFocused
        {
            get { return m_AllowInput; }
        }

        public float caretBlinkRate
        {
            get { return m_CaretBlinkRate; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_CaretBlinkRate, value))
                {
                    if (m_AllowInput)
                        SetCaretActive();
                }
            }
        }

        public int caretWidth { get { return m_CaretWidth; } set { if (SetPropertyUtility.SetStruct(ref m_CaretWidth, value)) MarkGeometryAsDirty(); } }

        public RectTransform textViewport { get { return m_TextViewport; } set { SetPropertyUtility.SetClass(ref m_TextViewport, value); } }

        public TMPText textComponent
        {
            get { return m_TextComponent; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_TextComponent, value))
                {
                    SetTextComponentWrapMode();
                }
            }
        }

        public Graphic placeholder { get { return m_Placeholder; } set { SetPropertyUtility.SetClass(ref m_Placeholder, value); } }

        public Scrollbar verticalScrollbar
        {
            get { return m_VerticalScrollbar; }
            set
            {
                if (m_VerticalScrollbar != null)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChange);

                SetPropertyUtility.SetClass(ref m_VerticalScrollbar, value);

                if (m_VerticalScrollbar)
                {
                    m_VerticalScrollbar.onValueChanged.AddListener(OnScrollbarValueChange);

                }
            }
        }

        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { if (SetPropertyUtility.SetStruct(ref m_ScrollSensitivity, value)) MarkGeometryAsDirty(); } }

        public Color caretColor { get { return customCaretColor ? m_CaretColor : textComponent.color; } set { if (SetPropertyUtility.SetColor(ref m_CaretColor, value)) MarkGeometryAsDirty(); } }

        public bool customCaretColor { get { return m_CustomCaretColor; } set { if (m_CustomCaretColor != value) { m_CustomCaretColor = value; MarkGeometryAsDirty(); } } }

        public Color selectionColor { get { return m_SelectionColor; } set { if (SetPropertyUtility.SetColor(ref m_SelectionColor, value)) MarkGeometryAsDirty(); } }

        public SubmitEvent onEndEdit { get { return m_OnEndEdit; } set { SetPropertyUtility.SetClass(ref m_OnEndEdit, value); } }

        public SubmitEvent onSubmit { get { return m_OnSubmit; } set { SetPropertyUtility.SetClass(ref m_OnSubmit, value); } }

        public SelectionEvent onSelect { get { return m_OnSelect; } set { SetPropertyUtility.SetClass(ref m_OnSelect, value); } }

        public SelectionEvent onDeselect { get { return m_OnDeselect; } set { SetPropertyUtility.SetClass(ref m_OnDeselect, value); } }

        public TextSelectionEvent onTextSelection { get { return m_OnTextSelection; } set { SetPropertyUtility.SetClass(ref m_OnTextSelection, value); } }

        public TextSelectionEvent onEndTextSelection { get { return m_OnEndTextSelection; } set { SetPropertyUtility.SetClass(ref m_OnEndTextSelection, value); } }

        public OnChangeEvent onValueChanged { get { return m_OnValueChanged; } set { SetPropertyUtility.SetClass(ref m_OnValueChanged, value); } }

        public TouchScreenKeyboardEvent onTouchScreenKeyboardStatusChanged { get { return m_OnTouchScreenKeyboardStatusChanged; } set { SetPropertyUtility.SetClass(ref m_OnTouchScreenKeyboardStatusChanged, value); } }

        public OnValidateInput onValidateInput { get { return m_OnValidateInput; } set { SetPropertyUtility.SetClass(ref m_OnValidateInput, value); } }

        public int characterLimit
        {
            get { return m_CharacterLimit; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_CharacterLimit, Math.Max(0, value)))
                {
                    UpdateLabel();
                    if (m_SoftKeyboard != null)
                        m_SoftKeyboard.characterLimit = value;
                }
            }
        }

        public float pointSize
        {
            get { return m_GlobalPointSize; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_GlobalPointSize, Math.Max(0, value)))
                {
                    SetGlobalPointSize(m_GlobalPointSize);
                    UpdateLabel();
                }
            }
        }

        public TMP_FontAsset fontAsset
        {
            get { return m_GlobalFontAsset; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_GlobalFontAsset, value))
                {
                    SetGlobalFontAsset(m_GlobalFontAsset);
                    UpdateLabel();
                }
            }
        }
        [SerializeField]
        protected TMP_FontAsset m_GlobalFontAsset;

        public bool onFocusSelectAll
        {
            get { return m_OnFocusSelectAll; }
            set { m_OnFocusSelectAll = value; }
        }
        [SerializeField]
        protected bool m_OnFocusSelectAll = true;
        protected bool m_isSelectAll;

        public bool resetOnDeActivation
        {
            get { return m_ResetOnDeActivation; }
            set { m_ResetOnDeActivation = value; }
        }
        [SerializeField]
        protected bool m_ResetOnDeActivation = true;
        private bool m_SelectionStillActive;
        private bool m_ReleaseSelection;
        private KeyCode m_LastKeyCode;

        private GameObject m_PreviouslySelectedObject;

        public bool keepTextSelectionVisible
        {
            get { return m_KeepTextSelectionVisible; }
            set { m_KeepTextSelectionVisible = value; }
        }

        [SerializeField]
        private bool m_KeepTextSelectionVisible;

        public bool restoreOriginalTextOnEscape
        {
            get { return m_RestoreOriginalTextOnEscape; }
            set { m_RestoreOriginalTextOnEscape = value; }
        }
        [SerializeField]
        private bool m_RestoreOriginalTextOnEscape = true;

        public bool isRichTextEditingAllowed
        {
            get { return m_isRichTextEditingAllowed; }
            set { m_isRichTextEditingAllowed = value; }
        }
        [SerializeField]
        protected bool m_isRichTextEditingAllowed;


        public ContentType contentType { get { return m_ContentType; } set { if (SetPropertyUtility.SetStruct(ref m_ContentType, value)) EnforceContentType(); } }

        public LineType lineType
        {
            get { return m_LineType; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_LineType, value))
                {
                    SetToCustomIfContentTypeIsNot(ContentType.Standard, ContentType.Autocorrected);
                    SetTextComponentWrapMode();
                }
            }
        }

        public int lineLimit
        {
            get { return m_LineLimit; }
            set
            {
                if (m_LineType == LineType.SingleLine)
                    m_LineLimit = 1;
                else
                    SetPropertyUtility.SetStruct(ref m_LineLimit, value);

            }
        }
        [SerializeField]
        protected int m_LineLimit;

        public InputType inputType { get { return m_InputType; } set { if (SetPropertyUtility.SetStruct(ref m_InputType, value)) SetToCustom(); } }

        public TouchScreenKeyboard touchScreenKeyboard { get { return m_SoftKeyboard; } }

        public TouchScreenKeyboardType keyboardType
        {
            get { return m_KeyboardType; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_KeyboardType, value))
                    SetToCustom();
            }
        }

        public bool isAlert;

        public CharacterValidation characterValidation { get { return m_CharacterValidation; } set { if (SetPropertyUtility.SetStruct(ref m_CharacterValidation, value)) SetToCustom(); } }

        public TMP_InputValidator inputValidator
        {
            get { return m_InputValidator; }
            set {  if (SetPropertyUtility.SetClass(ref m_InputValidator, value)) SetToCustom(CharacterValidation.CustomValidator); }
        }
        [SerializeField]
        protected TMP_InputValidator m_InputValidator;

        public bool readOnly { get { return m_ReadOnly; } set { m_ReadOnly = value; } }

        [SerializeField]
        private bool m_ShouldActivateOnSelect = true;

        public bool richText { get { return m_RichText; } set { m_RichText = value; SetTextComponentRichTextMode(); } }

        public bool multiLine { get { return m_LineType == LineType.MultiLineNewline || lineType == LineType.MultiLineSubmit; } }
        public char asteriskChar { get { return m_AsteriskChar; } set { if (SetPropertyUtility.SetStruct(ref m_AsteriskChar, value)) UpdateLabel(); } }
        public bool wasCanceled { get { return m_WasCanceled; } }


        protected void ClampStringPos(ref int pos)
        {
            if (pos <= 0)
                pos = 0;
            else if (pos > text.Length)
                pos = text.Length;
        }

        protected void ClampCaretPos(ref int pos)
        {
            if (pos > m_TextComponent.textInfo.characterCount - 1)
                pos = m_TextComponent.textInfo.characterCount - 1;

            if (pos <= 0)
                pos = 0;
        }

        private int ClampArrayIndex(int index)
        {
            if (index < 0)
                return 0;

            return index;
        }


        protected int caretPositionInternal { get { return m_CaretPosition + compositionLength; } set { m_CaretPosition = value; ClampCaretPos(ref m_CaretPosition); } }
        protected int stringPositionInternal { get { return m_StringPosition + compositionLength; } set { m_StringPosition = value; ClampStringPos(ref m_StringPosition); } }

        protected int caretSelectPositionInternal { get { return m_CaretSelectPosition + compositionLength; } set { m_CaretSelectPosition = value; ClampCaretPos(ref m_CaretSelectPosition); } }
        protected int stringSelectPositionInternal { get { return m_StringSelectPosition + compositionLength; } set { m_StringSelectPosition = value; ClampStringPos(ref m_StringSelectPosition); } }

        private bool hasSelection { get { return stringPositionInternal != stringSelectPositionInternal; } }
        private bool m_isSelected;
        private bool m_IsStringPositionDirty;
        private bool m_IsCaretPositionDirty;
        private bool m_forceRectTransformAdjustment;

        private bool m_IsKeyboardBeingClosedInHoloLens;

        public int caretPosition
        {
            get => caretSelectPositionInternal;
            set { selectionAnchorPosition = value; selectionFocusPosition = value; UpdateStringIndexFromCaretPosition(); }
        }

        public int selectionAnchorPosition
        {
            get
            {
                return caretPositionInternal;
            }

            set
            {
                if (compositionLength != 0)
                    return;

                caretPositionInternal = value;
                m_IsStringPositionDirty = true;
            }
        }

        public int selectionFocusPosition
        {
            get
            {
                return caretSelectPositionInternal;
            }
            set
            {
                if (compositionLength != 0)
                    return;

                caretSelectPositionInternal = value;
                m_IsStringPositionDirty = true;
            }
        }


        public int stringPosition
        {
            get => stringSelectPositionInternal;
            set { selectionStringAnchorPosition = value; selectionStringFocusPosition = value; UpdateCaretPositionFromStringIndex(); }
        }


        public int selectionStringAnchorPosition
        {
            get
            {
                return stringPositionInternal;
            }

            set
            {
                if (compositionLength != 0)
                    return;

                stringPositionInternal = value;
                m_IsCaretPositionDirty = true;
            }
        }


        public int selectionStringFocusPosition
        {
            get
            {
                return stringSelectPositionInternal;
            }
            set
            {
                if (compositionLength != 0)
                    return;

                stringSelectPositionInternal = value;
                m_IsCaretPositionDirty = true;
            }
        }


        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            EnforceContentType();

            m_CharacterLimit = Math.Max(0, m_CharacterLimit);

            if (!IsActive())
                return;

            SetTextComponentRichTextMode();

            UpdateLabel();

            if (m_AllowInput)
                SetCaretActive();
        }
        #endif

#if UNITY_ANDROID
        protected override void Awake()
        {
            base.Awake();

            if (s_IsQuestDeviceEvaluated)
                return;

            s_IsQuestDevice = SystemInfo.deviceModel == kOculusQuestDeviceModel;
            s_IsQuestDeviceEvaluated = true;
        }
    	#endif


        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_Text == null)
                m_Text = string.Empty;

            m_IsApplePlatform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX || SystemInfo.operatingSystem.Contains("iOS") || SystemInfo.operatingSystem.Contains("tvOS");

            ILayoutController layoutController = GetComponent<ILayoutController>();

            if (layoutController != null)
            {
                m_IsDrivenByLayoutComponents = true;
                m_LayoutGroup = GetComponent<LayoutGroup>();
            }
            else
                m_IsDrivenByLayoutComponents = false;

            if (Application.isPlaying)
            {
                if (m_CachedInputRenderer == null && m_TextComponent != null)
                {
                    GameObject go = new("Caret", typeof(TMP_SelectionCaret));

                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(m_TextComponent.transform.parent);
                    go.transform.SetAsFirstSibling();
                    go.layer = gameObject.layer;

                    caretRectTrans = go.GetComponent<RectTransform>();
                    m_CachedInputRenderer = go.GetComponent<CanvasRenderer>();
                    m_CachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);

                    go.AddComponent<LayoutElement>().ignoreLayout = true;

                    AssignPositioningIfNeeded();
                }
            }

            m_RectTransform = GetComponent<RectTransform>();

            IScrollHandler[] scrollHandlers = GetComponentsInParent<IScrollHandler>();
            if (scrollHandlers.Length > 1)
                m_IScrollHandlerParent = scrollHandlers[1] as ScrollRect;

            if (m_TextViewport != null)
            {
                UpdateMaskRegions();
            }

            if (m_CachedInputRenderer != null)
                m_CachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);

            if (m_TextComponent != null)
            {
                m_TextComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
                m_TextComponent.RegisterDirtyVerticesCallback(UpdateLabel);

                if (m_VerticalScrollbar != null)
                {
                    m_VerticalScrollbar.onValueChanged.AddListener(OnScrollbarValueChange);
                }

                UpdateLabel();
            }

            #if UNITY_2019_1_OR_NEWER
            m_TouchKeyboardAllowsInPlaceEditing = TouchScreenKeyboard.isInPlaceEditingAllowed;
            #endif

            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
        }

        protected override void OnDisable()
        {
            m_BlinkCoroutine = null;

            DeactivateInputField();
            if (m_TextComponent != null)
            {
                m_TextComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
                m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);

                if (m_VerticalScrollbar != null)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChange);

            }
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_CachedInputRenderer != null)
                m_CachedInputRenderer.Clear();

            if (m_Mesh != null)
                DestroyImmediate(m_Mesh);

            m_Mesh = null;

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);

            base.OnDisable();
        }


        /// <param name="obj"></param>
        private void ON_TEXT_CHANGED(UnityEngine.Object obj)
        {
            bool isThisObject = obj == m_TextComponent;

            if (isThisObject && !m_IsStringPositionDirty)
            {
                if (Application.isPlaying && compositionLength == 0)
                {
                    UpdateCaretPositionFromStringIndex();

                    #if TMP_DEBUG_MODE
                    Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
                    #endif
                }

                if (m_VerticalScrollbar)
                    UpdateScrollbar();
            }
        }


        private IEnumerator CaretBlink()
        {
            m_CaretVisible = true;
            yield return null;

            while ((isFocused || m_SelectionStillActive) && m_CaretBlinkRate > 0)
            {
                float blinkPeriod = 1f / m_CaretBlinkRate;

                bool blinkState = (Time.unscaledTime - m_BlinkStartTime) % blinkPeriod < blinkPeriod / 2;
                if (m_CaretVisible != blinkState)
                {
                    m_CaretVisible = blinkState;
                    if (!hasSelection)
                        MarkGeometryAsDirty();
                }

                yield return null;
            }
            m_BlinkCoroutine = null;
        }

        private void SetCaretVisible()
        {
            if (!m_AllowInput)
                return;

            m_CaretVisible = true;
            m_BlinkStartTime = Time.unscaledTime;
            SetCaretActive();
        }

        private void SetCaretActive()
        {
            if (!m_AllowInput)
                return;

            if (m_CaretBlinkRate > 0.0f)
            {
                if (m_BlinkCoroutine == null)
                    m_BlinkCoroutine = StartCoroutine(CaretBlink());
            }
            else
            {
                m_CaretVisible = true;
            }
        }

        protected void OnFocus()
        {
            if (m_OnFocusSelectAll)
                SelectAll();
        }

        protected void SelectAll()
        {
            m_isSelectAll = true;
            stringPositionInternal = text.Length;
            stringSelectPositionInternal = 0;
        }

        /// <param name="shift"></param>
        public void MoveTextEnd(bool shift)
        {
            if (m_isRichTextEditingAllowed)
            {
                int position = text.Length;

                if (shift)
                {
                    stringSelectPositionInternal = position;
                }
                else
                {
                    stringPositionInternal = position;
                    stringSelectPositionInternal = stringPositionInternal;
                }
            }
            else
            {
                int position = m_TextComponent.textInfo.characterCount - 1;

                if (shift)
                {
                    caretSelectPositionInternal = position;
                    stringSelectPositionInternal = GetStringIndexFromCaretPosition(position);
                }
                else
                {
                    caretPositionInternal = caretSelectPositionInternal = position;
                    stringSelectPositionInternal = stringPositionInternal = GetStringIndexFromCaretPosition(position);
                }
            }

            UpdateLabel();
        }

        /// <param name="shift"></param>
        public void MoveTextStart(bool shift)
        {
            if (m_isRichTextEditingAllowed)
            {
                int position = 0;

                if (shift)
                {
                    stringSelectPositionInternal = position;
                }
                else
                {
                    stringPositionInternal = position;
                    stringSelectPositionInternal = stringPositionInternal;
                }
            }
            else
            {
                int position = 0;

                if (shift)
                {
                    caretSelectPositionInternal = position;
                    stringSelectPositionInternal = GetStringIndexFromCaretPosition(position);
                }
                else
                {
                    caretPositionInternal = caretSelectPositionInternal = position;
                    stringSelectPositionInternal = stringPositionInternal = GetStringIndexFromCaretPosition(position);
                }
            }

            UpdateLabel();
        }


        /// <param name="shift"></param>
        public void MoveToEndOfLine(bool shift, bool ctrl)
        {
            int currentLine = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;

            int characterIndex = ctrl ? m_TextComponent.textInfo.characterCount - 1 : m_TextComponent.textInfo.lineInfo[currentLine].lastCharacterIndex;

            int position = m_TextComponent.textInfo.characterInfo[characterIndex].index;

            if (shift)
            {
                stringSelectPositionInternal = position;

                caretSelectPositionInternal = characterIndex;
            }
            else
            {
                stringPositionInternal = position;
                stringSelectPositionInternal = stringPositionInternal;

                caretSelectPositionInternal = caretPositionInternal = characterIndex;
            }

            UpdateLabel();
        }

        /// <param name="shift"></param>
        public void MoveToStartOfLine(bool shift, bool ctrl)
        {
            int currentLine = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;

            int characterIndex = ctrl ? 0 : m_TextComponent.textInfo.lineInfo[currentLine].firstCharacterIndex;

            int position = 0;
            if (characterIndex > 0)
                position = m_TextComponent.textInfo.characterInfo[characterIndex - 1].index + m_TextComponent.textInfo.characterInfo[characterIndex - 1].stringLength;

            if (shift)
            {
                stringSelectPositionInternal = position;

                caretSelectPositionInternal = characterIndex;
            }
            else
            {
                stringPositionInternal = position;
                stringSelectPositionInternal = stringPositionInternal;

                caretSelectPositionInternal = caretPositionInternal = characterIndex;
            }

            UpdateLabel();
        }


        private static string clipboard
        {
            get
            {
                return GUIUtility.systemCopyBuffer;
            }
            set
            {
                GUIUtility.systemCopyBuffer = value;
            }
        }

        private bool InPlaceEditing()
        {
            if (m_TouchKeyboardAllowsInPlaceEditing)
                return true;

            if (isUWP())
                return !TouchScreenKeyboard.isSupported;

            if (TouchScreenKeyboard.isSupported && shouldHideSoftKeyboard)
                return true;

            if (TouchScreenKeyboard.isSupported && !shouldHideSoftKeyboard && !shouldHideMobileInput)
                return false;

            return true;
        }

        private bool InPlaceEditingChanged()
        {
                return !s_IsQuestDevice && m_TouchKeyboardAllowsInPlaceEditing != TouchScreenKeyboard.isInPlaceEditingAllowed;
        }

        private bool TouchScreenKeyboardShouldBeUsed()
        {
            RuntimePlatform platform = Application.platform;
            switch (platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.WebGLPlayer:
                    if (s_IsQuestDevice)
                        return TouchScreenKeyboard.isSupported;

                    return !TouchScreenKeyboard.isInPlaceEditingAllowed;
                default:
                    return TouchScreenKeyboard.isSupported;
            }
        }

        private void UpdateKeyboardStringPosition()
        {
            if (m_HideMobileInput && m_SoftKeyboard != null && m_SoftKeyboard.canSetSelection &&
                (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS))
            {
                var selectionStart = Mathf.Min(stringSelectPositionInternal, stringPositionInternal);
                var selectionLength = Mathf.Abs(stringSelectPositionInternal - stringPositionInternal);
                m_SoftKeyboard.selection = new(selectionStart, selectionLength);
            }
        }

        private void UpdateStringPositionFromKeyboard()
        {
            var selectionRange = m_SoftKeyboard.selection;

            var selectionStart = selectionRange.start;
            var selectionEnd = selectionRange.end;

            var stringPositionChanged = false;

            if (stringPositionInternal != selectionStart)
            {
                stringPositionChanged = true;
                stringPositionInternal = selectionStart;

                caretPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
            }

            if (stringSelectPositionInternal != selectionEnd)
            {
                stringSelectPositionInternal = selectionEnd;
                stringPositionChanged = true;

                caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            }

            if (stringPositionChanged)
            {
                m_BlinkStartTime = Time.unscaledTime;

                UpdateLabel();
            }
        }

        protected virtual void LateUpdate()
        {
            if (m_ShouldActivateNextUpdate)
            {
                if (!isFocused)
                {
                    ActivateInputFieldInternal();
                    m_ShouldActivateNextUpdate = false;
                    return;
                }

                m_ShouldActivateNextUpdate = false;
            }

            if (isFocused && InPlaceEditingChanged())
                DeactivateInputField();

            if (!isFocused && m_SelectionStillActive)
            {
                GameObject selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

                if (selectedObject == null && m_ResetOnDeActivation)
                {
                    ReleaseSelection();
                    return;
                }

                if (selectedObject != null && selectedObject != gameObject)
                {
                    if (selectedObject == m_PreviouslySelectedObject)
                        return;

                    m_PreviouslySelectedObject = selectedObject;

                    if (m_VerticalScrollbar && selectedObject == m_VerticalScrollbar.gameObject)
                    {
                        return;
                    }

                    if (m_ResetOnDeActivation)
                    {
                        ReleaseSelection();
                        return;
                    }

                    if (!m_KeepTextSelectionVisible && selectedObject.GetComponent<TMP_InputField>() != null)
                        ReleaseSelection();

                    return;
                }

                #if ENABLE_INPUT_SYSTEM
                if (m_ProcessingEvent != null && m_ProcessingEvent.rawType == EventType.MouseDown && m_ProcessingEvent.button == 0)
                {
                    bool isDoubleClick = false;
                    float timeStamp = Time.unscaledTime;

                    if (m_KeyDownStartTime + m_DoubleClickDelay > timeStamp)
                        isDoubleClick = true;

                    m_KeyDownStartTime = timeStamp;

                    if (isDoubleClick)
                    {

                        ReleaseSelection();

                        return;
                    }
                }
                #else
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    bool isDoubleClick = false;
                    float timeStamp = Time.unscaledTime;

                    if (m_KeyDownStartTime + m_DoubleClickDelay > timeStamp)
                        isDoubleClick = true;

                    m_KeyDownStartTime = timeStamp;

                    if (isDoubleClick)
                    {
                        ReleaseSelection();

                        return;
                    }
                }
                #endif
            }

            UpdateMaskRegions();

            if (InPlaceEditing() && isKeyboardUsingEvents() || !isFocused)
            {
                return;
            }

            AssignPositioningIfNeeded();

            if (m_SoftKeyboard == null || m_SoftKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                if (m_SoftKeyboard != null)
                {
                    if (!m_ReadOnly)
                        text = m_SoftKeyboard.text;

                    TouchScreenKeyboard.Status status = m_SoftKeyboard.status;

                    if (m_LastKeyCode != KeyCode.Return && status == TouchScreenKeyboard.Status.Done && isUWP())
					{
                        status = TouchScreenKeyboard.Status.Canceled;
                        m_IsKeyboardBeingClosedInHoloLens = true;
					}

                    switch (status)
                    {
                        case TouchScreenKeyboard.Status.LostFocus:
                            SendTouchScreenKeyboardStatusChanged();
                            break;
                        case TouchScreenKeyboard.Status.Canceled:
                            m_ReleaseSelection = true;
                            m_WasCanceled = true;
                            SendTouchScreenKeyboardStatusChanged();
                            break;
                        case TouchScreenKeyboard.Status.Done:
                            m_ReleaseSelection = true;
                            SendTouchScreenKeyboardStatusChanged();
                            OnSubmit(null);
                            break;
                    }
                }

                OnDeselect(null);
                return;
            }

            string val = m_SoftKeyboard.text;

            if (m_Text != val)
            {
                if (m_ReadOnly)
                {
                    m_SoftKeyboard.text = m_Text;
                }
                else
                {
                    m_Text = "";

                    for (int i = 0; i < val.Length; ++i)
                    {
                        char c = val[i];
						bool hasValidateUpdatedText = false;

                        if (c == '\r' || c == 3)
                            c = '\n';

                        if (onValidateInput != null)
                            c = onValidateInput(m_Text, m_Text.Length, c);
                        else if (characterValidation != CharacterValidation.None)
						{
							string textBeforeValidate = m_Text;
                            c = Validate(m_Text, m_Text.Length, c);
                            hasValidateUpdatedText = textBeforeValidate != m_Text;
						}

                        if (lineType != LineType.MultiLineNewline && c == '\n')
                        {
                            UpdateLabel();

                            OnSubmit(null);
                            OnDeselect(null);
                            return;
                        }

                        if (c != 0 && (characterValidation != CharacterValidation.CustomValidator || !hasValidateUpdatedText))
                            m_Text += c;
                    }

                    if (characterLimit > 0 && m_Text.Length > characterLimit)
                        m_Text = m_Text.Substring(0, characterLimit);

                    UpdateStringPositionFromKeyboard();

                    if (m_Text != val)
                        m_SoftKeyboard.text = m_Text;

                    SendOnValueChangedAndUpdateLabel();
                }
            }
            else if (m_HideMobileInput && m_SoftKeyboard != null && m_SoftKeyboard.canSetSelection &&
                     Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.tvOS)
            {
                var selectionStart = Mathf.Min(stringSelectPositionInternal, stringPositionInternal);
                var selectionLength = Mathf.Abs(stringSelectPositionInternal - stringPositionInternal);
                m_SoftKeyboard.selection = new(selectionStart, selectionLength);
            }
            else if (m_HideMobileInput && Application.platform == RuntimePlatform.Android ||
                     m_SoftKeyboard.canSetSelection && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS))
            {
                UpdateStringPositionFromKeyboard();
            }

            if (m_SoftKeyboard != null && m_SoftKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                if (m_SoftKeyboard.status == TouchScreenKeyboard.Status.Canceled)
                    m_WasCanceled = true;

                OnDeselect(null);
            }
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() &&
                   IsInteractable() &&
                   eventData.button == PointerEventData.InputButton.Left &&
                   m_TextComponent != null &&
                   (m_SoftKeyboard == null || shouldHideSoftKeyboard || shouldHideMobileInput);
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            m_UpdateDrag = true;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            int insertionIndex = TMP_TextUtilities.GetCursorIndexFromPosition(m_TextComponent, eventData.position, eventData.pressEventCamera, out var insertionSide);

            if (m_isRichTextEditingAllowed)
            {
                if (insertionSide == CaretPosition.Left)
                {
                    stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                }
                else if (insertionSide == CaretPosition.Right)
                {
                    stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                }
            }
            else
            {
                if (insertionSide == CaretPosition.Left)
                {
                    stringSelectPositionInternal = insertionIndex == 0
                        ? m_TextComponent.textInfo.characterInfo[0].index
                        : m_TextComponent.textInfo.characterInfo[insertionIndex - 1].index + m_TextComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                }
                else if (insertionSide == CaretPosition.Right)
                {
                    stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                }
            }

            caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);

            MarkGeometryAsDirty();

            m_DragPositionOutOfBounds = !RectTransformUtility.RectangleContainsScreenPoint(textViewport, eventData.position, eventData.pressEventCamera);
            if (m_DragPositionOutOfBounds && m_DragCoroutine == null)
                m_DragCoroutine = StartCoroutine(MouseDragOutsideRect(eventData));

            UpdateKeyboardStringPosition();
            eventData.Use();

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        private IEnumerator MouseDragOutsideRect(PointerEventData eventData)
        {
            while (m_UpdateDrag && m_DragPositionOutOfBounds)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(textViewport, eventData.position, eventData.pressEventCamera, out var localMousePos);

                Rect rect = textViewport.rect;

                if (multiLine)
                {
                    if (localMousePos.y > rect.yMax)
                        MoveUp(true, true);
                    else if (localMousePos.y < rect.yMin)
                        MoveDown(true, true);
                }
                else
                {
                    if (localMousePos.x < rect.xMin)
                        MoveLeft(true, false);
                    else if (localMousePos.x > rect.xMax)
                        MoveRight(true, false);
                }

                UpdateLabel();

                float delay = multiLine ? kVScrollSpeed : kHScrollSpeed;

                if (m_WaitForSecondsRealtime == null)
                    m_WaitForSecondsRealtime = new(delay);
                else
                    m_WaitForSecondsRealtime.waitTime = delay;

                yield return m_WaitForSecondsRealtime;
            }
            m_DragCoroutine = null;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            m_UpdateDrag = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            bool hadFocusBefore = m_AllowInput;
            base.OnPointerDown(eventData);

            if (!InPlaceEditing())
            {
                if (m_SoftKeyboard == null || !m_SoftKeyboard.active)
                {
                    OnSelect(eventData);
                    return;
                }
            }

            #if ENABLE_INPUT_SYSTEM
            Event.PopEvent(m_ProcessingEvent);
            bool shift = m_ProcessingEvent != null && (m_ProcessingEvent.modifiers & EventModifiers.Shift) != 0;
            #else
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            #endif

            bool isDoubleClick = false;
            float timeStamp = Time.unscaledTime;

            if (m_PointerDownClickStartTime + m_DoubleClickDelay > timeStamp)
                isDoubleClick = true;

            m_PointerDownClickStartTime = timeStamp;

            if (hadFocusBefore || !m_OnFocusSelectAll)
            {
                int insertionIndex = TMP_TextUtilities.GetCursorIndexFromPosition(m_TextComponent, eventData.position, eventData.pressEventCamera, out var insertionSide);

                if (shift)
                {
                    if (m_isRichTextEditingAllowed)
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                    else
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringSelectPositionInternal = insertionIndex == 0
                                ? m_TextComponent.textInfo.characterInfo[0].index
                                : m_TextComponent.textInfo.characterInfo[insertionIndex - 1].index + m_TextComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                }
                else
                {
                    if (m_isRichTextEditingAllowed)
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringPositionInternal = stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringPositionInternal = stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                    else
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringPositionInternal = stringSelectPositionInternal = insertionIndex == 0
                                ? m_TextComponent.textInfo.characterInfo[0].index
                                : m_TextComponent.textInfo.characterInfo[insertionIndex - 1].index + m_TextComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringPositionInternal = stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                }


                if (isDoubleClick)
                {
                    int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextComponent, eventData.position, eventData.pressEventCamera);

                    if (wordIndex != -1)
                    {
                        caretPositionInternal = m_TextComponent.textInfo.wordInfo[wordIndex].firstCharacterIndex;
                        caretSelectPositionInternal = m_TextComponent.textInfo.wordInfo[wordIndex].lastCharacterIndex + 1;

                        stringPositionInternal = m_TextComponent.textInfo.characterInfo[caretPositionInternal].index;
                        stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].stringLength;
                    }
                    else
                    {
                        caretPositionInternal = insertionIndex;
                        caretSelectPositionInternal = caretPositionInternal + 1;

                        stringPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                        stringSelectPositionInternal = stringPositionInternal + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                    }
                }
                else
                {
                    caretPositionInternal = caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
                }

                m_isSelectAll = false;
            }

            UpdateLabel();
            UpdateKeyboardStringPosition();
            eventData.Use();

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        protected enum EditState
        {
            Continue,
            Finish
        }

        protected EditState KeyPressed(Event evt)
        {
            var currentEventModifiers = evt.modifiers;
            bool ctrl = m_IsApplePlatform ? (currentEventModifiers & EventModifiers.Command) != 0 : (currentEventModifiers & EventModifiers.Control) != 0;
            bool shift = (currentEventModifiers & EventModifiers.Shift) != 0;
            bool alt = (currentEventModifiers & EventModifiers.Alt) != 0;
            bool ctrlOnly = ctrl && !alt && !shift;
            m_LastKeyCode = evt.keyCode;

            switch (evt.keyCode)
            {
                case KeyCode.Backspace:
                    {
                        Backspace();
                        return EditState.Continue;
                    }

                case KeyCode.Delete:
                    {
                        DeleteKey();
                        return EditState.Continue;
                    }

                case KeyCode.Home:
                    {
                        MoveToStartOfLine(shift, ctrl);
                        return EditState.Continue;
                    }

                case KeyCode.End:
                    {
                        MoveToEndOfLine(shift, ctrl);
                        return EditState.Continue;
                    }

                case KeyCode.A:
                    {
                        if (ctrlOnly)
                        {
                            SelectAll();
                            return EditState.Continue;
                        }
                        break;
                    }

                case KeyCode.C:
                    {
                        if (ctrlOnly)
                        {
                            if (inputType != InputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            return EditState.Continue;
                        }
                        break;
                    }

                case KeyCode.V:
                    {
                        if (ctrlOnly)
                        {
                            Append(clipboard);
                            return EditState.Continue;
                        }
                        break;
                    }

                case KeyCode.X:
                    {
                        if (ctrlOnly)
                        {
                            if (inputType != InputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            Delete();
                            UpdateTouchKeyboardFromEditChanges();
                            SendOnValueChangedAndUpdateLabel();
                            return EditState.Continue;
                        }
                        break;
                    }

                case KeyCode.LeftArrow:
                    {
                        MoveLeft(shift, ctrl);
                        return EditState.Continue;
                    }

                case KeyCode.RightArrow:
                    {
                        MoveRight(shift, ctrl);
                        return EditState.Continue;
                    }

                case KeyCode.UpArrow:
                    {
                        MoveUp(shift);
                        return EditState.Continue;
                    }

                case KeyCode.DownArrow:
                    {
                        MoveDown(shift);
                        return EditState.Continue;
                    }

                case KeyCode.PageUp:
                    {
                        MovePageUp(shift);
                        return EditState.Continue;
                    }

                case KeyCode.PageDown:
                    {
                        MovePageDown(shift);
                        return EditState.Continue;
                    }

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    {
                        if (lineType != LineType.MultiLineNewline)
                        {
                            m_ReleaseSelection = true;
                            return EditState.Finish;
                        }
                        else
                        {
                            TMP_TextInfo textInfo = m_TextComponent.textInfo;

                            if (m_LineLimit > 0 && textInfo != null && textInfo.lineCount >= m_LineLimit)
                            {
                                m_ReleaseSelection = true;
                                return EditState.Finish;
                            }
                        }
                        break;
                    }

                case KeyCode.Escape:
                    {
                        m_ReleaseSelection = true;
                        m_WasCanceled = true;
                        return EditState.Finish;
                    }
            }

            char c = evt.character;

            if (!multiLine && (c == '\t' || c == '\r' || c == '\n'))
                return EditState.Continue;

            if (c == '\r' || c == 3)
                c = '\n';

            if (shift && c == '\n')
                c = '\v';

            if (IsValidChar(c))
            {
                Append(c);
            }

            if (c == 0)
            {
                if (compositionLength > 0)
                {
                    UpdateLabel();
                }
            }
            return EditState.Continue;
        }

        protected virtual bool IsValidChar(char c)
        {
            if (c == 127)
                return false;

            if (c == '\t' || c == '\n')
                return true;

            if (c < 32)
                return false;

            return true;
        }

        private Event m_ProcessingEvent = new();

        public void ProcessEvent(Event e)
        {
            KeyPressed(e);
        }


        /// <param name="eventData"></param>
        public virtual void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused)
                return;

            bool consumedEvent = false;
            EditState editState = EditState.Continue;

            while (Event.PopEvent(m_ProcessingEvent))
            {
                EventType eventType = m_ProcessingEvent.rawType;

                if (eventType == EventType.KeyUp)
                    continue;

                if (eventType == EventType.KeyDown)
                {
                    consumedEvent = true;

                    if (m_IsCompositionActive && compositionLength == 0)
                    {
                        if (m_ProcessingEvent.character == 0 && m_ProcessingEvent.modifiers == EventModifiers.None)
                            continue;
                    }

                    editState = KeyPressed(m_ProcessingEvent);
                    if (editState == EditState.Finish)
                    {
                        if (!m_WasCanceled)
                            SendOnSubmit();

                        DeactivateInputField();
                        break;
                    }

                    m_IsTextComponentUpdateRequired = true;
                    UpdateLabel();

                    continue;
                }

                switch (eventType)
                {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        switch (m_ProcessingEvent.commandName)
                        {
                            case "SelectAll":
                                SelectAll();
                                consumedEvent = true;
                                break;
                        }
                        break;
                }
            }

            if (consumedEvent || (m_IsCompositionActive && compositionLength > 0))
            {
                UpdateLabel();
                eventData.Use();
            }
        }

        /// <param name="eventData"></param>
        public virtual void OnScroll(PointerEventData eventData)
        {
            if (m_LineType == LineType.SingleLine)
            {
                if (m_IScrollHandlerParent != null)
                    m_IScrollHandlerParent.OnScroll(eventData);

                return;
            }

            if (m_TextComponent.preferredHeight < m_TextViewport.rect.height)
                return;

            float scrollDirection = -eventData.scrollDelta.y;

            m_ScrollPosition = GetScrollPositionRelativeToViewport();

            m_ScrollPosition += (1f / m_TextComponent.textInfo.lineCount) * scrollDirection * m_ScrollSensitivity;

            m_ScrollPosition = Mathf.Clamp01(m_ScrollPosition);

            AdjustTextPositionRelativeToViewport(m_ScrollPosition);

            if (m_VerticalScrollbar)
            {
                m_VerticalScrollbar.value = m_ScrollPosition;
            }
        }

        private float GetScrollPositionRelativeToViewport()
        {
            Rect viewportRect = m_TextViewport.rect;

            float scrollPosition = (m_TextComponent.textInfo.lineInfo[0].ascender + m_TextComponent.margin.y + m_TextComponent.margin.w - viewportRect.yMax + m_TextComponent.rectTransform.anchoredPosition.y) / ( m_TextComponent.preferredHeight - viewportRect.height);

            scrollPosition = (int)((scrollPosition * 1000) + 0.5f) / 1000.0f;

            return scrollPosition;
        }

        private string GetSelectedString()
        {
            if (!hasSelection)
                return "";

            int startPos = stringPositionInternal;
            int endPos = stringSelectPositionInternal;

            if (startPos > endPos)
            {
                int temp = startPos;
                startPos = endPos;
                endPos = temp;
            }


            return text.Substring(startPos, endPos - startPos);
        }

        private int FindNextWordBegin()
        {
            if (stringSelectPositionInternal + 1 >= text.Length)
                return text.Length;

            int spaceLoc = text.IndexOfAny(kSeparators, stringSelectPositionInternal + 1);

            if (spaceLoc == -1)
                spaceLoc = text.Length;
            else
                spaceLoc++;

            return spaceLoc;
        }

        private void MoveRight(bool shift, bool ctrl)
        {
            if (hasSelection && !shift)
            {
                stringPositionInternal = stringSelectPositionInternal = Mathf.Max(stringPositionInternal, stringSelectPositionInternal);
                caretPositionInternal = caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);

                #if TMP_DEBUG_MODE
                    Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
                #endif
                return;
            }

            int position;
            if (ctrl)
                position = FindNextWordBegin();
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    if (stringSelectPositionInternal < text.Length && char.IsHighSurrogate(text[stringSelectPositionInternal]))
                        position = stringSelectPositionInternal + 2;
                    else
                        position = stringSelectPositionInternal + 1;
                }
                else
                {
                    if (m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].character == '\r' && m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal + 1].character == '\n')
                        position = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal + 1].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal + 1].stringLength;
                    else
                        position = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].stringLength;
                }

            }

            if (shift)
            {
                stringSelectPositionInternal = position;
                caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            }
            else
            {
                stringSelectPositionInternal = stringPositionInternal = position;

                if (stringPositionInternal >= m_TextComponent.textInfo.characterInfo[caretPositionInternal].index + m_TextComponent.textInfo.characterInfo[caretPositionInternal].stringLength)
                    caretSelectPositionInternal = caretPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + "  Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + "  String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        private int FindPrevWordBegin()
        {
            if (stringSelectPositionInternal - 2 < 0)
                return 0;

            int spaceLoc = text.LastIndexOfAny(kSeparators, stringSelectPositionInternal - 2);

            if (spaceLoc == -1)
                spaceLoc = 0;
            else
                spaceLoc++;

            return spaceLoc;
        }

        private void MoveLeft(bool shift, bool ctrl)
        {
            if (hasSelection && !shift)
            {
                stringPositionInternal = stringSelectPositionInternal = Mathf.Min(stringPositionInternal, stringSelectPositionInternal);
                caretPositionInternal = caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);

                #if TMP_DEBUG_MODE
                    Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
                #endif
                return;
            }

            int position;
            if (ctrl)
                position = FindPrevWordBegin();
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    if (stringSelectPositionInternal > 0 && char.IsLowSurrogate(text[stringSelectPositionInternal - 1]))
                        position = stringSelectPositionInternal - 2;
                    else
                        position =  stringSelectPositionInternal - 1;
                }
                else
                {
                    position = caretSelectPositionInternal < 1
                        ? m_TextComponent.textInfo.characterInfo[0].index
                        : m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].index;

                    if (position > 0 && m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].character == '\n' && m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 2].character == '\r')
                        position = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 2].index;
                }
            }

            if (shift)
            {
                stringSelectPositionInternal = position;
                caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            }
            else
            {
                stringSelectPositionInternal = stringPositionInternal = position;

                if (caretPositionInternal > 0 && stringPositionInternal <= m_TextComponent.textInfo.characterInfo[caretPositionInternal - 1].index)
                    caretSelectPositionInternal = caretPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + "  Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + "  String Select Position: " + stringSelectPositionInternal);
            #endif
        }


        private int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
        {
            if (originalPos >= m_TextComponent.textInfo.characterCount)
                originalPos -= 1;

            TMP_CharacterInfo originChar = m_TextComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            if (originLine - 1 < 0)
                return goToFirstChar ? 0 : originalPos;

            int endCharIdx = m_TextComponent.textInfo.lineInfo[originLine].firstCharacterIndex - 1;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_TextComponent.textInfo.lineInfo[originLine - 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_TextComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }


        private int LineDownCharacterPosition(int originalPos, bool goToLastChar)
        {
            if (originalPos >= m_TextComponent.textInfo.characterCount)
                return m_TextComponent.textInfo.characterCount - 1;

            TMP_CharacterInfo originChar = m_TextComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            if (originLine + 1 >= m_TextComponent.textInfo.lineCount)
                return goToLastChar ? m_TextComponent.textInfo.characterCount - 1 : originalPos;

            int endCharIdx = m_TextComponent.textInfo.lineInfo[originLine + 1].lastCharacterIndex;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_TextComponent.textInfo.lineInfo[originLine + 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_TextComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }


         private int PageUpCharacterPosition(int originalPos, bool goToFirstChar)
        {
            if (originalPos >= m_TextComponent.textInfo.characterCount)
                originalPos -= 1;

            TMP_CharacterInfo originChar = m_TextComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            if (originLine - 1 < 0)
                return goToFirstChar ? 0 : originalPos;

            float viewportHeight = m_TextViewport.rect.height;

            int newLine = originLine - 1;
            for (; newLine > 0; newLine--)
            {
                if (m_TextComponent.textInfo.lineInfo[newLine].baseline > m_TextComponent.textInfo.lineInfo[originLine].baseline + viewportHeight)
                    break;
            }

            int endCharIdx = m_TextComponent.textInfo.lineInfo[newLine].lastCharacterIndex;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_TextComponent.textInfo.lineInfo[newLine].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_TextComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }


         private int PageDownCharacterPosition(int originalPos, bool goToLastChar)
        {
            if (originalPos >= m_TextComponent.textInfo.characterCount)
                return m_TextComponent.textInfo.characterCount - 1;

            TMP_CharacterInfo originChar = m_TextComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            if (originLine + 1 >= m_TextComponent.textInfo.lineCount)
                return goToLastChar ? m_TextComponent.textInfo.characterCount - 1 : originalPos;

            float viewportHeight = m_TextViewport.rect.height;

            int newLine = originLine + 1;
            for (; newLine < m_TextComponent.textInfo.lineCount - 1; newLine++)
            {
                if (m_TextComponent.textInfo.lineInfo[newLine].baseline < m_TextComponent.textInfo.lineInfo[originLine].baseline - viewportHeight)
                    break;
            }

            int endCharIdx = m_TextComponent.textInfo.lineInfo[newLine].lastCharacterIndex;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_TextComponent.textInfo.lineInfo[newLine].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_TextComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }


        private void MoveDown(bool shift)
        {
            MoveDown(shift, true);
        }


        private void MoveDown(bool shift, bool goToLastChar)
        {
            if (hasSelection && !shift)
            {
                caretPositionInternal = caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal);
            }

            int position = multiLine ? LineDownCharacterPosition(caretSelectPositionInternal, goToLastChar) : m_TextComponent.textInfo.characterCount - 1;

            if (shift)
            {
                caretSelectPositionInternal = position;
                stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }
            else
            {
                caretSelectPositionInternal = caretPositionInternal = position;
                stringSelectPositionInternal = stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        private void MoveUp(bool shift)
        {
            MoveUp(shift, true);
        }


        private void MoveUp(bool shift, bool goToFirstChar)
        {
            if (hasSelection && !shift)
            {
                caretPositionInternal = caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal);
            }

            int position = multiLine ? LineUpCharacterPosition(caretSelectPositionInternal, goToFirstChar) : 0;

            if (shift)
            {
                caretSelectPositionInternal = position;
                stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }
            else
            {
                caretSelectPositionInternal = caretPositionInternal = position;
                stringSelectPositionInternal = stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }


        private void MovePageUp(bool shift)
        {
            MovePageUp(shift, true);
        }

        private void MovePageUp(bool shift, bool goToFirstChar)
        {
            if (hasSelection && !shift)
            {
                caretPositionInternal = caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal);
            }

            int position = multiLine ? PageUpCharacterPosition(caretSelectPositionInternal, goToFirstChar) : 0;

            if (shift)
            {
                caretSelectPositionInternal = position;
                stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }
            else
            {
                caretSelectPositionInternal = caretPositionInternal = position;
                stringSelectPositionInternal = stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }


            if (m_LineType != LineType.SingleLine)
            {
                float offset = m_TextViewport.rect.height;

                float topTextBounds = m_TextComponent.rectTransform.position.y + m_TextComponent.TextBounds.max.y;
                float topViewportBounds = m_TextViewport.position.y + m_TextViewport.rect.yMax;

                offset = topViewportBounds > topTextBounds + offset ? offset : topViewportBounds - topTextBounds;

                m_TextComponent.rectTransform.anchoredPosition += new Vector2(0, offset);
                AssignPositioningIfNeeded();
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif

        }


        private void MovePageDown(bool shift)
        {
            MovePageDown(shift, true);
        }

        private void MovePageDown(bool shift, bool goToLastChar)
        {
             if (hasSelection && !shift)
            {
                caretPositionInternal = caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal);
            }

            int position = multiLine ? PageDownCharacterPosition(caretSelectPositionInternal, goToLastChar) : m_TextComponent.textInfo.characterCount - 1;

            if (shift)
            {
                caretSelectPositionInternal = position;
                stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }
            else
            {
                caretSelectPositionInternal = caretPositionInternal = position;
                stringSelectPositionInternal = stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
            }

            if (m_LineType != LineType.SingleLine)
            {
                float offset = m_TextViewport.rect.height;

                float bottomTextBounds = m_TextComponent.rectTransform.position.y + m_TextComponent.TextBounds.min.y;
                float bottomViewportBounds = m_TextViewport.position.y + m_TextViewport.rect.yMin;

                offset = bottomViewportBounds > bottomTextBounds + offset ? offset : bottomViewportBounds - bottomTextBounds;

                m_TextComponent.rectTransform.anchoredPosition += new Vector2(0, offset);
                AssignPositioningIfNeeded();
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif

        }

        private void Delete()
        {
            if (m_ReadOnly)
                return;

            if (m_StringPosition == m_StringSelectPosition)
                return;

            if (m_isRichTextEditingAllowed || m_isSelectAll)
            {
                if (m_StringPosition < m_StringSelectPosition)
                {
                    m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
                    m_StringSelectPosition = m_StringPosition;
                }
                else
                {
                    m_Text = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
                    m_StringPosition = m_StringSelectPosition;
                }

                if (m_isSelectAll)
                {
                    m_CaretPosition = m_CaretSelectPosition = 0;
                    m_isSelectAll = false;
                }
            }
            else
            {
                if (m_CaretPosition < m_CaretSelectPosition)
                {
                    int index = ClampArrayIndex(m_CaretSelectPosition - 1);
                    m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition].index;
                    m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[index].index + m_TextComponent.textInfo.characterInfo[index].stringLength;

                    m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);

                    m_StringSelectPosition = m_StringPosition;
                    m_CaretSelectPosition = m_CaretPosition;
                }
                else
                {
                    int index = ClampArrayIndex(m_CaretPosition - 1);
                    m_StringPosition = m_TextComponent.textInfo.characterInfo[index].index + m_TextComponent.textInfo.characterInfo[index].stringLength;
                    m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition].index;

                    m_Text = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);

                    m_StringPosition = m_StringSelectPosition;
                    m_CaretPosition = m_CaretSelectPosition;
                }
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        private void DeleteKey()
        {
            if (m_ReadOnly)
                return;

            if (hasSelection)
            {
                m_HasTextBeenRemoved = true;

                Delete();
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    if (stringPositionInternal < text.Length)
                    {
                        if (char.IsHighSurrogate(text[stringPositionInternal]))
                            m_Text = text.Remove(stringPositionInternal, 2);
                        else
                            m_Text = text.Remove(stringPositionInternal, 1);

                        m_HasTextBeenRemoved = true;

                        UpdateTouchKeyboardFromEditChanges();
                        SendOnValueChangedAndUpdateLabel();
                    }
                }
                else
                {
                    if (caretPositionInternal < m_TextComponent.textInfo.characterCount - 1)
                    {
                        int numberOfCharactersToRemove = m_TextComponent.textInfo.characterInfo[caretPositionInternal].stringLength;

                        if (m_TextComponent.textInfo.characterInfo[caretPositionInternal].character == '\r' && m_TextComponent.textInfo.characterInfo[caretPositionInternal + 1].character == '\n')
                            numberOfCharactersToRemove += m_TextComponent.textInfo.characterInfo[caretPositionInternal + 1].stringLength;

                        int nextCharacterStringPosition = m_TextComponent.textInfo.characterInfo[caretPositionInternal].index;

                        m_Text = text.Remove(nextCharacterStringPosition, numberOfCharactersToRemove);

                        m_HasTextBeenRemoved = true;

                        SendOnValueChangedAndUpdateLabel();
                    }
                }
            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        private void Backspace()
        {
            if (m_ReadOnly)
                return;

            if (hasSelection)
            {
                m_HasTextBeenRemoved = true;

                Delete();
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    if (stringPositionInternal > 0)
                    {
                        int numberOfCharactersToRemove = 1;

                        if (char.IsLowSurrogate(text[stringPositionInternal - 1]))
                            numberOfCharactersToRemove = 2;

                        stringSelectPositionInternal = stringPositionInternal = stringPositionInternal - numberOfCharactersToRemove;

                        m_Text = text.Remove(stringPositionInternal, numberOfCharactersToRemove);

                        caretSelectPositionInternal = caretPositionInternal = caretPositionInternal - 1;

                        m_HasTextBeenRemoved = true;

                        UpdateTouchKeyboardFromEditChanges();
                        SendOnValueChangedAndUpdateLabel();
                    }
                }
                else
                {
                    if (caretPositionInternal > 0)
                    {
                        int caretPositionIndex = caretPositionInternal - 1;
                        int numberOfCharactersToRemove = m_TextComponent.textInfo.characterInfo[caretPositionIndex].stringLength;

                        if (caretPositionIndex > 0 && m_TextComponent.textInfo.characterInfo[caretPositionIndex].character == '\n' && m_TextComponent.textInfo.characterInfo[caretPositionIndex - 1].character == '\r')
                        {
                            numberOfCharactersToRemove += m_TextComponent.textInfo.characterInfo[caretPositionIndex - 1].stringLength;
                            caretPositionIndex -= 1;
                        }

                        m_Text = text.Remove(m_TextComponent.textInfo.characterInfo[caretPositionIndex].index, numberOfCharactersToRemove);

                        stringSelectPositionInternal = stringPositionInternal = caretPositionInternal < 1
                            ? m_TextComponent.textInfo.characterInfo[0].index
                            : m_TextComponent.textInfo.characterInfo[caretPositionIndex].index;

                        caretSelectPositionInternal = caretPositionInternal = caretPositionIndex;
                    }

                    m_HasTextBeenRemoved = true;

                    UpdateTouchKeyboardFromEditChanges();
                    SendOnValueChangedAndUpdateLabel();
                }

            }

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }


        protected virtual void Append(string input)
        {
            if (m_ReadOnly)
                return;

            if (!InPlaceEditing())
                return;

            for (int i = 0, imax = input.Length; i < imax; ++i)
            {
                char c = input[i];

                if (c >= ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    Append(c);
                }
            }
        }

        protected virtual void Append(char input)
        {
            if (m_ReadOnly)
                return;

            if (!InPlaceEditing())
                return;

            int insertionPosition = Mathf.Min(stringPositionInternal, stringSelectPositionInternal);

            var validateText = text;

            if (selectionFocusPosition != selectionAnchorPosition)
            {

                m_HasTextBeenRemoved = true;

                if (m_isRichTextEditingAllowed || m_isSelectAll)
                {
                    if (m_StringPosition < m_StringSelectPosition)
                        validateText = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
                    else
                        validateText = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
                }
                else
                {
                    if (m_CaretPosition < m_CaretSelectPosition)
                    {
                        m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition].index;
                        m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition - 1].index + m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition - 1].stringLength;

                        validateText = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
                    }
                    else
                    {
                        m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition - 1].index + m_TextComponent.textInfo.characterInfo[m_CaretPosition - 1].stringLength;
                        m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition].index;

                        validateText = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
                    }
                }
            }

            if (onValidateInput != null)
            {
                input = onValidateInput(validateText, insertionPosition, input);
            }
            else if (characterValidation == CharacterValidation.CustomValidator)
            {
                input = Validate(validateText, insertionPosition, input);

                if (input == 0) return;

                if (!char.IsHighSurrogate(input))
                    m_CaretSelectPosition = m_CaretPosition += 1;

                SendOnValueChanged();
                UpdateLabel();

                return;
            }
            else if (characterValidation != CharacterValidation.None)
            {
                input = Validate(validateText, insertionPosition, input);
            }

            if (input == 0)
                return;

            Insert(input);
        }


        private void Insert(char c)
        {
            if (m_ReadOnly)
                return;

            string replaceString = c.ToString();
            Delete();

            if (characterLimit > 0 && text.Length >= characterLimit)
                return;

            m_Text = text.Insert(m_StringPosition, replaceString);

            if (!char.IsHighSurrogate(c))
                m_CaretSelectPosition = m_CaretPosition += 1;

            m_StringSelectPosition = m_StringPosition += 1;

            UpdateTouchKeyboardFromEditChanges();
            SendOnValueChanged();

            #if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
            #endif
        }

        private void UpdateTouchKeyboardFromEditChanges()
        {
            if (m_SoftKeyboard != null && InPlaceEditing())
            {
                m_SoftKeyboard.text = m_Text;
            }
        }

        private void SendOnValueChangedAndUpdateLabel()
        {
            UpdateLabel();
            SendOnValueChanged();
        }

        private void SendOnValueChanged()
        {
            if (onValueChanged != null)
                onValueChanged.Invoke(text);
        }


        protected void SendOnEndEdit()
        {
            if (onEndEdit != null)
                onEndEdit.Invoke(m_Text);
        }

        protected void SendOnSubmit()
        {
            if (onSubmit != null)
                onSubmit.Invoke(m_Text);
        }

        protected void SendOnFocus()
        {
            if (onSelect != null)
                onSelect.Invoke(m_Text);
        }

        protected void SendOnFocusLost()
        {
            if (onDeselect != null)
                onDeselect.Invoke(m_Text);
        }

        protected void SendOnTextSelection()
        {
            m_isSelected = true;

            if (onTextSelection != null)
                onTextSelection.Invoke(m_Text, stringPositionInternal, stringSelectPositionInternal);
        }

        protected void SendOnEndTextSelection()
        {
            if (!m_isSelected) return;

            if (onEndTextSelection != null)
                onEndTextSelection.Invoke(m_Text, stringPositionInternal, stringSelectPositionInternal);

            m_isSelected = false;
        }

        protected void SendTouchScreenKeyboardStatusChanged()
        {
            if (m_SoftKeyboard != null && onTouchScreenKeyboardStatusChanged != null)
                onTouchScreenKeyboardStatusChanged.Invoke(m_SoftKeyboard.status);
        }



        protected void UpdateLabel()
        {
            if (m_TextComponent != null && m_TextComponent.font != null && !m_PreventCallback)
            {
                m_PreventCallback = true;

                string fullText;
                if (compositionLength > 0 && !m_ReadOnly)
                {
                    Delete();

                    if (m_RichText)
                        fullText = text.Substring(0, m_StringPosition) +  "<u>" + compositionString + "</u>" + text.Substring(m_StringPosition);
                    else
                        fullText = text.Substring(0, m_StringPosition) +  compositionString + text.Substring(m_StringPosition);

                    m_IsCompositionActive = true;
                }
                else
                {
                    fullText = text;
                    m_IsCompositionActive = false;
                    m_ShouldUpdateIMEWindowPosition = true;

                }

                string processed;
                if (inputType == InputType.Password)
                    processed = new(asteriskChar, fullText.Length);
                else
                    processed = fullText;

                bool isEmpty = string.IsNullOrEmpty(fullText);

                if (m_Placeholder != null)
                    m_Placeholder.enabled = isEmpty;

                if (!isEmpty && !m_ReadOnly)
                {
                    SetCaretVisible();
                }

                m_TextComponent.text = processed + "\u200B";

                if (m_IsDrivenByLayoutComponents)
                    LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);

                if (m_LineLimit > 0)
                {
                    m_TextComponent.ForceMeshUpdate();

                    TMP_TextInfo textInfo = m_TextComponent.textInfo;

                    if (textInfo != null && textInfo.lineCount > m_LineLimit)
                    {
                        int lastValidCharacterIndex = textInfo.lineInfo[m_LineLimit - 1].lastCharacterIndex;
                        int characterStringIndex = textInfo.characterInfo[lastValidCharacterIndex].index + textInfo.characterInfo[lastValidCharacterIndex].stringLength;
                        text = processed.Remove(characterStringIndex, processed.Length - characterStringIndex);
                        m_TextComponent.text = text + "\u200B";
                    }
                }

                if (m_IsTextComponentUpdateRequired || m_VerticalScrollbar && !(m_IsCaretPositionDirty && m_IsStringPositionDirty))
                {
                    m_IsTextComponentUpdateRequired = false;
                    m_TextComponent.ForceMeshUpdate();
                }

                MarkGeometryAsDirty();

                m_PreventCallback = false;
            }
        }


        private void UpdateScrollbar()
        {
            if (m_VerticalScrollbar)
            {
                Rect viewportRect = m_TextViewport.rect;

                float size = viewportRect.height / m_TextComponent.preferredHeight;

                m_VerticalScrollbar.size = size;

                m_VerticalScrollbar.value = GetScrollPositionRelativeToViewport();
            }
        }


        /// <param name="value"></param>
        private void OnScrollbarValueChange(float value)
        {
            if (value < 0 || value > 1) return;

            AdjustTextPositionRelativeToViewport(value);

            m_ScrollPosition = value;
        }

        private void UpdateMaskRegions()
        {
        }

        /// <param name="relativePosition"></param>
        private void AdjustTextPositionRelativeToViewport (float relativePosition)
        {
            if (m_TextViewport == null)
                return;

            TMP_TextInfo textInfo = m_TextComponent.textInfo;

            if (textInfo == null || textInfo.lineInfo == null || textInfo.lineCount == 0 || textInfo.lineCount > textInfo.lineInfo.Length) return;

            float verticalAlignmentOffset = 0;
            float textHeight = m_TextComponent.preferredHeight;

            switch (m_TextComponent.verticalAlignment)
            {
                case VerticalAlignmentOptions.Top:
                    verticalAlignmentOffset = 0;
                    break;
                case VerticalAlignmentOptions.Middle:
                    verticalAlignmentOffset = 0.5f;
                    break;
                case VerticalAlignmentOptions.Bottom:
                    verticalAlignmentOffset = 1.0f;
                    break;
                case VerticalAlignmentOptions.Baseline:
                    break;
                case VerticalAlignmentOptions.Geometry:
                    verticalAlignmentOffset = 0.5f;
                    textHeight = m_TextComponent.bounds.size.y;
                    break;
                case VerticalAlignmentOptions.Capline:
                    verticalAlignmentOffset = 0.5f;
                    break;
            }

            m_TextComponent.rectTransform.anchoredPosition = new(m_TextComponent.rectTransform.anchoredPosition.x, (textHeight - m_TextViewport.rect.height) * (relativePosition - verticalAlignmentOffset));

            AssignPositioningIfNeeded();
        }


        private int GetCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_TextComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (m_TextComponent.textInfo.characterInfo[i].index >= stringIndex)
                    return i;
            }

            return count;
        }

        /// <param name="stringIndex"></param>
        /// <returns></returns>
        private int GetMinCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_TextComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (stringIndex < m_TextComponent.textInfo.characterInfo[i].index + m_TextComponent.textInfo.characterInfo[i].stringLength)
                    return i;
            }

            return count;
        }

        /// <param name="stringIndex"></param>
        /// <returns></returns>
        private int GetMaxCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_TextComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (m_TextComponent.textInfo.characterInfo[i].index >= stringIndex)
                    return i;
            }

            return count;
        }

        private int GetStringIndexFromCaretPosition(int caretPosition)
        {
            ClampCaretPos(ref caretPosition);

            return m_TextComponent.textInfo.characterInfo[caretPosition].index;
        }

        private void UpdateStringIndexFromCaretPosition()
        {
            stringPositionInternal = GetStringIndexFromCaretPosition(m_CaretPosition);
            stringSelectPositionInternal = GetStringIndexFromCaretPosition(m_CaretSelectPosition);
            m_IsStringPositionDirty = false;
        }

        private void UpdateCaretPositionFromStringIndex()
        {
            caretPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
            caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            m_IsCaretPositionDirty = false;
        }


        public void ForceLabelUpdate()
        {
            UpdateLabel();
        }

        private void MarkGeometryAsDirty()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
            #endif

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        public virtual void Rebuild(CanvasUpdate update)
        {
            switch (update)
            {
                case CanvasUpdate.PreRender:
                    UpdateGeometry();
                    break;
            }
        }

        public virtual void LayoutComplete()
        { }

        public virtual void GraphicUpdateComplete()
        { }

        private void UpdateGeometry()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            #endif

            if (!InPlaceEditing() && !isUWP())
                return;

            if (m_CachedInputRenderer == null)
                return;

            OnFillVBO(mesh);

            m_CachedInputRenderer.SetMesh(mesh);
        }


        private void AssignPositioningIfNeeded()
        {
            if (m_TextComponent != null && caretRectTrans != null &&
                (caretRectTrans.localPosition != m_TextComponent.rectTransform.localPosition ||
                 caretRectTrans.localRotation != m_TextComponent.rectTransform.localRotation ||
                 caretRectTrans.localScale != m_TextComponent.rectTransform.localScale ||
                 caretRectTrans.anchorMin != m_TextComponent.rectTransform.anchorMin ||
                 caretRectTrans.anchorMax != m_TextComponent.rectTransform.anchorMax ||
                 caretRectTrans.anchoredPosition != m_TextComponent.rectTransform.anchoredPosition ||
                 caretRectTrans.sizeDelta != m_TextComponent.rectTransform.sizeDelta ||
                 caretRectTrans.pivot != m_TextComponent.rectTransform.pivot))
            {
                caretRectTrans.localPosition = m_TextComponent.rectTransform.localPosition;
                caretRectTrans.localRotation = m_TextComponent.rectTransform.localRotation;
                caretRectTrans.localScale = m_TextComponent.rectTransform.localScale;
                caretRectTrans.anchorMin = m_TextComponent.rectTransform.anchorMin;
                caretRectTrans.anchorMax = m_TextComponent.rectTransform.anchorMax;
                caretRectTrans.anchoredPosition = m_TextComponent.rectTransform.anchoredPosition;
                caretRectTrans.sizeDelta = m_TextComponent.rectTransform.sizeDelta;
                caretRectTrans.pivot = m_TextComponent.rectTransform.pivot;
            }
        }


        private void OnFillVBO(Mesh vbo)
        {
            using (var helper = new VertexHelper())
            {
                if (!isFocused && !m_SelectionStillActive)
                {
                    helper.FillMesh(vbo);
                    return;
                }

                if (m_IsStringPositionDirty)
                    UpdateStringIndexFromCaretPosition();

                if (m_IsCaretPositionDirty)
                    UpdateCaretPositionFromStringIndex();

                if (!hasSelection)
                {
                    GenerateCaret(helper, Vector2.zero);
                    SendOnEndTextSelection();
                }
                else
                {
                    GenerateHighlight(helper, Vector2.zero);
                    SendOnTextSelection();
                }

                helper.FillMesh(vbo);
            }
        }


        private void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
        {
            if (!m_CaretVisible || m_TextComponent.canvas == null || m_ReadOnly)
                return;

            if (m_CursorVerts == null)
            {
                CreateCursorVerts();
            }

            Vector2 startPosition = Vector2.zero;
            float height = 0;
            TMP_CharacterInfo currentCharacter;

            if (caretPositionInternal >= m_TextComponent.textInfo.characterInfo.Length || caretPositionInternal < 0)
                return;

            int currentLine = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;

            if (caretPositionInternal == m_TextComponent.textInfo.lineInfo[currentLine].firstCharacterIndex)
            {
                currentCharacter = m_TextComponent.textInfo.characterInfo[caretPositionInternal];
                height = currentCharacter.ascender - currentCharacter.descender;

                if (m_TextComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    startPosition = new(currentCharacter.origin, 0 - height / 2);
                else
                    startPosition = new(currentCharacter.origin, currentCharacter.descender);
            }
            else
            {
                currentCharacter = m_TextComponent.textInfo.characterInfo[caretPositionInternal - 1];
                height = currentCharacter.ascender - currentCharacter.descender;

                if (m_TextComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    startPosition = new(currentCharacter.xAdvance, 0 - height / 2);
                else
                    startPosition = new(currentCharacter.xAdvance, currentCharacter.descender);

            }

            if (m_SoftKeyboard != null && compositionLength == 0)
            {
                int selectionStart = m_StringPosition;
                int softKeyboardStringLength = m_SoftKeyboard.text == null ? 0 : m_SoftKeyboard.text.Length;

                if (selectionStart < 0)
                    selectionStart = 0;

                if (selectionStart > softKeyboardStringLength)
                    selectionStart = softKeyboardStringLength;

                m_SoftKeyboard.selection = new(selectionStart, 0);
            }

            if (isFocused && startPosition != m_LastPosition || m_forceRectTransformAdjustment || m_HasTextBeenRemoved)
                AdjustRectTransformRelativeToViewport(startPosition, height, currentCharacter.isVisible);

            m_LastPosition = startPosition;

            float top = startPosition.y + height;
            float bottom = top - height;

            TMP_FontAsset fontAsset = m_TextComponent.font;
            float baseScale = (m_TextComponent.fontSize / fontAsset.m_FaceInfo.pointSize * fontAsset.m_FaceInfo.scale);
            float width = m_CaretWidth * fontAsset.faceInfo.lineHeight * baseScale * 0.05f;
            width = Mathf.Max(width, 1.0f);

            m_CursorVerts[0].position = new(startPosition.x, bottom, 0.0f);
            m_CursorVerts[1].position = new(startPosition.x, top, 0.0f);
            m_CursorVerts[2].position = new(startPosition.x + width, top, 0.0f);
            m_CursorVerts[3].position = new(startPosition.x + width, bottom, 0.0f);

            m_CursorVerts[0].color = caretColor;
            m_CursorVerts[1].color = caretColor;
            m_CursorVerts[2].color = caretColor;
            m_CursorVerts[3].color = caretColor;

            vbo.AddUIVertexQuad(m_CursorVerts);

            if (m_ShouldUpdateIMEWindowPosition || currentLine != m_PreviousIMEInsertionLine)
            {
                m_ShouldUpdateIMEWindowPosition = false;
                m_PreviousIMEInsertionLine = currentLine;

                Camera cameraRef;
                if (m_TextComponent.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    cameraRef = null;
                else
                {
                    cameraRef = m_TextComponent.canvas.worldCamera;

                    if (cameraRef == null)
                        cameraRef = Camera.current;
                }

                Vector3 cursorPosition = m_CachedInputRenderer.gameObject.transform.TransformPoint(m_CursorVerts[0].position);
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(cameraRef, cursorPosition);
                screenPosition.y = Screen.height - screenPosition.y;

                if (inputSystem != null)
                    inputSystem.compositionCursorPos = screenPosition;
            }
        }


        private void CreateCursorVerts()
        {
            m_CursorVerts = new UIVertex[4];

            for (int i = 0; i < m_CursorVerts.Length; i++)
            {
                m_CursorVerts[i] = UIVertex.simpleVert;
                m_CursorVerts[i].uv0 = Vector2.zero;
            }
        }


        private void GenerateHighlight(VertexHelper vbo, Vector2 roundingOffset)
        {
            UpdateMaskRegions();

            TMP_TextInfo textInfo = m_TextComponent.textInfo;

            if (textInfo.characterCount == 0)
                return;

            m_CaretPosition = GetCaretPositionFromStringIndex(stringPositionInternal);
            m_CaretSelectPosition = GetCaretPositionFromStringIndex(stringSelectPositionInternal);

            if (m_SoftKeyboard != null && compositionLength == 0)
            {
                int stringPosition = m_CaretPosition < m_CaretSelectPosition ? textInfo.characterInfo[m_CaretPosition].index : textInfo.characterInfo[m_CaretSelectPosition].index;
                int length = m_CaretPosition < m_CaretSelectPosition ? stringSelectPositionInternal - stringPosition : stringPositionInternal - stringPosition;
                m_SoftKeyboard.selection = new(stringPosition, length);
            }

            Vector2 caretPosition;
            float height = 0;
            if (m_CaretSelectPosition < textInfo.characterCount)
            {
                caretPosition = new(textInfo.characterInfo[m_CaretSelectPosition].origin, textInfo.characterInfo[m_CaretSelectPosition].descender);
                height = textInfo.characterInfo[m_CaretSelectPosition].ascender - textInfo.characterInfo[m_CaretSelectPosition].descender;
            }
            else
            {
                caretPosition = new(textInfo.characterInfo[m_CaretSelectPosition - 1].xAdvance, textInfo.characterInfo[m_CaretSelectPosition - 1].descender);
                height = textInfo.characterInfo[m_CaretSelectPosition - 1].ascender - textInfo.characterInfo[m_CaretSelectPosition - 1].descender;
            }

            AdjustRectTransformRelativeToViewport(caretPosition, height, true);

            int startChar = Mathf.Max(0, m_CaretPosition);
            int endChar = Mathf.Max(0, m_CaretSelectPosition);

            if (startChar > endChar)
            {
                int temp = startChar;
                startChar = endChar;
                endChar = temp;
            }

            endChar -= 1;


            int currentLineIndex = textInfo.characterInfo[startChar].lineNumber;
            int nextLineStartIdx = textInfo.lineInfo[currentLineIndex].lastCharacterIndex;

            UIVertex vert = UIVertex.simpleVert;
            vert.uv0 = Vector2.zero;
            vert.color = selectionColor;

            int currentChar = startChar;
            while (currentChar <= endChar && currentChar < textInfo.characterCount)
            {
                if (currentChar == nextLineStartIdx || currentChar == endChar)
                {
                    TMP_CharacterInfo startCharInfo = textInfo.characterInfo[startChar];
                    TMP_CharacterInfo endCharInfo = textInfo.characterInfo[currentChar];

                    if (currentChar > 0 && endCharInfo.character == '\n' && textInfo.characterInfo[currentChar - 1].character == '\r')
                        endCharInfo = textInfo.characterInfo[currentChar - 1];

                    Vector2 startPosition = new(startCharInfo.origin, textInfo.lineInfo[currentLineIndex].ascender);
                    Vector2 endPosition = new(endCharInfo.xAdvance, textInfo.lineInfo[currentLineIndex].descender);

                    var startIndex = vbo.currentVertCount;
                    vert.position = new(startPosition.x, endPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vert.position = new(endPosition.x, endPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vert.position = new(endPosition.x, startPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vert.position = new(startPosition.x, startPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vbo.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                    vbo.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);

                    startChar = currentChar + 1;
                    currentLineIndex++;

                    if (currentLineIndex < textInfo.lineCount)
                        nextLineStartIdx = textInfo.lineInfo[currentLineIndex].lastCharacterIndex;
                }
                currentChar++;
            }
        }


        /// <param name="startPosition"></param>
        /// <param name="height"></param>
        /// <param name="isCharVisible"></param>
        private void AdjustRectTransformRelativeToViewport(Vector2 startPosition, float height, bool isCharVisible)
        {
            if (m_TextViewport == null)
                return;

            Vector3 localPosition = transform.localPosition;
            Vector3 textComponentLocalPosition = m_TextComponent.rectTransform.localPosition;
            Vector3 textViewportLocalPosition = m_TextViewport.localPosition;
            Rect textViewportRect = m_TextViewport.rect;

            Vector2 caretPosition = new(startPosition.x + textComponentLocalPosition.x + textViewportLocalPosition.x + localPosition.x, startPosition.y + textComponentLocalPosition.y + textViewportLocalPosition.y + localPosition.y);
            Rect viewportWSRect = new(localPosition.x + textViewportLocalPosition.x + textViewportRect.x, localPosition.y + textViewportLocalPosition.y + textViewportRect.y, textViewportRect.width, textViewportRect.height);

            float rightOffset = viewportWSRect.xMax - (caretPosition.x + m_TextComponent.margin.z + m_CaretWidth);
            if (rightOffset < 0f)
            {
                if (!multiLine || (multiLine && isCharVisible))
                {
                    m_TextComponent.rectTransform.anchoredPosition += new Vector2(rightOffset, 0);

                    AssignPositioningIfNeeded();
                }
            }

            float leftOffset = (caretPosition.x - m_TextComponent.margin.x) - viewportWSRect.xMin;
            if (leftOffset < 0f)
            {
                m_TextComponent.rectTransform.anchoredPosition += new Vector2(-leftOffset, 0);
                AssignPositioningIfNeeded();
            }

            if (m_LineType != LineType.SingleLine)
            {
                float topOffset = viewportWSRect.yMax - (caretPosition.y + height);
                if (topOffset < -0.0001f)
                {
                    m_TextComponent.rectTransform.anchoredPosition += new Vector2(0, topOffset);
                    AssignPositioningIfNeeded();
                }

                float bottomOffset = caretPosition.y - viewportWSRect.yMin;
                if (bottomOffset < 0f)
                {
                    m_TextComponent.rectTransform.anchoredPosition -= new Vector2(0, bottomOffset);
                    AssignPositioningIfNeeded();
                }
            }

            if (m_HasTextBeenRemoved)
            {
                float anchoredPositionX = m_TextComponent.rectTransform.anchoredPosition.x;

                float firstCharPosition = localPosition.x + textViewportLocalPosition.x + textComponentLocalPosition.x + m_TextComponent.textInfo.characterInfo[0].origin - m_TextComponent.margin.x;
                int lastCharacterIndex = ClampArrayIndex(m_TextComponent.textInfo.characterCount - 1);
                float lastCharPosition = localPosition.x + textViewportLocalPosition.x + textComponentLocalPosition.x + m_TextComponent.textInfo.characterInfo[lastCharacterIndex].origin + m_TextComponent.margin.z + m_CaretWidth;

                if (anchoredPositionX > 0.0001f && firstCharPosition > viewportWSRect.xMin)
                {
                    float offset = viewportWSRect.xMin - firstCharPosition;

                    if (anchoredPositionX < -offset)
                        offset = -anchoredPositionX;

                    m_TextComponent.rectTransform.anchoredPosition += new Vector2(offset, 0);
                    AssignPositioningIfNeeded();
                }
                else if (anchoredPositionX < -0.0001f && lastCharPosition < viewportWSRect.xMax)
                {
                    float offset = viewportWSRect.xMax - lastCharPosition;

                    if (-anchoredPositionX < offset)
                        offset = -anchoredPositionX;

                    m_TextComponent.rectTransform.anchoredPosition += new Vector2(offset, 0);
                    AssignPositioningIfNeeded();
                }

                m_HasTextBeenRemoved = false;
            }

            m_forceRectTransformAdjustment = false;
        }

        protected char Validate(string text, int pos, char ch)
        {
            if (characterValidation == CharacterValidation.None || !enabled)
                return ch;

            if (characterValidation == CharacterValidation.Integer || characterValidation == CharacterValidation.Decimal)
            {
                bool cursorBeforeDash = (pos == 0 && text.Length > 0 && text[0] == '-');
                bool selectionAtStart = stringPositionInternal == 0 || stringSelectPositionInternal == 0;
                if (!cursorBeforeDash)
                {
                    if (ch >= '0' && ch <= '9') return ch;
                    if (ch == '-' && (pos == 0 || selectionAtStart) && !text.Contains('-')) return ch;

                    var separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    if (ch == Convert.ToChar(separator) && characterValidation == CharacterValidation.Decimal && !text.Contains(separator)) return ch;

                    if (characterValidation == CharacterValidation.Integer && ch == '.' && (pos == 0 || selectionAtStart) && !text.Contains('-')) return '-';
                }

            }
            else if (characterValidation == CharacterValidation.Digit)
            {
                if (ch >= '0' && ch <= '9') return ch;
            }
            else if (characterValidation == CharacterValidation.Alphanumeric)
            {
                if (ch >= 'A' && ch <= 'Z') return ch;
                if (ch >= 'a' && ch <= 'z') return ch;
                if (ch >= '0' && ch <= '9') return ch;
            }
            else if (characterValidation == CharacterValidation.Name)
            {
                char prevChar = (text.Length > 0) ? text[Mathf.Clamp(pos - 1, 0, text.Length - 1)] : ' ';
                char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
                char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';

                if (char.IsLetter(ch))
                {
                    if (char.IsLower(ch) && pos == 0)
                        return char.ToUpper(ch);

                    if (char.IsLower(ch) && (prevChar == ' ' || prevChar == '-'))
                        return char.ToUpper(ch);

                    if (char.IsUpper(ch) && pos > 0 && prevChar != ' ' && prevChar != '\'' && prevChar != '-' && !char.IsLower(prevChar))
                        return char.ToLower(ch);

                    if (char.IsUpper(ch) && char.IsUpper(lastChar))
                        return (char)0;

                    return ch;
                }
                else if (ch == '\'')
                {
                    if (lastChar != ' ' && lastChar != '\'' && nextChar != '\'' && !text.Contains("'"))
                        return ch;
                }

                if (char.IsLetter(prevChar) && ch == '-' && lastChar != '-')
                {
                    return ch;
                }

                if ((ch == ' ' || ch == '-') && pos != 0)
                {
                    if (prevChar != ' ' && prevChar != '\'' && prevChar != '-' &&
                        lastChar != ' ' && lastChar != '\'' && lastChar != '-' &&
                        nextChar != ' ' && nextChar != '\'' && nextChar != '-')
                        return ch;
                }
            }
            else if (characterValidation == CharacterValidation.EmailAddress)
            {
                if (ch >= 'A' && ch <= 'Z') return ch;
                if (ch >= 'a' && ch <= 'z') return ch;
                if (ch >= '0' && ch <= '9') return ch;
                if (ch == '@' && text.IndexOf('@') == -1) return ch;
                if (kEmailSpecialCharacters.IndexOf(ch) != -1) return ch;
                if (ch == '.')
                {
                    char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
                    char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';
                    if (lastChar != '.' && nextChar != '.')
                        return ch;
                }
            }
            else if (characterValidation == CharacterValidation.Regex)
            {
                if (Regex.IsMatch(ch.ToString(), m_RegexValue))
                {
                    return ch;
                }
            }
            else if (characterValidation == CharacterValidation.CustomValidator)
            {
                if (m_InputValidator != null)
                {
                    char c = m_InputValidator.Validate(ref text, ref pos, ch);
                    m_Text = text;
                    stringSelectPositionInternal = stringPositionInternal = pos;
                    return c;
                }
            }
            return (char)0;
        }

        public void ActivateInputField()
        {
            if (m_TextComponent == null || m_TextComponent.font == null || !IsActive() || !IsInteractable())
                return;

            if (isFocused)
            {
                if (m_SoftKeyboard != null && !m_SoftKeyboard.active)
                {
                    m_SoftKeyboard.active = true;
                    m_SoftKeyboard.text = m_Text;
                }
            }

            m_ShouldActivateNextUpdate = true;
        }

        private void ActivateInputFieldInternal()
        {
            if (EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);

            m_TouchKeyboardAllowsInPlaceEditing = !s_IsQuestDevice && TouchScreenKeyboard.isInPlaceEditingAllowed;

            if (TouchScreenKeyboardShouldBeUsed() && !shouldHideSoftKeyboard)
            {
                if (inputSystem != null && inputSystem.touchSupported)
                {
                    TouchScreenKeyboard.hideInput = shouldHideMobileInput;
                }

                if (!shouldHideSoftKeyboard && !m_ReadOnly)
                {
                    m_SoftKeyboard = (inputType == InputType.Password) ?
                        TouchScreenKeyboard.Open(m_Text, keyboardType, false, multiLine, true, isAlert, "", characterLimit) :
                        TouchScreenKeyboard.Open(m_Text, keyboardType, inputType == InputType.AutoCorrect, multiLine, false, isAlert, "", characterLimit);

                    OnFocus();

                    if (m_SoftKeyboard != null && m_SoftKeyboard.canSetSelection)
                    {
                        int length = stringPositionInternal < stringSelectPositionInternal ? stringSelectPositionInternal - stringPositionInternal : stringPositionInternal - stringSelectPositionInternal;
                        m_SoftKeyboard.selection = new(stringPositionInternal < stringSelectPositionInternal ? stringPositionInternal : stringSelectPositionInternal, length);
                    }
                }
            }
            else
            {
                if (!TouchScreenKeyboardShouldBeUsed() && !m_ReadOnly && inputSystem != null)
                    inputSystem.imeCompositionMode = IMECompositionMode.On;

                OnFocus();
            }

            m_AllowInput = true;
            m_OriginalText = text;
            m_WasCanceled = false;
            SetCaretVisible();
            UpdateLabel();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            SendOnFocus();

            if (shouldActivateOnSelect)
                ActivateInputField();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ActivateInputField();
        }

        public void OnControlClick()
        {
        }

        public void ReleaseSelection()
        {
            m_SelectionStillActive = false;
            m_ReleaseSelection = false;
            m_PreviouslySelectedObject = null;

            MarkGeometryAsDirty();

            SendOnEndEdit();
            SendOnEndTextSelection();
        }

        public void DeactivateInputField(bool clearSelection = false)
        {
            if (!m_AllowInput)
                return;

            m_HasDoneFocusTransition = false;
            m_AllowInput = false;

            if (m_Placeholder != null)
                m_Placeholder.enabled = string.IsNullOrEmpty(m_Text);

            if (m_TextComponent != null && IsInteractable())
            {
                if (m_WasCanceled && m_RestoreOriginalTextOnEscape && !m_IsKeyboardBeingClosedInHoloLens)
                    text = m_OriginalText;

                if (m_SoftKeyboard != null)
                {
                    m_SoftKeyboard.active = false;
                    m_SoftKeyboard = null;
                }

                m_SelectionStillActive = true;

                if (m_ResetOnDeActivation || m_ReleaseSelection || clearSelection)
                {
                    if (m_VerticalScrollbar == null)
                        ReleaseSelection();
                }

                if (inputSystem != null)
                    inputSystem.imeCompositionMode = IMECompositionMode.Auto;

				m_IsKeyboardBeingClosedInHoloLens = false;
            }

            MarkGeometryAsDirty();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            DeactivateInputField();

            base.OnDeselect(eventData);
            SendOnFocusLost();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (!isFocused)
                m_ShouldActivateNextUpdate = true;

            SendOnSubmit();
            DeactivateInputField();
            eventData?.Use();
        }

        public virtual void OnCancel(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (!isFocused)
                m_ShouldActivateNextUpdate = true;

            m_WasCanceled = true;
            DeactivateInputField();
            eventData.Use();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!m_AllowInput)
                base.OnMove(eventData);
        }

        private void EnforceContentType()
        {
            switch (contentType)
            {
                case ContentType.Standard:
                    {
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.None;
                        break;
                    }
                case ContentType.Autocorrected:
                    {
                        m_InputType = InputType.AutoCorrect;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.None;
                        break;
                    }
                case ContentType.IntegerNumber:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        m_CharacterValidation = CharacterValidation.Integer;
                        break;
                    }
                case ContentType.DecimalNumber:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        m_CharacterValidation = CharacterValidation.Decimal;
                        break;
                    }
                case ContentType.Alphanumeric:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.ASCIICapable;
                        m_CharacterValidation = CharacterValidation.Alphanumeric;
                        break;
                    }
                case ContentType.Name:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.Name;
                        break;
                    }
                case ContentType.EmailAddress:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.EmailAddress;
                        m_CharacterValidation = CharacterValidation.EmailAddress;
                        break;
                    }
                case ContentType.Password:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Password;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.None;
                        break;
                    }
                case ContentType.Pin:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Password;
                        m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                        m_CharacterValidation = CharacterValidation.Digit;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            SetTextComponentWrapMode();
        }

        private void SetTextComponentWrapMode()
        {
            if (m_TextComponent == null)
                return;

            if (multiLine)
                m_TextComponent.textWrappingMode = TextWrappingModes.Normal;
            else
                m_TextComponent.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        }

        private void SetTextComponentRichTextMode()
        {
            if (m_TextComponent == null)
                return;

            m_TextComponent.richText = m_RichText;
        }

        private void SetToCustomIfContentTypeIsNot(params ContentType[] allowedContentTypes)
        {
            if (contentType == ContentType.Custom)
                return;

            for (int i = 0; i < allowedContentTypes.Length; i++)
                if (contentType == allowedContentTypes[i])
                    return;

            contentType = ContentType.Custom;
        }

        private void SetToCustom()
        {
            if (contentType == ContentType.Custom)
                return;

            contentType = ContentType.Custom;
        }

        private void SetToCustom(CharacterValidation characterValidation)
        {
            if (contentType == ContentType.Custom)
            {
                characterValidation = CharacterValidation.CustomValidator;
                return;
            }

            contentType = ContentType.Custom;
            characterValidation = CharacterValidation.CustomValidator;
        }


        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (m_HasDoneFocusTransition)
                state = SelectionState.Selected;
            else if (state == SelectionState.Pressed)
                m_HasDoneFocusTransition = true;

            base.DoStateTransition(state, instant);
        }


        public virtual void CalculateLayoutInputHorizontal()
        { }

        public virtual void CalculateLayoutInputVertical()
        { }

        public virtual float minWidth { get { return 0; } }

        public virtual float preferredWidth
        {
            get
            {
                if (textComponent == null)
                    return 0;

                float horizontalPadding = 0;

                if (m_LayoutGroup != null)
                    horizontalPadding = m_LayoutGroup.padding.horizontal;

                if (m_TextViewport != null)
                    horizontalPadding += m_TextViewport.offsetMin.x - m_TextViewport.offsetMax.x;

                return m_TextComponent.preferredWidth + horizontalPadding;
            }
        }

        public virtual float flexibleWidth { get { return -1; } }

        public virtual float minHeight { get { return 0; } }

        public virtual float preferredHeight
        {
            get
            {
                if (textComponent == null)
                    return 0;

                float verticalPadding = 0;

                if (m_LayoutGroup != null)
                    verticalPadding = m_LayoutGroup.padding.vertical;

                if (m_TextViewport != null)
                    verticalPadding += m_TextViewport.offsetMin.y - m_TextViewport.offsetMax.y;

                return m_TextComponent.preferredHeight + verticalPadding;
            }
        }

        public virtual float flexibleHeight { get { return -1; } }

        public virtual int layoutPriority { get { return 1; } }


        /// <param name="pointSize"></param>
        public void SetGlobalPointSize(float pointSize)
        {
            TMPText placeholderTextComponent = m_Placeholder as TMPText;

            if (placeholderTextComponent != null)
                placeholderTextComponent.fontSize = pointSize;

            textComponent.fontSize = pointSize;
        }

        /// <param name="fontAsset"></param>
        public void SetGlobalFontAsset(TMP_FontAsset fontAsset)
        {
            TMPText placeholderTextComponent = m_Placeholder as TMPText;

            if (placeholderTextComponent != null)
                placeholderTextComponent.font = fontAsset;

            textComponent.font = fontAsset;
        }

    }


    internal static class SetPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetEquatableStruct<T>(ref T currentValue, T newValue) where T : IEquatable<T>
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}
