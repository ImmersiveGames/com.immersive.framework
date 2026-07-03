using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F45 lifecycle-local readiness evidence projection; caller owns readiness policy.")]
    internal readonly struct FrameworkLifecycleReadinessEvidence : IEquatable<FrameworkLifecycleReadinessEvidence>
    {
        public FrameworkLifecycleReadinessEvidence(
            string status,
            string reason,
            int issueCount,
            bool blockedByContent,
            string source,
            string requestReason)
        {
            Status = status.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
            IssueCount = Math.Max(0, issueCount);
            BlockedByContent = blockedByContent;
            Source = source.NormalizeText();
            RequestReason = requestReason.NormalizeText();
        }

        public string Status { get; }

        public string Reason { get; }

        public int IssueCount { get; }

        public bool BlockedByContent { get; }

        public string Source { get; }

        public string RequestReason { get; }

        public string DiagnosticText => FrameworkLifecycleReadinessEvidenceProjection.BuildDiagnosticString(this);

        public bool Equals(FrameworkLifecycleReadinessEvidence other)
        {
            return string.Equals(Status, other.Status, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && IssueCount == other.IssueCount
                && BlockedByContent == other.BlockedByContent
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(RequestReason, other.RequestReason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkLifecycleReadinessEvidence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(Status ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ BlockedByContent.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(RequestReason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return DiagnosticText;
        }
    }
}
