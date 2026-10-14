using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Performance;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PersistentContent.Tests;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Transition;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class FrameworkRuntimeHostControlledStopTests
    {
        private const string AssetFolder = "Assets/__IF_FrameworkRuntimeHostControlledStopTests";
        private const string ApplicationAssetPath = AssetFolder + "/GameApplication.asset";
        private const string PersistentContentScenePath = AssetFolder + "/PersistentContent.unity";
        private const string SetupMarkerKey =
            "Immersive.Framework.PersistentContent.HostControlledStop.SetupPrepared";
        private const int SessionParticipantCount = 3;

        private readonly List<string> _scenePaths = new();
        private readonly List<UnsavedSceneBackup> _unsavedSceneBackups = new();
        private readonly List<InputDevice> _temporaryInputDevices = new();
        private EditorBuildSettingsScene[] _originalBuildScenes;
        private Scene _originalActiveScene;
        private string _assetFolder;
        private FrameworkRuntimeHost _host;
        private int _originalPlayModeStartup;
        private float _originalTimeScale;
        private bool _changedPlayModeStartup;
        private bool _originalEnterPlayModeOptionsEnabled;
        private EnterPlayModeOptions _originalEnterPlayModeOptions;
        private bool _usesTwoPlayerCameraStopFixture;
        private ActivityAsset _postCommitReadinessActivity;
        private RouteAsset _postCommitReadinessRoute;
        private string _postCommitReadinessContentScenePath;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (SessionState.GetBool(SetupMarkerKey, false))
            {
                return;
            }

            HostSessionLifecycleTestProbe.ClearRecords();
            _usesTwoPlayerCameraStopFixture = TestContext.CurrentContext.Test.Name.Contains(
                "TwoJoinedPlayersCameraAndPause",
                StringComparison.Ordinal);
            _originalEnterPlayModeOptionsEnabled =
                EditorSettings.enterPlayModeOptionsEnabled;
            _originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload;
            _originalBuildScenes = EditorBuildSettings.scenes;
            _originalActiveScene = SceneManager.GetActiveScene();
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>("ImmersiveFrameworkSettings");
            if (settings != null)
            {
                var serializedSettings = new SerializedObject(settings);
                SerializedProperty playModeStartup =
                    serializedSettings.FindProperty("editorPlayModeStartup");
                _originalPlayModeStartup = playModeStartup.enumValueIndex;
                playModeStartup.enumValueIndex =
                    (int)FrameworkEditorPlayModeStartup.CurrentSceneOnly;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                _changedPlayModeStartup = true;
            }

            if (AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.DeleteAsset(AssetFolder);
            }

            _assetFolder = AssetFolder;
            AssetDatabase.CreateFolder("Assets", "__IF_FrameworkRuntimeHostControlledStopTests");

            int backupIndex = 0;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.IsValid() || !scene.isLoaded || !string.IsNullOrWhiteSpace(scene.path))
                {
                    continue;
                }

                string backupPath = _assetFolder + "/__EditorSceneBackup_" + backupIndex++ + ".unity";
                if (!EditorSceneManager.SaveScene(scene, backupPath))
                {
                    Assert.Fail("Could not snapshot unsaved Editor scene '" + scene.name + "' for Host fixture setup.");
                }

                _unsavedSceneBackups.Add(new UnsavedSceneBackup(
                    backupPath,
                    scene.name,
                    SceneManager.GetActiveScene() == scene));
            }

            AssetDatabase.Refresh();
            bool startupFailureCase = TestContext.CurrentContext.Test.Name.Contains(
                "StartupFailureAfterPersistentContentCommit",
                StringComparison.Ordinal);
            CreateApplicationFixture(
                startupFailureCase ? string.Empty : "host-stop.route",
                _usesTwoPlayerCameraStopFixture);
            if (TestContext.CurrentContext.Test.Name.Contains(
                    "PostCommitReadinessFailure",
                    StringComparison.Ordinal))
            {
                CreatePostCommitRouteReadinessFixture();
            }
            SessionState.SetBool(SetupMarkerKey, true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
            {
                if (_host != null)
                {
                    Task<FrameworkRuntimeHostStopResult> stopTask =
                        _host.StopAsync("test-fixture-cleanup");
                    while (!stopTask.IsCompleted)
                    {
                        yield return null;
                    }

                    FrameworkRuntimeHostStopResult stop = stopTask.GetAwaiter().GetResult();
                    if (!stop.Succeeded)
                    {
                        stopTask = _host.StopAsync("test-fixture-cleanup-retry");
                        while (!stopTask.IsCompleted)
                        {
                            yield return null;
                        }

                        stop = stopTask.GetAwaiter().GetResult();
                    }

                    if (stop.Succeeded && _host != null)
                    {
                        UnityEngine.Object.Destroy(_host.gameObject);
                        _host = null;
                    }
                }

                RemoveTemporaryInputDevices();

                foreach (GameObject root in HostSessionLifecycleTestProbe.CreatedRoots)
                {
                    if (root != null)
                    {
                        UnityEngine.Object.Destroy(root);
                    }
                }

                yield return null;
                yield return new ExitPlayMode();
            }

            foreach (GameObject root in HostSessionLifecycleTestProbe.CreatedRoots)
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            for (int index = _scenePaths.Count - 1; index >= 0; index--)
            {
                Scene scene = SceneManager.GetSceneByPath(_scenePaths[index]);
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }

            EditorBuildSettings.scenes = _originalBuildScenes;

            for (int index = 0; index < _unsavedSceneBackups.Count; index++)
            {
                UnsavedSceneBackup backup = _unsavedSceneBackups[index];
                Scene snapshot = SceneManager.GetSceneByPath(backup.Path);
                if (!snapshot.IsValid() || !snapshot.isLoaded)
                {
                    continue;
                }

                Scene restored = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                GameObject[] roots = snapshot.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    SceneManager.MoveGameObjectToScene(roots[rootIndex], restored);
                }

                EditorSceneManager.SaveScene(snapshot);
                EditorSceneManager.CloseScene(snapshot, removeScene: true);
                if (backup.WasActive)
                {
                    SceneManager.SetActiveScene(restored);
                }
            }

            if (_originalActiveScene.IsValid() && _originalActiveScene.isLoaded)
            {
                bool originallyUnsaved = false;
                for (int index = 0; index < _unsavedSceneBackups.Count; index++)
                {
                    if (_unsavedSceneBackups[index].WasActive)
                    {
                        originallyUnsaved = true;
                        break;
                    }
                }

                if (!originallyUnsaved)
                {
                    SceneManager.SetActiveScene(_originalActiveScene);
                }
            }
            if (!string.IsNullOrWhiteSpace(_assetFolder))
            {
                AssetDatabase.DeleteAsset(_assetFolder);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RestorePlayModeStartupSetting();
            Time.timeScale = _originalTimeScale;
            EditorSettings.enterPlayModeOptionsEnabled =
                _originalEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = _originalEnterPlayModeOptions;
            SessionState.EraseBool(SetupMarkerKey);
            HostSessionLifecycleTestProbe.ClearRecords();
        }

        [UnityTest]
        public IEnumerator FullStartupAndControlledStop_RetainExactRootsAndSameHostForReleaseRetry()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The controlled-stop integration requires the Editor to enter Play Mode.");
            HostSessionLifecycleTestProbe.ClearRecords();
            _host = CreateHost(LoadApplication(ApplicationAssetPath));

            PauseResult beforeStart = _host.RequestPause(
                PauseRequestKind.Pause,
                "host-stop-test",
                "before-start");

            Assert.That(beforeStart.Rejected, Is.True);
            Assert.That(beforeStart.Message, Does.Contain("Created"));

            Task<FrameworkGameFlowStartResult> startupTask = _host.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();

            Assert.That(startup.Started, Is.True, startup.Message);
            Assert.That(_host.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Operational));
            Assert.That(CallsForScopeKind(SceneCompositionOperation.Available, SceneCompositionScopeKind.Session), Has.Count.EqualTo(SessionParticipantCount));
            Assert.That(CallsForScopeKind(SceneCompositionOperation.Available, SceneCompositionScopeKind.Scene), Has.Count.EqualTo(SessionParticipantCount));
            Scene primaryScene = SceneManager.GetSceneByPath(_scenePaths[0]);
            Assert.That(primaryScene.IsValid() && primaryScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(primaryScene.handle));

            const float runningTimeScale = 0.625f;
            Time.timeScale = runningTimeScale;
            PauseResult paused = _host.RequestPause(
                PauseRequestKind.Pause,
                "host-stop-test",
                "controlled-stop-pause-restoration");
            Assert.That(paused.Completed, Is.True, paused.ToDiagnosticString());
            Assert.That(_host.PauseState, Is.EqualTo(Immersive.Framework.Pause.PauseState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.0001f));

            HostSessionLifecycleTestProbe.RejectRelease(
                "second",
                SceneCompositionScopeKind.Session,
                rejectionCount: 1);
            FrameworkRuntimeHost owner = _host;
            IReadOnlyList<GameObject> roots =
                CallsForScopeKind(SceneCompositionOperation.Available, SceneCompositionScopeKind.Session)[0].Roots;
            GameObject exactRoot = roots[0];

            LogAssert.Expect(
                LogType.Error,
                new Regex("FrameworkRuntimeHost controlled stop retained the live Host for retry\\."));
            Task<FrameworkRuntimeHostStopResult> rejectedStopTask =
                owner.StopAsync("controlled-stop-first-attempt");
            while (!rejectedStopTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult rejectedStop = rejectedStopTask.GetAwaiter().GetResult();

            Assert.That(rejectedStop.Succeeded, Is.False);
            Assert.That(owner.PauseState, Is.EqualTo(Immersive.Framework.Pause.PauseState.Running),
                "Controlled Activity exit must restore canonical Pause before later Session cleanup can reject.");
            Assert.That(Time.timeScale, Is.EqualTo(runningTimeScale).Within(0.0001f),
                "Controlled Activity exit must restore the exact captured Time.timeScale before returning a retryable stop failure.");
            Assert.That(owner.State.CurrentRoute, Is.Null,
                "A retryable Persistent Content release must not retain Framework Route authority after Route exit completed.");
            Assert.That(primaryScene.IsValid() && primaryScene.isLoaded, Is.True,
                "Controlled Route exit must preserve its Primary Scene as a loaded Unity scene.");
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(primaryScene.handle),
                "Controlled Route exit must preserve the Primary Scene as the active Unity scene.");
            Assert.That(owner != null, Is.True, "A failed aggregate release must retain its Host owner.");
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopping));
            Assert.That(exactRoot != null, Is.True, "The composed root remains alive while its release receipt is unresolved.");
            Assert.That(CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Session), Has.Count.EqualTo(SessionParticipantCount));
            IReadOnlyList<HostSessionLifecycleTestProbe.Call> routeSceneReleases =
                CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Scene);
            Assert.That(routeSceneReleases, Has.Count.EqualTo(SessionParticipantCount));
            Assert.That(routeSceneReleases[0].Roots[0].scene.handle, Is.EqualTo(primaryScene.handle));
            IReadOnlyList<HostSessionLifecycleTestProbe.Call> sessionReleases =
                CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Session);
            Assert.That(sessionReleases[0].Roots, Is.SameAs(roots));

            PauseResult whileStopping = owner.RequestPause(
                PauseRequestKind.Pause,
                "host-stop-test",
                "during-stop");
            Assert.That(whileStopping.Rejected, Is.True);
            Assert.That(whileStopping.Message, Does.Contain("Stopping"));

            Task<FrameworkRuntimeHostStopResult> retryTask =
                owner.StopAsync("controlled-stop-retry");
            while (!retryTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult retry = retryTask.GetAwaiter().GetResult();

            Assert.That(retry.Succeeded, Is.True, retry.Diagnostic);
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
            Assert.That(primaryScene.IsValid() && primaryScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(primaryScene.handle));
            Assert.That(owner != null, Is.True, "Controlled stop completes before the caller destroys the Host.");
            Assert.That(exactRoot != null, Is.True);
            AssertReleaseCallsUseSameOwnerAndRoots(owner, roots);

            int releaseCount = HostSessionLifecycleTestProbe.CallsFor(SceneCompositionOperation.Releasing).Count;
            Task<FrameworkRuntimeHostStopResult> repeatedTask =
                owner.StopAsync("controlled-stop-repeat");
            while (!repeatedTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult repeated = repeatedTask.GetAwaiter().GetResult();
            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(HostSessionLifecycleTestProbe.CallsFor(SceneCompositionOperation.Releasing), Has.Count.EqualTo(releaseCount));

            UnityEngine.Object.Destroy(owner.gameObject);
            foreach (GameObject root in HostSessionLifecycleTestProbe.CreatedRoots)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            yield return null;
            _host = null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator PostCommitReadinessFailure_RouteStartupActivityRetainsCommittedRouteAndRestoresBaseline()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The post-commit readiness integration requires Play Mode.");

            FrameworkRuntimeHost owner = CreateHost(LoadApplication(ApplicationAssetPath));
            _host = owner;
            Task<FrameworkGameFlowStartResult> startupTask = owner.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();
            Assert.That(startup.Started, Is.True, startup.Message);

            RouteAsset baselineRoute = owner.State.CurrentRoute;
            ActivityAsset baselineActivity = owner.State.CurrentActivity;
            Assert.That(baselineRoute, Is.Not.Null);
            Assert.That(baselineActivity, Is.Not.Null,
                "The fixture must start with the Route's authored Activity as its baseline.");
            Assert.That(_postCommitReadinessActivity, Is.Not.Null);
            Assert.That(_postCommitReadinessRoute, Is.Not.Null);
            AssertPostCommitReadinessActivityIdentity("before Route request");

            Scene activityContentScene = default;
            ActivityReadinessParticipant readinessParticipant = null;
            int readinessReleasedCount = 0;
            UnityAction<Scene, LoadSceneMode> captureReadinessParticipant = (scene, _) =>
            {
                if (!string.Equals(
                        scene.path,
                        _postCommitReadinessContentScenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                activityContentScene = scene;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int index = 0; index < roots.Length; index++)
                {
                    ActivityReadinessParticipant[] participants =
                        roots[index].GetComponentsInChildren<ActivityReadinessParticipant>(true);
                    if (participants.Length == 1)
                    {
                        readinessParticipant = participants[0];
                        readinessParticipant.PreparationReleased.AddListener(
                            () => readinessReleasedCount++);
                    }
                }
            };
            SceneManager.sceneLoaded += captureReadinessParticipant;

            Task<FrameworkRouteRequestResult> failedRequestTask =
                owner.RequestRouteAsync(
                    _postCommitReadinessRoute,
                    "i05-post-commit-test",
                    "fail-startup-activity-readiness-after-route-commit");
            bool failureIssued = false;
            double readinessDeadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (!failedRequestTask.IsCompleted &&
                   Time.realtimeSinceStartupAsDouble < readinessDeadline)
            {
                if (!failureIssued && readinessParticipant != null &&
                    readinessParticipant.State == ActivityReadinessParticipantState.Preparing)
                {
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex("Route Request committed the target Route but Startup Activity entry readiness did not complete\\."));
                    failureIssued = true;
                    readinessParticipant.FailPreparation(
                        "qa-post-commit-required-readiness-failure");
                }

                yield return null;
            }
            SceneManager.sceneLoaded -= captureReadinessParticipant;

            Assert.That(failureIssued, Is.True,
                "The real Activity content scene did not expose its Preparing occurrence before the request deadline. " +
                "Scene valid='" + activityContentScene.IsValid() + "' loaded='" + activityContentScene.isLoaded +
                "' participant='" + (readinessParticipant != null ? readinessParticipant.ParticipantId : "<none>") +
                "' participantState='" + (readinessParticipant != null ? readinessParticipant.State.ToString() : "<none>") +
                "' requestStatus='" + failedRequestTask.Status + "'.");
            Assert.That(failedRequestTask.IsCompleted, Is.True,
                "The readiness failure did not settle the asynchronous Activity request.");
            FrameworkRouteRequestResult failedRequest =
                failedRequestTask.GetAwaiter().GetResult();

            Assert.That(failedRequest.Succeeded, Is.False, failedRequest.Message);
            Assert.That(
                failedRequest.Kind,
                Is.EqualTo(FrameworkRouteRequestKind.FailedCommittedTargetNotReady),
                failedRequest.Message);
            Assert.That(failedRequest.DestinationAuthoritative, Is.True,
                "Post-commit readiness failure must report the committed Route as authoritative.");
            Assert.That(failedRequest.TargetRoute, Is.SameAs(_postCommitReadinessRoute));
            Assert.That(owner.State.CurrentRoute, Is.SameAs(_postCommitReadinessRoute));
            Assert.That(owner.State.CurrentActivity, Is.SameAs(_postCommitReadinessActivity));
            Assert.That(owner.State.GameFlowStarted, Is.True);
            Assert.That(readinessParticipant, Is.Not.Null);
            Assert.That(readinessParticipant.State,
                Is.EqualTo(ActivityReadinessParticipantState.Failed));
            Assert.That(activityContentScene.IsValid() && activityContentScene.isLoaded, Is.True,
                "The committed Activity content owner must remain available until a subsequent owner transition releases it.");
            Scene committedRouteScene = SceneManager.GetSceneByPath(
                _postCommitReadinessRoute.PrimaryScenePath);
            Assert.That(committedRouteScene.IsValid() && committedRouteScene.isLoaded, Is.True,
                "The committed Route Primary Scene must remain loaded while its Startup Activity readiness failed.");
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(committedRouteScene.handle));

            Task<FrameworkRouteRequestResult> restoreTask = owner.RequestRouteAsync(
                baselineRoute,
                "i05-post-commit-test",
                "restore-original-route-after-committed-startup-readiness-failure");
            while (!restoreTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRouteRequestResult restored = restoreTask.GetAwaiter().GetResult();
            Assert.That(restored.Succeeded, Is.True, restored.Message);
            Assert.That(owner.State.CurrentRoute, Is.SameAs(baselineRoute));
            Assert.That(owner.State.CurrentActivity, Is.SameAs(baselineActivity));
            Assert.That(SceneManager.GetActiveScene().path,
                Is.EqualTo(baselineRoute.PrimaryScenePath));
            Assert.That(readinessReleasedCount, Is.EqualTo(1),
                "The failed committed Activity occurrence must release its exact readiness participant once during recovery.");
            Assert.That(
                SceneManager.GetSceneByPath(_postCommitReadinessContentScenePath).isLoaded,
                Is.False,
                "The failed Activity's content scene must be released when the baseline Activity regains authority.");
            Assert.That(
                SceneManager.GetSceneByPath(_postCommitReadinessRoute.PrimaryScenePath).isLoaded,
                Is.False,
                "The failed Route Primary Scene must be released after the baseline Route regains authority.");
        }

        [UnityTest]
        public IEnumerator PostCommitReadinessFailure_DirectActivityRetainsCommittedOwnerAndRestoresBaseline()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The direct Activity readiness integration requires Play Mode.");

            FrameworkRuntimeHost owner = CreateHost(LoadApplication(ApplicationAssetPath));
            _host = owner;
            Task<FrameworkGameFlowStartResult> startupTask = owner.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();
            Assert.That(startup.Started, Is.True, startup.Message);

            RouteAsset baselineRoute = owner.State.CurrentRoute;
            ActivityAsset baselineActivity = owner.State.CurrentActivity;
            Assert.That(baselineRoute, Is.Not.Null);
            Assert.That(baselineActivity, Is.Not.Null,
                "The fixture must start with the Route's authored Activity as its baseline.");
            Assert.That(_postCommitReadinessActivity, Is.Not.Null);
            AssertPostCommitReadinessActivityIdentity("before direct Activity request");

            Scene activityContentScene = default;
            ActivityReadinessParticipant readinessParticipant = null;
            int readinessReleasedCount = 0;
            UnityAction<Scene, LoadSceneMode> captureReadinessParticipant = (scene, _) =>
            {
                if (!string.Equals(
                        scene.path,
                        _postCommitReadinessContentScenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                activityContentScene = scene;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int index = 0; index < roots.Length; index++)
                {
                    ActivityReadinessParticipant[] participants =
                        roots[index].GetComponentsInChildren<ActivityReadinessParticipant>(true);
                    if (participants.Length == 1)
                    {
                        readinessParticipant = participants[0];
                        readinessParticipant.PreparationReleased.AddListener(
                            () => readinessReleasedCount++);
                    }
                }
            };
            SceneManager.sceneLoaded += captureReadinessParticipant;

            Task<FrameworkActivityRequestResult> failedActivityTask = owner.RequestActivityAsync(
                _postCommitReadinessActivity,
                "i05-post-commit-test",
                "fail-direct-activity-readiness-after-activity-commit");
            bool activityFailureIssued = false;
            double activityReadinessDeadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (!failedActivityTask.IsCompleted &&
                   Time.realtimeSinceStartupAsDouble < activityReadinessDeadline)
            {
                if (!activityFailureIssued && readinessParticipant != null &&
                    readinessParticipant.State == ActivityReadinessParticipantState.Preparing)
                {
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex("Activity Request committed the target Activity but entry readiness did not complete\\."));
                    activityFailureIssued = true;
                    readinessParticipant.FailPreparation(
                        "qa-post-commit-direct-activity-readiness-failure");
                }

                yield return null;
            }
            SceneManager.sceneLoaded -= captureReadinessParticipant;

            Assert.That(activityFailureIssued, Is.True,
                "The direct Activity request did not expose its Preparing occurrence before the request deadline. " +
                "Scene valid='" + activityContentScene.IsValid() + "' loaded='" + activityContentScene.isLoaded +
                "' participant='" + (readinessParticipant != null ? readinessParticipant.ParticipantId : "<none>") +
                "' participantState='" + (readinessParticipant != null ? readinessParticipant.State.ToString() : "<none>") +
                "' requestStatus='" + failedActivityTask.Status + "'.");
            Assert.That(failedActivityTask.IsCompleted, Is.True,
                "The direct Activity readiness failure did not settle the asynchronous request.");
            FrameworkActivityRequestResult failedActivity =
                failedActivityTask.GetAwaiter().GetResult();
            Assert.That(failedActivity.Succeeded, Is.False, failedActivity.Message);
            Assert.That(
                failedActivity.Kind,
                Is.EqualTo(FrameworkActivityRequestKind.FailedCommittedTargetNotReady),
                failedActivity.Message);
            Assert.That(failedActivity.DestinationAuthoritative, Is.True,
                "Post-commit readiness failure must report the committed Activity as authoritative.");
            Assert.That(failedActivity.TargetActivity, Is.SameAs(_postCommitReadinessActivity));
            Assert.That(owner.State.CurrentRoute, Is.SameAs(baselineRoute),
                "A direct Activity request must retain its exact Route owner.");
            Assert.That(owner.State.CurrentActivity, Is.SameAs(_postCommitReadinessActivity));
            Assert.That(owner.State.GameFlowStarted, Is.True);
            Assert.That(readinessParticipant, Is.Not.Null);
            Assert.That(readinessParticipant.State,
                Is.EqualTo(ActivityReadinessParticipantState.Failed));
            Assert.That(activityContentScene.IsValid() && activityContentScene.isLoaded, Is.True,
                "The committed Activity content owner must remain loaded until a later Activity transition releases it.");
            Assert.That(SceneManager.GetActiveScene().path,
                Is.EqualTo(baselineRoute.PrimaryScenePath),
                "The direct Activity request must preserve the owning Route Primary Scene as active.");

            Task<FrameworkActivityRequestResult> restoreActivityTask = owner.RequestActivityAsync(
                baselineActivity,
                "i05-post-commit-test",
                "restore-original-activity-after-committed-readiness-failure");
            while (!restoreActivityTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkActivityRequestResult restoredActivity =
                restoreActivityTask.GetAwaiter().GetResult();
            Assert.That(restoredActivity.Succeeded, Is.True, restoredActivity.Message);
            Assert.That(owner.State.CurrentRoute, Is.SameAs(baselineRoute));
            Assert.That(owner.State.CurrentActivity, Is.SameAs(baselineActivity));
            Assert.That(readinessReleasedCount, Is.EqualTo(1),
                "The failed committed Activity occurrence must release its exact readiness participant once during recovery.");
            Assert.That(
                SceneManager.GetSceneByPath(_postCommitReadinessContentScenePath).isLoaded,
                Is.False,
                "The direct request's failed Activity content scene must be released after the baseline Activity regains authority.");
        }

        [UnityTest]
        public IEnumerator RoutePrimarySceneUnavailableBeforeCommit_ProjectsRetainedRouteAndCanRestoreActivity()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The Route pre-commit integration requires Play Mode.");

            FrameworkRuntimeHost owner = CreateHost(LoadApplication(ApplicationAssetPath));
            _host = owner;
            Task<FrameworkGameFlowStartResult> startupTask = owner.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();
            Assert.That(startup.Started, Is.True, startup.Message);

            RouteAsset baselineRoute = owner.State.CurrentRoute;
            ActivityAsset baselineActivity = owner.State.CurrentActivity;
            Scene baselinePrimaryScene = SceneManager.GetActiveScene();
            Assert.That(baselineRoute, Is.Not.Null);
            Assert.That(baselineActivity, Is.Not.Null);
            Assert.That(baselinePrimaryScene.IsValid() && baselinePrimaryScene.isLoaded, Is.True);

            RouteAsset unavailableRoute = ScriptableObject.CreateInstance<RouteAsset>();
            SetString(unavailableRoute, "routeId", "host-stop.pre-commit.unavailable-route");
            SetString(unavailableRoute, "routeName", "Unavailable Pre-Commit Route");
            SetString(
                unavailableRoute,
                "primaryScenePath",
                AssetFolder + "/UnavailablePrimary.unity");
            SetString(unavailableRoute, "primarySceneName", "UnavailablePrimary");
            Assert.That(unavailableRoute.HasValidRouteId, Is.True);
            Assert.That(unavailableRoute.HasPrimaryScene, Is.True);
            Assert.That(
                Application.CanStreamedLevelBeLoaded(unavailableRoute.PrimaryScenePath),
                Is.False,
                "The target scene must be unavailable to produce a real pre-commit Scene Lifecycle failure.");

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(?s)Route Scene Composition Result route='Unavailable Pre-Commit Route'.*" +
                    "kind='FailedInvalidConfig'.*currentRouteId='host-stop.route'.*" +
                    "currentRoute='Host Stop Route'"));
            Task<FrameworkRouteRequestResult> failedRequestTask = owner.RequestRouteAsync(
                unavailableRoute,
                "i05-pre-commit-test",
                "reject-unavailable-route-before-commit");
            while (!failedRequestTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRouteRequestResult failedRequest = failedRequestTask.GetAwaiter().GetResult();
            Assert.That(failedRequest.Succeeded, Is.False, failedRequest.Message);
            Assert.That(failedRequest.DestinationAuthoritative, Is.False, failedRequest.Message);
            Assert.That(owner.CurrentGameFlowRuntime.CurrentRoute, Is.SameAs(baselineRoute));
            Assert.That(owner.CurrentGameFlowRuntime.CurrentActivity, Is.Null,
                "A failed Route preparation may leave the previous Activity cleared; runtime authority must report that partial outcome.");
            Assert.That(
                owner.CurrentGameFlowRuntime.TryGetCurrentRouteLifecycleResult(
                    out RouteLifecycleStartResult retainedRouteResult),
                Is.True,
                "Game Flow must refresh its retained Route result after a pre-commit failure.");
            Assert.That(retainedRouteResult.Route, Is.SameAs(baselineRoute));
            Assert.That(retainedRouteResult.ActivityFlowResult.Activity, Is.Null,
                "The retained Route result must project the cleared Activity authority.");
            Assert.That(owner.State.CurrentRoute, Is.SameAs(baselineRoute));
            Assert.That(owner.State.CurrentActivity, Is.Null,
                "Host state must project the live Activity authority after a non-authoritative Route failure.");
            Assert.That(baselinePrimaryScene.IsValid() && baselinePrimaryScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(baselinePrimaryScene.handle));
            Assert.That(owner.CurrentGameFlowRuntime.CurrentTransitionGateSnapshot.HasBlockers, Is.False);
            Assert.That(owner.CurrentGameFlowRuntime.HasLifecycleRequestInFlight, Is.False);

            Task<FrameworkActivityRequestResult> recoveryTask = owner.RequestActivityAsync(
                baselineActivity,
                "i05-pre-commit-test",
                "restore-baseline-activity-after-unavailable-route");
            while (!recoveryTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkActivityRequestResult recovery = recoveryTask.GetAwaiter().GetResult();
            Assert.That(recovery.Succeeded, Is.True, recovery.Message);
            Assert.That(owner.State.CurrentRoute, Is.SameAs(baselineRoute));
            Assert.That(owner.State.CurrentActivity, Is.SameAs(baselineActivity));
            Assert.That(owner.CurrentGameFlowRuntime.CurrentRoute, Is.SameAs(baselineRoute));
            Assert.That(owner.CurrentGameFlowRuntime.CurrentActivity, Is.SameAs(baselineActivity));

            UnityEngine.Object.DestroyImmediate(unavailableRoute);
        }

        [UnityTest]
        public IEnumerator ControlledRouteSceneRelease_RejectionRetainsPrimaryAndRetriesSameRouteOwner()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The controlled-stop integration requires the Editor to enter Play Mode.");
            HostSessionLifecycleTestProbe.ClearRecords();
            FrameworkRuntimeHost owner = CreateHost(LoadApplication(ApplicationAssetPath));
            _host = owner;

            Task<FrameworkGameFlowStartResult> startupTask = owner.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();
            Assert.That(startup.Started, Is.True, startup.Message);

            RouteAsset exactRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(
                AssetFolder + "/Route.asset");
            Scene primaryScene = SceneManager.GetSceneByPath(_scenePaths[0]);
            Assert.That(exactRoute, Is.Not.Null);
            Assert.That(primaryScene.IsValid() && primaryScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(primaryScene.handle));

            HostSessionLifecycleTestProbe.RejectRelease(
                "route-scene",
                SceneCompositionScopeKind.Scene,
                rejectionCount: 1);
            LogAssert.Expect(
                LogType.Error,
                new Regex("FrameworkRuntimeHost controlled stop retained the live Host because canonical Route exit did not release every retained obligation\\."));
            Task<FrameworkRuntimeHostStopResult> firstStopTask =
                owner.StopAsync("route-scene-release-first-attempt");
            while (!firstStopTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult firstStop = firstStopTask.GetAwaiter().GetResult();
            Assert.That(firstStop.Succeeded, Is.False);
            Assert.That(firstStop.Diagnostic, Does.Contain("route-scene rejected Scene release once."));
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopping));
            Assert.That(owner.State.CurrentRoute, Is.SameAs(exactRoute));
            Assert.That(primaryScene.IsValid() && primaryScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(primaryScene.handle));
            Assert.That(CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Scene), Has.Count.EqualTo(SessionParticipantCount));
            Assert.That(
                CallsForScopeKindAndParticipant(
                    SceneCompositionOperation.Releasing,
                    SceneCompositionScopeKind.Scene,
                    "route-scene"),
                Has.Count.EqualTo(1));

            Task<FrameworkRuntimeHostStopResult> retryTask =
                owner.StopAsync("route-scene-release-retry");
            while (!retryTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult retry = retryTask.GetAwaiter().GetResult();
            Assert.That(retry.Succeeded, Is.True, retry.Diagnostic);
            Assert.That(owner.State.CurrentRoute, Is.Null);
            Assert.That(primaryScene.IsValid() && primaryScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(primaryScene.handle));
            Assert.That(CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Scene), Has.Count.EqualTo(SessionParticipantCount * 2));
            Assert.That(
                CallsForScopeKindAndParticipant(
                    SceneCompositionOperation.Releasing,
                    SceneCompositionScopeKind.Scene,
                    "route-scene"),
                Has.Count.EqualTo(2));

            UnityEngine.Object.Destroy(owner.gameObject);
            foreach (GameObject root in HostSessionLifecycleTestProbe.CreatedRoots)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            yield return null;
            _host = null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator StartupFailureAfterPersistentContentCommit_LeavesHostInertAndReleasesExactSessionRoots()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The controlled-stop integration requires the Editor to enter Play Mode.");
            HostSessionLifecycleTestProbe.ClearRecords();
            _host = CreateHost(LoadApplication(ApplicationAssetPath));
            FrameworkRuntimeHost owner = _host;

            Task<FrameworkGameFlowStartResult> startupTask = owner.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();

            Assert.That(startup.Started, Is.False);
            Assert.That(startup.Message, Does.Contain("Route ID is missing or invalid"));
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Failed));
            Assert.That(
                CallsForScopeKind(SceneCompositionOperation.Available, SceneCompositionScopeKind.Session),
                Has.Count.EqualTo(SessionParticipantCount));
            Assert.That(SceneManager.GetSceneByPath(PersistentContentScenePath).IsValid(), Is.False,
                "The Persistent Content source unloads only after its Session composition and application-lifetime commit succeed.");

            IReadOnlyList<GameObject> roots =
                CallsForScopeKind(SceneCompositionOperation.Available, SceneCompositionScopeKind.Session)[0].Roots;
            GameObject exactRoot = roots[0];
            Task<FrameworkRuntimeHostStopResult> stopTask =
                owner.StopAsync("startup-failure-controlled-stop");
            while (!stopTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult stop = stopTask.GetAwaiter().GetResult();

            Assert.That(stop.Succeeded, Is.True, stop.Diagnostic);
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
            Assert.That(exactRoot != null, Is.True);
            Assert.That(
                CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Session),
                Has.Count.EqualTo(SessionParticipantCount));
            AssertReleaseCallsUseSameOwnerAndRoots(owner, roots);

            UnityEngine.Object.Destroy(owner.gameObject);
            foreach (GameObject root in HostSessionLifecycleTestProbe.CreatedRoots)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            yield return null;
            _host = null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ControlledStop_TwoJoinedPlayersCameraAndPause_RetriesSameHostToBaseline()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.That(EditorApplication.isPlaying, Is.True,
                "The integrated Player/camera/Pause teardown requires Play Mode.");
            HostSessionLifecycleTestProbe.ClearRecords();

            FrameworkRuntimeHost owner = CreateHost(LoadApplication(ApplicationAssetPath));
            _host = owner;
            Task<FrameworkGameFlowStartResult> startupTask = owner.StartAsync();
            while (!startupTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkGameFlowStartResult startup = startupTask.GetAwaiter().GetResult();
            Assert.That(startup.Started, Is.True, startup.Message);
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Operational));

            Assert.That(
                owner.TryResolveLocalPlayerProvisioningAuthoring(
                    out LocalPlayerProvisioningAuthoring provisioning,
                    out bool provisioningConfigured,
                    out string provisioningDiagnostic),
                Is.True,
                provisioningDiagnostic);
            Assert.That(provisioningConfigured, Is.True, provisioningDiagnostic);
            Assert.That(
                LocalPlayerProvisioningRuntimeHostModule.TryAttach(
                    owner,
                    provisioning,
                    out LocalPlayerProvisioningRuntimeHostModule provisioningRuntime,
                    out string provisioningIssue),
                Is.True,
                provisioningIssue);
            Assert.That(provisioningRuntime.IsReady, Is.True, provisioningRuntime.Diagnostic);

            // Match Bootstrap: provisioning attaches after Game Flow startup.
            // Let the scoped-access module refresh its Join binding to this manager.
            for (int frame = 0; frame < 2; frame++)
            {
                yield return null;
            }

            Scene routeScene = SceneManager.GetSceneByPath(_scenePaths[0]);
            PlayerSessionJoinTrigger join = FindComponent<PlayerSessionJoinTrigger>(routeScene);
            PlayerSessionObserver observer = FindComponent<PlayerSessionObserver>(routeScene);
            Assert.That(join, Is.Not.Null);
            Assert.That(observer, Is.Not.Null);

            bool routeAccessAvailable = false;
            for (int frame = 0; frame < 60; frame++)
            {
                if (observer.IsAvailable)
                {
                    routeAccessAvailable = true;
                    break;
                }

                yield return null;
            }

            Assert.That(
                routeAccessAvailable,
                Is.True,
                "Route-scoped Player Session Observer did not bind after Framework startup. " +
                observer.Diagnostic);

            Keyboard firstKeyboard = InputSystem.AddDevice<Keyboard>();
            Keyboard secondKeyboard = InputSystem.AddDevice<Keyboard>();
            _temporaryInputDevices.Add(firstKeyboard);
            _temporaryInputDevices.Add(secondKeyboard);

            LocalPlayerJoinOutcome firstJoin = join.RequestJoin(
                new LocalPlayerJoinRequest(
                    "host-stop-integration",
                    "join-player-1",
                    firstKeyboard,
                    "QA.Keyboard"));
            LocalPlayerJoinOutcome secondJoin = join.RequestJoin(
                new LocalPlayerJoinRequest(
                    "host-stop-integration",
                    "join-player-2",
                    secondKeyboard,
                    "QA.Keyboard"));

            Assert.That(firstJoin.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.Joined),
                "code=" + firstJoin.Code + " admission=" + firstJoin.AdmissionState +
                " original=" + firstJoin.OriginalCode);
            Assert.That(secondJoin.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.Joined),
                "code=" + secondJoin.Code + " admission=" + secondJoin.AdmissionState +
                " original=" + secondJoin.OriginalCode);

            PlayerParticipationSnapshot joinedSnapshot = null;
            for (int frame = 0; frame < 60; frame++)
            {
                if (observer.TryGetSnapshot(out joinedSnapshot) &&
                    joinedSnapshot.JoinedCount == 2)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(joinedSnapshot, Is.Not.Null);
            Assert.That(joinedSnapshot.JoinedCount, Is.EqualTo(2));

            PlayerSlotProfile firstSlot = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(
                "Assets/QA-IF-ADR-043/Settings/PlayerSlot_QaIfAdr043_P1.asset");
            PlayerSlotProfile secondSlot = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(
                "Assets/QA-IF-ADR-043/Settings/PlayerSlot_QaIfAdr043_P2.asset");
            SessionCameraAssignmentAsset assignment = AssetDatabase.LoadAssetAtPath<SessionCameraAssignmentAsset>(
                "Assets/QA-IF-ADR-043/Settings/CameraAssignment_QaIfAdr043_Individual.asset");
            CameraOutputDefinition firstDefinition = AssetDatabase.LoadAssetAtPath<CameraOutputDefinition>(
                "Assets/QA-IF-ADR-043/Camera/CameraOutput_QaIfAdr043_P1.asset");
            CameraOutputDefinition secondDefinition = AssetDatabase.LoadAssetAtPath<CameraOutputDefinition>(
                "Assets/QA-IF-ADR-043/Camera/CameraOutput_QaIfAdr043_P2.asset");
            Assert.That(firstSlot, Is.Not.Null);
            Assert.That(secondSlot, Is.Not.Null);
            Assert.That(assignment, Is.Not.Null);
            Assert.That(firstDefinition, Is.Not.Null);
            Assert.That(secondDefinition, Is.Not.Null);

            LocalPlayerHostAuthoring firstHost = FindJoinedHost(firstSlot.PlayerSlotId);
            LocalPlayerHostAuthoring secondHost = FindJoinedHost(secondSlot.PlayerSlotId);
            Assert.That(firstHost, Is.Not.Null);
            Assert.That(secondHost, Is.Not.Null);
            Assert.That(firstHost.PlayerInput, Is.Not.Null);
            Assert.That(secondHost.PlayerInput, Is.Not.Null);
            PlayerInput firstPlayerInput = firstHost.PlayerInput;
            PlayerInput secondPlayerInput = secondHost.PlayerInput;

            CameraOutputAuthoring[] outputs = Array.Empty<CameraOutputAuthoring>();
            CameraOutputAuthoring firstOutput = null;
            CameraOutputAuthoring secondOutput = null;
            for (int frame = 0; frame < 60; frame++)
            {
                outputs = UnityEngine.Object.FindObjectsByType<CameraOutputAuthoring>(
                    FindObjectsSortMode.None);
                firstOutput = FindOutput(outputs, firstDefinition);
                secondOutput = FindOutput(outputs, secondDefinition);
                if (outputs.Length == 2 &&
                    firstOutput != null && firstOutput.Session?.OutputState.HasActiveAssignment == true &&
                    secondOutput != null && secondOutput.Session?.OutputState.HasActiveAssignment == true &&
                    firstPlayerInput.camera == firstOutput.UnityCamera &&
                    secondPlayerInput.camera == secondOutput.UnityCamera)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(outputs, Has.Length.EqualTo(2));
            Assert.That(firstOutput, Is.Not.Null);
            Assert.That(secondOutput, Is.Not.Null);
            Assert.That(firstOutput.Session.OutputState.HasActiveAssignment, Is.True);
            Assert.That(secondOutput.Session.OutputState.HasActiveAssignment, Is.True);
            Assert.That(firstOutput.Session.OutputState.ActiveAssignmentId,
                Is.EqualTo(assignment.AssignmentId));
            Assert.That(secondOutput.Session.OutputState.ActiveAssignmentId,
                Is.EqualTo(assignment.AssignmentId));
            Assert.That(firstPlayerInput.camera, Is.SameAs(firstOutput.UnityCamera));
            Assert.That(secondPlayerInput.camera, Is.SameAs(secondOutput.UnityCamera));

            PlayerPauseInput[] pauseBindings =
            {
                firstHost.GetComponent<PlayerPauseInput>(),
                secondHost.GetComponent<PlayerPauseInput>()
            };
            Assert.That(Array.Exists(
                    pauseBindings,
                    binding => binding != null && binding.HasActiveBinding),
                Is.True,
                "The supported single-player Pause binding must be owned while joined Players are active.");

            const float runningTimeScale = 0.625f;
            Time.timeScale = runningTimeScale;
            PauseResult paused = owner.RequestPause(
                PauseRequestKind.Pause,
                "host-stop-integration",
                "pause-before-controlled-stop");
            Assert.That(paused.Completed, Is.True, paused.ToDiagnosticString());
            Assert.That(owner.PauseState, Is.EqualTo(Immersive.Framework.Pause.PauseState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.0001f));

            HostSessionLifecycleTestProbe.RejectRelease(
                "second",
                SceneCompositionScopeKind.Session,
                rejectionCount: 1);
            LogAssert.Expect(
                LogType.Error,
                new Regex("FrameworkRuntimeHost controlled stop retained the live Host for retry\\."));
            Task<FrameworkRuntimeHostStopResult> firstStopTask =
                owner.StopAsync("two-player-controlled-stop-first-attempt");
            while (!firstStopTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult firstStop = firstStopTask.GetAwaiter().GetResult();
            Assert.That(firstStop.Succeeded, Is.False);
            Assert.That(firstStop.Diagnostic, Does.Contain("second rejected Session release once."));
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopping));
            Assert.That(owner.PauseState, Is.EqualTo(Immersive.Framework.Pause.PauseState.Running));
            Assert.That(Time.timeScale, Is.EqualTo(runningTimeScale).Within(0.0001f));
            Assert.That(owner.TryGetPlayerParticipationSnapshot(out PlayerParticipationSnapshot stoppedPlayers), Is.True);
            Assert.That(stoppedPlayers.JoinedCount, Is.Zero);
            Assert.That(stoppedPlayers.ReservedCount, Is.Zero);
            Assert.That(stoppedPlayers.LeavingCount, Is.Zero);
            AssertThatPlayerStopRestoredPhysicalOwners(
                firstPlayerInput,
                pauseBindings[0],
                firstOutput,
                "first Player");
            AssertThatPlayerStopRestoredPhysicalOwners(
                secondPlayerInput,
                pauseBindings[1],
                secondOutput,
                "second Player");
            Assert.That(CallsForScopeKind(
                    SceneCompositionOperation.Releasing,
                    SceneCompositionScopeKind.Session),
                Has.Count.EqualTo(SessionParticipantCount));

            Task<FrameworkRuntimeHostStopResult> retryTask =
                owner.StopAsync("two-player-controlled-stop-retry");
            while (!retryTask.IsCompleted)
            {
                yield return null;
            }

            FrameworkRuntimeHostStopResult retry = retryTask.GetAwaiter().GetResult();
            Assert.That(retry.Succeeded, Is.True, retry.Diagnostic);
            Assert.That(owner.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
            Assert.That(owner.TryGetPlayerParticipationSnapshot(out PlayerParticipationSnapshot finalPlayers), Is.True);
            Assert.That(finalPlayers.JoinedCount, Is.Zero);
            Assert.That(finalPlayers.ReservedCount, Is.Zero);
            Assert.That(finalPlayers.LeavingCount, Is.Zero);
            AssertReleaseCallsUseSameOwnerAndRoots(
                owner,
                CallsForScopeKind(
                    SceneCompositionOperation.Available,
                    SceneCompositionScopeKind.Session)[0].Roots);

            UnityEngine.Object.Destroy(owner.gameObject);
            foreach (GameObject root in HostSessionLifecycleTestProbe.CreatedRoots)
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            RemoveTemporaryInputDevices();
            yield return null;
            _host = null;
            yield return new ExitPlayMode();
        }

        private void RestorePlayModeStartupSetting()
        {
            if (!_changedPlayModeStartup)
            {
                return;
            }

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>("ImmersiveFrameworkSettings");
            if (settings == null)
            {
                return;
            }

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("editorPlayModeStartup").enumValueIndex =
                _originalPlayModeStartup;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            _changedPlayModeStartup = false;
        }

        private FrameworkRuntimeHost CreateHost(GameApplicationAsset application)
        {
            bool created = FrameworkRuntimeHost.TryCreateWithProjectFrameRate(
                application,
                new ApplicationFrameRatePolicy(),
                explicitPlayerSessionProfile: null,
                out FrameworkRuntimeHost host,
                out _,
                out PlayerParticipationOperationResult participationInitialization);

            Assert.That(created, Is.True, participationInitialization?.ToDiagnosticString());
            Assert.That(host, Is.Not.Null);
            return host;
        }

        private void CreateApplicationFixture(string routeId, bool configureTwoPlayerCameraStop)
        {
            string routeScenePath = CreateScene("StartupRoute", addParticipant: false);
            string contentScenePath = CreateScene(
                "PersistentContent",
                addParticipant: true,
                addRouteReleaseProbeParticipant: true,
                addPlayerProvisioning: configureTwoPlayerCameraStop);
            Scene stagedContentScene = SceneManager.GetSceneByPath(contentScenePath);
            if (stagedContentScene.IsValid() && stagedContentScene.isLoaded)
            {
                EditorSceneManager.CloseScene(stagedContentScene, removeScene: true);
            }
            var buildScenes = new List<EditorBuildSettingsScene>(_originalBuildScenes)
            {
                new EditorBuildSettingsScene(routeScenePath, enabled: true),
                new EditorBuildSettingsScene(contentScenePath, enabled: true)
            };
            EditorBuildSettings.scenes = buildScenes.ToArray();

            ActivityAsset activity = CreateAsset<ActivityAsset>("Activity.asset");
            SetString(activity, "activityId", "host-stop.activity");
            SetString(activity, "activityName", "Host Stop Activity");
            AssetDatabase.SaveAssetIfDirty(activity);
            string activityAssetPath = AssetFolder + "/Activity.asset";
            AssetDatabase.ImportAsset(
                activityAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            ActivityAsset importedActivity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(activityAssetPath);
            Assert.That(importedActivity, Is.Not.Null,
                "The authored Activity fixture must reload from its asset path.");

            RouteAsset route = CreateAsset<RouteAsset>("Route.asset");
            var serializedRoute = new SerializedObject(route);
            serializedRoute.FindProperty("routeId").stringValue = routeId;
            serializedRoute.FindProperty("routeName").stringValue = "Host Stop Route";
            serializedRoute.FindProperty("primaryScenePath").stringValue = routeScenePath;
            serializedRoute.FindProperty("primarySceneName").stringValue = "StartupRoute";
            serializedRoute.FindProperty("startupActivity").objectReferenceValue = importedActivity;
            serializedRoute.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);
            Assert.That(route.PrimaryScenePath, Is.EqualTo(routeScenePath),
                "The Route fixture must retain the Primary Scene path immediately after authoring.");
            string routeAssetPath = AssetFolder + "/Route.asset";
            AssetDatabase.SaveAssetIfDirty(route);
            string routeYamlPath = Path.Combine(
                Application.dataPath,
                "__IF_FrameworkRuntimeHostControlledStopTests",
                "Route.asset");
            string routeYaml = File.ReadAllText(routeYamlPath);
            Assert.That(routeYaml, Does.Contain(routeScenePath),
                "The Route asset file must serialize its Primary Scene path before import.");
            AssetDatabase.ImportAsset(
                routeAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            RouteAsset importedRoute =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(routeAssetPath);
            Assert.That(importedRoute, Is.Not.Null,
                "The authored Route fixture must reload from its asset path.");
            Assert.That(importedRoute.PrimaryScenePath, Is.EqualTo(routeScenePath),
                "The Route asset must retain its Primary Scene path after explicit import.");
            Assert.That(importedRoute.StartupActivity, Is.SameAs(importedActivity),
                "The Route fixture must retain its exact startup Activity reference.");
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(importedRoute.PrimaryScenePath),
                Is.Not.Null,
                "Host fixture Route Primary Scene must resolve to its saved scene asset.");

            if (configureTwoPlayerCameraStop)
            {
                ConfigureRoutePlayerSessionConsumer(routeScenePath, importedRoute);
            }

            GameApplicationAsset application = CreateAsset<GameApplicationAsset>("GameApplication.asset");
            var serializedApplication = new SerializedObject(application);
            serializedApplication.FindProperty("applicationName").stringValue =
                "Host Stop Integration Test";
            serializedApplication.FindProperty("startupRoute").objectReferenceValue =
                importedRoute;
            SerializedProperty persistentContent = serializedApplication.FindProperty("persistentContent");
            persistentContent.FindPropertyRelative("scenePath").stringValue = contentScenePath;
            persistentContent.FindPropertyRelative("sceneName").stringValue = "PersistentContent";
            GameObject cameraOutputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/QA-NEW-001/Camera/PF_CameraOutput_QaNew001.prefab");
            Assert.That(cameraOutputPrefab, Is.Not.Null,
                "Host integration requires the QAFramework's canonical Camera Output prefab.");
            SerializedProperty outputPrefabs = serializedApplication
                .FindProperty("cameraSession")
                .FindPropertyRelative("outputPrefabs");
            if (configureTwoPlayerCameraStop)
            {
                PlayerSessionProfile session = AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(
                    "Assets/QA-IF-ADR-043/Settings/PlayerSession_QaIfAdr043.asset");
                SessionCameraAssignmentAsset assignment = AssetDatabase.LoadAssetAtPath<SessionCameraAssignmentAsset>(
                    "Assets/QA-IF-ADR-043/Settings/CameraAssignment_QaIfAdr043_Individual.asset");
                GameObject firstOutput = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/QA-IF-ADR-043/Camera/PF_CameraOutput_QaIfAdr043_P1.prefab");
                GameObject secondOutput = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/QA-IF-ADR-043/Camera/PF_CameraOutput_QaIfAdr043_P2.prefab");
                Assert.That(session, Is.Not.Null);
                Assert.That(assignment, Is.Not.Null);
                Assert.That(firstOutput, Is.Not.Null);
                Assert.That(secondOutput, Is.Not.Null);

                serializedApplication.FindProperty("playerSessionEnabled").boolValue = true;
                serializedApplication.FindProperty("defaultPlayerSessionProfile").objectReferenceValue = session;
                outputPrefabs.arraySize = 2;
                outputPrefabs.GetArrayElementAtIndex(0).objectReferenceValue = firstOutput;
                outputPrefabs.GetArrayElementAtIndex(1).objectReferenceValue = secondOutput;
                SerializedProperty startupAssignments =
                    serializedApplication.FindProperty("startupCameraAssignments");
                startupAssignments.arraySize = 1;
                startupAssignments.GetArrayElementAtIndex(0).objectReferenceValue = assignment;
            }
            else
            {
                outputPrefabs.arraySize = 1;
                outputPrefabs.GetArrayElementAtIndex(0).objectReferenceValue = cameraOutputPrefab;
            }
            serializedApplication.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssetIfDirty(application);
            AssetDatabase.ImportAsset(
                ApplicationAssetPath,
                ImportAssetOptions.ForceSynchronousImport);

            GameApplicationAsset importedApplication =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationAssetPath);
            Assert.That(importedApplication, Is.Not.Null,
                "The authored Game Application fixture must reload from its asset path.");
            Assert.That(importedApplication.StartupRoute, Is.SameAs(importedRoute));
            Assert.That(importedApplication.StartupRoute.PrimaryScenePath, Is.EqualTo(routeScenePath),
                "The reloaded Game Application must retain its Route's Primary Scene path.");
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(importedApplication.StartupRoute.PrimaryScenePath),
                Is.Not.Null,
                "Host fixture Route Primary Scene must resolve to its saved scene asset.");
        }

        private void CreatePostCommitRouteReadinessFixture()
        {
            const string activityContentSceneName = "PostCommitActivityContent";
            string activityContentScenePath = CreateScene(
                activityContentSceneName,
                addParticipant: false);
            Scene contentScene = SceneManager.GetSceneByPath(activityContentScenePath);
            Assert.That(contentScene.IsValid() && contentScene.isLoaded, Is.True,
                "The post-commit Activity content scene must remain loaded while its authoring fixture is configured.");

            const string activityId = "host-stop.post-commit.activity";
            const string activityName = "Post-Commit Readiness Activity";
            string activityAssetPath = AssetFolder + "/PostCommitActivity.asset";
            ActivityAsset activity = CreateAsset<ActivityAsset>("PostCommitActivity.asset");
            SetString(activity, "activityId", activityId);
            SetString(activity, "activityName", activityName);
            SetInteger(
                activity,
                "activityEntryReadinessPolicy",
                (int)ActivityEntryReadinessPolicy.WaitVisible);
            SetInteger(
                activity,
                "transitionGateMode",
                (int)TransitionGateMode.InputInteractionAndGameplay);

            AssetDatabase.SaveAssetIfDirty(activity);
            AssetDatabase.ImportAsset(
                activityAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(activityAssetPath);
            Assert.That(activity, Is.Not.Null,
                "The authored post-commit Activity must reload from its persisted asset.");
            Assert.That(activity.HasValidActivityId, Is.True,
                "The post-commit Activity lost its stable ID during its first save/import. serializedId='" +
                new SerializedObject(activity).FindProperty("activityId").stringValue + "'.");
            Assert.That(activity.ActivityName, Is.EqualTo(activityName),
                "The post-commit Activity lost its display name during its first save/import.");

            ActivityContentProfileAsset profile =
                CreateAsset<ActivityContentProfileAsset>("PostCommitActivityContentProfile.asset");
            SetString(profile, "profileId", "host-stop.post-commit.profile");
            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty scenes = serializedProfile.FindProperty("scenes");
            Assert.That(scenes, Is.Not.Null,
                "Activity Content Profile must expose its serialized scene declarations.");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("contentId").stringValue =
                "host-stop.post-commit.content";
            entry.FindPropertyRelative("scenePath").stringValue = activityContentScenePath;
            entry.FindPropertyRelative("sceneName").stringValue = activityContentSceneName;
            entry.FindPropertyRelative("requiredness").intValue =
                (int)FrameworkContentRequiredness.Required;
            entry.FindPropertyRelative("loadMode").intValue =
                (int)ActivityContentSceneLoadMode.Additive;
            entry.FindPropertyRelative("releasePolicy").intValue =
                (int)ActivityContentReleasePolicy.ReleaseOnActivityChange;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            AssetDatabase.ImportAsset(
                AssetFolder + "/PostCommitActivityContentProfile.asset",
                ImportAssetOptions.ForceSynchronousImport);
            profile = AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(
                AssetFolder + "/PostCommitActivityContentProfile.asset");
            Assert.That(profile, Is.Not.Null,
                "The Post-commit Activity Content Profile must reload from its persisted asset.");

            SetObjectReference(activity, "activityContentProfile", profile);
            AssetDatabase.SaveAssetIfDirty(activity);
            AssetDatabase.ImportAsset(
                activityAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            _postCommitReadinessActivity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                activityAssetPath);
            Assert.That(_postCommitReadinessActivity, Is.Not.Null,
                "The post-commit Activity must reload from its persisted asset.");
            Assert.That(_postCommitReadinessActivity.HasValidActivityId, Is.True,
                "The post-commit Activity lost its stable ID when its content profile was assigned. serializedId='" +
                new SerializedObject(_postCommitReadinessActivity)
                    .FindProperty("activityId").stringValue + "'.");
            Assert.That(_postCommitReadinessActivity.ActivityName, Is.EqualTo(activityName),
                "The post-commit Activity lost its display name when its content profile was assigned.");
            Assert.That(_postCommitReadinessActivity.ActivityContentProfile, Is.SameAs(profile),
                "The post-commit Activity must retain its exact persisted Content Profile reference.");

            GameObject[] roots = contentScene.GetRootGameObjects();
            Assert.That(roots, Has.Length.EqualTo(1),
                "The Activity-owned content fixture must have one authored root.");
            GameObject contentRoot = roots[0];
            ActivityContentContribution contribution =
                contentRoot.AddComponent<ActivityContentContribution>();
            SetObjectReference(contribution, "activity", _postCommitReadinessActivity);
            SetString(contribution, "localContentId", "host-stop.post-commit.contribution");
            SetInteger(
                contribution,
                "requiredness",
                (int)FrameworkContentRequiredness.Required);

            ActivityReadinessParticipant participant =
                contentRoot.AddComponent<ActivityReadinessParticipant>();
            SetString(participant, "participantId", "host-stop.post-commit.required");
            SetInteger(
                participant,
                "requiredness",
                (int)ActivityContentExecutionRequiredness.Required);

            Assert.That(EditorSceneManager.SaveScene(contentScene), Is.True,
                "The Activity content scene must be persisted with its Activity-owned contribution and readiness participant.");
            Assert.That(EditorSceneManager.CloseScene(contentScene, removeScene: true), Is.True,
                "The Activity content scene must be closed so the request must materialize its authored scene at runtime.");

            string routeScenePath = CreateScene("PostCommitRoute", addParticipant: false);
            Scene routeScene = SceneManager.GetSceneByPath(routeScenePath);
            Assert.That(routeScene.IsValid() && routeScene.isLoaded, Is.True,
                "The post-commit Route Primary Scene must be persisted before it is authored onto the Route.");
            Assert.That(EditorSceneManager.CloseScene(routeScene, removeScene: true), Is.True,
                "The Route Primary Scene must be closed so the request must load its persisted scene at runtime.");

            RouteAsset route = CreateAsset<RouteAsset>("PostCommitRoute.asset");
            SetString(route, "routeId", "host-stop.post-commit.route");
            SetString(route, "routeName", "Post-Commit Readiness Route");
            SetString(route, "primaryScenePath", routeScenePath);
            SetString(route, "primarySceneName", "PostCommitRoute");
            SetObjectReference(route, "startupActivity", _postCommitReadinessActivity);
            AssetDatabase.SaveAssetIfDirty(route);
            string routeAssetPath = AssetFolder + "/PostCommitRoute.asset";
            AssetDatabase.ImportAsset(
                routeAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            _postCommitReadinessRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(
                routeAssetPath);
            Assert.That(_postCommitReadinessRoute, Is.Not.Null,
                "The post-commit Route must reload from its persisted asset.");
            Assert.That(_postCommitReadinessRoute.PrimaryScenePath, Is.EqualTo(routeScenePath),
                "The Route must retain its Primary Scene path after save/import.");
            Assert.That(_postCommitReadinessRoute.StartupActivity,
                Is.SameAs(_postCommitReadinessActivity));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(routeScenePath), Is.Not.Null,
                "The post-commit Route Primary Scene asset must resolve from its authored path.");

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(activityContentScenePath, enabled: true),
                new EditorBuildSettingsScene(routeScenePath, enabled: true)
            };
            EditorBuildSettings.scenes = buildScenes.ToArray();
            _postCommitReadinessContentScenePath = activityContentScenePath;
            AssetDatabase.SaveAssets();
        }

        private static GameApplicationAsset LoadApplication(string applicationAssetPath)
        {
            AssetDatabase.ImportAsset(
                applicationAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    applicationAssetPath);
            Assert.That(application, Is.Not.Null,
                "Game Application fixture did not import at '" + applicationAssetPath + "'.");
            Assert.That(application.StartupRoute, Is.Not.Null,
                "Game Application fixture must retain its exact Startup Route reference.");
            Assert.That(application.StartupRoute.HasPrimaryScene, Is.True,
                "Game Application Startup Route has no Primary Scene path.");
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(application.StartupRoute.PrimaryScenePath),
                Is.Not.Null,
                "Game Application Startup Route Primary Scene asset could not be resolved.");
            return application;
        }

        private string CreateScene(
            string sceneName,
            bool addParticipant,
            bool addRouteReleaseProbeParticipant = false,
            bool addPlayerProvisioning = false)
        {
            string scenePath = _assetFolder + "/" + sceneName + ".unity";
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True,
                "The Host fixture must create a loaded scene before adding its participant root.");
            GameObject root = new(sceneName + "Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            Assert.That(root.scene.handle, Is.EqualTo(scene.handle),
                "The fixture participant root must belong to its declared scene.");
            if (addRouteReleaseProbeParticipant)
            {
                HostSessionLifecycleTestProbe routeReleaseProbe =
                    root.AddComponent<HostSessionLifecycleTestProbe>();
                routeReleaseProbe.Configure("route-scene");
            }

            if (addParticipant)
            {
                HostSessionLifecycleTestProbe first = root.AddComponent<HostSessionLifecycleTestProbe>();
                first.Configure("first");
                HostSessionLifecycleTestProbe second = root.AddComponent<HostSessionLifecycleTestProbe>();
                second.Configure("second");
            }

            if (addPlayerProvisioning)
            {
                ConfigurePlayerProvisioning(root);
            }

            Assert.That(
                EditorSceneManager.SaveScene(scene, scenePath),
                Is.True,
                "The Host fixture scene must be saved before its path is assigned to Route.");
            _scenePaths.Add(scenePath);
            return scenePath;
        }

        private void ConfigureRoutePlayerSessionConsumer(
            string routeScenePath,
            RouteAsset route)
        {
            Scene scene = SceneManager.GetSceneByPath(routeScenePath);
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True,
                "Route scene must remain loaded while the integrated Join scope is authored.");
            GameObject root = scene.GetRootGameObjects()[0];

            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            SetObjectReference(contribution, "route", route);
            SetString(contribution, "localContentId", "host-stop.route-content");

            PlayerSessionJoinTrigger join = root.AddComponent<PlayerSessionJoinTrigger>();
            SetInteger(join, "scope", (int)LocalPlayerProvisioningConsumerScope.Route);
            PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
            SetInteger(observer, "scope", (int)LocalPlayerProvisioningConsumerScope.Route);

            Assert.That(EditorSceneManager.SaveScene(scene), Is.True,
                "Route Join and Observer fixture must be saved before Play Mode.");
        }

        private static void ConfigurePlayerProvisioning(GameObject root)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/QA-IF-ADR-044/Players/PF_LocalPlayerHost_QaIfAdr044.prefab");
            Assert.That(playerPrefab, Is.Not.Null,
                "The integrated Host test requires the QAFramework Local Player Host prefab with its supported Pause/Input binding.");

            PlayerInputManager manager = root.AddComponent<PlayerInputManager>();
            manager.playerPrefab = playerPrefab;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            manager.splitScreen = false;
            var serializedManager = new SerializedObject(manager);
            SerializedProperty maxPlayers = serializedManager.FindProperty("m_MaxPlayerCount");
            Assert.That(maxPlayers, Is.Not.Null,
                "PlayerInputManager must expose its serialized maximum-player setting.");
            maxPlayers.intValue = 2;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            LocalPlayerProvisioningAuthoring provisioning =
                root.AddComponent<LocalPlayerProvisioningAuthoring>();
            SetObjectReference(provisioning, "playerInputManager", manager);
            SetObjectReference(provisioning, "localPlayerHostPrefab", playerPrefab);
            LocalPlayerProvisioningEndpointRegistration registration =
                root.AddComponent<LocalPlayerProvisioningEndpointRegistration>();
            SetObjectReference(registration, "provisioningAuthoring", provisioning);

            Assert.That(manager.maxPlayerCount, Is.EqualTo(2));
            Assert.That(provisioning.UsesManualJoin, Is.True);
            Assert.That(provisioning.UsesCSharpJoinNotifications, Is.True);
            Assert.That(registration.ProvisioningAuthoring, Is.SameAs(provisioning));
        }

        private static T FindComponent<T>(Scene scene)
            where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T component = roots[rootIndex].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static LocalPlayerHostAuthoring FindJoinedHost(
            Immersive.Framework.PlayerSlots.PlayerSlotId slotId)
        {
            LocalPlayerHostAuthoring[] hosts =
                UnityEngine.Object.FindObjectsByType<LocalPlayerHostAuthoring>(FindObjectsSortMode.None);
            for (int index = 0; index < hosts.Length; index++)
            {
                if (hosts[index].IsJoined && hosts[index].JoinedPlayerSlotId == slotId)
                {
                    return hosts[index];
                }
            }

            return null;
        }

        private static CameraOutputAuthoring FindOutput(
            CameraOutputAuthoring[] outputs,
            CameraOutputDefinition definition)
        {
            for (int index = 0; index < outputs.Length; index++)
            {
                if (outputs[index].OutputDefinition == definition)
                {
                    return outputs[index];
                }
            }

            return null;
        }

        private static void AssertThatPlayerStopRestoredPhysicalOwners(
            PlayerInput playerInput,
            PlayerPauseInput pauseInput,
            CameraOutputAuthoring cameraOutput,
            string playerLabel)
        {
            Assert.That(
                playerInput == null || playerInput.camera == null,
                Is.True,
                playerLabel + " retained a Camera reference after canonical Leave.");
            Assert.That(
                pauseInput == null || !pauseInput.HasActiveBinding,
                Is.True,
                playerLabel + " retained its Pause/Input binding after canonical Leave.");
            Assert.That(
                cameraOutput == null || cameraOutput.Session == null ||
                !cameraOutput.Session.OutputState.HasActiveAssignment,
                Is.True,
                playerLabel + " retained an active Camera Assignment after canonical Leave.");
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + propertyName + "'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + propertyName + "'.");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private void RemoveTemporaryInputDevices()
        {
            for (int index = _temporaryInputDevices.Count - 1; index >= 0; index--)
            {
                InputDevice device = _temporaryInputDevices[index];
                if (device != null && device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            _temporaryInputDevices.Clear();
        }

        private T CreateAsset<T>(string fileName)
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, _assetFolder + "/" + fileName);
            return asset;
        }

        private void AssertPostCommitReadinessActivityIdentity(string phase)
        {
            ActivityAsset activity = _postCommitReadinessActivity;
            string serializedId = new SerializedObject(activity)
                .FindProperty("activityId").stringValue;
            Assert.That(activity.HasValidActivityId, Is.True,
                "Post-commit Activity identity is invalid " + phase + ". serializedId='" +
                serializedId + "'.");
            Assert.That(activity.ActivityName, Is.EqualTo("Post-Commit Readiness Activity"),
                "Post-commit Activity display name was not retained " + phase + ".");
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, "Missing serialized field '" + propertyName + "'.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void AssertReleaseCallsUseSameOwnerAndRoots(
            FrameworkRuntimeHost expectedOwner,
            IReadOnlyList<GameObject> expectedRoots)
        {
            IReadOnlyList<HostSessionLifecycleTestProbe.Call> releases =
                CallsForScopeKind(SceneCompositionOperation.Releasing, SceneCompositionScopeKind.Session);
            Assert.That(releases, Is.Not.Empty);
            foreach (HostSessionLifecycleTestProbe.Call call in releases)
            {
                Assert.That(call.Scope.Kind, Is.EqualTo(SceneCompositionScopeKind.Session));
                Assert.That(call.Scope, Is.EqualTo(SceneCompositionScope.ForSession(expectedOwner)));
                Assert.That(call.Roots, Is.SameAs(expectedRoots));
                Assert.That(call.Roots[0] != null, Is.True);
            }
        }

        private static IReadOnlyList<HostSessionLifecycleTestProbe.Call> CallsForScopeKind(
            SceneCompositionOperation operation,
            SceneCompositionScopeKind scopeKind)
        {
            IReadOnlyList<HostSessionLifecycleTestProbe.Call> calls =
                HostSessionLifecycleTestProbe.CallsFor(operation);
            var matching = new List<HostSessionLifecycleTestProbe.Call>();
            for (int index = 0; index < calls.Count; index++)
            {
                if (calls[index].Scope.Kind == scopeKind)
                {
                    matching.Add(calls[index]);
                }
            }

            return matching;
        }

        private static IReadOnlyList<HostSessionLifecycleTestProbe.Call> CallsForScopeKindAndParticipant(
            SceneCompositionOperation operation,
            SceneCompositionScopeKind scopeKind,
            string participantKey)
        {
            IReadOnlyList<HostSessionLifecycleTestProbe.Call> calls =
                CallsForScopeKind(operation, scopeKind);
            var matching = new List<HostSessionLifecycleTestProbe.Call>();
            for (int index = 0; index < calls.Count; index++)
            {
                if (string.Equals(
                        calls[index].ParticipantKey,
                        participantKey,
                        StringComparison.Ordinal))
                {
                    matching.Add(calls[index]);
                }
            }

            return matching;
        }

        private readonly struct UnsavedSceneBackup
        {
            internal UnsavedSceneBackup(string path, string originalName, bool wasActive)
            {
                Path = path;
                OriginalName = originalName;
                WasActive = wasActive;
            }

            internal string Path { get; }

            internal string OriginalName { get; }

            internal bool WasActive { get; }
        }
    }

}
