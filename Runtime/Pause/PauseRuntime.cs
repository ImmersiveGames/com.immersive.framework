using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. Minimal runtime owner for the logical Pause state.
    /// It applies PauseRequest values to PauseState, refreshes a PauseSnapshot and exposes the derived
    /// Pause capability Gate blockers. It does not read input, show UI, own overlays, change Time.timeScale,
    /// mutate Route/Activity lifecycle, pause components or register blockers in a global Gate runtime.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F27D Pause runtime exposes capability Gate blockers; no input, UI, component or Time.timeScale ownership.")]
    internal sealed class PauseRuntime
    {
        private PauseState _state;
        private PauseSnapshot _snapshot;
        private GateSnapshot _gateSnapshot;

        internal PauseRuntime()
        {
            _state = PauseState.Running;
            _snapshot = PauseSnapshot.FromState(
                _state,
                nameof(PauseRuntime),
                "pause-runtime-initialized",
                new[] { "Pause runtime initialized in Running state." });
            _gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                _snapshot,
                nameof(PauseRuntime),
                "pause-runtime-initialized");
        }

        internal PauseState State => _state;

        internal bool IsPaused => _state == PauseState.Paused;

        internal bool IsRunning => _state == PauseState.Running;

        internal PauseSnapshot Snapshot => _snapshot;

        internal GateSnapshot GateSnapshot => _gateSnapshot;

        internal PauseResult Request(PauseRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Pause runtime requires a valid Pause request.", nameof(request));
            }

            var previousState = _state;
            var targetState = PauseRequest.ResolveTargetState(request.Kind, previousState);
            PauseResult result;

            if (targetState == previousState)
            {
                result = PauseResult.IgnoredNoChangeResult(
                    request,
                    previousState,
                    $"Pause request ignored because state is already {previousState}.");
            }
            else
            {
                result = PauseResult.AppliedResult(
                    request,
                    previousState,
                    targetState,
                    $"Pause request applied. previousState='{previousState}' currentState='{targetState}'.");
            }

            ApplyResult(result);
            return result;
        }

        internal GateEvaluationResult EvaluateGate(
            GateScope scope,
            GateDomain domain,
            string subject,
            string source,
            string reason)
        {
            return _gateSnapshot.Evaluate(
                scope,
                domain,
                default,
                Normalize(subject, "PauseRuntimeGateEvaluation"),
                Normalize(source, nameof(PauseRuntime)),
                Normalize(reason, "pause-runtime.gate.evaluate"),
                PauseGateBlockerPolicy.PolicySource);
        }

        private void ApplyResult(PauseResult result)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException("Pause runtime cannot apply an invalid Pause result.", nameof(result));
            }

            _state = result.CurrentState;
            _snapshot = PauseSnapshot.FromResult(
                result,
                new[]
                {
                    result.Applied
                        ? "Pause runtime request applied."
                        : result.IgnoredNoChange
                            ? "Pause runtime request ignored because state did not change."
                            : "Pause runtime request completed without applying a state change."
                });
            _gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                _snapshot,
                result.Request.Source,
                result.Request.Reason);
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }
    }
}
