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
        private ActivityTransitionPreparationContext _currentActivityRelocationContext;
        private readonly Dictionary<PlayerSlotId, ActivityPlayerRelocationEvidence>
            _activityRelocationEvidenceBySlot = new Dictionary<PlayerSlotId, ActivityPlayerRelocationEvidence>();

        internal bool TryConfigureActivityRelocationContext(
            ActivityTransitionPreparationContext context, out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !context.IsValid || !context.Activity.HasDefinedPlayerRelocationPolicy)
            {
                issue = "Activity Player relocation requires a ready Player preparation module, a valid target occurrence and a defined policy.";
                return false;
            }

            _currentActivityRelocationContext = context;
            _activityRelocationEvidenceBySlot.Clear();
            if (context.Activity.PlayerRelocationPolicy == ActivityPlayerRelocationPolicy.NoRelocation)
                return true;

            PlayerParticipationSnapshot snapshot = _participationContext.CreateSnapshot();
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
            if (!_currentActivityRelocationContext.IsValid ||
                _currentActivityRelocationContext.Owner != owner ||
                _currentActivityRelocationContext.Activity.PlayerRelocationPolicy ==
                    ActivityPlayerRelocationPolicy.NoRelocation)
                return true;

            if (_currentActivityRelocationContext.Activity.PlayerRelocationPolicy !=
                ActivityPlayerRelocationPolicy.ApplyExplicitRelocation ||
                !playerSlotId.IsValid || !preparationToken.IsValid ||
                !TryGetPreparedPhysicalEvidence(playerSlotId, preparationToken,
                    out _, out _, out _, out PlayerActorMaterializationHandle handle, out issue))
                return false;

            Transform target = handle.Presentation != null ? handle.Presentation.transform : null;
            if (target == null)
            {
                issue = "Activity Player relocation requires a complete prepared physical Actor target.";
                return false;
            }

            if (_activityRelocationEvidenceBySlot.TryGetValue(playerSlotId, out ActivityPlayerRelocationEvidence previous) &&
                previous.IsApplied && previous.Owner == _currentActivityRelocationContext.Owner &&
                previous.Occurrence.Matches(_currentActivityRelocationContext.Activity,
                    _currentActivityRelocationContext.Occurrence.TransitionSequence))
                return true;

            if (!ActivityPlayerRelocationRuntime.TryApply(
                    _currentActivityRelocationContext, playerSlotId, handle.Request.ActorId,
                    handle.Request.RuntimeContentIdentity.StableText, target,
                    out ActivityPlayerRelocationEvidence evidence, out issue))
            {
                _activityRelocationEvidenceBySlot.Remove(playerSlotId);
                return false;
            }

            _activityRelocationEvidenceBySlot[playerSlotId] = evidence;
            return true;
        }

        internal bool TryPreflightCurrentActivityRelocation(
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            out string issue)
        {
            issue = string.Empty;
            if (!_currentActivityRelocationContext.IsValid ||
                _currentActivityRelocationContext.Owner != owner)
            {
                issue =
                    "Activity Player relocation preflight requires the current exact Activity occurrence context.";
                return false;
            }

            if (_currentActivityRelocationContext.Activity
                    .PlayerRelocationPolicy ==
                ActivityPlayerRelocationPolicy.NoRelocation)
            {
                return true;
            }

            return ActivityPlayerRelocationRuntime.TryPreflight(
                _currentActivityRelocationContext,
                playerSlotId,
                out issue);
        }
    }
}
