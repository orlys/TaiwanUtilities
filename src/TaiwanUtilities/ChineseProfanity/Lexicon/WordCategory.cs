namespace TaiwanUtilities.Internals;

using System;

[Flags]
internal enum WordCategory
{
    None = 0,

    /// <summary>
    /// 髒話動詞：肏、操、幹、姦、草、淦、贛、耖、襙、鄵、糙、駛、賽
    /// </summary>
    ProfaneVerb = 1,

    /// <summary>
    /// 人稱代詞：你、妳、祢、汝、他、她、它、牠、祂、恁、琳、您、林
    /// </summary>
    Pronoun = 1 << 1,

    /// <summary>
    /// 親屬詞：媽、娘、爸、爹、老母、祖宗、奶奶、姑媽...
    /// </summary>
    Kinship = 1 << 2,

    /// <summary>
    /// 身體/性器官詞：屄、逼、B、屌、鳥、雞巴、機掰、鮑魚...
    /// </summary>
    BodyPart = 1 << 3,

    /// <summary>
    /// 貶義前綴形容：死、臭、破、賤、爛
    /// </summary>
    PejorativePrefix = 1 << 4,

    /// <summary>
    /// 貶義名詞（獨立成詞）：畜生、廢物、垃圾、婊子、賤人...
    /// </summary>
    Slur = 1 << 5,

    /// <summary>
    /// 感嘆/語助：的、勒、啊、了個
    /// </summary>
    Particle = 1 << 6,

    /// <summary>
    /// 輕度感嘆詞（需上下文判斷）：靠、滾
    /// </summary>
    MildExpletive = 1 << 7,
}