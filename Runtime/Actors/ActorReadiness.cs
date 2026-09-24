using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Pure runtime implementation of <see cref="IActorReadiness"/>.
    /// This class is intentionally not a MonoBehaviour. Unity-facing authoring belongs to a later adapter cut.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49B pure Actor readiness implementation.")]
    public sealed class ActorReadiness : IActorReadiness
    {
        private ActorReadinessState _state;
        private string _reason;

        public ActorReadiness()
            : this(ActorReadinessState.NotReady, string.Empty)
        {
        }

        public ActorReadiness(ActorReadinessState state, string reason)
        {
            ValidateStateReason(state, reason);

            _state = state;
            _reason = reason.NormalizeText();
        }

        public ActorReadinessState State => _state;

        public bool IsReadyForView => _state == ActorReadinessState.ReadyForView
                                      || _state == ActorReadinessState.ReadyForControl;

        public bool IsReadyForControl => _state == ActorReadinessState.ReadyForControl;

        public bool IsFailed => _state == ActorReadinessState.Failed;

        public bool IsReleased => _state == ActorReadinessState.Released;

        public string Reason => _reason;

        public ActorReadinessSnapshot CreateSnapshot()
        {
            return new ActorReadinessSnapshot(_state, _reason);
        }

        public void SetReadiness(bool readyForView, bool readyForControl, string reason = null)
        {
            EnsureCanChangeReadiness();

            if (readyForControl && !readyForView)
            {
                throw new InvalidOperationException("Actor readiness is invalid: ReadyForControl requires ReadyForView.");
            }

            if (readyForControl)
            {
                Apply(ActorReadinessState.ReadyForControl, reason);
                return;
            }

            if (readyForView)
            {
                Apply(ActorReadinessState.ReadyForView, reason);
                return;
            }

            Apply(ActorReadinessState.NotReady, reason);
        }

        public void MarkReadyForView(string reason = null)
        {
            EnsureCanChangeReadiness();
            Apply(ActorReadinessState.ReadyForView, reason);
        }

        public void MarkReadyForControl(string reason = null)
        {
            EnsureCanChangeReadiness();
            Apply(ActorReadinessState.ReadyForControl, reason);
        }

        public void ClearReadiness(string reason = null)
        {
            EnsureCanChangeReadiness();
            Apply(ActorReadinessState.NotReady, reason);
        }

        public void MarkFailed(string reason)
        {
            EnsureCanChangeReadiness();

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Actor readiness failure requires an explicit reason.", nameof(reason));
            }

            Apply(ActorReadinessState.Failed, reason);
        }

        public void Release(string reason = null)
        {
            Apply(ActorReadinessState.Released, reason);
        }

        public void BeginNewCycle(string reason = null)
        {
            Apply(ActorReadinessState.NotReady, reason);
        }

        private void EnsureCanChangeReadiness()
        {
            if (_state == ActorReadinessState.Released)
            {
                throw new InvalidOperationException("Actor readiness was released. Start a new readiness cycle before changing readiness.");
            }
        }

        private void Apply(ActorReadinessState state, string reason)
        {
            ValidateStateReason(state, reason);

            _state = state;
            _reason = reason.NormalizeText();
        }

        private static void ValidateStateReason(ActorReadinessState state, string reason)
        {
            if (!Enum.IsDefined(typeof(ActorReadinessState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Actor readiness state is not defined.");
            }

            if (state == ActorReadinessState.Failed && string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Actor readiness failure requires an explicit reason.", nameof(reason));
            }
        }
    }
}
