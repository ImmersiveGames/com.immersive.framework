using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Rollback evidence for the ContentAnchor materialization service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "MAT-4 rollback result for ContentAnchor materialization service diagnostics.")]
    internal readonly struct ContentAnchorMaterializationRollbackResult : IEquatable<ContentAnchorMaterializationRollbackResult>
    {
        public ContentAnchorMaterializationRollbackResult(
            bool attempted,
            bool bindingUnbound,
            RuntimeReleaseResult physicalReleaseResult,
            RuntimeReleaseResult logicalReleaseResult,
            string source,
            string reason,
            string message)
        {
            Attempted = attempted;
            BindingUnbound = bindingUnbound;
            PhysicalReleaseResult = physicalReleaseResult;
            LogicalReleaseResult = logicalReleaseResult;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public bool Attempted { get; }

        public bool BindingUnbound { get; }

        public RuntimeReleaseResult PhysicalReleaseResult { get; }

        public RuntimeReleaseResult LogicalReleaseResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasPhysicalReleaseResult => PhysicalReleaseResult.Request.IsValid;

        public bool HasLogicalReleaseResult => LogicalReleaseResult.Request.IsValid;

        public bool Succeeded => Attempted && PhysicalReleaseResult.Succeeded && LogicalReleaseResult.Succeeded;

        public bool Equals(ContentAnchorMaterializationRollbackResult other)
        {
            return Attempted == other.Attempted
                && BindingUnbound == other.BindingUnbound
                && PhysicalReleaseResult.Equals(other.PhysicalReleaseResult)
                && LogicalReleaseResult.Equals(other.LogicalReleaseResult)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorMaterializationRollbackResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Attempted.GetHashCode();
                hashCode = hashCode * 397 ^ BindingUnbound.GetHashCode();
                hashCode = hashCode * 397 ^ PhysicalReleaseResult.GetHashCode();
                hashCode = hashCode * 397 ^ LogicalReleaseResult.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string physicalStatus = HasPhysicalReleaseResult ? PhysicalReleaseResult.Status.ToString() : "None";
            string logicalStatus = HasLogicalReleaseResult ? LogicalReleaseResult.Status.ToString() : "None";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"attempted='{Attempted}' succeeded='{Succeeded}' bindingUnbound='{BindingUnbound}' physicalRelease='{physicalStatus}' logicalRelease='{logicalStatus}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ContentAnchorMaterializationRollbackResult NotAttempted(
            string source,
            string reason,
            string message)
        {
            return new ContentAnchorMaterializationRollbackResult(
                false,
                false,
                default(RuntimeReleaseResult),
                default(RuntimeReleaseResult),
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor materialization rollback was not attempted."
                    : message);
        }
    }
}
