using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for preview.12C ResetExecutor.
    /// It validates the new executor without ObjectResetTrigger or ObjectEntry snapshots.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "preview.12C ResetExecutor synthetic smoke.")]
    internal static class ResetExecutorQaSmokeRunner
    {
        internal const string SmokeName = "Reset Executor Synthetic Smoke";

        internal static async Task<bool> RunDiagnosticsSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ResetExecutorQaSmokeRunner));
            int baselineSubjects = runtimeHost.ResetRegistrySubjectCount;
            int baselineParticipants = runtimeHost.ResetRegistryParticipantCount;

            bool ownerResolved = TryResolveOwner(runtimeHost, out RuntimeContentOwner owner, out string ownerIssue);
            if (!ownerResolved)
            {
                logger.Warning(
                    "QA Reset Executor Synthetic Smoke aborted.",
                    LogFields.Field("status", "Failed"),
                    LogFields.Field("reason", ownerIssue));
                return false;
            }

            bool singlePassed = await ValidateSingleSubjectExecutionAsync(runtimeHost, logger, normalizedSource, owner);
            bool emptySelectionPassed = await ValidateAllowNoSubjectsAsync(runtimeHost, logger, normalizedSource);
            bool noParticipantsPassed = await ValidateAllowNoParticipantsAsync(runtimeHost, logger, normalizedSource, owner);
            bool failurePolicyPassed = await ValidateRequiredFailureAndStopOnFailureAsync(runtimeHost, logger, normalizedSource, owner);
            bool multiSubjectPassed = await ValidateMultiSubjectExecutionAsync(runtimeHost, logger, normalizedSource, owner);

            runtimeHost.ResetRegistry.CleanupStaleOwners();
            bool cleanupPassed = runtimeHost.ResetRegistrySubjectCount == baselineSubjects
                && runtimeHost.ResetRegistryParticipantCount == baselineParticipants;

            bool passed = singlePassed
                && emptySelectionPassed
                && noParticipantsPassed
                && failurePolicyPassed
                && multiSubjectPassed
                && cleanupPassed;

            logger.Info(
                "QA Reset Executor Synthetic Smoke completed.",
                LogFields.Of(
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("source", normalizedSource),
                    LogFields.Field("singleSubject", singlePassed),
                    LogFields.Field("allowNoSubjects", emptySelectionPassed),
                    LogFields.Field("allowNoParticipants", noParticipantsPassed),
                    LogFields.Field("requiredFailure", failurePolicyPassed),
                    LogFields.Field("multiSubject", multiSubjectPassed),
                    LogFields.Field("cleanup", cleanupPassed),
                    LogFields.Field("subjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("participants", runtimeHost.ResetRegistryParticipantCount.ToString())));
            return passed;
        }

        private static async Task<bool> ValidateSingleSubjectExecutionAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeContentOwner owner)
        {
            using var fixture = ResetExecutorSmokeFixture.Create(runtimeHost, owner, "QA_ResetExecutor_SingleOwner");
            var required = new SyntheticResetParticipant("required-success", ResetParticipantRequiredness.Required, 0, ResetParticipantResultStatus.Succeeded);
            var optional = new SyntheticResetParticipant("optional-failure", ResetParticipantRequiredness.Optional, 10, ResetParticipantResultStatus.Failed);
            bool registered = fixture.RegisterSubject("qa.reset.executor.single", ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored)
                && fixture.RegisterParticipant(required)
                && fixture.RegisterParticipant(optional);

            var executor = new ResetExecutor(runtimeHost.ResetRegistry);
            ResetExecutionResult result = await executor.ExecuteAsync(ResetExecutionRequest.ForSingleSubject(
                fixture.Subject.SubjectId,
                allowNoParticipants: false,
                source: source,
                reason: "qa.reset-executor.single",
                stopOnFailure: true));

            bool passed = registered
                && result.Succeeded
                && result.Status == ResetExecutionStatus.Succeeded
                && result.SubjectCount == 1
                && result.ParticipantCount == 2
                && result.ParticipantSucceeded == 1
                && result.ParticipantFailed == 1
                && result.BlockingIssueCount == 0
                && result.NonBlockingIssueCount >= 1
                && required.ResetCalls == 1
                && optional.ResetCalls == 1;

            LogExecutorStep(logger, "single-subject", passed, runtimeHost, result, $"requiredCalls='{required.ResetCalls}' optionalCalls='{optional.ResetCalls}'");
            return passed;
        }

        private static async Task<bool> ValidateAllowNoSubjectsAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            var executor = new ResetExecutor(runtimeHost.ResetRegistry);
            ResetExecutionResult allowed = await executor.ExecuteAsync(ResetExecutionRequest.Empty(
                allowNoSubjects: true,
                allowNoParticipants: false,
                stopOnFailure: true,
                source: source,
                reason: "qa.reset-executor.no-subjects.allowed"));
            ResetExecutionResult blocked = await executor.ExecuteAsync(ResetExecutionRequest.Empty(
                allowNoSubjects: false,
                allowNoParticipants: false,
                stopOnFailure: true,
                source: source,
                reason: "qa.reset-executor.no-subjects.blocked"));

            bool passed = allowed.Succeeded
                && allowed.Status == ResetExecutionStatus.SucceededNoSubjects
                && blocked.Failed
                && blocked.Status == ResetExecutionStatus.FailedNoSubjects
                && blocked.BlockingIssueCount == 1;

            LogExecutorStep(logger, "allow-no-subjects", passed, runtimeHost, blocked, $"allowed='{allowed.Status}' blocked='{blocked.Status}'");
            return passed;
        }

        private static async Task<bool> ValidateAllowNoParticipantsAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeContentOwner owner)
        {
            using var allowedFixture = ResetExecutorSmokeFixture.Create(runtimeHost, owner, "QA_ResetExecutor_NoParticipantsAllowed");
            using var blockedFixture = ResetExecutorSmokeFixture.Create(runtimeHost, owner, "QA_ResetExecutor_NoParticipantsBlocked");
            bool registered = allowedFixture.RegisterSubject("qa.reset.executor.no-participants.allowed", ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored)
                && blockedFixture.RegisterSubject("qa.reset.executor.no-participants.blocked", ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored);

            var executor = new ResetExecutor(runtimeHost.ResetRegistry);
            ResetExecutionResult allowed = await executor.ExecuteAsync(ResetExecutionRequest.ForSingleSubject(
                allowedFixture.Subject.SubjectId,
                allowNoParticipants: true,
                source: source,
                reason: "qa.reset-executor.no-participants.allowed"));
            ResetExecutionResult blocked = await executor.ExecuteAsync(ResetExecutionRequest.ForSingleSubject(
                blockedFixture.Subject.SubjectId,
                allowNoParticipants: false,
                source: source,
                reason: "qa.reset-executor.no-participants.blocked"));

            bool passed = registered
                && allowed.Succeeded
                && allowed.Subjects.Count == 1
                && allowed.Subjects[0].Status == ResetSubjectResultStatus.SkippedNoParticipants
                && blocked.Failed
                && blocked.Subjects.Count == 1
                && blocked.Subjects[0].Status == ResetSubjectResultStatus.FailedNoParticipants
                && blocked.BlockingIssueCount == 1;

            LogExecutorStep(logger, "allow-no-participants", passed, runtimeHost, blocked, $"allowed='{allowed.Subjects[0].Status}' blocked='{blocked.Subjects[0].Status}'");
            return passed;
        }

        private static async Task<bool> ValidateRequiredFailureAndStopOnFailureAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeContentOwner owner)
        {
            using var fixture = ResetExecutorSmokeFixture.Create(runtimeHost, owner, "QA_ResetExecutor_RequiredFailure");
            var failingRequired = new SyntheticResetParticipant("required-failure", ResetParticipantRequiredness.Required, 0, ResetParticipantResultStatus.Failed);
            var shouldNotRun = new SyntheticResetParticipant("required-after-failure", ResetParticipantRequiredness.Required, 10, ResetParticipantResultStatus.Succeeded);
            bool registered = fixture.RegisterSubject("qa.reset.executor.required-failure", ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored)
                && fixture.RegisterParticipant(failingRequired)
                && fixture.RegisterParticipant(shouldNotRun);

            var executor = new ResetExecutor(runtimeHost.ResetRegistry);
            ResetExecutionResult result = await executor.ExecuteAsync(ResetExecutionRequest.ForSingleSubject(
                fixture.Subject.SubjectId,
                allowNoParticipants: false,
                source: source,
                reason: "qa.reset-executor.required-failure",
                stopOnFailure: true));

            bool passed = registered
                && result.Failed
                && result.Status == ResetExecutionStatus.Failed
                && result.SubjectCount == 1
                && result.ParticipantCount == 1
                && result.ParticipantFailed == 1
                && result.BlockingIssueCount >= 1
                && failingRequired.ResetCalls == 1
                && shouldNotRun.ResetCalls == 0;

            LogExecutorStep(logger, "required-failure-stop-on-failure", passed, runtimeHost, result, $"failingCalls='{failingRequired.ResetCalls}' skippedCalls='{shouldNotRun.ResetCalls}'");
            return passed;
        }

        private static async Task<bool> ValidateMultiSubjectExecutionAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            RuntimeContentOwner owner)
        {
            using var firstFixture = ResetExecutorSmokeFixture.Create(runtimeHost, owner, "QA_ResetExecutor_MultiA");
            using var secondFixture = ResetExecutorSmokeFixture.Create(runtimeHost, owner, "QA_ResetExecutor_MultiB");
            var firstParticipant = new SyntheticResetParticipant("first-required", ResetParticipantRequiredness.Required, 0, ResetParticipantResultStatus.Succeeded);
            var secondParticipant = new SyntheticResetParticipant("second-required", ResetParticipantRequiredness.Required, 0, ResetParticipantResultStatus.Succeeded);
            bool registered = firstFixture.RegisterSubject("qa.reset.executor.multi.a", ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored)
                && secondFixture.RegisterSubject("qa.reset.executor.multi.b", ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored)
                && firstFixture.RegisterParticipant(firstParticipant)
                && secondFixture.RegisterParticipant(secondParticipant);

            var executor = new ResetExecutor(runtimeHost.ResetRegistry);
            ResetExecutionResult result = await executor.ExecuteAsync(ResetExecutionRequest.ForSubjectIds(
                new[] { firstFixture.Subject.SubjectId, secondFixture.Subject.SubjectId },
                allowNoSubjects: false,
                allowNoParticipants: false,
                stopOnFailure: true,
                source: source,
                reason: "qa.reset-executor.multi",
                yieldBetweenSubjects: true));

            bool passed = registered
                && result.Succeeded
                && result.Status == ResetExecutionStatus.Succeeded
                && result.SubjectCount == 2
                && result.SubjectSucceeded == 2
                && result.ParticipantCount == 2
                && result.ParticipantSucceeded == 2
                && firstParticipant.ResetCalls == 1
                && secondParticipant.ResetCalls == 1;

            LogExecutorStep(logger, "multi-subject", passed, runtimeHost, result, $"firstCalls='{firstParticipant.ResetCalls}' secondCalls='{secondParticipant.ResetCalls}'");
            return passed;
        }

        private static bool TryResolveOwner(
            FrameworkRuntimeHost runtimeHost,
            out RuntimeContentOwner owner,
            out string issue)
        {
            if (runtimeHost.TryResolveCurrentResetOwner(ResetSubjectScope.Activity, out owner, out issue))
            {
                return true;
            }

            return runtimeHost.TryResolveCurrentResetOwner(ResetSubjectScope.Runtime, out owner, out issue);
        }

        private static void LogExecutorStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            FrameworkRuntimeHost runtimeHost,
            ResetExecutionResult result,
            string detail)
        {
            logger.Info(
                "QA Reset Executor Synthetic Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("status", passed ? "Succeeded" : "Failed"),
                    LogFields.Field("executionStatus", result.Status.ToString()),
                    LogFields.Field("subjects", result.SubjectCount.ToString()),
                    LogFields.Field("subjectSucceeded", result.SubjectSucceeded.ToString()),
                    LogFields.Field("subjectFailed", result.SubjectFailed.ToString()),
                    LogFields.Field("participants", result.ParticipantCount.ToString()),
                    LogFields.Field("participantSucceeded", result.ParticipantSucceeded.ToString()),
                    LogFields.Field("participantSkipped", result.ParticipantSkipped.ToString()),
                    LogFields.Field("participantFailed", result.ParticipantFailed.ToString()),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount.ToString()),
                    LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount.ToString()),
                    LogFields.Field("registrySubjects", runtimeHost.ResetRegistrySubjectCount.ToString()),
                    LogFields.Field("registryParticipants", runtimeHost.ResetRegistryParticipantCount.ToString()),
                    LogFields.Field("detail", detail)));
        }

        private sealed class SyntheticResetParticipant : IResetParticipant
        {
            private readonly ResetParticipantId _participantId;
            private readonly ResetParticipantRequiredness _requiredness;
            private readonly int _order;
            private readonly ResetParticipantResultStatus _status;

            internal SyntheticResetParticipant(
                string participantId,
                ResetParticipantRequiredness requiredness,
                int order,
                ResetParticipantResultStatus status)
            {
                _participantId = ResetParticipantId.From(participantId);
                _requiredness = requiredness;
                _order = order;
                _status = status;
            }

            internal int ResetCalls { get; private set; }

            public bool TryCreateResetParticipantDescriptor(
                ResetSubject subject,
                out ResetParticipantDescriptor descriptor,
                out ResetIssue issue)
            {
                descriptor = default;
                issue = default;
                if (!subject.IsValid || !_participantId.IsValid)
                {
                    issue = ResetIssue.Error(ResetIssueKind.InvalidParticipant, "Synthetic reset participant descriptor is invalid.");
                    return false;
                }

                descriptor = new ResetParticipantDescriptor(
                    _participantId,
                    subject.SubjectId,
                    _requiredness,
                    _order,
                    _participantId.StableText,
                    nameof(ResetExecutorQaSmokeRunner),
                    "qa.reset-executor.synthetic-participant");
                return true;
            }

            public ResetParticipantResult Reset(ResetContext context)
            {
                ResetCalls++;
                switch (_status)
                {
                    case ResetParticipantResultStatus.Succeeded:
                        return ResetParticipantResult.CreateSucceeded(
                            context.Participant,
                            nameof(ResetExecutorQaSmokeRunner),
                            context.Reason,
                            "Synthetic participant reset succeeded.");
                    case ResetParticipantResultStatus.Skipped:
                        return ResetParticipantResult.CreateSkipped(
                            context.Participant,
                            1,
                            nameof(ResetExecutorQaSmokeRunner),
                            context.Reason,
                            "Synthetic participant reset skipped.");
                    case ResetParticipantResultStatus.Failed:
                        return ResetParticipantResult.CreateFailed(
                            context.Participant,
                            1,
                            nameof(ResetExecutorQaSmokeRunner),
                            context.Reason,
                            "Synthetic participant reset failed.");
                    default:
                        throw new InvalidOperationException($"Unsupported synthetic participant status '{_status}'.");
                }
            }
        }

        private sealed class ResetExecutorSmokeFixture : IDisposable
        {
            private readonly FrameworkRuntimeHost _runtimeHost;
            private readonly RuntimeContentOwner _owner;
            private readonly GameObject _ownerObject;
            private readonly List<ResetRegistrationHandle> _handles = new();
            private bool _disposed;

            private ResetExecutorSmokeFixture(
                FrameworkRuntimeHost runtimeHost,
                RuntimeContentOwner owner,
                GameObject ownerObject)
            {
                _runtimeHost = runtimeHost;
                _owner = owner;
                _ownerObject = ownerObject;
            }

            internal ResetSubject Subject { get; private set; }

            internal ResetRegistrationHandle SubjectHandle { get; private set; }

            internal static ResetExecutorSmokeFixture Create(
                FrameworkRuntimeHost runtimeHost,
                RuntimeContentOwner owner,
                string ownerName)
            {
                return new ResetExecutorSmokeFixture(runtimeHost, owner, new GameObject(ownerName));
            }

            internal bool RegisterSubject(
                string subjectId,
                ResetSubjectScope scope,
                ResetSubjectOrigin origin)
            {
                Subject = new ResetSubject(
                    ResetSubjectId.From(subjectId),
                    scope,
                    origin,
                    _owner,
                    _ownerObject.name,
                    "Smoke:ResetExecutor");
                ResetRegistryOperationResult result = _runtimeHost.RegisterResetSubject(
                    Subject,
                    _ownerObject,
                    nameof(ResetExecutorQaSmokeRunner),
                    "qa.reset-executor.register-subject");
                if (!result.Succeeded)
                {
                    return false;
                }

                SubjectHandle = result.Handle;
                _handles.Add(result.Handle);
                return true;
            }

            internal bool RegisterParticipant(IResetParticipant participant)
            {
                ResetRegistryOperationResult result = _runtimeHost.RegisterResetParticipant(
                    SubjectHandle,
                    participant,
                    _ownerObject,
                    nameof(ResetExecutorQaSmokeRunner),
                    "qa.reset-executor.register-participant");
                if (!result.Succeeded)
                {
                    return false;
                }

                _handles.Add(result.Handle);
                return true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                for (int i = _handles.Count - 1; i >= 0; i--)
                {
                    _runtimeHost.UnregisterResetRegistration(
                        _handles[i],
                        _ownerObject,
                        nameof(ResetExecutorQaSmokeRunner),
                        "qa.reset-executor.fixture-cleanup");
                }

                DestroyIfNeeded(_ownerObject);
                _disposed = true;
            }
        }

        private static void DestroyIfNeeded(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
