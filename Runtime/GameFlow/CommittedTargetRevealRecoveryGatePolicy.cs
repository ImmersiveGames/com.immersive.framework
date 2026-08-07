using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Identity;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// IF-TXN-01 recovery capability blockers for a committed destination whose Transition After / reveal failed.
    /// Reuses the committed-target recovery model without classifying the failure as readiness failure.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-TXN-01 recovery capability blocker policy for committed-target reveal failure.")]
    internal static class CommittedTargetRevealRecoveryGatePolicy
    {
        internal const string PolicySource = "IF-TXN-01.CommittedTargetRevealRecovery";

        internal static GateSnapshot Create(
            ActivityReadinessOccurrence occurrence,
            FrameworkIdentityKey owner,
            string source,
            string reason)
        {
            if (!occurrence.IsValid)
            {
                throw new System.ArgumentException(
                    "Committed-target reveal recovery gate requires a valid occurrence.",
                    nameof(occurrence));
            }

            if (!owner.IsValid || owner.Domain != FrameworkIdentityDomain.Activity)
            {
                throw new System.ArgumentException(
                    "Committed-target reveal recovery gate requires a valid Activity owner identity.",
                    nameof(owner));
            }

            string diagnosticReason =
                $"Committed-target Transition After/reveal recovery is required for activity='{occurrence.Activity.ActivityName}' occurrence='{occurrence.TransitionSequence}'. {reason}";
            var blockers = new List<GateBlocker>(3)
            {
                GateBlocker.ForOwner(
                    "committed-target-reveal-recovery-input",
                    GateScope.Input,
                    GateDomain.InputAcceptance,
                    owner,
                    source,
                    diagnosticReason,
                    PolicySource),
                GateBlocker.ForOwner(
                    "committed-target-reveal-recovery-interaction",
                    GateScope.Interaction,
                    GateDomain.InteractionAcceptance,
                    owner,
                    source,
                    diagnosticReason,
                    PolicySource),
                GateBlocker.ForOwner(
                    "committed-target-reveal-recovery-gameplay",
                    GateScope.Gameplay,
                    GateDomain.GameplayAction,
                    owner,
                    source,
                    diagnosticReason,
                    PolicySource)
            };

            return new GateSnapshot(blockers);
        }
    }
}
