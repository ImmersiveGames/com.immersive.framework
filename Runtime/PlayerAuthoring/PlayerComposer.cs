using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerAuthoring
{
    /// <summary>
    /// API status: Experimental. Designer-first authoring surface for one concrete player instance.
    /// The Composer owns authoring intent and editor materialization only. It does not move, spawn,
    /// join, save or act as a runtime manager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [AddComponentMenu("Immersive Framework/Player/Player Composer")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Product surface for player authoring and minimal technical materialization.")]
    public sealed class PlayerComposer : MonoBehaviour
    {
        [Header("Designer")]
        [SerializeField] private PlayerRecipe recipe;
        [Tooltip("Stable semantic ActorId. This is not a GameObject name.")]
        [SerializeField] private string actorId = "player.actor";
        [Tooltip("Stable semantic PlayerSlotId. This is not PlayerInput.playerIndex.")]
        [SerializeField] private string playerSlotId = "player.1";
        [SerializeField] private PlayerComposerValidationMode validationMode = PlayerComposerValidationMode.Standard;

        [Header("Input")]
        [SerializeField] private bool controlEnabled = true;
        [Tooltip("Explicit Unity PlayerInput for this player.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("Required control configuration fails Apply/Rebuild when PlayerInput or its default action map is invalid.")]
        [SerializeField] private bool inputBindingRequired = true;
        [SerializeField] private bool gateParticipation = true;

        [Header("Camera")]
        [SerializeField] private bool cameraBindingRequired = true;
        [Tooltip("Explicit target that a CameraRigComposer may follow.")]
        [SerializeField] private Transform cameraTarget;
        [Tooltip("Explicit target that a CameraRigComposer may look at.")]
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private PlayerComposerLookAtPolicy lookAtPolicy = PlayerComposerLookAtPolicy.ExplicitTarget;

        [Header("Reset")]
        [SerializeField] private bool resetEnabled;
        [SerializeField] private PlayerComposerResetScope resetScope = PlayerComposerResetScope.Activity;
        [SerializeField] private PlayerComposerResetParticipantPolicy resetParticipantPolicy = PlayerComposerResetParticipantPolicy.None;

        [Header("Advanced")]
        [Tooltip("Technical materialization root. This root is organization only, not runtime authority.")]
        [SerializeField] private Transform frameworkBindingsRoot;
        [SerializeField] private bool createBindingsRootIfMissing = true;
        [SerializeField] private bool createAnchorsIfMissing = true;
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        [Tooltip("Authoring default action map. Apply/Rebuild writes this value to PlayerInput.defaultActionMap. Runtime pipelines may switch the active map later.")]
        [SerializeField] private string gameplayActionMap = "Player";

        // Retained as hidden compatibility data for existing scenes and QA serialization.
        // P3A no longer uses these fields as product authority or materialization policy.
        [HideInInspector, SerializeField] private Transform controlTarget;
        [HideInInspector, SerializeField] private PlayerControlStartupPolicy controlStartupPolicy = PlayerControlStartupPolicy.BindOnEnable;
        [HideInInspector, SerializeField] private bool materializeSlotOccupancy;
        [HideInInspector, SerializeField] private bool materializePassiveEntryViewControl;

        [Header("Debug")]
        [SerializeField] private string lastApplyRebuildStatus;
        [SerializeField] private string lastBlockingIssue;
        [TextArea(3, 12)]
        [SerializeField] private string lastMaterializationSummary;

        public PlayerRecipe Recipe => recipe;
        public string ActorId => actorId.NormalizeText();
        public string PlayerSlotId => playerSlotId.NormalizeText();
        public PlayerComposerValidationMode ValidationMode => validationMode;

        public bool ControlEnabled => controlEnabled;
        public PlayerInput PlayerInput => playerInput;
        public bool InputBindingRequired => inputBindingRequired;
        public bool GateParticipation => gateParticipation;
        public bool IsControlRequired => inputBindingRequired;
        public PlayerControlRequiredness ControlRequiredness => inputBindingRequired
            ? PlayerControlRequiredness.Required
            : PlayerControlRequiredness.Optional;
        public PlayerControlStartupPolicy ControlStartupPolicy => controlStartupPolicy;

        /// <summary>
        /// Authored default action map. Apply/Rebuild copies this value into PlayerInput.defaultActionMap.
        /// Runtime pipelines remain free to switch the active map later.
        /// </summary>
        public string GameplayActionMap => gameplayActionMap.NormalizeText();

        /// <summary>
        /// Compatibility property. Gameplay motors remain game-owned and are not materialized by PlayerComposer.
        /// </summary>
        public Transform ControlTarget => controlTarget;

        public bool HasCompleteControlConfiguration =>
            !controlEnabled
            || (!inputBindingRequired && playerInput == null)
            || HasValidRequiredInput();

        public bool CameraBindingRequired => cameraBindingRequired;
        public Transform CameraTarget => cameraTarget;
        public Transform LookAtTarget =>
            lookAtPolicy == PlayerComposerLookAtPolicy.UseFollowTarget ? cameraTarget : lookAtTarget;
        public PlayerComposerLookAtPolicy LookAtPolicy => lookAtPolicy;

        public bool ResetEnabled => resetEnabled;
        public string ResetScope => resetScope.ToString();
        public PlayerComposerResetScope ResetScopePolicy => resetScope;
        public PlayerComposerResetParticipantPolicy ResetParticipantPolicy => resetParticipantPolicy;

        public Transform FrameworkBindingsRoot => frameworkBindingsRoot;
        public bool CreateBindingsRootIfMissing => createBindingsRootIfMissing;
        public bool CreateAnchorsIfMissing => createAnchorsIfMissing;
        public bool MaterializeSlotOccupancy => false;
        public bool MaterializePassiveEntryViewControl => false;
        public bool LogApplyRebuildDiagnostics => logApplyRebuildDiagnostics;

        public string LastApplyRebuildStatus => lastApplyRebuildStatus.NormalizeText();
        public string LastBlockingIssue => lastBlockingIssue.NormalizeText();
        public string LastMaterializationSummary => lastMaterializationSummary.NormalizeText();

        public bool TryValidateForApply(out string issue)
        {
            issue = string.Empty;

            if (string.IsNullOrWhiteSpace(ActorId))
            {
                issue = "PlayerComposer requires a non-empty ActorId.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(PlayerSlotId))
            {
                issue = "PlayerComposer requires a non-empty PlayerSlotId.";
                return false;
            }

            if (playerInput == null)
            {
                issue = "PlayerComposer requires an assigned PlayerInput component.";
                return false;
            }

            if (controlEnabled && inputBindingRequired && playerInput != null && playerInput.actions == null)
            {
                issue = "PlayerComposer requires an InputActionAsset when control is enabled and required.";
                return false;
            }

            if (controlEnabled && inputBindingRequired && playerInput != null)
            {
                string authoredMap = GameplayActionMap;
                if (string.IsNullOrEmpty(authoredMap))
                {
                    issue = "PlayerComposer requires an authored Default Action Map when control is enabled and required.";
                    return false;
                }

                if (playerInput.actions.FindActionMap(authoredMap, false) == null)
                {
                    issue = $"PlayerComposer could not find authored Default Action Map '{authoredMap}' in the assigned InputActionAsset.";
                    return false;
                }
            }

            if (cameraBindingRequired && cameraTarget == null && !createAnchorsIfMissing)
            {
                issue = "PlayerComposer requires an explicit Camera Target when camera binding is required and automatic anchor creation is disabled.";
                return false;
            }

            if (cameraBindingRequired
                && lookAtPolicy == PlayerComposerLookAtPolicy.ExplicitTarget
                && lookAtTarget == null
                && !createAnchorsIfMissing)
            {
                issue = "PlayerComposer requires an explicit Look At Target when Look At Policy is Explicit Target and automatic anchor creation is disabled.";
                return false;
            }

            if (!System.Enum.IsDefined(typeof(PlayerComposerResetScope), resetScope))
            {
                issue = "PlayerComposer Reset Scope is invalid.";
                return false;
            }

            return true;
        }

        public PlayerComposerDebugSnapshot CreateDebugSnapshot()
        {
            string actionMap = GameplayActionMap;
            bool actionMapFound = playerInput != null
                && playerInput.actions != null
                && !string.IsNullOrEmpty(actionMap)
                && playerInput.actions.FindActionMap(actionMap, false) != null;

            return new PlayerComposerDebugSnapshot(
                ActorId,
                PlayerSlotId,
                controlEnabled,
                playerInput != null ? playerInput.name.NormalizeText() : string.Empty,
                actionMap,
                actionMapFound,
                string.Empty,
                controlStartupPolicy,
                ControlRequiredness,
                gateParticipation,
                frameworkBindingsRoot != null ? frameworkBindingsRoot.name.NormalizeText() : string.Empty,
                cameraTarget != null ? cameraTarget.name.NormalizeText() : string.Empty,
                LookAtTarget != null ? LookAtTarget.name.NormalizeText() : string.Empty,
                resetEnabled,
                LastApplyRebuildStatus,
                LastBlockingIssue,
                LastMaterializationSummary);
        }

#if UNITY_EDITOR
        public bool EditorApplyRecipeDefaults(bool overwriteExisting, out string issue)
        {
            issue = string.Empty;
            if (recipe == null)
            {
                issue = "PlayerComposer requires a PlayerRecipe before recipe defaults can be applied.";
                return false;
            }

            if (overwriteExisting || string.IsNullOrWhiteSpace(actorId))
            {
                actorId = recipe.ActorId;
            }

            if (overwriteExisting || string.IsNullOrWhiteSpace(playerSlotId))
            {
                playerSlotId = recipe.PlayerSlotId;
            }

            gameplayActionMap = recipe.GameplayActionMap;
            controlEnabled = recipe.ControlEnabled;
            inputBindingRequired = recipe.InputBindingRequired;
            gateParticipation = recipe.GateParticipation;
            cameraBindingRequired = recipe.CameraBindingRequired;
            lookAtPolicy = recipe.LookAtPolicy;
            resetEnabled = recipe.ResetEnabled;
            resetScope = recipe.ResetScope;
            resetParticipantPolicy = recipe.ResetParticipantPolicy;
            validationMode = recipe.ValidationMode;
            return true;
        }

        public void EditorSetGeneratedReferences(
            Transform technicalRoot,
            Transform generatedCameraTarget,
            Transform generatedLookAtTarget)
        {
            frameworkBindingsRoot = technicalRoot;

            if (cameraTarget == null)
            {
                cameraTarget = generatedCameraTarget;
            }

            if (lookAtPolicy == PlayerComposerLookAtPolicy.ExplicitTarget && lookAtTarget == null)
            {
                lookAtTarget = generatedLookAtTarget;
            }
        }

        public void EditorSetApplyRebuildResult(string status, string blockingIssue, string materializationSummary)
        {
            lastApplyRebuildStatus = status.NormalizeText();
            lastBlockingIssue = blockingIssue.NormalizeText();
            lastMaterializationSummary = materializationSummary.NormalizeText();
        }

        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
            cameraTarget = null;
            lookAtTarget = null;
            controlTarget = null;
        }

        private void OnValidate()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }
        }
#endif

        private bool HasValidRequiredInput()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return false;
            }

            string authoredMap = GameplayActionMap;
            return !string.IsNullOrEmpty(authoredMap)
                && playerInput.actions.FindActionMap(authoredMap, false) != null;
        }
    }

    public enum PlayerComposerValidationMode
    {
        Standard = 0,
        Strict = 1,
        DiagnosticsOnly = 2
    }

    public enum PlayerComposerResetParticipantPolicy
    {
        None = 0,
        Transform = 1
    }

    public enum PlayerComposerResetScope
    {
        Activity = 0,
        Route = 1
    }

    public enum PlayerComposerLookAtPolicy
    {
        ExplicitTarget = 0,
        UseFollowTarget = 1
    }

    public enum PlayerControlStartupPolicy
    {
        BindOnEnable = 0
    }

    public enum PlayerControlRequiredness
    {
        Required = 0,
        Optional = 1
    }

    public readonly struct PlayerComposerDebugSnapshot
    {
        public PlayerComposerDebugSnapshot(
            string actorId,
            string playerSlotId,
            bool controlEnabled,
            string playerInputName,
            string gameplayActionMap,
            bool actionMapFound,
            string controlTargetName,
            PlayerControlStartupPolicy controlStartupPolicy,
            PlayerControlRequiredness controlRequiredness,
            bool gateParticipation,
            string frameworkBindingsRootName,
            string cameraTargetName,
            string lookAtTargetName,
            bool resetEnabled,
            string lastApplyRebuildStatus,
            string lastBlockingIssue,
            string lastMaterializationSummary)
        {
            ActorId = actorId;
            PlayerSlotId = playerSlotId;
            ControlEnabled = controlEnabled;
            PlayerInputName = playerInputName;
            GameplayActionMap = gameplayActionMap;
            ActionMapFound = actionMapFound;
            ControlTargetName = controlTargetName;
            ControlStartupPolicy = controlStartupPolicy;
            ControlRequiredness = controlRequiredness;
            GateParticipation = gateParticipation;
            FrameworkBindingsRootName = frameworkBindingsRootName;
            CameraTargetName = cameraTargetName;
            LookAtTargetName = lookAtTargetName;
            ResetEnabled = resetEnabled;
            LastApplyRebuildStatus = lastApplyRebuildStatus;
            LastBlockingIssue = lastBlockingIssue;
            LastMaterializationSummary = lastMaterializationSummary;
        }

        public string ActorId { get; }
        public string PlayerSlotId { get; }
        public bool ControlEnabled { get; }
        public string PlayerInputName { get; }
        public string GameplayActionMap { get; }
        public bool ActionMapFound { get; }
        public string ControlTargetName { get; }
        public PlayerControlStartupPolicy ControlStartupPolicy { get; }
        public PlayerControlRequiredness ControlRequiredness { get; }
        public bool GateParticipation { get; }
        public string FrameworkBindingsRootName { get; }
        public string CameraTargetName { get; }
        public string LookAtTargetName { get; }
        public bool ResetEnabled { get; }
        public string LastApplyRebuildStatus { get; }
        public string LastBlockingIssue { get; }
        public string LastMaterializationSummary { get; }
    }
}
