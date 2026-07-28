using System;
using System.Collections.Generic;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Typed, cumulative evidence used to satisfy an Activity Player readiness requirement.
    /// </summary>
    public enum PlayerParticipationReadinessEvidence
    {
        JoinedSlot = 10,
        SelectedActor = 20,
        LogicalActorPrepared = 30,
        GameplayInputEligibility = 40,
        GameplayCameraEligibility = 50,
        GameplayActionEligibility = 60
    }

    /// <summary>
    /// Canonical cumulative definition of Activity Player readiness. Editor tooling and
    /// runtime diagnostics consume this definition without duplicating level semantics.
    /// </summary>
    public static class PlayerParticipationReadinessRequirements
    {
        private static readonly PlayerParticipationReadinessEvidence[] None = Array.Empty<PlayerParticipationReadinessEvidence>();
        private static readonly PlayerParticipationReadinessEvidence[] Joined = { PlayerParticipationReadinessEvidence.JoinedSlot };
        private static readonly PlayerParticipationReadinessEvidence[] Selected = { PlayerParticipationReadinessEvidence.JoinedSlot, PlayerParticipationReadinessEvidence.SelectedActor };
        private static readonly PlayerParticipationReadinessEvidence[] Prepared = { PlayerParticipationReadinessEvidence.JoinedSlot, PlayerParticipationReadinessEvidence.SelectedActor, PlayerParticipationReadinessEvidence.LogicalActorPrepared };
        private static readonly PlayerParticipationReadinessEvidence[] Gameplay = { PlayerParticipationReadinessEvidence.JoinedSlot, PlayerParticipationReadinessEvidence.SelectedActor, PlayerParticipationReadinessEvidence.LogicalActorPrepared, PlayerParticipationReadinessEvidence.GameplayInputEligibility, PlayerParticipationReadinessEvidence.GameplayCameraEligibility, PlayerParticipationReadinessEvidence.GameplayActionEligibility };

        public static IReadOnlyList<PlayerParticipationReadinessEvidence> GetRequiredEvidence(PlayerParticipationRequirementLevel level)
        {
            return level switch
            {
                PlayerParticipationRequirementLevel.None => None,
                PlayerParticipationRequirementLevel.JoinedSlots => Joined,
                PlayerParticipationRequirementLevel.SelectedActors => Selected,
                PlayerParticipationRequirementLevel.LogicalActorsPrepared => Prepared,
                PlayerParticipationRequirementLevel.GameplayReady => Gameplay,
                _ => None
            };
        }

        public static string GetDisplayName(PlayerParticipationReadinessEvidence evidence)
        {
            return evidence switch
            {
                PlayerParticipationReadinessEvidence.JoinedSlot => "Joined Slot",
                PlayerParticipationReadinessEvidence.SelectedActor => "Selected Actor",
                PlayerParticipationReadinessEvidence.LogicalActorPrepared => "Logical Actor Prepared",
                PlayerParticipationReadinessEvidence.GameplayInputEligibility => "Gameplay Input Eligibility",
                PlayerParticipationReadinessEvidence.GameplayCameraEligibility => "Gameplay Camera Eligibility",
                PlayerParticipationReadinessEvidence.GameplayActionEligibility => "Gameplay Action Eligibility",
                _ => "Unknown Evidence"
            };
        }
    }
}
