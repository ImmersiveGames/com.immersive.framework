namespace Immersive.Framework.GameFlow.Diagnostics
{
    internal interface IGameFlowDiagnosticFaultPlan
    {
        GameFlowDiagnosticFaultDecision Evaluate(GameFlowDiagnosticFaultRequest request);
    }
}
