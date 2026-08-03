using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "IF-READY-04 recovery capability blocker policy for a committed Activity destination.")]
    internal static class ActivityEntryReadinessRecoveryGatePolicy
    {
        internal const string PolicySource = "IF-READY-04.ActivityEntryReadinessRecovery";

        internal static GateSnapshot Create(
            ActivityReadinessOccurrence occurrence,
            string source,
            string reason)
        {
            if (!occurrence.IsValid)
            {
                return GateSnapshot.Empty();
            }

            string diagnosticReason =
                $"Activity entry readiness recovery is required for activity='{occurrence.Activity.ActivityName}' occurrence='{occurrence.TransitionSequence}'. {reason}";
            var blockers = new List<GateBlocker>(3)
            {
                GateBlocker.ForAnyOwner(
                    "activity-entry-readiness-recovery-input",
                    GateScope.Input,
                    GateDomain.InputAcceptance,
                    source,
                    diagnosticReason,
                    PolicySource),
                GateBlocker.ForAnyOwner(
                    "activity-entry-readiness-recovery-interaction",
                    GateScope.Interaction,
                    GateDomain.InteractionAcceptance,
                    source,
                    diagnosticReason,
                    PolicySource),
                GateBlocker.ForAnyOwner(
                    "activity-entry-readiness-recovery-gameplay",
                    GateScope.Gameplay,
                    GateDomain.GameplayAction,
                    source,
                    diagnosticReason,
                    PolicySource)
            };

            return new GateSnapshot(blockers);
        }
    }
}
