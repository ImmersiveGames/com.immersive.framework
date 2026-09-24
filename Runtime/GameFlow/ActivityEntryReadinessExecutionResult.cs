using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-READY-04 immutable Activity entry-readiness orchestration evidence.")]
    internal readonly struct ActivityEntryReadinessExecutionResult
    {
        private readonly ActivityReadinessOccurrence _occurrence;

        internal ActivityEntryReadinessExecutionResult(
            ActivityEntryReadinessPolicy policy,
            ActivityEntryReadinessExecutionStatus status,
            ActivityEntryReadinessWaitResult waitResult,
            ActivityFlowStartResult activityFlowResult,
            string reason,
            bool destinationAuthoritative,
            bool revealOccurred = false,
            bool loadingReleased = false,
            bool transitionGateReleased = false,
            bool recoveryGateApplied = false,
            ActivityReadinessOccurrence occurrence = default)
        {
            Policy = policy;
            Status = status;
            WaitResult = waitResult;
            ActivityFlowResult = activityFlowResult;
            Reason = reason ?? string.Empty;
            DestinationAuthoritative = destinationAuthoritative;
            RevealOccurred = revealOccurred;
            LoadingReleased = loadingReleased;
            TransitionGateReleased = transitionGateReleased;
            RecoveryGateApplied = recoveryGateApplied;
            _occurrence = occurrence.IsValid ? occurrence : waitResult.Occurrence;
        }

        internal ActivityEntryReadinessPolicy Policy { get; }
        internal ActivityEntryReadinessExecutionStatus Status { get; }
        internal ActivityEntryReadinessWaitResult WaitResult { get; }
        internal ActivityFlowStartResult ActivityFlowResult { get; }
        internal string Reason { get; }
        internal bool DestinationAuthoritative { get; }
        internal ActivityReadinessOccurrence Occurrence => _occurrence;
        internal ActivityReadinessState ReadinessState => WaitResult.ReadinessState;
        internal int Revision => WaitResult.Revision;
        internal bool RequiresWait => Policy is ActivityEntryReadinessPolicy.WaitVisible
            or ActivityEntryReadinessPolicy.WaitCovered;
        internal bool IsReady => Status == ActivityEntryReadinessExecutionStatus.Ready;
        internal bool IsSuperseded =>
            Status == ActivityEntryReadinessExecutionStatus.Superseded;
        internal bool IsFailure => Status is ActivityEntryReadinessExecutionStatus.Failed
            or ActivityEntryReadinessExecutionStatus.Invalidated
            or ActivityEntryReadinessExecutionStatus.Cancelled;
        internal bool IsConfigurationRejected =>
            Status == ActivityEntryReadinessExecutionStatus.RejectedInvalidConfiguration;

        internal ActivityEntryReadinessExecutionResult WithPresentation(
            bool revealOccurred,
            bool loadingReleased,
            bool transitionGateReleased,
            bool recoveryGateApplied)
        {
            return new ActivityEntryReadinessExecutionResult(
                Policy,
                Status,
                WaitResult,
                ActivityFlowResult,
                Reason,
                DestinationAuthoritative,
                revealOccurred,
                loadingReleased,
                transitionGateReleased,
                recoveryGateApplied,
                Occurrence);
        }

        internal bool RevealOccurred { get; }
        internal bool LoadingReleased { get; }
        internal bool TransitionGateReleased { get; }
        internal bool RecoveryGateApplied { get; }

        internal string ToDiagnosticString()
        {
            return $"policy='{Policy}' status='{Status}' occurrence='{Occurrence.TransitionSequence}' " +
                   $"revision='{Revision}' readiness='{ReadinessState.DiagnosticStatus}' " +
                   $"reason='{Reason}' reveal='{RevealOccurred}' loadingReleased='{LoadingReleased}' " +
                   $"gateReleased='{TransitionGateReleased}' recoveryGate='{RecoveryGateApplied}' " +
                   $"destinationAuthoritative='{DestinationAuthoritative}'.";
        }
    }
}
