using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E immutable per-Slot Activity Player handoff group evidence.")]
    public sealed class ActivityPlayerHandoffGroupSlotSnapshot
    {
        internal ActivityPlayerHandoffGroupSlotSnapshot(
            PlayerSlotId playerSlotId,
            PlayerGameplayChainHandoffToken handoffToken,
            PlayerGameplayChainHandoffStatus lastStatus,
            bool began,
            bool committed,
            bool rolledBack,
            bool cleanupPending,
            string message)
        {
            PlayerSlotId = playerSlotId;
            HandoffToken = handoffToken;
            LastStatus = lastStatus;
            Began = began;
            Committed = committed;
            RolledBack = rolledBack;
            CleanupPending = cleanupPending;
            Message = message.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayChainHandoffToken HandoffToken { get; }
        public PlayerGameplayChainHandoffStatus LastStatus { get; }
        public bool Began { get; }
        public bool Committed { get; }
        public bool RolledBack { get; }
        public bool CleanupPending { get; }
        public string Message { get; }
        public string ToDiagnosticString() =>
            $"slot='{PlayerSlotId.StableText}' handoff='{HandoffToken.StableText}' " +
            $"status='{LastStatus}' began='{Began}' committed='{Committed}' " +
            $"rolledBack='{RolledBack}' cleanupPending='{CleanupPending}' message='{Message}'";
    }
}
