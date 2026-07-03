using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Weighted contribution of one Loading step toward a future operation aggregation.
    /// This is passive data; F22C owns aggregation smoke and any aggregate result logic.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B weighted Loading progress primitive; no aggregator runtime.")]
    public readonly struct LoadingWeightedProgress : IEquatable<LoadingWeightedProgress>
    {
        public LoadingWeightedProgress(LoadingStepWeight weight, LoadingProgress progress)
        {
            if (!weight.IsValid)
            {
                throw new ArgumentException("Loading weighted progress requires a valid step weight.", nameof(weight));
            }

            Weight = weight;
            Progress = progress;
        }

        public LoadingStepWeight Weight { get; }

        public LoadingProgress Progress { get; }

        public float WeightedCompleted => Weight.Value * Progress.NormalizedValue;

        public float WeightedTotal => Weight.Value;

        public bool IsComplete => Progress.IsComplete;

        public bool IsValid => Weight.IsValid;

        public bool Equals(LoadingWeightedProgress other)
        {
            return Weight.Equals(other.Weight) && Progress.Equals(other.Progress);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingWeightedProgress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Weight.GetHashCode() * 397 ^ Progress.GetHashCode();
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"weight='{Weight.Value:0.###}' progress='{Progress.NormalizedValue:0.###}' weightedCompleted='{WeightedCompleted:0.###}' weightedTotal='{WeightedTotal:0.###}'";
        }

        public static LoadingWeightedProgress From(float weight, float normalizedProgress)
        {
            return new LoadingWeightedProgress(new LoadingStepWeight(weight), new LoadingProgress(normalizedProgress));
        }

        public static bool operator ==(LoadingWeightedProgress left, LoadingWeightedProgress right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingWeightedProgress left, LoadingWeightedProgress right)
        {
            return !left.Equals(right);
        }
    }
}
