namespace Immersive.Framework.GameFlow
{
    internal readonly struct RouteRequestTriggerBindingResult
    {
        private RouteRequestTriggerBindingResult(bool succeeded, string status, string message, int rootCount, int triggerCount, int boundCount, int idempotentCount, int rejectedCount)
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

        internal static RouteRequestTriggerBindingResult OptionalAbsent(int rootCount) =>
            new RouteRequestTriggerBindingResult(true, "OptionalAbsent", $"Route request trigger binding found no authored triggers in '{rootCount}' explicit roots.", rootCount, 0, 0, 0, 0);

        internal static RouteRequestTriggerBindingResult Completed(int rootCount, int triggerCount, int boundCount, int idempotentCount) =>
            new RouteRequestTriggerBindingResult(true, "Bound", $"Route request trigger binding completed. roots='{rootCount}' triggers='{triggerCount}' bound='{boundCount}' idempotent='{idempotentCount}' rejected='0'.", rootCount, triggerCount, boundCount, idempotentCount, 0);

        internal static RouteRequestTriggerBindingResult Rejected(string status, string message, int rootCount, int triggerCount, int boundCount, int idempotentCount, int rejectedCount) =>
            new RouteRequestTriggerBindingResult(false, status, message, rootCount, triggerCount, boundCount, idempotentCount, rejectedCount);
    }
}
