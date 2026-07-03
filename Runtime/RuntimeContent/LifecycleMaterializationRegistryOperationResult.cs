using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable result for one lifecycle-owned materialization registry operation.
    /// It reports registry evidence only; it does not execute physical release, logical RuntimeContent release or ContentAnchor cleanup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-N lifecycle-owned materialization registry operation result; no physical or lifecycle side effects.")]
    public readonly struct LifecycleMaterializationRegistryOperationResult : IEquatable<LifecycleMaterializationRegistryOperationResult>
    {
        public LifecycleMaterializationRegistryOperationResult(
            RuntimeContentIdentity identity,
            LifecycleMaterializationRegistryOperationStatus status,
            LifecycleMaterializedEntry entry,
            string source,
            string reason,
            string message)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Lifecycle materialization registry result identity must be valid.", nameof(identity));
            }

            if (!Enum.IsDefined(typeof(LifecycleMaterializationRegistryOperationStatus), status)
                || status == LifecycleMaterializationRegistryOperationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Lifecycle materialization registry result status must be explicit.");
            }

            if (entry != null && entry.Identity != identity)
            {
                throw new ArgumentException("Lifecycle materialization registry result entry identity must match the result identity.", nameof(entry));
            }

            Identity = identity;
            Status = status;
            Entry = entry;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public RuntimeContentIdentity Identity { get; }

        public RuntimeContentOwner Owner => Identity.Owner;

        public RuntimeContentScope Scope => Identity.Scope;

        public RuntimeContentId ContentId => Identity.ContentId;

        public LifecycleMaterializationRegistryOperationStatus Status { get; }

        public LifecycleMaterializedEntry Entry { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasEntry => Entry != null;

        public bool Succeeded => Status is LifecycleMaterializationRegistryOperationStatus.SucceededRegistered
            or LifecycleMaterializationRegistryOperationStatus.SucceededAlreadyRegistered
            or LifecycleMaterializationRegistryOperationStatus.SucceededReleaseRequested
            or LifecycleMaterializationRegistryOperationStatus.SucceededReleased
            or LifecycleMaterializationRegistryOperationStatus.SucceededReleaseFailedRecorded;

        public bool Failed => !Succeeded;

        public bool Equals(LifecycleMaterializationRegistryOperationResult other)
        {
            return Identity.Equals(other.Identity)
                && Status == other.Status
                && ReferenceEquals(Entry, other.Entry)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LifecycleMaterializationRegistryOperationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Identity.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ (Entry != null ? Entry.GetHashCode() : 0);
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
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string entryText = HasEntry ? Entry.ToDiagnosticString() : "<none>";
            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' status='{Status}' succeeded='{Succeeded}' entry={entryText} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static LifecycleMaterializationRegistryOperationResult Success(
            RuntimeContentIdentity identity,
            LifecycleMaterializationRegistryOperationStatus status,
            LifecycleMaterializedEntry entry,
            string source,
            string reason,
            string message)
        {
            if (status is LifecycleMaterializationRegistryOperationStatus.RejectedDuplicateEntry
                or LifecycleMaterializationRegistryOperationStatus.RejectedMissingEntry
                or LifecycleMaterializationRegistryOperationStatus.RejectedMismatchedHandle
                or LifecycleMaterializationRegistryOperationStatus.RejectedInvalidTransition)
            {
                throw new ArgumentException("Use Failure for rejected lifecycle materialization registry results.", nameof(status));
            }

            return new LifecycleMaterializationRegistryOperationResult(
                identity,
                status,
                entry,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Lifecycle materialization registry operation succeeded."
                    : message);
        }

        public static LifecycleMaterializationRegistryOperationResult Failure(
            RuntimeContentIdentity identity,
            LifecycleMaterializationRegistryOperationStatus status,
            LifecycleMaterializedEntry entry,
            string source,
            string reason,
            string message)
        {
            if (status is LifecycleMaterializationRegistryOperationStatus.SucceededRegistered
                or LifecycleMaterializationRegistryOperationStatus.SucceededAlreadyRegistered
                or LifecycleMaterializationRegistryOperationStatus.SucceededReleaseRequested
                or LifecycleMaterializationRegistryOperationStatus.SucceededReleased
                or LifecycleMaterializationRegistryOperationStatus.SucceededReleaseFailedRecorded)
            {
                throw new ArgumentException("Use Success for successful lifecycle materialization registry results.", nameof(status));
            }

            return new LifecycleMaterializationRegistryOperationResult(
                identity,
                status,
                entry,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Lifecycle materialization registry operation failed."
                    : message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
