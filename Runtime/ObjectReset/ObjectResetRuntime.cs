using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Deterministic executor for the Object Reset foundation.
    /// It resolves logical targets, builds ordered participant plans and aggregates results; it does not discover Unity objects or perform any reset itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14H Object Reset plan builder/executor used by Runtime Host; no Unity baseline adapter.")]
    public sealed class ObjectResetRuntime
    {
        public ObjectResetPlan BuildPlan(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetRequest request,
            IObjectResetParticipantSource participantSource)
        {
            var targetResolution = ObjectResetTargetResolver.ResolveTarget(snapshot, request);
            if (!targetResolution.Succeeded || !targetResolution.HasResolvedTarget)
            {
                return new ObjectResetPlan(
                    request,
                    default,
                    Array.Empty<ObjectResetParticipantEntry>(),
                    targetResolution.SnapshotIssues());
            }

            return BuildPlan(request, targetResolution.ResolvedTarget, participantSource);
        }

        public ObjectResetResult Execute(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetRequest request,
            IObjectResetParticipantSource participantSource)
        {
            string sourceText = Normalize(snapshot == null ? null : snapshot.Source);
            string reasonText = Normalize(request.Reason);
            var targetResolution = ObjectResetTargetResolver.ResolveTarget(snapshot, request);
            if (!targetResolution.Succeeded || !targetResolution.HasResolvedTarget)
            {
                return targetResolution;
            }

            var plan = BuildPlan(request, targetResolution.ResolvedTarget, participantSource);
            if (plan.HasBlockingIssues)
            {
                return ObjectResetResult.FromExecution(
                    request,
                    ObjectResetResultStatus.Failed,
                    targetResolution.ResolvedTarget,
                    true,
                    plan,
                    Array.Empty<ObjectResetParticipantResult>(),
                    plan.SnapshotIssues(),
                    "Object Reset plan failed because it contains blocking issues.");
            }

            if (!plan.HasParticipants)
            {
                return ObjectResetResult.FromExecution(
                    request,
                    ObjectResetResultStatus.SucceededNoParticipants,
                    targetResolution.ResolvedTarget,
                    true,
                    plan,
                    Array.Empty<ObjectResetParticipantResult>(),
                    plan.SnapshotIssues(),
                    "Object Reset completed with no participants.");
            }

            var issues = new List<ObjectResetIssue>(plan.SnapshotIssues());
            var participantResults = new List<ObjectResetParticipantResult>(plan.ParticipantCount);
            var executionEntries = new List<ParticipantExecutionEntry<ObjectResetParticipantInvocation>>(plan.ParticipantCount);
            var state = new ObjectResetExecutionState(
                request,
                targetResolution.ResolvedTarget,
                issues,
                participantResults);

            for (int i = 0; i < plan.Participants.Count; i++)
            {
                var entry = plan.Participants[i];
                executionEntries.Add(CreateExecutionEntry(entry, request, targetResolution.ResolvedTarget));
            }

            _ = ParticipantExecutor.Execute<ObjectResetParticipantInvocation, ObjectResetParticipantResult>(
                sourceText,
                reasonText,
                executionEntries,
                InvokeParticipant,
                (entry, result) => IsResultValid(entry, result),
                (entry, result) => IsResultSuccessful(state, entry, result),
                (entry, result) => IsResultBlocking(entry, result),
                (entry, result) => GetIssueCount(entry, result),
                (entry, exception) => CreateExceptionIssue(state, entry, exception),
                (entry, result) => CreateInvalidResultIssue(state, entry, result));

            int blockingIssues = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].IsBlocking)
                {
                    blockingIssues++;
                }
            }

            int nonBlockingIssues = issues.Count - blockingIssues;
            var status = blockingIssues > 0
                ? ObjectResetResultStatus.Failed
                : nonBlockingIssues > 0
                    ? ObjectResetResultStatus.CompletedWithWarnings
                    : ObjectResetResultStatus.Succeeded;
            string message = status == ObjectResetResultStatus.Succeeded
                ? "Object Reset completed successfully."
                : status == ObjectResetResultStatus.CompletedWithWarnings
                    ? "Object Reset completed with non-blocking participant warnings."
                    : "Object Reset failed because at least one required participant blocked reset.";

            return ObjectResetResult.FromExecution(
                request,
                status,
                targetResolution.ResolvedTarget,
                true,
                plan,
                participantResults,
                issues,
                message);
        }

        private static ParticipantExecutionEntry<ObjectResetParticipantInvocation> CreateExecutionEntry(
            ObjectResetParticipantEntry entry,
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget)
        {
            return new ParticipantExecutionEntry<ObjectResetParticipantInvocation>(
                new ObjectResetParticipantInvocation(entry, request, resolvedTarget),
                ToParticipantRequiredness(entry.Requiredness),
                entry.Order,
                entry.SourceIndex,
                entry.ParticipantId.StableText);
        }

        private static ObjectResetParticipantResult InvokeParticipant(ObjectResetParticipantInvocation participant)
        {
            var context = new ObjectResetContext(participant.Request, participant.ResolvedTarget, participant.Entry.Descriptor);
            return participant.Entry.Participant.ResetObject(context);
        }

        private static bool IsResultValid(
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            ObjectResetParticipantResult result)
        {
            return IsValidResultForEntry(result, entry.Participant.Entry, entry.Participant.Request, entry.Participant.ResolvedTarget, out _);
        }

        private static bool IsResultSuccessful(
            ObjectResetExecutionState state,
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            ObjectResetParticipantResult result)
        {
            state.ParticipantResults.Add(result);
            if (result.Failed)
            {
                state.Issues.Add(CreateFailureIssue(result));
            }

            return result.Succeeded || result.WasSkipped;
        }

        private static bool IsResultBlocking(
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            ObjectResetParticipantResult result)
        {
            return result.BlocksReset;
        }

        private static int GetIssueCount(
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            ObjectResetParticipantResult result)
        {
            return result.IssueCount;
        }

        private static ParticipantExecutionIssue CreateExceptionIssue(
            ObjectResetExecutionState state,
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            Exception exception)
        {
            string message = entry.Participant.Entry.IsRequired
                ? $"Required Object Reset participant threw: {exception.Message}"
                : $"Optional Object Reset participant threw: {exception.Message}";
            ObjectResetParticipantResult participantResult = CreateFailure(
                entry.Participant.Entry,
                entry.Participant.Request,
                entry.Participant.ResolvedTarget,
                entry.Participant.Entry.Descriptor.Source,
                entry.Participant.Entry.Descriptor.Reason,
                1,
                message);
            state.ParticipantResults.Add(participantResult);
            state.Issues.Add(CreateFailureIssue(participantResult));

            return CreateExecutionIssue(entry, participantResult);
        }

        private static ParticipantExecutionIssue CreateInvalidResultIssue(
            ObjectResetExecutionState state,
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            ObjectResetParticipantResult result)
        {
            string message = GetInvalidResultMessage(result, entry.Participant.Entry, entry.Participant.Request, entry.Participant.ResolvedTarget);
            ObjectResetParticipantResult participantResult = CreateFailure(
                entry.Participant.Entry,
                entry.Participant.Request,
                entry.Participant.ResolvedTarget,
                entry.Participant.Entry.Descriptor.Source,
                entry.Participant.Entry.Descriptor.Reason,
                Math.Max(1, result.IssueCount),
                message);
            state.ParticipantResults.Add(participantResult);
            state.Issues.Add(CreateFailureIssue(participantResult));

            return CreateExecutionIssue(entry, participantResult);
        }

        private static ParticipantExecutionIssue CreateExecutionIssue(
            ParticipantExecutionEntry<ObjectResetParticipantInvocation> entry,
            ObjectResetParticipantResult participantResult)
        {
            return new ParticipantExecutionIssue(
                participantResult.BlocksReset ? ParticipantExecutionIssueSeverity.Error : ParticipantExecutionIssueSeverity.Warning,
                entry.Label,
                participantResult.Source,
                participantResult.Reason,
                participantResult.Message,
                Math.Max(1, participantResult.IssueCount));
        }

        private static ObjectResetParticipantResult CreateFailure(
            ObjectResetParticipantEntry entry,
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            string source,
            string reason,
            int issueCount,
            string message)
        {
            var context = new ObjectResetContext(request, resolvedTarget, entry.Descriptor);
            return ObjectResetParticipantResult.Failure(context, issueCount, source, reason, message);
        }

        private static ObjectResetIssue CreateFailureIssue(ObjectResetParticipantResult participantResult)
        {
            return participantResult.BlocksReset
                ? ObjectResetIssue.Error(
                    ObjectResetIssueKind.RequiredParticipantFailed,
                    $"Required Object Reset participant '{participantResult.ParticipantId.StableText}' blocked reset with status '{participantResult.Status}'.")
                : ObjectResetIssue.Warning(
                    ObjectResetIssueKind.OptionalParticipantFailed,
                    $"Optional Object Reset participant '{participantResult.ParticipantId.StableText}' failed without blocking reset with status '{participantResult.Status}'.");
        }

        private static ParticipantRequiredness ToParticipantRequiredness(ObjectResetParticipantRequiredness requiredness)
        {
            switch (requiredness)
            {
                case ObjectResetParticipantRequiredness.Required:
                    return ParticipantRequiredness.Required;
                case ObjectResetParticipantRequiredness.Optional:
                    return ParticipantRequiredness.Optional;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Object Reset participant requiredness must be explicit.");
            }
        }

        private static bool IsValidResultForEntry(
            ObjectResetParticipantResult result,
            ObjectResetParticipantEntry entry,
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            out string message)
        {
            if (!Enum.IsDefined(typeof(ObjectResetParticipantResultStatus), result.Status)
                || result.Status == ObjectResetParticipantResultStatus.Unknown)
            {
                message = "Object Reset participant returned a result with an undefined status.";
                return false;
            }

            if (!result.Request.Equals(request))
            {
                message = "Object Reset participant returned a result for a different request.";
                return false;
            }

            if (!result.ResolvedTarget.Equals(resolvedTarget))
            {
                message = "Object Reset participant returned a result for a different resolved target.";
                return false;
            }

            if (!result.ParticipantId.Equals(entry.ParticipantId))
            {
                message = "Object Reset participant returned a result for a different participant id.";
                return false;
            }

            if (result.Target != entry.Target)
            {
                message = "Object Reset participant returned a result for a different participant target.";
                return false;
            }

            if (result.Requiredness != entry.Requiredness)
            {
                message = "Object Reset participant returned a result with different requiredness.";
                return false;
            }

            if (!result.IsValid)
            {
                message = "Object Reset participant returned an invalid result.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static string GetInvalidResultMessage(
            ObjectResetParticipantResult result,
            ObjectResetParticipantEntry entry,
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget)
        {
            if (!Enum.IsDefined(typeof(ObjectResetParticipantResultStatus), result.Status)
                || result.Status == ObjectResetParticipantResultStatus.Unknown)
            {
                return "Object Reset participant returned a result with an undefined status.";
            }

            if (!result.Request.Equals(request))
            {
                return "Object Reset participant returned a result for a different request.";
            }

            if (!result.ResolvedTarget.Equals(resolvedTarget))
            {
                return "Object Reset participant returned a result for a different resolved target.";
            }

            if (!result.ParticipantId.Equals(entry.ParticipantId))
            {
                return "Object Reset participant returned a result for a different participant id.";
            }

            if (result.Target != entry.Target)
            {
                return "Object Reset participant returned a result for a different participant target.";
            }

            if (result.Requiredness != entry.Requiredness)
            {
                return "Object Reset participant returned a result with different requiredness.";
            }

            return "Object Reset participant returned an invalid result.";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }

        private ObjectResetPlan BuildPlan(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            IObjectResetParticipantSource participantSource)
        {
            var issues = new List<ObjectResetIssue>();
            var entries = new List<ObjectResetParticipantEntry>();
            IReadOnlyList<IObjectResetParticipant> participants;

            try
            {
                participants = participantSource == null
                    ? Array.Empty<IObjectResetParticipant>()
                    : participantSource.ResolveObjectResetParticipants(request, resolvedTarget) ?? Array.Empty<IObjectResetParticipant>();
            }
            catch (Exception exception)
            {
                issues.Add(ObjectResetIssue.Error(
                    ObjectResetIssueKind.ParticipantSourceException,
                    $"Object Reset participant source threw while resolving participants: {exception.Message}"));
                participants = Array.Empty<IObjectResetParticipant>();
            }

            var participantIds = new HashSet<ObjectResetParticipantId>();
            for (int index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                if (participant == null)
                {
                    issues.Add(ObjectResetIssue.Warning(
                        ObjectResetIssueKind.InvalidParticipant,
                        $"Object Reset participant source returned a null participant at index '{index}'."));
                    continue;
                }

                ObjectResetParticipantDescriptor descriptor;
                try
                {
                    descriptor = participant.GetObjectResetDescriptor();
                }
                catch (Exception exception)
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.InvalidParticipant,
                        $"Object Reset participant descriptor failed at index '{index}': {exception.Message}"));
                    continue;
                }

                if (!descriptor.IsValid)
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.InvalidParticipant,
                        $"Object Reset participant descriptor at index '{index}' is invalid."));
                    continue;
                }

                if (!descriptor.SupportsResolvedTarget(request, resolvedTarget))
                {
                    issues.Add(ObjectResetIssue.Warning(
                        ObjectResetIssueKind.ForeignOrStaleParticipant,
                        $"Object Reset participant '{descriptor.ParticipantId.StableText}' does not support the resolved request target and was ignored."));
                    continue;
                }

                if (!participantIds.Add(descriptor.ParticipantId))
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.DuplicateParticipant,
                        $"Object Reset participant id '{descriptor.ParticipantId.StableText}' was provided more than once."));
                    continue;
                }

                entries.Add(new ObjectResetParticipantEntry(participant, descriptor, index));
            }

            if (entries.Count == 0 && !request.AllowsNoParticipants)
            {
                issues.Add(ObjectResetIssue.Error(
                    ObjectResetIssueKind.NoParticipants,
                    "Object Reset request rejected because no participants were available and policy disallows no-participant success."));
            }

            return new ObjectResetPlan(
                request,
                resolvedTarget,
                entries,
                issues);
        }

        private sealed class ObjectResetParticipantInvocation
        {
            internal ObjectResetParticipantInvocation(
                ObjectResetParticipantEntry entry,
                ObjectResetRequest request,
                ObjectEntryDescriptor resolvedTarget)
            {
                Entry = entry;
                Request = request;
                ResolvedTarget = resolvedTarget;
            }

            internal ObjectResetParticipantEntry Entry { get; }

            internal ObjectResetRequest Request { get; }

            internal ObjectEntryDescriptor ResolvedTarget { get; }
        }

        private sealed class ObjectResetExecutionState
        {
            internal ObjectResetExecutionState(
                ObjectResetRequest request,
                ObjectEntryDescriptor resolvedTarget,
                List<ObjectResetIssue> issues,
                List<ObjectResetParticipantResult> participantResults)
            {
                Request = request;
                ResolvedTarget = resolvedTarget;
                Issues = issues;
                ParticipantResults = participantResults;
            }

            internal ObjectResetRequest Request { get; }

            internal ObjectEntryDescriptor ResolvedTarget { get; }

            internal List<ObjectResetIssue> Issues { get; }

            internal List<ObjectResetParticipantResult> ParticipantResults { get; }
        }
    }
}
