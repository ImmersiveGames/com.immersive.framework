using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Dry-run operation a Unity Input adapter would perform later.
    /// This enum is intent only; it never invokes Unity Input behavior by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32C InputMode Unity application dry-run operation.")]
    public enum InputModeUnityApplicationPlanOperation
    {
        Unknown = 0,
        SelectActionMap = 10,
        LockInput = 20,
        NoOperation = 30
    }
}
