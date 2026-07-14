using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 prepared Player camera eligibility state.")]
    public enum PlayerGameplayCameraEligibilityState
    {
        None = 0,
        NotEvaluated = 10,
        SkippedOptional = 20,
        Eligible = 30
    }
}
