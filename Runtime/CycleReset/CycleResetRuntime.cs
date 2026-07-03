using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Executes an already-built Cycle Reset plan and aggregates participant results.
    /// It does not discover scene objects, reset Unity components, release content, reload scenes, restore snapshots or call gameplay systems directly.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset executor; executes supplied participants only and has no Unity side effects.")]
    public sealed class CycleResetRuntime
    {
        public CycleResetPlan CreatePlan(
            CycleResetRequest request,
            IReadOnlyList<ICycleResetParticipant> participants,
            string source,
            string reason)
        {
            return CycleResetPlan.FromParticipants(request, participants, source, reason);
        }

        public CycleResetResult ExecutePlan(
            CycleResetPlan plan,
            string source,
            string reason)
        {
            string sourceText = Normalize(source);
            string reasonText = Normalize(reason);

            if (!plan.Request.IsValid)
            {
                return CycleResetResult.RejectedInvalidPlan(
                    plan.Request,
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime rejected the plan because the request is invalid.");
            }

            if (plan.Rejected)
            {
                return CycleResetResult.RejectedInvalidPlan(
                    plan.Request,
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime rejected the plan because the plan was already rejected.");
            }

            if (plan.Skipped || !plan.HasEntries)
            {
                return CycleResetResult.FromResults(
                    plan.Request,
                    Array.Empty<CycleResetParticipantResult>(),
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime completed with no participants to execute.");
            }

            if (!plan.Planned)
            {
                return CycleResetResult.RejectedInvalidPlan(
                    plan.Request,
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime rejected the plan because it is not executable.");
            }

            var results = new List<CycleResetParticipantResult>(plan.EntryCount);
            var executionIssues = new List<CycleResetIssue>(plan.SnapshotIssues());
            var executionEntries = new List<ParticipantExecutionEntry<CycleResetParticipantInvocation>>(plan.EntryCount);
            IReadOnlyList<CycleResetParticipantEntry> entries = plan.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.IsValid)
                {
                    executionIssues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.InvalidDescriptor,
                        entry.ParticipantId,
                        entry.ParticipantScope,
                        "Cycle Reset runtime found an invalid participant entry."));
                    continue;
                }

                if (!entry.Descriptor.SupportsRequest(plan.Request))
                {
                    executionIssues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.UnsupportedScope,
                        entry.ParticipantId,
                        entry.ParticipantScope,
                        "Cycle Reset runtime found a participant entry that does not support the request scope."));
                    continue;
                }

                executionEntries.Add(CreateExecutionEntry(entry, plan.Request, executionEntries.Count));
            }

            if (executionEntries.Count > 0)
            {
                _ = ParticipantExecutor.Execute<CycleResetParticipantInvocation, CycleResetParticipantResult>(
                    sourceText,
                    reasonText,
                    executionEntries,
                    InvokeParticipant,
                    IsResultValid,
                    IsResultSuccessful,
                    IsResultBlocking,
                    GetIssueCount,
                    CreateExceptionIssue,
                    CreateInvalidResultIssue);
            }

            return CycleResetResult.FromResults(
                plan.Request,
                results,
                executionIssues,
                sourceText,
                reasonText,
                "Cycle Reset runtime executed the supplied plan.");

            ParticipantExecutionEntry<CycleResetParticipantInvocation> CreateExecutionEntry(
                CycleResetParticipantEntry entry,
                CycleResetRequest request,
                int order)
            {
                return new ParticipantExecutionEntry<CycleResetParticipantInvocation>(
                    new CycleResetParticipantInvocation(entry, request),
                    ToParticipantRequiredness(entry.Requiredness),
                    order,
                    entry.SourceIndex,
                    string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.ParticipantId.StableText : entry.DisplayName);
            }

            CycleResetParticipantResult InvokeParticipant(CycleResetParticipantInvocation participant)
            {
                var context = new CycleResetContext(participant.Request, participant.Entry.Descriptor);
                return participant.Entry.Participant.ResetCycle(context);
            }

            bool IsResultValid(ParticipantExecutionEntry<CycleResetParticipantInvocation> entry, CycleResetParticipantResult result)
            {
                return IsValidResultForEntry(result, entry.Participant.Entry, plan.Request, out _);
            }

            bool IsResultSuccessful(ParticipantExecutionEntry<CycleResetParticipantInvocation> entry, CycleResetParticipantResult result)
            {
                results.Add(result);
                if (result.Failed)
                {
                    executionIssues.Add(CreateFailureIssue(result));
                }

                return result.Succeeded || result.WasSkipped;
            }

            bool IsResultBlocking(ParticipantExecutionEntry<CycleResetParticipantInvocation> entry, CycleResetParticipantResult result)
            {
                return result.BlocksReset;
            }

            int GetIssueCount(ParticipantExecutionEntry<CycleResetParticipantInvocation> entry, CycleResetParticipantResult result)
            {
                return result.IssueCount;
            }

            ParticipantExecutionIssue CreateExceptionIssue(ParticipantExecutionEntry<CycleResetParticipantInvocation> entry, Exception exception)
            {
                string message = $"Cycle Reset participant threw an exception: {exception.GetType().Name}.";
                results.Add(CreateFailure(entry.Participant.Entry, plan.Request, sourceText, reasonText, message));
                executionIssues.Add(CycleResetIssue.BlockingIssue(
                    CycleResetIssueKind.ParticipantException,
                    entry.Participant.Entry.ParticipantId,
                    entry.Participant.Entry.ParticipantScope,
                    message));

                return new ParticipantExecutionIssue(
                    ParticipantExecutionIssueSeverity.Error,
                    entry.Label,
                    sourceText,
                    reasonText,
                    message,
                    1);
            }

            ParticipantExecutionIssue CreateInvalidResultIssue(ParticipantExecutionEntry<CycleResetParticipantInvocation> entry, CycleResetParticipantResult result)
            {
                string invalidResultMessage = GetInvalidResultMessage(result, entry.Participant.Entry, plan.Request);
                results.Add(CreateFailure(entry.Participant.Entry, plan.Request, sourceText, reasonText, invalidResultMessage));
                executionIssues.Add(CreateResultIssue(entry.Participant.Entry, invalidResultMessage));

                return new ParticipantExecutionIssue(
                    entry.Participant.Entry.IsRequired ? ParticipantExecutionIssueSeverity.Error : ParticipantExecutionIssueSeverity.Warning,
                    entry.Label,
                    sourceText,
                    reasonText,
                    invalidResultMessage,
                    1);
            }
        }

        private static CycleResetParticipantResult CreateFailure(
            CycleResetParticipantEntry entry,
            CycleResetRequest request,
            string source,
            string reason,
            string message)
        {
            var context = new CycleResetContext(request, entry.Descriptor);
            return CycleResetParticipantResult.Failure(context, 1, source, reason, message);
        }

        private static CycleResetIssue CreateResultIssue(CycleResetParticipantEntry entry, string message)
        {
            return entry.IsRequired
                ? CycleResetIssue.BlockingIssue(
                    CycleResetIssueKind.InvalidParticipantResult,
                    entry.ParticipantId,
                    entry.ParticipantScope,
                    message)
                : CycleResetIssue.NonBlockingIssue(
                    CycleResetIssueKind.InvalidParticipantResult,
                    entry.ParticipantId,
                    entry.ParticipantScope,
                    message);
        }

        private static CycleResetIssue CreateFailureIssue(CycleResetParticipantResult result)
        {
            return result.BlocksReset
                ? CycleResetIssue.BlockingIssue(
                    CycleResetIssueKind.RequiredParticipantFailed,
                    result.ParticipantId,
                    result.ParticipantScope,
                    result.Message)
                : CycleResetIssue.NonBlockingIssue(
                    CycleResetIssueKind.OptionalParticipantFailed,
                    result.ParticipantId,
                    result.ParticipantScope,
                    result.Message);
        }

        private static ParticipantRequiredness ToParticipantRequiredness(CycleResetParticipantRequiredness requiredness)
        {
            switch (requiredness)
            {
                case CycleResetParticipantRequiredness.Required:
                    return ParticipantRequiredness.Required;
                case CycleResetParticipantRequiredness.Optional:
                    return ParticipantRequiredness.Optional;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Cycle Reset participant requiredness must be explicit.");
            }
        }

        private static bool IsValidResultForEntry(
            CycleResetParticipantResult result,
            CycleResetParticipantEntry entry,
            CycleResetRequest request,
            out string message)
        {
            if (!result.IsValid)
            {
                message = "Cycle Reset participant returned an invalid result.";
                return false;
            }

            if (!result.Request.Equals(request))
            {
                message = "Cycle Reset participant returned a result for a different request.";
                return false;
            }

            if (!result.ParticipantId.Equals(entry.ParticipantId))
            {
                message = "Cycle Reset participant returned a result for a different participant id.";
                return false;
            }

            if (result.ParticipantScope != entry.ParticipantScope)
            {
                message = "Cycle Reset participant returned a result for a different participant scope.";
                return false;
            }

            if (result.Requiredness != entry.Requiredness)
            {
                message = "Cycle Reset participant returned a result with different requiredness.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static string GetInvalidResultMessage(
            CycleResetParticipantResult result,
            CycleResetParticipantEntry entry,
            CycleResetRequest request)
        {
            if (!result.IsValid)
            {
                return "Cycle Reset participant returned an invalid result.";
            }

            if (!result.Request.Equals(request))
            {
                return "Cycle Reset participant returned a result for a different request.";
            }

            if (!result.ParticipantId.Equals(entry.ParticipantId))
            {
                return "Cycle Reset participant returned a result for a different participant id.";
            }

            if (result.ParticipantScope != entry.ParticipantScope)
            {
                return "Cycle Reset participant returned a result for a different participant scope.";
            }

            if (result.Requiredness != entry.Requiredness)
            {
                return "Cycle Reset participant returned a result with different requiredness.";
            }

            return string.Empty;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }

        private sealed class CycleResetParticipantInvocation
        {
            internal CycleResetParticipantInvocation(CycleResetParticipantEntry entry, CycleResetRequest request)
            {
                Entry = entry;
                Request = request;
            }

            internal CycleResetParticipantEntry Entry { get; }

            internal CycleResetRequest Request { get; }
        }
    }
}
