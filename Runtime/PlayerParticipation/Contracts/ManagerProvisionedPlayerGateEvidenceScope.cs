namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Declares exactly which runtime authority produced GateHeld evidence.
    /// A Player readiness contribution is not equivalent to the aggregate
    /// Activity gate across all readiness participants.
    /// </summary>
    public enum ManagerProvisionedPlayerGateEvidenceScope
    {
        None = 0,
        ActivityPlayerReadinessContribution = 10,
        ActivityGateAggregate = 20
    }
}
