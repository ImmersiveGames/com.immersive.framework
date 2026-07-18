namespace Immersive.Framework.GlobalUi
{
    internal readonly struct GlobalUiPauseRequestTriggerBindingResult
    {
        private GlobalUiPauseRequestTriggerBindingResult(
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

        internal static GlobalUiPauseRequestTriggerBindingResult OptionalAbsent(
            int rootCount)
        {
            return new GlobalUiPauseRequestTriggerBindingResult(
                true,
                "OptionalAbsent",
                $"UIGlobal Pause request trigger binding found no authored triggers in '{rootCount}' persistent roots.",
                rootCount,
                0,
                0,
                0,
                0);
        }

        internal static GlobalUiPauseRequestTriggerBindingResult Completed(
            int rootCount,
            int triggerCount,
            int boundCount,
            int idempotentCount)
        {
            return new GlobalUiPauseRequestTriggerBindingResult(
                true,
                "Bound",
                $"UIGlobal Pause request trigger binding completed. roots='{rootCount}' triggers='{triggerCount}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='0'.",
                rootCount,
                triggerCount,
                boundCount,
                idempotentCount,
                0);
        }

        internal static GlobalUiPauseRequestTriggerBindingResult Rejected(
            string status,
            string message,
            int rootCount,
            int triggerCount,
            int boundCount,
            int idempotentCount,
            int rejectedCount)
        {
            return new GlobalUiPauseRequestTriggerBindingResult(
                false,
                status,
                message,
                rootCount,
                triggerCount,
                boundCount,
                idempotentCount,
                rejectedCount);
        }
    }
}
