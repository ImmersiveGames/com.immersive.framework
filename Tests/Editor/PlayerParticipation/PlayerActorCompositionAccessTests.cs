using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerActorCompositionAccessTests
    {
        private readonly List<UnityEngine.Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) UnityEngine.Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void PlayerCompositionAccessForwardsOnlyWhileItsScopedBindingIsCurrent()
        {
            var activity = Track(ScriptableObject.CreateInstance<ActivityAsset>());
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                "activity.composition-access",
                "Composition Access",
                RuntimeDefinitionToken.FromUnityObject(activity));
            var underlying = new FakeScopedCompositionAccess(
                LocalPlayerProvisioningConsumerScope.Activity,
                owner);
            PlayerActorCompositionAccess access = Track(
                new GameObject("Player Composition Access"))
                .AddComponent<PlayerActorCompositionAccess>();
            var request = new PlayerActorCompositionContributionRequest(
                PlayerSlotId.Player1,
                new PlayerOccurrenceId("player-occurrence:access-test:1"),
                new ActorId("actor.access-test"),
                new ActorOccurrenceId(71),
                priority: 25,
                source: "AppearanceSelection",
                reason: "Replace this Player Actor's visual");

            Assert.That(access.IsScopedAccessAvailable, Is.False);
            Assert.That(access.TryOpenContribution(
                request,
                out PlayerActorCompositionContribution unavailableContribution,
                out string issue), Is.False);
            Assert.That(unavailableContribution, Is.Null);
            Assert.That(issue, Does.Contain("not bound"));

            PlayerSessionScopedBindingRecipient recipient =
                PlayerSessionScopedBindingRecipient.ForActorCompositionAccess(
                    access,
                    _ => underlying);
            IDisposable binding = recipient.CreateBinding(null);
            Assert.That(binding, Is.SameAs(underlying));
            Assert.That(recipient.TryBind(
                binding,
                LocalPlayerProvisioningConsumerScope.Activity,
                out issue), Is.True, issue);
            Assert.That(access.IsScopedAccessAvailable, Is.True);

            Assert.That(access.TryOpenContribution(
                request,
                out PlayerActorCompositionContribution contribution,
                out issue), Is.True, issue);
            Assert.That(contribution, Is.Not.Null);
            Assert.That(underlying.LastRequest, Is.SameAs(request));
            Assert.That(contribution.PlayerOccurrenceId, Is.EqualTo(request.PlayerOccurrenceId));
            Assert.That(contribution.ActorOccurrenceId, Is.EqualTo(request.ActorOccurrenceId));

            Assert.That(recipient.Release(
                "Activity scope ended.",
                true,
                out issue), Is.True, issue);
            binding.Dispose();
            Assert.That(access.IsScopedAccessAvailable, Is.False);
            Assert.That(access.TryOpenContribution(
                request,
                out PlayerActorCompositionContribution staleContribution,
                out issue), Is.False);
            Assert.That(staleContribution, Is.Null);
            Assert.That(issue, Does.Contain("Activity scope ended"));
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            _created.Add(value);
            return value;
        }

        private sealed class FakeScopedCompositionAccess :
            IPlayerSessionScopedAccess,
            IPlayerActorCompositionAccess,
            IDisposable
        {
            private readonly PlayerSessionScopedAccessSnapshot _snapshot;

            internal FakeScopedCompositionAccess(
                LocalPlayerProvisioningConsumerScope scope,
                RuntimeContentOwner owner)
            {
                _snapshot = new PlayerSessionScopedAccessSnapshot(
                    scope, owner, true, false, false, "Test scoped access is current.");
            }

            internal PlayerActorCompositionContributionRequest LastRequest { get; private set; }
            public PlayerSessionScopedAccessSnapshot Snapshot => _snapshot;
            public event Action<PlayerSessionChange> Changed;

            public bool TryOpenContribution(
                PlayerActorCompositionContributionRequest request,
                out PlayerActorCompositionContribution contribution,
                out string issue)
            {
                LastRequest = request;
                contribution = new PlayerActorCompositionContribution(
                    request,
                    request.Priority,
                    () => 0,
                    (definition, cancellationToken) => Task.FromResult<CompositionResult>(null),
                    (definitions, cancellationToken) => Task.FromResult<CompositionResult>(null),
                    part => null,
                    (key, cancellationToken) => Task.FromResult<CompositionResult>(null),
                    cancellationToken => Task.FromResult<CompositionResult>(null),
                    null);
                issue = string.Empty;
                return true;
            }

            public bool TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) =>
                throw new NotImplementedException();
            public PlayerGameplayAvailabilityBlockResult RequestBlockRuntimeGameplay(
                PlayerSlotId playerSlotId, string source, string reason) =>
                throw new NotImplementedException();
            public PlayerGameplayAvailabilityBlockResult RequestReleaseRuntimeGameplay(
                PlayerGameplayAvailabilityBlockToken blockToken, string source, string reason) =>
                throw new NotImplementedException();
            public PlayerParticipationOperationResult OpenJoining(string source, string reason) =>
                throw new NotImplementedException();
            public PlayerParticipationOperationResult CloseJoining(string source, string reason) =>
                throw new NotImplementedException();
            public SessionPlayerLeaveResult RequestLeave(SessionPlayerLeaveRequest request) =>
                throw new NotImplementedException();
            public PlayerPreparedActorReplacementResult RequestReplacePreparedActor(
                PlayerPreparedActorReplacementRequest request) =>
                throw new NotImplementedException();
            public PlayerActorSelectionResult RequestSelectActorProfile(
                PlayerActorSelectionRequest request) =>
                throw new NotImplementedException();
            public PlayerActorSelectionResult RequestSelectDefaultActor(
                PlayerSlotId playerSlotId,
                int expectedSelectionRevision,
                string source,
                string reason) =>
                throw new NotImplementedException();
            public PlayerActorSelectionResult RequestReplaceActorSelection(
                PlayerActorSelectionRequest request) =>
                throw new NotImplementedException();
            public PlayerActorSelectionResult RequestClearActorSelection(
                PlayerActorSelectionRequest request) =>
                throw new NotImplementedException();

            public void Dispose()
            {
            }
        }
    }
}
