namespace Immersive.Framework.ObjectReset
{
    internal readonly struct ObjectResetTriggerBinderResult
    {
        private ObjectResetTriggerBinderResult(
            bool succeeded,
            string status,
            string message,
            int rootCount,
            int triggerCount,
            int boundCount,
            int idempotentCount,
            int rejectedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RootCount = rootCount;
            TriggerCount = triggerCount;
            BoundCount = boundCount;
            IdempotentCount = idempotentCount;
            RejectedCount = rejectedCount;
        }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Message { get; }

        internal int RootCount { get; }

        internal int TriggerCount { get; }

        internal int BoundCount { get; }

        internal int IdempotentCount { get; }

        internal int RejectedCount { get; }

        internal static ObjectResetTriggerBinderResult OptionalAbsent(int roots)
        {
            return new ObjectResetTriggerBinderResult(
                true,
                "OptionalAbsent",
                $"Object Reset trigger binding found no authored triggers in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static ObjectResetTriggerBinderResult Completed(
            int roots,
            int triggers,
            int bound,
            int idempotent)
        {
            return new ObjectResetTriggerBinderResult(
                true,
                "Bound",
                $"Object Reset trigger binding completed. roots='{roots}' triggers='{triggers}' bound='{bound}' idempotent='{idempotent}' rejected='0'.",
                roots,
                triggers,
                bound,
                idempotent,
                0);
        }

        internal static ObjectResetTriggerBinderResult Rejected(
            string status,
            string message,
            int roots,
            int triggers,
            int bound,
            int idempotent,
            int rejected)
        {
            return new ObjectResetTriggerBinderResult(
                false,
                status,
                message,
                roots,
                triggers,
                bound,
                idempotent,
                rejected);
        }
    }
}
