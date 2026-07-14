using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Specialized Player Actor declaration.
    /// Actor identity and the generic Actor descriptor are inherited from ActorDeclaration.
    /// P3J.1 preserves same-object PlayerInput evidence for P3G compatibility; Local Player Host
    /// separation is introduced by P3J.2.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [AddComponentMenu("Immersive Framework/Actors/Player Actor Declaration")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "F31A/F45A/P3J.1 Player Actor declaration inheriting the canonical Actor identity base.")]
    public sealed class PlayerActorDeclaration : ActorDeclaration
    {
        [Tooltip("Optional explicit PlayerInput evidence. If empty, the declaration checks PlayerInput on the same GameObject.")]
        [SerializeField] private PlayerInput playerInput;

        public override ActorKind ActorKind => ActorKind.Player;

        public override ActorRole ActorRole => ActorRole.Protagonist;

        public PlayerInput PlayerInput => playerInput != null ? playerInput : GetComponent<PlayerInput>();

        public bool HasPlayerInputEvidence => PlayerInput != null;

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

        protected override void Reset()
        {
            base.Reset();
            playerInput = GetComponent<PlayerInput>();
        }
    }
}
