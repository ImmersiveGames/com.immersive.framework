using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for the generic Participant executor primitives.
    /// It validates Common/Participants mechanics only and does not use CycleReset, ObjectReset, Snapshot, ActivityContentExecution, LocalContribution, Route, Activity, Pause, RuntimeContent, ContentAnchor or InputMode domains.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Synthetic Participant executor smoke; Common/Participants only.")]
    internal static class ParticipantExecutorSyntheticSmokeRunner
    {
        internal const string SmokeName = "Participant Executor Synthetic Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ParticipantExecutorSyntheticSmokeRunner));

            bool successOrderingPassed = ValidateSuccessfulOrdering(logger, normalizedSource);
            bool optionalFailurePassed = ValidateOptionalFailure(logger, normalizedSource);
            bool requiredFailurePassed = ValidateRequiredFailure(logger, normalizedSource);
            bool requiredExceptionPassed = ValidateRequiredException(logger, normalizedSource);
            bool optionalExceptionPassed = ValidateOptionalException(logger, normalizedSource);
            bool invalidResultPassed = ValidateInvalidResult(logger, normalizedSource);
            bool emptyListPassed = ValidateEmptyList(logger, normalizedSource);
            bool invalidInputPassed = ValidateInvalidInputs(logger, normalizedSource);

            return Task.FromResult(successOrderingPassed
                && optionalFailurePassed
                && requiredFailurePassed
                && requiredExceptionPassed
                && optionalExceptionPassed
                && invalidResultPassed
                && emptyListPassed
                && invalidInputPassed);
        }

        private static bool ValidateSuccessfulOrdering(FrameworkLogger logger, string source)
        {
            var entries = new[]
            {
                CreateEntry("optional-second", ParticipantRequiredness.Optional, 20, 1, SyntheticParticipantOutcome.Success, 0),
                CreateEntry("required-first", ParticipantRequiredness.Required, 10, 0, SyntheticParticipantOutcome.Success, 0)
            };

            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.success-ordering",
                entries,
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: true, Failed: false, ParticipantCount: 2, SuccessfulCount: 2, IssueCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 0 }
                && result.ExecutionOrder.Count == 2
                && string.Equals(result.ExecutionOrder[0], "required-first", StringComparison.Ordinal)
                && string.Equals(result.ExecutionOrder[1], "optional-second", StringComparison.Ordinal);

            LogStep(logger, "success-ordering", result, passed);
            return passed;
        }

        private static bool ValidateOptionalFailure(FrameworkLogger logger, string source)
        {
            var entries = new[]
            {
                CreateEntry("optional-failure", ParticipantRequiredness.Optional, 0, 0, SyntheticParticipantOutcome.OptionalFailure, 2)
            };

            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.optional-failure",
                entries,
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: false, Failed: true, ParticipantCount: 1, SuccessfulCount: 0, OptionalFailureCount: 1, BlockingFailureCount: 0, IssueCount: 2, BlockingIssueCount: 0, NonBlockingIssueCount: 2 };

            LogStep(logger, "optional-failure", result, passed);
            return passed;
        }

        private static bool ValidateRequiredFailure(FrameworkLogger logger, string source)
        {
            var entries = new[]
            {
                CreateEntry("required-failure", ParticipantRequiredness.Required, 0, 0, SyntheticParticipantOutcome.BlockingFailure, 3)
            };

            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.required-failure",
                entries,
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: false, Failed: true, ParticipantCount: 1, SuccessfulCount: 0, BlockingFailureCount: 1, OptionalFailureCount: 0, IssueCount: 3, BlockingIssueCount: 3, NonBlockingIssueCount: 0 };

            LogStep(logger, "required-failure", result, passed);
            return passed;
        }

        private static bool ValidateRequiredException(FrameworkLogger logger, string source)
        {
            var entries = new[]
            {
                CreateEntry("required-exception", ParticipantRequiredness.Required, 0, 0, SyntheticParticipantOutcome.Throws, 0)
            };

            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.required-exception",
                entries,
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: false, Failed: true, ExceptionCount: 1, BlockingIssueCount: 1, NonBlockingIssueCount: 0 };

            LogStep(logger, "required-exception", result, passed);
            return passed;
        }

        private static bool ValidateOptionalException(FrameworkLogger logger, string source)
        {
            var entries = new[]
            {
                CreateEntry("optional-exception", ParticipantRequiredness.Optional, 0, 0, SyntheticParticipantOutcome.Throws, 0)
            };

            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.optional-exception",
                entries,
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: false, Failed: true, ExceptionCount: 1, BlockingIssueCount: 0, NonBlockingIssueCount: 1 };

            LogStep(logger, "optional-exception", result, passed);
            return passed;
        }

        private static bool ValidateInvalidResult(FrameworkLogger logger, string source)
        {
            var entries = new[]
            {
                CreateEntry("invalid-result", ParticipantRequiredness.Required, 0, 0, SyntheticParticipantOutcome.InvalidResult, 1)
            };

            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.invalid-result",
                entries,
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: false, Failed: true, InvalidResultCount: 1, BlockingIssueCount: 1, NonBlockingIssueCount: 0 };

            LogStep(logger, "invalid-result", result, passed);
            return passed;
        }

        private static bool ValidateEmptyList(FrameworkLogger logger, string source)
        {
            var result = ParticipantExecutor.Execute(
                source,
                "qa.participant.empty",
                Array.Empty<ParticipantExecutionEntry<SyntheticParticipant>>(),
                Invoke,
                IsResultValid,
                IsResultSuccessful,
                IsResultBlocking,
                GetIssueCount,
                CreateExceptionIssue,
                CreateInvalidResultIssue);

            bool passed = result is { Succeeded: true, Failed: false, ParticipantCount: 0, SuccessfulCount: 0, IssueCount: 0, BlockingIssueCount: 0, NonBlockingIssueCount: 0 };

            LogStep(logger, "empty-list", result, passed);
            return passed;
        }

        private static bool ValidateInvalidInputs(FrameworkLogger logger, string source)
        {
            bool nullParticipantPassed = false;
            bool nullEntriesPassed = false;
            bool invalidEntryPassed = false;

            try
            {
                _ = new ParticipantExecutionEntry<SyntheticParticipant>(null, ParticipantRequiredness.Required, 0, 0, "null-participant");
            }
            catch (ArgumentNullException)
            {
                nullParticipantPassed = true;
            }

            try
            {
                _ = ParticipantExecutor.Execute<SyntheticParticipant, SyntheticParticipantResult>(
                    source,
                    "qa.participant.null-entries",
                    null,
                    Invoke,
                    IsResultValid,
                    IsResultSuccessful,
                    IsResultBlocking,
                    GetIssueCount,
                    CreateExceptionIssue,
                    CreateInvalidResultIssue);
            }
            catch (ArgumentNullException)
            {
                nullEntriesPassed = true;
            }

            try
            {
                _ = ParticipantExecutor.Execute(
                    source,
                    "qa.participant.invalid-entry",
                    new ParticipantExecutionEntry<SyntheticParticipant>[]
                    {
                        default(ParticipantExecutionEntry<SyntheticParticipant>)
                    },
                    Invoke,
                    IsResultValid,
                    IsResultSuccessful,
                    IsResultBlocking,
                    GetIssueCount,
                    CreateExceptionIssue,
                    CreateInvalidResultIssue);
            }
            catch (ArgumentException)
            {
                invalidEntryPassed = true;
            }

            bool passed = nullParticipantPassed && nullEntriesPassed && invalidEntryPassed;
            if (passed)
            {
                LogPass(logger, "invalid-inputs", source, "Participant executor rejected invalid inputs.");
                return true;
            }

            LogFail(logger, "invalid-inputs", source, "Participant executor did not reject invalid inputs.");
            return false;
        }

        private static ParticipantExecutionEntry<SyntheticParticipant> CreateEntry(
            string label,
            ParticipantRequiredness requiredness,
            int order,
            int sourceIndex,
            SyntheticParticipantOutcome outcome,
            int issueCount)
        {
            return new ParticipantExecutionEntry<SyntheticParticipant>(
                new SyntheticParticipant(label, outcome, issueCount),
                requiredness,
                order,
                sourceIndex,
                label);
        }

        private static SyntheticParticipantResult Invoke(SyntheticParticipant participant)
        {
            return participant.Execute();
        }

        private static bool IsResultValid(ParticipantExecutionEntry<SyntheticParticipant> entry, SyntheticParticipantResult result)
        {
            return result.IsValid;
        }

        private static bool IsResultSuccessful(ParticipantExecutionEntry<SyntheticParticipant> entry, SyntheticParticipantResult result)
        {
            return result.Succeeded;
        }

        private static bool IsResultBlocking(ParticipantExecutionEntry<SyntheticParticipant> entry, SyntheticParticipantResult result)
        {
            return result.Blocking;
        }

        private static int GetIssueCount(ParticipantExecutionEntry<SyntheticParticipant> entry, SyntheticParticipantResult result)
        {
            return result.IssueCount;
        }

        private static ParticipantExecutionIssue CreateExceptionIssue(ParticipantExecutionEntry<SyntheticParticipant> entry, Exception exception)
        {
            return new ParticipantExecutionIssue(
                entry.IsRequired ? ParticipantExecutionIssueSeverity.Error : ParticipantExecutionIssueSeverity.Warning,
                entry.Label,
                "qa.participant.exception",
                "qa.participant.exception",
                exception.Message,
                1);
        }

        private static ParticipantExecutionIssue CreateInvalidResultIssue(ParticipantExecutionEntry<SyntheticParticipant> entry, SyntheticParticipantResult result)
        {
            return new ParticipantExecutionIssue(
                ParticipantExecutionIssueSeverity.Error,
                entry.Label,
                "qa.participant.invalid-result",
                "qa.participant.invalid-result",
                "Participant returned an invalid result.",
                Math.Max(1, result.IssueCount));
        }

        private static void LogStep(FrameworkLogger logger, string step, ParticipantExecutionResult result, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("participants", result.ParticipantCount),
                LogFields.Field("succeeded", result.SuccessfulCount),
                LogFields.Field("failed", result.FailedCount),
                LogFields.Field("blockingFailures", result.BlockingFailureCount),
                LogFields.Field("optionalFailures", result.OptionalFailureCount),
                LogFields.Field("invalidResults", result.InvalidResultCount),
                LogFields.Field("exceptions", result.ExceptionCount),
                LogFields.Field("issues", result.IssueCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Participant Executor Synthetic Smoke step completed.", fields);
                logger.Debug("QA Participant Executor Synthetic Smoke details.", LogFields.Field("details", result.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Participant Executor Synthetic Smoke step failed.", fields);
            logger.Debug("QA Participant Executor Synthetic Smoke failure details.", LogFields.Field("details", result.ToDiagnosticString()));
        }

        private static void LogPass(FrameworkLogger logger, string step, string source, string message)
        {
            logger.Info(
                "QA Participant Executor Synthetic Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("source", source),
                    LogFields.Field("message", message)));
        }

        private static void LogFail(FrameworkLogger logger, string step, string source, string message)
        {
            logger.Warning(
                "QA Participant Executor Synthetic Smoke step failed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("source", source),
                    LogFields.Field("message", message)));
        }

        private sealed class SyntheticParticipant
        {
            internal SyntheticParticipant(string label, SyntheticParticipantOutcome outcome, int issueCount)
            {
                Label = label.NormalizeText();
                Outcome = outcome;
                IssueCount = issueCount;
            }

            internal string Label { get; }

            internal SyntheticParticipantOutcome Outcome { get; }

            internal int IssueCount { get; }

            internal SyntheticParticipantResult Execute()
            {
                return Outcome switch
                {
                    SyntheticParticipantOutcome.Success => SyntheticParticipantResult.Success(IssueCount),
                    SyntheticParticipantOutcome.OptionalFailure => SyntheticParticipantResult.OptionalFailure(IssueCount),
                    SyntheticParticipantOutcome.BlockingFailure => SyntheticParticipantResult.BlockingFailure(IssueCount),
                    SyntheticParticipantOutcome.InvalidResult => SyntheticParticipantResult.Invalid(IssueCount),
                    SyntheticParticipantOutcome.Throws => throw new InvalidOperationException($"Synthetic participant '{Label}' threw intentionally."),
                    _ => SyntheticParticipantResult.Invalid(IssueCount)
                };
            }
        }

        private enum SyntheticParticipantOutcome
        {
            Success = 0,
            OptionalFailure = 1,
            BlockingFailure = 2,
            Throws = 3,
            InvalidResult = 4
        }

        private readonly struct SyntheticParticipantResult
        {
            private SyntheticParticipantResult(bool isValid, bool succeeded, bool blocking, int issueCount)
            {
                IsValid = isValid;
                Succeeded = succeeded;
                Blocking = blocking;
                IssueCount = issueCount;
            }

            internal bool IsValid { get; }

            internal bool Succeeded { get; }

            internal bool Blocking { get; }

            internal int IssueCount { get; }

            internal static SyntheticParticipantResult Success(int issueCount)
            {
                return new SyntheticParticipantResult(true, true, false, Math.Max(0, issueCount));
            }

            internal static SyntheticParticipantResult OptionalFailure(int issueCount)
            {
                return new SyntheticParticipantResult(true, false, false, Math.Max(1, issueCount));
            }

            internal static SyntheticParticipantResult BlockingFailure(int issueCount)
            {
                return new SyntheticParticipantResult(true, false, true, Math.Max(1, issueCount));
            }

            internal static SyntheticParticipantResult Invalid(int issueCount)
            {
                return new SyntheticParticipantResult(false, false, true, Math.Max(1, issueCount));
            }
        }
    }
}
#endif
