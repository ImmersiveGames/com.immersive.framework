using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private ActivityTransitionPreparationContext currentActivityRelocationContext;
        private readonly Dictionary<PlayerSlotId, ActivityPlayerRelocationEvidence>
            activityRelocationEvidenceBySlot = new Dictionary<PlayerSlotId, ActivityPlayerRelocationEvidence>();

        internal bool TryConfigureActivityRelocationContext(
            ActivityTransitionPreparationContext context, out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !context.IsValid || !context.Activity.HasDefinedPlayerRelocationPolicy)
            {
                issue = "Activity Player relocation requires a ready Player preparation module, a valid target occurrence and a defined policy.";
                return false;
            }

            currentActivityRelocationContext = context;
            activityRelocationEvidenceBySlot.Clear();
            if (context.Activity.PlayerRelocationPolicy == ActivityPlayerRelocationPolicy.NoRelocation)
                return true;

            PlayerParticipationSnapshot snapshot = participationContext.CreateSnapshot();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined ||
                    !TryGetCurrentPreparation(slot.PlayerSlotId, out PlayerActorPreparationSummary preparation, out _))
                    continue;
                if (!TryApplyCurrentActivityRelocation(
                        context.Owner, slot.PlayerSlotId, preparation.Token, out issue))
                    return false;
            }
            return true;
        }

        internal bool TryApplyCurrentActivityRelocation(
            RuntimeContentOwner owner, PlayerSlotId playerSlotId,
            PlayerActorPreparationToken preparationToken, out string issue)
        {
            issue = string.Empty;
            if (!currentActivityRelocationContext.IsValid ||
                currentActivityRelocationContext.Owner != owner ||
                currentActivityRelocationContext.Activity.PlayerRelocationPolicy ==
                    ActivityPlayerRelocationPolicy.NoRelocation)
                return true;

            if (currentActivityRelocationContext.Activity.PlayerRelocationPolicy !=
                ActivityPlayerRelocationPolicy.ApplyExplicitRelocation ||
                !playerSlotId.IsValid || !preparationToken.IsValid ||
                !TryGetPreparedPhysicalEvidence(playerSlotId, preparationToken,
                    out _, out _, out _, out PlayerActorMaterializationHandle handle, out issue))
                return false;

            Transform declaration = handle.PlayerActorDeclaration != null
                ? handle.PlayerActorDeclaration.transform : null;
            Transform root = handle.LogicalActorHost != null ? handle.LogicalActorHost.transform : null;
            Transform target = declaration != null && root != null &&
                (ReferenceEquals(declaration, root) || declaration.IsChildOf(root)) ? root : declaration;
            if (target == null)
            {
                issue = "Activity Player relocation requires a complete prepared physical Actor target.";
                return false;
            }

            string representation = handle.Request.RuntimeContentIdentity.StableText;
            if (activityRelocationEvidenceBySlot.TryGetValue(playerSlotId, out ActivityPlayerRelocationEvidence previous) &&
                previous.IsApplied && previous.Owner == currentActivityRelocationContext.Owner &&
                previous.Occurrence.Matches(currentActivityRelocationContext.Activity,
                    currentActivityRelocationContext.Occurrence.TransitionSequence) &&
                previous.RepresentationIdentity == representation &&
                ReferenceEquals(previous.Target, target))
                return true;

            if (!ActivityPlayerRelocationRuntime.TryApply(
                    currentActivityRelocationContext, playerSlotId, handle.Request.ActorId,
                    representation, target, out ActivityPlayerRelocationEvidence evidence, out issue))
            {
                activityRelocationEvidenceBySlot.Remove(playerSlotId);
                return false;
            }

            activityRelocationEvidenceBySlot[playerSlotId] = evidence;
            return true;
        }
    }
}
