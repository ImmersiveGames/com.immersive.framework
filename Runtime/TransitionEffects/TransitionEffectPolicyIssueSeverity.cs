using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Severity for Transition Effect policy/authoring issues.
    /// This is diagnostics data only; it does not own Transition execution or adapter discovery.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19E Transition Effect policy issue severity; diagnostics and authoring guardrails only.")]
    public enum TransitionEffectPolicyIssueSeverity
    {
        /// <summary>Invalid default value.</summary>
        Unknown = 0,

        Info = 10,
        Warning = 20,
        Blocking = 30
    }
}
