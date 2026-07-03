using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Issue kind emitted by the explicit Unity PlayerInput adapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32D Unity PlayerInput adapter application issue kind.")]
    public enum InputModeUnityPlayerInputAdapterIssueKind
    {
        None = 0,
        InvalidPlan = 10,
        MissingPlayerInput = 20,
        MissingActionAsset = 30,
        MissingActionMap = 40,
        UnsupportedOperation = 50,
        UnityInputException = 60
    }
}
