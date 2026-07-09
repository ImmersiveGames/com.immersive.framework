using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerAuthoring
{
    /// <summary>
    /// API status: Experimental. Designer-first authoring surface for a framework-ready player.
    /// This component centralizes player intent for editor Apply/Rebuild tooling. It does not execute gameplay,
    /// move actors, spawn actors, join players, save progression, switch action maps at runtime or act as a PlayerManager.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Composer")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Product surface MVP for player authoring and technical materialization.")]
    public sealed class PlayerComposer : MonoBehaviour
    {
        [Header("Designer")]
        [Tooltip("Optional reusable player intent asset. Use Apply Recipe Defaults to copy reusable defaults into this Composer; local fields remain editable.")]
        [SerializeField] private PlayerRecipe recipe;
        [Tooltip("Stable ActorId for the player. This is not a GameObject name.")]
        [SerializeField] private string actorId = "player.actor";
        [Tooltip("Stable PlayerSlotId for the player. This is not PlayerInput.playerIndex.")]
        [SerializeField] private string playerSlotId = "player.1";
        [Tooltip("Explicit Unity PlayerInput used as typed input evidence.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("Expected gameplay action map on the PlayerInput action asset.")]
        [SerializeField] private string gameplayActionMap = "Player";
        [Tooltip("Transform that cameras or view bindings should track.")]
        [SerializeField] private Transform cameraTarget;
        [Tooltip("Transform that cameras or view bindings should look at.")]
        [SerializeField] private Transform lookAtTarget;
        [Tooltip("Optional reset materialization. Disabled by default because reset participants are a policy choice, not mandatory Player setup.")]
        [SerializeField] private bool resetEnabled;
        [SerializeField] private PlayerComposerValidationMode validationMode = PlayerComposerValidationMode.Standard;

        [Header("Advanced")]
        [Tooltip("Technical materialization root. This is not the product authority.")]
        [SerializeField] private Transform frameworkBindingsRoot;
        [SerializeField] private bool createBindingsRootIfMissing = true;
        [SerializeField] private bool createAnchorsIfMissing = true;
        [SerializeField] private bool inputBindingRequired = true;
        [SerializeField] private bool cameraBindingRequired = true;
        [Tooltip("Reset scope used only when reset materialization is enabled.")]
        [SerializeField] private string resetScope = "Activity";
        [Tooltip("Optional reset participant materialization. None does not create a participant; Transform creates UnityTransformResetParticipant when reset is enabled.")]
        [SerializeField] private PlayerComposerResetParticipantPolicy resetParticipantPolicy = PlayerComposerResetParticipantPolicy.None;
        [SerializeField] private bool materializeSlotOccupancy = true;
        [SerializeField] private bool materializePassiveEntryViewControl;
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        [Header("Debug")]
        [SerializeField] private string lastApplyRebuildStatus;
        [SerializeField] private string lastBlockingIssue;
        [SerializeField] private string lastMaterializationSummary;

        public PlayerRecipe Recipe => recipe;
        public string ActorId => actorId.NormalizeText();
        public string PlayerSlotId => playerSlotId.NormalizeText();
        public PlayerInput PlayerInput => playerInput != null ? playerInput : GetComponent<PlayerInput>();
        public string GameplayActionMap => gameplayActionMap.NormalizeText();
        public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
        public Transform LookAtTarget => lookAtTarget != null ? lookAtTarget : CameraTarget;
        public bool ResetEnabled => resetEnabled;
        public PlayerComposerValidationMode ValidationMode => validationMode;
        public Transform FrameworkBindingsRoot => frameworkBindingsRoot;
        public bool CreateBindingsRootIfMissing => createBindingsRootIfMissing;
        public bool CreateAnchorsIfMissing => createAnchorsIfMissing;
        public bool InputBindingRequired => inputBindingRequired;
        public bool CameraBindingRequired => cameraBindingRequired;
        public string ResetScope => resetScope.NormalizeTextOrFallback("Activity");
        public PlayerComposerResetParticipantPolicy ResetParticipantPolicy => resetParticipantPolicy;
        public bool MaterializeSlotOccupancy => materializeSlotOccupancy;
        public bool MaterializePassiveEntryViewControl => materializePassiveEntryViewControl;
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

            if (inputBindingRequired && PlayerInput == null)
            {
                issue = "PlayerComposer requires PlayerInput when input binding is required.";
                return false;
            }

            if (inputBindingRequired && string.IsNullOrWhiteSpace(GameplayActionMap))
            {
                issue = "PlayerComposer requires a gameplay action map when input binding is required.";
                return false;
            }

            if (inputBindingRequired && PlayerInput != null && PlayerInput.actions == null)
            {
                issue = "PlayerComposer requires PlayerInput.actions when input binding is required.";
                return false;
            }

            if (inputBindingRequired && PlayerInput != null && PlayerInput.actions != null && PlayerInput.actions.FindActionMap(GameplayActionMap, false) == null)
            {
                issue = $"PlayerComposer could not find action map '{GameplayActionMap}' in the PlayerInput action asset.";
                return false;
            }

            if (cameraBindingRequired && CameraTarget == null)
            {
                issue = "PlayerComposer requires a camera target when camera binding is required.";
                return false;
            }

            if (cameraBindingRequired && LookAtTarget == null)
            {
                issue = "PlayerComposer requires a look-at target when camera binding is required.";
                return false;
            }

            if (!createBindingsRootIfMissing && frameworkBindingsRoot == null)
            {
                issue = "PlayerComposer requires a framework bindings root when automatic creation is disabled.";
                return false;
            }

            return true;
        }

        public PlayerComposerDebugSnapshot CreateDebugSnapshot()
        {
            bool hasActionMap = PlayerInput != null && PlayerInput.actions != null && !string.IsNullOrWhiteSpace(GameplayActionMap) && PlayerInput.actions.FindActionMap(GameplayActionMap, false) != null;
            return new PlayerComposerDebugSnapshot(
                ActorId,
                PlayerSlotId,
                PlayerInput != null ? PlayerInput.name.NormalizeText() : string.Empty,
                GameplayActionMap,
                hasActionMap,
                frameworkBindingsRoot != null ? frameworkBindingsRoot.name.NormalizeText() : string.Empty,
                cameraTarget != null ? cameraTarget.name.NormalizeText() : string.Empty,
                lookAtTarget != null ? lookAtTarget.name.NormalizeText() : string.Empty,
                resetEnabled,
                lastApplyRebuildStatus.NormalizeText(),
                lastBlockingIssue.NormalizeText(),
                lastMaterializationSummary.NormalizeText());
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

            if (overwriteExisting || string.IsNullOrWhiteSpace(gameplayActionMap))
            {
                gameplayActionMap = recipe.GameplayActionMap;
            }

            resetEnabled = recipe.ResetEnabled;
            validationMode = recipe.ValidationMode;
            createBindingsRootIfMissing = recipe.CreateBindingsRootIfMissing;
            createAnchorsIfMissing = recipe.CreateAnchorsIfMissing;
            inputBindingRequired = recipe.InputBindingRequired;
            cameraBindingRequired = recipe.CameraBindingRequired;
            resetScope = recipe.ResetScope;
            resetParticipantPolicy = recipe.ResetParticipantPolicy;
            materializeSlotOccupancy = recipe.MaterializeSlotOccupancy;
            materializePassiveEntryViewControl = recipe.MaterializePassiveEntryViewControl;
            logApplyRebuildDiagnostics = recipe.LogApplyRebuildDiagnostics;
            return true;
        }

        public void EditorSetGeneratedReferences(Transform bindingsRoot, Transform generatedCameraTarget, Transform generatedLookAtTarget)
        {
            frameworkBindingsRoot = bindingsRoot;
            if (cameraTarget == null)
            {
                cameraTarget = generatedCameraTarget;
            }

            if (lookAtTarget == null)
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
            cameraTarget = transform;
            lookAtTarget = transform;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                actorId = "player.actor";
            }

            if (string.IsNullOrWhiteSpace(playerSlotId))
            {
                playerSlotId = "player.1";
            }
        }
#endif
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

    public readonly struct PlayerComposerDebugSnapshot
    {
        public PlayerComposerDebugSnapshot(
            string actorId,
            string playerSlotId,
            string playerInputName,
            string gameplayActionMap,
            bool actionMapFound,
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
            PlayerInputName = playerInputName;
            GameplayActionMap = gameplayActionMap;
            ActionMapFound = actionMapFound;
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
        public string PlayerInputName { get; }
        public string GameplayActionMap { get; }
        public bool ActionMapFound { get; }
        public string FrameworkBindingsRootName { get; }
        public string CameraTargetName { get; }
        public string LookAtTargetName { get; }
        public bool ResetEnabled { get; }
        public string LastApplyRebuildStatus { get; }
        public string LastBlockingIssue { get; }
        public string LastMaterializationSummary { get; }
    }
}
