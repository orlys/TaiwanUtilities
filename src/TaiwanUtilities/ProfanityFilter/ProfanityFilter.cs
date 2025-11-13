namespace TaiwanUtilities;

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 髒話過濾
/// </summary>
public static class ProfanityFilter
{


    private static readonly Lazy<Regex> s_pattern1 = new(static () =>
    {
        var aux = "((恁|琳|您|林|你|妳|祢|汝|他|她|它|牠|祂){1,2}(爹|娘|爸|(?<!媽)媽|馬|妹|姊|姐|老母|老師|娘親|親娘|奶奶|全家|祖宗|姑媽|姑奶奶|太祖|開基祖)的?)?";

        var patterns = $$"""
                        
            	- (淦|贛|幹){{aux}}(砲|炮)
            	- (性|做)愛{{aux}}(?!做)
            	- (咖)(小|洨)
            	- (哭|靠|看)?{{aux}}(山|三|沙|殺|莎){{aux}}(小|洨)
            	- (你|妳|祢)?老子
            	- (哭|靠){{aux}}(爸|北|杯|盃|邀|腰|么|夭|參)
            	- (性|口|乳|足|肛|援){{aux}}交
            	- (欠|破|下|狗){{aux}}(淦|贛|幹)
            	- (專用|專屬)?(肉{{aux}})?便器
            	- (專用|專屬)?(小|洨)?(母貓|母狗|公狗)
            	- (吃|食|喝){{aux}}(屎|尿|大便)
            	- 屁{{aux}}眼
            	- 去{{aux}}死
            	- 破{{aux}}處
            	- 天殺的
            	- 下{{aux}}三濫
            	- 打{{aux}}手槍
            	- 自{{aux}}慰
            	- 站{{aux}}壁
            	- 射在?\w+?上
            	- (手|賣|姦){{aux}}淫
                - (肏|操|草|糙|耖){{aux}}蛋

            	- (死|臭|破|賤)?淫{{aux}}(蕩|婦)
            	- (死|臭|破|賤)?蕩{{aux}}婦
            	- (死|臭|破|賤)?(懶|覽){{aux}}[趴叫]{1,2}
            	- (死|臭|破|賤)?婊{{aux}}(子)?
            	- (死|臭|破)?腦{{aux}}(殘|弱|包)
            	- (死|臭|破)?(機|雞|屐){{aux}}(掰|巴|8)
            	- (死|臭|破)?(屄|逼|B){{aux}}(機|雞|屐|樣)
            	- (死|臭|破)?慫{{aux}}(包|(屄|逼|B)?(機|雞|屐)?樣)
            	- (死|臭|破)?廢{{aux}}(物|人|柴)
            	- (死|臭|破)?(社{{aux}}會)?(敗{{aux}}類|低{{aux}}端)
            	- (死|臭|破)?(狗|兔){{aux}}崽(仔|子)
            	- (死|臭|破)?騷{{aux}}(包|樣|屄|逼|B|貨)
            	- (死|臭)?傻{{aux}}(瓜|子|逼|B|屄|屌|鳥)
            	- (死|臭)?低{{aux}}能(兒)?
            	- (死|臭)?(宰|人){{aux}}渣
            	- (死|臭)?王{{aux}}八(羔子|犢子)?
            	- (死|臭)?孬(孬|種|包|貨)
            	- (死|臭)?(笨|王八|滾|卵){{aux}}蛋
            	- (死|臭)?北{{aux}}(七|柒|爛)
            	- (死|臭)?白{{aux}}(痴|癡|目|爛)
            	- (死|臭)?(啟|乞){{aux}}智
            	- (死|臭)?(狗{{aux}})?畜{{aux}}(生|牲)
            	- (死|臭)?賤{{aux}}(屄|逼|B|畜|狗|女?人|貨|種|民|鮑魚?)
            	- (死|臭)?(乞丐|要飯)
            	- (死|臭)?混{{aux}}(帳|蛋)
            	- (死|臭)?俗{{aux}}辣
            	- (死|臭)?狗{{aux}}(娘|東西)(養的)?
            	- (死|臭)?窩囊廢?
            	- (死|臭)?雜{{aux}}(種|魚|碎)
            	- (死)?弱{{aux}}(雞|智)
            	- (死)?智{{aux}}(障|缺)
            	- (死)?龜{{aux}}(頭|狗|公)
            	- (死)?龜{{aux}}(兒|孫|崽)(仔|子)
            	- (死)?破{{aux}}(麻|狗|雞|鞋|腦|格|屄|逼|B|鮑魚?)
            	- (死)?爛{{aux}}(人|屄|逼|B|鮑魚?)
            	- (死)?垃{{aux}}圾

            	- (死|破|賤)?娼{{aux}}(妓)?
            	- (死|破|賤)?(?<!歌|舞|樂|官|藝)妓{{aux}}(女)?
            	- (死|臭|破)?(?<!便|貴|卑|低){{aux}}賤(?!價|賣|內|姓|骨頭)
            	- (?<!很|超)屌{{aux}}(樣|絲|毛)?
             	- (?<!軀|肢|骨|樹|主|莖|精|實|苦|能){{aux}}幹

            """;


        var g = string.Join(
            separator: "|",
            values: patterns
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Replace("-", null).Trim()));

        return new(
            pattern: $@"(?<PART>(?#PART){g})",
            options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.Compiled | RegexOptions.Multiline,
            matchTimeout: TimeSpan.FromMilliseconds(250));
    });

    private static readonly Lazy<Regex> s_pattern2 = new(static () => new(
        pattern: @"(?<PRE>(?#PRE1)真(他|她|它|牠|祂)(爹|娘|爸|(?<!媽)媽|馬)的?|(?#PRE2)(淦|贛|幹|姦|去|肏|操|草|糙|耖|駛|賽)(死|破)?(恁|琳|您|林|你|妳|祢|汝|他|她|它|牠|祂){1,2}(爹|娘|爸|(?<!媽)媽|馬|妹|姊|姐|老母|老師|娘親|親娘|奶奶|全家|祖宗|姑媽|姑奶奶|太祖|開基祖)(的|勒)?|(?#PRE3)(你|妳|祢)(他|她|它|牠|祂)?(娘|(?<!媽)媽|馬|老母|老師|奶奶|姑奶奶)的|(?#PRE4)((?<!媽)媽|馬)的)?",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.Compiled | RegexOptions.Multiline,
        matchTimeout: TimeSpan.FromMilliseconds(250)));

    private static readonly Lazy<Regex> s_pattern3 = new(static () => new(
        pattern: @"[\u3000-\u303F\uFF00-\uFF60!""#$%&'()*+,\-./:;<=>?@\[\\\]^_`{|}~\s]*?(?<WORD>滾|靠|淦|贛|幹|姦|操|糙|耖|肏)[\u3000-\u303F\uFF00-\uFF60!""#$%&'()*+,\-./:;<=>?@\[\\\]^_`{|}~\s]*?",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.Compiled | RegexOptions.Multiline,
        matchTimeout: TimeSpan.FromMilliseconds(250)));

    /// <summary>
    /// 檢查文字是否包含髒話
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static bool Censor(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
            Mask(text, '\ufffe').Contains('\ufffe');
    }

    /// <summary>
    /// 將文字中的髒話以指定字元遮蔽
    /// </summary>
    /// <param name="text"></param>
    /// <param name="censorChar"></param>
    /// <returns></returns>
    public static string? Mask(string? text, char censorChar = '*')
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var sb = new StringBuilder(text);

        try
        {
            foreach (Match m in s_pattern2.Value.Matches(text))
            {
                Populate(sb, m.Groups["PRE"], censorChar);
            }

            foreach (Match m in s_pattern1.Value.Matches(text))
            {
                Populate(sb, m.Groups["PART"], censorChar);
            }

            foreach (Match m in s_pattern3.Value.Matches(text))
            {
                Populate(sb, m.Groups["WORD"], censorChar);
            }
        }
        catch (RegexMatchTimeoutException)
        {
        }

        return sb.ToString();
    }



    private static StringBuilder Populate(StringBuilder sb, Group g, char c)
    {
        if (string.IsNullOrWhiteSpace(g.Value))
        {
            return sb;
        }

        var len = g.Length;
        var index = g.Index;
        return sb
            .Remove(index, len)
            .Insert(index, new string(c, len));
    }
}

