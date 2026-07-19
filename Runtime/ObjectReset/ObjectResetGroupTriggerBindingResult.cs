namespace Immersive.Framework.ObjectReset
{
    internal readonly struct ObjectResetGroupTriggerBindingResult
    {
        private ObjectResetGroupTriggerBindingResult(
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

        internal static ObjectResetGroupTriggerBindingResult OptionalAbsent(int roots)
        {
            return new ObjectResetGroupTriggerBindingResult(
                true,
                "OptionalAbsent",
                $"Object Reset Group trigger binding found no authored triggers in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static ObjectResetGroupTriggerBindingResult Completed(
            int roots,
            int triggers,
            int bound,
            int idempotent)
        {
            return new ObjectResetGroupTriggerBindingResult(
                true,
                "Bound",
                $"Object Reset Group trigger binding completed. roots='{roots}' triggers='{triggers}' bound='{bound}' idempotent='{idempotent}' rejected='0'.",
                roots,
                triggers,
                bound,
                idempotent,
                0);
        }

        internal static ObjectResetGroupTriggerBindingResult Rejected(
            string status,
            string message,
            int roots,
            int triggers,
            int bound,
            int idempotent,
            int rejected)
        {
            return new ObjectResetGroupTriggerBindingResult(
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
