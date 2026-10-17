using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Superfície de autoria de cena e observação somente leitura de uma referência a ocorrência de Actor.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Occurrence Reference")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 scene binding for read-only Actor occurrence resolution.")]
    public sealed class ActorOccurrenceReferenceBinding : MonoBehaviour
    {
        private enum SelectionMode { PlayerSlot = 10, ActorId = 20 }

        [SerializeField] private SelectionMode selectionMode = SelectionMode.PlayerSlot;
        [SerializeField] private PlayerSlotProfile playerSlot;
        [SerializeField] private ActorDeclaration actorDeclaration;
        [SerializeField] private bool useSessionOwner;
        [SerializeField] private RouteAsset routeOwner;
        [SerializeField] private ActivityAsset activityOwner;

        private ActorOccurrenceReferenceRuntime _runtime;
        private ActorOccurrenceReference _runtimeOverride;
        private bool _hasRuntimeOverride;
        private bool _hasCurrent;
        private ActorOccurrenceResolution _current;

        public event Action<ActorOccurrenceReferenceChange> Changed;

        public ActorOccurrenceResolution Current
        {
            get
            {
                TryGetCurrent(out ActorOccurrenceResolution result);
                return result;
            }
        }

        public bool TryGetCurrent(out ActorOccurrenceResolution result)
        {
            if (_runtime == null)
            {
                result = new ActorOccurrenceResolution(
                    ActorOccurrenceResolutionStatus.Unavailable,
                    default,
                    "Actor occurrence reference is not bound to an active Framework composition.");
                return false;
            }
            if (!TryGetReference(out ActorOccurrenceReference reference, out string issue))
            {
                result = new ActorOccurrenceResolution(
                    ActorOccurrenceResolutionStatus.InvalidReference,
                    default,
                    issue);
                UpdateCurrent(result);
                return false;
            }
            result = _runtime.Resolve(reference);
            UpdateCurrent(result);
            return result.Succeeded;
        }

        public bool TrySetReference(ActorOccurrenceReference reference, out string diagnostic)
        {
            if (!reference.IsValid)
            {
                diagnostic = "A valid Actor occurrence reference is required.";
                return false;
            }
            _runtimeOverride = reference;
            _hasRuntimeOverride = true;
            diagnostic = string.Empty;
            Refresh();
            return true;
        }

        public bool TrySetActorReference(ActorId actorId, RuntimeContentOwner owner, out string diagnostic) =>
            TrySetReference(ActorOccurrenceReference.ForActorId(actorId, owner), out diagnostic);

        public bool TrySetPlayerSlot(PlayerSlotProfile profile, out string diagnostic)
        {
            if (profile == null)
            {
                diagnostic = "Player Slot Profile is required.";
                return false;
            }
            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slot, out diagnostic))
                return false;
            _runtimeOverride = ActorOccurrenceReference.ForPlayerSlot(slot);
            _hasRuntimeOverride = true;
            diagnostic = string.Empty;
            Refresh();
            return true;
        }

        public bool TryGetReference(out ActorOccurrenceReference reference, out string diagnostic)
        {
            if (_hasRuntimeOverride)
            {
                reference = _runtimeOverride;
                diagnostic = reference.IsValid ? string.Empty : "Configured runtime Actor occurrence reference is invalid.";
                return reference.IsValid;
            }

            if (selectionMode == SelectionMode.PlayerSlot)
            {
                if (playerSlot == null)
                {
                    reference = default;
                    diagnostic = "Player Slot Profile is required.";
                    return false;
                }
                if (!playerSlot.TryGetPlayerSlotId(out PlayerSlotId slot, out diagnostic))
                {
                    reference = default;
                    return false;
                }
                reference = ActorOccurrenceReference.ForPlayerSlot(slot);
                return true;
            }

            if (selectionMode != SelectionMode.ActorId || actorDeclaration == null)
            {
                reference = default;
                diagnostic = "Actor identity selection requires an ActorDeclaration.";
                return false;
            }
            int ownerSelectors = (useSessionOwner ? 1 : 0) + (routeOwner != null ? 1 : 0) + (activityOwner != null ? 1 : 0);
            if (ownerSelectors > 1)
            {
                reference = default;
                diagnostic = "Select at most one exact Session, Route or Activity owner.";
                return false;
            }

            try
            {
                ActorId id = actorDeclaration.ActorId;
                if (useSessionOwner)
                {
                    if (_runtime == null || !_runtime.TryGetSessionOwner(out RuntimeContentOwner sessionOwner))
                    {
                        reference = default;
                        diagnostic = "Exact Session owner is unavailable before Framework Session composition.";
                        return false;
                    }
                    reference = ActorOccurrenceReference.ForActorId(id, sessionOwner);
                }
                else if (routeOwner != null)
                {
                    if (!routeOwner.HasValidRouteId)
                    {
                        reference = default;
                        diagnostic = "Actor reference Route owner has an invalid RouteId.";
                        return false;
                    }
                    reference = ActorOccurrenceReference.ForActorId(id, RuntimeContentOwner.Route(
                        routeOwner.RouteId.StableText,
                        routeOwner.RouteName,
                        RuntimeDefinitionToken.FromUnityObject(routeOwner)));
                }
                else if (activityOwner != null)
                {
                    if (!activityOwner.HasValidActivityId)
                    {
                        reference = default;
                        diagnostic = "Actor reference Activity owner has an invalid ActivityId.";
                        return false;
                    }
                    reference = ActorOccurrenceReference.ForActorId(id, RuntimeContentOwner.Activity(
                        activityOwner.ActivityId.StableText,
                        activityOwner.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(activityOwner)));
                }
                else
                {
                    reference = ActorOccurrenceReference.ForActorId(id);
                }
                diagnostic = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                reference = default;
                diagnostic = "Actor identity selection is invalid. " + exception.Message;
                return false;
            }
        }

        internal void BindRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            _runtime = runtime;
            Refresh();
        }

        internal void Refresh()
        {
            if (_runtime == null)
            {
                UpdateCurrent(new ActorOccurrenceResolution(
                    ActorOccurrenceResolutionStatus.Unavailable,
                    default,
                    "Actor occurrence reference is not bound to an active Framework composition."));
                return;
            }
            if (!TryGetReference(out ActorOccurrenceReference reference, out string issue))
            {
                UpdateCurrent(new ActorOccurrenceResolution(
                    ActorOccurrenceResolutionStatus.InvalidReference,
                    default,
                    issue));
                return;
            }
            UpdateCurrent(_runtime.Resolve(reference));
        }

        internal void UnbindRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            if (!ReferenceEquals(_runtime, runtime)) return;
            ActorOccurrenceResolution previous = _current;
            _runtime = null;
            var released = new ActorOccurrenceResolution(
                ActorOccurrenceResolutionStatus.Unavailable,
                default,
                "Actor occurrence consumer scope was released.");
            _current = released;
            _hasCurrent = true;
            Dispatch(new ActorOccurrenceReferenceChange(
                previous,
                released,
                ActorOccurrenceReferenceChangeKind.BindingReleased));
        }

        private void UpdateCurrent(ActorOccurrenceResolution next)
        {
            if (_hasCurrent && _current.SameState(next))
            {
                _current = next;
                return;
            }

            ActorOccurrenceResolution previous = _current;
            bool hadCurrent = _hasCurrent;
            _current = next;
            _hasCurrent = true;
            if (!hadCurrent) return;

            ActorOccurrenceReferenceChangeKind kind = next.Succeeded
                ? previous.Succeeded
                    ? ActorOccurrenceReferenceChangeKind.Replaced
                    : ActorOccurrenceReferenceChangeKind.Available
                : next.Status == ActorOccurrenceResolutionStatus.Stale
                    ? ActorOccurrenceReferenceChangeKind.Invalidated
                    : ActorOccurrenceReferenceChangeKind.Unavailable;
            Dispatch(new ActorOccurrenceReferenceChange(previous, next, kind));
        }

        private void Dispatch(ActorOccurrenceReferenceChange change)
        {
            Action<ActorOccurrenceReferenceChange> handlers = Changed;
            if (handlers == null) return;
            Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                try { ((Action<ActorOccurrenceReferenceChange>)invocationList[i])(change); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        private void OnValidate()
        {
            if (actorDeclaration == null && selectionMode == SelectionMode.ActorId)
                actorDeclaration = GetComponent<ActorDeclaration>();
        }

        private void OnDestroy()
        {
            if (_runtime != null)
                _runtime.UnbindConsumer(this);
        }
    }
}
