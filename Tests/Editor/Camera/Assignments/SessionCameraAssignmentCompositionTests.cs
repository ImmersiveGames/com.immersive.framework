using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.Pause;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Editor.Tests
{
    public sealed class SessionCameraAssignmentCompositionTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
                if (_objects[index] != null) Object.DestroyImmediate(_objects[index]);
            _objects.Clear();
        }

        [Test]
        public void SceneAvailable_IsIdempotent_AndReleaseAllowsCompensatingAvailable()
        {
            var runtime = new FakeCameraCommands();
            var participant = new SessionCameraAssignmentCommandSceneLifecycleParticipant(runtime);
            var lifecycle = new SceneLifecycleRuntime(participant);
            GameObject root = CreateRoot("camera-additive-root");
            SessionCameraAssignmentCommandTrigger trigger = root.AddComponent<SessionCameraAssignmentCommandTrigger>();
            GameObject owner = CreateRoot("session-owner");

            Assert.That(lifecycle.ComposeSessionScope(owner, new[] { root }).Succeeded, Is.True);
            Assert.That(trigger.HasRuntimeBinding, Is.True);
            Assert.That(lifecycle.ComposeSessionScope(owner, new[] { root }).Succeeded, Is.True);
            Assert.That(lifecycle.ReleaseSessionScope(owner, new[] { root }, "session-shutdown").Succeeded, Is.True);
            Assert.That(trigger.HasRuntimeBinding, Is.False);
            Assert.That(lifecycle.ComposeSessionScope(owner, new[] { root }).Succeeded, Is.True);
            Assert.That(trigger.HasRuntimeBinding, Is.True);
        }

        [Test]
        public void AdditiveSceneRoots_BindOnAvailable_ReentryIsIdempotent_AndReleaseDetaches()
        {
            GameObject root = CreateRoot("additive-scene-root");
            SceneCompositionScope scope = SceneCompositionScope.ForScene(root.scene);
            var runtime = new FakeCameraCommands();
            var participant = new SessionCameraAssignmentCommandSceneLifecycleParticipant(runtime);
            var releaseCompensation = new FailOnceReleaseParticipant();
            SessionCameraAssignmentCommandTrigger trigger = root.AddComponent<SessionCameraAssignmentCommandTrigger>();
            GameObject[] roots = { root };

            SceneCompositionResult first = participant.OnSceneAvailable(scope, roots);
            SceneCompositionResult reentry = participant.OnSceneAvailable(scope, roots);
            Assert.That(first.Succeeded, Is.True);
            Assert.That(reentry.Succeeded, Is.True);
            Assert.That(trigger.HasRuntimeBinding, Is.True);

            SceneCompositionResult failedRelease = releaseCompensation.OnSceneReleasing(scope, roots, "test-release");
            Assert.That(failedRelease.Succeeded, Is.False);
            Assert.That(trigger.HasRuntimeBinding, Is.True);
            SceneCompositionResult compensatedReentry = participant.OnSceneAvailable(scope, roots);
            Assert.That(compensatedReentry.Succeeded, Is.True);

            SceneCompositionResult release = participant.OnSceneReleasing(scope, roots, "test-release");
            Assert.That(release.Succeeded, Is.True);
            Assert.That(trigger.HasRuntimeBinding, Is.False);
        }

        [Test]
        public void DifferentAuthority_IsRejected_AndPartialBindRollsBackNewBindings()
        {
            var first = new FakeCameraCommands();
            var second = new FakeCameraCommands();
            GameObject firstRoot = CreateRoot("first-trigger");
            GameObject secondRoot = CreateRoot("second-trigger");
            SessionCameraAssignmentCommandTrigger firstTrigger = firstRoot.AddComponent<SessionCameraAssignmentCommandTrigger>();
            SessionCameraAssignmentCommandTrigger secondTrigger = secondRoot.AddComponent<SessionCameraAssignmentCommandTrigger>();
            Assert.That(secondTrigger.TryBind(second, out _), Is.True);

            SessionCameraAssignmentCommandTriggerBindingResult result =
                SessionCameraAssignmentCommandConsumerBinder.TryBind(new[] { firstRoot, secondRoot }, first);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(firstTrigger.HasRuntimeBinding, Is.False);
            Assert.That(secondTrigger.HasRuntimeBinding, Is.True);
            Assert.That(secondTrigger.TryBind(first, out _), Is.False);
        }

        [Test]
        public void PauseRequestBinding_RemainsIdempotentAndReleasesExactAuthority()
        {
            GameObject root = CreateRoot("pause-trigger-root");
            PauseRequestTrigger trigger = root.AddComponent<PauseRequestTrigger>();
            var authority = new FakePauseAuthority();
            var participant = new PauseProductBindingSceneLifecycleParticipant(authority, authority);
            GameObject owner = CreateRoot("pause-session-owner");
            SceneCompositionScope scope = SceneCompositionScope.ForSession(owner);

            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            Assert.That(participant.OnSceneAvailable(scope, new[] { root }).Succeeded, Is.True);
            Assert.That(participant.OnSceneReleasing(scope, new[] { root }, "test-release").Succeeded, Is.True);
            Assert.That(trigger.HasPauseProductRequestBinding, Is.False);
        }

        private GameObject CreateRoot(string name)
        {
            var root = new GameObject(name);
            _objects.Add(root);
            return root;
        }

        private sealed class FakeCameraCommands : ISessionCameraAssignmentCommandPort
        {
            public bool TryActivate(Immersive.Framework.CameraAuthoring.SessionCameraAssignmentAsset candidate, out string issue)
            {
                issue = string.Empty;
                return true;
            }

            public bool TryReplace(Immersive.Framework.CameraAuthoring.SessionCameraAssignmentAsset previousAssignment,
                Immersive.Framework.CameraAuthoring.SessionCameraAssignmentAsset candidate, out string issue)
            {
                issue = string.Empty;
                return true;
            }

            public bool TryClear(Immersive.Framework.CameraAuthoring.SessionCameraAssignmentAsset assignment, out string issue)
            {
                issue = string.Empty;
                return true;
            }
        }

        private sealed class FailOnceReleaseParticipant : ISceneLifecycleParticipant
        {
            private bool _shouldRejectRelease = true;

            public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots) =>
                SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, "Test participant available.");

            public SceneCompositionResult OnSceneReleasing(SceneCompositionScope scope, IReadOnlyList<GameObject> roots, string reason)
            {
                if (_shouldRejectRelease)
                {
                    _shouldRejectRelease = false;
                    return SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, "Intentional one-time release rejection.");
                }

                return SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, "Test participant released.");
            }
        }

        private sealed class FakePauseAuthority : IPauseProductBindingPort, IPauseProductRequestPort
        {
            public bool TryRegister(PlayerPauseInput binding,
                out PauseProductBindingToken token, out string diagnostic)
            {
                token = default;
                diagnostic = "Unused by scene request trigger test.";
                return false;
            }

            public bool ReleaseBinding(PauseProductBindingToken token, string reason, out string diagnostic)
            {
                diagnostic = "Unused by scene request trigger test.";
                return false;
            }

            public PauseProductRequestResult RequestPause(PauseRequest request) => default;

            public bool TryGetPauseSnapshot(out PauseSnapshot snapshot)
            {
                snapshot = default;
                return true;
            }
        }
    }
}
