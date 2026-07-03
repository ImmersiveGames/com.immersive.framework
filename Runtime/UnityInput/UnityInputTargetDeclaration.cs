using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Unity-facing declaration that a scene/authored object is an integration point for official Unity Input components.
    /// This component is declarative only: it does not enable input, switch action maps, spawn players, bind commands or replace PlayerInput/PlayerInputManager.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Input/Unity Input Integration Target Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A/F30B Unity Input integration target declaration; no InputMode behavior and no custom input manager.")]
    public sealed class UnityInputTargetDeclaration : MonoBehaviour
    {
        [Tooltip("Role this authored object plays as a framework-visible Unity Input integration point. This is not an action map name.")]
        [SerializeField] private UnityInputTargetRole targetRole = UnityInputTargetRole.GlobalUiPause;
        [Tooltip("Stable framework id for this integration point. Do not use GameObject names or scene paths as functional keys.")]
        [SerializeField] private string targetId = "qa.input.target.global-ui-pause";
        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "QA Unity Input Target";
        [Tooltip("Optional evidence reference to Unity's PlayerInput component. The framework does not own or replace PlayerInput.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("When enabled, validation requires PlayerInput evidence on this declaration. This validates official Unity component presence only; it does not activate input.")]
        [SerializeField] private bool requirePlayerInputEvidence;
        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "unity.input.integration.target.declaration";

        public UnityInputTargetRole TargetRole => targetRole;

        public string TargetIdText => targetId.NormalizeText();

        public string DisplayName => displayName.NormalizeTextOrFallback(name);

        public PlayerInput PlayerInput => playerInput;

        public bool HasPlayerInputReference => playerInput != null || GetComponent<PlayerInput>() != null;

        public bool RequiresPlayerInputEvidence => requirePlayerInputEvidence;

        public string Reason => reason.NormalizeText();

        public bool TryCreateDescriptor(
            string source,
            out UnityInputTargetDescriptor descriptor,
            out UnityInputTargetSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputTargetDeclaration));

            if (!Enum.IsDefined(typeof(UnityInputTargetRole), targetRole) || targetRole == UnityInputTargetRole.Unknown)
            {
                issue = UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.InvalidTargetRole,
                    targetRole,
                    TargetIdText,
                    normalizedSource,
                    "Unity Input target declaration has an invalid role.");
                return false;
            }

            string normalizedTargetId = targetId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedTargetId))
            {
                issue = UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.InvalidTargetId,
                    targetRole,
                    string.Empty,
                    normalizedSource,
                    "Unity Input target declaration has an empty target id.");
                return false;
            }

            try
            {
                descriptor = new UnityInputTargetDescriptor(
                    UnityInputTargetId.From(normalizedTargetId),
                    targetRole,
                    HasPlayerInputReference,
                    RequiresPlayerInputEvidence,
                    DisplayName,
                    gameObject.scene.IsValid() ? gameObject.scene.name : string.Empty,
                    gameObject.name,
                    normalizedSource,
                    Reason);
                return true;
            }
            catch (Exception exception)
            {
                issue = UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.InvalidDeclaration,
                    targetRole,
                    normalizedTargetId,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }


        internal void ConfigureForDiagnostics(
            UnityInputTargetRole role,
            string id,
            string label,
            PlayerInput inputReference,
            string declarationReason,
            bool requirePlayerInput = false)
        {
            targetRole = role;
            targetId = id.NormalizeText();
            displayName = label.NormalizeTextOrFallback(name);
            playerInput = inputReference;
            requirePlayerInputEvidence = requirePlayerInput;
            reason = declarationReason.NormalizeText();
        }

        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
