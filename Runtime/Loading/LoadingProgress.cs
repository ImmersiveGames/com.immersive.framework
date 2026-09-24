using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Normalized Loading progress value in the inclusive range 0..1.
    /// This is not a visual loading bar and does not imply UI, fade, curtain or loading screen prefab.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B normalized Loading progress primitive.")]
    public readonly struct LoadingProgress : IEquatable<LoadingProgress>
    {
        public static readonly LoadingProgress Zero = new LoadingProgress(0f);
        public static readonly LoadingProgress Complete = new LoadingProgress(1f);

        public LoadingProgress(float normalizedValue)
        {
            if (float.IsNaN(normalizedValue) || float.IsInfinity(normalizedValue))
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedValue), normalizedValue, "Loading progress must be a finite normalized value.");
            }

            if (normalizedValue is < 0f or > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedValue), normalizedValue, "Loading progress must be between 0 and 1 inclusive.");
            }

            NormalizedValue = normalizedValue;
        }

        public float NormalizedValue { get; }

        public int PercentRounded => (int)Math.Round(NormalizedValue * 100f, MidpointRounding.AwayFromZero);

        public bool IsComplete => NormalizedValue >= 1f;

        public bool Equals(LoadingProgress other)
        {
            return NormalizedValue.Equals(other.NormalizedValue);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingProgress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return NormalizedValue.GetHashCode();
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"progress='{NormalizedValue:0.###}' percent='{PercentRounded}'";
        }

        public static LoadingProgress FromNormalized(float normalizedValue)
        {
            return new LoadingProgress(normalizedValue);
        }

        public static LoadingProgress FromPercent(float percent)
        {
            if (float.IsNaN(percent) || float.IsInfinity(percent))
            {
                throw new ArgumentOutOfRangeException(nameof(percent), percent, "Loading progress percent must be finite.");
            }

            return new LoadingProgress(percent / 100f);
        }

        public static bool operator ==(LoadingProgress left, LoadingProgress right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingProgress left, LoadingProgress right)
        {
            return !left.Equals(right);
        }
    }
}
