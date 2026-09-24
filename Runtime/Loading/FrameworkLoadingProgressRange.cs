using System;
using System.Globalization;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// Immutable normalized interval used to map operation-local progress into a stable
    /// section of the global Loading envelope. It does not report progress or own presentation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-READY-PROGRESS-02 normalized Loading progress range.")]
    internal readonly struct FrameworkLoadingProgressRange :
        IEquatable<FrameworkLoadingProgressRange>
    {
        internal FrameworkLoadingProgressRange(float start01, float end01)
        {
            ValidateFinite(start01, nameof(start01));
            ValidateFinite(end01, nameof(end01));

            if (start01 < 0f || start01 > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(start01),
                    start01,
                    "Loading progress range start must be normalized.");
            }

            if (end01 < 0f || end01 > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(end01),
                    end01,
                    "Loading progress range end must be normalized.");
            }

            if (end01 < start01)
            {
                throw new ArgumentException(
                    "Loading progress range end cannot precede its start.",
                    nameof(end01));
            }

            Start01 = start01;
            End01 = end01;
        }

        internal float Start01 { get; }
        internal float End01 { get; }
        internal float Length01 => End01 - Start01;
        internal bool IsEmpty => Length01 <= 0f;
        internal bool IsFull => Start01 <= 0f && End01 >= 1f;

        internal float Map(float localProgress01)
        {
            ValidateFinite(localProgress01, nameof(localProgress01));
            return Start01 + Clamp01(localProgress01) * Length01;
        }

        internal static FrameworkLoadingProgressRange Full =>
            new FrameworkLoadingProgressRange(0f, 1f);

        internal static FrameworkLoadingProgressRange EmptyAt(float position01)
        {
            return new FrameworkLoadingProgressRange(position01, position01);
        }

        internal static FrameworkLoadingProgressRange FromUnits(
            int startUnit,
            int unitCount,
            int totalUnitCount)
        {
            if (startUnit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startUnit));
            }

            if (unitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitCount));
            }

            if (totalUnitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalUnitCount),
                    "Loading progress total unit count must be positive.");
            }

            if (startUnit > totalUnitCount ||
                unitCount > totalUnitCount - startUnit)
            {
                throw new ArgumentException(
                    "Loading progress units must fit inside the total envelope.");
            }

            float start01 = (float)startUnit / totalUnitCount;
            float end01 = (float)(startUnit + unitCount) / totalUnitCount;
            return new FrameworkLoadingProgressRange(start01, end01);
        }

        public bool Equals(FrameworkLoadingProgressRange other)
        {
            return Start01.Equals(other.Start01) &&
                   End01.Equals(other.End01);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkLoadingProgressRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start01.GetHashCode() * 397) ^ End01.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"[{Start01.ToString("0.0000", CultureInfo.InvariantCulture)}, " +
                   $"{End01.ToString("0.0000", CultureInfo.InvariantCulture)}]";
        }

        public static bool operator ==(
            FrameworkLoadingProgressRange left,
            FrameworkLoadingProgressRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FrameworkLoadingProgressRange left,
            FrameworkLoadingProgressRange right)
        {
            return !left.Equals(right);
        }

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Loading progress values must be finite.");
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
