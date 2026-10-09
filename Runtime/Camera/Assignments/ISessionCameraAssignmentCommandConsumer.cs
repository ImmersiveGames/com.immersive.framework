using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Implemented by a scene component that wants gameplay code to receive the
    /// Session Camera command port for that component's composition lifetime.
    /// Implementations must accept the same port idempotently, reject another
    /// Session authority, and detach only the exact port supplied to this method.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-039 scene-local Session Camera command injection contract.")]
    public interface ISessionCameraAssignmentCommandConsumer
    {
        bool IsBoundToSessionCameraAssignmentCommands(
            ISessionCameraAssignmentCommandPort commands);

        bool TryBindSessionCameraAssignmentCommands(
            ISessionCameraAssignmentCommandPort commands,
            out string issue);

        bool TryReleaseSessionCameraAssignmentCommands(
            ISessionCameraAssignmentCommandPort expectedCommands,
            out string issue);
    }
}
