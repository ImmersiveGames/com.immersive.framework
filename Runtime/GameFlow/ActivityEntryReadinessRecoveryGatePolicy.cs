using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Identity;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-READY-04 recovery capability blocker policy for a committed Activity destination.")]
    internal static class ActivityEntryReadinessRecoveryGatePolicy
    {
        internal const string PolicySource = "IF-READY-04.ActivityEntryReadinessRecovery";

        internal static GateSnapshot Create(
            ActivityReadinessOccurrence occurrence,
            FrameworkIdentityKey owner,
            string source,
            string reason)
        {
            if (!occurrence.IsValid)
            {
                throw new System.ArgumentException(
                    "Activity entry-readiness recovery gate requires a valid occurrence.",
                    nameof(occurrence));
            }

            if (!owner.IsValid || owner.Domain != FrameworkIdentityDomain.Activity)
            {
                throw new System.ArgumentException(
                    "Activity entry-readiness recovery gate requires a valid Activity owner identity.",
                    nameof(owner));
            }

            string diagnosticReason =
                $"Activity entry readiness recovery is required for activity='{occurrence.Activity.ActivityName}' occurrence='{occurrence.TransitionSequence}'. {reason}";
            var blockers = new List<GateBlocker>(3)
            {
                GateBlocker.ForOwner(
                    "activity-entry-readiness-recovery-input",
                    GateScope.Input,
                    GateDomain.InputAcceptance,
                    owner,
                    source,
                    diagnosticReason,
                    PolicySource),
                GateBlocker.ForOwner(
                    "activity-entry-readiness-recovery-interaction",
                    GateScope.Interaction,
                    GateDomain.InteractionAcceptance,
                    owner,
                    source,
                    diagnosticReason,
                    PolicySource),
                GateBlocker.ForOwner(
                    "activity-entry-readiness-recovery-gameplay",
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
