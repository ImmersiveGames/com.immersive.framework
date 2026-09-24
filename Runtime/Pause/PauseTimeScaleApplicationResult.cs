using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. Diagnostic result for the Time.timeScale side effect applied from a Pause result.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "FIRSTGAME-2C diagnostic result for Pause Time.timeScale application.")]
    internal readonly struct PauseTimeScaleApplicationResult : IEquatable<PauseTimeScaleApplicationResult>
    {
        internal PauseTimeScaleApplicationResult(
            string statusText,
            PauseState state,
            float previousTimeScale,
            float targetTimeScale,
            float currentTimeScale,
            float capturedRunningTimeScale,
            bool applied,
            bool restored,
            bool changed,
            string message)
        {
            StatusText = string.IsNullOrWhiteSpace(statusText) ? "Unknown" : statusText.Trim();
            State = state;
            PreviousTimeScale = previousTimeScale;
            TargetTimeScale = targetTimeScale;
            CurrentTimeScale = currentTimeScale;
            CapturedRunningTimeScale = capturedRunningTimeScale;
            Applied = applied;
            Restored = restored;
            Changed = changed;
            Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        }

        public string StatusText { get; }

        public PauseState State { get; }

        public float PreviousTimeScale { get; }

        public float TargetTimeScale { get; }

        public float CurrentTimeScale { get; }

        public float CapturedRunningTimeScale { get; }

        public bool Applied { get; }

        public bool Restored { get; }

        public bool Changed { get; }

        public string Message { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(StatusText) && State != PauseState.Unknown;

        public bool Equals(PauseTimeScaleApplicationResult other)
        {
            return string.Equals(StatusText, other.StatusText, StringComparison.Ordinal)
                && State == other.State
                && PreviousTimeScale.Equals(other.PreviousTimeScale)
                && TargetTimeScale.Equals(other.TargetTimeScale)
                && CurrentTimeScale.Equals(other.CurrentTimeScale)
                && CapturedRunningTimeScale.Equals(other.CapturedRunningTimeScale)
                && Applied == other.Applied
                && Restored == other.Restored
                && Changed == other.Changed
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseTimeScaleApplicationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(StatusText ?? string.Empty);
                hashCode = hashCode * 397 ^ (int)State;
                hashCode = hashCode * 397 ^ PreviousTimeScale.GetHashCode();
                hashCode = hashCode * 397 ^ TargetTimeScale.GetHashCode();
                hashCode = hashCode * 397 ^ CurrentTimeScale.GetHashCode();
                hashCode = hashCode * 397 ^ CapturedRunningTimeScale.GetHashCode();
                hashCode = hashCode * 397 ^ Applied.GetHashCode();
                hashCode = hashCode * 397 ^ Restored.GetHashCode();
                hashCode = hashCode * 397 ^ Changed.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public static bool operator ==(PauseTimeScaleApplicationResult left, PauseTimeScaleApplicationResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseTimeScaleApplicationResult left, PauseTimeScaleApplicationResult right)
        {
            return !left.Equals(right);
        }
    }
}
