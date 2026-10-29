using PrinterInstall.App.Services;
using PrinterInstall.App.ViewModels;
using Xunit;

namespace PrinterInstall.App.Tests.ViewModels;

public class RemovalWizardSummaryTests
{
    [Fact]
    public void BuildSummaryText_HidesZeroCountsAndPluralizes()
    {
        Assert.Equal("1 concluída.", RemovalWizardViewModel.BuildSummaryText(1, 0, 0, []));
        Assert.Equal("2 concluídas, 1 aviso, 3 erros.", RemovalWizardViewModel.BuildSummaryText(2, 1, 3, []).Split('\n')[0].TrimEnd());
    }

    [Fact]
    public void BuildSummaryText_ListsFailures()
    {
        var summary = RemovalWizardViewModel.BuildSummaryText(0, 0, 1, ["PC1 [A] — acesso negado"]);

        Assert.StartsWith("1 erro.", summary);
        Assert.Contains("PC1 [A] — acesso negado", summary);
    }

    [Fact]
    public void BuildSummaryText_WithNothingCounted_StillSaysSomething()
    {
        Assert.Equal("0 concluídas.", RemovalWizardViewModel.BuildSummaryText(0, 0, 0, []));
    }

    [Theory]
    [InlineData(OperationOutcome.Success, SummaryTone.Success)]
    [InlineData(OperationOutcome.Warning, SummaryTone.Warning)]
    [InlineData(OperationOutcome.Error, SummaryTone.Error)]
    internal void GetSummaryHeading_ToneFollowsOutcome(OperationOutcome outcome, SummaryTone expected)
    {
        Assert.Equal(expected, RemovalWizardViewModel.GetSummaryHeading(outcome).Tone);
    }
}
