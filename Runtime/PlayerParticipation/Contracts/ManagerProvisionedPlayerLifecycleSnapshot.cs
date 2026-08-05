using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable consumer-facing projection of Manager-Provisioned Player
    /// readiness and lifecycle evidence. It exposes diagnostics only; runtime
    /// authority remains in the scoped Session and Activity modules.
    /// </summary>
    public sealed class ManagerProvisionedPlayerLifecycleSnapshot
    {
        private static readonly IReadOnlyList<
            ManagerProvisionedPlayerLifecycleSlotSnapshot> NoSlots =
                new ReadOnlyCollection<
                    ManagerProvisionedPlayerLifecycleSlotSnapshot>(
                        Array.Empty<
                            ManagerProvisionedPlayerLifecycleSlotSnapshot>());

        private readonly IReadOnlyList<
            ManagerProvisionedPlayerLifecycleSlotSnapshot> slots;

        public ManagerProvisionedPlayerLifecycleSnapshot(
            bool isAvailable,
            ManagerProvisionedPlayerLifecycleStatus status,
            string activityName,
            int activityOccurrence,
            int sessionRevision,
            int requestedSessionRevision,
            int appliedSessionRevision,
            string entryPolicy,
            string readinessStatus,
            string readinessReason,
            bool hasGateEvidence,
            bool gateHeld,
            bool joiningOpen,
            int hostCount,
            IReadOnlyList<
                ManagerProvisionedPlayerLifecycleSlotSnapshot> slots,
            string diagnostic)
        {
            IsAvailable = isAvailable;
            Status = status;
            ActivityName = Normalize(activityName);
            ActivityOccurrence = Math.Max(0, activityOccurrence);
            SessionRevision = Math.Max(0, sessionRevision);
            RequestedSessionRevision = Math.Max(
                0,
                requestedSessionRevision);
            AppliedSessionRevision = Math.Max(
                0,
                appliedSessionRevision);
            EntryPolicy = Normalize(entryPolicy);
            ReadinessStatus = Normalize(readinessStatus);
            ReadinessReason = Normalize(readinessReason);
            HasGateEvidence = hasGateEvidence;
            GateHeld = hasGateEvidence && gateHeld;
            JoiningOpen = joiningOpen;
            HostCount = Math.Max(0, hostCount);
            this.slots = CopySlots(slots);
            Diagnostic = Normalize(diagnostic);
        }

        public bool IsAvailable { get; }

        public ManagerProvisionedPlayerLifecycleStatus Status { get; }

        public string ActivityName { get; }

        public int ActivityOccurrence { get; }

        public int SessionRevision { get; }

        public int RequestedSessionRevision { get; }

        public int AppliedSessionRevision { get; }

        public string EntryPolicy { get; }

        public string ReadinessStatus { get; }

        public string ReadinessReason { get; }

        /// <summary>
        /// True only when GateHeld was obtained from an explicit gate/readiness
        /// authority. A missing authority is not represented as a released gate.
        /// </summary>
        public bool HasGateEvidence { get; }

        public bool GateHeld { get; }

        public bool JoiningOpen { get; }

        public int HostCount { get; }

        public IReadOnlyList<
            ManagerProvisionedPlayerLifecycleSlotSnapshot> Slots => slots;

        public int SlotCount => slots.Count;

        public string Diagnostic { get; }

        public bool IsReady =>
            Status == ManagerProvisionedPlayerLifecycleStatus.Ready;

        public bool IsFailure =>
            Status == ManagerProvisionedPlayerLifecycleStatus.Failed;

        public bool IsReleased =>
            Status == ManagerProvisionedPlayerLifecycleStatus.Released;

        public static ManagerProvisionedPlayerLifecycleSnapshot Unavailable(
            string diagnostic)
        {
            return new ManagerProvisionedPlayerLifecycleSnapshot(
                false,
                ManagerProvisionedPlayerLifecycleStatus.Unavailable,
                string.Empty,
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                false,
                false,
                0,
                NoSlots,
                diagnostic);
        }

        public string ToDiagnosticString()
        {
            return
                $"available='{IsAvailable}' status='{Status}' " +
                $"activity='{ActivityName}' occurrence='{ActivityOccurrence}' " +
                $"sessionRevision='{SessionRevision}' " +
                $"requestedRevision='{RequestedSessionRevision}' " +
                $"appliedRevision='{AppliedSessionRevision}' " +
                $"entryPolicy='{EntryPolicy}' " +
                $"readiness='{ReadinessStatus}' " +
                $"readinessReason='{ReadinessReason}' " +
                $"hasGateEvidence='{HasGateEvidence}' " +
                $"gateHeld='{GateHeld}' joiningOpen='{JoiningOpen}' " +
                $"hostCount='{HostCount}' slotCount='{SlotCount}' " +
                $"diagnostic='{Diagnostic}'.";
        }

        private static IReadOnlyList<
            ManagerProvisionedPlayerLifecycleSlotSnapshot> CopySlots(
                IReadOnlyList<
                    ManagerProvisionedPlayerLifecycleSlotSnapshot> source)
        {
            if (source == null || source.Count == 0)
            {
                return NoSlots;
            }

            var copy =
                new ManagerProvisionedPlayerLifecycleSlotSnapshot[
                    source.Count];

            for (int index = 0; index < source.Count; index++)
            {
                ManagerProvisionedPlayerLifecycleSlotSnapshot item =
                    source[index];

                if (item == null)
                {
                    throw new ArgumentException(
                        "Lifecycle slot snapshots cannot contain null entries.",
                        nameof(source));
                }

                copy[index] = item;
            }

            return new ReadOnlyCollection<
                ManagerProvisionedPlayerLifecycleSlotSnapshot>(copy);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
