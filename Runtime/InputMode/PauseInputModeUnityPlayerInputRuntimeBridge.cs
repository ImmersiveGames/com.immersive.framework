using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Scene-authored, opt-in bridge that submits a logical Pause request to the framework runtime
    /// and, only after a safe preflight, applies the resulting InputMode to one explicit Unity PlayerInput.
    /// It does not register itself in FrameworkRuntimeHost, own PauseRuntime, own PlayerInputManager, call JoinPlayer, spawn players,
    /// read gameplay commands or create a custom input manager.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Input Mode/Pause PlayerInput Runtime Bridge")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33A opt-in Pause runtime to Unity PlayerInput wiring bridge.")]
    public sealed class PauseInputModeUnityPlayerInputRuntimeBridge : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseInputModeUnityPlayerInputRuntimeBridge);

        private FrameworkLogger _logger;
        private int _requestSequence;
        private PauseInputModeUnityPlayerInputRuntimeBridgeResult _lastResult;

        [Header("Unity PlayerInput")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Framework References")]
        [SerializeField] private UnityInputTargetDeclaration[] unityInputTargets;
        [SerializeField] private PlayerActorDeclaration[] playerActors;
        [SerializeField] private SessionPlayerInputManagerDeclaration[] sessionPlayerInputManagers;
        [SerializeField] private bool autoDiscoverMissingReferences = true;
        [SerializeField] private bool requireSessionPlayerInputManagerEvidence = true;

        [Header("Action Maps")]
        [SerializeField] private string gameplayActionMapName = "Player";
        [SerializeField] private string uiActionMapName = "UI";

        [Header("Request")]
        [SerializeField] private string reason = "pause.inputmode.playerinput.runtime.bridge";
        [SerializeField] private bool logResults = true;

        public PlayerInput PlayerInput => playerInput;

        public bool AutoDiscoverMissingReferences => autoDiscoverMissingReferences;

        public bool RequireSessionPlayerInputManagerEvidence => requireSessionPlayerInputManagerEvidence;

        public string GameplayActionMapName => gameplayActionMapName.NormalizeTextOrFallback("Player");

        public string UiActionMapName => uiActionMapName.NormalizeTextOrFallback("UI");

        public PauseInputModeUnityPlayerInputRuntimeBridgeResult LastResult => _lastResult;

        public bool HasLastResult => _lastResult != null;

        public bool LastSucceeded => _lastResult != null && _lastResult.Succeeded;

        public bool LastIgnored => _lastResult != null && _lastResult.Ignored;

        public bool LastFailed => _lastResult != null && _lastResult.Failed;

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseInputModeUnityPlayerInputRuntimeBridge>();
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Pause")]
        public void RequestPause()
        {
            Submit(PauseRequestKind.Pause, "pause.inputmode.playerinput.runtime.bridge.pause");
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Resume")]
        public void RequestResume()
        {
            Submit(PauseRequestKind.Resume, "pause.inputmode.playerinput.runtime.bridge.resume");
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Toggle")]
        public void TogglePause()
        {
            Submit(PauseRequestKind.Toggle, "pause.inputmode.playerinput.runtime.bridge.toggle");
        }

        private void Submit(PauseRequestKind kind, string fallbackReason)
        {
            _lastResult = SubmitInternal(kind, DefaultSource, ResolveReason(fallbackReason));
            if (logResults && _lastResult != null)
            {
                EnsureLogger();
                _logger.Info(
                    "Pause InputMode PlayerInput Runtime Bridge completed.",
                    Logging.Records.LogFields.Of(
                        Logging.Records.LogFields.Field("status", _lastResult.Status.ToString()),
                        Logging.Records.LogFields.Field("failedStage", _lastResult.FailedStage.ToString()),
                        Logging.Records.LogFields.Field("pauseStatus", _lastResult.PauseStatus.ToString()),
                        Logging.Records.LogFields.Field("requestedMode", _lastResult.RequestedMode.ToString()),
                        Logging.Records.LogFields.Field("operation", _lastResult.Operation.ToString()),
                        Logging.Records.LogFields.Field("previousActionMap", _lastResult.PreviousActionMapName.ToString()),
                        Logging.Records.LogFields.Field("appliedActionMap", _lastResult.AppliedActionMapName.ToString()),
                        Logging.Records.LogFields.Field("actionMapSwitching", _lastResult.SwitchesActionMaps),
                        Logging.Records.LogFields.Field("inputBehavior", _lastResult.AppliesInputBehavior),
                        Logging.Records.LogFields.Field("pauseRuntimeWiring", _lastResult.PauseRuntimeWiring),
                        Logging.Records.LogFields.Field("playerJoin", _lastResult.CallsPlayerJoin),
                        Logging.Records.LogFields.Field("actorSpawning", _lastResult.SpawnsActor),
                        Logging.Records.LogFields.Field("diagnostics", _lastResult.ToDiagnosticString())));
            }
        }

        internal PauseInputModeUnityPlayerInputRuntimeBridgeResult SubmitForDiagnostics(
            PauseRequestKind kind,
            string source,
            string requestReason)
        {
            _lastResult = SubmitInternal(kind, source, requestReason);
            return _lastResult;
        }

        internal void ConfigureForDiagnostics(
            PlayerInput input,
            UnityInputTargetDeclaration[] targets,
            PlayerActorDeclaration[] actors,
            SessionPlayerInputManagerDeclaration[] sessionManagers,
            string playerMap,
            string uiMap,
            bool autoDiscover,
            bool requireSessionManager)
        {
            playerInput = input;
            unityInputTargets = targets;
            playerActors = actors;
            sessionPlayerInputManagers = sessionManagers;
            gameplayActionMapName = playerMap.NormalizeTextOrFallback("Player");
            uiActionMapName = uiMap.NormalizeTextOrFallback("UI");
            autoDiscoverMissingReferences = autoDiscover;
            requireSessionPlayerInputManagerEvidence = requireSessionManager;
        }

        private PauseInputModeUnityPlayerInputRuntimeBridgeResult SubmitInternal(
            PauseRequestKind kind,
            string source,
            string requestReason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(DefaultSource);
            string normalizedReason = requestReason.NormalizeTextOrFallback(ResolveReason("pause.inputmode.playerinput.runtime.bridge"));

            FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost);
            ResolvedReferences references = ResolveReferences(normalizedSource, normalizedReason);
            _requestSequence++;
            string requestId = $"pause.inputmode.playerinput.runtime.bridge.{_requestSequence}";

            var request = new PauseInputModeApplyRequest(
                runtimeHost,
                kind,
                requestId,
                references.PlayerInput,
                references.TargetSet,
                references.PlayerActorSet,
                references.SessionPlayerInputManagerEvidence,
                references.ActionMapEvidence,
                references.ActionMapBindings,
                requireSessionPlayerInputManagerEvidence,
                normalizedSource,
                normalizedReason);

            var service = new PauseInputModeApplyService();
            PauseInputModeApplyResult result = service.Apply(request);
            return result.ToRuntimeBridgeResult();
        }

        private ResolvedReferences ResolveReferences(string source, string requestReason)
        {
            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            UnityInputTargetSet targetSet = ResolveTargetSet(source, requestReason);
            PlayerActorSet playerActorSet = ResolvePlayerActorSet(source, requestReason);
            UnityInputPlayerInputManagerEvidence sessionEvidence = ResolveSessionPlayerInputManagerEvidence(source, requestReason);
            UnityInputActionMapEvidence actionMapEvidence = UnityInputActionMapEvidence.FromInputActionAsset(
                resolvedPlayerInput == null ? null : resolvedPlayerInput.actions,
                source,
                requestReason);

            return new ResolvedReferences(
                resolvedPlayerInput,
                targetSet,
                playerActorSet,
                sessionEvidence,
                actionMapEvidence,
                CreateActionMapBindings(source, requestReason));
        }

        private UnityInputTargetSet ResolveTargetSet(string source, string requestReason)
        {
            if (unityInputTargets != null && unityInputTargets.Length > 0)
            {
                return UnityInputTargetValidator.ValidateDeclarations(unityInputTargets, source, requestReason);
            }

            return autoDiscoverMissingReferences
                ? UnityInputTargetValidator.ValidateLoadedSceneDeclarations(source, requestReason)
                : UnityInputTargetValidator.ValidateDeclarations(Array.Empty<UnityInputTargetDeclaration>(), source, requestReason);
        }

        private PlayerActorSet ResolvePlayerActorSet(string source, string requestReason)
        {
            if (playerActors != null && playerActors.Length > 0)
            {
                return PlayerActorValidator.ValidateDeclarations(playerActors, source, requestReason);
            }

            return autoDiscoverMissingReferences
                ? PlayerActorValidator.ValidateLoadedSceneDeclarations(source, requestReason)
                : PlayerActorValidator.ValidateDeclarations(Array.Empty<PlayerActorDeclaration>(), source, requestReason);
        }

        private UnityInputPlayerInputManagerEvidence ResolveSessionPlayerInputManagerEvidence(string source, string requestReason)
        {
            if (!requireSessionPlayerInputManagerEvidence)
            {
                return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(1, source, requestReason);
            }

            if (sessionPlayerInputManagers != null && sessionPlayerInputManagers.Length > 0)
            {
                return UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerDeclarations(sessionPlayerInputManagers, source, requestReason);
            }

            return autoDiscoverMissingReferences
                ? UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerEvidence(source, requestReason)
                : UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerDeclarations(Array.Empty<SessionPlayerInputManagerDeclaration>(), source, requestReason);
        }

        private PlayerInput ResolvePlayerInput()
        {
            if (playerInput != null)
            {
                return playerInput;
            }

            if (playerActors != null)
            {
                for (int i = 0; i < playerActors.Length; i++)
                {
                    if (playerActors[i] != null && playerActors[i].PlayerInput != null)
                    {
                        return playerActors[i].PlayerInput;
                    }
                }
            }

            if (!autoDiscoverMissingReferences)
            {
                return null;
            }

            PlayerActorDeclaration[] declarations = FindObjectsByType<PlayerActorDeclaration>(FindObjectsInactive.Include);
            for (int i = 0; i < declarations.Length; i++)
            {
                if (declarations[i] != null && declarations[i].PlayerInput != null)
                {
                    return declarations[i].PlayerInput;
                }
            }

            return null;
        }

        private InputModeUnityActionMapBinding[] CreateActionMapBindings(string source, string requestReason)
        {
            return new[]
            {
                new InputModeUnityActionMapBinding(InputModeKind.Gameplay, UnityInputActionMapName.From(GameplayActionMapName), true),
                new InputModeUnityActionMapBinding(InputModeKind.PauseOverlay, UnityInputActionMapName.From(UiActionMapName), true),
                new InputModeUnityActionMapBinding(InputModeKind.FrontendMenu, UnityInputActionMapName.From(UiActionMapName), true),
                new InputModeUnityActionMapBinding(InputModeKind.InputLocked, UnityInputActionMapName.From(string.Empty), false)
            };
        }

        private string ResolveReason(string fallbackReason)
        {
            return reason.NormalizeTextOrFallback(fallbackReason);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<PauseInputModeUnityPlayerInputRuntimeBridge>();
            }
        }

        private readonly struct ResolvedReferences
        {
            internal ResolvedReferences(
                PlayerInput playerInput,
                UnityInputTargetSet targetSet,
                PlayerActorSet playerActorSet,
                UnityInputPlayerInputManagerEvidence sessionPlayerInputManagerEvidence,
                UnityInputActionMapEvidence actionMapEvidence,
                InputModeUnityActionMapBinding[] actionMapBindings)
            {
                PlayerInput = playerInput;
                TargetSet = targetSet;
                PlayerActorSet = playerActorSet;
                SessionPlayerInputManagerEvidence = sessionPlayerInputManagerEvidence;
                ActionMapEvidence = actionMapEvidence;
                ActionMapBindings = actionMapBindings ?? Array.Empty<InputModeUnityActionMapBinding>();
            }

            internal PlayerInput PlayerInput { get; }

            internal UnityInputTargetSet TargetSet { get; }

            internal PlayerActorSet PlayerActorSet { get; }

            internal UnityInputPlayerInputManagerEvidence SessionPlayerInputManagerEvidence { get; }

            internal UnityInputActionMapEvidence ActionMapEvidence { get; }

            internal InputModeUnityActionMapBinding[] ActionMapBindings { get; }
        }
    }
}
