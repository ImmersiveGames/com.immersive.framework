using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Internal. Runtime-local guard that tracks scope versions and cancellation state.
    /// It prevents materialization requests from being created or consumed against stale/exiting scopes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F8H runtime-local transition guard and scoped cancellation; no global provider, threading, materialization or release execution.")]
    internal sealed class RuntimeScopeTransitionGuard
    {
        private readonly Dictionary<RuntimeContentOwner, ScopeEntry> _entries = new Dictionary<RuntimeContentOwner, ScopeEntry>();

        public RuntimeScopeTransitionGuardResult OpenScope(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            if (_entries.TryGetValue(owner, out var entry))
            {
                if (entry.State == RuntimeScopeTransitionState.Active)
                {
                    entry.Touch(source, reason);
                    return RuntimeScopeTransitionGuardResult.ScopeAlreadyActive(
                        owner,
                        entry.ToToken(),
                        source,
                        reason);
                }

                entry.Reopen(source, reason);
                return RuntimeScopeTransitionGuardResult.ScopeOpened(
                    owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            entry = ScopeEntry.Open(owner, source, reason);
            _entries.Add(owner, entry);

            return RuntimeScopeTransitionGuardResult.ScopeOpened(
                owner,
                entry.ToToken(),
                source,
                reason);
        }

        public RuntimeScopeTransitionGuardResult RequestCancellation(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            if (!_entries.TryGetValue(owner, out var entry))
            {
                return RuntimeScopeTransitionGuardResult.RejectedMissingScope(owner, source, reason);
            }

            if (entry.State == RuntimeScopeTransitionState.CancellationRequested)
            {
                entry.Touch(source, reason);
                return RuntimeScopeTransitionGuardResult.CancellationAlreadyRequested(
                    owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            if (entry.State == RuntimeScopeTransitionState.Removed)
            {
                return RuntimeScopeTransitionGuardResult.RejectedScopeRemoved(
                    owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            entry.RequestCancellation(source, reason);
            return RuntimeScopeTransitionGuardResult.CancellationRequested(
                owner,
                entry.ToToken(),
                source,
                reason);
        }

        public RuntimeScopeTransitionGuardResult MarkRemoved(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            if (!_entries.TryGetValue(owner, out var entry))
            {
                entry = ScopeEntry.Removed(owner, source, reason);
                _entries.Add(owner, entry);
            }
            else
            {
                entry.MarkRemoved(source, reason);
            }

            return RuntimeScopeTransitionGuardResult.ScopeRemoved(
                owner,
                entry.ToToken(),
                source,
                reason);
        }

        public RuntimeScopeTransitionGuardResult AllowMaterialization(
            RuntimeScopeContext context,
            string source,
            string reason)
        {
            ValidateContext(context);

            if (!_entries.TryGetValue(context.Owner, out var entry))
            {
                return RuntimeScopeTransitionGuardResult.RejectedMissingScope(context.Owner, source, reason);
            }

            if (entry.State == RuntimeScopeTransitionState.Active)
            {
                return RuntimeScopeTransitionGuardResult.MaterializationAllowed(
                    context.Owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            if (entry.State == RuntimeScopeTransitionState.CancellationRequested)
            {
                return RuntimeScopeTransitionGuardResult.RejectedScopeCancelling(
                    context.Owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            return RuntimeScopeTransitionGuardResult.RejectedScopeRemoved(
                context.Owner,
                entry.ToToken(),
                source,
                reason);
        }

        public RuntimeScopeTransitionGuardResult ValidateMaterializationRequest(
            RuntimeMaterializationRequest request,
            string source,
            string reason)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Runtime materialization request must be valid.", nameof(request));
            }

            if (!request.CancellationToken.IsValid)
            {
                return RuntimeScopeTransitionGuardResult.RejectedStaleToken(
                    request.Owner,
                    default(RuntimeScopeCancellationToken),
                    source,
                    reason);
            }

            if (request.CancellationToken.Owner != request.Owner)
            {
                return RuntimeScopeTransitionGuardResult.RejectedMismatchedOwner(
                    request.Owner,
                    request.CancellationToken,
                    source,
                    reason);
            }

            if (!_entries.TryGetValue(request.Owner, out var entry))
            {
                return RuntimeScopeTransitionGuardResult.RejectedMissingScope(request.Owner, source, reason);
            }

            if (entry.Version != request.CancellationToken.Version)
            {
                return RuntimeScopeTransitionGuardResult.RejectedStaleToken(
                    request.Owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            if (entry.State == RuntimeScopeTransitionState.Active)
            {
                return RuntimeScopeTransitionGuardResult.MaterializationAllowed(
                    request.Owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            if (entry.State == RuntimeScopeTransitionState.CancellationRequested)
            {
                return RuntimeScopeTransitionGuardResult.RejectedScopeCancelling(
                    request.Owner,
                    entry.ToToken(),
                    source,
                    reason);
            }

            return RuntimeScopeTransitionGuardResult.RejectedScopeRemoved(
                request.Owner,
                entry.ToToken(),
                source,
                reason);
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateContext(RuntimeScopeContext context)
        {
            if (!context.IsValid)
            {
                throw new ArgumentException("Runtime scope context must be valid.", nameof(context));
            }
        }

        private sealed class ScopeEntry
        {
            private string _source;
            private string _reason;

            private ScopeEntry(
                RuntimeContentOwner owner,
                int version,
                RuntimeScopeTransitionState state,
                string source,
                string reason)
            {
                Owner = owner;
                Version = version;
                State = state;
                _source = Normalize(source);
                _reason = Normalize(reason);
            }

            public RuntimeContentOwner Owner { get; }

            public int Version { get; private set; }

            public RuntimeScopeTransitionState State { get; private set; }

            public static ScopeEntry Open(RuntimeContentOwner owner, string source, string reason)
            {
                return new ScopeEntry(owner, 1, RuntimeScopeTransitionState.Active, source, reason);
            }

            public static ScopeEntry Removed(RuntimeContentOwner owner, string source, string reason)
            {
                return new ScopeEntry(owner, 1, RuntimeScopeTransitionState.Removed, source, reason);
            }

            public void Reopen(string source, string reason)
            {
                Version++;
                State = RuntimeScopeTransitionState.Active;
                Touch(source, reason);
            }

            public void RequestCancellation(string source, string reason)
            {
                Version++;
                State = RuntimeScopeTransitionState.CancellationRequested;
                Touch(source, reason);
            }

            public void MarkRemoved(string source, string reason)
            {
                Version++;
                State = RuntimeScopeTransitionState.Removed;
                Touch(source, reason);
            }

            public void Touch(string source, string reason)
            {
                _source = Normalize(source);
                _reason = Normalize(reason);
            }

            public RuntimeScopeCancellationToken ToToken()
            {
                return new RuntimeScopeCancellationToken(
                    Owner,
                    Version,
                    State,
                    _source,
                    _reason);
            }

            private static string Normalize(string value)
            {
                return value.NormalizeText();
            }
        }
    }
}
