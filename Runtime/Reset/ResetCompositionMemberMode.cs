using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>API status: Experimental. Initial member collection modes for ResetComposition.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 RESET-035-D member collection modes.")]
    public enum ResetCompositionMemberMode
    {
        Descendants = 0,
        ExplicitMembers = 1
    }
}
