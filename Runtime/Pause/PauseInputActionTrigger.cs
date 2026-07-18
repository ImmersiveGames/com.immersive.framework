using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Removed compatibility tombstone for the former direct Pause InputAction submitter.
    /// It never subscribes to input, submits Pause requests or mutates PlayerInput.
    /// Use PauseInputActionRuntimeBridgeTrigger with
    /// PauseInputModeUnityPlayerInputRuntimeBridge.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Removed,
        "IC3 retires the parallel direct Pause submitter. Use the canonical Pause/InputMode bridge trigger.")]
    public sealed class PauseInputActionTrigger : MonoBehaviour
    {
        private const string RemovedReason =
            "parallel_pause_input_submitter_removed";
        private const string RemovedMessage =
            "PauseInputActionTrigger is retired. Use " +
            "PauseInputActionRuntimeBridgeTrigger with " +
            "PauseInputModeUnityPlayerInputRuntimeBridge so Pause and InputMode share " +
            "one resident authority.";

        [Header("Legacy Serialized Input Source")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private InputActionAsset actionsAsset;

        [Header("Legacy Serialized Pause Action")]
        [SerializeField] private string pauseActionMapName = "Global";
        [SerializeField] private string pauseActionName = "Pause";

        [Header("Legacy Serialized Runtime")]
        [SerializeField] private bool subscribeOnEnable = true;
        [SerializeField] private bool enableResolvedActionsOnEnable = true;
        [SerializeField] private PauseRequestKind requestKind =
            PauseRequestKind.Toggle;
        [SerializeField] private string reason = "pause.input.action.trigger";

        [Header("Legacy Serialized Action Map Switching")]
        [SerializeField] private bool switchPlayerInputActionMap;
        [SerializeField] private string gameplayActionMapName = "Player";
        [SerializeField] private string pauseUiActionMapName = "UI";

        [Header("Legacy Serialized Diagnostics")]
        [SerializeField] private bool logReadyOnEnable = true;
        [SerializeField] private bool logPerformedInput = true;
        [SerializeField] private bool logIgnoredInput = true;
        [SerializeField] private bool logActionMapSwitching = true;

        private FrameworkLogger _logger;
        private string _lastIgnoredReason = RemovedReason;
        private PauseRequestStatus _lastStatus = PauseRequestStatus.Failed;
        private PauseState _lastPreviousState = PauseState.Unknown;
        private PauseState _lastCurrentState = PauseState.Unknown;

        public PlayerInput PlayerInput => playerInput;
        public InputActionAsset ActionsAsset => actionsAsset;
        public string PauseActionMapName =>
            pauseActionMapName.NormalizeTextOrFallback("Global");
        public string PauseActionName =>
            pauseActionName.NormalizeTextOrFallback("Pause");
        public bool IsSubscribed => false;
        public bool SwitchPlayerInputActionMap =>
            switchPlayerInputActionMap;
        public string GameplayActionMapName =>
            gameplayActionMapName.NormalizeTextOrFallback("Player");
        public string PauseUiActionMapName =>
            pauseUiActionMapName.NormalizeTextOrFallback("UI");
        public string LastHandledAction => string.Empty;
        public string LastIgnoredReason =>
            _lastIgnoredReason.NormalizeText();
        public PauseRequestStatus LastStatus => _lastStatus;
        public PauseState LastPreviousState => _lastPreviousState;
        public PauseState LastCurrentState => _lastCurrentState;
        public bool IsRemovedTrigger => true;
        public string RemovedTriggerReason => RemovedReason;
        public string RemovedTriggerMessage => RemovedMessage;

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseInputActionTrigger>();
        }

        private void OnEnable()
        {
            ReportRemovedTrigger();
        }

        [ContextMenu("Immersive Framework/Pause/Removed Trigger/Rebind")]
        public void Rebind()
        {
            ReportRemovedTrigger();
        }

        [ContextMenu("Immersive Framework/Pause/Removed Trigger/Submit Request")]
        public void SubmitRequest()
        {
            ReportRemovedTrigger();
        }

        private void ReportRemovedTrigger()
        {
            _logger ??= FrameworkLogger.Create<PauseInputActionTrigger>();
            _lastIgnoredReason = RemovedReason;
            _lastStatus = PauseRequestStatus.Failed;
            _lastPreviousState = PauseState.Unknown;
            _lastCurrentState = PauseState.Unknown;

            if (logIgnoredInput || logReadyOnEnable || logPerformedInput ||
                logActionMapSwitching || subscribeOnEnable ||
                enableResolvedActionsOnEnable ||
                requestKind != PauseRequestKind.Unknown ||
                !string.IsNullOrWhiteSpace(reason))
            {
                _logger.Warning(
                    "Legacy Pause Input Action Trigger is removed. " +
                    $"reason='{RemovedReason}' message='{RemovedMessage}'.");
            }
        }
    }
}
