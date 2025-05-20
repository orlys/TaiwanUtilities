// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities.UnitTests;

using Xunit;

using static TaiwanIdValidator;

[Trait("Category", "Unit")]
public partial class TaiwanIdValidatorTest
{
    [Fact]
    public static void 營利事業統一編號()
    {
        Assert.False(IsBusinessAdministrationNumber(null, applyOldRules: false));
        Assert.False(IsBusinessAdministrationNumber("", applyOldRules: false));

        Assert.True(IsBusinessAdministrationNumber("12345670", applyOldRules: false));
        Assert.True(IsBusinessAdministrationNumber("12345671", applyOldRules: false));
        Assert.True(IsBusinessAdministrationNumber("12345675", applyOldRules: false));
        Assert.True(IsBusinessAdministrationNumber("12345676", applyOldRules: false));
        Assert.True(IsBusinessAdministrationNumber("04595257", applyOldRules: false));

        Assert.False(IsBusinessAdministrationNumber("1234567", applyOldRules: false));
        Assert.False(IsBusinessAdministrationNumber("123456769", applyOldRules: false));
        Assert.False(IsBusinessAdministrationNumber("12345678", applyOldRules: false));
        Assert.False(IsBusinessAdministrationNumber("12345672", applyOldRules: false));
        Assert.False(IsBusinessAdministrationNumber("04595253", applyOldRules: false));

        Assert.False(IsBusinessAdministrationNumber(null, applyOldRules: true));
        Assert.False(IsBusinessAdministrationNumber("", applyOldRules: true));

        Assert.True(IsBusinessAdministrationNumber("12345675", applyOldRules: true));
        Assert.True(IsBusinessAdministrationNumber("12345676", applyOldRules: true));
        Assert.True(IsBusinessAdministrationNumber("04595257", applyOldRules: true));

        Assert.False(IsBusinessAdministrationNumber("1234567", applyOldRules: true));
        Assert.False(IsBusinessAdministrationNumber("123456769", applyOldRules: true));
        Assert.False(IsBusinessAdministrationNumber("12345678", applyOldRules: true));
        Assert.False(IsBusinessAdministrationNumber("12345670", applyOldRules: true));
        Assert.False(IsBusinessAdministrationNumber("12345671", applyOldRules: true));
        Assert.False(IsBusinessAdministrationNumber("04595252", applyOldRules: true));
    }

    [Fact]
    public static void 自然人憑證號碼()
    {
        Assert.False(IsCitizenDigitalCertificateNumber(null));
        Assert.False(IsCitizenDigitalCertificateNumber(""));
        Assert.False(IsCitizenDigitalCertificateNumber("AB123456789012345"));
        Assert.False(IsCitizenDigitalCertificateNumber("AB1234567890123")); ;

        Assert.True(IsCitizenDigitalCertificateNumber("AB12345678901234"));
        Assert.True(IsCitizenDigitalCertificateNumber("RP47809425348791"));
        Assert.True(IsCitizenDigitalCertificateNumber("ab12345678901234"));

        Assert.False(IsCitizenDigitalCertificateNumber("A112345678901234"));
        Assert.False(IsCitizenDigitalCertificateNumber("9B12345678901234"));
        Assert.False(IsCitizenDigitalCertificateNumber("AA123456789012J4"));
    }


    [Fact]
    public static void 電子發票捐贈碼()
    {
        Assert.False(IsElectronicInvoiceDonateCode(null));
        Assert.False(IsElectronicInvoiceDonateCode(""));

        Assert.True(IsElectronicInvoiceDonateCode("123"));
        Assert.True(IsElectronicInvoiceDonateCode("10001"));
        Assert.True(IsElectronicInvoiceDonateCode("2134567"));

        Assert.False(IsElectronicInvoiceDonateCode("00"));
        Assert.False(IsElectronicInvoiceDonateCode("12345678"));
        Assert.False(IsElectronicInvoiceDonateCode("ab3456"));
    }


    [Fact]
    public static void 電子發票手機條碼()
    {
        Assert.False(IsElectronicInvoiceMobileBarcode(null));
        Assert.False(IsElectronicInvoiceMobileBarcode(""));
        Assert.False(IsElectronicInvoiceMobileBarcode("3030101"));
        Assert.False(IsElectronicInvoiceMobileBarcode("/ABCD1234"));
        Assert.False(IsElectronicInvoiceMobileBarcode("/ABCD12"));

        Assert.True(IsElectronicInvoiceMobileBarcode("/+.-++.."));
        Assert.True(IsElectronicInvoiceMobileBarcode("/AAA33AA"));
        Assert.True(IsElectronicInvoiceMobileBarcode("/P4SV.-I"));
        Assert.True(IsElectronicInvoiceMobileBarcode("/O0O01I1"));
        Assert.True(IsElectronicInvoiceMobileBarcode("/ab12345"));

        Assert.False(IsElectronicInvoiceMobileBarcode("/ABCD12;"));
        Assert.False(IsElectronicInvoiceMobileBarcode("/ABCD$12"));
    }
     

    [Fact]
    public void 身分證號碼測試()
    {
        // 正確的身分證號碼
        Assert.True(IsIdentityCardNumber("Y190290172"));
        Assert.True(IsIdentityCardNumber("F131104093"));
        Assert.True(IsIdentityCardNumber("O158238845"));
        Assert.True(IsIdentityCardNumber("N116247806"));
        Assert.True(IsIdentityCardNumber("L122544270"));
        Assert.True(IsIdentityCardNumber("C180661564"));
        Assert.True(IsIdentityCardNumber("Y123456788"));

        // 錯誤的身分證號碼
        Assert.False(IsIdentityCardNumber("A12345678"));
        Assert.False(IsIdentityCardNumber("y190290172"));
        Assert.False(IsIdentityCardNumber("A123456788"));
        Assert.False(IsIdentityCardNumber("F131104091"));
        Assert.False(IsIdentityCardNumber("O158238842"));
    }

    [Fact]
    public void 舊式統一證號測試()
    {
        // 正確的舊式統一證號
        Assert.True(IsIdentityCardNumber("AB23456789", true));
        Assert.True(IsIdentityCardNumber("AA00000009", true));
        Assert.True(IsIdentityCardNumber("AB00207171", true));
        Assert.True(IsIdentityCardNumber("AC03095424", true));
        Assert.True(IsIdentityCardNumber("BD01300667", true));
        Assert.True(IsIdentityCardNumber("CC00151114", true));
        Assert.True(IsIdentityCardNumber("HD02717288", true));
        Assert.True(IsIdentityCardNumber("TD00251124", true));
        Assert.True(IsIdentityCardNumber("AD30196818", true));

        // 錯誤的舊式統一證號
        Assert.False(IsIdentityCardNumber("AA1234567", true));
        Assert.False(IsIdentityCardNumber("aa00000009", true));
        Assert.False(IsIdentityCardNumber("AA00000000", true));
        Assert.False(IsIdentityCardNumber("FG31104091", true));
        Assert.False(IsIdentityCardNumber("OY58238842", true));
        Assert.False(IsIdentityCardNumber("AE23456785", true));
        Assert.False(IsIdentityCardNumber("2123456789", true));
        Assert.False(IsIdentityCardNumber("1A23456789", true));
    }

    [Fact]
    public void 新式統一證號測試()
    {
        // 正確的新式統一證號
        Assert.True(IsIdentityCardNumber("A800000014"));
        Assert.True(IsIdentityCardNumber("A900207177"));
        Assert.True(IsIdentityCardNumber("A803095426"));
        Assert.True(IsIdentityCardNumber("B801300667"));
        Assert.True(IsIdentityCardNumber("C800151116"));
        Assert.True(IsIdentityCardNumber("H802717288"));
        Assert.True(IsIdentityCardNumber("T900251126"));
        Assert.True(IsIdentityCardNumber("A930196810"));

        // 各類別新式統一證號
        Assert.True(IsIdentityCardNumber("A800000014")); // 外國人或無國籍人士
        Assert.True(IsIdentityCardNumber("A870000015")); // 無國籍居民
        Assert.True(IsIdentityCardNumber("A880000018")); // 港澳居民
        Assert.True(IsIdentityCardNumber("A890000011")); // 中國大陸居民

        // 錯誤的新式統一證號
        Assert.False(IsIdentityCardNumber("a800000009"));
        Assert.False(IsIdentityCardNumber("A800000000"));
        Assert.False(IsIdentityCardNumber("F931104091"));
        Assert.False(IsIdentityCardNumber("O958238842"));
        Assert.False(IsIdentityCardNumber("A8923456"));
    }

    [Fact]
    public void 非法輸入測試()
    {
        // 非字串格式或長度不符
        Assert.False(IsIdentityCardNumber("A1234567899"));
        Assert.False(IsIdentityCardNumber("A12345678"));
        Assert.False(IsIdentityCardNumber("1234567890"));

        // 字首非英文字母
        Assert.False(IsIdentityCardNumber("2123456789"));
        Assert.False(IsIdentityCardNumber("1123456789"));

        // 第二位數字不在[1, 2, 8, 9]範圍內
        Assert.False(IsIdentityCardNumber("A323456789"));
        Assert.False(IsIdentityCardNumber("A423456789"));
        Assert.False(IsIdentityCardNumber("A523456789"));
        Assert.False(IsIdentityCardNumber("A623456789"));
        Assert.False(IsIdentityCardNumber("A723456789"));
    }
}
