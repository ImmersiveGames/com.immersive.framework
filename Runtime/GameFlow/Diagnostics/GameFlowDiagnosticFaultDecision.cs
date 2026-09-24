namespace Immersive.Framework.GameFlow.Diagnostics
{
    internal readonly struct GameFlowDiagnosticFaultDecision
    {
        internal GameFlowDiagnosticFaultDecision(bool shouldFail, string diagnostic)
        {
            ShouldFail = shouldFail;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal bool ShouldFail { get; }
        internal string Diagnostic { get; }

        internal static GameFlowDiagnosticFaultDecision None => default;
        internal static GameFlowDiagnosticFaultDecision Fail(string diagnostic) =>
            new GameFlowDiagnosticFaultDecision(true, diagnostic);
    }
}
