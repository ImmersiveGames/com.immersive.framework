using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Unity-facing declaration for a framework-recognized PlayerActor.
    /// A PlayerActor is an IActor and must provide evidence of Unity's PlayerInput component.
    /// This component does not move the player, switch action maps, join players, spawn actors or replace PlayerInput/PlayerInputManager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [AddComponentMenu("Immersive Framework/Actors/Player Actor Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A/F45A PlayerActor identity declaration requiring Unity PlayerInput evidence.")]
    public sealed class PlayerActorDeclaration : MonoBehaviour, IActor
    {
        [Tooltip("Stable framework actor id. Do not use GameObject names or scene paths as functional keys.")]
        [SerializeField] private string actorId = "qa.actor.player.primary";
        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "QA Player Actor";
        [Tooltip("Optional explicit PlayerInput evidence. If empty, the declaration checks PlayerInput on the same GameObject.")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "player.actor.declaration";

        public ActorId ActorId => new ActorId(actorId.NormalizeText());

        public ActorKind ActorKind => ActorKind.Player;

        public ActorRole ActorRole => ActorRole.Protagonist;

        public string ActorDisplayName => displayName.NormalizeTextOrFallback(name);

        public PlayerInput PlayerInput => playerInput != null ? playerInput : GetComponent<PlayerInput>();

        public bool HasPlayerInputEvidence => PlayerInput != null;

        public string Reason => reason.NormalizeText();

        public bool TryCreateDescriptor(
            string source,
            out PlayerActorDescriptor descriptor,
            out PlayerActorSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerActorDeclaration));
            string normalizedActorId = actorId.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                issue = PlayerActorSetIssue.BlockingIssue(
                    PlayerActorSetIssueKind.InvalidActorId,
                    string.Empty,
                    normalizedSource,
                    "PlayerActor declaration has an empty actor id.");
                return false;
            }

            try
            {
                descriptor = new PlayerActorDescriptor(
                    new ActorId(normalizedActorId),
                    ActorRole,
                    HasPlayerInputEvidence,
                    ActorDisplayName,
                    gameObject.scene.IsValid() ? gameObject.scene.name : string.Empty,
                    gameObject.name,
                    normalizedSource,
                    Reason);
                return true;
            }
            catch (Exception exception)
            {
                issue = PlayerActorSetIssue.BlockingIssue(
                    PlayerActorSetIssueKind.InvalidDeclaration,
                    normalizedActorId,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }

        internal void ConfigureForDiagnostics(
            string id,
            string label,
            PlayerInput inputReference,
            string declarationReason)
        {
            actorId = id.NormalizeText();
            displayName = label.NormalizeTextOrFallback(name);
            playerInput = inputReference;
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
