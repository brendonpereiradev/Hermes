using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrinterInstall.App.Localization;
using PrinterInstall.App.Resources;
using PrinterInstall.App.Services;
using PrinterInstall.Core.Logging;
using PrinterInstall.Core.Models;
using PrinterInstall.Core.Orchestration;
using PrinterInstall.Core.Remote;
using PrinterInstall.Core.Validation;

namespace PrinterInstall.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int DefaultDeployPort = 9100;
    private const TcpPrinterProtocol DefaultDeployProtocol = TcpPrinterProtocol.Raw;

    private readonly ISessionContext _session;
    private readonly PrinterDeploymentOrchestrator _orchestrator;
    private readonly DeploymentRollbackRunner _rollbackRunner;
    private readonly IServiceProvider _serviceProvider;
    private readonly LocalMachineIdentity _localMachineIdentity;
    private readonly IDeploymentNotificationService _notificationService;
    private readonly IConfirmationDialogService _dialogService;
    private readonly IThemeService? _themeService;
    private readonly IDiagnosticFileLogger? _diagnosticLogger;
    private CancellationTokenSource? _deployCts;
    private readonly ILocalDiagnosticCollector? _diagnosticCollector;
    private string _diagnosticContext = "";

    public MainViewModel(
        ISessionContext session,
        PrinterDeploymentOrchestrator orchestrator,
        DeploymentRollbackRunner rollbackRunner,
        IServiceProvider serviceProvider,
        LocalMachineIdentity localMachineIdentity,
        ILogExportService? logExportService = null,
        IDeploymentNotificationService? notificationService = null,
        IConfirmationDialogService? dialogService = null,
        IThemeService? themeService = null,
        IDiagnosticFileLogger? diagnosticLogger = null,
        ILocalDiagnosticCollector? diagnosticCollector = null)
    {
        _session = session;
        _orchestrator = orchestrator;
        _rollbackRunner = rollbackRunner;
        _serviceProvider = serviceProvider;
        _localMachineIdentity = localMachineIdentity;
        Log = new OperationLog(logExportService ?? new LogExportService(), diagnosticLogger, "MainDeploy");
        Log.PropertyChanged += OnLogChanged;
        _notificationService = notificationService ?? new DeploymentNotificationService();
        _dialogService = dialogService ?? new ConfirmationDialogService();
        _themeService = themeService;
        _diagnosticLogger = diagnosticLogger;
        _diagnosticCollector = diagnosticCollector;

        if (_themeService != null)
        {
            _isDarkMode = _themeService.IsDarkMode;
            _themeService.ThemeChanged += (_, _) =>
            {
                IsDarkMode = _themeService.IsDarkMode;
            };
        }

        PrinterRows.Add(new PrinterFormRowViewModel());
        PrinterRows.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanRemovePrinterRow));
            RemovePrinterRowCommand.NotifyCanExecuteChanged();
        };
        Targets.CollectionChanged += OnTargetsChanged;

        if (_session.IsLocalOnly)
            ComputersText = _localMachineIdentity.GetPrimaryLocalName();
    }

    /// <summary>Sessão sem domínio: a lista de computadores fica travada neste computador.</summary>
    public bool IsLocalOnly => _session.IsLocalOnly;

    public bool ShowStatusEmptyHint => Targets.Count == 0;

    public int StatusTargetCount => Targets.Count;

    public int StatusSucceededCount => DeploymentTargetRows.CountOutcomes(Targets).Succeeded;

    public int StatusFailedCount => DeploymentTargetRows.CountOutcomes(Targets).Failed;

    /// <summary>Linhas que terminaram sem sucesso nem falha ("Já existe", "Cancelado", "Revertido").</summary>
    public int StatusNeutralCount => DeploymentTargetRows.CountOutcomes(Targets).Neutral;

    /// <summary>Linhas ainda pendentes ou em andamento.</summary>
    public int StatusInProgressCount => StatusTargetCount - StatusSucceededCount - StatusFailedCount - StatusNeutralCount;

    private void OnTargetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (TargetRowViewModel row in e.NewItems)
                row.PropertyChanged += OnTargetRowChanged;
        }
        if (e.OldItems is not null)
        {
            foreach (TargetRowViewModel row in e.OldItems)
                row.PropertyChanged -= OnTargetRowChanged;
        }

        OnPropertyChanged(nameof(ShowStatusEmptyHint));
        NotifyStatusSummaryChanged();
    }

    private void OnTargetRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetRowViewModel.State))
            NotifyStatusSummaryChanged();
    }

    private void NotifyStatusSummaryChanged()
    {
        OnPropertyChanged(nameof(StatusTargetCount));
        OnPropertyChanged(nameof(StatusSucceededCount));
        OnPropertyChanged(nameof(StatusFailedCount));
        OnPropertyChanged(nameof(StatusNeutralCount));
        OnPropertyChanged(nameof(StatusInProgressCount));
    }

    public bool CanRemovePrinterRow => PrinterRows.Count > 1;

    [ObservableProperty]
    private bool _isDarkMode;

    [RelayCommand]
    private void ToggleTheme()
    {
        if (_themeService != null)
        {
            _themeService.ToggleTheme();
            IsDarkMode = _themeService.IsDarkMode;
        }
        else
        {
            IsDarkMode = !IsDarkMode;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputerCount))]
    [NotifyPropertyChangedFor(nameof(InvalidComputerCount))]
    [NotifyPropertyChangedFor(nameof(HasInvalidComputers))]
    private string _computersText = "";

    public bool HasInvalidComputers => InvalidComputerCount > 0;

    public int ComputerCount => ComputerNameListParser.Parse(ComputersText).Count;

    public int InvalidComputerCount =>
        ComputerNameListParser.Parse(ComputersText).Count(n => !ComputerNameValidator.IsPlausibleComputerName(n));

    [ObservableProperty]
    private bool _printTestPage;

    public OperationLog Log { get; }

    public string LogText
    {
        get => Log.Text;
        set => Log.Text = value;
    }

    private void OnLogChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OperationLog.Text))
            return;

        OnPropertyChanged(nameof(LogText));
        OnPropertyChanged(nameof(CanExportLog));
        ExportLogCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private string _lastSummaryText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExportLog))]
    private bool _isDeployRunning;

    partial void OnIsDeployRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExportLog));
        DeployCommand.NotifyCanExecuteChanged();
        CancelDeployCommand.NotifyCanExecuteChanged();
        ExportLogCommand.NotifyCanExecuteChanged();
    }

    public bool CanExportLog => Log.CanExport;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CancelDeployButtonText))]
    private bool _isCancellationRequested;

    public string CancelDeployButtonText => IsCancellationRequested ? "Cancelando…" : "Cancelar";

    partial void OnIsCancellationRequestedChanged(bool value) => CancelDeployCommand.NotifyCanExecuteChanged();

    public ObservableCollection<PrinterFormRowViewModel> PrinterRows { get; } = new();

    public ObservableCollection<TargetRowViewModel> Targets { get; } = new();

    [RelayCommand]
    private void AddThisComputer() =>
        ComputersText = ComputerListText.WithThisComputer(ComputersText, _localMachineIdentity);

    [RelayCommand]
    private void AddPrinterRow()
    {
        PrinterRows.Add(new PrinterFormRowViewModel());
    }

    [RelayCommand(CanExecute = nameof(CanRemovePrinterRow))]
    private void RemovePrinterRow(PrinterFormRowViewModel? row)
    {
        if (PrinterRows.Count <= 1)
            return;
        if (row is not null && PrinterRows.Contains(row))
        {
            PrinterRows.Remove(row);
            return;
        }

        PrinterRows.RemoveAt(PrinterRows.Count - 1);
    }

    [RelayCommand]
    private void CopySummaryToClipboard()
    {
        if (string.IsNullOrEmpty(LastSummaryText))
            return;
        try
        {
            Clipboard.SetText(LastSummaryText);
        }
        catch
        {
            // Clipboard may fail in rare cases; ignore
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportLog))]
    private void ExportLog()
    {
        Log.SetSensitiveValue(_session.Credential?.Password);
        var operatorId = OperationLog.DescribeOperator(_session.Credential);

        var targetSummaries = Targets.Select(t => (
            t.ComputerName,
            t.PrinterQueueName,
            TargetMachineStateDisplay.GetDisplay(t.State),
            (string?)t.Message
        ));

        Log.Export(
            $"PrinterInstall_Deploy_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt",
            logText => LogReportFormatter.FormatDeployReport(
                operatorId,
                _localMachineIdentity.GetPrimaryLocalName(),
                targetSummaries,
                logText,
                diagnosticLogPath: _diagnosticLogger?.CurrentLogFilePath,
                diagnosticContext: _diagnosticContext,
                diagnosticLogText: _diagnosticLogger?.ReadSessionLog()));
    }

    [RelayCommand(CanExecute = nameof(CanDeploy))]
    private async Task DeployAsync()
    {
        LogText = "";
        Targets.Clear();
        LastSummaryText = "";
        _diagnosticContext = "";

        var cred = _session.Credential;
        if (cred is null)
        {
            Log.Append(UiStrings.Main_NotAuthenticated);
            return;
        }
        Log.SetSensitiveValue(cred.Password);

        var rawNames = ComputerNameListParser.Parse(ComputersText);
        if (rawNames.Count == 0)
        {
            Log.Append(UiStrings.Main_Validation_ComputersRequired);
            await _dialogService.ShowNoComputersWarningAsync();
            return;
        }

        foreach (var row in PrinterRows)
        {
            if (string.IsNullOrWhiteSpace(row.DisplayName))
            {
                Log.Append(UiStrings.Main_Validation_DisplayNameRequired);
                return;
            }

            if (string.IsNullOrWhiteSpace(row.PrinterHostAddress))
            {
                Log.Append(UiStrings.Main_Validation_PrinterHostRequired);
                return;
            }
        }

        var invertedRows = new List<(PrinterFormRowViewModel Row, string DisplayName, string HostAddress)>();
        foreach (var row in PrinterRows)
        {
            var trimmedDisplayName = row.DisplayName.Trim();
            var trimmedHost = row.PrinterHostAddress.Trim();

            if (PrinterHostValidator.DetectProbableInversion(trimmedDisplayName, trimmedHost))
            {
                invertedRows.Add((row, trimmedDisplayName, trimmedHost));
            }
        }

        if (invertedRows.Count > 0)
        {
            var inversionItems = invertedRows
                .Select(item => string.Format(UiStrings.Main_InversionDialogItemFormat, item.DisplayName, item.HostAddress))
                .ToList();

            var proceed = await _dialogService.ConfirmInversionCorrectionAsync(inversionItems);
            if (!proceed)
            {
                Log.Append(UiStrings.Main_DeployCancelledByInversionWarning);
                return;
            }

            // Inverter os campos automaticamente na interface e registrar log
            foreach (var item in invertedRows)
            {
                item.Row.DisplayName = item.HostAddress;
                item.Row.PrinterHostAddress = item.DisplayName;
                Log.Append(string.Format(UiStrings.Main_InversionCorrectedLogFormat, item.HostAddress, item.DisplayName));
            }
        }

        var definitions = new List<PrinterQueueDefinition>();
        foreach (var row in PrinterRows)
        {
            var trimmedDisplayName = row.DisplayName.Trim();
            var trimmedHost = row.PrinterHostAddress.Trim();

            if (!PrinterHostValidator.IsValidHostAddress(trimmedHost))
            {
                Log.Append(string.Format(UiStrings.Main_Validation_InvalidHostAddressFormat, trimmedHost));
                return;
            }

            if (row.Brand == PrinterBrand.Gainscha && row.GainschaLabelPreset is null)
            {
                Log.Append(UiStrings.Main_Validation_GainschaLabelPresetRequired);
                return;
            }

            definitions.Add(new PrinterQueueDefinition
            {
                Brand = row.Brand,
                DisplayName = trimmedDisplayName,
                PrinterHostAddress = trimmedHost,
                PortNumber = DefaultDeployPort,
                Protocol = DefaultDeployProtocol,
                GainschaLabelPreset = row.Brand == PrinterBrand.Gainscha ? row.GainschaLabelPreset : null
            });
        }

        var heuristicWarnings = PrinterBrandHeuristicsValidator.Inspect(definitions);
        if (heuristicWarnings.Count > 0)
        {
            var proceed = await _dialogService.ConfirmDeployWarningAsync(heuristicWarnings);
            if (!proceed)
            {
                Log.Append(UiStrings.Main_DeployCancelledByMismatchWarning);
                return;
            }
        }

        var validNames = new List<string>();
        foreach (var n in rawNames)
        {
            if (!ComputerNameValidator.IsPlausibleComputerName(n))
            {
                AddTargetRows(n, definitions, TargetMachineState.Error, UiStrings.Main_InvalidComputerNameFormat);
                continue;
            }

            if (_session.IsLocalOnly && !_localMachineIdentity.IsLocalMachine(n))
            {
                AddTargetRows(n, definitions, TargetMachineState.Error, UiStrings.Main_LocalOnlyRemoteBlocked);
                continue;
            }

            validNames.Add(n);
        }

        if (validNames.Count == 0)
            return;

        foreach (var n in validNames)
            AddTargetRows(n, definitions, TargetMachineState.Pending, message: null);

        var request = new PrinterDeploymentRequest
        {
            TargetComputerNames = validNames,
            Printers = definitions,
            DomainCredential = cred,
            PrintTestPage = PrintTestPage
        };

        var journal = new DeploymentRollbackJournal();
        _deployCts = new CancellationTokenSource();
        var deployToken = _deployCts.Token;
        IsCancellationRequested = false;
        IsDeployRunning = true;
        var started = DateTimeOffset.Now;
        var runId = Guid.NewGuid().ToString("N");
        var plan = new StringBuilder();
        plan.AppendLine($"Execução: {runId}; sessão do aplicativo: {_diagnosticLogger?.SessionId}; início: {started:O}");
        plan.AppendLine($"Conta informada: {PrinterInstall.Core.Auth.CredentialHelper.BuildCredentialUserName(cred)}; teste de impressão: {request.PrintTestPage}");
        foreach (var computer in validNames)
            plan.AppendLine($"Destino: {computer}; caminho: {(_localMachineIdentity.IsLocalMachine(computer) ? "LOCAL" : "REMOTO (WMI/SMB)")}");
        foreach (var printer in definitions)
            plan.AppendLine($"Fila planejada: {printer.DisplayName}; fabricante: {printer.Brand}; endereço: {printer.PrinterHostAddress}; porta: {printer.PortNumber}; protocolo: {printer.Protocol}; etiqueta: {printer.GainschaLabelPreset}");
        _diagnosticContext = plan.ToString();
        Log.Append($"Início do deploy. Execução: {runId}.");

        var identityBlockReasons = new List<string>();
        var progress = new SynchronousProgress<DeploymentProgressEvent>(e =>
        {
            OperationLog.RunOnUi(() =>
            {
                if (e.State is (TargetMachineState.PrinterIdentityMismatch or TargetMachineState.PrinterIdentityUnknown) &&
                    !string.IsNullOrWhiteSpace(e.Detail))
                    identityBlockReasons.Add(e.Detail);

                DeploymentTargetRows.ApplyProgress(Targets, e);

                var q = e.PrinterQueueName ?? "geral";
                var stateText = TargetMachineStateDisplay.GetDisplay(e.State);
                var logMessage = e.Message.StartsWith(stateText, StringComparison.OrdinalIgnoreCase)
                    ? e.Message : $"{stateText}: {e.Message}";
                Log.Append($"{e.ComputerName} [{q}]: {logMessage}");
            });
        });

        var diagnosticProgress = new SynchronousProgress<string>(msg =>
        {
            OperationLog.RunOnUi(() => Log.Append(msg));
        });

        try
        {
            var context = await Task.Run(() => _diagnosticCollector is null
                ? Task.FromResult(LocalDiagnosticCollector.CaptureBasicContext())
                : _diagnosticCollector.CollectAsync(deployToken), deployToken);
            _diagnosticContext = plan.ToString() + context;
            _diagnosticLogger?.LogDebug(_diagnosticContext, "DeployContext");
            // APIs nativas síncronas nunca devem bloquear o Dispatcher e impedir o clique em Cancelar.
            await Task.Run(() => _orchestrator.RunAsync(request, journal, progress, deployToken, diagnosticProgress), deployToken);
            deployToken.ThrowIfCancellationRequested();

            LastSummaryText = DeploymentTargetRows.BuildSummaryText(Targets);
            _notificationService.Notify(DeploymentTargetRows.GetOutcome(Targets));
        }
        catch (OperationCanceledException)
        {
            Log.Append("Deploy interrompido. Verificando alterações desta execução.");
            OperationLog.RunOnUi(() => DeploymentTargetRows.MarkIntermediateAsCancelled(Targets));

            if (journal.HasRollbackWork)
            {
                Log.Append(UiStrings.Main_DeployRollbackStarting);
                var rbProgress = new SynchronousProgress<PrinterRemovalProgressEvent>(e =>
                {
                    OperationLog.RunOnUi(() =>
                    {
                        DeploymentTargetRows.ApplyRollbackProgress(Targets, e, journal);
                        Log.Append($"{e.ComputerName}: {e.Message}");
                    });
                });
                try
                {
                    await Task.Run(() => _rollbackRunner.RunAsync(journal, cred, rbProgress, CancellationToken.None));
                    Log.Append(UiStrings.Main_DeployRollbackFinished);
                }
                catch (Exception ex)
                {
                    _diagnosticLogger?.LogError("Falha durante a reversão.", "MainDeploy", ex);
                    Log.Append(string.Format(UiStrings.Main_DeployRollbackErrorFormat, ex.Message));
                }
            }

            OperationLog.RunOnUi(() => DeploymentTargetRows.FinalizeAfterRollback(Targets));

            Log.Append(UiStrings.Main_DeployCooperativeCancelHint);
            LastSummaryText = DeploymentTargetRows.BuildSummaryText(Targets);
            _notificationService.NotifyWarning();
        }
        catch (Exception ex)
        {
            Log.Append("Falha inesperada no deploy: " + DiagnosticLogFormatter.FormatExceptionDetails(ex));
            _diagnosticLogger?.LogError("Falha inesperada no deploy.", "MainDeploy", ex);
            LastSummaryText = DeploymentTargetRows.BuildSummaryText(Targets);
            _notificationService.NotifyWarning();
        }
        finally
        {
            _diagnosticContext += $"\nFim da execução: {DateTimeOffset.Now:O}; duração: {(DateTimeOffset.Now - started).TotalSeconds:F1}s; cancelamento solicitado: {IsCancellationRequested}.\n";
            Log.Append($"Fim do deploy. Execução: {runId}; duração: {(DateTimeOffset.Now - started).TotalSeconds:F1}s; cancelamento solicitado: {IsCancellationRequested}.");
            _deployCts?.Dispose();
            _deployCts = null;
            IsDeployRunning = false;
        }

        var distinctIdentityBlockReasons = identityBlockReasons
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinctIdentityBlockReasons.Length > 0)
        {
            var confirmedMismatch = !Targets.Any(t => t.State == TargetMachineState.PrinterIdentityUnknown);
            await _dialogService.ShowPrinterIdentityBlockAsync(distinctIdentityBlockReasons, confirmedMismatch);
        }
        else if (!string.IsNullOrEmpty(LastSummaryText))
        {
            await _dialogService.ShowSummaryAsync(UiStrings.Main_SummaryDialogTitle, LastSummaryText, SummaryTone.Info);
        }
    }

    private bool CanDeploy() => !IsDeployRunning;

    [RelayCommand(CanExecute = nameof(CanCancelDeploy))]
    private void CancelDeploy()
    {
        if (_deployCts is null || IsCancellationRequested) return;
        IsCancellationRequested = true;
        Log.Append(UiStrings.Main_DeployCancelRequested + " Encerrando a etapa atual; novas etapas não serão iniciadas.");
        foreach (var row in Targets.Where(r => DeploymentTargetRows.IsIntermediateState(r.State)))
            row.Message = "Cancelamento solicitado — aguardando encerramento";
        _deployCts.Cancel();
    }

    private bool CanCancelDeploy() => IsDeployRunning && !IsCancellationRequested;

    private void AddTargetRows(string computerName, IEnumerable<PrinterQueueDefinition> definitions,
        TargetMachineState state, string? message)
    {
        foreach (var def in definitions)
        {
            var row = new TargetRowViewModel
            {
                ComputerName = computerName,
                PrinterQueueName = def.DisplayName,
                ExpectedPortName = PrinterPortNaming.BuildPortName(def.PrinterHostAddress, DefaultDeployPort),
                State = state
            };
            if (message is not null)
                row.Message = message;
            Targets.Add(row);
            ConfigureTargetRowDisplay(row, computerName);
        }
    }

    private void ConfigureTargetRowDisplay(TargetRowViewModel row, string computerName)
    {
        var isLocal = _localMachineIdentity.IsLocalMachine(computerName);
        row.IsLocalMachine = isLocal;
        row.ComputerNameDisplay = isLocal
            ? $"{computerName} {UiStrings.Main_LocalComputerSuffix}"
            : computerName;
    }

    [RelayCommand]
    private void OpenLogFolder() => Log.OpenFolder();

    [RelayCommand]
    private void OpenRemovalWizard() => ShowDialog<Views.RemovalWizardWindow>();

    [RelayCommand]
    private void OpenPrinterNetworkTest() => ShowDialog<Views.PrinterNetworkTestWindow>();

    private void ShowDialog<TWindow>() where TWindow : Window
    {
        var window = _serviceProvider.GetRequiredService<TWindow>();
        var owner = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsLoaded && w.IsVisible && !ReferenceEquals(w, window));
        if (owner is not null)
            window.Owner = owner;
        window.ShowDialog();
    }
}
