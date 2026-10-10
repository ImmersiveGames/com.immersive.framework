using System.Collections.Generic;
using Immersive.Audio.Authoring;
using Immersive.Framework.Audio;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Immersive.Framework.Audio.Editor.Tests
{
    public sealed class FrameworkBgmDirectorCompositionTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<Object> _assets = new();

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
            for (int index = _assets.Count - 1; index >= 0; index--)
            {
                if (_assets[index] != null)
                {
                    Object.DestroyImmediate(_assets[index]);
                }
            }

            _assets.Clear();
        }

        [Test]
        public void SessionRoots_DiscoverDirectorParticipant_AndReleaseItExplicitly()
        {
            GameObject sessionRoot = Create("persistent-content");
            GameObject originalRoot = sessionRoot;
            FrameworkBgmDirector director = sessionRoot.AddComponent<FrameworkBgmDirector>();
            sessionRoot.AddComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            ActivityBgmAuthoring activity = sessionRoot.AddComponent<ActivityBgmAuthoring>();
            var lifecycle = new SceneLifecycleRuntime();

            SceneCompositionResult available = lifecycle.ComposeSessionScope(
                sessionRoot,
                new[] { sessionRoot });

            Assert.That(available.Succeeded, Is.True, available.Diagnostic);
            Assert.That(activity.Director, Is.SameAs(director));

            GameObject sceneRoot = Create("transient-route-content");
            RouteBgmAuthoring route = sceneRoot.AddComponent<RouteBgmAuthoring>();
            FrameworkBgmDirectorSceneLifecycleParticipant participant =
                sessionRoot.GetComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            SceneCompositionScope sceneScope = SceneCompositionScope.ForScene(sceneRoot.scene);
            SceneCompositionResult sceneAvailable = participant.OnSceneAvailable(
                sceneScope,
                new[] { sceneRoot });
            Assert.That(sceneAvailable.Succeeded, Is.True, sceneAvailable.Diagnostic);
            Assert.That(route.Director, Is.SameAs(director));

            SceneCompositionResult release = lifecycle.ReleaseSessionScope(
                sessionRoot,
                new[] { sessionRoot },
                "test-shutdown");

            Assert.That(release.Succeeded, Is.True, release.Diagnostic);
            Assert.That(activity.Director, Is.Null);
            Assert.That(route.Director, Is.Null);
            Assert.That(sessionRoot != null, Is.True);
            Assert.That(director != null, Is.True);
            Assert.That(sessionRoot, Is.SameAs(originalRoot));

            SceneCompositionResult repeatedRelease = lifecycle.ReleaseSessionScope(
                sessionRoot,
                new[] { sessionRoot },
                "test-shutdown-repeated");

            Assert.That(repeatedRelease.Succeeded, Is.True, repeatedRelease.Diagnostic);
            Assert.That(repeatedRelease.Diagnostic, Does.Contain("with no participants"));
            Assert.That(activity.Director, Is.Null);
            Assert.That(director != null, Is.True,
                "Session composition release detaches bindings but does not destroy Persistent Content roots.");
        }

        [Test]
        public void SceneAvailable_IsIdempotent_AndReleaseDetachesActivityAndRouteConsumers()
        {
            GameObject directorRoot = Create("director");
            FrameworkBgmDirector director = directorRoot.AddComponent<FrameworkBgmDirector>();
            directorRoot.AddComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            GameObject consumerRoot = Create("route-activity-consumers");
            ActivityBgmAuthoring activity = consumerRoot.AddComponent<ActivityBgmAuthoring>();
            RouteBgmAuthoring route = consumerRoot.AddComponent<RouteBgmAuthoring>();
            SceneCompositionScope scope = SceneCompositionScope.ForScene(consumerRoot.scene);
            var lifecycle = new SceneLifecycleRuntime();
            SceneCompositionResult session = lifecycle.ComposeSessionScope(
                directorRoot,
                new[] { directorRoot });
            Assert.That(session.Succeeded, Is.True, session.Diagnostic);

            FrameworkBgmDirectorSceneLifecycleParticipant participant =
                directorRoot.GetComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            IReadOnlyList<GameObject> roots = new[] { consumerRoot };
            SceneCompositionResult first = participant.OnSceneAvailable(scope, roots);
            SceneCompositionResult reentry = participant.OnSceneAvailable(scope, roots);

            Assert.That(first.Succeeded, Is.True, first.Diagnostic);
            Assert.That(reentry.Succeeded, Is.True, reentry.Diagnostic);
            Assert.That(activity.Director, Is.SameAs(director));
            Assert.That(route.Director, Is.SameAs(director));

            SceneCompositionResult release = participant.OnSceneReleasing(
                scope,
                roots,
                "test-scene-unload");

            Assert.That(release.Succeeded, Is.True, release.Diagnostic);
            Assert.That(activity.Director, Is.Null);
            Assert.That(route.Director, Is.Null);

            SceneCompositionResult sessionRelease = lifecycle.ReleaseSessionScope(
                directorRoot,
                new[] { directorRoot },
                "test-shutdown");
            Assert.That(sessionRelease.Succeeded, Is.True, sessionRelease.Diagnostic);
        }

        [Test]
        public void SceneAvailable_RejectsSecondDirector_AndPreservesCurrentBindings()
        {
            FrameworkBgmDirector firstDirector = CreateDirector("first-director");
            FrameworkBgmDirector secondDirector = CreateDirector("second-director");
            FrameworkBgmDirectorSceneLifecycleParticipant firstParticipant =
                firstDirector.GetComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            FrameworkBgmDirectorSceneLifecycleParticipant secondParticipant =
                secondDirector.GetComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            GameObject consumerRoot = Create("route-consumer");
            RouteBgmAuthoring route = consumerRoot.AddComponent<RouteBgmAuthoring>();
            SceneCompositionScope scope = SceneCompositionScope.ForScene(consumerRoot.scene);
            IReadOnlyList<GameObject> roots = new[] { consumerRoot };

            SceneCompositionResult first = firstParticipant.OnSceneAvailable(scope, roots);
            SceneCompositionResult rejected = InvokeWithExpectedErrorLog(
                () => secondParticipant.OnSceneAvailable(scope, roots),
                "[ERROR][Immersive.Framework][RouteBgmAuthoring] Route BGM binding rejected a different FrameworkBgmDirector authority. currentDirector='first-director' rejectedDirector='second-director'");

            Assert.That(first.Succeeded, Is.True, first.Diagnostic);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.Diagnostic, Does.Contain("rejected"));
            Assert.That(route.Director, Is.SameAs(firstDirector));
        }

        [Test]
        public void SceneAvailable_FailureRollsBackOnlyNewConsumerBindings()
        {
            FrameworkBgmDirector ownerDirector = Create("owner-director").AddComponent<FrameworkBgmDirector>();
            FrameworkBgmDirector incomingDirector = Create("incoming-director").AddComponent<FrameworkBgmDirector>();
            FrameworkBgmDirectorSceneLifecycleParticipant ownerParticipant =
                ownerDirector.gameObject.AddComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            FrameworkBgmDirectorSceneLifecycleParticipant incomingParticipant =
                incomingDirector.gameObject.AddComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            GameObject activityRoot = Create("activity-consumer");
            GameObject routeRoot = Create("route-consumer");
            ActivityBgmAuthoring newlyBound = activityRoot.AddComponent<ActivityBgmAuthoring>();
            RouteBgmAuthoring alreadyOwned = routeRoot.AddComponent<RouteBgmAuthoring>();
            SceneCompositionScope scope = SceneCompositionScope.ForScene(activityRoot.scene);

            SceneCompositionResult existingBinding = ownerParticipant.OnSceneAvailable(scope, new[] { routeRoot });
            Assert.That(existingBinding.Succeeded, Is.True, existingBinding.Diagnostic);
            SceneCompositionResult rejected = InvokeWithExpectedErrorLog(
                () => incomingParticipant.OnSceneAvailable(
                    scope,
                    new[] { activityRoot, routeRoot }),
                "[ERROR][Immersive.Framework][RouteBgmAuthoring] Route BGM binding rejected a different FrameworkBgmDirector authority. currentDirector='owner-director' rejectedDirector='incoming-director'");

            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.Diagnostic, Does.Contain("rejected"));
            Assert.That(newlyBound.Director, Is.Null);
            Assert.That(alreadyOwned.Director, Is.SameAs(ownerDirector));
        }

        private static SceneCompositionResult InvokeWithExpectedErrorLog(
            System.Func<SceneCompositionResult> operation,
            string expectedMessage)
        {
            var receivedErrors = new List<string>();
            Application.LogCallback captureError = (condition, stackTrace, type) =>
            {
                if (type == LogType.Error)
                {
                    receivedErrors.Add(condition);
                }
            };
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            Application.logMessageReceived += captureError;
            SceneCompositionResult result = default;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                result = operation();
            }
            finally
            {
                try
                {
                    LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                }
                finally
                {
                    Application.logMessageReceived -= captureError;
                }
            }

            Assert.That(receivedErrors, Has.Count.EqualTo(1),
                "The rejected authority attempt must emit exactly one Unity Error.");
            Assert.That(receivedErrors[0], Is.EqualTo(expectedMessage));
            return result;
        }

        [Test]
        public void SceneRelease_PreservesProviderConfirmedBgmPresentation()
        {
            GameObject directorRoot = Create("director");
            FrameworkBgmDirector director = directorRoot.AddComponent<FrameworkBgmDirector>();
            FrameworkBgmDirectorSceneLifecycleParticipant participant =
                directorRoot.AddComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            GameObject audioRoot = Create("audio-runtime");
            Immersive.Audio.Unity.Hosts.AudioRuntimeHost audioHost =
                audioRoot.AddComponent<Immersive.Audio.Unity.Hosts.AudioRuntimeHost>();
            AudioDefaultsAsset defaults = ScriptableObject.CreateInstance<AudioDefaultsAsset>();
            AudioBgmCueAsset cue = ScriptableObject.CreateInstance<AudioBgmCueAsset>();
            AudioClip clip = AudioClip.Create("composition-test", 1, 1, 44100, false);
            _assets.Add(defaults);
            _assets.Add(cue);
            _assets.Add(clip);

            var defaultsObject = new SerializedObject(defaults);
            defaultsObject.FindProperty("defaultFadeInSeconds").floatValue = 0f;
            defaultsObject.FindProperty("defaultFadeOutSeconds").floatValue = 0f;
            defaultsObject.ApplyModifiedPropertiesWithoutUndo();

            var cueObject = new SerializedObject(cue);
            cueObject.FindProperty("cueId").stringValue = "composition-test";
            cueObject.FindProperty("clip").objectReferenceValue = clip;
            cueObject.FindProperty("routingBus").stringValue = "BGM";
            cueObject.FindProperty("fadeInSeconds").floatValue = 0f;
            cueObject.FindProperty("fadeOutSeconds").floatValue = 0f;
            cueObject.ApplyModifiedPropertiesWithoutUndo();

            var hostObject = new SerializedObject(audioHost);
            hostObject.FindProperty("defaults").objectReferenceValue = defaults;
            hostObject.FindProperty("composeOnAwake").boolValue = false;
            hostObject.FindProperty("ensurePersistentListener").boolValue = false;
            hostObject.ApplyModifiedPropertiesWithoutUndo();
            audioHost.Compose();

            var directorObject = new SerializedObject(director);
            directorObject.FindProperty("audioRuntimeHost").objectReferenceValue = audioHost;
            directorObject.ApplyModifiedPropertiesWithoutUndo();
            FrameworkBgmOperationResult apply = director.SetRouteBgm(
                cue,
                FrameworkBgmRoutePolicy.PlayOwn);
            Assert.That(apply.Outcome, Is.EqualTo(FrameworkBgmOperationOutcome.Applied));
            Assert.That(director.ConfirmedBgm, Is.SameAs(cue));

            GameObject consumerRoot = Create("route-consumer");
            RouteBgmAuthoring route = consumerRoot.AddComponent<RouteBgmAuthoring>();
            SceneCompositionScope scope = SceneCompositionScope.ForScene(consumerRoot.scene);
            Assert.That(participant.OnSceneAvailable(scope, new[] { consumerRoot }).Succeeded, Is.True);
            Assert.That(participant.OnSceneReleasing(scope, new[] { consumerRoot }, "test-unload").Succeeded, Is.True);

            Assert.That(route.Director, Is.Null);
            Assert.That(director.ConfirmedBgm, Is.SameAs(cue));
            Assert.That(director.CurrentRouteBgm, Is.SameAs(cue));
            Assert.That(director.ConfirmedExplicitSilence, Is.False);
        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private FrameworkBgmDirector CreateDirector(string name)
        {
            GameObject gameObject = Create(name);
            FrameworkBgmDirector director = gameObject.AddComponent<FrameworkBgmDirector>();
            gameObject.AddComponent<FrameworkBgmDirectorSceneLifecycleParticipant>();
            return director;
        }

    }
}
