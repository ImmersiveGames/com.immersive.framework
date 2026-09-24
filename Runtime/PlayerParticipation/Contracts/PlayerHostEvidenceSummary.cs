using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical view of retained Host evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-3 public non-physical Host evidence summary.")]
    public readonly struct PlayerHostEvidenceSummary
    {
        internal PlayerHostEvidenceSummary(
            PlayerSlotId playerSlotId,
            PlayerHostProvisioningMode physicalProvisioningMode,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            bool isConfirmed,
            string source,
            string reason,
            string message)
        {
            PlayerSlotId = playerSlotId;
            PhysicalProvisioningMode = physicalProvisioningMode;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
            IsConfirmed = isConfirmed;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerHostProvisioningMode PhysicalProvisioningMode { get; }
        public PlayerSlotAssignmentToken AssignmentToken { get; }
        public PlayerHostBindingIdentity HostBindingIdentity { get; }
        public bool IsConfirmed { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool IsRecorded =>
            PlayerSlotId.IsValid &&
            PhysicalProvisioningMode is
                PlayerHostProvisioningMode.ManagerProvisioned or
                PlayerHostProvisioningMode.SceneProvided;

        public bool HasContextualProjection =>
            AssignmentToken.IsValid &&
            AssignmentToken.PlayerSlotId == PlayerSlotId &&
            HostBindingIdentity.IsValid &&
            AssignmentToken.HostBindingIdentity == HostBindingIdentity;
    }
}
