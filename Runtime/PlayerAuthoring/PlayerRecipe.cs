using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerAuthoring
{
    /// <summary>
    /// API status: Experimental. Reusable player intent copied into PlayerComposer.
    /// Technical editor materialization settings do not belong to this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerRecipe", menuName = "Immersive Framework/Player/Player Recipe")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Reusable defaults for PlayerComposer intent.")]
    public sealed class PlayerRecipe : ScriptableObject
    {
        [Header("Identity Defaults")]
        [SerializeField] private string actorId = "player.actor";
        [SerializeField] private string playerSlotId = "player.1";

        [Header("Input Defaults")]
        [SerializeField] private bool controlEnabled = true;
        [Tooltip("Default action map copied into PlayerComposer. Apply/Rebuild writes it to PlayerInput.defaultActionMap.")]
        [SerializeField] private string gameplayActionMap = "Player";
        [SerializeField] private bool inputBindingRequired = true;
        [SerializeField] private bool gateParticipation = true;

        [Header("Camera Defaults")]
        [SerializeField] private bool cameraBindingRequired = true;
        [SerializeField] private PlayerComposerLookAtPolicy lookAtPolicy = PlayerComposerLookAtPolicy.ExplicitTarget;

        [Header("Reset Defaults")]
        [SerializeField] private bool resetEnabled;
        [SerializeField] private PlayerComposerResetScope resetScope = PlayerComposerResetScope.Activity;
        [SerializeField] private PlayerComposerResetParticipantPolicy resetParticipantPolicy = PlayerComposerResetParticipantPolicy.None;

        [Header("Validation")]
        [SerializeField] private PlayerComposerValidationMode validationMode = PlayerComposerValidationMode.Standard;

        // Compatibility API for existing QA and serialized data.
        [HideInInspector, SerializeField] private PlayerControlStartupPolicy controlStartupPolicy = PlayerControlStartupPolicy.BindOnEnable;
        [HideInInspector, SerializeField] private bool createBindingsRootIfMissing = true;
        [HideInInspector, SerializeField] private bool createAnchorsIfMissing = true;
        [HideInInspector, SerializeField] private bool materializeSlotOccupancy;
        [HideInInspector, SerializeField] private bool materializePassiveEntryViewControl;
        [HideInInspector, SerializeField] private bool logApplyRebuildDiagnostics = true;

        public string ActorId => actorId.NormalizeText();
        public string PlayerSlotId => playerSlotId.NormalizeText();
        public bool ControlEnabled => controlEnabled;
        public bool InputBindingRequired => inputBindingRequired;
        public bool GateParticipation => gateParticipation;
        public bool CameraBindingRequired => cameraBindingRequired;
        public PlayerComposerLookAtPolicy LookAtPolicy => lookAtPolicy;
        public bool ResetEnabled => resetEnabled;
        public PlayerComposerResetScope ResetScope => resetScope;
        public PlayerComposerResetParticipantPolicy ResetParticipantPolicy => resetParticipantPolicy;
        public PlayerComposerValidationMode ValidationMode => validationMode;

        public string GameplayActionMap => gameplayActionMap.NormalizeText();
        public PlayerControlStartupPolicy ControlStartupPolicy => controlStartupPolicy;
        public PlayerControlRequiredness ControlRequiredness => inputBindingRequired
            ? PlayerControlRequiredness.Required
            : PlayerControlRequiredness.Optional;
        public bool CreateBindingsRootIfMissing => createBindingsRootIfMissing;
        public bool CreateAnchorsIfMissing => createAnchorsIfMissing;
        public bool MaterializeSlotOccupancy => false;
        public bool MaterializePassiveEntryViewControl => false;
        public bool LogApplyRebuildDiagnostics => logApplyRebuildDiagnostics;
    }
}
