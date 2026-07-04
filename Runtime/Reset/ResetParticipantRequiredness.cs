using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Participant-level requiredness for reset execution.
    /// Requiredness deliberately belongs to participants, not subjects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset participant requiredness.")]
    public enum ResetParticipantRequiredness
    {
        Unknown = 0,
        Required = 10,
        Optional = 20
    }
}
