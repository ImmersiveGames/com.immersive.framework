using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Result of resolving Pause Activity binding declarations from explicit Activity roots or declarations.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P2.1A Pause Activity binding intent resolution status.")]
    public enum PauseActivityBindingIntentStatus
    {
        Unknown = 0,
        IntentAbsent = 10,
        IntentCreated = 20,
        InvalidAuthoring = 30,
        UnsupportedMultipleDeclarations = 40
    }
}
