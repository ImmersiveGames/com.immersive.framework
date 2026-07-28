using System;
using Immersive.Framework.GameFlow.Diagnostics;

namespace Immersive.Framework.Editor.Diagnostics.GameFlow
{
    public sealed class FrameworkGameFlowDiagnosticFaultLease : IDisposable
    {
        private readonly Action<FrameworkGameFlowDiagnosticFaultLease> release;

        internal FrameworkGameFlowDiagnosticFaultLease(
            FrameworkGameFlowDiagnosticFaultScenario scenario,
            GameFlowDiagnosticFaultCheckpoint expectedCheckpoint,
            string leaseId,
            Action<FrameworkGameFlowDiagnosticFaultLease> release)
        {
            Scenario = scenario;
            ExpectedCheckpoint = expectedCheckpoint.ToString();
            LeaseId = leaseId;
            this.release = release;
        }

        public FrameworkGameFlowDiagnosticFaultScenario Scenario { get; }
        public string LeaseId { get; }
        public string ExpectedCheckpoint { get; }
        public bool Consumed { get; private set; }
        public int ConsumptionCount { get; private set; }
        public string ActualCheckpoint { get; private set; } = string.Empty;
        public bool Released { get; private set; }
        public string Diagnostic { get; private set; } = string.Empty;
        public FrameworkGameFlowDiagnosticFaultReport Report { get; private set; }

        internal void MarkConsumed(GameFlowDiagnosticFaultRequest request, string diagnostic)
        {
            Consumed = true;
            ConsumptionCount++;
            ActualCheckpoint = request.Checkpoint.ToString();
            Diagnostic = diagnostic ?? string.Empty;
            Report = new FrameworkGameFlowDiagnosticFaultReport(
                Diagnostic, request.Operation, request.Transaction, request.Slot);
        }

        internal void MarkReleased() => Released = true;

        public void Dispose()
        {
            if (!Released) release?.Invoke(this);
        }
    }
}
