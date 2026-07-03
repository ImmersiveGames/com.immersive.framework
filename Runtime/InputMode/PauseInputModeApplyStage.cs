using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Failed stage vocabulary for the Pause/InputMode apply boundary.
    /// This is boundary-local evidence and not a universal status enum.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F38 Pause/InputMode apply boundary stage evidence.")]
    public enum PauseInputModeApplyStage
    {
        Unknown = 0,
        None = 10,
        MissingRuntimeHost = 100,
        MissingPauseRuntime = 110,
        MissingPlayerInput = 120,
        MissingActionMap = 130,
        InvalidRequest = 140,
        PreflightRejected = 150,
        PauseRequestFailed = 160,
        AdapterApplyFailed = 170,
        DiagnosticsFailed = 180
    }
}
