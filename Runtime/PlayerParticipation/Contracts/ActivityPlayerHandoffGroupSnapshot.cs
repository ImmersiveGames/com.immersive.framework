using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E immutable multi-Slot Activity Player handoff group evidence.")]
    public sealed class ActivityPlayerHandoffGroupSnapshot
    {
        private readonly ActivityPlayerHandoffGroupSlotSnapshot[] slots;
        internal ActivityPlayerHandoffGroupSnapshot(
            ActivityPlayerHandoffGroupToken token,
            ActivityPlayerHandoffGroupState state,
            ActivityPlayerAdmissionFlowDecision admissionDecision,
            ActivityPlayerHandoffGroupSlotSnapshot[] slots,
            string activityName,
            string source,
            string reason,
            string message)
        {
            Token = token;
            State = state;
            AdmissionDecision = admissionDecision;
            this.slots = slots != null
                ? (ActivityPlayerHandoffGroupSlotSnapshot[])slots.Clone()
                : Array.Empty<ActivityPlayerHandoffGroupSlotSnapshot>();
            ActivityName = activityName.NormalizeText();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ActivityPlayerHandoffGroupToken Token { get; }
        public ActivityPlayerHandoffGroupState State { get; }
        public ActivityPlayerAdmissionFlowDecision AdmissionDecision { get; }
        public IReadOnlyList<ActivityPlayerHandoffGroupSlotSnapshot> Slots => slots;
        public string ActivityName { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public int SlotCount => slots.Length;
        public bool IsReadyToCommit => State == ActivityPlayerHandoffGroupState.ReadyToCommit;
        public bool IsCommitted => State == ActivityPlayerHandoffGroupState.Committed;
        public bool IsRollbackFailed => State == ActivityPlayerHandoffGroupState.RollbackFailed;
        public bool IsCommitCleanupFailed => State == ActivityPlayerHandoffGroupState.CommitCleanupFailed;
        public string ToDiagnosticString() =>
            $"group='{Token.StableText}' state='{State}' activity='{ActivityName}' slots='{SlotCount}' " +
            $"admission='{(AdmissionDecision != null ? AdmissionDecision.Disposition.ToString() : string.Empty)}' " +
            $"source='{Source}' reason='{Reason}' message='{Message}'";

        internal static ActivityPlayerHandoffGroupSnapshot Empty(
            string source, string reason, string message) =>
            new ActivityPlayerHandoffGroupSnapshot(default,
                ActivityPlayerHandoffGroupState.None, null,
                Array.Empty<ActivityPlayerHandoffGroupSlotSnapshot>(), string.Empty,
                source, reason, message);
    }
}
