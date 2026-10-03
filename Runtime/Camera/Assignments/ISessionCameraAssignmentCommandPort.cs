using Immersive.Framework.CameraAuthoring;

namespace Immersive.Framework.Camera
{
    internal interface ISessionCameraAssignmentCommandPort
    {
        bool TryActivate(SessionCameraAssignmentAsset candidate, out string issue);
        bool TryReplace(SessionCameraAssignmentAsset previousAssignment, SessionCameraAssignmentAsset candidate, out string issue);
        bool TryClear(SessionCameraAssignmentAsset assignment, out string issue);
    }
}
