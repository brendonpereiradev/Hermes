using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrinterInstall.App.Converters;
using PrinterInstall.App.Resources;
using PrinterInstall.App.Services;
using PrinterInstall.Core.Logging;
using PrinterInstall.Core.Models;
using PrinterInstall.Core.Network;
using PrinterInstall.Core.Orchestration;
using PrinterInstall.Core.Remote;
using PrinterInstall.Core.Validation;

namespace PrinterInstall.App.ViewModels;

public enum ComputerPingStatus
{
    None,
    Checking,
    Online,
    Offline
}

public partial class RemovalWizardViewModel : ObservableObject
{
    private readonly ISessionContext _session;
    private readonly IRemotePrinterOperations _remote;
    private readonly PrinterControlOrchestrator _orchestrator;
    private readonly LocalMachineIdentity _localMachineIdentity;
    private readonly IDeploymentNotificationService _notificationService;
    private readonly IConfirmationDialogService _dialogService;
    private readonly IPrinterPingService _pingService;
    private readonly IFastHostReachabilityChecker _reachabilityChecker;
    private readonly IDiagnosticFileLogger? _diagnosticLogger;

    private readonly Dictionary<string, List<PrinterRemovalQueueItem>> _selectionsByComputer = new();
    private readonly Dictionary<string, List<PrinterRenameItem>> _renamesByComputer = new();
    private List<string> _machineOrder = new();
    private int _machineIndex;
    private CancellationTokenSource? _loadCts;

    public RemovalWizardViewModel(
        ISessionContext session,
        IRemotePrinterOperations remote,
        PrinterControlOrchestrator orchestrator,
        ILogExportService? logExportService = null,
        LocalMachineIdentity? localMachineIdentity = null,
        IDeploymentNotificationService? notificationService = null,
        IConfirmationDialogService? dialogService = null,
        IPrinterPingService? pingService = null,
        IFastHostReachabilityChecker? reachabilityChecker = null,
        IDiagnosticFileLogger? diagnosticLogger = null)
    {
        _session = session;
        _remote = remote;
        _orchestrator = orchestrator;
        Log = new OperationLog(logExportService ?? new LogExportService(), diagnosticLogger, "RemovalWizard");
        Log.PropertyChanged += OnLogChanged;
        _localMachineIdentity = localMachineIdentity ?? new LocalMachineIdentity();
        _notificationService = notificationService ?? new DeploymentNotificationService();
        _dialogService = dialogService ?? new ConfirmationDialogService();
        _pingService = pingService ?? new NullPrinterPingService();
        _reachabilityChecker = reachabilityChecker ?? new NullFastHostReachabilityChecker();
        _diagnosticLogger = diagnosticLogger;
        QueuesForCurrentComputer.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowQueuesEmptyHint));

        if (_session.IsLocalOnly)
            ComputersText = _localMachineIdentity.GetPrimaryLocalName();
    }

    /// <summary>Sessão sem domínio: a lista de computadores fica travada neste computador.</summary>
    public bool IsLocalOnly => _session.IsLocalOnly;

    [ObservableProperty] private int _currentStepIndex;

    [ObservableProperty] private string _computersText = "";

    [ObservableProperty] private string _currentComputerName = "";
    [ObservableProperty] private string _currentStepLabel = "";
    [ObservableProperty] private bool _isLoadingQueues;
    [ObservableProperty] private string? _queuesLoadError;

    [ObservableProperty] private ComputerPingStatus _pingStatus = ComputerPingStatus.None;
    [ObservableProperty] private string _loadingStatusMessage = "";
    [ObservableProperty] private bool _isComputerOffline;
    [ObservableProperty] private string _pingStatusBadgeText = "";

    [ObservableProperty] private int _reviewTotalRemovals;
    [ObservableProperty] private int _reviewTotalRenames;
    [ObservableProperty] private int _reviewTotalComputers;

    public bool HasPingBadge => PingStatus != ComputerPingStatus.None;

    private static bool IsCurrentDarkTheme()
    {
        try
        {
            return TargetMachineStateToBrushConverter.IsDarkTheme ||
                   Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;
        }
        catch
        {
            return TargetMachineStateToBrushConverter.IsDarkTheme;
        }
    }

    public string PingBadgeForeground => PingBadgePalette.Foreground(PingStatus, IsCurrentDarkTheme());

    public string PingBadgeDot => PingBadgePalette.Dot(PingStatus, IsCurrentDarkTheme());

    partial void OnPingStatusChanged(ComputerPingStatus value)
    {
        OnPropertyChanged(nameof(HasPingBadge));
        OnPropertyChanged(nameof(PingBadgeForeground));
        OnPropertyChanged(nameof(PingBadgeDot));
    }

    partial void OnIsComputerOfflineChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowQueuesEmptyHint));
        OnPropertyChanged(nameof(ShowComputerOfflineHint));
        OnPropertyChanged(nameof(CanResetSpooler));
        ResetCurrentMachineSpoolerCommand.NotifyCanExecuteChanged();
    }

    public ObservableCollection<SelectableQueueRow> QueuesForCurrentComputer { get; } = new();

    public bool ShowQueuesEmptyHint =>
        !IsLoadingQueues &&
        !IsComputerOffline &&
        string.IsNullOrEmpty(QueuesLoadError) &&
        QueuesForCurrentComputer.Count == 0;

    public bool ShowComputerOfflineHint =>
        !IsLoadingQueues &&
        IsComputerOffline;

    [ObservableProperty] private string _reviewSummary = "";

    public OperationLog Log { get; }

    public string LogText
    {
        get => Log.Text;
        set => Log.Text = value;
    }

    [ObservableProperty] private bool _isExecuting;

    public bool CanExecute => CurrentStepIndex == 2 && !IsExecuting;

    public bool CanClose => CurrentStepIndex == 3 && !IsExecuting;

    public bool CanExportLog => CurrentStepIndex == 3 && !IsExecuting && Log.CanExport;

    public event EventHandler? CloseRequested;

    [ObservableProperty] private bool _isResettingSpooler;

    partial void OnIsResettingSpoolerChanged(bool value)
    {
        OnPropertyChanged(nameof(CanResetSpooler));
        OnPropertyChanged(nameof(CanAdvanceQueueStep));
        OnPropertyChanged(nameof(CanRetryCurrentMachine));
        ResetCurrentMachineSpoolerCommand.NotifyCanExecuteChanged();
        NextQueueStepCommand.NotifyCanExecuteChanged();
        RetryCurrentMachineCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentComputerNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanResetSpooler));
        ResetCurrentMachineSpoolerCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExecutingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExecute));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(CanExportLog));
        CloseCommand.NotifyCanExecuteChanged();
        ExportLogCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanExecute));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(CanExportLog));
        OnPropertyChanged(nameof(CanResetSpooler));
        OnPropertyChanged(nameof(CanAdvanceQueueStep));
        OnPropertyChanged(nameof(CanRetryCurrentMachine));
        CloseCommand.NotifyCanExecuteChanged();
        ExportLogCommand.NotifyCanExecuteChanged();
        ResetCurrentMachineSpoolerCommand.NotifyCanExecuteChanged();
        NextQueueStepCommand.NotifyCanExecuteChanged();
        RetryCurrentMachineCommand.NotifyCanExecuteChanged();
    }

    private void OnLogChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OperationLog.Text))
            return;

        OnPropertyChanged(nameof(LogText));
        OnPropertyChanged(nameof(CanExportLog));
        ExportLogCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingQueuesChanged(bool value)
    {
        OnPropertyChanged(nameof(CanResetSpooler));
        OnPropertyChanged(nameof(CanAdvanceQueueStep));
        OnPropertyChanged(nameof(CanRetryCurrentMachine));
        NextQueueStepCommand.NotifyCanExecuteChanged();
        ResetCurrentMachineSpoolerCommand.NotifyCanExecuteChanged();
        RetryCurrentMachineCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowQueuesEmptyHint));
        OnPropertyChanged(nameof(ShowComputerOfflineHint));
    }

    partial void OnQueuesLoadErrorChanged(string? value) => OnPropertyChanged(nameof(ShowQueuesEmptyHint));

    [RelayCommand]
    private void AddThisComputer() =>
        ComputersText = ComputerListText.WithThisComputer(ComputersText, _localMachineIdentity);

    [RelayCommand]
    private async Task StartAsync()
    {
        if (_session.Credential is null)
        {
            Log.Append(UiStrings.Removal_NotAuthenticated);
            return;
        }

        var names = ComputerNameListParser.Parse(ComputersText);
        if (names.Count == 0)
        {
            Log.Append(UiStrings.Removal_Validation_ComputersRequired);
            return;
        }

        if (_session.IsLocalOnly)
        {
            var remoteNames = names.Where(n => !_localMachineIdentity.IsLocalMachine(n)).ToList();
            if (remoteNames.Count > 0)
            {
                Log.Append(string.Format(UiStrings.Removal_LocalOnlyRemoteBlockedFormat, string.Join(", ", remoteNames)));
                return;
            }
        }

        _machineOrder = names.ToList();
        _selectionsByComputer.Clear();
        _renamesByComputer.Clear();
        _machineIndex = 0;
        CurrentStepIndex = 1;
        await LoadCurrentMachineAsync().ConfigureAwait(true);
    }

    public bool CanAdvanceQueueStep =>
        CurrentStepIndex == 1 &&
        !IsLoadingQueues &&
        !IsResettingSpooler;

    [RelayCommand(CanExecute = nameof(CanAdvanceQueueStep))]
    private async Task NextQueueStepAsync()
    {
        _loadCts?.Cancel();
        CaptureCurrentSelection();
        if (_machineIndex + 1 < _machineOrder.Count)
        {
            _machineIndex++;
            await LoadCurrentMachineAsync().ConfigureAwait(true);
            return;
        }

        BuildReviewSummary();
        CurrentStepIndex = 2;
    }

    public bool CanRetryCurrentMachine =>
        CurrentStepIndex == 1 &&
        !IsLoadingQueues &&
        !IsResettingSpooler;

    [RelayCommand(CanExecute = nameof(CanRetryCurrentMachine))]
    private async Task RetryCurrentMachineAsync()
    {
        await LoadCurrentMachineAsync().ConfigureAwait(true);
    }

    public bool CanResetSpooler =>
        CurrentStepIndex == 1 &&
        !IsResettingSpooler &&
        !IsLoadingQueues &&
        !IsComputerOffline &&
        !string.IsNullOrWhiteSpace(CurrentComputerName);

    [RelayCommand(CanExecute = nameof(CanResetSpooler))]
    private async Task ResetCurrentMachineSpoolerAsync()
    {
        if (_session.Credential is null)
        {
            Log.Append(UiStrings.Removal_NotAuthenticated);
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentComputerName))
            return;

        var confirmed = await _dialogService.ConfirmSpoolerResetAsync(CurrentComputerName).ConfigureAwait(true);
        if (!confirmed)
            return;

        IsResettingSpooler = true;
        Log.Append(string.Format(UiStrings.Removal_ResetSpoolerRunning, CurrentComputerName));

        try
        {
            var result = await _remote.ResetSpoolerServiceAsync(
                CurrentComputerName,
                _session.Credential,
                purgeJobs: true).ConfigureAwait(true);

            if (result.IsSuccess)
            {
                Log.Append(string.Format(UiStrings.Removal_ResetSpoolerSuccess, CurrentComputerName));
                _notificationService.NotifySuccess();
                await LoadCurrentMachineAsync().ConfigureAwait(true);
            }
            else
            {
                Log.Append(string.Format(UiStrings.Removal_ResetSpoolerErrorFormat, CurrentComputerName, result.ErrorMessage));
                _notificationService.NotifyError();
            }
        }
        catch (Exception ex)
        {
            Log.Append(string.Format(UiStrings.Removal_ResetSpoolerErrorFormat, CurrentComputerName, ex.Message));
            _notificationService.NotifyError();
        }
        finally
        {
            IsResettingSpooler = false;
        }
    }

    private static bool HasMeaningfulRename(SelectableQueueRow r)
    {
        var t = r.NewName?.Trim() ?? "";
        return t.Length > 0 && !string.Equals(t, r.Name, StringComparison.OrdinalIgnoreCase);
    }

    private void OnQueueRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SelectableQueueRow.IsSelected) or nameof(SelectableQueueRow.NewName))
        {
            NextQueueStepCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (_session.Credential is null)
        {
            Log.Append(UiStrings.Removal_NotAuthenticated);
            return;
        }

        IsExecuting = true;
        CurrentStepIndex = 3;
        try
        {
            var targets = new List<PrinterControlTarget>();
            foreach (var computer in _machineOrder)
            {
                _selectionsByComputer.TryGetValue(computer, out var removes);
                _renamesByComputer.TryGetValue(computer, out var renames);
                removes ??= new List<PrinterRemovalQueueItem>();
                renames ??= new List<PrinterRenameItem>();
                if (removes.Count == 0 && renames.Count == 0)
                    continue;
                targets.Add(new PrinterControlTarget
                {
                    ComputerName = computer,
                    QueuesToRemove = removes,
                    Renames = renames
                });
            }

            if (targets.Count == 0)
            {
                Log.Append(UiStrings.Removal_NoPrintersSelected);
                return;
            }

            var request = new PrinterControlRequest
            {
                DomainCredential = _session.Credential,
                Targets = targets
            };

            var totalActions = targets.Sum(t => t.QueuesToRemove.Count + t.Renames.Count);
            var errorCount = 0;
            var warningCount = 0;
            var queueSuccessCount = 0;
            var failureLines = new List<string>();

            var progress = new SynchronousProgress<PrinterRemovalProgressEvent>(ev =>
            {
                Log.Append($"{ev.ComputerName}: {ev.State} - {ev.Message}");

                if (ev.State == PrinterRemovalProgressState.Error)
                {
                    errorCount++;
                    failureLines.Add($"{ev.ComputerName} [{ev.PrinterQueueName ?? "-"}] — {ev.Message}");
                }
                else if (ev.State == PrinterRemovalProgressState.Warning)
                {
                    warningCount++;
                }
                else if (ev.State == PrinterRemovalProgressState.RollbackSucceeded)
                {
                    queueSuccessCount++;
                }
            });

            await _orchestrator.RunAsync(request, progress).ConfigureAwait(true);
            Log.Append(UiStrings.Removal_Finished);

            var totalRenames = targets.Sum(t => t.Renames.Count);
            var renameErrors = failureLines.Count(f => targets.Any(t => t.Renames.Any(r => f.Contains(r.CurrentName, StringComparison.OrdinalIgnoreCase))));
            var successfulRenames = Math.Max(0, totalRenames - renameErrors);
            var effectiveSuccessCount = queueSuccessCount + successfulRenames;

            var outcome = GetControlOutcome(totalActions, effectiveSuccessCount, warningCount, errorCount);
            _notificationService.Notify(outcome);

            var summary = BuildSummaryText(effectiveSuccessCount, warningCount, errorCount, failureLines);
            Log.Append(summary);

            var (title, tone) = GetSummaryHeading(outcome);
            await _dialogService.ShowSummaryAsync(title, summary, tone).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            Log.Append(UiStrings.Removal_Cancelled);
            _notificationService.NotifyWarning();
        }
        catch (Exception ex)
        {
            _diagnosticLogger?.LogError(ex.Message, "RemovalWizard", ex);
            var errorFormatted = DiagnosticLogFormatter.FormatException(ex);
            Log.Append(string.Format(UiStrings.Removal_LogErrorFormat, errorFormatted));
            _notificationService.NotifyError();

            await _dialogService.ShowSummaryAsync(
                UiStrings.Removal_SummaryTitleError,
                string.Format(UiStrings.Removal_LogErrorFormat, errorFormatted),
                SummaryTone.Error).ConfigureAwait(true);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private static OperationOutcome GetControlOutcome(int totalActions, int successCount, int warningCount, int errorCount)
    {
        if (totalActions == 0)
            return OperationOutcome.None;
        if (errorCount == 0 && warningCount == 0)
            return OperationOutcome.Success;
        if (errorCount > 0 && successCount == 0 && errorCount >= totalActions)
            return OperationOutcome.Error;
        return OperationOutcome.Warning;
    }

    /// <summary>Título e tom da caixa de resumo: responde de cara se as alterações deram certo.</summary>
    internal static (string Title, SummaryTone Tone) GetSummaryHeading(OperationOutcome outcome) => outcome switch
    {
        OperationOutcome.Error => (UiStrings.Removal_SummaryTitleError, SummaryTone.Error),
        OperationOutcome.Warning => (UiStrings.Removal_SummaryTitleWarning, SummaryTone.Warning),
        _ => (UiStrings.Removal_SummaryTitleSuccess, SummaryTone.Success)
    };

    internal static string BuildSummaryText(int successCount, int warningCount, int errorCount, IReadOnlyList<string> failureLines)
    {
        // Só as contagens diferentes de zero, numa frase: "1 concluída, 2 erros."
        var counts = new[]
            {
                CountText(successCount, UiStrings.Removal_SummarySucceededOne, UiStrings.Removal_SummarySucceededMany),
                CountText(warningCount, UiStrings.Removal_SummaryWarningOne, UiStrings.Removal_SummaryWarningMany),
                CountText(errorCount, UiStrings.Removal_SummaryErrorOne, UiStrings.Removal_SummaryErrorMany)
            }
            .Where(c => c is not null)
            .ToList();
        // Sem nenhuma contagem a caixa nem abriria; "0 concluídas" ao menos diz que nada mudou.
        if (counts.Count == 0)
            counts.Add(string.Format(UiStrings.Removal_SummarySucceededMany, 0));

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(", ", counts) + ".");
        if (failureLines.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(UiStrings.Removal_SummaryFailuresHeader);
            foreach (var line in failureLines)
            {
                sb.AppendLine(line);
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static string? CountText(int count, string oneFormat, string manyFormat) =>
        count == 0 ? null : string.Format(count == 1 ? oneFormat : manyFormat, count);

    private async Task LoadCurrentMachineAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        IsLoadingQueues = true;
        IsComputerOffline = false;
        QueuesLoadError = null;
        PingStatus = ComputerPingStatus.None;
        PingStatusBadgeText = string.Empty;

        foreach (var row in QueuesForCurrentComputer.ToList())
            row.PropertyChanged -= OnQueueRowPropertyChanged;
        QueuesForCurrentComputer.Clear();

        CurrentComputerName = _machineOrder[_machineIndex];
        CurrentStepLabel = string.Format(
            UiStrings.Removal_StepLabelFormat,
            _machineIndex + 1,
            _machineOrder.Count,
            CurrentComputerName);

        try
        {
            var isLocal = _localMachineIdentity.IsLocalMachine(CurrentComputerName);
            if (!isLocal)
            {
                PingStatus = ComputerPingStatus.Checking;
                PingStatusBadgeText = UiStrings.Removal_Ping_Checking;
                LoadingStatusMessage = string.Format(UiStrings.Removal_Ping_TestingHostFormat, CurrentComputerName);

                var isAlive = await CheckHostAliveAsync(CurrentComputerName, ct).ConfigureAwait(true);
                ct.ThrowIfCancellationRequested();

                if (!isAlive)
                {
                    PingStatus = ComputerPingStatus.Offline;
                    PingStatusBadgeText = UiStrings.Removal_Ping_Offline;
                    IsComputerOffline = true;
                    QueuesLoadError = string.Format(UiStrings.Removal_Ping_ComputerOfflineFormat, CurrentComputerName);
                    Log.Append(string.Format(UiStrings.Removal_LogListPrintersFailedFormat, CurrentComputerName, QueuesLoadError));
                    return;
                }
            }

            PingStatus = ComputerPingStatus.Online;
            PingStatusBadgeText = UiStrings.Removal_Ping_Online;
            LoadingStatusMessage = UiStrings.Removal_Ping_ConnectedLoadingPrinters;

            var cred = _session.Credential!;
            var list = await _remote.ListPrinterQueuesAsync(CurrentComputerName, cred, ct).ConfigureAwait(true);
            ct.ThrowIfCancellationRequested();

            foreach (var q in list.OrderBy(q => q.Name, StringComparer.OrdinalIgnoreCase))
            {
                var row = new SelectableQueueRow
                {
                    Name = q.Name,
                    PortName = q.PortName,
                    IsSelected = false
                };
                row.PropertyChanged += OnQueueRowPropertyChanged;
                QueuesForCurrentComputer.Add(row);
            }
        }
        catch (OperationCanceledException)
        {
            // Operação cancelada cooperativamente (ex: avanço, skip ou fechamento)
        }
        catch (Exception ex)
        {
            QueuesLoadError = ex.Message;
            Log.Append(string.Format(UiStrings.Removal_LogListPrintersFailedFormat, CurrentComputerName, ex.Message));
        }
        finally
        {
            IsLoadingQueues = false;
        }
    }

    private async Task<bool> CheckHostAliveAsync(string host, CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pingTask = _pingService.PingAsync(host, TimeSpan.FromSeconds(2), linkedCts.Token);
        var reachTask = _reachabilityChecker.CheckReachabilityAsync(host, linkedCts.Token);

        var tasks = new List<Task> { pingTask, reachTask };
        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks).ConfigureAwait(true);
            tasks.Remove(finished);

            if (finished == pingTask)
            {
                try
                {
                    var ok = await pingTask.ConfigureAwait(true);
                    if (ok)
                    {
                        linkedCts.Cancel();
                        return true;
                    }
                }
                catch
                {
                    // Falha no ICMP; aguarda o reachTask
                }
            }
            else if (finished == reachTask)
            {
                try
                {
                    var (ok, _) = await reachTask.ConfigureAwait(true);
                    if (ok)
                    {
                        linkedCts.Cancel();
                        return true;
                    }
                }
                catch
                {
                    // Falha no TCP 135/445; aguarda o pingTask
                }
            }
        }

        return false;
    }

    private void CaptureCurrentSelection()
    {
        var chosen = QueuesForCurrentComputer
            .Where(r => r.IsSelected)
            .Select(r => new PrinterRemovalQueueItem(r.Name, r.PortName))
            .ToList();
        _selectionsByComputer[CurrentComputerName] = chosen;

        var renames = QueuesForCurrentComputer
            .Where(HasMeaningfulRename)
            .Select(r => new PrinterRenameItem(r.Name, r.NewName.Trim()))
            .ToList();
        _renamesByComputer[CurrentComputerName] = renames;
    }

    private void BuildReviewSummary()
    {
        var lines = new List<string>();
        var totalRemoves = 0;
        var totalRenames = 0;
        var totalComputers = 0;
        foreach (var computer in _machineOrder)
        {
            _selectionsByComputer.TryGetValue(computer, out var queues);
            _renamesByComputer.TryGetValue(computer, out var renames);
            queues ??= new List<PrinterRemovalQueueItem>();
            renames ??= new List<PrinterRenameItem>();
            if (queues.Count == 0 && renames.Count == 0)
            {
                lines.Add(string.Format(UiStrings.Removal_ReviewNothingFormat, computer));
                continue;
            }
            totalComputers++;
            totalRemoves += queues.Count;
            totalRenames += renames.Count;
            foreach (var rename in renames)
            {
                lines.Add(string.Format(
                    UiStrings.Removal_ReviewRenameFormat,
                    computer,
                    rename.CurrentName,
                    rename.NewName));
            }
            foreach (var q in queues)
            {
                lines.Add(string.Format(
                    UiStrings.Removal_ReviewRemoveFormat,
                    computer,
                    q.PrinterName,
                    q.PortName ?? "-"));
            }
        }
        ReviewSummary = string.Join(Environment.NewLine, lines);
        ReviewTotalRemovals = totalRemoves;
        ReviewTotalRenames = totalRenames;
        ReviewTotalComputers = totalComputers;
    }

    [RelayCommand(CanExecute = nameof(CanExportLog))]
    private void ExportLog()
    {
        Log.SetSensitiveValue(_session.Credential?.Password);
        var operatorId = OperationLog.DescribeOperator(_session.Credential);

        Log.Export(
            $"PrinterInstall_Controle_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt",
            logText => LogReportFormatter.FormatRemovalReport(
                operatorId,
                Environment.MachineName,
                ReviewSummary,
                logText,
                diagnosticLogPath: _diagnosticLogger?.CurrentLogFilePath,
                diagnosticContext: LocalDiagnosticCollector.CaptureBasicContext(),
                diagnosticLogText: _diagnosticLogger?.ReadSessionLog()));
    }

    [RelayCommand]
    private void OpenLogFolder() => Log.OpenFolder();

    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Close()
    {
        _loadCts?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
