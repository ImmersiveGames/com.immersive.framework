using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerSessionJoinTriggerTests
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private readonly List<InputDevice> _devices = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _devices.Count - 1; index >= 0; index--)
            {
                if (_devices[index] != null && _devices[index].added)
                    InputSystem.RemoveDevice(_devices[index]);
            }

            _devices.Clear();
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
            }

            _objects.Clear();
        }

        [Test]
        public void Trigger_IsStandaloneAndDoesNotExposeTheExperimentalConsumerSurface()
        {
            Assert.That(
                typeof(PlayerSessionScopedAccessConsumer).IsAssignableFrom(typeof(PlayerSessionJoinTrigger)),
                Is.False);
            Assert.That(typeof(PlayerSessionJoinTrigger).IsSealed, Is.True);
        }

        [Test]
        public void JoinTriggerRecipient_UsesTheExactStandaloneComponentAndAuthoredScope()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Activity);

            PlayerSessionScopedBindingRecipient recipient =
                PlayerSessionScopedBindingRecipient.ForJoinTrigger(trigger);

            Assert.That(recipient.Component, Is.SameAs(trigger));
            Assert.That(recipient.Scope, Is.EqualTo(LocalPlayerProvisioningConsumerScope.Activity));
            Assert.That(recipient.BindingKind, Is.EqualTo(typeof(PlayerSessionJoinTrigger)));
            Assert.That(recipient.TryValidateScope(out string issue), Is.True, issue);
        }

        [Test]
        public void JoinTriggerRecipient_RejectsAuthoredScopeChangesAfterDiscovery()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            PlayerSessionScopedBindingRecipient recipient =
                PlayerSessionScopedBindingRecipient.ForJoinTrigger(trigger);
            var serialized = new SerializedObject(trigger);
            serialized.FindProperty("scope").intValue = (int)LocalPlayerProvisioningConsumerScope.Activity;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(recipient.TryValidateScope(out string issue), Is.False);
            Assert.That(issue, Does.Contain("changed after binding discovery"));
        }

        [Test]
        public void RequestJoin_DelegatesOnceAndPreservesRequestAndOutcomeIdentity()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            InputDevice device = InputSystem.AddDevice<Gamepad>();
            _devices.Add(device);
            var request = new LocalPlayerJoinRequest("test-source", "test-reason", device, "Gamepad");
            LocalPlayerJoinOutcome expected = Outcome(
                LocalPlayerJoinOutcomeCode.Joined,
                LocalPlayerJoinAdmissionState.Admitted);
            var port = new TestJoinPort(_ => expected);
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            LocalPlayerJoinOutcome actual = trigger.RequestJoin(request);

            Assert.That(actual, Is.SameAs(expected));
            Assert.That(trigger.LastOutcome, Is.SameAs(actual));
            Assert.That(port.RequestCount, Is.EqualTo(1));
            Assert.That(port.LastRequest.Source, Is.EqualTo(request.Source));
            Assert.That(port.LastRequest.Reason, Is.EqualTo(request.Reason));
            Assert.That(port.LastRequest.PairWithDevice, Is.SameAs(device));
            Assert.That(port.LastRequest.ControlScheme, Is.EqualTo(request.ControlScheme));
        }

        [Test]
        public void RequestJoin_WithoutBindingReturnsUnavailable()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest("test-source", "test-reason"));

            AssertOutcome(outcome, LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinAdmissionState.NotAdmitted);
            Assert.That(trigger.LastOutcome, Is.SameAs(outcome));
        }

        [Test]
        public void RequestJoin_WithUnspecifiedScopeReturnsConfigurationRejection()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Unspecified);

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest("test-source", "test-reason"));

            AssertOutcome(outcome, LocalPlayerJoinOutcomeCode.RejectedConfiguration,
                LocalPlayerJoinAdmissionState.NotAdmitted);
        }

        [Test]
        public void TryBindJoinPort_WithDifferentActiveScopeRejectsBinding()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.Joined,
                LocalPlayerJoinAdmissionState.Admitted));

            Assert.That(trigger.TryBindJoinPort(
                port, LocalPlayerProvisioningConsumerScope.Activity, out string issue), Is.False);
            Assert.That(issue, Does.Contain("does not match"));
            AssertOutcome(trigger.RequestJoin(
                    new LocalPlayerJoinRequest("test-source", "test-reason")),
                LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinAdmissionState.NotAdmitted);
        }

        [Test]
        public void RequestJoin_WithInvalidRequestRejectsBeforeDelegation()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.Joined,
                LocalPlayerJoinAdmissionState.Admitted));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(default);

            AssertOutcome(outcome, LocalPlayerJoinOutcomeCode.RejectedInvalidRequest,
                LocalPlayerJoinAdmissionState.NotAdmitted);
            Assert.That(port.RequestCount, Is.Zero);
        }

        [Test]
        public void Invoke_UsesAuthoredReasonAndControlScheme()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(
                LocalPlayerProvisioningConsumerScope.Activity,
                reason: " authored reason ",
                controlScheme: "Gamepad");
            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.RejectedJoiningClosed,
                LocalPlayerJoinAdmissionState.NotAdmitted));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Activity, out _), Is.True);

            trigger.Invoke();

            Assert.That(port.RequestCount, Is.EqualTo(1));
            Assert.That(port.LastRequest.Source, Is.EqualTo(nameof(PlayerSessionJoinTrigger)));
            Assert.That(port.LastRequest.Reason, Is.EqualTo("authored reason"));
            Assert.That(port.LastRequest.ControlScheme, Is.EqualTo("Gamepad"));
            AssertOutcome(trigger.LastOutcome, LocalPlayerJoinOutcomeCode.RejectedJoiningClosed,
                LocalPlayerJoinAdmissionState.NotAdmitted);
        }

        [Test]
        public void Invoke_UsesJoinAsReasonWhenNoReasonWasAuthored()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinAdmissionState.NotAdmitted));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            trigger.Invoke();

            Assert.That(port.LastRequest.Reason, Is.EqualTo("Join"));
        }

        [Test]
        public void InvokeFromDevice_PreservesExactInputDeviceReference()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(
                LocalPlayerProvisioningConsumerScope.Route,
                controlScheme: "Gamepad");
            InputDevice device = InputSystem.AddDevice<Gamepad>();
            _devices.Add(device);
            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinAdmissionState.NotAdmitted));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            trigger.InvokeFromDevice(device);

            Assert.That(port.RequestCount, Is.EqualTo(1));
            Assert.That(port.LastRequest.PairWithDevice, Is.SameAs(device));
            Assert.That(port.LastRequest.ControlScheme, Is.EqualTo("Gamepad"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvokeFromDevice_WithNullOrRemovedDeviceRejectsWithoutDelegation(bool removeDevice)
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            InputDevice device = removeDevice ? InputSystem.AddDevice<Gamepad>() : null;
            if (device != null)
            {
                _devices.Add(device);
                InputSystem.RemoveDevice(device);
            }

            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.Joined,
                LocalPlayerJoinAdmissionState.Admitted));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            trigger.InvokeFromDevice(device);

            AssertOutcome(trigger.LastOutcome, LocalPlayerJoinOutcomeCode.RejectedInvalidRequest,
                LocalPlayerJoinAdmissionState.NotAdmitted);
            Assert.That(port.RequestCount, Is.Zero);
        }

        [Test]
        public void RequestJoin_WhenPortReturnsStaleLeaseRejectionPreservesIt()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Activity);
            LocalPlayerJoinOutcome stale = Outcome(
                LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinAdmissionState.NotAdmitted);
            var port = new TestJoinPort(_ => stale);
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Activity, out _), Is.True);

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest("test-source", "test-reason"));

            Assert.That(outcome, Is.SameAs(stale));
            Assert.That(trigger.LastOutcome, Is.SameAs(stale));
        }

        [Test]
        public void RequestJoin_PreservesOriginalCodeForFailedRollback()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            LocalPlayerJoinOutcome failedRollback = Outcome(
                LocalPlayerJoinOutcomeCode.FailedRollback,
                LocalPlayerJoinAdmissionState.Unknown,
                LocalPlayerJoinOutcomeCode.FailedAdmission);
            var port = new TestJoinPort(_ => failedRollback);
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest("test-source", "test-reason"));

            Assert.That(outcome, Is.SameAs(failedRollback));
            Assert.That(outcome.Code, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedRollback));
            Assert.That(outcome.OriginalCode, Is.EqualTo(LocalPlayerJoinOutcomeCode.FailedAdmission));
            Assert.That(outcome.AdmissionState, Is.EqualTo(LocalPlayerJoinAdmissionState.Unknown));
        }

        [Test]
        public void ReleaseBinding_IsIdempotentAndSubsequentRequestIsUnavailable()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            var port = new TestJoinPort(_ => Outcome(
                LocalPlayerJoinOutcomeCode.Joined,
                LocalPlayerJoinAdmissionState.Admitted));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);

            trigger.ReleaseJoinPort("test release", true);
            trigger.ReleaseJoinPort("duplicate release", true);
            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest("test-source", "test-reason"));

            Assert.That(port.RequestCount, Is.Zero);
            AssertOutcome(outcome, LocalPlayerJoinOutcomeCode.RejectedUnavailable,
                LocalPlayerJoinAdmissionState.NotAdmitted);
        }

        [Test]
        public void RequestJoin_WhenPortThrowsReturnsUnknownAndRetainsDiagnosticInLog()
        {
            PlayerSessionJoinTrigger trigger = CreateTrigger(LocalPlayerProvisioningConsumerScope.Route);
            var port = new TestJoinPort(_ => throw new InvalidOperationException("injected join failure"));
            Assert.That(trigger.TryBindJoinPort(port, LocalPlayerProvisioningConsumerScope.Route, out _), Is.True);
            LogAssert.Expect(LogType.Exception, new Regex("injected join failure"));

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest("test-source", "test-reason"));

            AssertOutcome(outcome, LocalPlayerJoinOutcomeCode.FailedUnknown,
                LocalPlayerJoinAdmissionState.Unknown);
            Assert.That(port.RequestCount, Is.EqualTo(1));
            Assert.That(trigger.LastOutcome, Is.SameAs(outcome));
        }

        private PlayerSessionJoinTrigger CreateTrigger(
            LocalPlayerProvisioningConsumerScope scope,
            string reason = "",
            string controlScheme = "")
        {
            var gameObject = new GameObject("Player Session Join Trigger Test");
            _objects.Add(gameObject);
            PlayerSessionJoinTrigger trigger = gameObject.AddComponent<PlayerSessionJoinTrigger>();
            var serialized = new SerializedObject(trigger);
            serialized.FindProperty("scope").intValue = (int)scope;
            serialized.FindProperty("reason").stringValue = reason;
            serialized.FindProperty("controlScheme").stringValue = controlScheme;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
        }

        private static LocalPlayerJoinOutcome Outcome(
            LocalPlayerJoinOutcomeCode code,
            LocalPlayerJoinAdmissionState admissionState,
            LocalPlayerJoinOutcomeCode? originalCode = null) =>
            new(code, originalCode, admissionState);

        private static void AssertOutcome(
            LocalPlayerJoinOutcome outcome,
            LocalPlayerJoinOutcomeCode code,
            LocalPlayerJoinAdmissionState admissionState)
        {
            Assert.That(outcome, Is.Not.Null);
            Assert.That(outcome.Code, Is.EqualTo(code));
            Assert.That(outcome.AdmissionState, Is.EqualTo(admissionState));
        }

        private sealed class TestJoinPort : IPlayerSessionJoinPort
        {
            private readonly Func<LocalPlayerJoinRequest, LocalPlayerJoinOutcome> _request;

            internal TestJoinPort(Func<LocalPlayerJoinRequest, LocalPlayerJoinOutcome> request)
            {
                _request = request;
            }

            internal int RequestCount { get; private set; }
            internal LocalPlayerJoinRequest LastRequest { get; private set; }

            public LocalPlayerJoinOutcome RequestJoin(LocalPlayerJoinRequest request)
            {
                RequestCount++;
                LastRequest = request;
                return _request(request);
            }

            public void Dispose()
            {
            }
        }
    }
}
