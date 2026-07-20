namespace Immersive.Framework.PlayerParticipation
{
    internal readonly struct LocalPlayerActorSelectionRequestAuthoringBindingResult
    {
        private LocalPlayerActorSelectionRequestAuthoringBindingResult(
            bool succeeded,
            string status,
            string message,
            int rootCount,
            int authoringCount,
            int boundCount,
            int idempotentCount,
            int rejectedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RootCount = rootCount;
            AuthoringCount = authoringCount;
            BoundCount = boundCount;
            IdempotentCount = idempotentCount;
            RejectedCount = rejectedCount;
        }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Message { get; }

        internal int RootCount { get; }

        internal int AuthoringCount { get; }

        internal int BoundCount { get; }

        internal int IdempotentCount { get; }

        internal int RejectedCount { get; }

        internal static LocalPlayerActorSelectionRequestAuthoringBindingResult OptionalAbsent(
            int roots)
        {
            return new LocalPlayerActorSelectionRequestAuthoringBindingResult(
                true,
                "OptionalAbsent",
                $"Local Player Actor Selection Request binding found no authored request surfaces in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static LocalPlayerActorSelectionRequestAuthoringBindingResult Completed(
            int roots,
            int authorings,
            int bound,
            int idempotent)
        {
            return new LocalPlayerActorSelectionRequestAuthoringBindingResult(
                true,
                "Bound",
                $"Local Player Actor Selection Request binding completed. roots='{roots}' authorings='{authorings}' bound='{bound}' idempotent='{idempotent}' rejected='0'.",
                roots,
                authorings,
                bound,
                idempotent,
                0);
        }

        internal static LocalPlayerActorSelectionRequestAuthoringBindingResult Rejected(
            string status,
            string message,
            int roots,
            int authorings,
            int bound,
            int idempotent,
            int rejected)
        {
            return new LocalPlayerActorSelectionRequestAuthoringBindingResult(
                false,
                status,
                message,
                roots,
                authorings,
                bound,
                idempotent,
                rejected);
        }
    }
}
