using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


internal class TMP_MarkupTagUpdateUtility
{
    struct MarkupTagDescriptor
    {
        public string name;
        public string tag;
        public string description;

        public MarkupTagDescriptor(string name, string tag, string description)
        {
            this.name = name;
            this.tag = tag;
            this.description = description;
        }

        public MarkupTagDescriptor(string name)
        {
            this.name = name;
            this.tag = null;
            this.description = null;
        }

        public static MarkupTagDescriptor linefeed = new("\n");
    }

    private static MarkupTagDescriptor[] m_MarkupTags =
    {
        new("BOLD", "b", "// <b>"),
        new("SLASH_BOLD", "/b", "// </b>"),
        new("ITALIC", "i", "// <i>"),
        new("SLASH_ITALIC", "/i", "// </i>"),
        new("UNDERLINE", "u", "// <u>"),
        new("SLASH_UNDERLINE", "/u", "// </u>"),
        new("STRIKETHROUGH", "s", "// <s>"),
        new("SLASH_STRIKETHROUGH", "/s", "// </s>"),
        new("SUBSCRIPT", "sub", "// <sub>"),
        new("SLASH_SUBSCRIPT", "/sub", "// </sub>"),
        new("SUPERSCRIPT", "sup", "// <sup>"),
        new("SLASH_SUPERSCRIPT", "/sup", "// </sup>"),
        new("MARK", "mark", "// <mark>"),
        new("SLASH_MARK", "/mark", "// </mark>"),
        MarkupTagDescriptor.linefeed,

        new("COLOR", "color", "// <color>"),
        new("SLASH_COLOR", "/color", "// </color>"),
        new("ALPHA", "alpha", "// <alpha>"),
        new("SLASH_ALPHA", "/alpha", "// </alpha>"),
        MarkupTagDescriptor.linefeed,

        new("FONT", "font", "// <font=\"Name of Font Asset\"> or <font family=\"Arial\" style=\"Regular\">"),
        new("SLASH_FONT", "/font", "// </font>"),
        new("MATERIAL", "material",
            "// <material=\"Name of Material Preset\"> or as attribute <font=\"Name of font asset\" material=\"Name of material\">"),
        new("SLASH_MATERIAL", "/material", "// </material>"),
        new("SIZE", "size", "// <size>"),
        new("SLASH_SIZE", "/size", "// </size>"),
        new("FONT_WEIGHT", "font-weight", "// <font-weight>"),
        new("SLASH_FONT_WEIGHT", "/font-weight", "// </font-weight>"),
        new("SCALE", "scale", "// <scale>"),
        new("SLASH_SCALE", "/scale", "// </scale>"),
        MarkupTagDescriptor.linefeed,

        new("SPRITE", "sprite", "// <sprite>"),
        new("STYLE", "style", "// <style>"),
        new("SLASH_STYLE", "/style", "// </style>"),
        new("GRADIENT", "gradient", "// <gradient>"),
        new("SLASH_GRADIENT", "/gradient", "// </gradient>"),
        MarkupTagDescriptor.linefeed,

        new("A", "a", "// <a>"),
        new("SLASH_A", "/a", "// </a>"),
        new("LINK", "link", "// <link>"),
        new("SLASH_LINK", "/link", "// </link>"),
        MarkupTagDescriptor.linefeed,

        new("POSITION", "pos", "// <pos>"),
        new("SLASH_POSITION", "/pos", "// </pos>"),
        new("VERTICAL_OFFSET", "voffset", "// <voffset>"),
        new("SLASH_VERTICAL_OFFSET", "/voffset", "// </voffset>"),
        new("ROTATE", "rotate", "// <rotate>"),
        new("SLASH_ROTATE", "/rotate", "// </rotate>"),
        new("TRANSFORM", "transform", "// <transform=\"position, rotation, scale\">"),
        new("SLASH_TRANSFORM", "/transform", "// </transform>"),
        new("SPACE", "space", "// <space>"),
        new("SLASH_SPACE", "/space", "// </space>"),
        new("CHARACTER_SPACE", "cspace", "// <cspace>"),
        new("SLASH_CHARACTER_SPACE", "/cspace", "// </cspace>"),
        new("MONOSPACE", "mspace", "// <mspace>"),
        new("SLASH_MONOSPACE", "/mspace", "// </mspace>"),
        new("CHARACTER_SPACING", "character-spacing", "// <character-spacing>"),
        new("SLASH_CHARACTER_SPACING", "/character-spacing", "// </character-spacing>"),
        MarkupTagDescriptor.linefeed,

        new("ALIGN", "align", "// <align>"),
        new("SLASH_ALIGN", "/align", "// </align>"),
        new("WIDTH", "width", "// <width>"),
        new("SLASH_WIDTH", "/width", "// </width>"),
        new("MARGIN", "margin", "// <margin>"),
        new("SLASH_MARGIN", "/margin", "// </margin>"),
        new("MARGIN_LEFT", "margin-left", "// <margin-left>"),
        new("MARGIN_RIGHT", "margin-right", "// <margin-right>"),
        new("INDENT", "indent", "// <indent>"),
        new("SLASH_INDENT", "/indent", "// </indent>"),
        new("LINE_INDENT", "line-indent", "// <line-indent>"),
        new("SLASH_LINE_INDENT", "/line-indent", "// </line-indent>"),
        new("LINE_HEIGHT", "line-height", "// <line-height>"),
        new("SLASH_LINE_HEIGHT", "/line-height", "// </line-height>"),
        MarkupTagDescriptor.linefeed,

        new("NO_BREAK", "nobr", "// <nobr>"),
        new("SLASH_NO_BREAK", "/nobr", "// </nobr>"),
        new("NO_PARSE", "noparse", "// <noparse>"),
        new("SLASH_NO_PARSE", "/noparse", "// </noparse>"),
        new("PAGE", "page", "// <page>"),
        new("SLASH_PAGE", "/page", "// </page>"),
        MarkupTagDescriptor.linefeed,

        new("ACTION", "action", "// <action>"),
        new("SLASH_ACTION", "/action", "// </action>"),
        MarkupTagDescriptor.linefeed,

        new("CLASS", "class", "// <class>"),
        new("TABLE", "table", "// <table>"),
        new("SLASH_TABLE", "/table", "// </table>"),
        new("TH", "th", "// <th>"),
        new("SLASH_TH", "/th", "// </th>"),
        new("TR", "tr", "// <tr>"),
        new("SLASH_TR", "/tr", "// </tr>"),
        new("TD", "td", "// <td>"),
        new("SLASH_TD", "/td", "// </td>"),
        MarkupTagDescriptor.linefeed,

        new("// Text Styles"),
        new("LOWERCASE", "lowercase", "// <lowercase>"),
        new("SLASH_LOWERCASE", "/lowercase", "// </lowercase>"),
        new("ALLCAPS", "allcaps", "// <allcaps>"),
        new("SLASH_ALLCAPS", "/allcaps", "// </allcaps>"),
        new("UPPERCASE", "uppercase", "// <uppercase>"),
        new("SLASH_UPPERCASE", "/uppercase", "// </uppercase>"),
        new("SMALLCAPS", "smallcaps", "// <smallcaps>"),
        new("SLASH_SMALLCAPS", "/smallcaps", "// </smallcaps>"),
        new("CAPITALIZE", "capitalize", "// <capitalize>"),
        new("SLASH_CAPITALIZE", "/capitalize", "// </capitalize>"),
        MarkupTagDescriptor.linefeed,

        new("// Font Features"),
        new("LIGA", "liga", "// <liga>"),
        new("SLASH_LIGA", "/liga", "// </liga>"),
        new("FRAC", "frac", "// <frac>"),
        new("SLASH_FRAC", "/frac", "// </frac>"),
        MarkupTagDescriptor.linefeed,

        new("// Attributes"),
        new("NAME", "name", "// <sprite name=\"Name of Sprite\">"),
        new("INDEX", "index", "// <sprite index=7>"),
        new("TINT", "tint", "// <tint=bool>"),
        new("ANIM", "anim", "// <anim=\"first frame, last frame, frame rate\">"),
        new("HREF", "href", "// <a href=\"url\">text to be displayed.</a>"),
        new("ANGLE", "angle", "// <i angle=\"40\">Italic Slant Angle</i>"),
        new("FAMILY", "family", "// <font family=\"Arial\">"),
        MarkupTagDescriptor.linefeed,

        new("// Named Colors"),
        new("RED", "red", ""),
        new("GREEN", "green", ""),
        new("BLUE", "blue", ""),
        new("WHITE", "white", ""),
        new("BLACK", "black", ""),
        new("CYAN", "cyna", ""),
        new("MAGENTA", "magenta", ""),
        new("YELLOW", "yellow", ""),
        new("ORANGE", "orange", ""),
        new("PURPLE", "purple", ""),
        MarkupTagDescriptor.linefeed,

        new("// Unicode Characters"),
        new("BR", "br", "// <br> Line Feed (LF) \\u0A"),
        new("ZWSP", "zwsp", "// <zwsp> Zero Width Space \\u200B"),
        new("NBSP", "nbsp", "// <nbsp> Non Breaking Space \\u00A0"),
        new("SHY", "shy", "// <shy> Soft Hyphen \\u00AD"),
        new("ZWJ", "zwj", "// <zwj> Zero Width Joiner \\u200D"),
        new("WJ", "wj", "// <wj> Word Joiner \\u2060"),
        MarkupTagDescriptor.linefeed,

        new("// Alignment"),
        new("LEFT", "left", "// <align=left>"),
        new("RIGHT", "right", "// <align=right>"),
        new("CENTER", "center", "// <align=center>"),
        new("JUSTIFIED", "justified", "// <align=justified>"),
        new("FLUSH", "flush", "// <align=flush>"),
        MarkupTagDescriptor.linefeed,

        new("// Prefix and Unit suffix"),
        new("NONE", "none", ""),
        new("PLUS", "+", ""),
        new("MINUS", "-", ""),
        new("PX", "px", ""),
        new("PLUS_PX", "+px", ""),
        new("MINUS_PX", "-px", ""),
        new("EM", "em", ""),
        new("PLUS_EM", "+em", ""),
        new("MINUS_EM", "-em", ""),
        new("PCT", "pct", ""),
        new("PLUS_PCT", "+pct", ""),
        new("MINUS_PCT", "-pct", ""),
        new("PERCENTAGE", "%", ""),
        new("PLUS_PERCENTAGE", "+%", ""),
        new("MINUS_PERCENTAGE", "-%", ""),
        new("HASH", "#", "// #"),
        MarkupTagDescriptor.linefeed,

        new("TRUE", "true", ""),
        new("FALSE", "false", ""),
        MarkupTagDescriptor.linefeed,

        new("INVALID", "invalid", ""),
        MarkupTagDescriptor.linefeed,

        new("NORMAL", "normal", "// <style=\"Normal\">"),
        new("DEFAULT", "default", "// <font=\"Default\">"),
    };


    [MenuItem("Window/TextMeshPro/Internal/Update Markup Tag Hash Codes", false, 2200, true)]
    static void UpdateMarkupTagHashCodes()
    {
        Dictionary<int, MarkupTagDescriptor> markupHashCodes = new Dictionary<int, MarkupTagDescriptor>();
        string output = string.Empty;

        for (int i = 0; i < m_MarkupTags.Length; i++)
        {
            MarkupTagDescriptor descriptor = m_MarkupTags[i];
            int hashCode = descriptor.tag == null ? 0 : GetHashCodeCaseInSensitive(descriptor.tag);

            if (descriptor.name == "\n")
                output += "\n";
            else if (hashCode == 0)
                output += descriptor.name + "\n";
            else
            {
                output += descriptor.name + " = " + hashCode + ",\t" + descriptor.description + "\n";

                if (markupHashCodes.ContainsKey(hashCode) == false)
                    markupHashCodes.Add(hashCode, descriptor);
                else
                    Debug.Log("[" + descriptor.name + "] with HashCode [" + hashCode + "] collides with [" +
                              markupHashCodes[hashCode].name + "].");
            }
        }

        Debug.Log(output);
    }

    const string k_lookupStringU =
        "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";

    public static char ToUpperFast(char c)
    {
        if (c > k_lookupStringU.Length - 1)
            return c;

        return k_lookupStringU[c];
    }

    public static int GetHashCodeCaseInSensitive(string s)
    {
        int hashCode = 5381;

        for (int i = 0; i < s.Length; i++)
            hashCode = (hashCode << 5) + hashCode ^ ToUpperFast(s[i]);

        return hashCode;
    }
}