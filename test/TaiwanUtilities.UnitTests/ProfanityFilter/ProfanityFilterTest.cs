namespace TaiwanUtilities.UnitTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

public class ProfanityFilterTest
{
    [Fact]
    public void 檢查是否帶有髒話()
    {
        // 測試文章
        var testArticle =
            """
                靠北啦！這個哭爸的專案真的讓人想死全家。
                那個白目87智障腦殘低能弱智北七的傢伙，
                整天只會說幹話、講垃圾、當廢物、做敗類、behave like人渣。
                他媽的這個賤人婊子雞掰王八蛋龜兒子混蛋畜生，
                幹你娘操你媽草你爸去你的操你全家幹你祖宗十八代！
                他娘的他奶奶的去死吧去死啦滾開滾蛋！
                三小娘咧阿你娘真是北爛機車白爛欠揍找死，
                你老娘我老子姑奶奶看了都想打人。
                這種賤貨狗娘養的就是個笨蛋傻子傻逼，
                整天只會靠杯靠腰，死好死開算了。
                那個懶叫小雞雞屌鳥臭雞真是下賤到極點。
            """;

        // 檢查是否包含髒話
        Assert.True(TaiwanUtilities.ProfanityFilter.Contains(testArticle)); 
    }

    [Fact]
    public void 遮蔽髒話測試()
    {
        // 測試文章
        var testArticle =
            """
                靠北啦！這個哭爸的專案真的讓人想死全家。
                那個白目87智障腦殘低能弱智北七的傢伙，
                整天只會說幹話、講垃圾、當廢物、做敗類、behave like人渣。
                他媽的這個賤人婊子雞掰王八蛋龜兒子混蛋畜生，
                幹你娘操你媽草你爸去你的操你全家幹你祖宗十八代！
                他娘的他奶奶的去死吧去死啦滾開滾蛋！
                三小娘咧阿你娘真是北爛機車白爛欠揍找死，
                你老娘我老子姑奶奶看了都想打人。
                這種賤貨狗娘養的就是個笨蛋傻子傻逼，
                整天只會靠杯靠腰，死好死開算了。
                那個懶叫小雞雞屌鳥臭雞真是下賤到極點。
            """;
        // 遮蔽髒話
        var censoredArticle = TaiwanUtilities.ProfanityFilter.Censor(testArticle);
        // 檢查遮蔽後的文章是否仍包含髒話
        Assert.False(TaiwanUtilities.ProfanityFilter.Contains(censoredArticle));

        
    }
}
