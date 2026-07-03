using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Issue kinds emitted by the F32E explicit PlayerInput application wrapper.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32E Unity PlayerInput application issue kind.")]
    public enum InputModeUnityPlayerInputApplicationIssueKind
    {
        None = 0,
        InvalidPlan = 10,
        MissingPlayerInput = 20,
        MissingActionAsset = 30,
        MissingActionMap = 40,
        PlayerInputActivationException = 50,
        PlayerInputAdapterFailed = 60,
        UnsupportedOperation = 70
    }
}
