using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Marcador explícito e opt-in na raiz física exata de um Actor NonPlayer.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Occurrence Publication")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 owner-aware NonPlayer occurrence declaration; IF-ADR-050 D07 optional composition mount.")]
    public sealed class ActorOccurrencePublication : MonoBehaviour
    {
        [SerializeField] private ActorDeclaration actorDeclaration;
        [Tooltip("Optional mount for replaceable content. Assign it to open composition contributions; keep Camera Observation outside this mount.")]
        [SerializeField] private Transform compositionMount;
        private ActorOccurrenceReferenceRuntime _runtime;

        internal ActorDeclaration ActorDeclaration => actorDeclaration != null
            ? actorDeclaration
            : GetComponent<ActorDeclaration>();

        /// <summary>Exact authored mount for replaceable content, when this publication supports composition.</summary>
        public Transform CompositionMount => compositionMount;

        /// <summary>
        /// Opens a contribution for this exact Actor when it was materialized as
        /// owner-registered content, such as a static Actor in an Activity.
        /// Dynamic producers continue to open through their publication Scope and token.
        /// </summary>
        public bool TryOpenCompositionContribution(
            int priority,
            string source,
            string reason,
            out ActorOccurrenceCompositionContribution contribution,
            out string diagnostic)
        {
            contribution = null;
            if (_runtime == null)
            {
                diagnostic = "Static Actor composition access is not bound to an active Framework composition.";
                return false;
            }

            return _runtime.TryOpenStaticCompositionContribution(
                this,
                priority,
                source,
                reason,
                out contribution,
                out diagnostic);
        }

        internal bool TryValidate(out ActorId actorId, out string diagnostic)
        {
            actorId = default;
            diagnostic = string.Empty;
            ActorDeclaration declaration = ActorDeclaration;
            if (declaration == null || !ReferenceEquals(declaration.gameObject, gameObject))
            {
                diagnostic = "Publication requires an ActorDeclaration on the same GameObject as the physical Actor root.";
                return false;
            }
            if (declaration.ActorKind != ActorKind.NonPlayer)
            {
                diagnostic = "ActorOccurrencePublication accepts only NonPlayer declarations. Player publication is owned by Player Session.";
                return false;
            }
            Transform mount = CompositionMount;
            if (mount != null && mount != transform && !mount.IsChildOf(transform))
            {
                diagnostic = "Published Actor composition mount must be the exact producer-owned root or one of its children.";
                return false;
            }
            try
            {
                actorId = declaration.ActorId;
                return actorId.IsValid;
            }
            catch (Exception exception)
            {
                diagnostic = "Publication ActorDeclaration has an invalid ActorId. " + exception.Message;
                return false;
            }
        }

        internal void BindRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            _runtime = runtime;
            if (actorDeclaration == null)
                actorDeclaration = GetComponent<ActorDeclaration>();
        }

        internal void UnbindRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            if (ReferenceEquals(_runtime, runtime) && !runtime.HasPublication(this))
                _runtime = null;
        }

        private void Reset() => actorDeclaration = GetComponent<ActorDeclaration>();

        private void OnDestroy() => _runtime?.RemoveDestroyedPublication(this);
    }

    /// <summary>Token que autoriza a remoção de uma publicação dinâmica exata de Actor.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 owner-bound NonPlayer publication token.")]
    public readonly struct ActorOccurrencePublicationToken : IEquatable<ActorOccurrencePublicationToken>
    {
        internal ActorOccurrencePublicationToken(ActorOccurrenceId occurrenceId, RuntimeContentOwner owner)
        {
            OccurrenceId = occurrenceId;
            Owner = owner;
        }

        public ActorOccurrenceId OccurrenceId { get; }
        public RuntimeContentOwner Owner { get; }
        public bool IsValid => OccurrenceId.IsValid && Owner.IsValid;
        public bool Equals(ActorOccurrencePublicationToken other) =>
            OccurrenceId == other.OccurrenceId && Owner == other.Owner;
        public override bool Equals(object obj) => obj is ActorOccurrencePublicationToken other && Equals(other);
        public override int GetHashCode() => OccurrenceId.GetHashCode() * 397 ^ Owner.GetHashCode();
        public static bool operator ==(ActorOccurrencePublicationToken left, ActorOccurrencePublicationToken right) => left.Equals(right);
        public static bool operator !=(ActorOccurrencePublicationToken left, ActorOccurrencePublicationToken right) => !left.Equals(right);
    }

    /// <summary>Porta de publicação no escopo do owner para Actors NonPlayer criados por produtores em runtime.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Occurrence Publication Scope")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 owner-bound NonPlayer publisher; IF-ADR-050 D07 exact-occurrence composition access.")]
    public sealed class ActorOccurrencePublicationScope : MonoBehaviour
    {
        private ActorOccurrenceReferenceRuntime _runtime;
        private ActorOccurrenceReferenceRuntime _cleanupRuntime;

        public bool TryPublish(
            ActorOccurrencePublication publication,
            RuntimeContentOwner owner,
            out ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            token = default;
            if (_runtime == null)
            {
                diagnostic = "Publication Scope is not bound to an active Framework composition.";
                return false;
            }
            return _runtime.TryPublishDynamic(this, publication, owner, out token, out diagnostic);
        }

        public bool TryPublishForSession(
            ActorOccurrencePublication publication,
            out ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            token = default;
            if (_runtime == null)
            {
                diagnostic = "Publication Scope is not bound to an active Framework composition.";
                return false;
            }
            return _runtime.TryPublishDynamicForSession(this, publication, out token, out diagnostic);
        }

        public bool TryUnpublish(
            ActorOccurrencePublication publication,
            ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            if (_runtime == null)
            {
                diagnostic = "Publication Scope is not bound to an active Framework composition.";
                return false;
            }
            return _runtime.TryUnpublish(this, publication, token, out diagnostic);
        }

        /// <summary>Opens a keyed contribution for the exact published NonPlayer occurrence and owner token.</summary>
        public bool TryOpenCompositionContribution(
            ActorOccurrencePublication publication,
            ActorOccurrencePublicationToken token,
            int priority,
            string source,
            string reason,
            out ActorOccurrenceCompositionContribution contribution,
            out string diagnostic)
        {
            contribution = null;
            if (_runtime == null)
            {
                diagnostic = "Publication Scope is not bound to an active Framework composition.";
                return false;
            }
            return _runtime.TryOpenDynamicCompositionContribution(
                this,
                publication,
                token,
                priority,
                source,
                reason,
                out contribution,
                out diagnostic);
        }

        public bool TryRetryCompositionCleanup(
            ActorOccurrencePublication publication,
            ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            ActorOccurrenceReferenceRuntime runtime = _runtime ?? _cleanupRuntime;
            if (runtime == null)
            {
                diagnostic = "Publication Scope is not bound to an active Framework composition.";
                return false;
            }
            return runtime.TryRetryCompositionCleanup(this, publication, token, out diagnostic);
        }

        internal void BindRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            _runtime = runtime;
            _cleanupRuntime = null;
        }

        internal void UnbindRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            if (!ReferenceEquals(_runtime, runtime)) return;
            runtime.RemoveScopePublications(this);
            _runtime = null;
            _cleanupRuntime = runtime.HasScopePublications(this) ? runtime : null;
        }

        private void OnDestroy()
        {
            (_runtime ?? _cleanupRuntime)?.RemoveDestroyedScopePublications(this);
        }
    }
}
