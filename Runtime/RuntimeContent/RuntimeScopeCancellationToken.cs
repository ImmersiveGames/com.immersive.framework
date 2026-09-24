using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable scoped transition token captured when a runtime materialization request is created.
    /// It is not a System.Threading cancellation token and does not run callbacks. Future materializers must validate it against RuntimeContentRuntime before and after side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8H scoped runtime cancellation token for stale-transition validation; no callbacks, threads or async scheduling.")]
    public readonly struct RuntimeScopeCancellationToken : IEquatable<RuntimeScopeCancellationToken>
    {
        public RuntimeScopeCancellationToken(
            RuntimeContentOwner owner,
            int version,
            RuntimeScopeTransitionState state,
            string source,
            string reason)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime scope cancellation token owner must be valid.", nameof(owner));
            }

            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, "Runtime scope cancellation token version must be positive.");
            }

            if (!Enum.IsDefined(typeof(RuntimeScopeTransitionState), state)
                || state == RuntimeScopeTransitionState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Runtime scope transition state must be explicit.");
            }

            Owner = owner;
            Version = version;
            State = state;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeContentOwner Owner { get; }

        public RuntimeContentScope Scope => Owner.Scope;

        public int Version { get; }

        public RuntimeScopeTransitionState State { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Owner.IsValid && Version > 0 && State != RuntimeScopeTransitionState.Unknown;

        public bool IsCancellationRequested => State is RuntimeScopeTransitionState.CancellationRequested or RuntimeScopeTransitionState.Removed;

        public bool AllowsMaterialization => IsValid
            && State == RuntimeScopeTransitionState.Active
            && !IsCancellationRequested;

        public bool Equals(RuntimeScopeCancellationToken other)
        {
            return Owner.Equals(other.Owner)
                && Version == other.Version
                && State == other.State
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeScopeCancellationToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Owner.GetHashCode();
                hashCode = hashCode * 397 ^ Version;
                hashCode = hashCode * 397 ^ (int)State;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
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
            return $"owner='{Owner.StableText}' scope='{Scope}' version='{Version}' state='{State}' cancellationRequested='{IsCancellationRequested}' source='{sourceText}' reason='{reasonText}'";
        }

        public static bool operator ==(RuntimeScopeCancellationToken left, RuntimeScopeCancellationToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeScopeCancellationToken left, RuntimeScopeCancellationToken right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
