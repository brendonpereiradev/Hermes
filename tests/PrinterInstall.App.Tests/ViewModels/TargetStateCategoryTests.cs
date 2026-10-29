using System.Globalization;
using PrinterInstall.App.Converters;
using PrinterInstall.App.Localization;
using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Models;

namespace PrinterInstall.App.Tests.ViewModels;

public class TargetStateCategoryTests
{
    public static TheoryData<TargetMachineState> AllStates()
    {
        var data = new TheoryData<TargetMachineState>();
        foreach (var state in Enum.GetValues<TargetMachineState>())
            data.Add(state);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllStates))]
    public void EveryState_HasCategoryLabelAndIcon(TargetMachineState state)
    {
        TargetStateCategories.Of(state);

        var label = TargetMachineStateDisplay.GetDisplay(state);
        Assert.NotEqual(state.ToString(), label);

        var icon = new TargetMachineStateToIconConverter().Convert(state, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrEmpty(icon as string));
    }

    [Theory]
    [InlineData(TargetMachineState.Configuring, TargetStateCategory.InProgress)]
    [InlineData(TargetMachineState.RollbackRemovingPort, TargetStateCategory.InProgress)]
    [InlineData(TargetMachineState.CompletedSuccess, TargetStateCategory.Succeeded)]
    [InlineData(TargetMachineState.SkippedAlreadyExists, TargetStateCategory.Skipped)]
    [InlineData(TargetMachineState.AbortedDriverMissing, TargetStateCategory.Failed)]
    [InlineData(TargetMachineState.PrinterIdentityUnknown, TargetStateCategory.Failed)]
    [InlineData(TargetMachineState.DeployCancelled, TargetStateCategory.Cancelled)]
    [InlineData(TargetMachineState.RolledBack, TargetStateCategory.Cancelled)]
    internal void Of_GroupsStates(TargetMachineState state, TargetStateCategory expected)
    {
        Assert.Equal(expected, TargetStateCategories.Of(state));
    }

    [Fact]
    public void Of_UnknownState_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TargetStateCategories.Of((TargetMachineState)999));
    }
}
