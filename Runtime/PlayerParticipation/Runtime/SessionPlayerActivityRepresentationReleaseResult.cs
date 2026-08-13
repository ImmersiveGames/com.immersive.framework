using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal evidence for retiring one Session Player's current Activity representation.
    /// Successful completion only means the contextual Activity layer is retired; provisioning
    /// resources, Session Actor selection and terminal Slot vacancy remain downstream stages.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Session Player Activity representation release result and diagnostics.")]
    internal sealed class SessionPlayerActivityRepresentationReleaseResult
    {
        internal SessionPlayerActivityRepresentationReleaseResult(
            SessionPlayerActivityRepresentationReleaseStatus status,
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerLeaveRuntimeResult leaveConfirmation,
            string activityName,
            RuntimeContentOwner activityOwner,
            PlayerActorPreparationToken preparationToken,
            PlayerActorPreparationResult actorRelease,
            bool hadActivityRepresentation,
            bool hadPreparedActor,
            bool gameplayAdmissionReleased,
            bool cameraReleased,
            bool inputReleased,
            bool occupancyReleased,
            bool preparedActorReleased,
            bool actorRetainedCleanupPending,
            bool activityLedgerRetired,
            bool readinessContributionRetired,
            string source,
            string reason,
            string message)
        {
            Status = status;
            LeaveToken = leaveToken;
            LeaveConfirmation = leaveConfirmation;
            ActivityName = activityName ?? string.Empty;
            ActivityOwner = activityOwner;
            PreparationToken = preparationToken;
            ActorRelease = actorRelease;
            HadActivityRepresentation = hadActivityRepresentation;
            HadPreparedActor = hadPreparedActor;
            GameplayAdmissionReleased = gameplayAdmissionReleased;
            CameraReleased = cameraReleased;
            InputReleased = inputReleased;
            OccupancyReleased = occupancyReleased;
            PreparedActorReleased = preparedActorReleased;
            ActorRetainedCleanupPending = actorRetainedCleanupPending;
            ActivityLedgerRetired = activityLedgerRetired;
            ReadinessContributionRetired = readinessContributionRetired;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal SessionPlayerActivityRepresentationReleaseStatus Status { get; }
        internal SessionPlayerLeaveToken LeaveToken { get; }
        internal SessionPlayerLeaveRuntimeResult LeaveConfirmation { get; }
        internal string ActivityName { get; }
        internal RuntimeContentOwner ActivityOwner { get; }
        internal PlayerActorPreparationToken PreparationToken { get; }
        internal PlayerActorPreparationResult ActorRelease { get; }
        internal bool HadActivityRepresentation { get; }
        internal bool HadPreparedActor { get; }
        internal bool GameplayAdmissionReleased { get; }
        internal bool CameraReleased { get; }
        internal bool InputReleased { get; }
        internal bool OccupancyReleased { get; }
        internal bool PreparedActorReleased { get; }
        internal bool ActorRetainedCleanupPending { get; }
        internal bool ActivityLedgerRetired { get; }
        internal bool ReadinessContributionRetired { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }

        internal bool Succeeded => Status is
            SessionPlayerActivityRepresentationReleaseStatus.SucceededReleased or
            SessionPlayerActivityRepresentationReleaseStatus.SucceededAlreadyReleased or
            SessionPlayerActivityRepresentationReleaseStatus.SucceededNoCurrentRepresentation;

        internal bool StateChanged =>
            Status == SessionPlayerActivityRepresentationReleaseStatus.SucceededReleased;

        internal string ToDiagnosticString()
        {
            return
                $"status='{Status}' leaveToken='{LeaveToken.StableText}' " +
                $"activity='{ActivityName}' owner='{(ActivityOwner.IsValid ? ActivityOwner.StableText : string.Empty)}' " +
                $"preparation='{PreparationToken.StableText}' hadRepresentation='{HadActivityRepresentation}' " +
                $"hadPreparedActor='{HadPreparedActor}' admissionReleased='{GameplayAdmissionReleased}' " +
                $"cameraReleased='{CameraReleased}' inputReleased='{InputReleased}' occupancyReleased='{OccupancyReleased}' " +
                $"preparedActorReleased='{PreparedActorReleased}' retainedActorCleanupPending='{ActorRetainedCleanupPending}' " +
                $"activityLedgerRetired='{ActivityLedgerRetired}' " +
                $"readinessContributionRetired='{ReadinessContributionRetired}' " +
                $"actorRelease='{(ActorRelease != null ? ActorRelease.ToDiagnosticString() : string.Empty)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        internal static SessionPlayerActivityRepresentationReleaseResult RuntimeUnavailable(
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason,
            string message)
        {
            return new SessionPlayerActivityRepresentationReleaseResult(
                SessionPlayerActivityRepresentationReleaseStatus.RejectedRuntimeUnavailable,
                leaveToken,
                null,
                string.Empty,
                default,
                default,
                null,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                source,
                reason,
                message);
        }
    }
}
