using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 lifecycle-local stage evidence projection; caller owns domain status semantics.")]
    internal readonly struct FrameworkLifecycleOperationStageEvidence : IEquatable<FrameworkLifecycleOperationStageEvidence>
    {
        public FrameworkLifecycleOperationStageEvidence(
            FrameworkLifecycleOperationStage stage,
            string statusText,
            string source,
            string reason,
            int issueCount,
            int blockingIssueCount,
            bool sideEffectsApplied,
            bool failed,
            bool skipped,
            string message,
            string originalEvidenceText = "")
        {
            if (!Enum.IsDefined(typeof(FrameworkLifecycleOperationStage), stage) || stage == FrameworkLifecycleOperationStage.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "Lifecycle operation stage evidence requires an explicit lifecycle-local stage.");
            }

            Stage = stage;
            StatusText = statusText.NormalizeTextOrFallback("Unknown");
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            IssueCount = Math.Max(0, issueCount);
            BlockingIssueCount = Math.Max(0, blockingIssueCount);
            SideEffectsApplied = sideEffectsApplied;
            Failed = failed;
            Skipped = skipped;
            Message = message.NormalizeText();
            OriginalEvidenceText = originalEvidenceText.NormalizeText();
        }

        public FrameworkLifecycleOperationStage Stage { get; }

        public string StageText => Stage.ToString();

        public string StatusText { get; }

        public string Source { get; }

        public string Reason { get; }

        public int IssueCount { get; }

        public int BlockingIssueCount { get; }

        public int NonBlockingIssueCount => Math.Max(0, IssueCount - BlockingIssueCount);

        public bool SideEffectsApplied { get; }

        public bool Failed { get; }

        public bool Skipped { get; }

        public string Message { get; }

        public string OriginalEvidenceText { get; }

        public bool Equals(FrameworkLifecycleOperationStageEvidence other)
        {
            return Stage == other.Stage
                && string.Equals(StatusText, other.StatusText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && IssueCount == other.IssueCount
                && BlockingIssueCount == other.BlockingIssueCount
                && SideEffectsApplied == other.SideEffectsApplied
                && Failed == other.Failed
                && Skipped == other.Skipped
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && string.Equals(OriginalEvidenceText, other.OriginalEvidenceText, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkLifecycleOperationStageEvidence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Stage;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(StatusText ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ BlockingIssueCount;
                hashCode = hashCode * 397 ^ SideEffectsApplied.GetHashCode();
                hashCode = hashCode * 397 ^ Failed.GetHashCode();
                hashCode = hashCode * 397 ^ Skipped.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(OriginalEvidenceText ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return FrameworkLifecycleOperationDiagnostics.BuildStageDiagnosticString(this);
        }
    }
}
