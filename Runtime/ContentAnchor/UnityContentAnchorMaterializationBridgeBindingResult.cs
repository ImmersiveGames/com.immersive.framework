namespace Immersive.Framework.ContentAnchor
{
    internal readonly struct UnityContentAnchorMaterializationBridgeBindingResult
    {
        private UnityContentAnchorMaterializationBridgeBindingResult(
            bool succeeded,
            string status,
            string message,
            int rootCount,
            int bridgeCount,
            int boundCount,
            int idempotentCount,
            int rejectedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RootCount = rootCount;
            BridgeCount = bridgeCount;
            BoundCount = boundCount;
            IdempotentCount = idempotentCount;
            RejectedCount = rejectedCount;
        }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Message { get; }

        internal int RootCount { get; }

        internal int BridgeCount { get; }

        internal int BoundCount { get; }

        internal int IdempotentCount { get; }

        internal int RejectedCount { get; }

        internal static UnityContentAnchorMaterializationBridgeBindingResult OptionalAbsent(
            int roots)
        {
            return new UnityContentAnchorMaterializationBridgeBindingResult(
                true,
                "OptionalAbsent",
                $"Unity Content Anchor Materialization Bridge binding found no authored bridges in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static UnityContentAnchorMaterializationBridgeBindingResult Completed(
            int roots,
            int bridges,
            int bound,
            int idempotent)
        {
            return new UnityContentAnchorMaterializationBridgeBindingResult(
                true,
                "Bound",
                $"Unity Content Anchor Materialization Bridge binding completed. roots='{roots}' bridges='{bridges}' bound='{bound}' idempotent='{idempotent}' rejected='0'.",
                roots,
                bridges,
                bound,
                idempotent,
                0);
        }

        internal static UnityContentAnchorMaterializationBridgeBindingResult Rejected(
            string status,
            string message,
            int roots,
            int bridges,
            int bound,
            int idempotent,
            int rejected)
        {
            return new UnityContentAnchorMaterializationBridgeBindingResult(
                false,
                status,
                message,
                roots,
                bridges,
                bound,
                idempotent,
                rejected);
        }
    }
}
