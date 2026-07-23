using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Public authoring asset for a gameplay Activity.
    /// Activity participation intent is explicit through Activity-owned Projection configuration and a Requirements Profile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Activity",
        menuName = "Immersive Framework/Activity",
        order = 20)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable functional identity. It must not be changed because the Activity asset, file or display name was renamed.")]
        private string activityId = string.Empty;

        [SerializeField]
        [Tooltip("Human-readable name shown in the Inspector and diagnostics. It is not runtime identity.")]
        private string activityName = "Activity";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for the activity. This has no runtime behavior yet.")]
        private string description = string.Empty;

        [SerializeField]
        [Tooltip("Selects which Session Player Slots this Activity projects.")]
        private ActivityParticipationProjectionMode playerParticipationProjectionMode =
            ActivityParticipationProjectionMode.NoSlots;

        [SerializeField]
        [Tooltip("Declares whether a dynamic projection may resolve to zero participating Slots.")]
        private ActivityParticipationZeroParticipantPolicy playerParticipationZeroParticipantPolicy =
            ActivityParticipationZeroParticipantPolicy.Allowed;

        [SerializeField]
        [Tooltip("Ordered Slot Profile references used only by Explicit Slots. Identity remains owned by each PlayerSlotProfile.")]
        private PlayerSlotProfile[] playerParticipationExplicitSlotProfiles =
            Array.Empty<PlayerSlotProfile>();

        [SerializeField]
        [Tooltip("Mandatory reusable Profile defining the readiness level required from projected Player Slots. Activities with no Players use an explicit None Profile.")]
        private PlayerParticipationRequirementsProfile playerParticipationRequirementsProfile;

        [SerializeField]
        [Tooltip("Optional Activity Content Profile. Declares Activity-owned scenes for composition and release by Activity operations.")]
        private ActivityContentProfileAsset activityContentProfile;

        [SerializeField]
        [Tooltip("Defines whether Activity operations use the session TransitionSurface and, for scene side-effects, the canonical LoadingSurface. Seamless/Fade/FadeWithLoading are all valid with Activity-owned scene load/release; they select presentation.")]
        private ActivityVisualTransitionMode visualTransitionMode = ActivityVisualTransitionMode.Seamless;

        [SerializeField]
        [Tooltip("Controls which requests/capabilities are blocked while this Activity transition is running. For Fade/FadeWithLoading, InputInteractionAndGameplay is recommended.")]
        private TransitionGateMode transitionGateMode = TransitionGateMode.LifecycleRequestsOnly;

        public ActivityId ActivityId
        {
            get
            {
                if (!HasValidActivityId)
                {
                    throw new System.InvalidOperationException("Activity ID is missing or invalid.");
                }

                return new ActivityId(activityId);
            }
        }

        public bool HasValidActivityId => global::Immersive.Framework.Authoring.ActivityId.IsValidText(activityId);

        public bool HasSameIdentity(ActivityAsset other) =>
            other != null &&
            HasValidActivityId &&
            other.HasValidActivityId &&
            ActivityId == other.ActivityId;

        public string ActivityName => activityName.NormalizeText();

        public string Description => description ?? string.Empty;

        public ActivityParticipationProjectionMode PlayerParticipationProjectionMode =>
            playerParticipationProjectionMode;

        public ActivityParticipationZeroParticipantPolicy PlayerParticipationZeroParticipantPolicy =>
            playerParticipationZeroParticipantPolicy;

        public IReadOnlyList<PlayerSlotProfile> PlayerParticipationExplicitSlotProfiles =>
            playerParticipationExplicitSlotProfiles ?? Array.Empty<PlayerSlotProfile>();

        public PlayerParticipationRequirementsProfile PlayerParticipationRequirementsProfile =>
            playerParticipationRequirementsProfile;

        public bool HasPlayerParticipationConfiguration =>
            Enum.IsDefined(
                typeof(ActivityParticipationProjectionMode),
                playerParticipationProjectionMode) &&
            Enum.IsDefined(
                typeof(ActivityParticipationZeroParticipantPolicy),
                playerParticipationZeroParticipantPolicy) &&
            playerParticipationRequirementsProfile != null;

        public bool TryGetPlayerParticipationProjectionDescriptor(
            out ActivityParticipationProjectionDescriptor descriptor,
            out string issue)
        {
            descriptor = default;

            if (!Enum.IsDefined(
                    typeof(ActivityParticipationProjectionMode),
                    playerParticipationProjectionMode))
            {
                issue = $"Activity '{ActivityName}' has an invalid Player participation Projection Mode.";
                return false;
            }

            if (!Enum.IsDefined(
                    typeof(ActivityParticipationZeroParticipantPolicy),
                    playerParticipationZeroParticipantPolicy))
            {
                issue = $"Activity '{ActivityName}' has an invalid Zero Participant Policy.";
                return false;
            }

            PlayerSlotProfile[] slots =
                playerParticipationExplicitSlotProfiles ?? Array.Empty<PlayerSlotProfile>();

            switch (playerParticipationProjectionMode)
            {
                case ActivityParticipationProjectionMode.NoSlots:
                    if (slots.Length != 0)
                    {
                        issue = $"Activity '{ActivityName}' uses NoSlots but contains {slots.Length} Explicit Slot reference(s).";
                        return false;
                    }

                    if (playerParticipationZeroParticipantPolicy !=
                        ActivityParticipationZeroParticipantPolicy.Allowed)
                    {
                        issue = $"Activity '{ActivityName}' uses NoSlots and therefore requires Zero Participants = Allowed.";
                        return false;
                    }
                    break;

                case ActivityParticipationProjectionMode.AllJoinedSlots:
                    if (slots.Length != 0)
                    {
                        issue = $"Activity '{ActivityName}' uses AllJoinedSlots but contains {slots.Length} Explicit Slot reference(s).";
                        return false;
                    }
                    break;

                case ActivityParticipationProjectionMode.ExplicitSlots:
                    if (slots.Length == 0)
                    {
                        issue = $"Activity '{ActivityName}' uses ExplicitSlots but has no PlayerSlotProfile references.";
                        return false;
                    }

                    if (playerParticipationZeroParticipantPolicy !=
                        ActivityParticipationZeroParticipantPolicy.Rejected)
                    {
                        issue = $"Activity '{ActivityName}' uses ExplicitSlots and therefore requires Zero Participants = Rejected.";
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
                    issue = $"Activity '{ActivityName}' Explicit Slots[{index}] is missing.";
                    return false;
                }

                if (!profileOwners.Add(slotProfile))
                {
                    issue = $"Activity '{ActivityName}' repeats PlayerSlotProfile '{slotProfile.name}' at Explicit Slots[{index}].";
                    return false;
                }

                if (!slotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string identityIssue))
                {
                    issue = identityIssue;
                    return false;
                }

                if (!identityOwners.Add(playerSlotId))
                {
                    issue = $"Activity '{ActivityName}' contains duplicate PlayerSlotId '{playerSlotId}' at Explicit Slots[{index}].";
                    return false;
                }
            }

            descriptor = new ActivityParticipationProjectionDescriptor(
                playerParticipationProjectionMode,
                playerParticipationZeroParticipantPolicy,
                slots);
            issue = string.Empty;
            return true;
        }

        public ActivityContentProfileAsset ActivityContentProfile => activityContentProfile;

        public bool HasActivityContentProfile => activityContentProfile != null;

        public bool HasActivityContentScenes => activityContentProfile != null && activityContentProfile.HasScenes;

        public ActivityVisualTransitionMode VisualTransitionMode
        {
            get
            {
                return System.Enum.IsDefined(typeof(ActivityVisualTransitionMode), visualTransitionMode)
                    ? visualTransitionMode
                    : ActivityVisualTransitionMode.Seamless;
            }
        }

        public TransitionGateMode TransitionGateMode
        {
            get
            {
                return System.Enum.IsDefined(typeof(TransitionGateMode), transitionGateMode)
                    ? transitionGateMode
                    : TransitionGateMode.LifecycleRequestsOnly;
            }
        }
    }
}
