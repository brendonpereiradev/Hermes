using CommunityToolkit.Mvvm.ComponentModel;
using PrinterInstall.App.Localization;
using PrinterInstall.Core.Models;

namespace PrinterInstall.App.ViewModels;

public partial class TargetRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _computerName = "";

    [ObservableProperty]
    private bool _isLocalMachine;

    [ObservableProperty]
    private string _computerNameDisplay = "";

    [ObservableProperty]
    private string _printerQueueName = "";

    [ObservableProperty]
    private string _expectedPortName = "";

    [ObservableProperty]
    private TargetMachineState _state = TargetMachineState.Pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private string _stateDisplay = TargetMachineStateDisplay.GetDisplay(TargetMachineState.Pending);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private string _message = "";

    /// <summary>
    /// A mensagem, mostrada sob o estado só quando acrescenta algo: vazia quando apenas repete o estado
    /// ("Concluído" / "Concluído", "Instalando driver" / "Instalando driver…").
    /// </summary>
    public string Detail => Normalize(Message).Equals(Normalize(StateDisplay), StringComparison.OrdinalIgnoreCase)
        ? ""
        : Message;

    partial void OnStateChanged(TargetMachineState value) =>
        StateDisplay = TargetMachineStateDisplay.GetDisplay(value);

    private static string Normalize(string text) => text.Trim().TrimEnd('…', '.').TrimEnd();
}
