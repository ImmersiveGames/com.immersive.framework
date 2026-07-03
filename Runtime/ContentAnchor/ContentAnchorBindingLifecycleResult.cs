using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic result for logical Content Anchor binding lifecycle cleanup.
    /// It reports registry cleanup only; it does not move transforms, destroy objects, release scenes or touch adapters.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9D Content Anchor binding lifecycle cleanup result; no physical placement or release.")]
    public readonly struct ContentAnchorBindingLifecycleResult : IEquatable<ContentAnchorBindingLifecycleResult>
    {
        public ContentAnchorBindingLifecycleResult(
            ContentAnchorBindingLifecycleStatus status,
            int bindingCountBefore,
            int bindingCountAfter,
            RuntimeContentOwner runtimeOwner,
            RuntimeContentIdentity runtimeIdentity,
            ContentAnchorScope anchorScope,
            FrameworkIdentityKey anchorOwner,
            ContentAnchorKind anchorKind,
            ContentAnchorId anchorId,
            string operation,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ContentAnchorBindingLifecycleStatus), status)
                || status == ContentAnchorBindingLifecycleStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Content Anchor binding lifecycle status must be explicit.");
            }

            if (bindingCountBefore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingCountBefore), bindingCountBefore, "Binding count before cleanup cannot be negative.");
            }

            if (bindingCountAfter < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingCountAfter), bindingCountAfter, "Binding count after cleanup cannot be negative.");
            }

            if (bindingCountAfter > bindingCountBefore)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingCountAfter), bindingCountAfter, "Binding cleanup cannot increase the binding count.");
            }

            Status = status;
            BindingCountBefore = bindingCountBefore;
            BindingCountAfter = bindingCountAfter;
            RuntimeOwner = runtimeOwner;
            RuntimeIdentity = runtimeIdentity;
            AnchorScope = anchorScope;
            AnchorOwner = anchorOwner;
            AnchorKind = anchorKind;
            AnchorId = anchorId;
            Operation = Normalize(operation);
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ContentAnchorBindingLifecycleStatus Status { get; }

        public int BindingCountBefore { get; }

        public int BindingCountAfter { get; }

        public int RemovedCount => BindingCountBefore - BindingCountAfter;

        public RuntimeContentOwner RuntimeOwner { get; }

        public RuntimeContentIdentity RuntimeIdentity { get; }

        public ContentAnchorScope AnchorScope { get; }

        public FrameworkIdentityKey AnchorOwner { get; }

        public ContentAnchorKind AnchorKind { get; }

        public ContentAnchorId AnchorId { get; }

        public string Operation { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Executed => Status != ContentAnchorBindingLifecycleStatus.Unknown;

        public string DiagnosticStatus => Executed ? Status.ToString() : "None";

        public bool Succeeded => Status is ContentAnchorBindingLifecycleStatus.Succeeded or ContentAnchorBindingLifecycleStatus.SucceededNoBindings;

        public bool RemovedAny => RemovedCount > 0;

        public bool HasRuntimeOwner => RuntimeOwner.IsValid;

        public bool HasRuntimeIdentity => RuntimeIdentity.IsValid;

        public bool HasAnchorOwner => AnchorOwner.IsValid;

        public bool HasAnchorId => AnchorId.IsValid;

        public bool Equals(ContentAnchorBindingLifecycleResult other)
        {
            return Status == other.Status
                && BindingCountBefore == other.BindingCountBefore
                && BindingCountAfter == other.BindingCountAfter
                && RuntimeOwner.Equals(other.RuntimeOwner)
                && RuntimeIdentity.Equals(other.RuntimeIdentity)
                && AnchorScope == other.AnchorScope
                && AnchorOwner.Equals(other.AnchorOwner)
                && AnchorKind == other.AnchorKind
                && AnchorId.Equals(other.AnchorId)
                && string.Equals(Operation, other.Operation, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorBindingLifecycleResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ BindingCountBefore;
                hashCode = hashCode * 397 ^ BindingCountAfter;
                hashCode = hashCode * 397 ^ RuntimeOwner.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ (int)AnchorScope;
                hashCode = hashCode * 397 ^ AnchorOwner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)AnchorKind;
                hashCode = hashCode * 397 ^ AnchorId.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Operation ?? string.Empty);
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
            string runtimeOwnerText = HasRuntimeOwner ? RuntimeOwner.StableText : "<none>";
            string runtimeIdentityText = HasRuntimeIdentity ? RuntimeIdentity.StableText : "<none>";
            string anchorOwnerText = HasAnchorOwner ? AnchorOwner.StableText : "<none>";
            string anchorIdText = HasAnchorId ? AnchorId.StableText : "<none>";
            string operationText = Operation.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();

            return $"operation='{operationText}' status='{Status}' removed='{RemovedCount}' before='{BindingCountBefore}' after='{BindingCountAfter}' runtimeOwner='{runtimeOwnerText}' runtimeIdentity='{runtimeIdentityText}' anchorScope='{AnchorScope}' anchorOwner='{anchorOwnerText}' anchorKind='{AnchorKind}' anchorId='{anchorIdText}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ContentAnchorBindingLifecycleResult FromCounts(
            int bindingCountBefore,
            int bindingCountAfter,
            RuntimeContentOwner runtimeOwner,
            RuntimeContentIdentity runtimeIdentity,
            ContentAnchorScope anchorScope,
            FrameworkIdentityKey anchorOwner,
            ContentAnchorKind anchorKind,
            ContentAnchorId anchorId,
            string operation,
            string source,
            string reason,
            string message)
        {
            var status = bindingCountAfter < bindingCountBefore
                ? ContentAnchorBindingLifecycleStatus.Succeeded
                : ContentAnchorBindingLifecycleStatus.SucceededNoBindings;

            return new ContentAnchorBindingLifecycleResult(
                status,
                bindingCountBefore,
                bindingCountAfter,
                runtimeOwner,
                runtimeIdentity,
                anchorScope,
                anchorOwner,
                anchorKind,
                anchorId,
                operation,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor binding lifecycle cleanup completed."
                    : message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
