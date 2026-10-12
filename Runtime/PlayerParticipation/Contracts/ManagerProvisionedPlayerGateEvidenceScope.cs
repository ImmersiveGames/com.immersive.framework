using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Declares exactly which runtime authority produced GateHeld evidence.
    /// A Player readiness contribution is not equivalent to the aggregate
    /// Activity gate across all readiness participants.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-015 Manager-Provisioned Player lifecycle diagnostic evidence; outside IF-ADR-046 Stable Join outcome.")]
    public enum ManagerProvisionedPlayerGateEvidenceScope
    {
        None = 0,
        ActivityPlayerReadinessContribution = 10,
        ActivityGateAggregate = 20
    }
}
