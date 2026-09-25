using System;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        /// <summary>
        /// IF-ADR-005 Pause lifecycle cleanup. Called by ActivityFlowRuntime, through the
        /// GameFlowRuntime/RouteLifecycleRuntime chain, immediately before an Activity exit
        /// commits (previousActivity is still the officially active Activity at the call site).
        /// If Pause is Running, this is a no-op. If Pause is Paused, it restores the physical
        /// PlayerInput posture first (the only step with a concrete, non-speculative failure
        /// surface: PauseProductBindingRuntimeContext.TryRestorePhysicalPostureForLifecycleExit),
        /// and only once that succeeds does it force the canonical Pause snapshot to Running
        /// (IPauseProductApplicationPort.TryRestorePauseSnapshot: logical state, TimeScale,
        /// presentation). This ordering avoids a partially-applied precondition: if physical
        /// restoration fails, canonical Pause simply remains Paused and nothing else was
        /// touched. Admission is intentionally bypassed at both steps, since this call is itself
        /// part of the lifecycle operation that admission exists to protect. PauseRuntime remains
        /// the only writer of Running/Paused; this method never mutates Pause state directly.
        /// </summary>
        bool IPauseActivityLifecyclePort.PrepareForActivityExit(
            string source,
            string reason,
            out string diagnostic)
        {
            if (_pauseRuntime == null)
            {
                diagnostic =
                    "Pause Activity exit precondition rejected because Pause runtime is not initialized.";
                return false;
            }

            if (_pauseRuntime.State != PauseState.Paused)
            {
                diagnostic =
                    "Pause Activity exit precondition satisfied; Pause was already Running.";
                return true;
            }

            string resolvedSource = source.NormalizeTextOrFallback(nameof(FrameworkRuntimeHost));
            string resolvedReason = reason.NormalizeTextOrFallback("activity-exit-pause-cleanup");

            if (_pauseProductBindingRuntime != null &&
                !_pauseProductBindingRuntime.TryRestorePhysicalPostureForLifecycleExit(
                    resolvedReason,
                    out string physicalDiagnostic))
            {
                diagnostic =
                    "Pause Activity exit precondition failed to restore physical PlayerInput posture; canonical Pause remains Paused. " +
                    physicalDiagnostic;
                return false;
            }

            if (!((IPauseProductApplicationPort)this).TryRestorePauseSnapshot(
                    PauseSnapshot.FromState(
                        PauseState.Running,
                        nameof(FrameworkRuntimeHost),
                        resolvedReason,
                        Array.Empty<string>()),
                    resolvedReason,
                    out string logicalDiagnostic))
            {
                diagnostic =
                    "Pause Activity exit precondition restored physical PlayerInput posture but failed to restore logical Running state. " +
                    logicalDiagnostic;
                return false;
            }

            if (_pauseRuntime.State != PauseState.Running)
            {
                diagnostic =
                    "Pause Activity exit precondition failed; Pause did not reach a terminal Running state.";
                return false;
            }

            diagnostic =
                "Pause Activity exit precondition restored physical PlayerInput posture and resumed Running through the canonical pipeline.";
            return true;
        }
    }
}
