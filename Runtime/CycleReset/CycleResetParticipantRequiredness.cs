using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Requiredness metadata for one Cycle Reset participant.
    /// Required failures block the aggregate reset; optional failures complete with warnings.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant requiredness primitive.")]
    public enum CycleResetParticipantRequiredness
    {
        /// <summary>
        /// Invalid default value. Participants must explicitly choose Required or Optional.
        /// </summary>
        Unknown = 0,

        Required = 10,
        Optional = 20
    }
}
