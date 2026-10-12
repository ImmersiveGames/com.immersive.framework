using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Editor.Diagnostics.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Editor-only evidence from diagnostic GameFlow fault injection; not product API.")]
    public sealed class FrameworkGameFlowDiagnosticFaultReport
    {
        internal FrameworkGameFlowDiagnosticFaultReport(
            string diagnostic,
            string operation,
            string transaction,
            string slot)
        {
            Diagnostic = diagnostic ?? string.Empty;
            Operation = operation ?? string.Empty;
            Transaction = transaction ?? string.Empty;
            Slot = slot ?? string.Empty;
        }

        public string Diagnostic { get; }
        public string Operation { get; }
        public string Transaction { get; }
        public string Slot { get; }
    }
}
