using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause.Tests
{
    public sealed class PauseProductTransactionTests
    {
        [Test]
        public void Transaction_RetainsPreviousPauseSnapshotAndTarget()
        {
            PauseSnapshot previous = PauseSnapshot.FromState(
                PauseState.Running,
                "test",
                "before-pause",
                Array.Empty<string>());

            var transaction = new PauseProductTransaction(
                1,
                previous,
                PauseState.Paused,
                "test",
                "pause-request");

            Assert.That(transaction.IsValid, Is.True);
            Assert.That(transaction.PreviousPause, Is.EqualTo(previous));
            Assert.That(transaction.TargetState, Is.EqualTo(PauseState.Paused));
            Assert.That(transaction.Source, Is.EqualTo("test"));
            Assert.That(transaction.Reason, Is.EqualTo("pause-request"));
        }

        [Test]
        public void Transaction_RejectsInvalidSequenceSnapshotAndTarget()
        {
            PauseSnapshot previous = PauseSnapshot.FromState(
                PauseState.Running,
                "test",
                "before-pause",
                Array.Empty<string>());

            Assert.Throws<ArgumentOutOfRangeException>(() => new PauseProductTransaction(
                0, previous, PauseState.Paused, "test", "invalid-sequence"));
            Assert.Throws<ArgumentException>(() => new PauseProductTransaction(
                1, default, PauseState.Paused, "test", "invalid-snapshot"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PauseProductTransaction(
                1, previous, PauseState.Unknown, "test", "invalid-target"));
            Assert.That(default(PauseProductTransaction).IsValid, Is.False);
        }

        [Test]
        public void RuntimeAssembly_DoesNotExposeInputModeNamespace()
        {
            var runtimeTypes = typeof(PauseProductTransaction).Assembly.GetTypes();
            Type[] remainingTypes = runtimeTypes
                .Where(type => type.Namespace == "Immersive.Framework.InputMode")
                .ToArray();

            Assert.That(remainingTypes, Is.Empty);
            Assert.That(Enum.GetNames(typeof(Immersive.Framework.Identity.FrameworkIdentityDomain)),
                Does.Not.Contain("InputMode"));
        }

        [Test]
        public void ApplicationOnly_InvalidPauseResult_RestoresPreviousLogicalSnapshot()
        {
            PauseSnapshot previous = PauseSnapshot.FromState(
                PauseState.Running,
                "test",
                "before-pause",
                Array.Empty<string>());
            var application = new FakeApplication(previous);
            var context = new PauseProductBindingRuntimeContext(application);

            PauseProductRequestResult result = context.RequestPause(
                PauseRequest.Pause("test.pause", "test", "invalid-result"));

            Assert.That(result.Status, Is.EqualTo(PauseProductRequestStatus.Failed));
            Assert.That(application.RestoredSnapshots, Has.Count.EqualTo(1));
            Assert.That(application.RestoredSnapshots[0], Is.EqualTo(previous));
        }

        [Test]
        public void ApplicationOnly_ReentrantPauseRequest_IsRejectedWhileOperationIsInFlight()
        {
            PauseSnapshot previous = PauseSnapshot.FromState(
                PauseState.Running,
                "test",
                "before-pause",
                Array.Empty<string>());
            var application = new FakeApplication(previous);
            var context = new PauseProductBindingRuntimeContext(application);
            PauseProductRequestResult nested = default;
            application.OnApply = () => nested = context.RequestPause(
                PauseRequest.Pause("nested.pause", "test", "reentrant"));

            context.RequestPause(PauseRequest.Pause("outer.pause", "test", "outer"));

            Assert.That(nested.Status, Is.EqualTo(PauseProductRequestStatus.Rejected));
            Assert.That(application.ApplyCount, Is.EqualTo(1));
        }

        [Test]
        public void PhysicalPauseFailure_RestoresPreviousLogicalSnapshot()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            UnityEngine.Object.DestroyImmediate(_playerInput);

            PauseProductRequestResult result = context.RequestPause(
                PauseRequest.Pause("physical-failure.pause", "test", "destroyed-player-input"));

            Assert.That(result.Status, Is.EqualTo(PauseProductRequestStatus.Failed));
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(application.RestoredSnapshots, Has.Count.EqualTo(1));
            Assert.That(application.RestoredSnapshots[0].State, Is.EqualTo(PauseState.Running));
        }

        [Test]
        public void ResumeRestoreFailure_ReappliesPausedPostureAndRestoresLogicalSnapshot()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            PauseProductRequestResult pause = context.RequestPause(
                PauseRequest.Pause("resume-failure.pause", "test", "pause-first"));
            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.True);
            UnityEngine.Object.DestroyImmediate(_playerInput);

            PauseProductRequestResult resume = context.RequestPause(
                PauseRequest.Resume("resume-failure.resume", "test", "destroyed-player-input"));

            Assert.That(resume.Status, Is.EqualTo(PauseProductRequestStatus.Failed));
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.True);
            Assert.That(application.RestoredSnapshots, Has.Count.EqualTo(1));
            Assert.That(application.RestoredSnapshots[0].State, Is.EqualTo(PauseState.Paused));
        }

        [Test]
        public void PauseResumeAndRelease_RestorePauseAndBindingPosturesSeparately()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            PauseProductBindingToken token = context.ActiveToken;

            PauseProductRequestResult pause = context.RequestPause(
                PauseRequest.Pause("posture.pause", "test", "pause"));
            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            Assert.That(_globalMap.enabled, Is.True);
            Assert.That(_gameplayMap.enabled, Is.False);

            PauseProductRequestResult resume = context.RequestPause(
                PauseRequest.Resume("posture.resume", "test", "resume"));
            Assert.That(resume.Succeeded, Is.True, resume.Diagnostic);
            Assert.That(_globalMap.enabled, Is.True);
            Assert.That(_gameplayMap.enabled, Is.True);

            Assert.That(context.ReleaseBinding(token, "test-release", out _, out string diagnostic), Is.True, diagnostic);
            Assert.That(_globalMap.enabled, Is.EqualTo(_initialGlobalEnabled));
            Assert.That(_gameplayMap.enabled, Is.EqualTo(_initialGameplayEnabled));
        }

        [Test]
        public void PausePosture_PreventsGameplayRuntimeRepairUntilResume()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext pauseContext = CreateBoundContext(application);
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            PlayerSlotId slot = PlayerSlotId.Player1;
            PlayerGameplayInputBindingToken bindingToken = CreateGameplayInputBinding(
                slot,
                _playerInput,
                adapter,
                out PlayerGameplayInputBindingRuntimeContext inputContext);

            PauseProductRequestResult pause = pauseContext.RequestPause(
                PauseRequest.Pause("repair-guard.pause", "test", "pause"));

            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.True);
            Assert.That(_playerInput.currentActionMap, Is.SameAs(_globalMap));
            Assert.That(_gameplayMap.enabled, Is.False);
            Assert.That(inputContext.TryReassertUngatedContractedGameplayMap(
                slot, bindingToken, "test", "simulate-player-late-update"), Is.False);
            Assert.That(_playerInput.currentActionMap, Is.SameAs(_globalMap));
            Assert.That(_gameplayMap.enabled, Is.False);

            PauseProductRequestResult resume = pauseContext.RequestPause(
                PauseRequest.Resume("repair-guard.resume", "test", "resume"));

            Assert.That(resume.Succeeded, Is.True, resume.Diagnostic);
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(_gameplayMap.enabled, Is.True);

            Assert.That(TryApplyGlobalOnly(adapter), Is.True);
            Assert.That(inputContext.TryReassertUngatedContractedGameplayMap(
                slot, bindingToken, "test", "simulate-post-resume-repair"), Is.True);
            Assert.That(_gameplayMap.enabled, Is.True);
        }

        [Test]
        public void PauseCommitFailure_RestoresPriorPostureAndClearsSuppression()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            Assert.That(adapter.TryCapturePosture(out PausePlayerInputPostureReceipt priorPosture, out string captureDiagnostic),
                Is.True, captureDiagnostic);
            InputActionMap priorCurrentActionMap = _playerInput.currentActionMap;
            string priorDefaultActionMap = _playerInput.defaultActionMap;
            application.OnApply = () => SetField(context, "_activeTransaction", default(PauseProductTransaction));

            PauseProductRequestResult result = context.RequestPause(
                PauseRequest.Pause("pause-commit-failure", "test", "invalidate-transaction-before-commit"));

            Assert.That(result.Status, Is.EqualTo(PauseProductRequestStatus.Failed));
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(_playerInput.currentActionMap, Is.SameAs(priorCurrentActionMap));
            Assert.That(UnityPlayerInputStateWriter.HasExactEnabledActionMapSet(
                _playerInput, priorPosture.EnabledActionMapNames), Is.True);
            Assert.That(_playerInput.defaultActionMap, Is.EqualTo(priorDefaultActionMap));
            Assert.That(_playerInput.inputIsActive, Is.EqualTo(priorPosture.InputWasActive));
            Assert.That(application.CurrentSnapshot.State, Is.EqualTo(PauseState.Running));
        }

        [Test]
        public void ResumeCommitFailure_RestoresPausedPostureAndRetainsSuppression()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            Assert.That(context.RequestPause(
                PauseRequest.Pause("resume-commit-failure.pause", "test", "pause-first")).Succeeded, Is.True);
            application.OnApply = () => SetField(context, "_activeTransaction", default(PauseProductTransaction));

            PauseProductRequestResult result = context.RequestPause(
                PauseRequest.Resume("resume-commit-failure", "test", "invalidate-transaction-before-commit"));

            Assert.That(result.Status, Is.EqualTo(PauseProductRequestStatus.Failed));
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.True);
            Assert.That(_gameplayMap.enabled, Is.False);
            Assert.That(application.CurrentSnapshot.State, Is.EqualTo(PauseState.Paused));
        }

        [Test]
        public void ActivityExit_RestoresPausePostureWithoutReleasingHostBinding()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            PauseProductBindingToken token = context.ActiveToken;
            PauseProductRequestResult pause = context.RequestPause(
                PauseRequest.Pause("activity-exit.pause", "test", "pause"));
            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.True);

            Assert.That(application.TryRestorePauseSnapshot(RunningSnapshot(), "activity-exit", out _), Is.True);
            Assert.That(context.TryRestorePhysicalPostureForLifecycleExit("activity-exit", out PlayerInputPostureRestoreOutcome outcome, out string diagnostic), Is.True, diagnostic);
            Assert.That(outcome, Is.EqualTo(PlayerInputPostureRestoreOutcome.Restored));
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(context.HasActivePlayerInputBinding, Is.True);
            Assert.That(_globalMap.enabled, Is.True);
            Assert.That(_gameplayMap.enabled, Is.True);
            Assert.That(context.ReleaseBinding(token, "test-release", out _, out diagnostic), Is.True, diagnostic);
            Assert.That(_globalMap.enabled, Is.EqualTo(_initialGlobalEnabled));
            Assert.That(_gameplayMap.enabled, Is.EqualTo(_initialGameplayEnabled));
        }

        [Test]
        public void ActivityExit_RestoreFailureRetainsSuppressionForRetry()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            PauseProductRequestResult pause = context.RequestPause(
                PauseRequest.Pause("activity-exit-retry.pause", "test", "pause"));
            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            UnityPlayerInputActionMapSetWriteReceipt validReceipt =
                GetField<UnityPlayerInputActionMapSetWriteReceipt>(context, "_pauseActionMapSetReceipt");
            var invalidReceipt = new UnityPlayerInputActionMapSetWriteReceipt(
                validReceipt.PlayerInputEntityId,
                "MissingMap",
                new[] { "MissingMap" },
                "MissingMap",
                new[] { "MissingMap" });
            SetField(context, "_pauseActionMapSetReceipt", invalidReceipt);
            Assert.That(application.TryRestorePauseSnapshot(RunningSnapshot(), "activity-exit", out _), Is.True);

            bool firstRestore = context.TryRestorePhysicalPostureForLifecycleExit(
                "activity-exit-failure", out PlayerInputPostureRestoreOutcome firstOutcome, out _);

            Assert.That(firstRestore, Is.False);
            Assert.That(firstOutcome, Is.EqualTo(PlayerInputPostureRestoreOutcome.FailedRecoverable));
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.True);

            SetField(context, "_pauseActionMapSetReceipt", validReceipt);
            Assert.That(context.TryRestorePhysicalPostureForLifecycleExit(
                "activity-exit-retry", out PlayerInputPostureRestoreOutcome retryOutcome, out string retryDiagnostic),
                Is.True, retryDiagnostic);
            Assert.That(retryOutcome, Is.EqualTo(PlayerInputPostureRestoreOutcome.Restored));
            Assert.That(adapter.IsContractedGameplayRestoreSuppressed, Is.False);
        }

        [Test]
        public void GateAdapter_AlreadyBlocked_ReappliesWhenGameplayMapWasPhysicallyReenabled()
        {
            CreateBoundContext(new FakeApplication(RunningSnapshot()));
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            MethodInfo applyBlock = typeof(UnityPlayerInputGateAdapter).GetMethod(
                "ApplyBlock", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyBlock, Is.Not.Null);

            applyBlock.Invoke(adapter, new object[] { "test-first-block", false, true });
            Assert.That(_gameplayMap.enabled, Is.False);
            Assert.That(adapter.IsBlockedByAdapter, Is.True);

            _gameplayMap.Enable();
            Assert.That(adapter.IsBlockedByAdapter, Is.False);
            applyBlock.Invoke(adapter, new object[] { "test-reapply-block", false, true });

            Assert.That(_gameplayMap.enabled, Is.False);
            Assert.That(adapter.LastStatus, Is.Not.EqualTo("AlreadyBlocked"));
        }

        [Test]
        public void GateAdapter_AutomaticUpdateRetriesAreBoundedAndExplicitApplyRestartsThem()
        {
            CreateBoundContext(new FakeApplication(RunningSnapshot()));
            UnityPlayerInputGateAdapter adapter = _host.GetComponent<UnityPlayerInputGateAdapter>();
            MethodInfo update = typeof(UnityPlayerInputGateAdapter).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);

            update.Invoke(adapter, null);
            update.Invoke(adapter, null);
            Assert.That(adapter.LastStatus, Is.EqualTo("SkippedMissingInputGateRuntime"));

            update.Invoke(adapter, null);
            Assert.That(adapter.LastStatus, Is.EqualTo("RetryLimitReached"));

            adapter.ApplyCurrentGate();
            Assert.That(adapter.LastStatus, Is.EqualTo("SkippedMissingInputGateRuntime"));
        }

        [Test]
        public void ReleaseBinding_RestorationFailureRetainsTokenAndReceiptForRetry()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            PauseProductBindingToken token = context.ActiveToken;
            PauseProductRequestResult pause = context.RequestPause(
                PauseRequest.Pause("release-retry.pause", "test", "pause-before-release"));
            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            var invalidButOwnedPosture = new PausePlayerInputPostureReceipt(
                _initialPosture.PlayerInputEntityId,
                "MissingMap",
                Array.Empty<string>(),
                _initialPosture.DefaultActionMapName,
                _initialPosture.InputWasActive);
            SetField(context, "_bindingPosture", invalidButOwnedPosture);

            bool firstRelease = context.ReleaseBinding(
                token,
                "test-release-failure",
                out PauseBindingReleaseOutcome firstOutcome,
                out string firstDiagnostic);

            Assert.That(firstRelease, Is.False, firstDiagnostic);
            Assert.That(firstOutcome, Is.EqualTo(PauseBindingReleaseOutcome.FailedRecoverable));
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.True);
            Assert.That(context.ActiveToken, Is.EqualTo(token));
            Assert.That(context.HasActivePlayerInputBinding, Is.True);
            Assert.That(application.CurrentSnapshot.State, Is.EqualTo(PauseState.Paused));
            Assert.That(GetField<UnityPlayerInputActionMapSetWriteReceipt>(context, "_pauseActionMapSetReceipt").IsValid, Is.True);
            PausePlayerInputPostureReceipt retainedPosture =
                GetField<PausePlayerInputPostureReceipt>(context, "_bindingPosture");
            Assert.That(retainedPosture.IsValid, Is.True);
            Assert.That(retainedPosture.CurrentActionMapName, Is.EqualTo("MissingMap"));

            SetField(context, "_bindingPosture", _initialPosture);
            bool retry = context.ReleaseBinding(
                token,
                "test-release-retry",
                out PauseBindingReleaseOutcome retryOutcome,
                out string retryDiagnostic);

            Assert.That(retry, Is.True, retryDiagnostic);
            Assert.That(retryOutcome, Is.EqualTo(PauseBindingReleaseOutcome.Restored));
            Assert.That(context.ActiveToken.IsValid, Is.False);
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(_globalMap.enabled, Is.EqualTo(_initialGlobalEnabled));
            Assert.That(_gameplayMap.enabled, Is.EqualTo(_initialGameplayEnabled));

            Assert.That(context.TryRegister(_binding, out PauseProductBindingToken replacement, out retryDiagnostic), Is.True, retryDiagnostic);
            Assert.That(replacement.IsValid, Is.True);
            Assert.That(replacement, Is.Not.EqualTo(token));
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(context.ReleaseBinding(replacement, "replacement-release", out _, out retryDiagnostic), Is.True, retryDiagnostic);
        }

        [Test]
        public void ReleaseBinding_DestroyedExactPlayerInputIsTerminalAndNotReportedAsRestored()
        {
            var application = new FakeApplication(RunningSnapshot())
            {
                ReturnInvalidResult = false
            };
            PauseProductBindingRuntimeContext context = CreateBoundContext(application);
            PauseProductBindingToken token = context.ActiveToken;
            PauseProductRequestResult pause = context.RequestPause(
                PauseRequest.Pause("destroyed-paused-binding.pause", "test", "pause-before-target-destroyed"));
            Assert.That(pause.Succeeded, Is.True, pause.Diagnostic);
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.True);
            UnityEngine.Object.DestroyImmediate(_playerInput);

            bool released = context.ReleaseBinding(
                token,
                "test-target-destroyed",
                out PauseBindingReleaseOutcome outcome,
                out string diagnostic);

            Assert.That(released, Is.True, diagnostic);
            Assert.That(outcome, Is.EqualTo(PauseBindingReleaseOutcome.TargetDestroyed));
            Assert.That(context.ActiveToken.IsValid, Is.False);
            Assert.That(_host.GetComponent<UnityPlayerInputGateAdapter>().IsContractedGameplayRestoreSuppressed, Is.False);
            Assert.That(diagnostic, Does.Contain("destroyed"));
        }

        private PlayerGameplayInputBindingToken CreateGameplayInputBinding(
            PlayerSlotId slot,
            PlayerInput playerInput,
            UnityPlayerInputGateAdapter adapter,
            out PlayerGameplayInputBindingRuntimeContext context)
        {
            const string sessionId = "pause-repair-test-session";
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                "pause-repair-test-activity",
                "Pause repair test Activity",
                RuntimeDefinitionToken.MintAnonymous());
            RuntimeContentIdentity runtimeContent = RuntimeContentIdentity.From(
                owner,
                "pause-repair-test-content");
            ActorProfileId profileId = ActorProfileId.From("pause-repair-test-profile");
            ActorId actorId = ActorId.From("pause-repair-test-actor");
            var preparation = new PlayerActorPreparationToken(
                sessionId, slot, profileId, 1, actorId, runtimeContent, 1, 1);
            var occupancy = new PlayerGameplayOccupancyToken(
                sessionId, owner, slot, profileId, actorId, preparation, runtimeContent, 1, 1);
            var hostIdentity = new PlayerHostBindingIdentity(sessionId, 1);
            var assignment = new PlayerSlotAssignmentToken(sessionId, slot, 1, 1, hostIdentity);
            var token = new PlayerGameplayInputBindingToken(
                sessionId, owner, slot, assignment, hostIdentity, preparation, 1);
            var summary = new PlayerGameplayInputBindingSummary(
                sessionId,
                slot,
                PlayerGameplayInputBindingState.Bound,
                PlayerGameplayInputAvailability.Allowed,
                assignment,
                hostIdentity,
                profileId,
                actorId,
                owner,
                runtimeContent,
                preparation,
                occupancy,
                token,
                "Player",
                "Player",
                "Global",
                playerInput.name,
                1,
                1,
                "test",
                "pause-repair-test",
                "Synthetic current input-binding evidence for the internal repair guard test.");
            Assert.That(summary.IsValid, Is.True, summary.ToDiagnosticString());

            Type contextType = typeof(PlayerGameplayInputBindingRuntimeContext);
            context = (PlayerGameplayInputBindingRuntimeContext)Activator.CreateInstance(
                contextType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { sessionId, null, null, new[] { slot } },
                null);
            var slots = (IDictionary)contextType.GetField(
                "_slots",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
            var records = (IDictionary)contextType.GetField(
                "_records",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
            slots[slot] = summary;

            Type recordType = contextType.GetNestedType("BindingRecord", BindingFlags.NonPublic);
            object record = Activator.CreateInstance(recordType, nonPublic: true);
            SetField(record, "playerInput", playerInput);
            SetField(record, "gateAdapter", adapter);
            records.Add(slot, record);
            return token;
        }

        private static bool TryApplyGlobalOnly(UnityPlayerInputGateAdapter adapter) =>
            adapter.TryApplyActionMapSet(
                adapter.PlayerInput,
                "Global",
                new[] { "Global" },
                "test",
                "simulate-pause-action-map-posture",
                out _,
                out _);

        [Test]
        public void PlayerPauseInput_RetainsReleaseAuthorityUntilRetryRestoresOrTargetEnds()
        {
            var host = new GameObject("Pause Binding Release Retry Test Host");
            try
            {
                PlayerPauseInput binding = host.AddComponent<PlayerPauseInput>();
                var port = new FakeReleasePort();
                Assert.That(binding.TryInjectBindingPort(port, out string bindDiagnostic), Is.True, bindDiagnostic);

                Assert.That(binding.TryReleaseBinding("first-release", out string firstDiagnostic), Is.False, firstDiagnostic);
                Assert.That(binding.HasActiveBinding, Is.True);
                Assert.That(binding.BindingStatus, Is.EqualTo("ReleasePending"));

                Assert.That(binding.TryReleaseBinding("retry-release", out string retryDiagnostic), Is.True, retryDiagnostic);
                Assert.That(binding.HasActiveBinding, Is.False);
                Assert.That(binding.LastReleaseOutcome, Is.EqualTo(PauseBindingReleaseOutcome.Restored));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private GameObject _host;
        private PlayerInput _playerInput;
        private InputActionAsset _actions;
        private InputActionMap _globalMap;
        private InputActionMap _gameplayMap;
        private PlayerPauseInput _binding;
        private bool _initialGlobalEnabled;
        private bool _initialGameplayEnabled;
        private PausePlayerInputPostureReceipt _initialPosture;

        private PauseProductBindingRuntimeContext CreateBoundContext(FakeApplication application)
        {
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            _globalMap = new InputActionMap("Global");
            _gameplayMap = new InputActionMap("Player");
            _actions.AddActionMap(_globalMap);
            _actions.AddActionMap(_gameplayMap);
            InputAction pauseAction = _globalMap.AddAction("Pause", InputActionType.Button);
            _gameplayMap.AddAction("Move", InputActionType.Value);

            _host = new GameObject("Pause Product Transaction Test Host");
            _playerInput = _host.AddComponent<PlayerInput>();
            _playerInput.actions = _actions;
            _initialGlobalEnabled = _globalMap.enabled;
            _initialGameplayEnabled = _gameplayMap.enabled;
            UnityPlayerInputGateAdapter adapter = _host.AddComponent<UnityPlayerInputGateAdapter>();
            SetField(adapter, "playerInput", _playerInput);
            SetField(adapter, "gameplayActionMap", PlayerInputActionMapReference.From(_gameplayMap));
            Assert.That(adapter.TryCapturePosture(out _initialPosture, out string captureIssue), Is.True, captureIssue);
            _binding = _host.AddComponent<PlayerPauseInput>();
            SetField(_binding, "pauseAction", InputActionReference.Create(pauseAction));

            var context = new PauseProductBindingRuntimeContext(application);
            Assert.That(context.TryRegister(_binding, out _, out string diagnostic), Is.True, diagnostic);
            return context;
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                UnityEngine.Object.DestroyImmediate(_host);
            if (_actions != null)
                UnityEngine.Object.DestroyImmediate(_actions);
            _binding = null;
            _globalMap = null;
            _gameplayMap = null;
        }

        private static PauseSnapshot RunningSnapshot() => PauseSnapshot.FromState(
            PauseState.Running,
            "test",
            "before-pause",
            Array.Empty<string>());

        private static void SetField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{name}' was not found.");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{name}' was not found.");
            return (T)field.GetValue(target);
        }

        private sealed class FakeApplication : IPauseProductApplicationPort
        {
            private PauseSnapshot _snapshot;

            internal FakeApplication(PauseSnapshot snapshot) => _snapshot = snapshot;

            internal int ApplyCount { get; private set; }
            internal PauseSnapshot CurrentSnapshot => _snapshot;
            internal List<PauseSnapshot> RestoredSnapshots { get; } = new List<PauseSnapshot>();
            internal System.Action OnApply { get; set; }
            internal bool ReturnInvalidResult { get; set; } = true;

            public bool TryApplyProductPause(
                PauseRequest request,
                out PauseResult result,
                out string diagnostic)
            {
                ApplyCount++;
                OnApply?.Invoke();
                if (ReturnInvalidResult)
                {
                    result = default;
                    diagnostic = "Injected invalid Pause result.";
                    return true;
                }

                PauseState next = PauseRequest.ResolveTargetState(request.Kind, _snapshot.State);
                result = PauseResult.AppliedResult(request, _snapshot.State, next, "Injected Pause result.");
                _snapshot = PauseSnapshot.FromResult(result, Array.Empty<string>());
                diagnostic = string.Empty;
                return true;
            }

            public bool TryRestorePauseSnapshot(
                PauseSnapshot snapshot,
                string reason,
                out string diagnostic)
            {
                RestoredSnapshots.Add(snapshot);
                _snapshot = snapshot;
                diagnostic = string.Empty;
                return true;
            }

            public bool TryGetApplicationPauseSnapshot(out PauseSnapshot snapshot)
            {
                snapshot = _snapshot;
                return true;
            }
        }

        private sealed class FakeReleasePort : IPauseProductBindingPort
        {
            private int _releaseAttempts;

            public bool TryRegister(
                PlayerPauseInput binding,
                out PauseProductBindingToken token,
                out string diagnostic)
            {
                token = new PauseProductBindingToken(1, 7);
                diagnostic = "Injected binding token.";
                return true;
            }

            public bool ReleaseBinding(
                PauseProductBindingToken token,
                string reason,
                out PauseBindingReleaseOutcome outcome,
                out string diagnostic)
            {
                _releaseAttempts++;
                if (_releaseAttempts == 1)
                {
                    outcome = PauseBindingReleaseOutcome.FailedRecoverable;
                    diagnostic = "Injected recoverable physical restore failure.";
                    return false;
                }

                outcome = PauseBindingReleaseOutcome.Restored;
                diagnostic = "Injected exact physical restore confirmation.";
                return true;
            }
        }
    }
}
