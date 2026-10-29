using PrinterInstall.App.Services;
using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Remote;

namespace PrinterInstall.App.Tests.ViewModels;

public class MainViewModelComputerInputModeTests
{
    private static MainViewModel CreateSut() =>
        // A troca de modo não usa orquestrador nem service provider.
        new(new SessionContext(), null!, null!, null!, new LocalMachineIdentity());

    [Fact]
    public void StartsInSingleMode()
    {
        var sut = CreateSut();

        Assert.True(sut.IsSingleMode);
        Assert.False(sut.IsListMode);
    }

    [Fact]
    public void SingleMode_TargetsComeFromSingleField()
    {
        var sut = CreateSut();
        sut.ListComputersText = "pc-lista-01\npc-lista-02";

        sut.SingleComputerText = "192.0.2.120";

        Assert.Equal("192.0.2.120", sut.ComputersText);
        Assert.Equal(1, sut.ComputerCount);
    }

    [Fact]
    public void ListMode_TargetsComeFromList()
    {
        var sut = CreateSut();
        sut.SingleComputerText = "192.0.2.120";
        sut.ListComputersText = "pc-lista-01\npc-lista-02";

        sut.IsListMode = true;

        Assert.Equal("pc-lista-01\npc-lista-02", sut.ComputersText);
        Assert.Equal(2, sut.ComputerCount);
    }

    [Fact]
    public void SwitchToList_EmptyList_CarriesSingleComputer()
    {
        var sut = CreateSut();
        sut.SingleComputerText = " 192.0.2.120 ";

        sut.IsListMode = true;

        Assert.Equal("192.0.2.120", sut.ListComputersText);
    }

    [Fact]
    public void SwitchBackToSingle_KeepsEachModesText()
    {
        var sut = CreateSut();
        sut.SingleComputerText = "192.0.2.120";
        sut.IsListMode = true;
        sut.ListComputersText = "pc-lista-01\npc-lista-02";

        sut.IsSingleMode = true;

        Assert.Equal("192.0.2.120", sut.ComputersText);
        Assert.Equal("pc-lista-01\npc-lista-02", sut.ListComputersText);
    }

    [Fact]
    public void SwitchToSingle_EmptyField_TakesTheOnlyListedComputer()
    {
        var sut = CreateSut();
        sut.IsListMode = true;
        sut.ListComputersText = "pc-lista-01\n";

        sut.IsListMode = false;

        Assert.Equal("pc-lista-01", sut.SingleComputerText);
    }

    [Fact]
    public void SettingSeveralComputers_SwitchesToListMode()
    {
        var sut = CreateSut();

        sut.ComputersText = "pc-01\r\npc-02";

        Assert.True(sut.IsListMode);
        Assert.Equal("pc-01\r\npc-02", sut.ListComputersText);
        Assert.Equal(2, sut.ComputerCount);
    }

    [Fact]
    public void ModeChange_NotifiesCount()
    {
        var sut = CreateSut();
        sut.SingleComputerText = "192.0.2.120";
        sut.ListComputersText = "pc-lista-01\npc-lista-02";
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.IsListMode = true;

        Assert.Contains(nameof(MainViewModel.ComputerCount), changed);
        Assert.Contains(nameof(MainViewModel.ComputersText), changed);
        Assert.Contains(nameof(MainViewModel.IsSingleMode), changed);
    }
}
