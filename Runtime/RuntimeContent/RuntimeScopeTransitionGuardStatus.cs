using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Internal. Result vocabulary for the runtime scope transition guard.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F8H internal runtime scope transition guard status; diagnostics only.")]
    internal enum RuntimeScopeTransitionGuardStatus
    {
        Unknown = 0,
        ScopeOpened = 10,
        ScopeAlreadyActive = 20,
        CancellationRequested = 30,
        CancellationAlreadyRequested = 40,
        ScopeRemoved = 50,
        MaterializationAllowed = 60,
        RejectedMissingScope = 100,
        RejectedScopeCancelling = 110,
        RejectedScopeRemoved = 120,
        RejectedStaleToken = 130,
        RejectedMismatchedOwner = 140
    }
}
