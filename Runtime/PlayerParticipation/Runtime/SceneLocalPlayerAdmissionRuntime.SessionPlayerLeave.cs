using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class SceneLocalPlayerAdmissionRuntime
    {
        /// <summary>
        /// Resolves the exact Scene-Provided contextual admission record currently associated
        /// with one Slot. This is read-only correlation evidence for ADR-020 Session Player Leave.
        /// </summary>
        internal bool TryGetSessionPlayerLeaveRepresentation(
            PlayerSlotId playerSlotId,
            out SceneProvidedLocalPlayerAuthoring authoring,
            out LocalPlayerHostAuthoring host,
            out SceneLocalPlayerAdmissionToken sceneAdmissionToken,
            out PlayerSlotAssignmentSnapshot assignment)
        {
            authoring = null;
            host = null;
            sceneAdmissionToken = default;
            assignment = default;

            if (!playerSlotId.IsValid ||
                !_recordsBySlot.TryGetValue(playerSlotId, out AdmissionRecord record) ||
                record == null)
            {
                return false;
            }

            authoring = record.Authoring;
            host = record.Host;
            sceneAdmissionToken = record.Token;
            assignment = record.Assignment;
            return sceneAdmissionToken.IsValid &&
                sceneAdmissionToken.PlayerSlotId == playerSlotId;
        }

        /// <summary>
        /// Retires only the Scene admission runtime record for an already staged Session Leave.
        /// Host evidence, Host admission and canonical assignment must be released by the
        /// orchestrating ADR-020 Scene-Provided Leave path before this terminal contextual-record
        /// cleanup. This method never changes Slot membership or Actor selection.
        /// </summary>
        internal bool TryReleaseSessionPlayerLeaveRepresentationRecord(
            SessionPlayerLeaveToken leaveToken,
            SceneLocalPlayerAdmissionToken expectedSceneAdmissionToken,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;

            if (!leaveToken.IsValid ||
                !expectedSceneAdmissionToken.IsValid ||
                expectedSceneAdmissionToken.PlayerSlotId != leaveToken.PlayerSlotId)
            {
                issue =
                    "Scene-Provided Session Leave contextual-record release requires matching valid Leave and Scene admission tokens.";
                return false;
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                _participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    source,
                    reason);
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                issue = leaveConfirmation != null
                    ? "Scene contextual-record release rejected stale Leave correlation. " +
                      leaveConfirmation.ToDiagnosticString()
                    : "Scene contextual-record release received no Leave confirmation result.";
                return false;
            }

            if (!_recordsBySlot.TryGetValue(
                    leaveToken.PlayerSlotId,
                    out AdmissionRecord record) ||
                record == null)
            {
                issue =
                    "Scene contextual-record release expected an active record for the Leaving occurrence, but none remains.";
                return false;
            }

            if (record.Token != expectedSceneAdmissionToken)
            {
                issue =
                    "Scene contextual-record release rejected a foreign or stale Scene admission token.";
                return false;
            }

            if (_participationContext.TryGetCurrentAssignment(
                    leaveToken.PlayerSlotId,
                    out PlayerSlotAssignmentSnapshot currentAssignment) &&
                currentAssignment.IsAssigned)
            {
                issue =
                    "Scene contextual-record release requires the canonical current assignment to be released first.";
                return false;
            }

            _records.Remove(record);
            _recordsBySlot.Remove(leaveToken.PlayerSlotId);
            return true;
        }
    }
}
