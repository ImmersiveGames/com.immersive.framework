using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerAuthoring
{
    /// <summary>
    /// API status: Experimental. Reusable designer intent for PlayerComposer defaults.
    /// This asset does not materialize bindings by itself, execute gameplay, own PlayerInput at runtime, spawn players or act as a PlayerManager.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerRecipe", menuName = "Immersive Framework/Player/Player Recipe")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Product surface MVP for reusable PlayerComposer defaults.")]
    public sealed class PlayerRecipe : ScriptableObject
    {
        [Header("Designer Defaults")]
        [Tooltip("Default stable ActorId copied into PlayerComposer. This is not a GameObject name.")]
        [SerializeField] private string actorId = "player.actor";
        [Tooltip("Default stable PlayerSlotId copied into PlayerComposer. This is not PlayerInput.playerIndex.")]
        [SerializeField] private string playerSlotId = "player.1";
        [Tooltip("Optional reset materialization default. Disabled by default because reset participant setup is a policy choice.")]
        [SerializeField] private bool resetEnabled;
        [SerializeField] private PlayerComposerValidationMode validationMode = PlayerComposerValidationMode.Standard;

        [Header("Control Defaults")]
        [Tooltip("Whether PlayerComposer should author and materialize Player control evidence.")]
        [SerializeField] private bool controlEnabled = true;
        [Tooltip("Default gameplay action map expected on the target PlayerInput action asset.")]
        [SerializeField] private string gameplayActionMap = "Player";
        [Tooltip("Control startup intent. Bind On Enable is the only supported P2 MVP policy.")]
        [SerializeField] private PlayerControlStartupPolicy controlStartupPolicy = PlayerControlStartupPolicy.BindOnEnable;
        [Tooltip("Requiredness of complete control references. Enabled means Required; disabled means Optional.")]
        [SerializeField] private bool inputBindingRequired = true;
        [Tooltip("Whether Player control participates in framework Gate blocking.")]
        [SerializeField] private bool gateParticipation = true;

        [Header("Authoring Defaults")]
        [SerializeField] private bool createBindingsRootIfMissing = true;
        [SerializeField] private bool createAnchorsIfMissing = true;
        [SerializeField] private bool cameraBindingRequired = true;
        [Tooltip("Reset scope copied only when reset materialization is enabled.")]
        [SerializeField] private string resetScope = "Activity";
        [Tooltip("Optional reset participant policy. None does not create reset participants by default.")]
        [SerializeField] private PlayerComposerResetParticipantPolicy resetParticipantPolicy = PlayerComposerResetParticipantPolicy.None;
        [SerializeField] private bool materializeSlotOccupancy = true;
        [SerializeField] private bool materializePassiveEntryViewControl;
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        public string ActorId => actorId.NormalizeText();

        public string PlayerSlotId => playerSlotId.NormalizeText();

        public bool ControlEnabled => controlEnabled;

        public string GameplayActionMap => gameplayActionMap.NormalizeText();

        public PlayerControlStartupPolicy ControlStartupPolicy => controlStartupPolicy;

        public PlayerControlRequiredness ControlRequiredness => inputBindingRequired
            ? PlayerControlRequiredness.Required
            : PlayerControlRequiredness.Optional;

        public bool InputBindingRequired => inputBindingRequired;

        public bool GateParticipation => gateParticipation;

        public bool ResetEnabled => resetEnabled;

        public PlayerComposerValidationMode ValidationMode => validationMode;

        public bool CreateBindingsRootIfMissing => createBindingsRootIfMissing;

        public bool CreateAnchorsIfMissing => createAnchorsIfMissing;

        public bool CameraBindingRequired => cameraBindingRequired;

        public string ResetScope => resetScope.NormalizeTextOrFallback("Activity");

        public PlayerComposerResetParticipantPolicy ResetParticipantPolicy => resetParticipantPolicy;

        public bool MaterializeSlotOccupancy => materializeSlotOccupancy;

        public bool MaterializePassiveEntryViewControl => materializePassiveEntryViewControl;

        public bool LogApplyRebuildDiagnostics => logApplyRebuildDiagnostics;
    }
}
