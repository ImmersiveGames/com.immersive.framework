using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Occurrence-scoped reason exposed by the package-owned Player readiness contribution.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-M07-10 Manager-Provisioned Player Activity readiness reason.")]
    public enum ActivityPlayerActorReadinessReason
    {
        None = 0,
        WaitingForJoin = 10,
        WaitingForActorSelection = 20,
        PreparingLogicalActor = 30,
        PreparingGameplayAdmission = 40,
        RequirementSatisfied = 50,
        Failed = 60,
        Released = 70
    }
}
