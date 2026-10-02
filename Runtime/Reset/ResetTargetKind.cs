using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>API status: Experimental. Supported semantic Reset request target kinds.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 RESET-035-E/F semantic target kinds.")]
    public enum ResetTargetKind
    {
        Unknown = 0,
        Object = 10,
        Composition = 20,
        CurrentActivity = 30,
        CurrentRoute = 40
    }
}
