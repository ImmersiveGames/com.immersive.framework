using System;
using System.Globalization;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Internal. Canonical loading progress contract for framework diagnostics.
    /// It records whether progress is supported and, when available, the normalized progress snapshot.
    /// It does not own loading execution or any visual loading surface.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26B loading progress contract.")]
    internal readonly struct FrameworkLoadingProgress : IEquatable<FrameworkLoadingProgress>
    {
        public FrameworkLoadingProgress(
            bool supported,
            FrameworkLoadingProgressMode mode,
            float value01,
            string phase,
            string message)
        {
            if (!Enum.IsDefined(typeof(FrameworkLoadingProgressMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Loading progress mode must be explicit.");
            }

            if (!supported && mode == FrameworkLoadingProgressMode.Determinate)
            {
                throw new ArgumentException("Unsupported loading progress cannot be determinate.", nameof(mode));
            }

            if (supported && mode == FrameworkLoadingProgressMode.Unknown)
            {
                throw new ArgumentException("Supported loading progress cannot use the unknown mode.", nameof(mode));
            }

            if (mode is FrameworkLoadingProgressMode.Unknown or FrameworkLoadingProgressMode.Indeterminate
                && value01 != 0f)
            {
                throw new ArgumentException("Unknown or indeterminate loading progress cannot carry a normalized value.", nameof(value01));
            }

            if (float.IsNaN(value01) || float.IsInfinity(value01))
            {
                throw new ArgumentOutOfRangeException(nameof(value01), value01, "Loading progress must be finite.");
            }

            Supported = supported;
            Mode = mode;
            Value01 = Clamp01(value01);
            Phase = Normalize(phase);
            Message = Normalize(message);
        }

        public bool Supported { get; }

        public FrameworkLoadingProgressMode Mode { get; }

        public float Value01 { get; }

        public int Percent => (int)Math.Round(Value01 * 100f, MidpointRounding.AwayFromZero);

        public string Phase { get; }

        public string Message { get; }

        public bool IsDeterminate => Mode == FrameworkLoadingProgressMode.Determinate;

        public bool IsUnknown => Mode == FrameworkLoadingProgressMode.Unknown;

        public string ModeText => Mode.ToString();

        public string ValueText => Value01.ToString("0.00", CultureInfo.InvariantCulture);

        public string PercentText => Percent.ToString(CultureInfo.InvariantCulture);

        public string PhaseText => Phase.ToDiagnosticText();

        public string MessageText => Message.ToDiagnosticText();

        public bool Equals(FrameworkLoadingProgress other)
        {
            return Supported == other.Supported
                && Mode == other.Mode
                && Value01.Equals(other.Value01)
                && string.Equals(Phase, other.Phase, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkLoadingProgress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Supported.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Mode;
                hashCode = hashCode * 397 ^ Value01.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Phase ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"supported='{Supported}' mode='{ModeText}' value='{ValueText}' percent='{PercentText}' phase='{PhaseText}' message='{MessageText}'";
        }

        public static FrameworkLoadingProgress Unknown(string phase, string message)
        {
            return new FrameworkLoadingProgress(false, FrameworkLoadingProgressMode.Unknown, 0f, phase, message);
        }

        public static FrameworkLoadingProgress Unsupported(string phase, string message)
        {
            return new FrameworkLoadingProgress(false, FrameworkLoadingProgressMode.Indeterminate, 0f, phase, message);
        }

        public static FrameworkLoadingProgress Indeterminate(bool supported, string phase, string message)
        {
            return new FrameworkLoadingProgress(supported, FrameworkLoadingProgressMode.Indeterminate, 0f, phase, message);
        }

        public static FrameworkLoadingProgress Determinate(float value01, string phase, string message)
        {
            return new FrameworkLoadingProgress(true, FrameworkLoadingProgressMode.Determinate, value01, phase, message);
        }

        public static bool operator ==(FrameworkLoadingProgress left, FrameworkLoadingProgress right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkLoadingProgress left, FrameworkLoadingProgress right)
        {
            return !left.Equals(right);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
