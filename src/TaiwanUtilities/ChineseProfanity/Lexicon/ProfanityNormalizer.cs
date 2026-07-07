namespace TaiwanUtilities.Internals;

using System;

internal static class ProfanityNormalizer
{
    internal static char[] Normalize(ReadOnlySpan<char> text)
    {
        var normalized = new char[text.Length];

        for (var i = 0; i < text.Length; i++)
        {
            normalized[i] = NormalizeChar(text[i]);
        }

        return normalized;
    }

    internal static string NormalizeToString(string text) => new(Normalize(text.AsSpan()));

    private static char NormalizeChar(char ch)
    {
        if (ch >= '\uFF01' && ch <= '\uFF5E')
        {
            ch = (char)(ch - 0xFEE0);
        }
        else if (ch == '\u3000')
        {
            ch = ' ';
        }

        if (ch >= 'a' && ch <= 'z')
        {
            ch = (char)(ch - ('a' - 'A'));
        }

        return ch switch
        {
            '妈' => '媽',
            '嬷' => '嬤',
            '干' => '幹',
            '奸' => '姦',
            '赣' => '贛',
            '驶' => '駛',
            '赛' => '賽',
            '卖' => '賣',
            '强' => '強',
            '尸' => '屍',
            '爱' => '愛',
            '处' => '處',
            '阳' => '陽',
            '滥' => '濫',
            '猫' => '貓',
            '马' => '馬',
            '参' => '參',
            '亲' => '親',
            '开' => '開',
            '师' => '師',
            '鲍' => '鮑',
            '龟' => '龜',
            '鸡' => '雞',
            '鸟' => '鳥',
            '废' => '廢',
            '痴' => '癡',
            '帐' => '帳',
            '犊' => '犢',
            '窝' => '窩',
            '败' => '敗',
            '种' => '種',
            '怂' => '慫',
            '货' => '貨',
            '饭' => '飯',
            '孙' => '孫',
            '会' => '會',
            '东' => '東',
            '杂' => '雜',
            '骚' => '騷',
            '懒' => '懶',
            '启' => '啟',
            '荡' => '蕩',
            '妇' => '婦',
            '纹' => '紋',
            '样' => '樣',
            '滚' => '滾',
            '贱' => '賤',
            '烂' => '爛',
            '脑' => '腦',
            '残' => '殘',
            _ => ch,
        };
    }
}
