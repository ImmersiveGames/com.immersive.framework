using System;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable occurrence-scoped evidence used by diagnostics and later Loading progress projection.
    /// It does not own readiness or publish presentation progress.
    /// </summary>
    internal readonly struct ActivityReadinessProgressSnapshot
    {
        private ActivityReadinessProgressSnapshot(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readiness,
            float readinessRatio)
        {
            Occurrence = occurrence;
            RequiredCount = readiness.RequiredCount;
            RequiredPendingCount = readiness.RequiredPendingCount;
            RequiredCompletedCount = readiness.RequiredCompletedCount;
            RequiredFailedCount = readiness.RequiredFailedCount;
            RequiredReleasedCount = readiness.RequiredReleasedCount;
            OptionalCount = readiness.OptionalCount;
            OptionalPendingCount = readiness.OptionalPendingCount;
            OptionalCompletedCount = readiness.OptionalCompletedCount;
            OptionalFailedCount = readiness.OptionalFailedCount;
            OptionalReleasedCount = readiness.OptionalReleasedCount;
            ReadinessRatio = readinessRatio;
            IsReady = readiness.IsReady;
            HasTerminalFailure = readiness.HasTerminalFailure;
        }

        internal ActivityReadinessOccurrence Occurrence { get; }
        internal int RequiredCount { get; }
        internal int RequiredPendingCount { get; }
        internal int RequiredCompletedCount { get; }
        internal int RequiredFailedCount { get; }
        internal int RequiredReleasedCount { get; }
        internal int OptionalCount { get; }
        internal int OptionalPendingCount { get; }
        internal int OptionalCompletedCount { get; }
        internal int OptionalFailedCount { get; }
        internal int OptionalReleasedCount { get; }
        internal float ReadinessRatio { get; }
        internal bool IsReady { get; }
        internal bool HasTerminalFailure { get; }
        internal bool IsValid => Occurrence.IsValid;

        internal static ActivityReadinessProgressSnapshot Create(
            ActivityReadinessOccurrence occurrence,
            ActivityReadinessState readiness)
        {
            if (!occurrence.IsValid)
            {
                throw new ArgumentException(
                    "Activity readiness occurrence must be valid.",
                    nameof(occurrence));
            }

            if (!readiness.HasActivity ||
                !ReferenceEquals(readiness.Activity, occurrence.Activity))
            {
                throw new ArgumentException(
                    "Activity readiness state must belong to the supplied occurrence.",
                    nameof(readiness));
            }

            float readinessRatio = readiness.RequiredCount > 0
                ? (float)readiness.RequiredCompletedCount / readiness.RequiredCount
                : readiness.IsReady
                    ? 1f
                    : 0f;

            return new ActivityReadinessProgressSnapshot(
                occurrence,
                readiness,
                readinessRatio);
        }
    }
}
