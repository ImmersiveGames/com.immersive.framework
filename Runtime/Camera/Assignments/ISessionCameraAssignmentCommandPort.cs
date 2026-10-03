using Immersive.Framework.CameraAuthoring;

namespace Immersive.Framework.Camera
{
    internal interface ISessionCameraAssignmentCommandPort
    {
        bool TryActivate(SessionCameraAssignmentAuthoring candidate, out string issue);
        bool TryReplace(SessionCameraAssignmentId previousAssignmentId, SessionCameraAssignmentAuthoring candidate, out string issue);
        bool TryClear(SessionCameraAssignmentId assignmentId, out string issue);
    }
}
