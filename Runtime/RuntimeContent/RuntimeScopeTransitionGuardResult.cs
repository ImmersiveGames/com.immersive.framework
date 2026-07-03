using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Internal. Immutable diagnostic result for runtime scope transition guard decisions.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F8H internal transition guard result for scoped materialization/cancellation diagnostics.")]
    internal readonly struct RuntimeScopeTransitionGuardResult
    {
        public RuntimeScopeTransitionGuardResult(
            RuntimeContentOwner owner,
            RuntimeScopeTransitionGuardStatus status,
            RuntimeScopeCancellationToken token,
            string source,
            string reason,
            string message)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime scope transition guard result owner must be valid.", nameof(owner));
            }

            if (!Enum.IsDefined(typeof(RuntimeScopeTransitionGuardStatus), status)
                || status == RuntimeScopeTransitionGuardStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Runtime scope transition guard status must be explicit.");
            }

            Owner = owner;
            Status = status;
            Token = token;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public RuntimeContentOwner Owner { get; }

        public RuntimeContentScope Scope => Owner.Scope;

        public RuntimeScopeTransitionGuardStatus Status { get; }

        public RuntimeScopeCancellationToken Token { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasToken => Token.IsValid;

        public bool Applied => Status is RuntimeScopeTransitionGuardStatus.ScopeOpened or RuntimeScopeTransitionGuardStatus.CancellationRequested or RuntimeScopeTransitionGuardStatus.ScopeRemoved;

        public bool Allowed => Status == RuntimeScopeTransitionGuardStatus.MaterializationAllowed;

        public bool Rejected => Status is RuntimeScopeTransitionGuardStatus.RejectedMissingScope or RuntimeScopeTransitionGuardStatus.RejectedScopeCancelling or RuntimeScopeTransitionGuardStatus.RejectedScopeRemoved or RuntimeScopeTransitionGuardStatus.RejectedStaleToken or RuntimeScopeTransitionGuardStatus.RejectedMismatchedOwner;

        public RuntimeMaterializationStatus ToMaterializationStatus()
        {
            switch (Status)
            {
                case RuntimeScopeTransitionGuardStatus.RejectedMissingScope:
                    return RuntimeMaterializationStatus.FailedMissingRoot;
                case RuntimeScopeTransitionGuardStatus.RejectedScopeCancelling:
                    return RuntimeMaterializationStatus.RejectedScopeCancellation;
                case RuntimeScopeTransitionGuardStatus.RejectedScopeRemoved:
                    return RuntimeMaterializationStatus.RejectedScopeTransition;
                case RuntimeScopeTransitionGuardStatus.RejectedStaleToken:
                    return RuntimeMaterializationStatus.RejectedStaleScope;
                case RuntimeScopeTransitionGuardStatus.RejectedMismatchedOwner:
                    return RuntimeMaterializationStatus.RejectedMismatchedIdentity;
                default:
                    return RuntimeMaterializationStatus.FailedInvalidRequest;
            }
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string tokenText = HasToken ? Token.ToDiagnosticString() : "<none>";
            return $"owner='{Owner.StableText}' scope='{Scope}' status='{Status}' token={tokenText} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static RuntimeScopeTransitionGuardResult ScopeOpened(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.ScopeOpened,
                token,
                source,
                reason,
                "Runtime scope transition guard opened the scope.");
        }

        public static RuntimeScopeTransitionGuardResult ScopeAlreadyActive(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.ScopeAlreadyActive,
                token,
                source,
                reason,
                "Runtime scope transition guard was already active for this scope.");
        }

        public static RuntimeScopeTransitionGuardResult CancellationRequested(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.CancellationRequested,
                token,
                source,
                reason,
                "Runtime scope cancellation requested.");
        }

        public static RuntimeScopeTransitionGuardResult CancellationAlreadyRequested(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.CancellationAlreadyRequested,
                token,
                source,
                reason,
                "Runtime scope cancellation was already requested.");
        }

        public static RuntimeScopeTransitionGuardResult ScopeRemoved(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.ScopeRemoved,
                token,
                source,
                reason,
                "Runtime scope transition guard marked the scope as removed.");
        }

        public static RuntimeScopeTransitionGuardResult MaterializationAllowed(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.MaterializationAllowed,
                token,
                source,
                reason,
                "Runtime materialization is allowed for the current scope token.");
        }

        public static RuntimeScopeTransitionGuardResult RejectedMissingScope(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.RejectedMissingScope,
                default(RuntimeScopeCancellationToken),
                source,
                reason,
                "Runtime scope is missing from the transition guard.");
        }

        public static RuntimeScopeTransitionGuardResult RejectedScopeCancelling(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.RejectedScopeCancelling,
                token,
                source,
                reason,
                "Runtime scope is cancelling; new materialization is rejected.");
        }

        public static RuntimeScopeTransitionGuardResult RejectedScopeRemoved(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.RejectedScopeRemoved,
                token,
                source,
                reason,
                "Runtime scope was removed; new materialization is rejected.");
        }

        public static RuntimeScopeTransitionGuardResult RejectedStaleToken(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken expectedToken,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.RejectedStaleToken,
                expectedToken,
                source,
                reason,
                "Runtime materialization request token is stale for the current scope version.");
        }

        public static RuntimeScopeTransitionGuardResult RejectedMismatchedOwner(
            RuntimeContentOwner owner,
            RuntimeScopeCancellationToken token,
            string source,
            string reason)
        {
            return new RuntimeScopeTransitionGuardResult(
                owner,
                RuntimeScopeTransitionGuardStatus.RejectedMismatchedOwner,
                token,
                source,
                reason,
                "Runtime materialization request token owner does not match the request owner.");
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
