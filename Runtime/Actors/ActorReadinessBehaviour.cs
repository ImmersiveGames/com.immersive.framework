using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Unity-facing adapter for <see cref="ActorReadiness"/>.
    /// This component exposes Actor readiness to authored GameObjects without owning PlayerEntry,
    /// PlayerView, ControlBinding, PlayerInputManager or gameplay movement.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Readiness")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49C Unity adapter for Actor readiness.")]
    public sealed class ActorReadinessBehaviour : MonoBehaviour, IActorReadiness
    {
        [Header("Initial State")]
        [SerializeField] private ActorReadinessState initialState = ActorReadinessState.NotReady;
        [SerializeField] private string initialReason = string.Empty;
        [SerializeField] private bool applyInitialStateOnAwake = true;

        private readonly ActorReadiness _readiness = new ActorReadiness();

        public ActorReadinessState State => _readiness.State;

        public bool IsReadyForView => _readiness.IsReadyForView;

        public bool IsReadyForControl => _readiness.IsReadyForControl;

        public bool IsFailed => _readiness.IsFailed;

        public bool IsReleased => _readiness.IsReleased;

        public string Reason => _readiness.Reason;

        private void Awake()
        {
            if (applyInitialStateOnAwake)
            {
                ApplyConfiguredInitialState();
            }
        }

        public ActorReadinessSnapshot CreateSnapshot()
        {
            return _readiness.CreateSnapshot();
        }

        public void ApplyConfiguredInitialState()
        {
            if (_readiness.IsReleased)
            {
                _readiness.BeginNewCycle("actor-readiness.behaviour.apply-configured-initial-state");
            }

            ApplyState(initialState, initialReason);
        }

        public void SetReadiness(bool readyForView, bool readyForControl, string reason = null)
        {
            _readiness.SetReadiness(readyForView, readyForControl, reason);
        }

        public void MarkReadyForView(string reason = null)
        {
            _readiness.MarkReadyForView(reason);
        }

        public void MarkReadyForControl(string reason = null)
        {
            _readiness.MarkReadyForControl(reason);
        }

        public void ClearReadiness(string reason = null)
        {
            _readiness.ClearReadiness(reason);
        }

        public void MarkFailed(string reason)
        {
            _readiness.MarkFailed(reason);
        }

        public void Release(string reason = null)
        {
            _readiness.Release(reason);
        }

        public void BeginNewCycle(string reason = null)
        {
            _readiness.BeginNewCycle(reason);
        }

        [ContextMenu("Actor Readiness/Mark Ready For View")]
        private void ContextMarkReadyForView()
        {
            MarkReadyForView("actor-readiness.behaviour.context.ready-for-view");
        }

        [ContextMenu("Actor Readiness/Mark Ready For Control")]
        private void ContextMarkReadyForControl()
        {
            MarkReadyForControl("actor-readiness.behaviour.context.ready-for-control");
        }

        [ContextMenu("Actor Readiness/Clear Readiness")]
        private void ContextClearReadiness()
        {
            ClearReadiness("actor-readiness.behaviour.context.clear");
        }

        [ContextMenu("Actor Readiness/Release")]
        private void ContextRelease()
        {
            Release("actor-readiness.behaviour.context.release");
        }

        [ContextMenu("Actor Readiness/Begin New Cycle")]
        private void ContextBeginNewCycle()
        {
            BeginNewCycle("actor-readiness.behaviour.context.new-cycle");
        }

        private void ApplyState(ActorReadinessState state, string reason)
        {
            switch (state)
            {
                case ActorReadinessState.NotReady:
                    _readiness.ClearReadiness(reason);
                    break;
                case ActorReadinessState.ReadyForView:
                    _readiness.MarkReadyForView(reason);
                    break;
                case ActorReadinessState.ReadyForControl:
                    _readiness.MarkReadyForControl(reason);
                    break;
                case ActorReadinessState.Failed:
                    _readiness.MarkFailed(reason);
                    break;
                case ActorReadinessState.Released:
                    _readiness.Release(reason);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(state), state, "Actor readiness state is not defined.");
            }
        }
    }
}
