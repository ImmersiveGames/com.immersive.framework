using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. Applies the concrete Unity simulation pause effect for the framework Pause state.
    /// It captures the running Time.timeScale before entering Pause and restores that captured value when Pause exits.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "FIRSTGAME-2C applies Time.timeScale as the built-in concrete Pause effect.")]
    internal sealed class PauseTimeScaleRuntime
    {
        private const float DefaultPausedTimeScale = 0f;
        private const float DefaultUncapturedRunningTimeScale = 1f;

        private readonly float _pausedTimeScale;
        private bool _hasCapturedRunningTimeScale;
        private float _capturedRunningTimeScale = DefaultUncapturedRunningTimeScale;

        internal PauseTimeScaleRuntime()
            : this(DefaultPausedTimeScale)
        {
        }

        internal PauseTimeScaleRuntime(float pausedTimeScale)
        {
            _pausedTimeScale = Mathf.Max(0f, pausedTimeScale);
        }

        internal bool HasCapturedRunningTimeScale => _hasCapturedRunningTimeScale;

        internal float CapturedRunningTimeScale => _capturedRunningTimeScale;

        internal float PausedTimeScale => _pausedTimeScale;

        internal PauseTimeScaleApplicationResult Apply(PauseResult pauseResult)
        {
            if (!pauseResult.IsValid)
            {
                throw new ArgumentException("Pause TimeScale runtime requires a valid Pause result.", nameof(pauseResult));
            }

            return pauseResult.CurrentState == PauseState.Paused
                ? ApplyPaused(pauseResult)
                : ApplyRunning(pauseResult);
        }

        internal PauseTimeScaleApplicationResult RestoreIfCaptured(string reason)
        {
            if (!_hasCapturedRunningTimeScale)
            {
                float current = Time.timeScale;
                return new PauseTimeScaleApplicationResult(
                    "NoCapturedRunningTimeScale",
                    PauseState.Running,
                    current,
                    current,
                    current,
                    _capturedRunningTimeScale,
                    applied: false,
                    restored: false,
                    changed: false,
                    string.IsNullOrWhiteSpace(reason) ? "No captured running Time.timeScale to restore." : reason.Trim());
            }

            float previous = Time.timeScale;
            float target = _capturedRunningTimeScale;
            ApplyTimeScale(target);
            _hasCapturedRunningTimeScale = false;

            return new PauseTimeScaleApplicationResult(
                "RestoredOnShutdown",
                PauseState.Running,
                previous,
                target,
                Time.timeScale,
                target,
                applied: true,
                restored: true,
                changed: !Approximately(previous, Time.timeScale),
                string.IsNullOrWhiteSpace(reason) ? "Restored captured running Time.timeScale on shutdown." : reason.Trim());
        }

        private PauseTimeScaleApplicationResult ApplyPaused(PauseResult pauseResult)
        {
            float previous = Time.timeScale;
            if (!_hasCapturedRunningTimeScale || pauseResult.StateChanged)
            {
                _capturedRunningTimeScale = previous;
                _hasCapturedRunningTimeScale = true;
            }

            ApplyTimeScale(_pausedTimeScale);
            float current = Time.timeScale;

            return new PauseTimeScaleApplicationResult(
                pauseResult.StateChanged ? "AppliedPausedTimeScale" : "MaintainedPausedTimeScale",
                pauseResult.CurrentState,
                previous,
                _pausedTimeScale,
                current,
                _capturedRunningTimeScale,
                applied: true,
                restored: false,
                changed: !Approximately(previous, current),
                pauseResult.StateChanged
                    ? "Pause applied Time.timeScale for Paused state."
                    : "Pause maintained Time.timeScale for already Paused state.");
        }

        private PauseTimeScaleApplicationResult ApplyRunning(PauseResult pauseResult)
        {
            float previous = Time.timeScale;
            if (!_hasCapturedRunningTimeScale)
            {
                return new PauseTimeScaleApplicationResult(
                    "NoCapturedRunningTimeScale",
                    pauseResult.CurrentState,
                    previous,
                    previous,
                    previous,
                    _capturedRunningTimeScale,
                    applied: false,
                    restored: false,
                    changed: false,
                    "Pause is Running and no captured running Time.timeScale exists.");
            }

            float target = _capturedRunningTimeScale;
            ApplyTimeScale(target);
            _hasCapturedRunningTimeScale = false;
            float current = Time.timeScale;

            return new PauseTimeScaleApplicationResult(
                "RestoredRunningTimeScale",
                pauseResult.CurrentState,
                previous,
                target,
                current,
                target,
                applied: true,
                restored: true,
                changed: !Approximately(previous, current),
                "Pause restored captured running Time.timeScale for Running state.");
        }

        private static void ApplyTimeScale(float value)
        {
            if (!Approximately(Time.timeScale, value))
            {
                Time.timeScale = value;
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.0001f;
        }
    }
}
