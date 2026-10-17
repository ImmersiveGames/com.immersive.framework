using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Publicação e resolução de ocorrências no escopo do FrameworkRuntimeHost.</summary>
    internal sealed class ActorOccurrenceReferenceRuntime
    {
        private sealed class OwnerRegistration
        {
            internal OwnerRegistration(RuntimeContentOwner owner, List<ActorOccurrencePublication> publishers)
            {
                Owner = owner;
                Publishers = publishers;
            }
            internal RuntimeContentOwner Owner { get; }
            internal List<ActorOccurrencePublication> Publishers { get; }
        }

        private sealed class PublicationRecord
        {
            internal PublicationRecord(
                ActorOccurrenceId id,
                ActorId actorId,
                ActorOccurrencePublication publisher,
                RuntimeContentOwner owner,
                ActorOccurrencePublicationScope publicationScope = null)
            {
                Id = id;
                ActorId = actorId;
                Publisher = publisher;
                Owner = owner;
                PublicationScope = publicationScope;
            }
            internal ActorOccurrenceId Id { get; }
            internal ActorId ActorId { get; }
            internal ActorOccurrencePublication Publisher { get; }
            internal RuntimeContentOwner Owner { get; }
            internal ActorOccurrencePublicationScope PublicationScope { get; }
        }

        private sealed class PlayerRecord
        {
            internal PlayerRecord(PlayerActorPhysicalOccurrence physical, ActorOccurrence occurrence)
            {
                Physical = physical;
                Occurrence = occurrence;
            }
            internal PlayerActorPhysicalOccurrence Physical { get; }
            internal ActorOccurrence Occurrence { get; }
        }

        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _stagedOwners = new();
        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _committedOwners = new();
        private readonly Dictionary<ActorOccurrenceId, PublicationRecord> _publications = new();
        private readonly Dictionary<ActorOccurrencePublicationScope, List<PublicationRecord>> _suspendedScopePublications = new();
        private readonly Dictionary<PlayerSlotId, PlayerRecord> _players = new();
        private readonly List<ActorOccurrenceReferenceBinding> _consumers = new();
        private IPlayerPreparedActorOccurrenceSource _playerSource;
        private RuntimeContentOwner _sessionOwner;
        private long _sequence;

        internal void SetPlayerSource(IPlayerPreparedActorOccurrenceSource playerSource)
        {
            if (ReferenceEquals(_playerSource, playerSource)) return;
            if (_playerSource != null)
                _playerSource.CurrentActorInvalidated -= HandlePlayerActorInvalidated;
            _playerSource = playerSource;
            if (_playerSource != null)
                _playerSource.CurrentActorInvalidated += HandlePlayerActorInvalidated;
            RefreshAll();
        }

        internal void SetSessionOwner(RuntimeContentOwner owner)
        {
            _sessionOwner = owner;
        }

        internal bool TryGetSessionOwner(out RuntimeContentOwner owner)
        {
            owner = _sessionOwner;
            return owner.IsValid;
        }

        internal bool TryRegisterOwnerContent(
            RuntimeContentOwner owner,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid || roots == null)
            {
                diagnostic = "Actor occurrence publication requires an exact valid owner and materialized roots.";
                return false;
            }
            if (_stagedOwners.ContainsKey(owner) || _committedOwners.ContainsKey(owner))
            {
                diagnostic = $"Actor occurrence owner '{owner.StableText}' is already staged or committed.";
                return false;
            }

            var publishers = new List<ActorOccurrencePublication>();
            var seen = new HashSet<ActorOccurrencePublication>();
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    diagnostic = $"Actor occurrence owner '{owner.StableText}' contains a missing materialized root.";
                    return false;
                }
                ActorOccurrencePublication[] candidates = root.GetComponentsInChildren<ActorOccurrencePublication>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    ActorOccurrencePublication publisher = candidates[index];
                    if (publisher == null || !seen.Add(publisher)) continue;
                    if (!publisher.TryValidate(out _, out string issue))
                    {
                        diagnostic = $"Actor occurrence publication under owner '{owner.StableText}' is invalid. {issue}";
                        return false;
                    }
                    if (IsPublished(publisher))
                    {
                        diagnostic = $"Actor occurrence publisher '{publisher.name}' is already owned by another committed scope.";
                        return false;
                    }
                    publishers.Add(publisher);
                }
            }

            _stagedOwners.Add(owner, new OwnerRegistration(owner, publishers));
            return true;
        }

        internal bool TryCommitOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid || !_stagedOwners.TryGetValue(owner, out OwnerRegistration registration))
            {
                diagnostic = $"Actor occurrence owner '{owner.StableText}' has no staged publication registration.";
                return false;
            }

            for (int index = 0; index < registration.Publishers.Count; index++)
            {
                ActorOccurrencePublication publisher = registration.Publishers[index];
                if (publisher == null)
                {
                    diagnostic = "Actor occurrence publication was destroyed before owner commit.";
                    RemoveOwnerPublications(owner);
                    return false;
                }
                if (!publisher.TryValidate(out ActorId actorId, out string issue))
                {
                    diagnostic = "Actor occurrence publication became invalid before owner commit. " + issue;
                    RemoveOwnerPublications(owner);
                    return false;
                }
                if (!TryAddPublication(null, publisher, actorId, owner, out _, out diagnostic))
                {
                    RemoveOwnerPublications(owner);
                    return false;
                }
            }

            _stagedOwners.Remove(owner);
            _committedOwners.Add(owner, registration);
            return true;
        }

        internal bool TryRollbackOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid)
            {
                diagnostic = "Actor occurrence rollback requires an exact valid owner.";
                return false;
            }
            _stagedOwners.Remove(owner);
            if (_committedOwners.ContainsKey(owner))
                RemoveOwner(owner);
            return true;
        }

        internal bool TryReleaseOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid)
            {
                diagnostic = "Actor occurrence release requires an exact valid owner.";
                return false;
            }
            _stagedOwners.Remove(owner);
            if (_committedOwners.ContainsKey(owner))
                RemoveOwner(owner);
            return true;
        }

        internal bool TryPublishDynamic(
            ActorOccurrencePublicationScope scope,
            ActorOccurrencePublication publisher,
            RuntimeContentOwner owner,
            out ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            token = default;
            diagnostic = string.Empty;
            if (scope == null || publisher == null || !owner.IsValid || !_committedOwners.ContainsKey(owner))
            {
                diagnostic = "Dynamic Actor publication requires an explicit Scope and an owner already committed by Framework composition.";
                return false;
            }
            if (!publisher.TryValidate(out ActorId actorId, out diagnostic)) return false;
            publisher.BindRuntime(this);
            if (IsPublished(publisher))
            {
                diagnostic = "This physical Actor root already has an active publication.";
                return false;
            }
            if (!TryAddPublication(scope, publisher, actorId, owner, out token, out diagnostic)) return false;
            RemoveSuspendedPublisher(publisher);
            RefreshAll();
            return true;
        }

        internal bool TryPublishDynamicForSession(
            ActorOccurrencePublicationScope scope,
            ActorOccurrencePublication publisher,
            out ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            if (!_sessionOwner.IsValid)
            {
                token = default;
                diagnostic = "The active Framework Session owner is unavailable.";
                return false;
            }
            return TryPublishDynamic(scope, publisher, _sessionOwner, out token, out diagnostic);
        }

        internal bool TryUnpublish(
            ActorOccurrencePublicationScope scope,
            ActorOccurrencePublication publisher,
            ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (publisher == null || !token.IsValid ||
                !_publications.TryGetValue(token.OccurrenceId, out PublicationRecord record) ||
                !ReferenceEquals(record.Publisher, publisher) ||
                !ReferenceEquals(record.PublicationScope, scope) || record.Owner != token.Owner)
            {
                diagnostic = "Publication token is invalid, stale or owned by another Actor root.";
                return false;
            }
            RemovePublication(record);
            RefreshAll();
            return true;
        }

        internal void RestoreScopePublications(ActorOccurrencePublicationScope scope)
        {
            if (scope == null || !_suspendedScopePublications.TryGetValue(scope, out List<PublicationRecord> records))
                return;
            _suspendedScopePublications.Remove(scope);
            bool restoredAny = false;
            for (int index = 0; index < records.Count; index++)
            {
                PublicationRecord record = records[index];
                if (!_committedOwners.ContainsKey(record.Owner) || record.Publisher == null ||
                    IsPublished(record.Publisher) ||
                    !record.Publisher.TryValidate(out ActorId actorId, out _) || actorId != record.ActorId)
                    continue;
                _publications.Add(record.Id, record);
                restoredAny = true;
            }
            if (restoredAny) RefreshAll();
        }

        internal bool HasPublication(ActorOccurrencePublication publisher)
        {
            if (publisher == null) return false;
            if (IsPublished(publisher)) return true;
            foreach (List<PublicationRecord> records in _suspendedScopePublications.Values)
                for (int index = 0; index < records.Count; index++)
                    if (ReferenceEquals(records[index].Publisher, publisher)) return true;
            return false;
        }

        internal bool HasScopePublications(ActorOccurrencePublicationScope scope) =>
            scope != null && _suspendedScopePublications.ContainsKey(scope);

        internal ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference)
        {
            if (!reference.IsValid)
                return Result(ActorOccurrenceResolutionStatus.InvalidReference, "Actor occurrence selector is invalid.");

            if (reference.Kind == ActorOccurrenceReferenceKind.PlayerSlot)
            {
                if (TryGetPlayer(reference.PlayerSlotId, out ActorOccurrence player))
                    return MatchExpectedOccurrence(reference, player);
                return Result(reference.IsExactOccurrence
                    ? ActorOccurrenceResolutionStatus.Stale
                    : ActorOccurrenceResolutionStatus.Unavailable,
                    "No current physical Player Actor occurrence is available for the selected Slot.");
            }

            var candidates = new List<ActorOccurrence>();
            bool stalePublication = false;
            foreach (PublicationRecord record in _publications.Values)
            {
                if (record.ActorId != reference.ActorId ||
                    reference.HasOwner && record.Owner != reference.Owner) continue;
                if (record.Publisher == null || !record.Publisher.TryValidate(out ActorId currentId, out _) ||
                    currentId != record.ActorId)
                {
                    stalePublication = true;
                    continue;
                }
                candidates.Add(CreatePublicationOccurrence(record));
            }

            if (_playerSource != null)
            {
                var physicalPlayers = new List<PlayerActorPhysicalOccurrence>();
                _playerSource.CollectCurrentPhysicalActorOccurrences(physicalPlayers);
                for (int index = 0; index < physicalPlayers.Count; index++)
                {
                    PlayerActorPhysicalOccurrence physical = physicalPlayers[index];
                    if (physical.ActorId != reference.ActorId ||
                        reference.HasOwner && physical.Owner != reference.Owner) continue;
                    if (TryGetPlayer(physical.PlayerSlotId, out ActorOccurrence player))
                        candidates.Add(player);
                }
            }

            if (reference.IsExactOccurrence)
            {
                for (int index = 0; index < candidates.Count; index++)
                    if (candidates[index].Id == reference.ExpectedOccurrence)
                        return Result(ActorOccurrenceResolutionStatus.Resolved, string.Empty, candidates[index]);
                return Result(ActorOccurrenceResolutionStatus.Stale,
                    "The exact Actor occurrence was released or replaced.");
            }

            if (candidates.Count == 1)
                return Result(ActorOccurrenceResolutionStatus.Resolved, string.Empty, candidates[0]);
            if (candidates.Count > 1)
                return Result(ActorOccurrenceResolutionStatus.Ambiguous,
                    $"ActorId '{reference.ActorId.StableText}' resolves to {candidates.Count} current occurrences. Select an exact owner.");
            return Result(stalePublication
                    ? ActorOccurrenceResolutionStatus.Stale
                    : ActorOccurrenceResolutionStatus.Unavailable,
                stalePublication
                    ? "The published Actor root or declaration is no longer valid."
                    : "No committed physical Actor occurrence matches this selector.");
        }

        internal void BindConsumer(ActorOccurrenceReferenceBinding consumer)
        {
            if (consumer == null) return;
            if (!_consumers.Contains(consumer)) _consumers.Add(consumer);
            consumer.BindRuntime(this);
        }

        internal void UnbindConsumer(ActorOccurrenceReferenceBinding consumer)
        {
            if (consumer == null) return;
            _consumers.Remove(consumer);
            consumer.UnbindRuntime(this);
        }

        internal void RefreshAll()
        {
            for (int index = _consumers.Count - 1; index >= 0; index--)
            {
                ActorOccurrenceReferenceBinding consumer = _consumers[index];
                if (consumer == null)
                {
                    _consumers.RemoveAt(index);
                    continue;
                }
                consumer.Refresh();
            }
        }

        internal void RemoveDestroyedPublication(ActorOccurrencePublication publisher)
        {
            if (ReferenceEquals(publisher, null)) return;
            var records = new List<PublicationRecord>();
            foreach (PublicationRecord record in _publications.Values)
                if (ReferenceEquals(record.Publisher, publisher)) records.Add(record);
            for (int index = 0; index < records.Count; index++) RemovePublication(records[index]);
            RemoveSuspendedPublisher(publisher);
            RefreshAll();
        }

        internal void RemoveScopePublications(ActorOccurrencePublicationScope scope)
        {
            if (scope == null) return;
            var records = new List<PublicationRecord>();
            foreach (PublicationRecord record in _publications.Values)
                if (ReferenceEquals(record.PublicationScope, scope)) records.Add(record);
            if (records.Count == 0) return;
            for (int index = 0; index < records.Count; index++) RemovePublication(records[index]);
            _suspendedScopePublications[scope] = records;
            RefreshAll();
        }

        internal void RemoveDestroyedScopePublications(ActorOccurrencePublicationScope scope)
        {
            if (scope == null) return;
            RemoveScopePublications(scope);
            _suspendedScopePublications.Remove(scope);
        }

        private bool TryGetPlayer(PlayerSlotId slot, out ActorOccurrence occurrence)
        {
            occurrence = default;
            if (_playerSource == null || !_playerSource.TryGetCurrentPhysicalActorOccurrence(
                    slot, out PlayerActorPhysicalOccurrence physical))
            {
                _players.Remove(slot);
                return false;
            }

            if (!physical.IsValid)
            {
                _players.Remove(slot);
                return false;
            }
            if (_players.TryGetValue(slot, out PlayerRecord previous) &&
                previous.Physical.Preparation == physical.Preparation &&
                ReferenceEquals(previous.Physical.ActorRoot, physical.ActorRoot))
            {
                occurrence = previous.Occurrence;
                return true;
            }

            occurrence = new ActorOccurrence(
                NextOccurrenceId(),
                physical.ActorId,
                physical.ActorRoot,
                physical.Owner,
                physical.PlayerSlotId);
            _players[slot] = new PlayerRecord(physical, occurrence);
            return true;
        }

        private bool TryAddPublication(
            ActorOccurrencePublicationScope publicationScope,
            ActorOccurrencePublication publisher,
            ActorId actorId,
            RuntimeContentOwner owner,
            out ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            token = default;
            diagnostic = string.Empty;
            if (IsPublished(publisher))
            {
                diagnostic = "This physical Actor root already has an active publication.";
                return false;
            }
            ActorOccurrenceId id = NextOccurrenceId();
            var record = new PublicationRecord(id, actorId, publisher, owner, publicationScope);
            _publications.Add(id, record);
            token = new ActorOccurrencePublicationToken(id, owner);
            return true;
        }

        private void RemoveOwner(RuntimeContentOwner owner)
        {
            _committedOwners.Remove(owner);
            var records = new List<PublicationRecord>();
            foreach (PublicationRecord record in _publications.Values)
                if (record.Owner == owner) records.Add(record);
            for (int index = 0; index < records.Count; index++) RemovePublication(records[index]);
            RemoveSuspendedOwner(owner);
        }

        private void RemoveOwnerPublications(RuntimeContentOwner owner)
        {
            var records = new List<PublicationRecord>();
            foreach (PublicationRecord record in _publications.Values)
                if (record.Owner == owner) records.Add(record);
            for (int index = 0; index < records.Count; index++) RemovePublication(records[index]);
        }

        private void RemovePublication(PublicationRecord record)
        {
            _publications.Remove(record.Id);
        }

        private void RemoveSuspendedPublisher(ActorOccurrencePublication publisher)
        {
            var emptyScopes = new List<ActorOccurrencePublicationScope>();
            foreach (KeyValuePair<ActorOccurrencePublicationScope, List<PublicationRecord>> pair in _suspendedScopePublications)
            {
                pair.Value.RemoveAll(record => ReferenceEquals(record.Publisher, publisher));
                if (pair.Value.Count == 0) emptyScopes.Add(pair.Key);
            }
            for (int index = 0; index < emptyScopes.Count; index++)
                _suspendedScopePublications.Remove(emptyScopes[index]);
        }

        private void RemoveSuspendedOwner(RuntimeContentOwner owner)
        {
            var emptyScopes = new List<ActorOccurrencePublicationScope>();
            foreach (KeyValuePair<ActorOccurrencePublicationScope, List<PublicationRecord>> pair in _suspendedScopePublications)
            {
                pair.Value.RemoveAll(record => record.Owner == owner);
                if (pair.Value.Count == 0) emptyScopes.Add(pair.Key);
            }
            for (int index = 0; index < emptyScopes.Count; index++)
                _suspendedScopePublications.Remove(emptyScopes[index]);
        }

        private bool IsPublished(ActorOccurrencePublication publisher)
        {
            foreach (PublicationRecord record in _publications.Values)
                if (ReferenceEquals(record.Publisher, publisher)) return true;
            return false;
        }

        private ActorOccurrence CreatePublicationOccurrence(PublicationRecord record) =>
            new ActorOccurrence(record.Id, record.ActorId, record.Publisher.gameObject, record.Owner, default);

        private ActorOccurrenceId NextOccurrenceId()
        {
            if (_sequence == long.MaxValue)
                throw new InvalidOperationException("Actor occurrence sequence exhausted for this Framework host.");
            return new ActorOccurrenceId(++_sequence);
        }

        private static ActorOccurrenceResolution MatchExpectedOccurrence(
            ActorOccurrenceReference reference,
            ActorOccurrence occurrence)
        {
            return reference.IsExactOccurrence && reference.ExpectedOccurrence != occurrence.Id
                ? Result(ActorOccurrenceResolutionStatus.Stale, "The exact Actor occurrence was replaced.")
                : Result(ActorOccurrenceResolutionStatus.Resolved, string.Empty, occurrence);
        }

        private void HandlePlayerActorInvalidated(PlayerSlotId playerSlotId)
        {
            RefreshAll();
        }

        private static ActorOccurrenceResolution Result(
            ActorOccurrenceResolutionStatus status,
            string diagnostic,
            ActorOccurrence occurrence = default) =>
            new ActorOccurrenceResolution(status, occurrence, diagnostic);
    }
}
