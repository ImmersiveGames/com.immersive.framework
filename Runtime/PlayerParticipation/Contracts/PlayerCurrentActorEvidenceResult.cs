using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-3 immutable current Logical Player Actor evidence result.")]
    public sealed class PlayerCurrentActorEvidenceResult
    {
        internal PlayerCurrentActorEvidenceResult(
            PlayerCurrentActorEvidenceStatus status,
            string operation,
            PlayerActorCorrelationEvidence retainedEvidence,
            PlayerActorPreparationSummary preparation,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            RetainedEvidence = retainedEvidence;
            Preparation = preparation;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PlayerCurrentActorEvidenceStatus Status { get; }
        public string Operation { get; }
        public PlayerActorCorrelationEvidence RetainedEvidence { get; }
        public PlayerActorPreparationSummary Preparation { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool Succeeded =>
            Status == PlayerCurrentActorEvidenceStatus.SucceededCurrent;
        public bool HasRetainedEvidence => RetainedEvidence.IsValid;

        public string ToDiagnosticString()
        {
            return
                $"operation='{Operation}' status='{Status}' " +
                $"evidence=({(HasRetainedEvidence ? RetainedEvidence.ToDiagnosticString() : string.Empty)}) " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }
    }
}
