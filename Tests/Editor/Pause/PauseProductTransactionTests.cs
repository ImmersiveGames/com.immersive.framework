using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
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
            UnityEngine.Object.DestroyImmediate(_playerInput);

            PauseProductRequestResult resume = context.RequestPause(
                PauseRequest.Resume("resume-failure.resume", "test", "destroyed-player-input"));

            Assert.That(resume.Status, Is.EqualTo(PauseProductRequestStatus.Failed));
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

            Assert.That(context.ReleaseBinding(token, "test-release", out string diagnostic), Is.True, diagnostic);
            Assert.That(_globalMap.enabled, Is.EqualTo(_initialGlobalEnabled));
            Assert.That(_gameplayMap.enabled, Is.EqualTo(_initialGameplayEnabled));
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

            Assert.That(application.TryRestorePauseSnapshot(RunningSnapshot(), "activity-exit", out _), Is.True);
            Assert.That(context.TryRestorePhysicalPostureForLifecycleExit("activity-exit", out string diagnostic), Is.True, diagnostic);
            Assert.That(context.HasActivePlayerInputBinding, Is.True);
            Assert.That(_globalMap.enabled, Is.True);
            Assert.That(_gameplayMap.enabled, Is.True);
            Assert.That(context.ReleaseBinding(token, "test-release", out diagnostic), Is.True, diagnostic);
            Assert.That(_globalMap.enabled, Is.EqualTo(_initialGlobalEnabled));
            Assert.That(_gameplayMap.enabled, Is.EqualTo(_initialGameplayEnabled));
        }

        private GameObject _host;
        private PlayerInput _playerInput;
        private InputActionAsset _actions;
        private InputActionMap _globalMap;
        private InputActionMap _gameplayMap;
        private PlayerPauseInput _binding;
        private bool _initialGlobalEnabled;
        private bool _initialGameplayEnabled;

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

        private sealed class FakeApplication : IPauseProductApplicationPort
        {
            private PauseSnapshot _snapshot;

            internal FakeApplication(PauseSnapshot snapshot) => _snapshot = snapshot;

            internal int ApplyCount { get; private set; }
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
    }
}
