namespace Immersive.Framework.GameFlow.Diagnostics
{
    internal sealed class NoOpGameFlowDiagnosticFaultPlan : IGameFlowDiagnosticFaultPlan
    {
        internal static readonly NoOpGameFlowDiagnosticFaultPlan Instance =
            new NoOpGameFlowDiagnosticFaultPlan();

        private NoOpGameFlowDiagnosticFaultPlan() { }

        public GameFlowDiagnosticFaultDecision Evaluate(
            GameFlowDiagnosticFaultRequest request) =>
            GameFlowDiagnosticFaultDecision.None;
    }
}
