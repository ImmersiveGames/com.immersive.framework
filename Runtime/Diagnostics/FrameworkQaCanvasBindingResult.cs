#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Immersive.Framework.Diagnostics
{
    internal readonly struct FrameworkQaCanvasBindingResult
    {
        private FrameworkQaCanvasBindingResult(
            bool succeeded,
            string status,
            string message,
            int rootCount,
            int canvasCount,
            int boundCount,
            int idempotentCount,
            int rejectedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RootCount = rootCount;
            CanvasCount = canvasCount;
            BoundCount = boundCount;
            IdempotentCount = idempotentCount;
            RejectedCount = rejectedCount;
        }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Message { get; }

        internal int RootCount { get; }

        internal int CanvasCount { get; }

        internal int BoundCount { get; }

        internal int IdempotentCount { get; }

        internal int RejectedCount { get; }

        internal static FrameworkQaCanvasBindingResult OptionalAbsent(
            int roots)
        {
            return new FrameworkQaCanvasBindingResult(
                true,
                "OptionalAbsent",
                $"Framework QA Canvas binding found no authored canvases in '{roots}' explicit roots.",
                roots,
                0,
                0,
                0,
                0);
        }

        internal static FrameworkQaCanvasBindingResult Completed(
            int roots,
            int canvases,
            int bound,
            int idempotent)
        {
            return new FrameworkQaCanvasBindingResult(
                true,
                "Bound",
                $"Framework QA Canvas binding completed. roots='{roots}' canvases='{canvases}' bound='{bound}' idempotent='{idempotent}' rejected='0'.",
                roots,
                canvases,
                bound,
                idempotent,
                0);
        }

        internal static FrameworkQaCanvasBindingResult Rejected(
            string status,
            string message,
            int roots,
            int canvases,
            int bound,
            int idempotent,
            int rejected)
        {
            return new FrameworkQaCanvasBindingResult(
                false,
                status,
                message,
                roots,
                canvases,
                bound,
                idempotent,
                rejected);
        }
    }
}
#endif
