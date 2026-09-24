using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F45 lifecycle-local content evidence projection; caller owns domain execution and status semantics.")]
    internal readonly struct FrameworkLifecycleContentEvidence : IEquatable<FrameworkLifecycleContentEvidence>
    {
        public FrameworkLifecycleContentEvidence(
            string status,
            string enterStatus,
            string exitStatus,
            int enterRequestCount,
            int exitRequestCount,
            int participantCount,
            string participantSourceStatus,
            int issueCount,
            int blockingIssueCount,
            bool blocksReadiness,
            int contentHandleCount,
            string source,
            string reason)
        {
            Status = status.NormalizeTextOrFallback("Unknown");
            EnterStatus = enterStatus.NormalizeTextOrFallback("Unknown");
            ExitStatus = exitStatus.NormalizeTextOrFallback("Unknown");
            EnterRequestCount = Math.Max(0, enterRequestCount);
            ExitRequestCount = Math.Max(0, exitRequestCount);
            ParticipantCount = Math.Max(0, participantCount);
            ParticipantSourceStatus = participantSourceStatus.NormalizeTextOrFallback("None");
            IssueCount = Math.Max(0, issueCount);
            BlockingIssueCount = Math.Max(0, blockingIssueCount);
            BlocksReadiness = blocksReadiness;
            ContentHandleCount = Math.Max(0, contentHandleCount);
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public string Status { get; }

        public string EnterStatus { get; }

        public string ExitStatus { get; }

        public int EnterRequestCount { get; }

        public int ExitRequestCount { get; }

        public int ParticipantCount { get; }

        public string ParticipantSourceStatus { get; }

        public int IssueCount { get; }

        public int BlockingIssueCount { get; }

        public bool BlocksReadiness { get; }

        public int ContentHandleCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public string DiagnosticText => FrameworkLifecycleContentEvidenceProjection.BuildDiagnosticString(this);

        public bool Equals(FrameworkLifecycleContentEvidence other)
        {
            return string.Equals(Status, other.Status, StringComparison.Ordinal)
                && string.Equals(EnterStatus, other.EnterStatus, StringComparison.Ordinal)
                && string.Equals(ExitStatus, other.ExitStatus, StringComparison.Ordinal)
                && EnterRequestCount == other.EnterRequestCount
                && ExitRequestCount == other.ExitRequestCount
                && ParticipantCount == other.ParticipantCount
                && string.Equals(ParticipantSourceStatus, other.ParticipantSourceStatus, StringComparison.Ordinal)
                && IssueCount == other.IssueCount
                && BlockingIssueCount == other.BlockingIssueCount
                && BlocksReadiness == other.BlocksReadiness
                && ContentHandleCount == other.ContentHandleCount
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkLifecycleContentEvidence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(Status ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(EnterStatus ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ExitStatus ?? string.Empty);
                hashCode = hashCode * 397 ^ EnterRequestCount;
                hashCode = hashCode * 397 ^ ExitRequestCount;
                hashCode = hashCode * 397 ^ ParticipantCount;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ParticipantSourceStatus ?? string.Empty);
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ BlockingIssueCount;
                hashCode = hashCode * 397 ^ BlocksReadiness.GetHashCode();
                hashCode = hashCode * 397 ^ ContentHandleCount;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return DiagnosticText;
        }
    }
}
