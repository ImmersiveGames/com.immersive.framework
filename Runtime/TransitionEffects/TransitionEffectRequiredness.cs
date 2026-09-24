using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Requiredness of one Transition Effect request.
    /// Requiredness is data for diagnostics and future authoring policy; F19B does not create effect discovery or fallback.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B Transition Effect requiredness primitive; no authoring policy or fallback runtime.")]
    public enum TransitionEffectRequiredness
    {
        /// <summary>Invalid default value. Do not use for canonical effect requests.</summary>
        Unknown = 0,

        Optional = 10,
        Required = 20
    }
}
