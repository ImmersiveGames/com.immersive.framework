using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable evidence for the current assignment state of one configured Session Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 canonical current Player Slot assignment snapshot.")]
    public readonly struct PlayerSlotAssignmentSnapshot
    {
        internal PlayerSlotAssignmentSnapshot(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            int configuredIndex,
            PlayerSlotAssignmentState state,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            RuntimeContentOwner assignmentOwner,
            int assignmentSequence,
            int assignmentRevision,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            string source,
            string reason)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            PlayerSlotId = playerSlotId;
            ConfiguredIndex = configuredIndex;
            State = state;
            AssignmentOrigin = assignmentOrigin;
            AssignmentOwner = assignmentOwner;
            AssignmentSequence = assignmentSequence;
            AssignmentRevision = assignmentRevision;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public int ConfiguredIndex { get; }
        public PlayerSlotAssignmentState State { get; }
        public PlayerSlotAssignmentOrigin AssignmentOrigin { get; }
        public RuntimeContentOwner AssignmentOwner { get; }
        public int AssignmentSequence { get; }
        public int AssignmentRevision { get; }
        public PlayerSlotAssignmentToken AssignmentToken { get; }
        public PlayerHostBindingIdentity HostBindingIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            ConfiguredIndex >= 0;

        public bool IsAssigned =>
            IsValid &&
            State == PlayerSlotAssignmentState.Assigned &&
            (AssignmentOrigin is
                PlayerSlotAssignmentOrigin.ManagerProvisioned or
                PlayerSlotAssignmentOrigin.SceneProvided) &&
            AssignmentOwner.IsValid &&
            AssignmentSequence > 0 &&
            AssignmentRevision > 0 &&
            AssignmentToken.IsValid &&
            HostBindingIdentity.IsValid;
    }
}
