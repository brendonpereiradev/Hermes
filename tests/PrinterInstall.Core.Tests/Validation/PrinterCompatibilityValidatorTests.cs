using PrinterInstall.Core.Models;
using PrinterInstall.Core.Validation;

namespace PrinterInstall.Core.Tests.Validation;

public class PrinterCompatibilityValidatorTests
{
    [Theory]
    [InlineData("EPSON M1180 Series", PrinterBrand.Epson, "M1180")]
    [InlineData("EPSON WorkForce Pro WF-M5899", PrinterBrand.Epson, "WF-M5899")]
    [InlineData("Lexmark CX532adwe", PrinterBrand.Lexmark, "CX532ADWE")]
    [InlineData("Lexmark MX622adhe", PrinterBrand.Lexmark, "MX622ADHE")]
    [InlineData("Lexmark MX622ade", PrinterBrand.Lexmark, "MX622ADE")]
    [InlineData("Lexmark MS431dn", PrinterBrand.Lexmark, "MS431DN")]
    [InlineData("Lexmark MS821dn", PrinterBrand.Lexmark, "MS821DN")]
    [InlineData("Lexmark CS531dw", PrinterBrand.Lexmark, "CS531DW")]
    [InlineData("Lexmark CX735adse", PrinterBrand.Lexmark, "CX735ADSE")]
    [InlineData("Lexmark MB2442adwe", PrinterBrand.Lexmark, "MB2442ADWE")]
    [InlineData("Lexmark B2338dw", PrinterBrand.Lexmark, "B2338DW")]
    [InlineData("Lexmark MC3326i", PrinterBrand.Lexmark, "MC3326I")]
    [InlineData("Brother HL-L5212DW", PrinterBrand.Brother, "HL-L5212DW")]
    [InlineData("Gainscha GA-2408T", PrinterBrand.Gainscha, "GA-2408T")]
    [InlineData("EPSON WFM5899", PrinterBrand.Epson, "WF-M5899")]
    [InlineData("Brother HLL5212DW", PrinterBrand.Brother, "HL-L5212DW")]
    [InlineData("Gainscha GA2408T", PrinterBrand.Gainscha, "GA-2408T")]
    public void ParseAndCheck_AcceptsKnownModels(string description, PrinterBrand brand, string model)
    {
        var identity = PrinterIdentityParser.Parse("192.0.2.10", description, "SNMP");
        Assert.Equal(brand, identity.Brand);
        Assert.Equal(model, identity.Model);
        Assert.True(PrinterCompatibilityValidator.Check(identity, brand).Compatible);
    }

    [Theory]
    [InlineData(PrinterBrand.Epson, PrinterBrand.Lexmark)]
    [InlineData(PrinterBrand.Epson, PrinterBrand.Brother)]
    [InlineData(PrinterBrand.Epson, PrinterBrand.Gainscha)]
    [InlineData(PrinterBrand.Lexmark, PrinterBrand.Epson)]
    [InlineData(PrinterBrand.Lexmark, PrinterBrand.Brother)]
    [InlineData(PrinterBrand.Lexmark, PrinterBrand.Gainscha)]
    [InlineData(PrinterBrand.Brother, PrinterBrand.Epson)]
    [InlineData(PrinterBrand.Brother, PrinterBrand.Lexmark)]
    [InlineData(PrinterBrand.Brother, PrinterBrand.Gainscha)]
    [InlineData(PrinterBrand.Gainscha, PrinterBrand.Epson)]
    [InlineData(PrinterBrand.Gainscha, PrinterBrand.Lexmark)]
    [InlineData(PrinterBrand.Gainscha, PrinterBrand.Brother)]
    public void Check_RejectsEveryCrossBrandCombination(PrinterBrand detected, PrinterBrand selected)
    {
        var model = detected switch
        {
            PrinterBrand.Epson => "M1180",
            PrinterBrand.Lexmark => "CX532ADWE",
            PrinterBrand.Brother => "HL-L5212DW",
            _ => "GA-2408T"
        };
        var result = PrinterCompatibilityValidator.Check(new("192.0.2.10", detected, model, "Test"), selected);
        Assert.False(result.Compatible);
        Assert.Contains(detected.ToString(), result.Message);
        Assert.Contains(selected.ToString(), result.Message);
    }

    [Fact]
    public void Check_RejectsUnknownModelEvenWhenBrandMatches()
    {
        var identity = PrinterIdentityParser.Parse("192.0.2.10", "Lexmark MS310dn", "SNMP");
        Assert.Equal(PrinterBrand.Lexmark, identity.Brand);
        Assert.Equal("MS310DN", identity.Model);
        Assert.False(PrinterCompatibilityValidator.Check(identity, PrinterBrand.Lexmark).Compatible);
    }

    [Fact]
    public void Check_RejectsUnsupportedKnownModelEvenWhenBrandMatches()
    {
        var identity = new PrinterIdentityResult("192.0.2.10", PrinterBrand.Lexmark, "B2236DW", "SNMP");
        Assert.False(PrinterCompatibilityValidator.Check(identity, PrinterBrand.Lexmark).Compatible);
    }

    [Theory]
    [InlineData("MX62")]
    [InlineData("MX6220")]
    [InlineData("MB2236ADW")]
    public void Check_LexmarkFamilyMatchRequiresExactFamily(string model)
    {
        var identity = new PrinterIdentityResult("192.0.2.10", PrinterBrand.Lexmark, model, "SNMP");
        Assert.False(PrinterCompatibilityValidator.Check(identity, PrinterBrand.Lexmark).Compatible);
    }

    [Fact]
    public void Parse_ConflictingBrandsAreNeverApproved()
    {
        var identity = PrinterIdentityParser.Parse("192.0.2.10", "Lexmark Epson CX532adwe", "SNMP");
        Assert.True(identity.ConflictingEvidence);
        Assert.False(PrinterCompatibilityValidator.Check(identity, PrinterBrand.Lexmark).Compatible);
    }
}
