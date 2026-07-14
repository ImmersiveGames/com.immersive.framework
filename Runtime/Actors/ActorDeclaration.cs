using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Unity-facing base declaration for a framework-recognized Actor.
    /// Specialized Actor declarations inherit this component so Actor identity has one authority.
    /// This component does not own Actor lifetime, materialization, movement, input, reset, snapshot or save behavior.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Declaration")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "F45A/P3J.1 base Actor identity declaration shared by specialized Actor declarations.")]
    public class ActorDeclaration : MonoBehaviour, IActor
    {
        [Tooltip("Stable framework ActorId. Do not use GameObject names, tags, scene paths or prefab paths as functional keys.")]
        [SerializeField] private string actorId = "qa.actor.generic";

        [Tooltip("Broad Actor category. Specialized declarations may expose a fixed category instead.")]
        [HideInInspector, SerializeField] private ActorKind actorKind = ActorKind.NonPlayer;

        [Tooltip("Broad gameplay role. Specialized declarations may expose a fixed role instead.")]
        [HideInInspector, SerializeField] private ActorRole actorRole = ActorRole.Neutral;

        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "QA Actor";

        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "actor.declaration";

        public ActorId ActorId => new ActorId(actorId.NormalizeText());

        public virtual ActorKind ActorKind => actorKind;

        public virtual ActorRole ActorRole => actorRole;

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
                    "Actor declaration has an empty ActorId.");
                return false;
            }

            try
            {
                descriptor = new ActorDescriptor(
                    new ActorId(normalizedActorId),
                    ActorKind,
                    ActorRole,
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

        protected virtual void Reset()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
