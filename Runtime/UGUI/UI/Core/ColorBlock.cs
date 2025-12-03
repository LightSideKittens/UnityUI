using System;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [Serializable]
    public struct ColorBlock : IEquatable<ColorBlock>
    {
        [FormerlySerializedAs("normalColor")]
        [SerializeField]
        private Color m_NormalColor;

        [FormerlySerializedAs("highlightedColor")]
        [SerializeField]
        private Color m_HighlightedColor;

        [FormerlySerializedAs("pressedColor")]
        [SerializeField]
        private Color m_PressedColor;

        [FormerlySerializedAs("m_HighlightedColor")]
        [SerializeField]
        private Color m_SelectedColor;

        [FormerlySerializedAs("disabledColor")]
        [SerializeField]
        private Color m_DisabledColor;

        [Range(1, 5)]
        [SerializeField]
        private float m_ColorMultiplier;

        [FormerlySerializedAs("fadeDuration")]
        [SerializeField]
        private float m_FadeDuration;

        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///     public Color newColor;
        ///
        ///     void Start()
        ///     {
        ///         //Changes the button's Normal color to the new color.
        ///         ColorBlock cb = button.colors;
        ///         cb.normalColor = newColor;
        ///         button.colors = cb;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Color normalColor       { get { return m_NormalColor; } set { m_NormalColor = value; } }

        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///     public Color newColor;
        ///
        ///     void Start()
        ///     {
        ///         //Changes the button's Highlighted color to the new color.
        ///         ColorBlock cb = button.colors;
        ///         cb.highlightedColor = newColor;
        ///         button.colors = cb;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Color highlightedColor  { get { return m_HighlightedColor; } set { m_HighlightedColor = value; } }

        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///     public Color newColor;
        ///
        ///     void Start()
        ///     {
        ///         //Changes the button's Pressed color to the new color.
        ///         ColorBlock cb = button.colors;
        ///         cb.pressedColor = newColor;
        ///         button.colors = cb;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Color pressedColor      { get { return m_PressedColor; } set { m_PressedColor = value; } }

        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///     public Color newColor;
        ///
        ///     void Start()
        ///     {
        ///         //Changes the button's Selected color to the new color.
        ///         ColorBlock cb = button.colors;
        ///         cb.selectedColor = newColor;
        ///         button.colors = cb;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Color selectedColor     { get { return m_SelectedColor; } set { m_SelectedColor = value; } }

        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///     public Color newColor;
        ///
        ///     void Start()
        ///     {
        ///         //Changes the button's Disabled color to the new color.
        ///         ColorBlock cb = button.colors;
        ///         cb.disabledColor = newColor;
        ///         button.colors = cb;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Color disabledColor     { get { return m_DisabledColor; } set { m_DisabledColor = value; } }

        public float colorMultiplier   { get { return m_ColorMultiplier; } set { m_ColorMultiplier = value; } }

        public float fadeDuration      { get { return m_FadeDuration; } set { m_FadeDuration = value; } }

        public static ColorBlock defaultColorBlock;

        static ColorBlock()
        {
            defaultColorBlock = new ColorBlock
            {
                m_NormalColor      = new Color32(255, 255, 255, 255),
                m_HighlightedColor = new Color32(245, 245, 245, 255),
                m_PressedColor     = new Color32(200, 200, 200, 255),
                m_SelectedColor    = new Color32(245, 245, 245, 255),
                m_DisabledColor    = new Color32(200, 200, 200, 128),
                colorMultiplier    = 1.0f,
                fadeDuration       = 0.1f
            };
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColorBlock))
                return false;

            return Equals((ColorBlock)obj);
        }

        public bool Equals(ColorBlock other)
        {
            return normalColor == other.normalColor &&
                highlightedColor == other.highlightedColor &&
                pressedColor == other.pressedColor &&
                selectedColor == other.selectedColor &&
                disabledColor == other.disabledColor &&
                colorMultiplier == other.colorMultiplier &&
                fadeDuration == other.fadeDuration;
        }

        public static bool operator==(ColorBlock point1, ColorBlock point2)
        {
            return point1.Equals(point2);
        }

        public static bool operator!=(ColorBlock point1, ColorBlock point2)
        {
            return !point1.Equals(point2);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
