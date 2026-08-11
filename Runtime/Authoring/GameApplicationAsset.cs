using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.ProgressionSave;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Stable. Public authoring root for one Immersive game/application.
    ///
    /// The asset owns application-level intent only. Mutable Session, Player, Route, Activity,
    /// Camera, Progression Save and scene runtime state remain outside this asset.
    /// Project-level frame pacing is owned by Project Settings > Immersive Framework.
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
        [Tooltip("Enables application-scoped Progression Save composition. When enabled, Default Progression Save Profile is required and materialized once during framework boot.")]
        private bool progressionSaveEnabled;

        [SerializeField]
        [Tooltip("Authored backend intent used to materialize the application-scoped Progression Save Runtime. Runtime store state remains outside this asset.")]
        private ProgressionSaveProfile defaultProgressionSaveProfile;

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

        public bool PlayerSessionEnabled => playerSessionEnabled;

        public PlayerSessionProfile DefaultPlayerSessionProfile =>
            defaultPlayerSessionProfile;

        public PlayerActorSelectionDuplicatePolicy PlayerActorSelectionDuplicatePolicy =>
            playerActorSelectionDuplicatePolicy;

        public bool HasDefinedPlayerActorSelectionDuplicatePolicy =>
            playerActorSelectionDuplicatePolicy.IsDefinedPolicy();

        /// <summary>
        /// Enables Progression Save application composition. Disabled is an explicit
        /// valid absence of a Progression Save runtime.
        /// </summary>
        public bool ProgressionSaveEnabled => progressionSaveEnabled;

        /// <summary>
        /// Reusable authored backend intent. Required only when
        /// ProgressionSaveEnabled is true.
        /// </summary>
        public ProgressionSaveProfile DefaultProgressionSaveProfile =>
            defaultProgressionSaveProfile;

        public PersistentContentComposition PersistentContent =>
            persistentContent;

        public bool HasPersistentContentComposition =>
            persistentContent != null &&
            persistentContent.IsComplete;

        public FrameworkValidationMode ValidationMode =>
            validationMode;
    }
}
