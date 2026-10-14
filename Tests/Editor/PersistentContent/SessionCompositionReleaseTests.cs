using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class SessionCompositionReleaseTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void ReleaseSessionScope_AggregatesFailuresAndAllowsRetryWithSameOwnerAndRoots()
        {
            GameObject owner = Create("session-owner");
            GameObject root = Create("persistent-content-root");
            GameObject[] roots = { root };
            var first = new RecordingParticipant("first participant", rejectFirstRelease: true);
            var second = new RecordingParticipant("second participant", rejectFirstRelease: true);
            var lifecycle = new SceneLifecycleRuntime(first, second);

            SceneCompositionResult composition = lifecycle.ComposeSessionScope(owner, roots);
            SceneCompositionResult failedRelease = lifecycle.ReleaseSessionScope(
                owner,
                roots,
                "session-shutdown");

            Assert.That(composition.Succeeded, Is.True, composition.Diagnostic);
            Assert.That(composition.Scope, Is.EqualTo(SceneCompositionScope.ForSession(owner)));
            Assert.That(failedRelease.Succeeded, Is.False);
            Assert.That(failedRelease.Operation, Is.EqualTo(SceneCompositionOperation.Releasing));
            Assert.That(failedRelease.Status, Is.EqualTo("Rejected"));
            Assert.That(failedRelease.Diagnostic, Does.Contain("first participant rejected release"));
            Assert.That(failedRelease.Diagnostic, Does.Contain("second participant rejected release"));
            AssertReleaseEvidence(first, owner, roots, "session-shutdown", 1);
            AssertReleaseEvidence(second, owner, roots, "session-shutdown", 1);
            Assert.That(root != null, Is.True, "A failed composition release does not destroy the authored root.");

            SceneCompositionResult retry = lifecycle.ReleaseSessionScope(
                owner,
                roots,
                "session-shutdown-retry");

            Assert.That(retry.Succeeded, Is.True, retry.Diagnostic);
            AssertReleaseEvidence(first, owner, roots, "session-shutdown-retry", 2);
            AssertReleaseEvidence(second, owner, roots, "session-shutdown-retry", 2);
            Assert.That(root != null, Is.True, "Composition release and physical root destruction are separate operations.");
        }

        [Test]
        public void ComposeSessionScope_FailureRollsBackAndRetainsEveryAttemptedParticipantForRetry()
        {
            GameObject owner = Create("session-owner");
            GameObject foreignOwner = Create("foreign-session-owner");
            GameObject root = Create("persistent-content-root");
            GameObject[] roots = { root };
            var events = new List<string>();
            RecordingSessionLifecycleParticipant first = root.AddComponent<RecordingSessionLifecycleParticipant>();
            first.Configure("first participant", events, rejectAvailable: false, releaseRejections: 0);
            RecordingSessionLifecycleParticipant failing = root.AddComponent<RecordingSessionLifecycleParticipant>();
            failing.Configure("failing participant", events, rejectAvailable: true, releaseRejections: 1);
            RecordingSessionLifecycleParticipant notReached = root.AddComponent<RecordingSessionLifecycleParticipant>();
            notReached.Configure("not reached participant", events, rejectAvailable: false, releaseRejections: 0);
            var lifecycle = new SceneLifecycleRuntime();

            SceneCompositionResult composition = lifecycle.ComposeSessionScope(owner, roots);

            Assert.That(composition.Succeeded, Is.False);
            Assert.That(composition.Diagnostic, Does.Contain("failing participant rejected composition."));
            Assert.That(composition.Diagnostic, Does.Contain("failing participant rejected composition-bind-rollback."));
            Assert.That(composition.Diagnostic, Does.Not.Contain("first participant rejected composition-bind-rollback."));
            Assert.That(first.AvailableCount, Is.EqualTo(1));
            Assert.That(failing.AvailableCount, Is.EqualTo(1));
            Assert.That(notReached.AvailableCount, Is.Zero);
            Assert.That(first.ReleaseCount, Is.EqualTo(1));
            Assert.That(failing.ReleaseCount, Is.EqualTo(1), "The participant that rejects Available may already own Session resources.");
            Assert.That(notReached.ReleaseCount, Is.Zero);
            Assert.That(events, Is.EqualTo(new[]
            {
                "available:first participant",
                "available:failing participant",
                "release:failing participant:composition-bind-rollback",
                "release:first participant:composition-bind-rollback"
            }));

            SceneCompositionResult foreignRelease = lifecycle.ReleaseSessionScope(
                foreignOwner,
                roots,
                "foreign-owner-release");

            Assert.That(foreignRelease.Succeeded, Is.False);
            Assert.That(foreignRelease.Diagnostic, Does.Contain("exact live Session owner"));
            Assert.That(first.ReleaseCount, Is.EqualTo(1));
            Assert.That(failing.ReleaseCount, Is.EqualTo(1));

            SceneCompositionResult retry = lifecycle.ReleaseSessionScope(
                owner,
                roots,
                "session-release-retry");

            Assert.That(retry.Succeeded, Is.True, retry.Diagnostic);
            Assert.That(first.ReleaseCount, Is.EqualTo(2));
            Assert.That(failing.ReleaseCount, Is.EqualTo(2));
            Assert.That(notReached.ReleaseCount, Is.Zero);
            Assert.That(first.LastReleaseScope, Is.EqualTo(SceneCompositionScope.ForSession(owner)));
            Assert.That(failing.LastReleaseScope, Is.EqualTo(SceneCompositionScope.ForSession(owner)));
            Assert.That(first.LastReleaseRoots, Is.SameAs(roots));
            Assert.That(failing.LastReleaseRoots, Is.SameAs(roots));
            Assert.That(events, Is.EqualTo(new[]
            {
                "available:first participant",
                "available:failing participant",
                "release:failing participant:composition-bind-rollback",
                "release:first participant:composition-bind-rollback",
                "release:failing participant:session-release-retry",
                "release:first participant:session-release-retry"
            }));

            SceneCompositionResult duplicateRelease = lifecycle.ReleaseSessionScope(
                owner,
                roots,
                "duplicate-session-release");

            Assert.That(duplicateRelease.Succeeded, Is.True, duplicateRelease.Diagnostic);
            Assert.That(first.ReleaseCount, Is.EqualTo(2));
            Assert.That(failing.ReleaseCount, Is.EqualTo(2));
        }

        [Test]
        public void ComposeSessionScope_FailureWithCompleteRollbackRetainsLedgerUntilAggregateRelease()
        {
            GameObject owner = Create("session-owner");
            GameObject root = Create("persistent-content-root");
            GameObject[] roots = { root };
            var events = new List<string>();
            RecordingSessionLifecycleParticipant first = root.AddComponent<RecordingSessionLifecycleParticipant>();
            first.Configure("first participant", events, rejectAvailable: false, releaseRejections: 0);
            RecordingSessionLifecycleParticipant failing = root.AddComponent<RecordingSessionLifecycleParticipant>();
            failing.Configure("failing participant", events, rejectAvailable: true, releaseRejections: 0);
            var lifecycle = new SceneLifecycleRuntime();

            SceneCompositionResult composition = lifecycle.ComposeSessionScope(owner, roots);

            Assert.That(composition.Succeeded, Is.False);
            Assert.That(composition.Diagnostic, Does.Contain("failing participant rejected composition."));
            Assert.That(composition.Diagnostic, Does.Contain("Rollback='Succeeded'."));
            Assert.That(first.ReleaseCount, Is.EqualTo(1));
            Assert.That(failing.ReleaseCount, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(new[]
            {
                "available:first participant",
                "available:failing participant",
                "release:failing participant:composition-bind-rollback",
                "release:first participant:composition-bind-rollback"
            }));

            SceneCompositionResult sessionRelease = lifecycle.ReleaseSessionScope(
                owner,
                roots,
                "persistent-content-composition-rollback");
            SceneCompositionResult duplicateRelease = lifecycle.ReleaseSessionScope(
                owner,
                roots,
                "duplicate-session-release");

            Assert.That(sessionRelease.Succeeded, Is.True, sessionRelease.Diagnostic);
            Assert.That(duplicateRelease.Succeeded, Is.True, duplicateRelease.Diagnostic);
            Assert.That(first.ReleaseCount, Is.EqualTo(2));
            Assert.That(failing.ReleaseCount, Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(new[]
            {
                "available:first participant",
                "available:failing participant",
                "release:failing participant:composition-bind-rollback",
                "release:first participant:composition-bind-rollback",
                "release:failing participant:persistent-content-composition-rollback",
                "release:first participant:persistent-content-composition-rollback"
            }));
        }

        private GameObject Create(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private static void AssertReleaseEvidence(
            RecordingParticipant participant,
            GameObject owner,
            IReadOnlyList<GameObject> roots,
            string reason,
            int expectedReleaseCount)
        {
            Assert.That(participant.ReleaseCount, Is.EqualTo(expectedReleaseCount));
            Assert.That(participant.LastReleaseScope, Is.EqualTo(SceneCompositionScope.ForSession(owner)));
            Assert.That(participant.LastReleaseRoots, Is.SameAs(roots));
            Assert.That(participant.LastReleaseReason, Is.EqualTo(reason));
        }

        private sealed class RecordingParticipant : ISceneLifecycleParticipant
        {
            private readonly string _name;
            private bool _rejectNextRelease;

            internal RecordingParticipant(string name, bool rejectFirstRelease)
            {
                _name = name;
                _rejectNextRelease = rejectFirstRelease;
            }

            internal int ReleaseCount { get; private set; }

            internal SceneCompositionScope LastReleaseScope { get; private set; }

            internal IReadOnlyList<GameObject> LastReleaseRoots { get; private set; }

            internal string LastReleaseReason { get; private set; }

            public SceneCompositionResult OnSceneAvailable(
                SceneCompositionScope scope,
                IReadOnlyList<GameObject> roots) =>
                SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Available,
                    $"{_name} composed.");

            public SceneCompositionResult OnSceneReleasing(
                SceneCompositionScope scope,
                IReadOnlyList<GameObject> roots,
                string reason)
            {
                ReleaseCount++;
                LastReleaseScope = scope;
                LastReleaseRoots = roots;
                LastReleaseReason = reason;
                if (_rejectNextRelease)
                {
                    _rejectNextRelease = false;
                    return SceneCompositionResult.Rejected(
                        scope,
                        SceneCompositionOperation.Releasing,
                        $"{_name} rejected release.");
                }

                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Releasing,
                    $"{_name} released.");
            }
        }

        internal sealed class RecordingSessionLifecycleParticipant : MonoBehaviour, ISceneLifecycleParticipant
        {
            private List<string> _events;
            private bool _rejectAvailable;
            private int _remainingReleaseRejections;

            internal int AvailableCount { get; private set; }

            internal int ReleaseCount { get; private set; }

            internal SceneCompositionScope LastReleaseScope { get; private set; }

            internal IReadOnlyList<GameObject> LastReleaseRoots { get; private set; }

            internal void Configure(
                string participantName,
                List<string> events,
                bool rejectAvailable,
                int releaseRejections)
            {
                ParticipantName = participantName;
                _events = events;
                _rejectAvailable = rejectAvailable;
                _remainingReleaseRejections = releaseRejections;
            }

            private string ParticipantName { get; set; }

            public SceneCompositionResult OnSceneAvailable(
                SceneCompositionScope scope,
                IReadOnlyList<GameObject> roots)
            {
                AvailableCount++;
                _events.Add($"available:{ParticipantName}");
                return _rejectAvailable
                    ? SceneCompositionResult.Rejected(
                        scope,
                        SceneCompositionOperation.Available,
                        $"{ParticipantName} rejected composition.")
                    : SceneCompositionResult.Completed(
                        scope,
                        SceneCompositionOperation.Available,
                        $"{ParticipantName} composed.");
            }

            public SceneCompositionResult OnSceneReleasing(
                SceneCompositionScope scope,
                IReadOnlyList<GameObject> roots,
                string reason)
            {
                ReleaseCount++;
                LastReleaseScope = scope;
                LastReleaseRoots = roots;
                _events.Add($"release:{ParticipantName}:{reason}");
                if (_remainingReleaseRejections > 0)
                {
                    _remainingReleaseRejections--;
                    return SceneCompositionResult.Rejected(
                        scope,
                        SceneCompositionOperation.Releasing,
                        $"{ParticipantName} rejected {reason}.");
                }

                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Releasing,
                    $"{ParticipantName} released.");
            }
        }
    }
}
