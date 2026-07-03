using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.ObjectEntry;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    internal static class ObjectResetGroupExecutor
    {
        internal static async Awaitable<ObjectResetGroupResult> ExecuteAsync(
            FrameworkRuntimeHost runtimeHost,
            ObjectResetSelectionMode selectionMode,
            IReadOnlyList<ObjectResetGroupEntry> explicitEntries,
            string groupId,
            string source,
            string reason,
            bool allowNoParticipants,
            bool stopOnFailure)
        {
            string resolvedGroupId = groupId.NormalizeTextOrFallback("object-reset-group");
            string resolvedSource = source.NormalizeTextOrFallback(nameof(ObjectResetGroupExecutor));
            string resolvedReason = reason.NormalizeTextOrFallback("Object Reset Group");

            if (runtimeHost == null)
            {
                return ObjectResetGroupResult.Rejected(
                    resolvedGroupId,
                    resolvedSource,
                    resolvedReason,
                    ObjectResetGroupResultStatus.RejectedRuntimeUnavailable,
                    "Object Reset Group failed. Application Runtime is unavailable.");
            }

            if (selectionMode == ObjectResetSelectionMode.Unknown)
            {
                return ObjectResetGroupResult.Rejected(
                    resolvedGroupId,
                    resolvedSource,
                    resolvedReason,
                    ObjectResetGroupResultStatus.RejectedInvalidRequest,
                    "Object Reset Group failed. Reset selection mode is invalid.");
            }

            if (!runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var snapshot) || snapshot == null || !snapshot.IsAvailable)
            {
                return ObjectResetGroupResult.Rejected(
                    resolvedGroupId,
                    resolvedSource,
                    resolvedReason,
                    ObjectResetGroupResultStatus.RejectedRuntimeContextUnavailable,
                    "Object Reset Group failed. Current Object Entry runtime context snapshot is unavailable.");
            }

            var targetResults = new List<ObjectResetGroupTargetResult>();
            bool stoppedOnFailure = false;

            if (selectionMode == ObjectResetSelectionMode.ExplicitTargets)
            {
                IReadOnlyList<ObjectResetGroupEntry> entries = explicitEntries ?? Array.Empty<ObjectResetGroupEntry>();
                if (entries.Count == 0)
                {
                    return ObjectResetGroupResult.Rejected(
                        resolvedGroupId,
                        resolvedSource,
                        resolvedReason,
                        ObjectResetGroupResultStatus.RejectedInvalidRequest,
                        "Object Reset Group failed. No explicit targets are configured.");
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    ObjectResetGroupEntry entry = entries[i];
                    if (entry == null)
                    {
                        targetResults.Add(ObjectResetGroupTargetResult.SkippedTarget(i, string.Empty, "Object Reset Group entry is null."));
                        continue;
                    }

                    string targetIdText = entry.ResolveObjectEntryIdText();
                    if (!entry.Enabled)
                    {
                        targetResults.Add(ObjectResetGroupTargetResult.SkippedTarget(i, targetIdText, "Object Reset Group entry is disabled."));
                        continue;
                    }

                    if (!TryCreateRequestFromEntry(
                            snapshot,
                            entry,
                            i,
                            allowNoParticipants,
                            resolvedSource,
                            resolvedReason,
                            out ObjectResetRequest request,
                            out string failedTargetId,
                            out string failureMessage))
                    {
                        targetResults.Add(ObjectResetGroupTargetResult.FailedTarget(i, failedTargetId, failureMessage));
                        if (stopOnFailure)
                        {
                            stoppedOnFailure = true;
                            break;
                        }

                        continue;
                    }

                    ObjectResetResult resetResult = await runtimeHost.RequestObjectResetAsync(request);
                    targetResults.Add(ObjectResetGroupTargetResult.FromResetResult(i, request.Target.ObjectEntryId.StableText, resetResult));
                    if (resetResult.Failed && stopOnFailure)
                    {
                        stoppedOnFailure = true;
                        break;
                    }
                }
            }
            else
            {
                var descriptors = SelectDescriptors(snapshot, selectionMode);
                for (int i = 0; i < descriptors.Count; i++)
                {
                    ObjectEntryDescriptor descriptor = descriptors[i];
                    if (!TryCreateRequestFromDescriptor(
                            descriptor,
                            i,
                            allowNoParticipants,
                            resolvedSource,
                            resolvedReason,
                            out ObjectResetRequest request,
                            out string failedTargetId,
                            out string failureMessage))
                    {
                        targetResults.Add(ObjectResetGroupTargetResult.FailedTarget(i, failedTargetId, failureMessage));
                        if (stopOnFailure)
                        {
                            stoppedOnFailure = true;
                            break;
                        }

                        continue;
                    }

                    ObjectResetResult resetResult = await runtimeHost.RequestObjectResetAsync(request);
                    targetResults.Add(ObjectResetGroupTargetResult.FromResetResult(i, request.Target.ObjectEntryId.StableText, resetResult));
                    if (resetResult.Failed && stopOnFailure)
                    {
                        stoppedOnFailure = true;
                        break;
                    }
                }
            }

            return ObjectResetGroupResult.FromTargets(
                resolvedGroupId,
                resolvedSource,
                resolvedReason,
                targetResults,
                stoppedOnFailure);
        }

        private static IReadOnlyList<ObjectEntryDescriptor> SelectDescriptors(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetSelectionMode selectionMode)
        {
            if (snapshot == null || !snapshot.IsAvailable)
            {
                return Array.Empty<ObjectEntryDescriptor>();
            }

            var descriptors = new List<ObjectEntryDescriptor>();
            IReadOnlyList<ObjectEntryDescriptor> entries = snapshot.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ObjectEntryDescriptor descriptor = entries[i];
                if (MatchesSelection(descriptor, selectionMode))
                {
                    descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }

        private static bool MatchesSelection(ObjectEntryDescriptor descriptor, ObjectResetSelectionMode selectionMode)
        {
            switch (selectionMode)
            {
                case ObjectResetSelectionMode.CurrentActivityEntries:
                    return descriptor.Scope == ObjectEntryScope.Activity;
                case ObjectResetSelectionMode.CurrentRouteEntries:
                    return descriptor.Scope == ObjectEntryScope.Route;
                case ObjectResetSelectionMode.CurrentRouteAndActivityEntries:
                    return descriptor.Scope == ObjectEntryScope.Route || descriptor.Scope == ObjectEntryScope.Activity;
                case ObjectResetSelectionMode.AllCurrentEntries:
                    return descriptor.Scope == ObjectEntryScope.Session
                        || descriptor.Scope == ObjectEntryScope.Route
                        || descriptor.Scope == ObjectEntryScope.Activity;
                default:
                    return false;
            }
        }

        private static bool TryCreateRequestFromEntry(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetGroupEntry entry,
            int index,
            bool allowNoParticipants,
            string source,
            string reason,
            out ObjectResetRequest request,
            out string failedTargetId,
            out string failureMessage)
        {
            request = default;
            failedTargetId = entry == null ? string.Empty : entry.ResolveObjectEntryIdText();
            failureMessage = string.Empty;

            if (entry == null)
            {
                failureMessage = "Object Reset Group target failed. Entry is null.";
                return false;
            }

            string idText = entry.ResolveObjectEntryIdText();
            failedTargetId = idText;
            if (string.IsNullOrWhiteSpace(idText))
            {
                failureMessage = $"Object Reset Group target failed. Object Entry Id is missing. index='{index}'.";
                return false;
            }

            ObjectEntryId id;
            try
            {
                id = ObjectEntryId.From(idText);
            }
            catch (ArgumentException exception)
            {
                failureMessage = $"Object Reset Group target failed. Object Entry Id is invalid. index='{index}' objectEntry='{idText}'. {exception.Message}";
                return false;
            }

            if (!snapshot.TryGet(id, out var descriptor))
            {
                failureMessage = $"Object Reset Group target failed. Object Entry target was not found in the current snapshot. index='{index}' objectEntry='{id.StableText}'.";
                return false;
            }

            return TryCreateRequestFromDescriptor(
                descriptor,
                index,
                allowNoParticipants,
                source,
                entry.ResolveReason(reason),
                out request,
                out failedTargetId,
                out failureMessage);
        }

        private static bool TryCreateRequestFromDescriptor(
            ObjectEntryDescriptor descriptor,
            int index,
            bool allowNoParticipants,
            string source,
            string reason,
            out ObjectResetRequest request,
            out string failedTargetId,
            out string failureMessage)
        {
            request = default;
            failedTargetId = descriptor.Id.IsValid ? descriptor.Id.StableText : string.Empty;
            failureMessage = string.Empty;

            try
            {
                request = new ObjectResetRequest(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    new ObjectResetPolicy(requireCurrentSnapshot: true, allowNoParticipants: allowNoParticipants),
                    source,
                    reason);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                failureMessage = $"Object Reset Group target failed. Request could not be created. index='{index}' objectEntry='{failedTargetId}'. {exception.Message}";
                return false;
            }

            return true;
        }
    }
}
