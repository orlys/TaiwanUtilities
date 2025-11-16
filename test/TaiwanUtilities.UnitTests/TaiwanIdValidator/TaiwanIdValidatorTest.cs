// transpile from https://github.com/enylin/taiwan-id-validator/commit/6a673c608e5ec2287a58457a6dc2317f7a03f338
// license: MIT

namespace TaiwanUtilities.UnitTests;

using Xunit; 

[Trait("Category", "Unit")]
public partial class TaiwanIdValidatorTest
{
    [Fact]
    public static void 營利事業統一編號()
    {
        Assert.False(BusinessAdministrationNumber.Validate(null, applyOldRules: false));
        Assert.False(BusinessAdministrationNumber.Validate("", applyOldRules: false));

        Assert.True(BusinessAdministrationNumber.Validate("12345670", applyOldRules: false));
        Assert.True(BusinessAdministrationNumber.Validate("12345671", applyOldRules: false));
        Assert.True(BusinessAdministrationNumber.Validate("12345675", applyOldRules: false));
        Assert.True(BusinessAdministrationNumber.Validate("12345676", applyOldRules: false));
        Assert.True(BusinessAdministrationNumber.Validate("04595257", applyOldRules: false));

        Assert.False(BusinessAdministrationNumber.Validate("1234567", applyOldRules: false));
        Assert.False(BusinessAdministrationNumber.Validate("123456769", applyOldRules: false));
        Assert.False(BusinessAdministrationNumber.Validate("12345678", applyOldRules: false));
        Assert.False(BusinessAdministrationNumber.Validate("12345672", applyOldRules: false));
        Assert.False(BusinessAdministrationNumber.Validate("04595253", applyOldRules: false));

        Assert.False(BusinessAdministrationNumber.Validate(null, applyOldRules: true));
        Assert.False(BusinessAdministrationNumber.Validate("", applyOldRules: true));

        Assert.True(BusinessAdministrationNumber.Validate("12345675", applyOldRules: true));
        Assert.True(BusinessAdministrationNumber.Validate("12345676", applyOldRules: true));
        Assert.True(BusinessAdministrationNumber.Validate("04595257", applyOldRules: true));

        Assert.False(BusinessAdministrationNumber.Validate("1234567", applyOldRules: true));
        Assert.False(BusinessAdministrationNumber.Validate("123456769", applyOldRules: true));
        Assert.False(BusinessAdministrationNumber.Validate("12345678", applyOldRules: true));
        Assert.False(BusinessAdministrationNumber.Validate("12345670", applyOldRules: true));
        Assert.False(BusinessAdministrationNumber.Validate("12345671", applyOldRules: true));
        Assert.False(BusinessAdministrationNumber.Validate("04595252", applyOldRules: true));
    }

    [Fact]
    public static void 自然人憑證號碼()
    {
        Assert.False(CitizenDigitalCertificateNumber.Validate(null));
        Assert.False(CitizenDigitalCertificateNumber.Validate(""));
        Assert.False(CitizenDigitalCertificateNumber.Validate("AB123456789012345"));
        Assert.False(CitizenDigitalCertificateNumber.Validate("AB1234567890123")); ;

        Assert.True(CitizenDigitalCertificateNumber.Validate("AB12345678901234"));
        Assert.True(CitizenDigitalCertificateNumber.Validate("RP47809425348791"));
        Assert.True(CitizenDigitalCertificateNumber.Validate("ab12345678901234"));

        Assert.False(CitizenDigitalCertificateNumber.Validate("A112345678901234"));
        Assert.False(CitizenDigitalCertificateNumber.Validate("9B12345678901234"));
        Assert.False(CitizenDigitalCertificateNumber.Validate("AA123456789012J4"));
    }


    [Fact]
    public static void 電子發票捐贈碼()
    {
        Assert.False(ElectronicInvoiceDonateCode.Validate(null));
        Assert.False(ElectronicInvoiceDonateCode.Validate(""));

        Assert.True(ElectronicInvoiceDonateCode.Validate("123"));
        Assert.True(ElectronicInvoiceDonateCode.Validate("10001"));
        Assert.True(ElectronicInvoiceDonateCode.Validate("2134567"));

        Assert.False(ElectronicInvoiceDonateCode.Validate("00"));
        Assert.False(ElectronicInvoiceDonateCode.Validate("12345678"));
        Assert.False(ElectronicInvoiceDonateCode.Validate("ab3456"));
    }


    [Fact]
    public static void 電子發票手機條碼()
    {
        Assert.False(ElectronicInvoiceMobileBarCode.Validate(null));
        Assert.False(ElectronicInvoiceMobileBarCode.Validate(""));
        Assert.False(ElectronicInvoiceMobileBarCode.Validate("3030101"));
        Assert.False(ElectronicInvoiceMobileBarCode.Validate("/ABCD1234"));
        Assert.False(ElectronicInvoiceMobileBarCode.Validate("/ABCD12"));

        Assert.True(ElectronicInvoiceMobileBarCode.Validate("/+.-++.."));
        Assert.True(ElectronicInvoiceMobileBarCode.Validate("/AAA33AA"));
        Assert.True(ElectronicInvoiceMobileBarCode.Validate("/P4SV.-I"));
        Assert.True(ElectronicInvoiceMobileBarCode.Validate("/O0O01I1"));
        Assert.True(ElectronicInvoiceMobileBarCode.Validate("/ab12345"));

        Assert.False(ElectronicInvoiceMobileBarCode.Validate("/ABCD12;"));
        Assert.False(ElectronicInvoiceMobileBarCode.Validate("/ABCD$12"));
    }
     

    [Fact]
    public void 身分證號碼測試()
    {
        // 正確的身分證號碼
        Assert.True(NationalIdentificationCardNumber.Validate("Y190290172"));
        Assert.True(NationalIdentificationCardNumber.Validate("F131104093"));
        Assert.True(NationalIdentificationCardNumber.Validate("O158238845"));
        Assert.True(NationalIdentificationCardNumber.Validate("N116247806"));
        Assert.True(NationalIdentificationCardNumber.Validate("L122544270"));
        Assert.True(NationalIdentificationCardNumber.Validate("C180661564"));
        Assert.True(NationalIdentificationCardNumber.Validate("Y123456788"));

        // 錯誤的身分證號碼
        Assert.False(NationalIdentificationCardNumber.Validate("A12345678"));
        Assert.False(NationalIdentificationCardNumber.Validate("y190290172"));
        Assert.False(NationalIdentificationCardNumber.Validate("A123456788"));
        Assert.False(NationalIdentificationCardNumber.Validate("F131104091"));
        Assert.False(NationalIdentificationCardNumber.Validate("O158238842"));
    }

    [Fact]
    public void 舊式統一證號測試()
    {
        // 正確的舊式統一證號
        Assert.True(NationalIdentificationCardNumber.Validate("AB23456789", true));
        Assert.True(NationalIdentificationCardNumber.Validate("AA00000009", true));
        Assert.True(NationalIdentificationCardNumber.Validate("AB00207171", true));
        Assert.True(NationalIdentificationCardNumber.Validate("AC03095424", true));
        Assert.True(NationalIdentificationCardNumber.Validate("BD01300667", true));
        Assert.True(NationalIdentificationCardNumber.Validate("CC00151114", true));
        Assert.True(NationalIdentificationCardNumber.Validate("HD02717288", true));
        Assert.True(NationalIdentificationCardNumber.Validate("TD00251124", true));
        Assert.True(NationalIdentificationCardNumber.Validate("AD30196818", true));

        // 錯誤的舊式統一證號
        Assert.False(NationalIdentificationCardNumber.Validate("AA1234567", true));
        Assert.False(NationalIdentificationCardNumber.Validate("aa00000009", true));
        Assert.False(NationalIdentificationCardNumber.Validate("AA00000000", true));
        Assert.False(NationalIdentificationCardNumber.Validate("FG31104091", true));
        Assert.False(NationalIdentificationCardNumber.Validate("OY58238842", true));
        Assert.False(NationalIdentificationCardNumber.Validate("AE23456785", true));
        Assert.False(NationalIdentificationCardNumber.Validate("2123456789", true));
        Assert.False(NationalIdentificationCardNumber.Validate("1A23456789", true));
    }

    [Fact]
    public void 新式統一證號測試()
    {
        // 正確的新式統一證號
        Assert.True(NationalIdentificationCardNumber.Validate("A800000014"));
        Assert.True(NationalIdentificationCardNumber.Validate("A900207177"));
        Assert.True(NationalIdentificationCardNumber.Validate("A803095426"));
        Assert.True(NationalIdentificationCardNumber.Validate("B801300667"));
        Assert.True(NationalIdentificationCardNumber.Validate("C800151116"));
        Assert.True(NationalIdentificationCardNumber.Validate("H802717288"));
        Assert.True(NationalIdentificationCardNumber.Validate("T900251126"));
        Assert.True(NationalIdentificationCardNumber.Validate("A930196810"));

        // 各類別新式統一證號
        Assert.True(NationalIdentificationCardNumber.Validate("A800000014")); // 外國人或無國籍人士
        Assert.True(NationalIdentificationCardNumber.Validate("A870000015")); // 無國籍居民
        Assert.True(NationalIdentificationCardNumber.Validate("A880000018")); // 港澳居民
        Assert.True(NationalIdentificationCardNumber.Validate("A890000011")); // 中國大陸居民

        // 錯誤的新式統一證號
        Assert.False(NationalIdentificationCardNumber.Validate("a800000009"));
        Assert.False(NationalIdentificationCardNumber.Validate("A800000000"));
        Assert.False(NationalIdentificationCardNumber.Validate("F931104091"));
        Assert.False(NationalIdentificationCardNumber.Validate("O958238842"));
        Assert.False(NationalIdentificationCardNumber.Validate("A8923456"));
    }

    [Fact]
    public void 非法輸入測試()
    {
        // 非字串格式或長度不符
        Assert.False(NationalIdentificationCardNumber.Validate("A1234567899"));
        Assert.False(NationalIdentificationCardNumber.Validate("A12345678"));
        Assert.False(NationalIdentificationCardNumber.Validate("1234567890"));

        // 字首非英文字母
        Assert.False(NationalIdentificationCardNumber.Validate("2123456789"));
        Assert.False(NationalIdentificationCardNumber.Validate("1123456789"));

        // 第二位數字不在[1, 2, 8, 9]範圍內
        Assert.False(NationalIdentificationCardNumber.Validate("A323456789"));
        Assert.False(NationalIdentificationCardNumber.Validate("A423456789"));
        Assert.False(NationalIdentificationCardNumber.Validate("A523456789"));
        Assert.False(NationalIdentificationCardNumber.Validate("A623456789"));
        Assert.False(NationalIdentificationCardNumber.Validate("A723456789"));
    }
}
