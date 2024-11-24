namespace TaiwanUtilities;

using System.Collections.Generic;
using System;
using System.Collections.Frozen;

partial struct ChineseNumeric
{
    private readonly record struct Character(char Value, int Index, Token Token)
    {
        /// <summary>
        /// 判斷是否為未知。
        /// </summary> 
        /// <returns></returns>
        public bool IsUnknown => Token.IsKindOf(TokenKind.Unknown);

        /// <summary>
        /// 判斷是否為零。
        /// </summary> 
        /// <returns></returns>
        public bool IsZero => Token.IsKindOf(TokenKind.Zero);

        /// <summary>
        /// 判斷是否為零到九(不包含零)。
        /// </summary> 
        /// <returns></returns>
        public bool IsDigit => Token.IsKindOf(TokenKind.Digit);

        /// <summary>
        /// 判斷是否為一到九。
        /// </summary> 
        /// <returns></returns>
        public bool IsUnit => Token.IsKindOf(TokenKind.Unit);


        /// <summary>
        /// 判斷是否為複合字。
        /// </summary> 
        /// <returns></returns>
        public bool IsDuplex => Token.IsKindOf(TokenKind.Duplex);


        /// <summary>
        /// 判斷是否為十百千。
        /// </summary> 
        /// <returns></returns>
        public bool IsGroupTinyMultipler => Token.IsKindOf(TokenKind.GroupTinyMultipler);

        /// <summary>
        /// 判斷是否為萬億兆京垓秭穰。
        /// </summary> 
        /// <returns></returns>
        public bool IsGroupMultipler => Token.IsKindOf(TokenKind.GroupMultipler);

        public override string ToString()
        {
            return $"[{Index}] = '{Value}'";
        }


        public static implicit operator decimal(Character character)
        {
            return character.Token.Value;
        }


        public static decimal operator *(Character left, decimal right)
        {
            return left.Token.Value * right;
        }

        public static decimal operator *(decimal left, Character right)
        {
            return left * right.Token.Value;
        }

        public static decimal operator *(Character left, Character right)
        {
            return left.Token.Value * right.Token.Value;
        }


        public static decimal operator +(Character left, decimal right)
        {
            return left.Token.Value + right;
        }

        public static decimal operator +(decimal left, Character right)
        {
            return left + right.Token.Value;
        }

        public static decimal operator +(Character left, Character right)
        {
            return left.Token.Value + right.Token.Value;
        }

        public static bool operator >(Character left, Character right)
        {
            return left.Token.Value > right.Token.Value;
        }
        public static bool operator <(Character left, Character right)
        {
            return left.Token.Value < right.Token.Value;
        }
    }


    private static class Tokenizer
    {
        public static List<Character> Tokenize(ReadOnlySpan<char> str)
        {
            var list = new List<Character>(str.Length);

            for (var index = 0; index < str.Length; index++)
            {
                var character = str[index];
                var token = Token.GetToken(character);
                list.Add(new Character(character, index, token));
            }

            return list;
        }
    }


    private class Token
    {
        static Token()
        {
            Unknown = new Token(TokenKind.Unknown, 0m);

            Zero = new Token(TokenKind.Zero, 0m, '〇', '0', '零', '０');

            One = new Token(TokenKind.Unit, 1m, '一', '1', '壹', '１');
            Two = new Token(TokenKind.Unit, 2m, '二', '2', '貳', '２', '贰', '兩');
            Three = new Token(TokenKind.Unit, 3m, '三', '3', '參', '３', '参');
            Four = new Token(TokenKind.Unit, 4m, '四', '4', '肆', '４');
            Five = new Token(TokenKind.Unit, 5m, '五', '5', '伍', '５');
            Six = new Token(TokenKind.Unit, 6m, '六', '6', '陸', '６', '陆');
            Seven = new Token(TokenKind.Unit, 7m, '七', '7', '柒', '７');
            Eight = new Token(TokenKind.Unit, 8m, '八', '8', '捌', '８');
            Nine = new Token(TokenKind.Unit, 9m, '九', '9', '玖', '９');

            Unit = new Token(TokenKind.Unknown, 1m, char.MinValue);
            Ten = new Token(TokenKind.Ten, 10m, '十', '拾');
            Hundred = new Token(TokenKind.Hundred, 100m, '百', '佰');
            Thousand = new Token(TokenKind.Thousand, 1000m, '千', '仟');

            TenThousand = new Token(TokenKind.GroupMultipler, value: 10_000m, '萬', '万');
            HundredMillion = new Token(TokenKind.GroupMultipler, value: 100_000_000m, '億', '亿');
            Trillion = new Token(TokenKind.GroupMultipler, value: 1000_000_000_000m, '兆');
            TenQuadrillion = new Token(TokenKind.GroupMultipler, value: 10_000_000_000_000_000m, '京');
            HundredQuintillion = new Token(TokenKind.GroupMultipler, value: 100_000_000_000_000_000_000m, '垓');
            Septillion = new Token(TokenKind.GroupMultipler, value: 1000_000_000_000_000_000_000_000m, '秭');
            TenOctillion = new Token(TokenKind.GroupMultipler, value: 10_000_000_000_000_000_000_000_000_000m, '穰');


            Unknown.PreviousToken = Unknown;
            Unknown.NextToken = Unknown;

            Zero.PreviousToken = Unknown;
            Zero.NextToken = Unknown;

            One.PreviousToken = Unknown;
            One.NextToken = Two;
            Two.PreviousToken = One;
            Two.NextToken = Three;
            Three.PreviousToken = Two;
            Three.NextToken = Four;
            Four.PreviousToken = Three;
            Four.NextToken = Five;
            Five.PreviousToken = Four;
            Five.NextToken = Six;
            Six.PreviousToken = Five;
            Six.NextToken = Seven;
            Seven.PreviousToken = Six;
            Seven.NextToken = Eight;
            Eight.PreviousToken = Seven;
            Eight.NextToken = Nine;
            Nine.PreviousToken = Eight;
            Nine.NextToken = Unknown;


            Unit.PreviousToken = Unknown;
            Unit.NextToken = Ten;
            Ten.PreviousToken = Unit;
            Ten.NextToken = Hundred;
            Hundred.PreviousToken = Ten;
            Hundred.NextToken = Thousand;
            Thousand.PreviousToken = Hundred;
            Thousand.NextToken = Unknown;

            TenThousand.PreviousToken = Unknown;
            TenThousand.NextToken = HundredMillion;
            HundredMillion.PreviousToken = TenThousand;
            HundredMillion.NextToken = Trillion;
            Trillion.PreviousToken = HundredMillion;
            Trillion.NextToken = TenQuadrillion;
            TenQuadrillion.PreviousToken = Trillion;
            TenQuadrillion.NextToken = HundredQuintillion;
            HundredQuintillion.PreviousToken = TenQuadrillion;
            HundredQuintillion.NextToken = Septillion;
            Septillion.PreviousToken = HundredQuintillion;
            Septillion.NextToken = TenOctillion;
            TenOctillion.PreviousToken = Septillion;
            TenOctillion.NextToken = Unknown;


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
            var numericTokens = new Dictionary<char, Token>();
            foreach (var token in List)
            {
                foreach (var c in token.Characters)
                {
                    numericTokens[c] = token;
                }
            }

            s_numericTokens = FrozenDictionary.ToFrozenDictionary(numericTokens);

        }

        public static Token Zero { get; }
        public static Token One { get; }
        public static Token Two { get; }
        public static Token Three { get; }
        public static Token Four { get; }
        public static Token Five { get; }
        public static Token Six { get; }
        public static Token Seven { get; }
        public static Token Eight { get; }
        public static Token Nine { get; }


        public static Token Unit { get; }
        public static Token Ten { get; }
        public static Token Hundred { get; }
        public static Token Thousand { get; }


        public static Token TenThousand { get; }
        public static Token HundredMillion { get; }
        public static Token Trillion { get; }
        public static Token TenQuadrillion { get; }
        public static Token HundredQuintillion { get; }
        public static Token Septillion { get; }
        public static Token TenOctillion { get; }

        public static Token Unknown { get; }

        public static bool ContainsToken(char character)
        {
            return s_numericTokens.ContainsKey(character);
        }


        public static Token GetToken(char character)
        {
            return s_numericTokens.GetValueOrDefault(character, Unknown);
        }

        private static readonly IReadOnlyDictionary<char, Token> s_numericTokens;

        public static IReadOnlyList<Token> List { get; }

        public bool IsUnknown => IsKindOf(TokenKind.Unknown);

        internal Token(TokenKind kind, decimal value, params char[] characters)
        {
            Kind = kind;
            Characters = characters;
            Value = value;
        }

        public Token NextToken { get; private set; }
        public Token PreviousToken { get; private set; }

        public TokenKind Kind { get; }
        public char[] Characters { get; }
        public decimal Value { get; }

        public bool IsKindOf(TokenKind flag)
        {
            return Kind.HasFlag(flag);
        }

        public static implicit operator decimal(Token token) => token.Value;
    }


    [Flags]
    private enum TokenKind
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
}
