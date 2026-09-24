using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic snapshot for Actor readiness.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49B passive Actor readiness snapshot.")]
    public readonly struct ActorReadinessSnapshot : IEquatable<ActorReadinessSnapshot>
    {
        private readonly string _reason;

        public ActorReadinessSnapshot(ActorReadinessState state, string reason)
        {
            if (!Enum.IsDefined(typeof(ActorReadinessState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Actor readiness state is not defined.");
            }

            State = state;
            _reason = reason.NormalizeText();
        }

        public ActorReadinessState State { get; }

        public bool IsReadyForView => State == ActorReadinessState.ReadyForView
                                      || State == ActorReadinessState.ReadyForControl;

        public bool IsReadyForControl => State == ActorReadinessState.ReadyForControl;

        public bool IsFailed => State == ActorReadinessState.Failed;

        public bool IsReleased => State == ActorReadinessState.Released;

        public string Reason => _reason;

        public string DiagnosticReason => _reason.ToDiagnosticText();

        public bool Equals(ActorReadinessSnapshot other)
        {
            return State == other.State && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorReadinessSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)State * 397) ^ (_reason != null ? _reason.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return $"state='{State}' reason='{DiagnosticReason}'";
        }

        public static bool operator ==(ActorReadinessSnapshot left, ActorReadinessSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorReadinessSnapshot left, ActorReadinessSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
