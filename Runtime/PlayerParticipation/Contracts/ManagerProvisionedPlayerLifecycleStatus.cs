namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Consumer-safe lifecycle stage for one Manager-Provisioned Player flow.
    /// This status is diagnostic only and does not grant mutation authority.
    /// </summary>
    public enum ManagerProvisionedPlayerLifecycleStatus
    {
        Unavailable = 0,
        WaitingForActivity = 10,
        WaitingForJoin = 20,
        WaitingForActorSelection = 30,
        PreparingLogicalActor = 40,
        MaterializingPhysicalActor = 50,
        PreparingGameplayAdmission = 60,
        Ready = 70,
        Failed = 100,
        Released = 110
    }
}
