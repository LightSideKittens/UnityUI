using System.Collections.Generic;
using UnityEngine;

namespace TMPro
{
    /// <summary>
    /// Inert stand-in for TextMeshPro's <c>TMP_Asset</c> base. Exists only so packages that
    /// reference TextMeshPro compile against this TMP-free Unity UI fork; it stores nothing and
    /// does nothing. The fork's text solution is UniText, not TextMeshPro.
    /// </summary>
    public abstract class TMP_Asset : ScriptableObject { }

    /// <summary>
    /// Compatibility stub for <c>TMPro.TMP_FontAsset</c>. Holds no font data — present so consumers
    /// such as Unity Localization's <c>LocalizedTmpFont</c> resolve when real TextMeshPro is removed.
    /// </summary>
    public class TMP_FontAsset : TMP_Asset { }

    /// <summary>
    /// Compatibility stub for <c>TMPro.TMP_SpriteAsset</c>. Compile-time placeholder with no data.
    /// </summary>
    public class TMP_SpriteAsset : TMP_Asset { }

    /// <summary>
    /// Inert base stub for the TextMeshPro text components. Exposes the members packages commonly
    /// reference at compile time; every one of them is a no-op at runtime.
    /// </summary>
    [AddComponentMenu("")]
    public abstract class TMP_Text : MonoBehaviour
    {
        /// <summary>The displayed string. Stored, but never rendered by this stub.</summary>
        public virtual string text { get; set; }

        /// <summary>Font size. Ignored by this stub.</summary>
        public float fontSize { get; set; }

        /// <summary>Vertex color. Ignored by this stub.</summary>
        public Color color { get; set; } = Color.white;

        /// <summary>Font asset. Ignored by this stub.</summary>
        public TMP_FontAsset font { get; set; }
    }

    /// <summary>
    /// Compatibility stub for <c>TMPro.TextMeshProUGUI</c>, the uGUI (Canvas) text component.
    /// </summary>
    [AddComponentMenu("")]
    public class TextMeshProUGUI : TMP_Text { }

    /// <summary>
    /// Compatibility stub for <c>TMPro.TextMeshPro</c>, the world-space (MeshRenderer) text component.
    /// </summary>
    [AddComponentMenu("")]
    public class TextMeshPro : TMP_Text { }

    /// <summary>
    /// Compatibility stub for <c>TMPro.TMP_InputField</c>.
    /// </summary>
    [AddComponentMenu("")]
    public class TMP_InputField : MonoBehaviour
    {
        /// <summary>The field contents. Stored, but inert in this stub.</summary>
        public string text { get; set; }
    }

    /// <summary>
    /// Compatibility stub for <c>TMPro.TMP_Dropdown</c>. The option methods exist so reflection-based
    /// callers (such as Unity Localization's localize menus) bind to them; all are no-ops here.
    /// </summary>
    [AddComponentMenu("")]
    public class TMP_Dropdown : MonoBehaviour
    {
        /// <summary>Mirror of TMP's nested option type, carrying a label only.</summary>
        public class OptionData
        {
            /// <summary>Option label.</summary>
            public string text;

            /// <summary>Creates an empty option.</summary>
            public OptionData() { }

            /// <summary>Creates an option with the given label.</summary>
            public OptionData(string text) { this.text = text; }
        }

        /// <summary>Selected option index. Inert in this stub.</summary>
        public int value { get; set; }

        /// <summary>No-op stand-in for <c>TMP_Dropdown.RefreshShownValue</c>.</summary>
        public void RefreshShownValue() { }

        /// <summary>No-op stand-in for <c>TMP_Dropdown.ClearOptions</c>.</summary>
        public void ClearOptions() { }

        /// <summary>No-op stand-in for <c>TMP_Dropdown.AddOptions(List&lt;string&gt;)</c>.</summary>
        public void AddOptions(List<string> options) { }

        /// <summary>No-op stand-in for <c>TMP_Dropdown.AddOptions(List&lt;OptionData&gt;)</c>.</summary>
        public void AddOptions(List<OptionData> options) { }
    }
}
