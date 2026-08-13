using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal evidence for releasing Framework authority from one Scene-Provided Player while
    /// preserving the externally scene-owned Host, PlayerInput and Logical Actor objects.
    /// This result never means that logical Session membership is Available; terminal membership
    /// commit remains owned by the Session Player Leave authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Scene-Provided Session Player authority-release result and diagnostics.")]
    internal sealed class SceneProvidedSessionPlayerLeaveReleaseResult
    {
        internal SceneProvidedSessionPlayerLeaveReleaseResult(
            SceneProvidedSessionPlayerLeaveReleaseStatus status,
            SessionPlayerLeaveToken leaveToken,
            SceneLocalPlayerAdmissionToken sceneAdmissionToken,
            SessionPlayerLeaveRuntimeResult leaveConfirmation,
            PlayerHostEvidenceResult hostEvidenceRelease,
            PlayerSlotAssignmentResult assignmentResult,
            SceneLocalPlayerAdmissionAuthoring authoring,
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerActorDeclaration sceneLogicalPlayerActor,
            bool hostEvidenceReleased,
            bool hostAdmissionReleased,
            bool assignmentReleased,
            bool contextualRecordReleased,
            string source,
            string reason,
            string message)
        {
            Status = status;
            LeaveToken = leaveToken;
            SceneAdmissionToken = sceneAdmissionToken;
            LeaveConfirmation = leaveConfirmation;
            HostEvidenceRelease = hostEvidenceRelease;
            AssignmentResult = assignmentResult;
            Authoring = authoring;
            LocalPlayerHost = localPlayerHost;
            SceneLogicalPlayerActor = sceneLogicalPlayerActor;
            HostEvidenceReleased = hostEvidenceReleased;
            HostAdmissionReleased = hostAdmissionReleased;
            AssignmentReleased = assignmentReleased;
            ContextualRecordReleased = contextualRecordReleased;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal SceneProvidedSessionPlayerLeaveReleaseStatus Status { get; }
        internal SessionPlayerLeaveToken LeaveToken { get; }
        internal SceneLocalPlayerAdmissionToken SceneAdmissionToken { get; }
        internal SessionPlayerLeaveRuntimeResult LeaveConfirmation { get; }
        internal PlayerHostEvidenceResult HostEvidenceRelease { get; }
        internal PlayerSlotAssignmentResult AssignmentResult { get; }
        internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
        internal LocalPlayerHostAuthoring LocalPlayerHost { get; }
        internal PlayerActorDeclaration SceneLogicalPlayerActor { get; }
        internal bool HostEvidenceReleased { get; }
        internal bool HostAdmissionReleased { get; }
        internal bool AssignmentReleased { get; }
        internal bool ContextualRecordReleased { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }

        internal bool Succeeded => Status is
            SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededReleased or
            SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededAlreadyReleased or
            SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededNoCurrentRepresentation;

        internal bool StateChanged =>
            Status == SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededReleased;

        internal string ToDiagnosticString()
        {
            return $"status='{Status}' leaveToken='{LeaveToken.StableText}' " +
                $"sceneAdmission='{SceneAdmissionToken.StableText}' " +
                $"authoring='{UnityObjectText(Authoring)}' host='{UnityObjectText(LocalPlayerHost)}' " +
                $"sceneActor='{UnityObjectText(SceneLogicalPlayerActor)}' " +
                $"hostEvidenceReleased='{HostEvidenceReleased}' hostAdmissionReleased='{HostAdmissionReleased}' " +
                $"assignmentReleased='{AssignmentReleased}' contextualRecordReleased='{ContextualRecordReleased}' " +
                $"assignment='{AssignmentText(AssignmentResult)}' hostEvidence='{HostEvidenceText(HostEvidenceRelease)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        private static string UnityObjectText(Object value)
        {
            if (object.ReferenceEquals(value, null))
            {
                return "<clr-null>";
            }

            return value == null ? "<destroyed-or-unloaded>" : value.name;
        }

        private static string AssignmentText(PlayerSlotAssignmentResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : string.Empty;
        }

        private static string HostEvidenceText(PlayerHostEvidenceResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : string.Empty;
        }
    }
}
