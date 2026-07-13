using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Public authoring asset for a gameplay Activity.
    /// Activity participation intent is explicit through separate Projection and Requirements Profiles.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Activity",
        menuName = "Immersive Framework/Activity",
        order = 20)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable activity name shown in framework diagnostics. If empty, the asset name is used.")]
        private string activityName = "Activity";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for the activity. This has no runtime behavior yet.")]
        private string description = string.Empty;

        [SerializeField]
        [Tooltip("Mandatory reusable Profile selecting which Session Player Slots this Activity projects. Null is invalid and never means a permissive default.")]
        private ActivityParticipationProjectionProfile playerParticipationProjectionProfile;

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

        public string ActivityName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(activityName))
                {
                    return activityName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "Activity";
            }
        }

        public string Description => description ?? string.Empty;

        public ActivityParticipationProjectionProfile PlayerParticipationProjectionProfile =>
            playerParticipationProjectionProfile;

        public PlayerParticipationRequirementsProfile PlayerParticipationRequirementsProfile =>
            playerParticipationRequirementsProfile;

        public bool HasPlayerParticipationConfiguration =>
            playerParticipationProjectionProfile != null &&
            playerParticipationRequirementsProfile != null;

        public bool TryGetPlayerParticipationProjectionDescriptor(
            out ActivityParticipationProjectionDescriptor descriptor,
            out string issue)
        {
            if (playerParticipationProjectionProfile == null)
            {
                descriptor = default;
                issue = $"Activity '{ActivityName}' requires an explicit Activity Participation Projection Profile.";
                return false;
            }

            return playerParticipationProjectionProfile.TryCreateDescriptor(out descriptor, out issue);
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
