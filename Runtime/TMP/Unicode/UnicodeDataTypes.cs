// Runtime/Unicode/UnicodeDataTypes.cs

#nullable enable


/// <summary>
/// Direction of a paragraph or a line in visual/logical terms.
/// This is NOT a direct Unicode property; it is a convenience enum for the layout engine.
/// </summary>
public enum BidiDirection : byte
{
    LeftToRight = 0,
    RightToLeft = 1
}

/// <summary>
/// Unicode Bidi_Class property values (bc), long names.
/// See UAX #9 and UCD property Bidi_Class.
/// </summary>
public enum BidiClass : byte
{
    // Strong types
    LeftToRight, // L
    RightToLeft, // R
    ArabicLetter, // AL

    // Weak types
    EuropeanNumber, // EN
    EuropeanSeparator, // ES
    EuropeanTerminator, // ET
    ArabicNumber, // AN
    CommonSeparator, // CS
    NonspacingMark, // NSM

    // Neutral types
    BoundaryNeutral, // BN
    ParagraphSeparator, // B
    SegmentSeparator, // S
    WhiteSpace, // WS
    OtherNeutral, // ON

    // Explicit formatting
    LeftToRightEmbedding, // LRE
    LeftToRightOverride, // LRO
    RightToLeftEmbedding, // RLE
    RightToLeftOverride, // RLO
    PopDirectionalFormat, // PDF

    // Isolates
    LeftToRightIsolate, // LRI
    RightToLeftIsolate, // RLI
    FirstStrongIsolate, // FSI
    PopDirectionalIsolate // PDI
}

/// <summary>
/// Unicode Bidi_Paired_Bracket_Type values (bpt).
/// </summary>
public enum BidiPairedBracketType : byte
{
    None, // n
    Open, // o
    Close // c
}

/// <summary>
/// Unicode Joining_Type property values (jt).
/// </summary>
public enum JoiningType : byte
{
    NonJoining, // U
    Transparent, // T
    JoinCausing, // C
    LeftJoining, // L
    RightJoining, // R
    DualJoining // D
}

/// <summary>
/// Unicode Joining_Group property values (jg).
/// Full list from the UCD property definition.
/// </summary>
public enum JoiningGroup : byte
{
    NoJoiningGroup,

    AfricanFeh,
    AfricanNoon,
    AfricanQaf,
    Ain,
    Alaph,
    Alef,
    Beh,
    Beth,
    BurushaskiYehBarree,
    Dal,
    DalathRish,
    E,
    FarsiYeh,
    Fe,
    Feh,
    FinalSemkath,
    Gaf,
    Gamal,
    Hah,
    HanifiRohingyaKinnaYa,
    HanifiRohingyaPa,
    He,
    Heh,
    HehGoal,
    Heth,
    Kaf,
    Kaph,
    KashmiriYeh,
    Khaph,
    KnottedHeh,
    Lam,
    Lamadh,

    // Malayalam joining groups (Syriac extensions)
    MalayalamBha,
    MalayalamJa,
    MalayalamLla,
    MalayalamLlla,
    MalayalamNga,
    MalayalamNna,
    MalayalamNnna,
    MalayalamNya,
    MalayalamRa,
    MalayalamSsa,
    MalayalamTta,

    // Manichaean joining groups
    ManichaeanAleph,
    ManichaeanAyin,
    ManichaeanBeth,
    ManichaeanDaleth,
    ManichaeanDhamedh,
    ManichaeanFive,
    ManichaeanGimel,
    ManichaeanHeth,
    ManichaeanHundred,
    ManichaeanKaph,
    ManichaeanLamedh,
    ManichaeanMem,
    ManichaeanNun,
    ManichaeanOne,
    ManichaeanPe,
    ManichaeanQoph,
    ManichaeanResh,
    ManichaeanSadhe,
    ManichaeanSamekh,
    ManichaeanTaw,
    ManichaeanTen,
    ManichaeanTeth,
    ManichaeanThamedh,
    ManichaeanTwenty,
    ManichaeanWaw,
    ManichaeanYodh,
    ManichaeanZayin,

    Meem,
    Mim,
    Noon,
    Nun,
    Nya,
    Pe,
    Qaf,
    Qaph,
    Reh,
    ReversedPe,
    RohingyaYeh,
    Sad,
    Sadhe,
    Seen,
    Semkath,
    Shin,
    StraightWaw,
    SwashKaf,
    SyriacWaw,
    Tah,
    Taw,
    TehMarbuta,
    TehMarbutaGoal,
    HamzaOnHehGoal,
    Teth,
    ThinYeh,
    VerticalTail,
    Waw,
    Yeh,
    YehBarree,
    YehWithTail,
    Yudh,
    YudhHe,
    Zain,
    Zhain
}

/// <summary>
/// Core abstraction over Unicode character properties used by the Bidi engine
/// and the Arabic shaping engine. Implementations are expected to cover
/// the entire range of Unicode scalar values (U+0000..U+10FFFF).
/// </summary>
public interface IUnicodeDataProvider
{
    /// <summary>
    /// Returns the Unicode Bidi_Class property for the given code point.
    /// </summary>
    BidiClass GetBidiClass(int codePoint);

    /// <summary>
    /// Returns whether the given code point has Bidi_Mirrored = Yes.
    /// </summary>
    bool IsBidiMirrored(int codePoint);

    /// <summary>
    /// Returns the Bidi_Mirroring_Glyph for the given code point, if defined.
    /// If there is no mirroring glyph, implementors may return the input code point.
    /// </summary>
    int GetBidiMirroringGlyph(int codePoint);

    /// <summary>
    /// Returns the Unicode Bidi_Paired_Bracket_Type for the given code point.
    /// </summary>
    BidiPairedBracketType GetBidiPairedBracketType(int codePoint);

    /// <summary>
    /// Returns the Unicode Bidi_Paired_Bracket for the given code point.
    /// If there is no paired bracket, implementors may return the input code point.
    /// </summary>
    int GetBidiPairedBracket(int codePoint);

    /// <summary>
    /// Returns the Unicode Joining_Type property for the given code point.
    /// This is required for Arabic (and related) cursive shaping.
    /// </summary>
    JoiningType GetJoiningType(int codePoint);

    /// <summary>
    /// Returns the Unicode Joining_Group property for the given code point.
    /// Characters not explicitly listed in ArabicShaping / DerivedJoiningGroup
    /// must be treated as No_Joining_Group.
    /// </summary>
    JoiningGroup GetJoiningGroup(int codePoint);
}