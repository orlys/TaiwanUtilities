namespace TaiwanUtilities.UnitTests;

using Xunit;

public class ChineseProfanityTest
{
    [Fact]
    public void 檢查是否帶有髒話()
    {
        // 測試文章
        var testArticle =
            """
                欠人姦是不是
                靠你媽北
                靠你媽的北
                靠北三小

                這個妓女真是低端人渣，根本就是垃圾人渣和社會敗類。那個廢物整天像廢柴一樣，根本是個廢人。他媽的機掰和雞巴破麻、破狗、破雞、破妓、破鞋，還有破婊子都不如。
                那些龜公、龜頭、龜狗、龜兒子、龜孫子根本是畜生和畜牲。欠幹、破幹、下幹、欠淦、破淦、下淦、欠贛、破贛、下贛的賤人和賤貨。
                白痴、白癡、白目、白爛加上智障、智缺、啟智、乞智、腦殘、腦弱、腦包、低能兒、低能、弱雞、弱智。
                北七、北柒、北爛、87、笨蛋、混蛋、傻瓜、傻子、傻逼、傻B、傻屄、傻屌、三小、三洨、王八蛋、王八羔子。


                你這個智障是不是腦袋有問題？整天在那邊亂講話，根本就是個社會敗類。看你那副死樣子就知道是個廢物，連基本常識都沒有還敢出來丟人現眼。
                別以為自己很厲害，其實就是個笨蛋罷了。你媽沒教你怎麼做人嗎？還是說你天生就是個白痴？看你講話的方式就知道智商堪憂，根本腦殘到不行。
                閉上你那張臭嘴好嗎？每次看到你發言都覺得噁心，真不知道哪來的勇氣一直在那邊叫囂。你以為你是誰啊？不過就是個垃圾，還真把自己當一回事了。
                滾一邊去吧，別在這裡礙眼。你這種人就該被封鎖，省得污染大家的眼睛。看了就煩，懶得理你這種貨色。

                幹你娘機掰！不會開車就別上路啦，你他媽的是在龜三小？這個雞巴王八蛋根本不配握方向盤，開車跟狗屎一樣爛。
                你是瞎了嗎？眼睛長在屁眼上是不是？這死傻逼真的是北七到極點，連個轉彎都不會打方向燈。幹你老母的三小垃圾，害老子差點撞上去。
                靠北啊！你這賤人是沒考過駕照還是用買的？整個就是低能兒在開車，智障到讓人無言。你全家都是廢物是吧？教出你這種垃圾人渣來禍害社會。
                滾你媽的蛋！老子今天心情不好，別惹我發火。你再這樣亂開信不信我下車扁你？死雞掰臭婊子，看你那副慫樣就知道是個孬種。
                操你媽的破麻爛貨！這龜兒子根本就是馬路三寶，應該被吊銷執照一輩子不准開車。幹！真他媽倒楣遇到你這畜生。

                肏

                你這個狗娘養的混帳東西！真是個沒用的廢柴，爛到骨子裡了。這種爛咖根本就是人間敗筆，連狗都不如的玩意兒。
                你算什麼鳥蛋？整天在那邊裝模作樣，其實就是個軟腳蝦。看你那副熊樣就覺得噁心，根本就是個窩囊廢。
                去死啦你！這種垃圾咖小還敢在這裡大小聲？你腦子進水了是吧？整個就是個草包，中看不中用的繡花枕頭。
                閉嘴啦臭俗辣！你這個孬貨、慫包、膿包，連個屁都不敢放還敢嗆聲？真是不要臉到極點，恬不知恥的爛人。
                滾遠點啦！看到你就倒胃口，整個就是個活該被淘汰的loser。你這種貨色就該被社會拋棄，別出來丟人現眼了。
            """;

        Assert.True(ChineseProfanity.Censor(testArticle));
    }

    [Fact]
    public void 遮蔽髒話測試()
    {
        var testArticle =
            """
                靠你媽北
                靠你媽的北
                靠北三小

                這個妓女真是低端人渣，根本就是垃圾人渣和社會敗類。那個廢物整天像廢柴一樣，根本是個廢人。他媽的機掰和雞巴破麻、破狗、破雞、破妓、破鞋，還有破婊子都不如。
                那些龜公、龜頭、龜狗、龜兒子、龜孫子根本是畜生和畜牲。欠幹、破幹、下幹、欠淦、破淦、下淦、欠贛、破贛、下贛的賤人和賤貨。
                白痴、白癡、白目、白爛加上智障、智缺、啟智、乞智、腦殘、腦弱、腦包、低能兒、低能、弱雞、弱智。
                北七、北柒、北爛、87、笨蛋、混蛋、傻瓜、傻子、傻逼、傻B、傻屄、傻屌、三小、三洨、王八蛋、王八羔子。


                你這個智障是不是腦袋有問題？整天在那邊亂講話，根本就是個社會敗類。看你那副死樣子就知道是個廢物，連基本常識都沒有還敢出來丟人現眼。
                別以為自己很厲害，其實就是個笨蛋罷了。你媽沒教你怎麼做人嗎？還是說你天生就是個白痴？看你講話的方式就知道智商堪憂，根本腦殘到不行。
                閉上你那張臭嘴好嗎？每次看到你發言都覺得噁心，真不知道哪來的勇氣一直在那邊叫囂。你以為你是誰啊？不過就是個垃圾，還真把自己當一回事了。
                滾一邊去吧，別在這裡礙眼。你這種人就該被封鎖，省得污染大家的眼睛。看了就煩，懶得理你這種貨色。

                幹你娘機掰！不會開車就別上路啦，你他媽的是在龜三小？這個雞巴王八蛋根本不配握方向盤，開車跟狗屎一樣爛。
                你是瞎了嗎？眼睛長在屁眼上是不是？這死傻逼真的是北七到極點，連個轉彎都不會打方向燈。幹你老母的三小垃圾，害老子差點撞上去。
                靠北啊！你這賤人是沒考過駕照還是用買的？整個就是低能兒在開車，智障到讓人無言。你全家都是廢物是吧？教出你這種垃圾人渣來禍害社會。
                滾你媽的蛋！老子今天心情不好，別惹我發火。你再這樣亂開信不信我下車扁你？死雞掰臭婊子，看你那副慫樣就知道是個孬種。
                操你媽的破麻爛貨！這龜兒子根本就是馬路三寶，應該被吊銷執照一輩子不准開車。幹！真他媽倒楣遇到你這畜生。

                肏

                你這個狗娘養的混帳東西！真是個沒用的廢柴，爛到骨子裡了。這種爛咖根本就是人間敗筆，連狗都不如的玩意兒。
                你算什麼鳥蛋？整天在那邊裝模作樣，其實就是個軟腳蝦。看你那副熊樣就覺得噁心，根本就是個窩囊廢。
                去死啦你！這種垃圾咖小還敢在這裡大小聲？你腦子進水了是吧？整個就是個草包，中看不中用的繡花枕頭。
                閉嘴啦臭俗辣！你這個孬貨、慫包、膿包，連個屁都不敢放還敢嗆聲？真是不要臉到極點，恬不知恥的爛人。
                滾遠點啦！看到你就倒胃口，整個就是個活該被淘汰的loser。你這種貨色就該被社會拋棄，別出來丟人現眼了。
            """;
        var censoredArticle = ChineseProfanity.Replace(testArticle);
        Assert.False(ChineseProfanity.Censor(censoredArticle));
    }

    // ================================================================
    // 安全詞誤判測試：正常用語不應被判定為髒話
    // ================================================================

    [Theory]
    // 賽/駛 — 比賽、駕駛
    [InlineData("賽馬比賽很精彩")]
    [InlineData("今天的籃球賽打得非常激烈")]
    [InlineData("這場比賽的結果令人意外")]
    [InlineData("決賽在下週六舉行")]
    [InlineData("他是一位優秀的駕駛員")]
    [InlineData("請小心行駛，前方有施工")]
    [InlineData("自動駕駛技術日益成熟")]
    [InlineData("車輛駛入高速公路")]
    // 幹 — 幹部、能幹
    [InlineData("他是公司的幹部")]
    [InlineData("這位員工非常能幹")]
    [InlineData("他是骨幹人員")]
    [InlineData("高速公路幹線已經開通")]
    // 操 — 操場、操作
    [InlineData("學生在操場跑步")]
    [InlineData("操場上有人在跑步")]
    [InlineData("這台機器操作很簡單")]
    [InlineData("他的操守令人敬佩")]
    [InlineData("請按照操作手冊進行")]
    [InlineData("早操時間到了")]
    // 草 — 草地、草案
    [InlineData("草地上有花")]
    [InlineData("草稿已經完成")]
    [InlineData("這片草原真美")]
    [InlineData("薰衣草的香味很好聞")]
    [InlineData("南投草屯鎮是個好地方")]
    // 靠 — 靠近、可靠
    [InlineData("這個人很靠近我")]
    [InlineData("可靠的夥伴")]
    [InlineData("船隻已經靠港")]
    [InlineData("他是個靠譜的人")]
    // 滾 — 滾動、翻滾
    [InlineData("滾動條往下拉")]
    [InlineData("水滾了可以下麵")]
    [InlineData("體操選手做出漂亮的翻滾")]
    // 糙 — 粗糙
    [InlineData("這塊布料質地很粗糙")]
    [InlineData("糙米飯比白米飯營養")]
    // 贛 — 江西省
    [InlineData("贛州是江西省的城市")]
    [InlineData("贛江流經多個城市")]
    // 逼 — 逼近、逼迫
    [InlineData("颱風正在逼近台灣")]
    [InlineData("不要逼迫別人做不想做的事")]
    [InlineData("這幅畫非常逼真")]
    // 鳥 — 鳥類
    [InlineData("公園裡有很多鳥兒在唱歌")]
    [InlineData("候鳥每年秋天南飛")]
    [InlineData("鳥瞰城市全景非常壯觀")]
    // 雞 — 雞肉、雞蛋
    [InlineData("雞蛋很好吃")]
    [InlineData("今天午餐吃雞排")]
    [InlineData("土雞湯是冬天的好選擇")]
    // 馬 — 馬匹、賽馬
    [InlineData("騎馬是一項優雅的運動")]
    [InlineData("馬拉松比賽在早上開跑")]
    [InlineData("斑馬線要注意安全")]
    [InlineData("馬來西亞是東南亞國家")]
    [InlineData("他做事很馬虎")]
    // 賤 — 賤價
    [InlineData("請問賤價出售的商品在哪")]
    // 妓 — 藝妓
    [InlineData("日本藝妓文化")]
    // 射 — 注射、射擊、射在牆上（正常用法）
    [InlineData("注射疫苗很重要")]
    [InlineData("射擊比賽開始了")]
    [InlineData("發射火箭升空")]
    [InlineData("把球射在牆上")]
    [InlineData("箭射在靶上")]
    // 屌 — 俚語讚美義
    [InlineData("這招很屌")]
    [InlineData("超屌的操作")]
    [InlineData("這個人真屌")]
    // 死 — 死亡
    [InlineData("死亡人數統計")]
    [InlineData("他誓死保衛國家")]
    [InlineData("九死一生的經歷")]
    // 臭 — 臭豆腐
    [InlineData("臭豆腐是台灣名產")]
    [InlineData("臭氧層破洞需要關注")]
    // 破 — 突破
    [InlineData("突破紀錄")]
    [InlineData("警方已經破案了")]
    [InlineData("打破沉默說出真相")]
    // 爛 — 燦爛
    [InlineData("燦爛的陽光")]
    [InlineData("水果已經腐爛了")]
    // 媽/娘/爸 — 正常親屬稱呼
    [InlineData("媽媽做的飯最好吃")]
    [InlineData("新娘穿著白色婚紗")]
    [InlineData("爸爸帶我去釣魚")]
    [InlineData("媽祖遶境是重要的民俗活動")]
    [InlineData("那位姑娘真漂亮")]
    // 林 — 森林（閩南話代詞，但也是姓/森林）
    [InlineData("森林裡有很多動物")]
    [InlineData("造林計畫已經啟動")]
    // 垃圾 — 正常生活用語
    [InlineData("請記得倒垃圾")]
    [InlineData("垃圾分類是每個人的責任")]
    [InlineData("垃圾車晚上七點來")]
    // 廢物利用
    [InlineData("廢物利用可以減少浪費")]
    // 拎 — 閩南話「你」，但普通話是「提著」
    [InlineData("拎包出門上班去")]
    [InlineData("他拎著一袋水果回來")]
    // 太祖
    [InlineData("明太祖朱元璋建立明朝")]
    // 全家
    [InlineData("全家便利商店到處都有")]
    [InlineData("全家人一起去旅行")]
    // 姐妹
    [InlineData("姐妹倆感情很好")]
    [InlineData("空姐的工作很辛苦")]
    // 馬 — 常見詞後接「的」不應誤判
    [InlineData("馬祖的廟很有名")]
    [InlineData("馬桶的蓋子壞了")]
    [InlineData("馬路的盡頭")]
    [InlineData("馬甲的扣子")]
    [InlineData("馬蹄的聲音")]
    [InlineData("靠北邊一點")]
    // 親屬稱呼 — 正常用語
    [InlineData("阿姨你好")]
    [InlineData("叔叔帶我去公園")]
    [InlineData("隔壁伯伯人很好")]
    [InlineData("舅舅住在台中")]
    [InlineData("嬸嬸做的菜很好吃")]
    [InlineData("姑姑下週來家裡")]
    [InlineData("姨太太在客廳等你")]
    // 混合用法
    [InlineData("賽馬場的駕駛員在操場旁邊的草地上靠近一棵樹幹休息")]
    public static void 正常用語不應被判定為髒話(string normalText)
    {
        Assert.False(ChineseProfanity.Censor(normalText));
    }

    // ================================================================
    // 髒話偵測測試
    // ================================================================

    [Theory]
    // 普通話髒話
    [InlineData("幹你娘")]
    [InlineData("操你媽")]
    [InlineData("他媽的")]
    [InlineData("媽的")]
    [InlineData("智障")]
    [InlineData("白痴")]
    [InlineData("垃圾")]
    [InlineData("廢物")]
    [InlineData("畜生")]
    [InlineData("混帳")]
    [InlineData("混蛋")]
    [InlineData("王八蛋")]
    [InlineData("去死")]
    [InlineData("靠北")]
    [InlineData("機掰")]
    [InlineData("雞巴")]
    [InlineData("幹")]
    [InlineData("肏")]
    [InlineData("狗屎")]
    [InlineData("死傻逼")]
    [InlineData("臭婊子")]
    [InlineData("龜兒子")]
    [InlineData("三小")]
    [InlineData("北七")]
    [InlineData("咖小")]
    [InlineData("腦殘")]
    [InlineData("賤人")]
    [InlineData("爛人")]
    [InlineData("破麻")]
    // 動詞+代詞+親屬（靠 VerbPronoun 抓到「幹你」即可）
    [InlineData("幹你阿姨")]
    [InlineData("操你叔叔")]
    // 射 的性相關用法
    [InlineData("射精")]
    [InlineData("顏射")]
    [InlineData("內射")]
    [InlineData("射在臉上")]
    // 動詞+代詞（委婉替代式，如「幹你香蕉個芭樂」）
    [InlineData("幹你")]
    [InlineData("操你")]
    [InlineData("肏你")]
    [InlineData("幹他")]
    [InlineData("操他")]
    // 閩南話髒話
    [InlineData("賽拎娘")]
    [InlineData("駛拎娘")]
    [InlineData("賽你娘")]
    [InlineData("駛你娘")]
    [InlineData("拎北")]
    [InlineData("林北")]
    [InlineData("欠駛")]
    [InlineData("賽三小")]
    public static void 髒話應被正確偵測(string profaneText)
    {
        Assert.True(ChineseProfanity.Censor(profaneText));
    }

    // ================================================================
    // 替換正確性測試
    // ================================================================

    [Theory]
    [InlineData("幹你娘", "○○○")]
    [InlineData("你這個白痴", "你這個○○")]
    [InlineData("去死啦", "○○啦")]
    [InlineData("幹你香蕉個芭樂", "○○香蕉個芭樂")]
    public static void 髒話應被正確替換(string input, string expected)
    {
        Assert.Equal(expected, ChineseProfanity.Replace(input));
    }

    // ================================================================
    // 邊界條件測試
    // ================================================================

    [Fact]
    public void 空值和空白不應有問題()
    {
        Assert.False(ChineseProfanity.Censor(null));
        Assert.False(ChineseProfanity.Censor(""));
        Assert.False(ChineseProfanity.Censor("   "));
        Assert.Null(ChineseProfanity.Replace(null));
        Assert.Equal("", ChineseProfanity.Replace(""));
        Assert.Equal("   ", ChineseProfanity.Replace("   "));
    }

    // ================================================================
    // 情境式文章測試：包含多種髒話的長篇文章
    // ================================================================

    [Fact]
    public void 遊戲對戰聊天室髒話偵測()
    {
        var chatLog =
            """
            玩家A：你會不會玩啊？打個輔助都不會跟，真的是腦殘到不行。
            玩家B：閉嘴啦廢物，你自己那個操作才叫垃圾好嗎？
            玩家A：操你媽的，你說誰垃圾？你才是那個整天送頭的白痴！
            玩家B：幹你娘機掰！老子打的比你好一百倍，死智障！
            玩家C：你們兩個都滾啦，一群低能兒在那邊互罵，看了就煩。
            玩家A：關你屁事？靠北喔，多管閒事的畜生。
            玩家B：就是說啊，這種咖小還敢嗆人，笑死人了。
            玩家A：賽拎娘！你這個龜兒子連排位都上不去還敢說別人？北七仔！
            """;

        Assert.True(ChineseProfanity.Censor(chatLog));
        var censored = ChineseProfanity.Replace(chatLog);
        Assert.False(ChineseProfanity.Censor(censored));
    }

    [Fact]
    public void 遊戲對戰聊天室髒話偵測_閩南語混用()
    {
        var chatLog =
            """
            路人甲：哩賽三小？不會開車就不要上路啦！
            路人乙：駛拎娘！你自己闖紅燈還怪別人？
            路人甲：拎北在這裡開車十幾年了，你算哪根蔥？
            路人乙：林北才不管你開幾年，你就是個三小路隊長！
            路人甲：欠駛是不是？下車來講啊！
            路人乙：來啊，誰怕誰？賽你娘！
            """;

        Assert.True(ChineseProfanity.Censor(chatLog));
        var censored = ChineseProfanity.Replace(chatLog);
        Assert.False(ChineseProfanity.Censor(censored));
    }

    [Fact]
    public void 論壇筆戰髒話偵測()
    {
        var forumPost =
            """
            樓主：最近看到一個新聞真的很生氣，那些貪污的官員根本就是社會敗類，應該全部抓去關！

            1樓：同意！那些人渣簡直是國家之恥，畜生不如的東西！
            2樓：他媽的，每次看到這種新聞就火大。那些混帳東西上任時說得多好聽，結果一個比一個爛！
            3樓：這種垃圾官員應該死刑伺候，整天欺壓百姓的王八蛋。
            4樓：樓上各位消消氣，罵人解決不了問題......
            5樓：消什麼氣？你是不是那群廢物的同夥？要不要也一起滾？
            6樓：就是有你這種鄉愿的人才讓那些敗類有恃無恐！覺醒好嗎腦殘？
            7樓：操你媽的，五樓六樓吃炸藥了嗎？人家好意勸架你們還罵人，真是狗咬呂洞賓。
            """;

        Assert.True(ChineseProfanity.Censor(forumPost));
        var censored = ChineseProfanity.Replace(forumPost);
        Assert.False(ChineseProfanity.Censor(censored));
    }

    [Fact]
    public void 正常文章不應被判定為髒話()
    {
        var normalArticle =
            """
            今天下午，台北市舉辦了一場盛大的馬拉松比賽。來自全國各地的選手齊聚一堂，
            在操場旁的幹道上展開激烈的競賽。駕駛員們被要求在比賽期間改道行駛，以確保
            選手的安全。

            賽事組委會的幹部表示，這次比賽的參賽人數突破了歷年紀錄。選手們在草地上
            進行熱身操，教練們操心著每位選手的狀態。一位來自贛州的選手表現特別突出，
            他的粗糙雙手展現了多年苦幹的痕跡。

            觀眾席上，媽媽帶著孩子來為爸爸加油。姑娘們拎著加油牌在靠近終點線的地方
            等待。全家人一起出遊觀賽的場景處處可見。樹林邊的鳥兒似乎也被這熱鬧的
            氣氛感染，嘰嘰喳喳叫個不停。

            比賽結束後，選手們前往全家便利商店購買飲料，順便買了雞排和臭豆腐慶祝。
            這真是燦爛的一天。媽祖廟前的香火也格外旺盛，彷彿在為今天的賽事祈福。
            """;

        Assert.False(ChineseProfanity.Censor(normalArticle));
    }

    [Fact]
    public void 新聞報導不應被誤判()
    {
        var newsArticle =
            """
            【社會新聞】昨日凌晨，一輛自小客車在高速公路上行駛時突然失控，駕駛因
            疲勞駕駛而打瞌睡，車輛衝破護欄後翻覆。消防隊員趕到現場時，駕駛已經
            死亡，副駕駛座的乘客則是九死一生，被緊急送醫搶救。

            警方表示，死者為四十五歲男性，死因初步判斷為頸部骨折。這起死亡車禍
            再度凸顯了疲勞駕駛的危險性。交通部呼籲民眾開車時要注意休息，發現
            自己打瞌睡時應立即靠邊停車。

            近年來，自動駕駛技術日益成熟，許多車廠已開始研發能在駕駛疲勞時自動
            接管的系統。專家認為這項技術的突破將有效降低死亡車禍的發生率。

            在另一則新聞中，贛江流域因連日暴雨導致水位暴漲。當地官員組織民眾
            誓死保衛堤壩，軍民合作打了一場漂亮的防洪戰役。暴雨後，許多農作物
            已經腐爛，農民損失慘重。政府正在評估災損並研擬補助方案。
            """;

        Assert.False(ChineseProfanity.Censor(newsArticle));
    }

    [Fact]
    public void 混合語境包含髒話的文章_應被偵測()
    {
        var mixedArticle =
            """
            小明今天在操場上跑步，結果被一個不長眼的傢伙撞到。那個白痴連道歉
            都不會，小明氣得大罵：「靠北！你眼睛長在屁眼上嗎？」

            旁邊的同學趕緊過來勸架：「別吵了別吵了。」但那個人居然回嘴說：
            「關你屁事？滾一邊去啦廢物！」

            小明的媽媽剛好來學校接他，聽到這些話非常震驚。她拎著包包走過來
            對那個人說：「同學，請注意你的言行。」

            結果那人居然回了一句：「幹你娘，你又是誰？」小明氣到不行，
            差點衝上去打人。老師趕到後才平息了這場風波。
            """;

        Assert.True(ChineseProfanity.Censor(mixedArticle));
        var censored = ChineseProfanity.Replace(mixedArticle);
        // 確保安全詞沒有被替換
        Assert.Contains("操場", censored);
        Assert.Contains("媽媽", censored);
        Assert.Contains("拎著", censored);
        Assert.Contains("老師", censored);
    }
}
