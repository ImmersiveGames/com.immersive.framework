using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Explicit request for applying Pause/InputMode state to one Unity PlayerInput.
    /// It carries authoring/runtime evidence only; it does not locate services or mutate Unity input by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F38 explicit Pause/InputMode apply request.")]
    internal sealed class PauseInputModeApplyRequest
    {
        internal PauseInputModeApplyRequest(
            FrameworkRuntimeHost runtimeHost,
            PauseRequestKind requestKind,
            string requestId,
            PlayerInput playerInput,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            UnityInputPlayerInputManagerEvidence sessionPlayerInputManagerEvidence,
            UnityInputActionMapEvidence actionMapEvidence,
            InputModeUnityActionMapBinding[] actionMapBindings,
            bool requireSessionPlayerInputManagerEvidence,
            string source,
            string reason)
        {
            RuntimeHost = runtimeHost;
            RequestKind = requestKind;
            RequestId = requestId.NormalizeTextOrFallback(nameof(PauseInputModeApplyRequest));
            PlayerInput = playerInput;
            TargetSet = targetSet;
            PlayerActorSet = playerActorSet;
            SessionPlayerInputManagerEvidence = sessionPlayerInputManagerEvidence;
            ActionMapEvidence = actionMapEvidence;
            ActionMapBindings = CopyBindings(actionMapBindings);
            RequireSessionPlayerInputManagerEvidence = requireSessionPlayerInputManagerEvidence;
            Source = source.NormalizeTextOrFallback(nameof(PauseInputModeApplyRequest));
            Reason = reason.NormalizeText();
        }

        internal FrameworkRuntimeHost RuntimeHost { get; }

        internal PauseRequestKind RequestKind { get; }

        internal string RequestId { get; }

        internal PlayerInput PlayerInput { get; }

        internal UnityInputTargetSet TargetSet { get; }

        internal PlayerActorSet PlayerActorSet { get; }

        internal UnityInputPlayerInputManagerEvidence SessionPlayerInputManagerEvidence { get; }

        internal UnityInputActionMapEvidence ActionMapEvidence { get; }

        internal InputModeUnityActionMapBinding[] ActionMapBindings { get; }

        internal bool RequireSessionPlayerInputManagerEvidence { get; }

        internal string Source { get; }

        internal string Reason { get; }

        private static InputModeUnityActionMapBinding[] CopyBindings(InputModeUnityActionMapBinding[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
            {
                return Array.Empty<InputModeUnityActionMapBinding>();
            }

            var copy = new InputModeUnityActionMapBinding[bindings.Length];
            Array.Copy(bindings, copy, bindings.Length);
            return copy;
        }
    }
}
