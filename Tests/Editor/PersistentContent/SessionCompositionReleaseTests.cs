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
    }
}
