
//namespace TaiwanUtilities.UnitTests;

//using Xunit;

//using static TaiwanIdValidator;

//[Trait("Category", "Unit")]
//public partial class TaiwanIdGeneratorTest
//{
//    [Fact]
//    public static void 營利事業統一編號()
//    {
//        for (int i = 0; i < 100; i++)
//        {

//            var actual = TaiwanIdGenerator.GenerateBusinessAdministrationNumber();

//            Assert.True(
//                condition: IsBusinessAdministrationNumber(actual, applyOldRules: false),
//                 $"產生的營利事業統一編號 {actual} 不符合規則");
//        }
//    }
//}