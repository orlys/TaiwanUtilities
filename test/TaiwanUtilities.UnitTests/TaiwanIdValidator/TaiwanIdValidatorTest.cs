namespace TaiwanUtilities.UnitTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static TaiwanIdValidator;
using Xunit;

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
    
}
