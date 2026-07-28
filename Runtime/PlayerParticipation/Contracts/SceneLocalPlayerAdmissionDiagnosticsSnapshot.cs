using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Immutable host-scoped diagnostic projection of the last Scene-Provided Player operation.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "PLAYER-DIAG-1 persistent Scene-Provided Player admission diagnostics.")]
    public readonly struct SceneLocalPlayerAdmissionDiagnosticsSnapshot
    {
        internal SceneLocalPlayerAdmissionDiagnosticsSnapshot(
            string operation,
            SceneLocalPlayerAdmissionRuntimeStatus status,
            string source,
            string reason,
            string message,
            bool hadActiveAdmission,
            bool releaseRequested,
            bool releaseSucceeded,
            bool alreadyReleased,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            bool hostEvidencePresent,
            bool tokenValid,
            int activeAdmissionCount,
            int occupiedSlotCount)
        {
            Operation = operation.NormalizeText();
            Status = status;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
            HadActiveAdmission = hadActiveAdmission;
            ReleaseRequested = releaseRequested;
            ReleaseSucceeded = releaseSucceeded;
            AlreadyReleased = alreadyReleased;
            PlayerSlotId = playerSlotId;
            ActorId = actorId;
            HostEvidencePresent = hostEvidencePresent;
            TokenValid = tokenValid;
            ActiveAdmissionCount = activeAdmissionCount;
            OccupiedSlotCount = occupiedSlotCount;
        }

        public string Operation { get; }
        public SceneLocalPlayerAdmissionRuntimeStatus Status { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool HadActiveAdmission { get; }
        public bool ReleaseRequested { get; }
        public bool ReleaseSucceeded { get; }
        public bool AlreadyReleased { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorId ActorId { get; }
        public bool HostEvidencePresent { get; }
        public bool TokenValid { get; }
        public int ActiveAdmissionCount { get; }
        public int OccupiedSlotCount { get; }
        public bool HasOperation => !string.IsNullOrWhiteSpace(Operation);

        public string ToDiagnosticString() =>
            $"operation='{Operation}' status='{Status}' source='{Source}' reason='{Reason}' " +
            $"message='{Message}' hadActiveAdmission='{HadActiveAdmission}' " +
            $"releaseRequested='{ReleaseRequested}' releaseSucceeded='{ReleaseSucceeded}' " +
            $"alreadyReleased='{AlreadyReleased}' slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : "<none>")}' " +
            $"actor='{(ActorId.IsValid ? ActorId.StableText : "<none>")}' " +
            $"hostEvidencePresent='{HostEvidencePresent}' tokenValid='{TokenValid}' " +
            $"activeAdmissionCount='{ActiveAdmissionCount}' occupiedSlotCount='{OccupiedSlotCount}'";

        internal static SceneLocalPlayerAdmissionDiagnosticsSnapshot Empty(string message) =>
            new SceneLocalPlayerAdmissionDiagnosticsSnapshot(
                "None",
                SceneLocalPlayerAdmissionRuntimeStatus.None,
                string.Empty,
                string.Empty,
                message,
                false,
                false,
                false,
                false,
                default,
                default,
                false,
                false,
                0,
                0);
    }
}
