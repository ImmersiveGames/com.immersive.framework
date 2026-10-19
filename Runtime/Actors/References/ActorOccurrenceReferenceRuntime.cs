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
            internal ActorCompositionRuntimeBinding CompositionBinding { get; set; }
        }

        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _stagedOwners = new();
        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _committedOwners = new();
        private readonly Dictionary<ActorOccurrenceId, PublicationRecord> _publications = new();
        private readonly Dictionary<ActorOccurrencePublicationScope, List<PublicationRecord>> _suspendedScopePublications = new();
        private readonly List<PublicationRecord> _pendingCompositionCleanup = new();
        private readonly List<ActorOccurrenceReferenceBinding> _consumers = new();
        private readonly ActorOccurrenceIdSequence _occurrenceIds = new();
        private IPlayerPreparedActorOccurrenceSource _playerSource;
        private RuntimeContentOwner _sessionOwner;

        internal ActorOccurrenceIdSequence OccurrenceIds => _occurrenceIds;

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
                return RemoveOwner(owner, out diagnostic);
            return TryClosePendingCompositionCleanup(owner, out diagnostic);
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
            return RemoveOwner(owner, out diagnostic);
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
            if (!TryRemoveSuspendedPublisher(publisher, out diagnostic))
                return false;
            if (!TryAddPublication(scope, publisher, actorId, owner, out token, out diagnostic)) return false;
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
            return TryCloseCompositionBinding(record, out diagnostic);
        }

        internal bool TryOpenDynamicCompositionContribution(
            ActorOccurrencePublicationScope scope,
            ActorOccurrencePublication publisher,
            ActorOccurrencePublicationToken token,
            int priority,
            string source,
            string reason,
            out ActorOccurrenceCompositionContribution contribution,
            out string diagnostic)
        {
            contribution = null;
            diagnostic = string.Empty;
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(reason))
            {
                diagnostic = "Actor occurrence composition requires a non-empty source and reason.";
                return false;
            }
            if (!TryGetExactPublication(scope, publisher, token, out PublicationRecord record, out diagnostic))
                return false;
            if (!IsCurrentPublication(record))
            {
                diagnostic = CleanupIdentity(record) + " Published Actor occurrence is no longer current.";
                return false;
            }

            return TryOpenCompositionContribution(
                record, priority, source, reason, out contribution, out diagnostic);
        }

        internal bool TryOpenStaticCompositionContribution(
            ActorOccurrencePublication publisher,
            int priority,
            string source,
            string reason,
            out ActorOccurrenceCompositionContribution contribution,
            out string diagnostic)
        {
            contribution = null;
            diagnostic = string.Empty;
            if (publisher == null || string.IsNullOrWhiteSpace(source) ||
                string.IsNullOrWhiteSpace(reason))
            {
                diagnostic = "Static Actor composition requires its exact owner-registered Actor, source, and reason.";
                return false;
            }

            PublicationRecord record = null;
            foreach (PublicationRecord candidate in _publications.Values)
            {
                if (!ReferenceEquals(candidate.Publisher, publisher)) continue;
                record = candidate;
                break;
            }

            if (record == null || record.PublicationScope != null ||
                !_committedOwners.ContainsKey(record.Owner))
            {
                diagnostic = "Static Actor composition requires this exact Actor occurrence to be registered under a committed content owner; dynamic publications require their Scope and token.";
                return false;
            }
            if (!IsCurrentPublication(record))
            {
                diagnostic = CleanupIdentity(record) + " Owner-registered Actor occurrence is no longer current.";
                return false;
            }

            return TryOpenCompositionContribution(
                record, priority, source, reason, out contribution, out diagnostic);
        }

        private bool TryOpenCompositionContribution(
            PublicationRecord record,
            int priority,
            string source,
            string reason,
            out ActorOccurrenceCompositionContribution contribution,
            out string diagnostic)
        {
            contribution = null;
            diagnostic = string.Empty;

            if (record.CompositionBinding == null)
            {
                if (!ActorCompositionRuntimeBinding.TryCreateForPublishedOccurrence(
                        record.Publisher.CompositionMount,
                        out ActorCompositionRuntimeBinding binding,
                        out diagnostic))
                {
                    diagnostic = CleanupIdentity(record) + " " + diagnostic;
                    return false;
                }
                record.CompositionBinding = binding;
            }

            return record.CompositionBinding.TryOpenContribution(
                record.ActorId,
                record.Id,
                record.Owner,
                priority,
                source,
                reason,
                () => IsCurrentPublication(record),
                out contribution,
                out diagnostic);
        }

        internal bool TryRetryCompositionCleanup(
            ActorOccurrencePublicationScope scope,
            ActorOccurrencePublication publisher,
            ActorOccurrencePublicationToken token,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (scope == null || publisher == null || !token.IsValid)
            {
                diagnostic = "Composition cleanup retry requires its exact publication scope, Actor root and token.";
                return false;
            }

            for (int index = 0; index < _pendingCompositionCleanup.Count; index++)
            {
                PublicationRecord record = _pendingCompositionCleanup[index];
                if (record.Id != token.OccurrenceId || record.Owner != token.Owner ||
                    !ReferenceEquals(record.PublicationScope, scope) ||
                    !ReferenceEquals(record.Publisher, publisher))
                {
                    continue;
                }
                return TryCloseCompositionBinding(record, out diagnostic);
            }

            diagnostic = "No pending composition cleanup matches this exact Actor occurrence, owner, publication scope and publisher token.";
            return false;
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
            for (int index = 0; index < _pendingCompositionCleanup.Count; index++)
                if (ReferenceEquals(_pendingCompositionCleanup[index].Publisher, publisher)) return true;
            return false;
        }

        internal bool HasScopePublications(ActorOccurrencePublicationScope scope)
        {
            if (scope == null) return false;
            if (_suspendedScopePublications.ContainsKey(scope)) return true;
            for (int index = 0; index < _pendingCompositionCleanup.Count; index++)
                if (ReferenceEquals(_pendingCompositionCleanup[index].PublicationScope, scope)) return true;
            return false;
        }

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
            for (int index = 0; index < records.Count; index++)
            {
                RemovePublication(records[index]);
                TryCloseCompositionBinding(records[index], out _);
            }
            TryRemoveSuspendedPublisher(publisher, out _);
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
            if (_suspendedScopePublications.TryGetValue(scope, out List<PublicationRecord> records))
            {
                _suspendedScopePublications.Remove(scope);
                for (int index = 0; index < records.Count; index++)
                    TryCloseCompositionBinding(records[index], out _);
            }
            for (int index = _pendingCompositionCleanup.Count - 1; index >= 0; index--)
            {
                PublicationRecord record = _pendingCompositionCleanup[index];
                if (ReferenceEquals(record.PublicationScope, scope))
                    TryCloseCompositionBinding(record, out _);
            }
        }

        private bool TryGetPlayer(PlayerSlotId slot, out ActorOccurrence occurrence)
        {
            occurrence = default;
            if (_playerSource == null || !_playerSource.TryGetCurrentPhysicalActorOccurrence(
                    slot, out PlayerActorPhysicalOccurrence physical))
            {
                return false;
            }

            if (!physical.IsValid)
            {
                return false;
            }

            occurrence = new ActorOccurrence(
                physical.OccurrenceId,
                physical.ActorId,
                physical.ActorRoot,
                physical.Owner,
                physical.PlayerSlotId);
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
            ActorOccurrenceId id = _occurrenceIds.Next();
            var record = new PublicationRecord(id, actorId, publisher, owner, publicationScope);
            _publications.Add(id, record);
            token = new ActorOccurrencePublicationToken(id, owner);
            return true;
        }

        private bool RemoveOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            _committedOwners.Remove(owner);
            var records = new List<PublicationRecord>();
            foreach (PublicationRecord record in _publications.Values)
                if (record.Owner == owner) records.Add(record);
            for (int index = 0; index < records.Count; index++) RemovePublication(records[index]);
            records.AddRange(RemoveSuspendedOwner(owner));
            for (int index = 0; index < _pendingCompositionCleanup.Count; index++)
            {
                PublicationRecord pending = _pendingCompositionCleanup[index];
                if (pending.Owner == owner && !records.Contains(pending))
                    records.Add(pending);
            }

            bool succeeded = true;
            var issues = new List<string>();
            for (int index = 0; index < records.Count; index++)
            {
                if (!TryCloseCompositionBinding(records[index], out string issue))
                {
                    succeeded = false;
                    issues.Add(issue);
                }
            }
            RefreshAll();
            diagnostic = string.Join(" | ", issues);
            return succeeded;
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

        private bool TryGetExactPublication(
            ActorOccurrencePublicationScope scope,
            ActorOccurrencePublication publisher,
            ActorOccurrencePublicationToken token,
            out PublicationRecord record,
            out string diagnostic)
        {
            record = null;
            if (scope == null || publisher == null || !token.IsValid ||
                !_publications.TryGetValue(token.OccurrenceId, out record) ||
                !ReferenceEquals(record.Publisher, publisher) ||
                !ReferenceEquals(record.PublicationScope, scope) || record.Owner != token.Owner ||
                !_committedOwners.ContainsKey(record.Owner))
            {
                diagnostic = "The exact published Actor occurrence, owner and scope token are required.";
                record = null;
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        private bool IsCurrentPublication(PublicationRecord record)
        {
            return record != null && _committedOwners.ContainsKey(record.Owner) &&
                _publications.TryGetValue(record.Id, out PublicationRecord current) &&
                ReferenceEquals(current, record) && record.Publisher != null &&
                record.Publisher.TryValidate(out ActorId actorId, out _) && actorId == record.ActorId &&
                (record.CompositionBinding == null ||
                 (record.Publisher.CompositionMount != null &&
                  ReferenceEquals(record.CompositionBinding.CompositionMount, record.Publisher.CompositionMount)));
        }

        private bool TryCloseCompositionBinding(PublicationRecord record, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (record == null || record.CompositionBinding == null)
            {
                if (record != null)
                {
                    _pendingCompositionCleanup.Remove(record);
                    record.Publisher?.UnbindRuntime(this);
                }
                return true;
            }

            if (record.CompositionBinding.TryClose(out string issue))
            {
                record.CompositionBinding = null;
                _pendingCompositionCleanup.Remove(record);
                record.Publisher?.UnbindRuntime(this);
                return true;
            }

            if (!_pendingCompositionCleanup.Contains(record))
                _pendingCompositionCleanup.Add(record);
            diagnostic = CleanupIdentity(record) + " Composition cleanup remains pending. " + issue;
            return false;
        }

        private bool TryClosePendingCompositionCleanup(RuntimeContentOwner owner, out string diagnostic)
        {
            var issues = new List<string>();
            for (int index = _pendingCompositionCleanup.Count - 1; index >= 0; index--)
            {
                PublicationRecord record = _pendingCompositionCleanup[index];
                if (record.Owner != owner) continue;
                if (!TryCloseCompositionBinding(record, out string issue))
                    issues.Add(issue);
            }
            diagnostic = string.Join(" | ", issues);
            return issues.Count == 0;
        }

        private static string CleanupIdentity(PublicationRecord record) =>
            record == null
                ? "Actor occurrence composition cleanup has no publication record."
                : $"actor='{record.ActorId.StableText}' occurrence='{record.Id}' owner='{record.Owner.StableText}'.";

        private bool TryRemoveSuspendedPublisher(ActorOccurrencePublication publisher, out string diagnostic)
        {
            var emptyScopes = new List<ActorOccurrencePublicationScope>();
            var records = new List<PublicationRecord>();
            foreach (KeyValuePair<ActorOccurrencePublicationScope, List<PublicationRecord>> pair in _suspendedScopePublications)
            {
                for (int index = pair.Value.Count - 1; index >= 0; index--)
                {
                    if (!ReferenceEquals(pair.Value[index].Publisher, publisher)) continue;
                    records.Add(pair.Value[index]);
                    pair.Value.RemoveAt(index);
                }
                if (pair.Value.Count == 0) emptyScopes.Add(pair.Key);
            }
            for (int index = 0; index < emptyScopes.Count; index++)
                _suspendedScopePublications.Remove(emptyScopes[index]);

            bool succeeded = true;
            var issues = new List<string>();
            for (int index = 0; index < records.Count; index++)
            {
                if (!TryCloseCompositionBinding(records[index], out string issue))
                {
                    succeeded = false;
                    issues.Add(issue);
                }
            }
            diagnostic = string.Join(" | ", issues);
            return succeeded;
        }

        private List<PublicationRecord> RemoveSuspendedOwner(RuntimeContentOwner owner)
        {
            var emptyScopes = new List<ActorOccurrencePublicationScope>();
            var records = new List<PublicationRecord>();
            foreach (KeyValuePair<ActorOccurrencePublicationScope, List<PublicationRecord>> pair in _suspendedScopePublications)
            {
                for (int index = pair.Value.Count - 1; index >= 0; index--)
                {
                    if (pair.Value[index].Owner != owner) continue;
                    records.Add(pair.Value[index]);
                    pair.Value.RemoveAt(index);
                }
                if (pair.Value.Count == 0) emptyScopes.Add(pair.Key);
            }
            for (int index = 0; index < emptyScopes.Count; index++)
                _suspendedScopePublications.Remove(emptyScopes[index]);
            return records;
        }

        private bool IsPublished(ActorOccurrencePublication publisher)
        {
            foreach (PublicationRecord record in _publications.Values)
                if (ReferenceEquals(record.Publisher, publisher)) return true;
            return false;
        }

        private ActorOccurrence CreatePublicationOccurrence(PublicationRecord record) =>
            new ActorOccurrence(record.Id, record.ActorId, record.Publisher.gameObject, record.Owner, default);

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
