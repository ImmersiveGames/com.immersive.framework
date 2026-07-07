using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Unity-facing declaration for a framework-recognized Actor.
    /// This component declares identity only. It does not own actor lifetime, materialization, movement, input, reset, snapshot or save behavior.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A generic actor identity declaration.")]
    public sealed class ActorDeclaration : MonoBehaviour, IActor
    {
        [Tooltip("Stable framework actor id. Do not use GameObject names, tags, scene paths or prefab paths as functional keys.")]
        [SerializeField] private string actorId = "qa.actor.generic";
        [Tooltip("Broad actor category. This is not a gameplay taxonomy.")]
        [SerializeField] private ActorKind actorKind = ActorKind.NonPlayer;
        [Tooltip("Broad gameplay role. This is not a project-specific enemy/class taxonomy.")]
        [SerializeField] private ActorRole actorRole = ActorRole.Neutral;
        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "QA Actor";
        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "actor.declaration";

        public ActorId ActorId => new ActorId(actorId.NormalizeText());

        public ActorKind ActorKind => actorKind;

        public ActorRole ActorRole => actorRole;

        public string ActorDisplayName => displayName.NormalizeTextOrFallback(name);

        public string Reason => reason.NormalizeText();

        public bool TryCreateDescriptor(
            string source,
            out ActorDescriptor descriptor,
            out ActorSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(ActorDeclaration));
            string normalizedActorId = actorId.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                issue = ActorSetIssue.BlockingIssue(
                    ActorSetIssueKind.InvalidActorId,
                    string.Empty,
                    normalizedSource,
                    "Actor declaration has an empty actor id.");
                return false;
            }

            try
            {
                descriptor = new ActorDescriptor(
                    new ActorId(normalizedActorId),
                    actorKind,
                    actorRole,
                    ActorDisplayName,
                    gameObject.scene.IsValid() ? gameObject.scene.name : string.Empty,
                    gameObject.name,
                    normalizedSource,
                    Reason);
                return true;
            }
            catch (Exception exception)
            {
                issue = ActorSetIssue.BlockingIssue(
                    ActorSetIssueKind.InvalidDeclaration,
                    normalizedActorId,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }

        internal void ConfigureForDiagnostics(
            string id,
            ActorKind kind,
            ActorRole role,
            string label,
            string declarationReason)
        {
            actorId = id.NormalizeText();
            actorKind = kind;
            actorRole = role;
            displayName = label.NormalizeTextOrFallback(name);
            reason = declarationReason.NormalizeText();
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
