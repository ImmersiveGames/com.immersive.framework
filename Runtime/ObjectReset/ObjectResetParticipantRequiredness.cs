using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Requiredness metadata for one Object Reset participant.
    /// Required failures block the aggregate reset; optional failures complete with warnings in later executor cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant requiredness primitive.")]
    public enum ObjectResetParticipantRequiredness
    {
        /// <summary>
        /// Invalid default value. Participants must explicitly choose Required or Optional.
        /// </summary>
        Unknown = 0,

        Required = 10,
        Optional = 20
    }
}
