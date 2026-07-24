using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Public authoring root for one Immersive game/application.
    ///
    /// The asset owns application-level intent only. Mutable Session, Player, Route, Activity,
    /// Camera and scene runtime state remain outside this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameApplication",
        menuName = "Immersive Framework/Game Application",
        order = 0)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Baseline product authoring surface for application configuration.")]
    public sealed class GameApplicationAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable name shown in framework diagnostics. If empty, the asset name is used.")]
        private string applicationName = "Game Application";

        [SerializeField]
        [Tooltip("First Route requested by Game Flow after framework boot. The Route declares the first Primary Scene.")]
        private RouteAsset startupRoute;

        [SerializeField]
        [Tooltip("Ordered local Player participation seats. Array order is the canonical default allocation order; Profile Display Order is presentation metadata only.")]
        private PlayerSlotProfile[] localPlayerSlots = Array.Empty<PlayerSlotProfile>();

        [SerializeField]
        [Tooltip("Session duplicate-selection rule for ActorProfile selection across joined local Player Slots. Runtime selection state remains outside this asset.")]
        private PlayerActorSelectionDuplicatePolicy playerActorSelectionDuplicatePolicy =
            PlayerActorSelectionDuplicatePolicy.AllowDuplicates;

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
        /// Ordered immutable Slot Profile references used by the default
        /// First Available By Configured Order allocation policy.
        /// </summary>
        public IReadOnlyList<PlayerSlotProfile> LocalPlayerSlots =>
            localPlayerSlots ?? Array.Empty<PlayerSlotProfile>();

        public int LocalPlayerSlotCount =>
            localPlayerSlots != null
                ? localPlayerSlots.Length
                : 0;

        /// <summary>
        /// Session duplicate-selection policy composed into PlayerParticipationRuntimeContext.
        /// This asset is the single authoring authority and never stores current Slot selections.
        /// </summary>
        public PlayerActorSelectionDuplicatePolicy PlayerActorSelectionDuplicatePolicy =>
            playerActorSelectionDuplicatePolicy;

        public bool HasDefinedPlayerActorSelectionDuplicatePolicy =>
            playerActorSelectionDuplicatePolicy.IsDefinedPolicy();

        public PersistentContentComposition PersistentContent =>
            persistentContent;

        public bool HasPersistentContentComposition =>
            persistentContent != null &&
            persistentContent.IsComplete;

        public FrameworkValidationMode ValidationMode =>
            validationMode;

        public bool TryGetLocalPlayerSlot(
            int configuredIndex,
            out PlayerSlotProfile playerSlotProfile)
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
    }
}
