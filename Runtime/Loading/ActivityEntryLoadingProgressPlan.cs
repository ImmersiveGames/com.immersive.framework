using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// Immutable operation-scoped allocation of technical and Activity-readiness progress.
    /// The readiness participant count is intentionally absent because it is captured later
    /// by the authoritative Activity readiness occurrence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IF-READY-PROGRESS-02 stable Activity entry Loading progress plan.")]
    internal readonly struct ActivityEntryLoadingProgressPlan :
        IEquatable<ActivityEntryLoadingProgressPlan>
    {
        private ActivityEntryLoadingProgressPlan(
            int technicalStepCount,
            bool reservesReadinessPhase,
            int totalPhaseUnitCount,
            FrameworkLoadingProgressRange technicalRange,
            FrameworkLoadingProgressRange readinessRange)
        {
            TechnicalStepCount = technicalStepCount;
            ReservesReadinessPhase = reservesReadinessPhase;
            TotalPhaseUnitCount = totalPhaseUnitCount;
            TechnicalRange = technicalRange;
            ReadinessRange = readinessRange;
        }

        internal int TechnicalStepCount { get; }
        internal int TechnicalPhaseUnitCount => TechnicalStepCount;
        internal int ReadinessPhaseUnitCount => ReservesReadinessPhase ? 1 : 0;
        internal int TotalPhaseUnitCount { get; }
        internal bool ReservesReadinessPhase { get; }
        internal FrameworkLoadingProgressRange TechnicalRange { get; }
        internal FrameworkLoadingProgressRange ReadinessRange { get; }
        internal bool HasTechnicalRange => !TechnicalRange.IsEmpty;
        internal bool HasReadinessRange =>
            ReservesReadinessPhase && !ReadinessRange.IsEmpty;

        internal static ActivityEntryLoadingProgressPlan Create(
            int technicalStepCount,
            bool reserveReadinessPhase)
        {
            if (technicalStepCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(technicalStepCount),
                    "Technical Loading step count cannot be negative.");
            }

            if (!reserveReadinessPhase)
            {
                return new ActivityEntryLoadingProgressPlan(
                    technicalStepCount,
                    false,
                    technicalStepCount,
                    FrameworkLoadingProgressRange.Full,
                    FrameworkLoadingProgressRange.EmptyAt(1f));
            }

            int totalPhaseUnitCount = checked(technicalStepCount + 1);
            FrameworkLoadingProgressRange technicalRange =
                FrameworkLoadingProgressRange.FromUnits(
                    0,
                    technicalStepCount,
                    totalPhaseUnitCount);
            FrameworkLoadingProgressRange readinessRange =
                FrameworkLoadingProgressRange.FromUnits(
                    technicalStepCount,
                    1,
                    totalPhaseUnitCount);

            return new ActivityEntryLoadingProgressPlan(
                technicalStepCount,
                true,
                totalPhaseUnitCount,
                technicalRange,
                readinessRange);
        }

        public bool Equals(ActivityEntryLoadingProgressPlan other)
        {
            return TechnicalStepCount == other.TechnicalStepCount &&
                   ReservesReadinessPhase == other.ReservesReadinessPhase &&
                   TotalPhaseUnitCount == other.TotalPhaseUnitCount &&
                   TechnicalRange.Equals(other.TechnicalRange) &&
                   ReadinessRange.Equals(other.ReadinessRange);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityEntryLoadingProgressPlan other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = TechnicalStepCount;
                hashCode = hashCode * 397 ^ ReservesReadinessPhase.GetHashCode();
                hashCode = hashCode * 397 ^ TotalPhaseUnitCount;
                hashCode = hashCode * 397 ^ TechnicalRange.GetHashCode();
                hashCode = hashCode * 397 ^ ReadinessRange.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            ActivityEntryLoadingProgressPlan left,
            ActivityEntryLoadingProgressPlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ActivityEntryLoadingProgressPlan left,
            ActivityEntryLoadingProgressPlan right)
        {
            return !left.Equals(right);
        }
    }
}
