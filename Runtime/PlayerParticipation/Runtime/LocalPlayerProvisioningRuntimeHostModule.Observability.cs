using System;
using System.Collections.Generic;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Diagnostic-only projection logging for the Manager-Provisioned Player
    /// runtime. This file owns no Player state and never feeds lifecycle,
    /// readiness or reconciliation decisions.
    /// </summary>
    internal sealed partial class LocalPlayerProvisioningRuntimeHostModule
    {
        private bool _hasLifecycleObservabilityFingerprint;
        private int _lifecycleObservabilityFingerprint;
        private readonly Dictionary<string, int>
            _slotObservabilityFingerprints = new();

        /// <summary>
        /// The public provisioning observation contract is intentionally pull-only.
        /// Runtime observability therefore lives at the producer, not in the
        /// scene-facing PlayerProvisioningStatusBinding. LateUpdate observes only
        /// immutable projections and deduplicates every emitted record.
        /// </summary>
        private void LateUpdate()
        {
            if (!IsReady)
            {
                ResetLifecycleObservability();
                return;
            }

            PublishLifecycleObservabilityIfChanged();
        }

        private void PublishLifecycleObservabilityIfChanged()
        {
            bool available = this.TryGetLifecycleSnapshot(
                out ManagerProvisionedPlayerLifecycleSnapshot snapshot);
            snapshot ??= ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                "Manager-Provisioned Player lifecycle projection returned no snapshot.");

            int fingerprint = BuildLifecycleObservabilityFingerprint(snapshot);
            bool aggregateChanged =
                !_hasLifecycleObservabilityFingerprint ||
                fingerprint != _lifecycleObservabilityFingerprint;

            if (aggregateChanged)
            {
                _hasLifecycleObservabilityFingerprint = true;
                _lifecycleObservabilityFingerprint = fingerprint;
                LogLifecycleSnapshot(snapshot, available);
            }

            if (!available || !snapshot.IsAvailable)
            {
                if (_slotObservabilityFingerprints.Count > 0)
                {
                    _slotObservabilityFingerprints.Clear();
                }

                return;
            }

            LogChangedSlots(snapshot);
            RemoveStaleSlotFingerprints(snapshot);
        }

        private void LogLifecycleSnapshot(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            bool available)
        {
            var fields = LogFields.Of(
                LogFields.Field("status", snapshot.Status),
                LogFields.Field("available", available && snapshot.IsAvailable),
                LogFields.Field("activity", snapshot.ActivityName),
                LogFields.Field("activityOccurrence", snapshot.ActivityOccurrence),
                LogFields.Field("sessionRevision", snapshot.SessionRevision),
                LogFields.Field("requestedSessionRevision", snapshot.RequestedSessionRevision),
                LogFields.Field("appliedSessionRevision", snapshot.AppliedSessionRevision),
                LogFields.Field("entryPolicy", snapshot.EntryPolicy),
                LogFields.Field("readiness", snapshot.ReadinessStatus),
                LogFields.Field("readinessReason", snapshot.ReadinessReason),
                LogFields.Field("gateEvidenceScope", snapshot.GateEvidenceScope),
                LogFields.Field("hasGateEvidence", snapshot.HasGateEvidence),
                LogFields.Field("gateHeld", snapshot.GateHeld),
                LogFields.Field("joiningOpen", snapshot.JoiningOpen),
                LogFields.Field("hostCount", snapshot.HostCount),
                LogFields.Field("slotCount", snapshot.SlotCount),
                LogFields.Field("message", snapshot.Diagnostic));

            FrameworkLogger logger =
                FrameworkLogger.Create(typeof(LocalPlayerProvisioningRuntimeHostModule));

            if (snapshot.IsFailure)
            {
                logger.Warning(
                    "Manager-provisioned Player lifecycle changed to a failed state.",
                    fields);
                return;
            }

            logger.Debug(
                snapshot.IsAvailable
                    ? "Manager-provisioned Player lifecycle changed."
                    : "Manager-provisioned Player lifecycle projection is unavailable.",
                fields);
        }

        private void LogChangedSlots(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            FrameworkLogger logger =
                FrameworkLogger.Create(typeof(LocalPlayerProvisioningRuntimeHostModule));

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                ManagerProvisionedPlayerLifecycleSlotSnapshot slot =
                    snapshot.Slots[index];
                string key = string.IsNullOrWhiteSpace(slot.PlayerSlotId)
                    ? $"<slot:{index}>"
                    : slot.PlayerSlotId;
                int fingerprint = BuildSlotObservabilityFingerprint(slot);

                if (_slotObservabilityFingerprints.TryGetValue(
                        key,
                        out int previousFingerprint) &&
                    previousFingerprint == fingerprint)
                {
                    continue;
                }

                _slotObservabilityFingerprints[key] = fingerprint;
                logger.Debug(
                    "Manager-provisioned Player slot lifecycle changed.",
                    LogFields.Of(
                        LogFields.Field("slotIndex", index),
                        LogFields.Field("playerSlot", slot.PlayerSlotId),
                        LogFields.Field("allocation", slot.SlotState),
                        LogFields.Field("technicalHost", slot.HasTechnicalHost),
                        LogFields.Field("selectedActor", slot.SelectedActorProfile),
                        LogFields.Field("logicalActorPrepared", slot.LogicalActorPrepared),
                        LogFields.Field("physicallyMaterialized", slot.PhysicalActorMaterialized),
                        LogFields.Field("gameplayAdmitted", slot.GameplayAdmitted),
                        LogFields.Field("message", slot.Diagnostic)));
            }
        }

        private void RemoveStaleSlotFingerprints(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (_slotObservabilityFingerprints.Count <= snapshot.SlotCount)
            {
                return;
            }

            var stale = new List<string>();
            foreach (string key in _slotObservabilityFingerprints.Keys)
            {
                bool found = false;
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    string currentKey = string.IsNullOrWhiteSpace(
                            snapshot.Slots[index].PlayerSlotId)
                        ? $"<slot:{index}>"
                        : snapshot.Slots[index].PlayerSlotId;
                    if (!string.Equals(key, currentKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    stale.Add(key);
                }
            }

            if (stale.Count == 0)
            {
                return;
            }

            FrameworkLogger logger =
                FrameworkLogger.Create(typeof(LocalPlayerProvisioningRuntimeHostModule));
            for (int index = 0; index < stale.Count; index++)
            {
                string key = stale[index];
                _slotObservabilityFingerprints.Remove(key);
                logger.Debug(
                    "Manager-provisioned Player slot lifecycle projection released.",
                    LogFields.Of(
                        LogFields.Field("playerSlot", key),
                        LogFields.Field(
                            "message",
                            "The Slot is no longer present in the current Manager-Provisioned lifecycle projection.")));
            }
        }

        private void ResetLifecycleObservability()
        {
            _hasLifecycleObservabilityFingerprint = false;
            _lifecycleObservabilityFingerprint = 0;
            _slotObservabilityFingerprints.Clear();
        }

        private static int BuildLifecycleObservabilityFingerprint(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            var hash = new HashCode();
            hash.Add(snapshot.IsAvailable);
            hash.Add(snapshot.Status);
            hash.Add(snapshot.ActivityName, StringComparer.Ordinal);
            hash.Add(snapshot.ActivityOccurrence);
            hash.Add(snapshot.SessionRevision);
            hash.Add(snapshot.RequestedSessionRevision);
            hash.Add(snapshot.AppliedSessionRevision);
            hash.Add(snapshot.EntryPolicy, StringComparer.Ordinal);
            hash.Add(snapshot.ReadinessStatus, StringComparer.Ordinal);
            hash.Add(snapshot.ReadinessReason, StringComparer.Ordinal);
            hash.Add(snapshot.GateEvidenceScope);
            hash.Add(snapshot.HasGateEvidence);
            hash.Add(snapshot.GateHeld);
            hash.Add(snapshot.JoiningOpen);
            hash.Add(snapshot.HostCount);
            hash.Add(snapshot.SlotCount);
            hash.Add(snapshot.Diagnostic, StringComparer.Ordinal);
            return hash.ToHashCode();
        }

        private static int BuildSlotObservabilityFingerprint(
            ManagerProvisionedPlayerLifecycleSlotSnapshot slot)
        {
            var hash = new HashCode();
            hash.Add(slot.PlayerSlotId, StringComparer.Ordinal);
            hash.Add(slot.SlotState, StringComparer.Ordinal);
            hash.Add(slot.HasTechnicalHost);
            hash.Add(slot.SelectedActorProfile, StringComparer.Ordinal);
            hash.Add(slot.LogicalActorPrepared);
            hash.Add(slot.PhysicalActorMaterialized);
            hash.Add(slot.GameplayAdmitted);
            hash.Add(slot.Diagnostic, StringComparer.Ordinal);
            return hash.ToHashCode();
        }
    }
}
