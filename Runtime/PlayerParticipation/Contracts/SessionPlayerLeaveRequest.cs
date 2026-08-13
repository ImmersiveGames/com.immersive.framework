using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Product-authorized intent to end one exact joined Session Player occurrence.
    /// Stable Slot identity alone is insufficient because the Slot may be reused after Leave.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-020 exact Session Player Leave request correlated by Slot and occurrence revision.")]
    public readonly struct SessionPlayerLeaveRequest
    {
        public SessionPlayerLeaveRequest(
            PlayerSlotId playerSlotId,
            int expectedOccurrenceRevision,
            string source,
            string reason)
        {
            PlayerSlotId = playerSlotId;
            ExpectedOccurrenceRevision = expectedOccurrenceRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }
        public int ExpectedOccurrenceRevision { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => TryValidate(out _);

        public bool TryValidate(out string issue)
        {
            if (!PlayerSlotId.IsValid)
            {
                issue = "Session Player Leave requires a valid target Player Slot identity.";
                return false;
            }

            if (ExpectedOccurrenceRevision < 0)
            {
                issue = "Session Player Leave requires a non-negative expected occurrence revision.";
                return false;
            }

            if (string.IsNullOrEmpty(Source))
            {
                issue = "Session Player Leave request requires a non-empty source.";
                return false;
            }

            if (string.IsNullOrEmpty(Reason))
            {
                issue = "Session Player Leave request requires a non-empty reason.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public string ToDiagnosticString()
        {
            return
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"expectedOccurrenceRevision='{ExpectedOccurrenceRevision}' " +
                $"source='{Source}' reason='{Reason}'";
        }
    }
}
