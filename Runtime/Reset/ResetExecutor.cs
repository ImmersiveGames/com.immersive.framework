using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Single orchestration boundary for ResetSubject execution.
    /// It uses UnityEngine.Awaitable at the executor boundary while participants remain synchronous.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12C ResetExecutor over ResetRegistry.")]
    public sealed class ResetExecutor
    {
        private readonly ResetRegistry _registry;

        public ResetExecutor(ResetRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async Awaitable<ResetExecutionResult> ExecuteAsync(ResetExecutionRequest request)
        {
            if (!request.IsValid)
            {
                return ResetExecutionResult.RejectedInvalidRequest(
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, "ResetExecutor requires a valid ResetExecutionRequest."),
                    request.Source,
                    request.Reason);
            }

            _registry.CleanupStaleOwners();

            if (!request.HasSubjects)
            {
                var issue = request.AllowNoSubjects
                    ? ResetIssue.Info(ResetIssueKind.NoSubjects, "Reset execution request selected no subjects and AllowNoSubjects is true.")
                    : ResetIssue.Error(ResetIssueKind.NoSubjects, "Reset execution request selected no subjects.");

                return request.AllowNoSubjects
                    ? ResetExecutionResult.SucceededNoSubjects(issue, request.Source, request.Reason)
                    : ResetExecutionResult.FailedNoSubjects(issue, request.Source, request.Reason);
            }

            var subjectResults = new List<ResetSubjectResult>(request.SubjectIds.Count);
            var executionIssues = new List<ResetIssue>();
            bool failed = false;

            for (int i = 0; i < request.SubjectIds.Count; i++)
            {
                ResetSubjectId subjectId = request.SubjectIds[i];
                ResetSubjectResult subjectResult = ExecuteSubject(subjectId, request);
                subjectResults.Add(subjectResult);

                if (subjectResult.HasBlockingIssues || subjectResult.Failed)
                {
                    failed = true;
                    if (request.StopOnFailure)
                    {
                        break;
                    }
                }

                if (request.YieldBetweenSubjects && i < request.SubjectIds.Count - 1)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            return ResetExecutionResult.FromSubjectResults(
                subjectResults,
                executionIssues,
                request.Source,
                request.Reason,
                failed);
        }

        private ResetSubjectResult ExecuteSubject(ResetSubjectId subjectId, ResetExecutionRequest request)
        {
            if (!_registry.TryGetSubjectHandle(subjectId, out ResetRegistrationHandle subjectHandle, out ResetSubject subject, out ResetIssue resolveIssue))
            {
                return ResetSubjectResult.FailedSubjectNotFound(subjectId, resolveIssue, request.Source, request.Reason);
            }

            IReadOnlyList<ResetParticipantRuntimeEntry> participants = _registry.GetParticipantRuntimeEntries(subjectHandle);
            if (participants.Count == 0)
            {
                ResetIssue issue = request.AllowNoParticipants
                    ? ResetIssue.Info(ResetIssueKind.NoParticipants, $"Reset subject '{subjectId.StableText}' has no registered participants and AllowNoParticipants is true.")
                    : ResetIssue.Error(ResetIssueKind.NoParticipants, $"Reset subject '{subjectId.StableText}' has no registered participants.");

                return request.AllowNoParticipants
                    ? ResetSubjectResult.SkippedNoParticipants(subject, issue, request.Source, request.Reason)
                    : ResetSubjectResult.FailedNoParticipants(subject, issue, request.Source, request.Reason);
            }

            var participantResults = new List<ResetParticipantResult>(participants.Count);
            var subjectIssues = new List<ResetIssue>();
            bool hasBlockingFailure = false;

            for (int i = 0; i < participants.Count; i++)
            {
                ResetParticipantRuntimeEntry entry = participants[i];
                ResetParticipantResult participantResult = ExecuteParticipant(subject, entry, request, subjectIssues);
                participantResults.Add(participantResult);

                if (participantResult.BlocksReset)
                {
                    hasBlockingFailure = true;
                    if (request.StopOnFailure)
                    {
                        break;
                    }
                }
            }

            return hasBlockingFailure
                ? ResetSubjectResult.FailedResult(
                    subject,
                    participantResults,
                    subjectIssues,
                    request.Source,
                    request.Reason,
                    "Reset subject failed because at least one required participant failed.")
                : ResetSubjectResult.SucceededResult(
                    subject,
                    participantResults,
                    subjectIssues,
                    request.Source,
                    request.Reason,
                    "Reset subject completed successfully.");
        }

        private static ResetParticipantResult ExecuteParticipant(
            ResetSubject subject,
            ResetParticipantRuntimeEntry entry,
            ResetExecutionRequest request,
            List<ResetIssue> subjectIssues)
        {
            try
            {
                var context = new ResetContext(subject, entry.Descriptor, request.Source, request.Reason);
                ResetParticipantResult result = entry.Participant.Reset(context);
                if (result.Descriptor.ParticipantId != entry.Descriptor.ParticipantId || result.SubjectId != subject.SubjectId)
                {
                    var issue = ResetIssue.Error(
                        ResetIssueKind.InvalidParticipant,
                        $"Reset participant '{entry.Descriptor.ParticipantId.StableText}' returned a result for a different participant or subject.");
                    subjectIssues.Add(issue);
                    return ResetParticipantResult.CreateFailed(
                        entry.Descriptor,
                        1,
                        request.Source,
                        request.Reason,
                        issue.Message);
                }

                if (result.Failed && result.IsOptional)
                {
                    subjectIssues.Add(ResetIssue.Warning(
                        ResetIssueKind.InvalidParticipant,
                        $"Optional reset participant '{entry.Descriptor.ParticipantId.StableText}' failed without blocking the subject reset."));
                }

                return result;
            }
            catch (Exception exception)
            {
                var issue = ResetIssue.Error(
                    ResetIssueKind.Exception,
                    $"Reset participant '{entry.Descriptor.ParticipantId.StableText}' threw during reset. message='{exception.Message}'");
                subjectIssues.Add(issue);
                return ResetParticipantResult.CreateFailed(
                    entry.Descriptor,
                    1,
                    request.Source,
                    request.Reason,
                    issue.Message);
            }
        }
    }
}
