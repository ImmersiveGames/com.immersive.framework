namespace Immersive.Framework.PlayerParticipation
{
    internal readonly struct LocalPlayerActorSelectionRequestAuthoringReleaseResult
    {
        private LocalPlayerActorSelectionRequestAuthoringReleaseResult(
            bool succeeded,
            string status,
            string message,
            int rootCount,
            int authoringCount,
            int releasedCount,
            int idempotentCount,
            int rejectedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RootCount = rootCount;
            AuthoringCount = authoringCount;
            ReleasedCount = releasedCount;
            IdempotentCount = idempotentCount;
            RejectedCount = rejectedCount;
        }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Message { get; }

        internal int RootCount { get; }

        internal int AuthoringCount { get; }

        internal int ReleasedCount { get; }

        internal int IdempotentCount { get; }

        internal int RejectedCount { get; }

        internal static LocalPlayerActorSelectionRequestAuthoringReleaseResult OptionalAbsent(
            int roots)
        {
            return new LocalPlayerActorSelectionRequestAuthoringReleaseResult(
                true,
                "OptionalAbsent",
                $"Local Player Actor Selection Request release found no authored request surfaces in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static LocalPlayerActorSelectionRequestAuthoringReleaseResult Completed(
            int roots,
            int authorings,
            int released,
            int idempotent)
        {
            return new LocalPlayerActorSelectionRequestAuthoringReleaseResult(
                true,
                released > 0 ? "Released" : "Idempotent",
                $"Local Player Actor Selection Request release completed. roots='{roots}' authorings='{authorings}' released='{released}' idempotent='{idempotent}' rejected='0'.",
                roots,
                authorings,
                released,
                idempotent,
                0);
        }

        internal static LocalPlayerActorSelectionRequestAuthoringReleaseResult Rejected(
            string status,
            string message,
            int roots,
            int authorings,
            int released,
            int idempotent,
            int rejected)
        {
            return new LocalPlayerActorSelectionRequestAuthoringReleaseResult(
                false,
                status,
                message,
                roots,
                authorings,
                released,
                idempotent,
                rejected);
        }
    }
}
