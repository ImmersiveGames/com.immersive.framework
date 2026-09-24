using System;
using Immersive.Framework.Pause;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        bool IPauseProductApplicationPort.TryApplyProductPause(
            PauseRequest request,
            out PauseResult result,
            out string diagnostic)
        {
            result = RequestPause(request);
            diagnostic = result.Message;
            return result.Completed;
        }

        bool IPauseProductApplicationPort.TryRestorePauseSnapshot(
            PauseSnapshot snapshot,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!snapshot.IsValid || _pauseRuntime == null)
            {
                diagnostic = "Pause snapshot restoration requires an initialized valid Pause state.";
                return false;
            }

            if (_pauseRuntime.State != snapshot.State)
            {
                PauseRequestKind kind = snapshot.State == PauseState.Paused
                    ? PauseRequestKind.Pause
                    : PauseRequestKind.Resume;
                // Lifecycle compensation intentionally bypasses external transition admission.
                PauseResult result = _pauseRuntime.Request(CreatePauseRequest(kind, nameof(FrameworkRuntimeHost), reason));
                _pauseTimeScaleRuntime?.Apply(result);
                ApplyPauseSurfaceSnapshot(nameof(FrameworkRuntimeHost), reason);
            }

            bool matches = _pauseRuntime.Snapshot.State == snapshot.State;
            diagnostic = matches
                ? $"Pause application restored '{snapshot.State}'."
                : $"Pause application restoration did not reach '{snapshot.State}'.";
            return matches;
        }

        bool IPauseProductApplicationPort.TryGetApplicationPauseSnapshot(out PauseSnapshot snapshot) =>
            TryGetPauseSnapshot(out snapshot);
    }
}
