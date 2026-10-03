using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit command boundary for consumers that receive Session Camera commands
    /// through their own composition. The command authority remains Session-owned.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-039 Session Camera command boundary.")]
    public interface ISessionCameraAssignmentCommandPort
    {
        /// <summary>Activates an Assignment only when its targeted Outputs have no active normal Assignment.</summary>
        bool TryActivate(SessionCameraAssignmentAsset candidate, out string issue);

        /// <summary>Transactionally replaces the exact expected active Assignment.</summary>
        bool TryReplace(SessionCameraAssignmentAsset previousAssignment, SessionCameraAssignmentAsset candidate, out string issue);

        /// <summary>Clears the exact active Assignment and returns its Outputs to fallback.</summary>
        bool TryClear(SessionCameraAssignmentAsset assignment, out string issue);
    }
}
