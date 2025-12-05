#nullable enable


public enum BidiDirection : byte
{
    LeftToRight = 0,
    RightToLeft = 1
}


public enum BidiClass : byte
{
    LeftToRight,
    RightToLeft,
    ArabicLetter,

    EuropeanNumber,
    EuropeanSeparator,
    EuropeanTerminator,
    ArabicNumber,
    CommonSeparator,
    NonspacingMark,

    BoundaryNeutral,
    ParagraphSeparator,
    SegmentSeparator,
    WhiteSpace,
    OtherNeutral,

    LeftToRightEmbedding,
    LeftToRightOverride,
    RightToLeftEmbedding,
    RightToLeftOverride,
    PopDirectionalFormat,

    LeftToRightIsolate,
    RightToLeftIsolate,
    FirstStrongIsolate,
    PopDirectionalIsolate
}


public enum BidiPairedBracketType : byte
{
    None,
    Open,
    Close
}


public enum JoiningType : byte
{
    NonJoining,
    Transparent,
    JoinCausing,
    LeftJoining,
    RightJoining,
    DualJoining
}


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


public interface IUnicodeDataProvider
{
    BidiClass GetBidiClass(int codePoint);


    bool IsBidiMirrored(int codePoint);


    int GetBidiMirroringGlyph(int codePoint);


    BidiPairedBracketType GetBidiPairedBracketType(int codePoint);


    int GetBidiPairedBracket(int codePoint);


    JoiningType GetJoiningType(int codePoint);


    JoiningGroup GetJoiningGroup(int codePoint);
}