using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal static class ActivityPlayerParticipationProjectionResolver
    {
        internal static bool TryResolve(
            ActivityAsset activity,
            PlayerParticipationRuntimeContext participationContext,
            out PlayerParticipationRequirementLevel requirementLevel,
            out List<PlayerSlotRuntimeSnapshot> projectedSlots,
            out string issue)
        {
            requirementLevel = PlayerParticipationRequirementLevel.None;
            projectedSlots = new List<PlayerSlotRuntimeSnapshot>();
            issue = string.Empty;

            if (activity == null)
            {
                issue = "Activity Player projection requires an Activity.";
                return false;
            }

            if (participationContext == null)
            {
                issue = "Activity Player projection requires the Session participation authority.";
                return false;
            }

            if (!activity.TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out issue))
            {
                return false;
            }

            PlayerParticipationRequirementsProfile requirements =
                activity.PlayerParticipationRequirementsProfile;
            if (requirements == null ||
                !requirements.HasDefinedRequirementLevel)
            {
                issue =
                    $"Activity '{activity.ActivityName}' requires a valid Player Participation Requirements Profile.";
                return false;
            }

            requirementLevel = requirements.RequirementLevel;
            PlayerParticipationSnapshot session =
                participationContext.CreateSnapshot();
            if (session == null || !session.IsInitialized)
            {
                issue = "Session Player participation snapshot is unavailable.";
                return false;
            }

            if (descriptor.ProjectsNoSlots)
            {
                if (requirementLevel !=
                    PlayerParticipationRequirementLevel.None)
                {
                    issue =
                        $"Activity '{activity.ActivityName}' projects no Slots but requires '{requirementLevel}'.";
                    return false;
                }

                return true;
            }

            if (descriptor.ProjectsAllJoinedSlots)
            {
                for (int index = 0;
                     index < session.Slots.Count;
                     index++)
                {
                    PlayerSlotRuntimeSnapshot slot = session.Slots[index];
                    if (slot.IsJoined)
                    {
                        projectedSlots.Add(slot);
                    }
                }

                if (projectedSlots.Count == 0 &&
                    !descriptor.AllowsZeroParticipants)
                {
                    issue =
                        $"Activity '{activity.ActivityName}' rejects zero projected participants.";
                    return false;
                }

                return true;
            }

            if (!descriptor.ProjectsExplicitSlots)
            {
                issue =
                    $"Activity '{activity.ActivityName}' has unsupported projection mode '{descriptor.Mode}'.";
                return false;
            }

            for (int explicitIndex = 0;
                 explicitIndex < descriptor.ExplicitSlotProfiles.Count;
                 explicitIndex++)
            {
                PlayerSlotProfile profile =
                    descriptor.ExplicitSlotProfiles[explicitIndex];
                string profileIssue = string.Empty;
                PlayerSlotId expectedSlotId = default;
                if (profile == null ||
                    !profile.TryGetPlayerSlotId(
                        out expectedSlotId,
                        out profileIssue))
                {
                    issue = string.IsNullOrWhiteSpace(profileIssue)
                        ? $"Explicit projection entry '{explicitIndex}' is invalid."
                        : profileIssue;
                    return false;
                }

                bool found = false;
                for (int slotIndex = 0;
                     slotIndex < session.Slots.Count;
                     slotIndex++)
                {
                    PlayerSlotRuntimeSnapshot slot =
                        session.Slots[slotIndex];
                    if (slot.PlayerSlotId != expectedSlotId)
                    {
                        continue;
                    }

                    projectedSlots.Add(slot);
                    found = true;
                    break;
                }

                if (!found)
                {
                    issue =
                        $"Explicit projected Player Slot '{expectedSlotId.StableText}' is not configured in the Session.";
                    return false;
                }
            }

            return true;
        }
    }
}
