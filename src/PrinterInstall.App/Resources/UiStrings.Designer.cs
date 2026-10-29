#nullable enable
namespace PrinterInstall.App.Resources;

[System.CodeDom.Compiler.GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[System.Diagnostics.DebuggerNonUserCode]
[System.Runtime.CompilerServices.CompilerGenerated]
public class UiStrings
{
    private static System.Resources.ResourceManager? s_resourceManager;
    private static readonly System.Globalization.CultureInfo ResourceCulture = new("pt-BR");

    internal UiStrings() { }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static System.Resources.ResourceManager ResourceManager =>
        s_resourceManager ??= new System.Resources.ResourceManager(
            "PrinterInstall.App.Resources.UiStrings",
            typeof(UiStrings).Assembly);

    public static string Main_NotAuthenticated => ResourceManager.GetString(nameof(Main_NotAuthenticated), ResourceCulture)!;
    public static string Main_Validation_ComputersRequired => ResourceManager.GetString(nameof(Main_Validation_ComputersRequired), ResourceCulture)!;
    public static string Main_Validation_DisplayNameRequired => ResourceManager.GetString(nameof(Main_Validation_DisplayNameRequired), ResourceCulture)!;
    public static string Main_Validation_PrinterHostRequired => ResourceManager.GetString(nameof(Main_Validation_PrinterHostRequired), ResourceCulture)!;
    public static string Main_Validation_GainschaLabelPresetRequired => ResourceManager.GetString(nameof(Main_Validation_GainschaLabelPresetRequired), ResourceCulture)!;
    public static string Main_InvalidComputerNameFormat => ResourceManager.GetString(nameof(Main_InvalidComputerNameFormat), ResourceCulture)!;
    public static string Main_LocalComputerSuffix => ResourceManager.GetString(nameof(Main_LocalComputerSuffix), ResourceCulture)!;
    public static string Main_LocalOnlyRemoteBlocked => ResourceManager.GetString(nameof(Main_LocalOnlyRemoteBlocked), ResourceCulture)!;
    public static string Main_SummarySucceededOne => ResourceManager.GetString(nameof(Main_SummarySucceededOne), ResourceCulture)!;
    public static string Main_SummarySucceededMany => ResourceManager.GetString(nameof(Main_SummarySucceededMany), ResourceCulture)!;
    public static string Main_SummaryWarnedOne => ResourceManager.GetString(nameof(Main_SummaryWarnedOne), ResourceCulture)!;
    public static string Main_SummaryWarnedMany => ResourceManager.GetString(nameof(Main_SummaryWarnedMany), ResourceCulture)!;
    public static string Main_SummarySkippedOne => ResourceManager.GetString(nameof(Main_SummarySkippedOne), ResourceCulture)!;
    public static string Main_SummarySkippedMany => ResourceManager.GetString(nameof(Main_SummarySkippedMany), ResourceCulture)!;
    public static string Main_SummaryErrorOne => ResourceManager.GetString(nameof(Main_SummaryErrorOne), ResourceCulture)!;
    public static string Main_SummaryErrorMany => ResourceManager.GetString(nameof(Main_SummaryErrorMany), ResourceCulture)!;
    public static string Main_SummaryDriverMissingOne => ResourceManager.GetString(nameof(Main_SummaryDriverMissingOne), ResourceCulture)!;
    public static string Main_SummaryDriverMissingMany => ResourceManager.GetString(nameof(Main_SummaryDriverMissingMany), ResourceCulture)!;
    public static string Main_SummaryRolledBackOne => ResourceManager.GetString(nameof(Main_SummaryRolledBackOne), ResourceCulture)!;
    public static string Main_SummaryRolledBackMany => ResourceManager.GetString(nameof(Main_SummaryRolledBackMany), ResourceCulture)!;
    public static string Main_SummaryDeployCancelledOne => ResourceManager.GetString(nameof(Main_SummaryDeployCancelledOne), ResourceCulture)!;
    public static string Main_SummaryDeployCancelledMany => ResourceManager.GetString(nameof(Main_SummaryDeployCancelledMany), ResourceCulture)!;
    public static string Main_SummaryInProgressOne => ResourceManager.GetString(nameof(Main_SummaryInProgressOne), ResourceCulture)!;
    public static string Main_SummaryInProgressMany => ResourceManager.GetString(nameof(Main_SummaryInProgressMany), ResourceCulture)!;
    public static string Main_SummaryFailureLineFormat => ResourceManager.GetString(nameof(Main_SummaryFailureLineFormat), ResourceCulture)!;
    public static string Main_SummaryTitleSuccess => ResourceManager.GetString(nameof(Main_SummaryTitleSuccess), ResourceCulture)!;
    public static string Main_SummaryTitleWarning => ResourceManager.GetString(nameof(Main_SummaryTitleWarning), ResourceCulture)!;
    public static string Main_SummaryTitleError => ResourceManager.GetString(nameof(Main_SummaryTitleError), ResourceCulture)!;
    public static string Main_SummaryTitleCancelled => ResourceManager.GetString(nameof(Main_SummaryTitleCancelled), ResourceCulture)!;
    public static string Main_DeployCancelRequested => ResourceManager.GetString(nameof(Main_DeployCancelRequested), ResourceCulture)!;
    public static string Main_DeployRollbackStarting => ResourceManager.GetString(nameof(Main_DeployRollbackStarting), ResourceCulture)!;
    public static string Main_DeployRollbackFinished => ResourceManager.GetString(nameof(Main_DeployRollbackFinished), ResourceCulture)!;
    public static string Main_DeployRollbackErrorFormat => ResourceManager.GetString(nameof(Main_DeployRollbackErrorFormat), ResourceCulture)!;
    public static string Main_DeployCooperativeCancelHint => ResourceManager.GetString(nameof(Main_DeployCooperativeCancelHint), ResourceCulture)!;
    public static string Main_DeployCancelledRowMessage => ResourceManager.GetString(nameof(Main_DeployCancelledRowMessage), ResourceCulture)!;
    public static string Main_RollbackPreparingOnHost => ResourceManager.GetString(nameof(Main_RollbackPreparingOnHost), ResourceCulture)!;
    public static string Login_Validation_DomainUserRequired => ResourceManager.GetString(nameof(Login_Validation_DomainUserRequired), ResourceCulture)!;
    public static string Login_Validation_PasswordRequired => ResourceManager.GetString(nameof(Login_Validation_PasswordRequired), ResourceCulture)!;
    public static string Login_Validation_DomainTitle => ResourceManager.GetString(nameof(Login_Validation_DomainTitle), ResourceCulture)!;
    public static string Login_Validation_DomainRequired => ResourceManager.GetString(nameof(Login_Validation_DomainRequired), ResourceCulture)!;
    public static string Removal_NotAuthenticated => ResourceManager.GetString(nameof(Removal_NotAuthenticated), ResourceCulture)!;
    public static string Removal_Validation_ComputersRequired => ResourceManager.GetString(nameof(Removal_Validation_ComputersRequired), ResourceCulture)!;
    public static string Removal_LocalOnlyRemoteBlockedFormat => ResourceManager.GetString(nameof(Removal_LocalOnlyRemoteBlockedFormat), ResourceCulture)!;
    public static string Removal_NoPrintersSelected => ResourceManager.GetString(nameof(Removal_NoPrintersSelected), ResourceCulture)!;
    public static string Removal_Finished => ResourceManager.GetString(nameof(Removal_Finished), ResourceCulture)!;
    public static string Removal_StepLabelFormat => ResourceManager.GetString(nameof(Removal_StepLabelFormat), ResourceCulture)!;
    public static string Removal_ReviewNothingFormat => ResourceManager.GetString(nameof(Removal_ReviewNothingFormat), ResourceCulture)!;
    public static string Removal_ReviewRemoveFormat => ResourceManager.GetString(nameof(Removal_ReviewRemoveFormat), ResourceCulture)!;
    public static string Removal_ReviewRenameFormat => ResourceManager.GetString(nameof(Removal_ReviewRenameFormat), ResourceCulture)!;
    public static string Removal_LogListPrintersFailedFormat => ResourceManager.GetString(nameof(Removal_LogListPrintersFailedFormat), ResourceCulture)!;
    public static string Removal_LogErrorFormat => ResourceManager.GetString(nameof(Removal_LogErrorFormat), ResourceCulture)!;
    public static string NetworkTest_Validation_HostRequired => ResourceManager.GetString(nameof(NetworkTest_Validation_HostRequired), ResourceCulture)!;
    public static string NetworkTest_Progress_Connectivity => ResourceManager.GetString(nameof(NetworkTest_Progress_Connectivity), ResourceCulture)!;
    public static string NetworkTest_Progress_Sending => ResourceManager.GetString(nameof(NetworkTest_Progress_Sending), ResourceCulture)!;
    public static string NetworkTest_Progress_Identity => ResourceManager.GetString(nameof(NetworkTest_Progress_Identity), ResourceCulture)!;
    public static string NetworkTest_Cancelled => ResourceManager.GetString(nameof(NetworkTest_Cancelled), ResourceCulture)!;
    public static string Main_Validation_InvalidHostAddressFormat => ResourceManager.GetString(nameof(Main_Validation_InvalidHostAddressFormat), ResourceCulture)!;
    public static string Main_Validation_InversionDetectedFormat => ResourceManager.GetString(nameof(Main_Validation_InversionDetectedFormat), ResourceCulture)!;
    public static string Log_ExportSuccessFormat => ResourceManager.GetString(nameof(Log_ExportSuccessFormat), ResourceCulture)!;
    public static string Log_ExportErrorFormat => ResourceManager.GetString(nameof(Log_ExportErrorFormat), ResourceCulture)!;
    public static string Log_OpenFolderErrorFormat => ResourceManager.GetString(nameof(Log_OpenFolderErrorFormat), ResourceCulture)!;
    public static string Removal_SummaryTitleSuccess => ResourceManager.GetString(nameof(Removal_SummaryTitleSuccess), ResourceCulture)!;
    public static string Removal_SummaryTitleWarning => ResourceManager.GetString(nameof(Removal_SummaryTitleWarning), ResourceCulture)!;
    public static string Removal_SummaryTitleError => ResourceManager.GetString(nameof(Removal_SummaryTitleError), ResourceCulture)!;
    public static string Removal_SummarySucceededOne => ResourceManager.GetString(nameof(Removal_SummarySucceededOne), ResourceCulture)!;
    public static string Removal_SummarySucceededMany => ResourceManager.GetString(nameof(Removal_SummarySucceededMany), ResourceCulture)!;
    public static string Removal_SummaryWarningOne => ResourceManager.GetString(nameof(Removal_SummaryWarningOne), ResourceCulture)!;
    public static string Removal_SummaryWarningMany => ResourceManager.GetString(nameof(Removal_SummaryWarningMany), ResourceCulture)!;
    public static string Removal_SummaryErrorOne => ResourceManager.GetString(nameof(Removal_SummaryErrorOne), ResourceCulture)!;
    public static string Removal_SummaryErrorMany => ResourceManager.GetString(nameof(Removal_SummaryErrorMany), ResourceCulture)!;
    public static string Removal_SummaryFailuresHeader => ResourceManager.GetString(nameof(Removal_SummaryFailuresHeader), ResourceCulture)!;
    public static string Removal_Cancelled => ResourceManager.GetString(nameof(Removal_Cancelled), ResourceCulture)!;
    public static string Main_DeployWarningDialogTitle => ResourceManager.GetString(nameof(Main_DeployWarningDialogTitle), ResourceCulture)!;
    public static string Main_DeployWarningHeader => ResourceManager.GetString(nameof(Main_DeployWarningHeader), ResourceCulture)!;
    public static string Main_DeployWarningQuestion => ResourceManager.GetString(nameof(Main_DeployWarningQuestion), ResourceCulture)!;
    public static string Main_DeployWarningProceedButton => ResourceManager.GetString(nameof(Main_DeployWarningProceedButton), ResourceCulture)!;
    public static string Main_DeployWarningCancelButton => ResourceManager.GetString(nameof(Main_DeployWarningCancelButton), ResourceCulture)!;
    public static string Main_PrinterIdentityBlockTitle => ResourceManager.GetString(nameof(Main_PrinterIdentityBlockTitle), ResourceCulture)!;
    public static string Main_PrinterIdentityBlockHeader => ResourceManager.GetString(nameof(Main_PrinterIdentityBlockHeader), ResourceCulture)!;
    public static string Main_PrinterIdentityUnknownHeader => ResourceManager.GetString(nameof(Main_PrinterIdentityUnknownHeader), ResourceCulture)!;
    public static string Main_PrinterIdentityBlockButton => ResourceManager.GetString(nameof(Main_PrinterIdentityBlockButton), ResourceCulture)!;
    public static string Main_DeployCancelledByMismatchWarning => ResourceManager.GetString(nameof(Main_DeployCancelledByMismatchWarning), ResourceCulture)!;
    public static string NetworkTest_ConfirmDialogTitle => ResourceManager.GetString(nameof(NetworkTest_ConfirmDialogTitle), ResourceCulture)!;
    public static string NetworkTest_ConfirmHeader => ResourceManager.GetString(nameof(NetworkTest_ConfirmHeader), ResourceCulture)!;
    public static string NetworkTest_ConfirmHostFormat => ResourceManager.GetString(nameof(NetworkTest_ConfirmHostFormat), ResourceCulture)!;
    public static string NetworkTest_ConfirmBrandFormat => ResourceManager.GetString(nameof(NetworkTest_ConfirmBrandFormat), ResourceCulture)!;
    public static string NetworkTest_ConfirmPresetFormat => ResourceManager.GetString(nameof(NetworkTest_ConfirmPresetFormat), ResourceCulture)!;
    public static string NetworkTest_ConfirmProceedButton => ResourceManager.GetString(nameof(NetworkTest_ConfirmProceedButton), ResourceCulture)!;
    public static string NetworkTest_ConfirmCancelButton => ResourceManager.GetString(nameof(NetworkTest_ConfirmCancelButton), ResourceCulture)!;
    public static string Removal_ResetSpoolerConfirmTitle => ResourceManager.GetString(nameof(Removal_ResetSpoolerConfirmTitle), ResourceCulture)!;
    public static string Removal_ResetSpoolerConfirmMessage => ResourceManager.GetString(nameof(Removal_ResetSpoolerConfirmMessage), ResourceCulture)!;
    public static string Removal_ResetSpoolerRunning => ResourceManager.GetString(nameof(Removal_ResetSpoolerRunning), ResourceCulture)!;
    public static string Removal_ResetSpoolerSuccess => ResourceManager.GetString(nameof(Removal_ResetSpoolerSuccess), ResourceCulture)!;
    public static string Removal_ResetSpoolerErrorFormat => ResourceManager.GetString(nameof(Removal_ResetSpoolerErrorFormat), ResourceCulture)!;
    public static string Removal_Ping_Checking => ResourceManager.GetString(nameof(Removal_Ping_Checking), ResourceCulture)!;
    public static string Removal_Ping_Online => ResourceManager.GetString(nameof(Removal_Ping_Online), ResourceCulture)!;
    public static string Removal_Ping_Offline => ResourceManager.GetString(nameof(Removal_Ping_Offline), ResourceCulture)!;
    public static string Removal_Ping_TestingHostFormat => ResourceManager.GetString(nameof(Removal_Ping_TestingHostFormat), ResourceCulture)!;
    public static string Removal_Ping_ConnectedLoadingPrinters => ResourceManager.GetString(nameof(Removal_Ping_ConnectedLoadingPrinters), ResourceCulture)!;
    public static string Removal_Ping_ComputerOfflineFormat => ResourceManager.GetString(nameof(Removal_Ping_ComputerOfflineFormat), ResourceCulture)!;
    public static string Main_InversionDialogTitle => ResourceManager.GetString(nameof(Main_InversionDialogTitle), ResourceCulture)!;
    public static string Main_InversionDialogHeader => ResourceManager.GetString(nameof(Main_InversionDialogHeader), ResourceCulture)!;
    public static string Main_InversionDialogItemFormat => ResourceManager.GetString(nameof(Main_InversionDialogItemFormat), ResourceCulture)!;
    public static string Main_InversionDialogQuestion => ResourceManager.GetString(nameof(Main_InversionDialogQuestion), ResourceCulture)!;
    public static string Main_InversionDialogProceedButton => ResourceManager.GetString(nameof(Main_InversionDialogProceedButton), ResourceCulture)!;
    public static string Main_InversionDialogCancelButton => ResourceManager.GetString(nameof(Main_InversionDialogCancelButton), ResourceCulture)!;
    public static string Main_InversionCorrectedLogFormat => ResourceManager.GetString(nameof(Main_InversionCorrectedLogFormat), ResourceCulture)!;
    public static string Main_DeployCancelledByInversionWarning => ResourceManager.GetString(nameof(Main_DeployCancelledByInversionWarning), ResourceCulture)!;
    public static string Main_NoComputersDialogTitle => ResourceManager.GetString(nameof(Main_NoComputersDialogTitle), ResourceCulture)!;
    public static string Main_NoComputersDialogHeader => ResourceManager.GetString(nameof(Main_NoComputersDialogHeader), ResourceCulture)!;
    public static string Main_NoComputersDialogMessage => ResourceManager.GetString(nameof(Main_NoComputersDialogMessage), ResourceCulture)!;
    public static string Main_NoComputersDialogButton => ResourceManager.GetString(nameof(Main_NoComputersDialogButton), ResourceCulture)!;
    public static string Dialog_OkButton => ResourceManager.GetString(nameof(Dialog_OkButton), ResourceCulture)!;
}

