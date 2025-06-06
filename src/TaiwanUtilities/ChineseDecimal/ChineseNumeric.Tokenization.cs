namespace TaiwanUtilities;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;

partial struct ChineseNumeric
{
    internal static class InvalidToken
    {
        private static FormatException Create(
            char character,
            int index,
            string reason)
        {
            return new FormatException($"Invalid character '{character}' at index {index}, reason: {reason}");
        }

        internal static FormatException UnknownCharacter(
            char character,
            int index)
        {
            return Create(character, index, "Unknown character");
        }

        internal static FormatException SegmentOverflow(
            Token character,
            int index)
        {
            return Create(character.Value, index, "Segment overflow");
        }

        internal static FormatException InvalidUnitPosition(
            Token character,
            int index)
        {
            return Create(character.Value, index, "Invalid unit position");
        }

    }
    internal record Token(char Value, int Index, Character Character)
    {
        public static List<Token>? Tokenize(ReadOnlySpan<char> str, bool throwError)
        {
            // 檢查空字串
            if (str.IsWhiteSpace())
            {
                if (throwError)
                {
                    throw new ArgumentNullException(nameof(str));
                }
                else
                {
                    return null;
                }
            }

            // 檢查是否包含無效字元
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (!Character.Contains(c))
                {
                    if (throwError)
                    {
                        throw InvalidToken.UnknownCharacter(c, i);
                    }
                    else
                    {
                        return null;
                    }
                }
            }


            var list = new List<Token>(str.Length);
            var previous = default(Token);

            for (var index = 0; index < str.Length; index++)
            {
                var character = str[index];

                var token = Character.Get(character);

                var current = new Token(character, index, token);

                current.Previous = previous;
                if (previous is { } p)
                {
                    p.Next = current;
                }
                previous = current;

                list.Add(current);
            }

            return list;
        }


        public Token Previous { get; internal set; }

        public Token Next { get; internal set; }

        /// <summary>
        /// 判斷是否為未知。
        /// </summary> 
        /// <returns></returns>
        public bool IsUnknown => Character.IsKindOf(CharacterKind.Unknown);

        /// <summary>
        /// 判斷是否為零。
        /// </summary> 
        /// <returns></returns>
        public bool IsZero => Character.IsKindOf(CharacterKind.Zero);

        /// <summary>
        /// 判斷是否為零到九(不包含零)。
        /// </summary> 
        /// <returns></returns>
        public bool IsDigit => Character.IsKindOf(CharacterKind.Digit);

        /// <summary>
        /// 判斷是否為一到九。
        /// </summary> 
        /// <returns></returns>
        public bool IsUnit => Character.IsKindOf(CharacterKind.Unit);


        /// <summary>
        /// 判斷是否為複合字。
        /// </summary> 
        /// <returns></returns>
        public bool IsDuplex => Character.IsKindOf(CharacterKind.Duplex);


        /// <summary>
        /// 判斷是否為十百千。
        /// </summary> 
        /// <returns></returns>
        public bool IsTinyMultipler => Character.IsKindOf(CharacterKind.GroupTinyMultipler);

        /// <summary>
        /// 判斷是否為萬億兆京垓秭穰。
        /// </summary> 
        /// <returns></returns>
        public bool IsLargeMultiplier => Character.IsKindOf(CharacterKind.GroupMultipler);

        public override string ToString()
        {
            return $"[{Index}] = '{Value}'";
        }


        public static implicit operator decimal(Token character)
        {
            return character.Character.Value;
        }


        public static decimal operator *(Token left, decimal right)
        {
            return left.Character.Value * right;
        }

        public static decimal operator *(decimal left, Token right)
        {
            return left * right.Character.Value;
        }

        public static decimal operator *(Token left, Token right)
        {
            return left.Character.Value * right.Character.Value;
        }


        public static decimal operator +(Token left, decimal right)
        {
            return left.Character.Value + right;
        }

        public static decimal operator +(decimal left, Token right)
        {
            return left + right.Character.Value;
        }

        public static decimal operator +(Token left, Token right)
        {
            return left.Character.Value + right.Character.Value;
        }

        public static bool operator >(Token left, Token right)
        {
            return left.Character.Value > right.Character.Value;
        }
        public static bool operator <(Token left, Token right)
        {
            return left.Character.Value < right.Character.Value;
        }
    }

    [DebuggerDisplay("{ToString(),nc}")]
    internal sealed class Character
    {
        static Character()
        {
            Unknown = new(CharacterKind.Unknown, 0m);

            Zero = new(CharacterKind.Zero, 0m, '〇', '0', '零', '０', '零');

            One = new(CharacterKind.Unit, 1m, '一', '1', '壹', '１', '壱', '弌');
            Two = new(CharacterKind.Unit, 2m, '二', '2', '貳', '２', '贰', '兩', '弐', '貮');
            Three = new(CharacterKind.Unit, 3m, '三', '3', '參', '３', '参', '叁', '叄', '弎');
            Four = new(CharacterKind.Unit, 4m, '四', '4', '肆', '４', '䦉');
            Five = new(CharacterKind.Unit, 5m, '五', '5', '伍', '５');
            Six = new(CharacterKind.Unit, 6m, '六', '6', '陸', '６', '陆');
            Seven = new(CharacterKind.Unit, 7m, '七', '7', '柒', '７', '漆');
            Eight = new(CharacterKind.Unit, 8m, '八', '8', '捌', '８');
            Nine = new(CharacterKind.Unit, 9m, '九', '9', '玖', '９');

            Unit = new(CharacterKind.Unknown, 1m, char.MinValue);
            Ten = new(CharacterKind.Ten, 10m, '十', '拾', '什');
            Hundred = new(CharacterKind.Hundred, 100m, '百', '佰', '陌');
            Thousand = new(CharacterKind.Thousand, 1000m, '千', '仟', '阡');

            GroupUnit = new(CharacterKind.Unknown, 1m, char.MinValue);
            TenThousand = new(CharacterKind.GroupMultipler, value: 1_0000m, '萬', '万');
            HundredMillion = new(CharacterKind.GroupMultipler, value: 1_0000_0000m, '億', '亿');
            Trillion = new(CharacterKind.GroupMultipler, value: 1_0000_0000_0000m, '兆');
            TenQuadrillion = new(CharacterKind.GroupMultipler, value: 1_0000_0000_0000_0000m, '京');
            HundredQuintillion = new(CharacterKind.GroupMultipler, value: 1_0000_0000_0000_0000_0000m, '垓');
            Septillion = new(CharacterKind.GroupMultipler, value: 1_0000_0000_0000_0000_0000_0000m, '秭');
            TenOctillion = new(CharacterKind.GroupMultipler, value: 1_0000_0000_0000_0000_0000_0000_0000m, '穰');


            Unknown.Previous = Unknown;
            Unknown.Next = Unknown;

            Zero.Previous = Unknown;
            Zero.Next = Unknown;

            One.Previous = Unknown;
            One.Next = Two;
            Two.Previous = One;
            Two.Next = Three;
            Three.Previous = Two;
            Three.Next = Four;
            Four.Previous = Three;
            Four.Next = Five;
            Five.Previous = Four;
            Five.Next = Six;
            Six.Previous = Five;
            Six.Next = Seven;
            Seven.Previous = Six;
            Seven.Next = Eight;
            Eight.Previous = Seven;
            Eight.Next = Nine;
            Nine.Previous = Eight;
            Nine.Next = Unknown;


            Unit.Previous = Unknown;
            Unit.Next = Ten;
            Ten.Previous = Unit;
            Ten.Next = Hundred;
            Hundred.Previous = Ten;
            Hundred.Next = Thousand;
            Thousand.Previous = Hundred;
            Thousand.Next = Unknown;

            GroupUnit.Previous = Unknown;
            GroupUnit.Next = TenThousand;
            TenThousand.Previous = GroupUnit;
            TenThousand.Next = HundredMillion;
            HundredMillion.Previous = TenThousand;
            HundredMillion.Next = Trillion;
            Trillion.Previous = HundredMillion;
            Trillion.Next = TenQuadrillion;
            TenQuadrillion.Previous = Trillion;
            TenQuadrillion.Next = HundredQuintillion;
            HundredQuintillion.Previous = TenQuadrillion;
            HundredQuintillion.Next = Septillion;
            Septillion.Previous = HundredQuintillion;
            Septillion.Next = TenOctillion;
            TenOctillion.Previous = Septillion;
            TenOctillion.Next = Unknown;


            List = [
                Zero,
                One,
                Two,
                Three,
                Four,
                Five,
                Six,
                Seven,
                Eight,
                Nine,
                Ten,
                Hundred,
                Thousand,
                TenThousand,
                HundredMillion,
                Trillion,
                TenQuadrillion,
                HundredQuintillion,
                Septillion,
                TenOctillion,
            ];
            var numericTokens = new Dictionary<char, Character>();
            foreach (var token in List)
            {
                foreach (var c in token.CandidateList)
                {
                    numericTokens[c] = token;
                }
            }

            s_numericTokens = FrozenDictionary.ToFrozenDictionary(numericTokens);

        }
        /// <summary>
        /// 零
        /// </summary>
        public static Character Zero { get; }
        /// <summary>
        /// 一
        /// </summary>
        public static Character One { get; }
        /// <summary>
        /// 二
        /// </summary>
        public static Character Two { get; }
        /// <summary>
        /// 三
        /// </summary>
        public static Character Three { get; }
        /// <summary>
        /// 四
        /// </summary>
        public static Character Four { get; }
        /// <summary>
        /// 五
        /// </summary>
        public static Character Five { get; }
        /// <summary>
        /// 六
        /// </summary>
        public static Character Six { get; }
        /// <summary>
        /// 七
        /// </summary>
        public static Character Seven { get; }
        /// <summary>
        /// 八
        /// </summary>
        public static Character Eight { get; }
        /// <summary>
        /// 九
        /// </summary>
        public static Character Nine { get; }


        public static Character Unit { get; }

        /// <summary>
        /// 十
        /// </summary>
        public static Character Ten { get; }
        /// <summary>
        /// 百
        /// </summary>
        public static Character Hundred { get; }
        /// <summary>
        /// 千
        /// </summary>
        public static Character Thousand { get; }


        public static Character GroupUnit { get; }

        /// <summary>
        /// 萬
        /// </summary>
        public static Character TenThousand { get; }
        /// <summary>
        /// 億
        /// </summary>
        public static Character HundredMillion { get; }
        /// <summary>
        /// 兆
        /// </summary>
        public static Character Trillion { get; }
        /// <summary>
        /// 京
        /// </summary>
        public static Character TenQuadrillion { get; }
        /// <summary>
        /// 垓
        /// </summary>
        public static Character HundredQuintillion { get; }
        /// <summary>
        /// 秭
        /// </summary>
        public static Character Septillion { get; }
        /// <summary>
        /// 穰
        /// </summary>
        public static Character TenOctillion { get; }

        /// <summary>
        /// 表示未知的字元
        /// </summary>
        public static Character Unknown { get; }

        public static bool Contains(char character)
        {
            return s_numericTokens.ContainsKey(character);
        }


        public static Character Get(char character)
        {
            return s_numericTokens.GetValueOrDefault(character, Unknown);
        }

        private static readonly IReadOnlyDictionary<char, Character> s_numericTokens;

        public static IReadOnlyList<char> CharSet => (IReadOnlyList<char>)s_numericTokens.Keys;

        public static IReadOnlyList<Character> List { get; }

        public bool IsUnknown => IsKindOf(CharacterKind.Unknown);

        internal Character(CharacterKind kind, decimal value, params char[] characters)
        {
            Kind = kind;
            CandidateList = characters;
            Value = value;
        }

        public Character Next { get; private set; }

        public Character Previous { get; private set; }

        public CharacterKind Kind { get; }
        public char[] CandidateList { get; }
        public decimal Value { get; }

        public bool IsKindOf(CharacterKind flag)
        {
            return Kind.HasFlag(flag);
        }

        public static implicit operator decimal(Character token) => token.Value;

        public override string ToString()
        {
            return $"Value = {Value}, Kind = {Kind}, CandidateList = [{string.Join(",", CandidateList)}]";
        }
    }

    [Flags]
    internal enum CharacterKind
    {
        /// <summary> 表示整數 </summary>
        Integer = 0,

        /// <summary> 零到九 </summary>
        Digit = 1,
        /// <summary> 零 </summary>
        Zero = 2 | Digit,
        /// <summary> 一至九 </summary>
        Unit = 4 | Digit,


        /// <summary> 十倍量單位 </summary>
        Multipler = 8,

        /// <summary> 以十為基礎的小型單位 </summary>
        GroupTinyMultipler = 16 | Multipler,
        /// <summary> 十位 </summary>
        Ten = 32 | GroupTinyMultipler,
        /// <summary> 百位 </summary>
        Hundred = 64 | GroupTinyMultipler,
        /// <summary> 千位 </summary>
        Thousand = 128 | GroupTinyMultipler,

        /// <summary> 以十為基礎的巨量單位 </summary>
        GroupMultipler = 512 | Multipler,

        /// <summary> 同義詞 </summary>
        Duplex = 8192,

        /// <summary> 未知 </summary>
        Unknown = 16384,
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static IReadOnlyList<char> CharSet
    {
        get
        {
            return Character.CharSet;
        }
    }


}