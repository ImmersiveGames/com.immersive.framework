using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Specialized declaration for one contextual logical Player Actor.
    /// Actor identity is inherited from ActorDeclaration. PlayerInput belongs to the stable
    /// Local Player Host and may be injected later by an explicit composition adapter.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Player Actor Declaration")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.2 contextual Player Actor declaration separated from the stable Local Player Host.")]
    public sealed class PlayerActorDeclaration : ActorDeclaration
    {
        [SerializeField, HideInInspector]
        [Tooltip("Optional runtime evidence injected from a bound Local Player Host. This is not authored on the Logical Actor Host prefab.")]
        private PlayerInput playerInput;

        public override ActorKind ActorKind => ActorKind.Player;

        public override ActorRole ActorRole => ActorRole.Protagonist;

        public PlayerInput PlayerInput => playerInput;

        public bool HasPlayerInputEvidence => playerInput != null;

        public bool TryCreateDescriptor(
            string source,
            out PlayerActorDescriptor descriptor,
            out PlayerActorSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerActorDeclaration));

            ActorId actorId;
            try
            {
                actorId = ActorId;
            }
            catch (Exception exception)
            {
                issue = PlayerActorSetIssue.BlockingIssue(
                    PlayerActorSetIssueKind.InvalidActorId,
                    string.Empty,
                    normalizedSource,
                    exception.Message);
                return false;
            }

            try
            {
                descriptor = new PlayerActorDescriptor(
                    actorId,
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
                    actorId.StableText,
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
            base.ConfigureForDiagnostics(
                id,
                ActorKind.Player,
                ActorRole.Protagonist,
                label,
                declarationReason);
            playerInput = inputReference;
        }

        internal void BindPlayerInputEvidence(PlayerInput inputReference)
        {
            playerInput = inputReference;
        }

        internal void ClearPlayerInputEvidence(PlayerInput expectedReference)
        {
            if (ReferenceEquals(playerInput, expectedReference))
            {
                playerInput = null;
            }
        }

        protected override void Reset()
        {
            base.Reset();
            playerInput = null;
        }
    }
}
