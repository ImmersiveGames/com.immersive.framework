using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Owner-bound anchor for on-demand Actor occurrence queries and physical recognition. The Framework
    /// binds it when the Session, Route or Activity content owner whose roots contain it commits, and
    /// unbinds it on owner rollback, owner release or host teardown. The owner defines lifetime only; it
    /// does not restrict which committed occurrences can be resolved. Hand its typed query interfaces to
    /// plain C# consumers explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Occurrence Query")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Owner-bound on-demand Actor occurrence query and physical recognition anchor.")]
    public sealed class ActorOccurrenceQueryBinding : MonoBehaviour, IActorOccurrenceQuery, IActorOccurrenceRecognitionQuery
    {
        private const string UnboundDiagnostic =
            "Actor occurrence query is not bound to a committed Framework owner.";
        private const string RecognitionUnboundDiagnostic =
            "Actor occurrence recognition query is not bound to a committed Framework owner.";

        private ActorOccurrenceReferenceRuntime _runtime;
        private RuntimeContentOwner _owner;

        /// <inheritdoc />
        public ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference)
        {
            ActorOccurrenceReferenceRuntime runtime = _runtime;
            return runtime == null
                ? new ActorOccurrenceResolution(ActorOccurrenceResolutionStatus.Unavailable, default, UnboundDiagnostic)
                : runtime.Resolve(reference);
        }

        /// <inheritdoc />
        public ActorOccurrenceRecognitionResult Recognize(Collider candidate)
        {
            if (ReferenceEquals(candidate, null))
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Invalid,
                    "Actor occurrence recognition requires a live Collider candidate.");
            }

            if (candidate == null)
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Unavailable,
                    "The supplied Collider candidate has been destroyed and is no longer physically available.");
            }

            ActorDeclaration declaration = candidate.GetComponentInParent<ActorDeclaration>(true);
            if (declaration == null)
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Unrecognized,
                    "No Actor declaration exists in the Collider candidate's ancestor chain.");
            }

            if (!declaration.TryCreateDescriptor(
                    nameof(IActorOccurrenceRecognitionQuery),
                    out ActorDescriptor descriptor,
                    out ActorSetIssue issue))
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Invalid,
                    "The nearest Actor declaration is invalid. " + issue.Message);
            }

            ActorKind currentActorKind;
            ActorId currentActorId;
            try
            {
                currentActorKind = declaration.ActorKind;
                currentActorId = declaration.ActorId;
            }
            catch (Exception exception)
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Invalid,
                    "The nearest Actor declaration became invalid during recognition. " + exception.Message);
            }

            if ((currentActorKind != ActorKind.Player && currentActorKind != ActorKind.NonPlayer) ||
                !currentActorId.IsValid || currentActorId != descriptor.ActorId ||
                currentActorKind != descriptor.ActorKind)
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Invalid,
                    "The nearest Actor declaration changed during recognition.");
            }

            if (declaration == null || candidate == null ||
                candidate.GetComponentInParent<ActorDeclaration>(true) != declaration)
            {
                return new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Unavailable,
                    "The Collider candidate or its nearest Actor declaration changed during recognition.");
            }

            ActorOccurrenceReferenceRuntime runtime = _runtime;
            return runtime == null
                ? new ActorOccurrenceRecognitionResult(
                    ActorOccurrenceRecognitionStatus.Unavailable,
                    RecognitionUnboundDiagnostic)
                : runtime.Recognize(declaration, descriptor);
        }

        internal RuntimeContentOwner BoundOwner => _owner;

        internal bool CanBindRuntime(
            ActorOccurrenceReferenceRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (runtime == null || !owner.IsValid)
            {
                diagnostic = "Actor occurrence query binding requires an exact runtime and valid lifecycle owner.";
                return false;
            }

            if (_runtime != null)
            {
                diagnostic = _runtime == runtime && _owner == owner
                    ? "Actor occurrence query binding is already bound to this owner."
                    : $"Actor occurrence query binding is already bound to owner '{_owner.StableText}'.";
                return false;
            }

            return true;
        }

        internal bool TryBindRuntime(
            ActorOccurrenceReferenceRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            if (!CanBindRuntime(runtime, owner, out diagnostic)) return false;
            _runtime = runtime;
            _owner = owner;
            return true;
        }

        internal void UnbindRuntime(ActorOccurrenceReferenceRuntime runtime, RuntimeContentOwner owner)
        {
            if (_runtime != runtime || _owner != owner) return;
            _runtime = null;
            _owner = default;
        }
    }
}
