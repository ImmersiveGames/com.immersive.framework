using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable reusable policy that selects which Session Player Slots one Activity evaluates.
    /// Runtime Slot state and projection results remain outside this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActivityParticipationProjectionProfile",
        menuName = "Immersive Framework/Player/Activity Participation Projection Profile",
        order = 30)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3D immutable Activity Player participation projection Profile.")]
    public sealed class ActivityParticipationProjectionProfile : ScriptableObject
    {
        [Header("Designer")]
        [SerializeField] private string displayName = "Activity Participation — No Players";

        [TextArea(2, 6)]
        [SerializeField] private string description =
            "This Activity projects no Player Slots. Pair with an explicit None requirements Profile.";

        [Header("Projection")]
        [Tooltip("Selects which Session Slot set is evaluated by the Activity requirements Profile.")]
        [SerializeField] private ActivityParticipationProjectionMode projectionMode =
            ActivityParticipationProjectionMode.NoSlots;

        [Tooltip("Explicit zero-participant behavior. NoSlots requires Allowed; ExplicitSlots requires Rejected.")]
        [SerializeField] private ActivityParticipationZeroParticipantPolicy zeroParticipantPolicy =
            ActivityParticipationZeroParticipantPolicy.Allowed;

        [Tooltip("Ordered Slot Profile references used only by ExplicitSlots. Identity remains owned by each PlayerSlotProfile.")]
        [SerializeField] private PlayerSlotProfile[] explicitSlotProfiles = Array.Empty<PlayerSlotProfile>();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public ActivityParticipationProjectionMode ProjectionMode => projectionMode;

        public ActivityParticipationZeroParticipantPolicy ZeroParticipantPolicy => zeroParticipantPolicy;

        public IReadOnlyList<PlayerSlotProfile> ExplicitSlotProfiles =>
            explicitSlotProfiles ?? Array.Empty<PlayerSlotProfile>();

        public bool HasDefinedProjectionMode =>
            Enum.IsDefined(typeof(ActivityParticipationProjectionMode), projectionMode);

        public bool HasDefinedZeroParticipantPolicy =>
            Enum.IsDefined(typeof(ActivityParticipationZeroParticipantPolicy), zeroParticipantPolicy);

        public bool TryCreateDescriptor(
            out ActivityParticipationProjectionDescriptor descriptor,
            out string issue)
        {
            descriptor = default;

            if (!HasDefinedProjectionMode)
            {
                issue = $"Activity Participation Projection Profile '{name}' has an invalid Projection Mode.";
                return false;
            }

            if (!HasDefinedZeroParticipantPolicy)
            {
                issue = $"Activity Participation Projection Profile '{name}' has an invalid Zero Participant Policy.";
                return false;
            }

            PlayerSlotProfile[] slots = explicitSlotProfiles ?? Array.Empty<PlayerSlotProfile>();

            switch (projectionMode)
            {
                case ActivityParticipationProjectionMode.NoSlots:
                    if (slots.Length != 0)
                    {
                        issue = $"Projection Profile '{name}' uses NoSlots but contains {slots.Length} Explicit Slot reference(s). Remove them or select ExplicitSlots.";
                        return false;
                    }

                    if (zeroParticipantPolicy != ActivityParticipationZeroParticipantPolicy.Allowed)
                    {
                        issue = $"Projection Profile '{name}' uses NoSlots and therefore requires Zero Participant Policy = Allowed.";
                        return false;
                    }
                    break;

                case ActivityParticipationProjectionMode.AllJoinedSlots:
                    if (slots.Length != 0)
                    {
                        issue = $"Projection Profile '{name}' uses AllJoinedSlots but contains {slots.Length} Explicit Slot reference(s). Explicit references are not evaluated in this mode.";
                        return false;
                    }
                    break;

                case ActivityParticipationProjectionMode.ExplicitSlots:
                    if (slots.Length == 0)
                    {
                        issue = $"Projection Profile '{name}' uses ExplicitSlots but has no PlayerSlotProfile references.";
                        return false;
                    }

                    if (zeroParticipantPolicy != ActivityParticipationZeroParticipantPolicy.Rejected)
                    {
                        issue = $"Projection Profile '{name}' uses ExplicitSlots and therefore requires Zero Participant Policy = Rejected.";
                        return false;
                    }
                    break;
            }

            var profileOwners = new HashSet<PlayerSlotProfile>();
            var identityOwners = new HashSet<PlayerSlotId>();

            for (int index = 0; index < slots.Length; index++)
            {
                PlayerSlotProfile slotProfile = slots[index];
                if (slotProfile == null)
                {
                    issue = $"Projection Profile '{name}' Explicit Slots[{index}] is missing.";
                    return false;
                }

                if (!profileOwners.Add(slotProfile))
                {
                    issue = $"Projection Profile '{name}' repeats PlayerSlotProfile '{slotProfile.name}' at Explicit Slots[{index}].";
                    return false;
                }

                if (!slotProfile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string identityIssue))
                {
                    issue = identityIssue;
                    return false;
                }

                if (!identityOwners.Add(playerSlotId))
                {
                    issue = $"Projection Profile '{name}' contains duplicate PlayerSlotId '{playerSlotId}' at Explicit Slots[{index}].";
                    return false;
                }
            }

            descriptor = new ActivityParticipationProjectionDescriptor(
                projectionMode,
                zeroParticipantPolicy,
                slots);
            issue = string.Empty;
            return true;
        }
    }
}
