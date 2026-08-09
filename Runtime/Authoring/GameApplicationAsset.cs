using Immersive.Framework.ApiStatus;
using Immersive.Framework.Performance;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Stable. Public authoring root for one Immersive game/application.
    ///
    /// The asset owns application-level intent only. Mutable Session, Player, Route, Activity,
    /// Camera and scene runtime state remain outside this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameApplication",
        menuName = "Immersive Framework/Game Application",
        order = 0)]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public sealed class GameApplicationAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable name shown in framework diagnostics. If empty, the asset name is used.")]
        private string applicationName = "Game Application";

        [SerializeField]
        [Tooltip("First Route requested by Game Flow after framework boot. The Route declares the first Primary Scene.")]
        private RouteAsset startupRoute;

        [SerializeField]
        [Tooltip("Enables the authored Player Session. When enabled, Default Player Session Profile is required and resolved once at Session creation.")]
        private bool playerSessionEnabled;

        [SerializeField]
        [Tooltip("Authored default used to create the Player Session when Player Session is enabled. Runtime uses only its resolved immutable configuration.")]
        private PlayerSessionProfile defaultPlayerSessionProfile;

        [SerializeField]
        [Tooltip("Session duplicate-selection rule for ActorProfile selection across joined local Player Slots. Runtime selection state remains outside this asset.")]
        private PlayerActorSelectionDuplicatePolicy playerActorSelectionDuplicatePolicy =
            PlayerActorSelectionDuplicatePolicy.AllowDuplicates;

        [SerializeField]
        [Tooltip("Application-level frame pacing intent applied once during framework boot. Use Unity Defaults preserves current project and platform behavior.")]
        private ApplicationFrameRatePolicy frameRatePolicy =
            new ApplicationFrameRatePolicy();

        [SerializeField]
        [Tooltip("Concrete scene composition retained for the application lifetime. The scene is authored manually; the framework validates and consumes it without creating or repairing content.")]
        private PersistentContentComposition persistentContent =
            new PersistentContentComposition();

        [SerializeField]
        [Tooltip("Controls validation and diagnostics severity. Required configuration fails in every mode; Strict promotes warnings, Standard keeps them, Release suppresses info diagnostics.")]
        private FrameworkValidationMode validationMode =
            FrameworkValidationMode.Standard;

        public string ApplicationName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(applicationName))
                {
                    return applicationName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name)
                    ? name
                    : "Game Application";
            }
        }

        public RouteAsset StartupRoute => startupRoute;

        /// <summary>
        /// Enables Player Session composition. Disabled is an explicit valid absence of a Player Session.
        /// </summary>
        public bool PlayerSessionEnabled => playerSessionEnabled;

        /// <summary>
        /// Default authored Player Session intent. It is required only when PlayerSessionEnabled is true.
        /// </summary>
        public PlayerSessionProfile DefaultPlayerSessionProfile => defaultPlayerSessionProfile;

        /// <summary>
        /// Session duplicate-selection policy composed into PlayerParticipationRuntimeContext.
        /// This asset is the single authoring authority and never stores current Slot selections.
        /// </summary>
        public PlayerActorSelectionDuplicatePolicy PlayerActorSelectionDuplicatePolicy =>
            playerActorSelectionDuplicatePolicy;

        public bool HasDefinedPlayerActorSelectionDuplicatePolicy =>
            playerActorSelectionDuplicatePolicy.IsDefinedPolicy();

        /// <summary>
        /// Application-level frame pacing intent. Runtime values are applied by FrameworkRuntimeHost.
        /// </summary>
        public ApplicationFrameRatePolicy FrameRatePolicy =>
            frameRatePolicy;

        public PersistentContentComposition PersistentContent =>
            persistentContent;

        public bool HasPersistentContentComposition =>
            persistentContent != null &&
            persistentContent.IsComplete;

        public FrameworkValidationMode ValidationMode =>
            validationMode;

    }
}
