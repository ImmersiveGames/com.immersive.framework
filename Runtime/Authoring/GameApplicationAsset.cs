using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Policy for the canonical app/session scoped Unity UI scene.
    /// The Startup Route Primary Scene is prepared first; then Global UI is loaded additively,
    /// its roots are persisted under the FrameworkRuntimeHost, and Transition/Loading adapters are discovered from that scene.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24E canonical UIGlobal scene policy for GameApplication authoring.")]
    public enum GlobalUiScenePolicy
    {
        NoneConfigured = 0,
        Required = 1
    }

    /// <summary>
    /// API status: Experimental. Public authoring root retained as the baseline entry point before F1 identity/status hardening.
    /// Public root asset for an Immersive game/application.
    /// Keep this asset small: it should grow only when a real framework cut needs a new decision.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameApplication",
        menuName = "Immersive Framework/Game Application",
        order = 0)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class GameApplicationAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable name shown in framework diagnostics. If empty, the asset name is used.")]
        private string applicationName = "Game Application";

        [SerializeField]
        [Tooltip("First route requested by the Game Flow after framework boot. The Route declares the Primary Scene loaded by Scene Lifecycle.")]
        private RouteAsset startupRoute;

        [SerializeField]
        [Tooltip("Ordered local Player participation seats. Array order is the canonical default allocation order; Profile Display Order is presentation metadata only.")]
        private PlayerSlotProfile[] localPlayerSlots = Array.Empty<PlayerSlotProfile>();

        [SerializeField]
        [Tooltip("Required Session policy for ActorProfile selection across joined local Player Slots. Runtime selection state remains outside this asset.")]
        private PlayerActorSelectionPolicyProfile playerActorSelectionPolicyProfile;

        [SerializeField]
        [Tooltip("Controls whether this Game Application uses a canonical app/session scoped UIGlobal scene. Required loads it additively after the Startup Route Primary Scene is prepared, persists its UI roots under the FrameworkRuntimeHost, and discovers Transition/Loading adapters from it.")]
        private GlobalUiScenePolicy globalUiScenePolicy = GlobalUiScenePolicy.NoneConfigured;

        [SerializeField]
        [Tooltip("Project-relative path of the canonical UIGlobal scene. Managed by the Game Application Inspector.")]
        private string globalUiScenePath = string.Empty;

        [SerializeField]
        [Tooltip("Cached human-readable UIGlobal scene name shown in framework diagnostics.")]
        private string globalUiSceneName = string.Empty;

        [SerializeField]
        [Tooltip("Controls validation and diagnostics severity. Required configuration fails in every mode; Strict promotes warnings, Standard keeps them, Release suppresses info diagnostics.")]
        private FrameworkValidationMode validationMode = FrameworkValidationMode.Standard;

        public string ApplicationName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(applicationName))
                {
                    return applicationName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "Game Application";
            }
        }

        public RouteAsset StartupRoute => startupRoute;

        /// <summary>
        /// Ordered immutable Slot Profile references used by the default
        /// First Available By Configured Order allocation policy.
        /// </summary>
        public IReadOnlyList<PlayerSlotProfile> LocalPlayerSlots =>
            localPlayerSlots ?? Array.Empty<PlayerSlotProfile>();

        public int LocalPlayerSlotCount => localPlayerSlots != null ? localPlayerSlots.Length : 0;

        /// <summary>
        /// Required immutable Session policy composed into PlayerParticipationRuntimeContext.
        /// This asset never stores current Slot selections.
        /// </summary>
        public PlayerActorSelectionPolicyProfile PlayerActorSelectionPolicyProfile =>
            playerActorSelectionPolicyProfile;

        public bool HasPlayerActorSelectionPolicy =>
            playerActorSelectionPolicyProfile != null &&
            playerActorSelectionPolicyProfile.HasDefinedDuplicatePolicy;

        public bool TryGetLocalPlayerSlot(int configuredIndex, out PlayerSlotProfile playerSlotProfile)
        {
            if (localPlayerSlots == null ||
                configuredIndex < 0 ||
                configuredIndex >= localPlayerSlots.Length)
            {
                playerSlotProfile = null;
                return false;
            }

            playerSlotProfile = localPlayerSlots[configuredIndex];
            return playerSlotProfile != null;
        }

        public GlobalUiScenePolicy GlobalUiScenePolicyValue => globalUiScenePolicy;

        public string GlobalUiScenePath => globalUiScenePath ?? string.Empty;

        public string GlobalUiSceneName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(globalUiSceneName))
                {
                    return globalUiSceneName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(globalUiScenePath))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(globalUiScenePath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        return fileName;
                    }
                }

                return string.Empty;
            }
        }

        public bool HasGlobalUiScene => !string.IsNullOrWhiteSpace(globalUiScenePath);

        public FrameworkValidationMode ValidationMode => validationMode;
    }
}
