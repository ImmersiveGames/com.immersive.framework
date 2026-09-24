namespace Immersive.Framework.GameFlow.Diagnostics
{
    internal readonly struct GameFlowDiagnosticFaultRequest
    {
        internal GameFlowDiagnosticFaultRequest(
            GameFlowDiagnosticFaultCheckpoint checkpoint,
            string operation,
            string transaction,
            string slot)
        {
            Checkpoint = checkpoint;
            Operation = operation ?? string.Empty;
            Transaction = transaction ?? string.Empty;
            Slot = slot ?? string.Empty;
        }

        internal GameFlowDiagnosticFaultCheckpoint Checkpoint { get; }
        internal string Operation { get; }
        internal string Transaction { get; }
        internal string Slot { get; }
    }
}
