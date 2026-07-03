using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Positive relative weight for one Loading step.
    /// Weight is used by future aggregation only; it does not schedule or execute the step.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading step weight primitive.")]
    public readonly struct LoadingStepWeight : IEquatable<LoadingStepWeight>
    {
        public static readonly LoadingStepWeight One = new LoadingStepWeight(1f);

        public LoadingStepWeight(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Loading step weight must be a finite positive value.");
            }

            Value = value;
        }

        public float Value { get; }

        public bool IsValid => Value > 0f && !float.IsNaN(Value) && !float.IsInfinity(Value);

        public bool Equals(LoadingStepWeight other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingStepWeight other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"weight='{Value:0.###}'";
        }

        public static LoadingStepWeight From(float value)
        {
            return new LoadingStepWeight(value);
        }

        public static bool operator ==(LoadingStepWeight left, LoadingStepWeight right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingStepWeight left, LoadingStepWeight right)
        {
            return !left.Equals(right);
        }
    }
}
