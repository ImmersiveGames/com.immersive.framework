using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable diagnostics for the most recent Activity-owned Player Actor lifecycle operation.
    /// Physical Unity references remain inside the runtime participant.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.6 Activity-scoped Logical Player Actor lifecycle diagnostics.")]
    public sealed class ActivityPlayerActorLifecycleSnapshot
    {
        private readonly ActivityPlayerActorSlotLifecycleSnapshot[] slots;

        internal ActivityPlayerActorLifecycleSnapshot(
            ActivityPlayerActorLifecycleStatus status,
            string activityName,
            RuntimeContentOwner owner,
            PlayerParticipationRequirementLevel requirementLevel,
            int projectedSlotCount,
            int selectedCount,
            int preparedCount,
            int releasedCount,
            int failedCount,
            ActivityPlayerActorSlotLifecycleSnapshot[] slots,
            string message)
        {
            Status = status;
            ActivityName = activityName ?? string.Empty;
            Owner = owner;
            RequirementLevel = requirementLevel;
            ProjectedSlotCount = projectedSlotCount;
            SelectedCount = selectedCount;
            PreparedCount = preparedCount;
            ReleasedCount = releasedCount;
            FailedCount = failedCount;
            this.slots = slots != null
                ? (ActivityPlayerActorSlotLifecycleSnapshot[])slots.Clone()
                : Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>();
            Message = message ?? string.Empty;
        }

        public ActivityPlayerActorLifecycleStatus Status { get; }
        public string ActivityName { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerParticipationRequirementLevel RequirementLevel { get; }
        public int ProjectedSlotCount { get; }
        public int SelectedCount { get; }
        public int PreparedCount { get; }
        public int ReleasedCount { get; }
        public int FailedCount { get; }
        public IReadOnlyList<ActivityPlayerActorSlotLifecycleSnapshot> Slots => slots;
        public string Message { get; }
        public bool Succeeded => Status is
            ActivityPlayerActorLifecycleStatus.SucceededEntered or
            ActivityPlayerActorLifecycleStatus.SucceededEnteredNoParticipants or
            ActivityPlayerActorLifecycleStatus.SucceededExited or
            ActivityPlayerActorLifecycleStatus.SucceededExitedNoActors;
        public bool Failed => !Succeeded && Status != ActivityPlayerActorLifecycleStatus.None;
        public bool HasOwner => Owner.IsValid;

        internal static ActivityPlayerActorLifecycleSnapshot Empty(string message)
        {
            return new ActivityPlayerActorLifecycleSnapshot(
                ActivityPlayerActorLifecycleStatus.None,
                string.Empty,
                default,
                PlayerParticipationRequirementLevel.None,
                0,
                0,
                0,
                0,
                0,
                Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                message);
        }

        public string ToDiagnosticString()
        {
            return $"status='{Status}' activity='{ActivityName}' owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"requirement='{RequirementLevel}' projected='{ProjectedSlotCount}' selected='{SelectedCount}' " +
                $"prepared='{PreparedCount}' released='{ReleasedCount}' failed='{FailedCount}' message='{Message}'";
        }
    }
}
