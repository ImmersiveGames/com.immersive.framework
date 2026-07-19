namespace Immersive.Framework.UnityInput
{
    internal readonly struct UnityPlayerInputGateAdapterBindingResult
    {
        private UnityPlayerInputGateAdapterBindingResult(
            bool succeeded,
            string status,
            string message,
            int rootCount,
            int adapterCount,
            int boundCount,
            int idempotentCount,
            int rejectedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RootCount = rootCount;
            AdapterCount = adapterCount;
            BoundCount = boundCount;
            IdempotentCount = idempotentCount;
            RejectedCount = rejectedCount;
        }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Message { get; }

        internal int RootCount { get; }

        internal int AdapterCount { get; }

        internal int BoundCount { get; }

        internal int IdempotentCount { get; }

        internal int RejectedCount { get; }

        internal static UnityPlayerInputGateAdapterBindingResult OptionalAbsent(int roots)
        {
            return new UnityPlayerInputGateAdapterBindingResult(
                true,
                "OptionalAbsent",
                $"Unity PlayerInput Gate Adapter binding found no authored adapters in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static UnityPlayerInputGateAdapterBindingResult Completed(
            int roots,
            int adapters,
            int bound,
            int idempotent)
        {
            return new UnityPlayerInputGateAdapterBindingResult(
                true,
                "Bound",
                $"Unity PlayerInput Gate Adapter binding completed. roots='{roots}' adapters='{adapters}' bound='{bound}' idempotent='{idempotent}' rejected='0'.",
                roots,
                adapters,
                bound,
                idempotent,
                0);
        }

        internal static UnityPlayerInputGateAdapterBindingResult Rejected(
            string status,
            string message,
            int roots,
            int adapters,
            int bound,
            int idempotent,
            int rejected)
        {
            return new UnityPlayerInputGateAdapterBindingResult(
                false,
                status,
                message,
                roots,
                adapters,
                bound,
                idempotent,
                rejected);
        }
    }
}
