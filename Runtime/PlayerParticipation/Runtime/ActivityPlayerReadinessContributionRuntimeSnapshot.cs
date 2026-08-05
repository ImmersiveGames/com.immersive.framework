using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable exact observation of the official Player readiness
    /// contribution materialized for one Activity occurrence.
    /// </summary>
    internal sealed class
        ActivityPlayerReadinessContributionRuntimeSnapshot
    {
        internal ActivityPlayerReadinessContributionRuntimeSnapshot(
            bool isAvailable,
            string activityName,
            int occurrence,
            string requirementLevel,
            ActivityReadinessParticipantState state,
            string lastReason,
            string diagnostic)
        {
            IsAvailable = isAvailable;
            ActivityName = activityName ?? string.Empty;
            Occurrence = occurrence > 0 ? occurrence : 0;
            RequirementLevel = requirementLevel ?? string.Empty;
            State = state;
            LastReason = lastReason ?? string.Empty;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal bool IsAvailable { get; }

        internal string ActivityName { get; }

        internal int Occurrence { get; }

        internal string RequirementLevel { get; }

        internal ActivityReadinessParticipantState State { get; }

        internal string LastReason { get; }

        internal string Diagnostic { get; }

        internal bool HasOccurrence =>
            IsAvailable &&
            Occurrence > 0 &&
            State != ActivityReadinessParticipantState.Idle;

        internal bool GateHeld =>
            HasOccurrence &&
            State == ActivityReadinessParticipantState.Preparing;

        internal bool Completed =>
            HasOccurrence &&
            State == ActivityReadinessParticipantState.Completed;

        internal bool Failed =>
            HasOccurrence &&
            State == ActivityReadinessParticipantState.Failed;

        internal bool Released =>
            HasOccurrence &&
            State == ActivityReadinessParticipantState.Released;

        internal static
            ActivityPlayerReadinessContributionRuntimeSnapshot Unavailable(
                string diagnostic)
        {
            return new
                ActivityPlayerReadinessContributionRuntimeSnapshot(
                    false,
                    string.Empty,
                    0,
                    string.Empty,
                    ActivityReadinessParticipantState.Idle,
                    string.Empty,
                    diagnostic);
        }

        internal string ToDiagnosticString()
        {
            return
                $"available='{IsAvailable}' activity='{ActivityName}' " +
                $"occurrence='{Occurrence}' " +
                $"requirement='{RequirementLevel}' state='{State}' " +
                $"lastReason='{LastReason}' gateHeld='{GateHeld}' " +
                $"diagnostic='{Diagnostic}'.";
        }
    }
}
