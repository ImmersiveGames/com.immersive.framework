using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause.Tests
{
    public sealed class PauseActivityBindingRuntimeContextTests
    {
        private readonly List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void AbsentIntent_SkipsWithoutTouchingPortOrHosts()
        {
            var context = new PauseActivityBindingRuntimeContext();
            var port = new FakePauseProductBindingPort();

            bool accepted = context.TryActivate(
                CreateScope("activity.absent", 1),
                PauseActivityBindingIntentResolution.Absent("test.absent"),
                null,
                port,
                "test",
                "absent",
                out PauseActivityBindingOperationResult result);

            Assert.That(accepted, Is.True);
            Assert.That(result.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Absent));
            Assert.That(result.Diagnostic, Does.Contain("intent-absent"));
            Assert.That(port.RegisterCalls, Is.Zero);
            Assert.That(context.State, Is.EqualTo(PauseActivityBindingRuntimeState.Inactive));
        }

        [Test]
        public void LifecycleModule_AbsentIntent_DoesNotRequirePlayerEvidence()
        {
            GameObject root = CreateObject("Activity Root");
            PauseActivityBindingScope scope = CreateScope("activity.lifecycle.absent", 7);
            var module = new PauseActivityBindingRuntimeHostModule(
                new PauseActivityBindingRuntimeContext(),
                new FakePauseProductBindingPort());

            Assert.That(module.TryPrepareIntent(
                scope.Owner,
                scope.ActivityEntrySequence,
                new[] { root },
                "test",
                "intent-absent",
                out string preparationDiagnostic), Is.True);
            bool accepted = module.TryActivate(
                null,
                scope.Owner,
                scope.ActivityEntrySequence,
                "test",
                "intent-absent",
                out string diagnostic);

            Assert.That(accepted, Is.True);
            Assert.That(preparationDiagnostic, Does.Contain("intent-absent"));
            Assert.That(diagnostic, Does.Contain("intent-absent"));
            Assert.That(module.Snapshot.HasActiveBinding, Is.False);
        }

        [Test]
        public void LifecycleModule_RequiredIntentWithoutOfficialEvidence_BlocksBeforeRunning()
        {
            GameObject root = CreateObject("Activity Root");
            root.AddComponent<PauseActivityBindingAuthoring>();
            PauseActivityBindingScope scope = CreateScope("activity.lifecycle.required", 8);
            var module = new PauseActivityBindingRuntimeHostModule(
                new PauseActivityBindingRuntimeContext(),
                new FakePauseProductBindingPort());

            Assert.That(module.TryPrepareIntent(
                scope.Owner,
                scope.ActivityEntrySequence,
                new[] { root },
                "test",
                "waiting-for-player-admission",
                out _), Is.True);
            bool accepted = module.TryActivate(
                null,
                scope.Owner,
                scope.ActivityEntrySequence,
                "test",
                "waiting-for-player-admission",
                out string diagnostic);

            Assert.That(accepted, Is.False);
            Assert.That(diagnostic, Does.Contain("waiting-for-player-admission"));
            Assert.That(module.Snapshot.HasActiveBinding, Is.False);
        }

        [Test]
        public void LifecycleModule_ReleaseKeepsForeignScopeBlockedAndRetriesExactScope()
        {
            LocalPlayerHostAuthoring host = CreateJoinedHost("Lifecycle Release", out _);
            PauseActivityBindingScope scope = CreateScope("activity.lifecycle.release", 9);
            var context = new PauseActivityBindingRuntimeContext();
            var port = new FakePauseProductBindingPort { ReleaseSucceeds = false };
            Assert.That(
                context.TryActivate(
                    scope,
                    RequiredIntent(),
                    new[] { host },
                    port,
                    "test",
                    "activate",
                    out _),
                Is.True);
            var module = new PauseActivityBindingRuntimeHostModule(context, port);

            Assert.That(
                module.TryReleaseForOwner(
                    CreateScope("activity.lifecycle.foreign", 9).Owner,
                    "test",
                    "foreign",
                    out string foreignDiagnostic),
                Is.False);
            Assert.That(foreignDiagnostic, Does.Contain("binding-release-failed"));
            Assert.That(context.Snapshot.HasActiveBinding, Is.True);

            Assert.That(
                module.TryReleaseForOwner(scope.Owner, "test", "fail", out string failedDiagnostic),
                Is.False);
            Assert.That(failedDiagnostic, Does.Contain("binding-release-failed"));
            Assert.That(context.Snapshot.HasActiveBinding, Is.True);

            port.ReleaseSucceeds = true;
            Assert.That(
                module.TryReleaseForOwner(scope.Owner, "test", "retry", out string releasedDiagnostic),
                Is.True);
            Assert.That(releasedDiagnostic, Does.Contain("binding-released"));
            Assert.That(context.Snapshot.HasActiveBinding, Is.False);
        }

        [Test]
        public void InvalidIntent_FailsBeforeHostResolution()
        {
            var context = new PauseActivityBindingRuntimeContext();

            bool accepted = context.TryActivate(
                CreateScope("activity.invalid", 1),
                PauseActivityBindingIntentResolution.Invalid(1, "test.invalid", "invalid authoring"),
                null,
                new FakePauseProductBindingPort(),
                "test",
                "invalid",
                out PauseActivityBindingOperationResult result);

            Assert.That(accepted, Is.False);
            Assert.That(result.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Failed));
            Assert.That(result.Diagnostic, Does.Contain("intent-invalid"));
        }

        [TestCaseSource(nameof(InvalidHostEvidenceCases))]
        public void RequiredIntent_RejectsInvalidHostEvidence(
            IReadOnlyList<LocalPlayerHostAuthoring> hosts,
            string expectedDiagnostic)
        {
            var context = new PauseActivityBindingRuntimeContext();

            bool accepted = context.TryActivate(
                CreateScope("activity.hosts", 1),
                RequiredIntent(),
                hosts,
                new FakePauseProductBindingPort(),
                "test",
                "host-validation",
                out PauseActivityBindingOperationResult result);

            Assert.That(accepted, Is.False);
            Assert.That(result.Diagnostic, Does.Contain(expectedDiagnostic));
        }

        [Test]
        public void RequiredIntent_RejectsTwoDistinctJoinedHosts()
        {
            LocalPlayerHostAuthoring first = CreateJoinedHost("First", out _);
            LocalPlayerHostAuthoring second = CreateJoinedHost("Second", out _);
            var context = new PauseActivityBindingRuntimeContext();

            bool accepted = context.TryActivate(
                CreateScope("activity.two-hosts", 1),
                RequiredIntent(),
                new[] { first, second },
                new FakePauseProductBindingPort(),
                "test",
                "two-hosts",
                out PauseActivityBindingOperationResult result);

            Assert.That(accepted, Is.False);
            Assert.That(result.Diagnostic, Does.Contain("unsupported-multiple-eligible-hosts"));
        }

        [Test]
        public void BindingResolution_RejectsMissingChildAndMismatchedBindings()
        {
            LocalPlayerHostAuthoring missing = CreateJoinedHost("Missing", out _);
            var missingContext = new PauseActivityBindingRuntimeContext();
            missingContext.TryActivate(
                CreateScope("activity.missing", 1), RequiredIntent(), new[] { missing },
                new FakePauseProductBindingPort(), "test", "missing", out PauseActivityBindingOperationResult missingResult);
            Assert.That(missingResult.Diagnostic, Does.Contain("binding-missing"));

            LocalPlayerHostAuthoring childHost = CreateJoinedHost("Child", out _);
            GameObject child = CreateObject("Child Binding");
            child.transform.SetParent(childHost.transform);
            PausePlayerInputBinding childBinding = child.AddComponent<PausePlayerInputBinding>();
            SetPrivateField(childBinding, "playerInput", childHost.PlayerInput);
            var childContext = new PauseActivityBindingRuntimeContext();
            childContext.TryActivate(
                CreateScope("activity.child", 1), RequiredIntent(), new[] { childHost },
                new FakePauseProductBindingPort(), "test", "child", out PauseActivityBindingOperationResult childResult);
            Assert.That(childResult.Diagnostic, Does.Contain("binding-not-colocated"));

            LocalPlayerHostAuthoring mismatchHost = CreateJoinedHost("Mismatch", out PausePlayerInputBinding mismatchBinding);
            PlayerInput otherInput = CreateObject("Other Input").AddComponent<PlayerInput>();
            SetPrivateField(mismatchBinding, "playerInput", otherInput);
            var mismatchContext = new PauseActivityBindingRuntimeContext();
            mismatchContext.TryActivate(
                CreateScope("activity.mismatch", 1), RequiredIntent(), new[] { mismatchHost },
                new FakePauseProductBindingPort(), "test", "mismatch", out PauseActivityBindingOperationResult mismatchResult);
            Assert.That(mismatchResult.Diagnostic, Does.Contain("binding-playerinput-mismatch"));
        }

        [Test]
        public void ValidCoLocatedBinding_RegistersOnceAndSameActivationIsIdempotent()
        {
            LocalPlayerHostAuthoring host = CreateJoinedHost("Valid", out PausePlayerInputBinding binding);
            PauseActivityBindingScope scope = CreateScope("activity.valid", 1);
            PauseActivityBindingIntentResolution intent = RequiredIntent();
            var context = new PauseActivityBindingRuntimeContext();
            var port = new FakePauseProductBindingPort();

            Assert.That(context.TryActivate(scope, intent, new[] { host }, port, "test", "first", out PauseActivityBindingOperationResult first), Is.True);
            Assert.That(first.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Activated));
            Assert.That(first.Diagnostic, Does.Contain("binding-registered"));
            Assert.That(binding.HasActiveBinding, Is.True);

            Assert.That(context.TryActivate(scope, intent, new[] { host }, port, "test", "repeat", out PauseActivityBindingOperationResult repeat), Is.True);
            Assert.That(repeat.Status, Is.EqualTo(PauseActivityBindingOperationStatus.AlreadyActive));
            Assert.That(repeat.Diagnostic, Does.Contain("binding-already-active"));
            Assert.That(port.RegisterCalls, Is.EqualTo(1));
        }

        [Test]
        public void ActiveBinding_RejectsDifferentHostScopeAndPort()
        {
            LocalPlayerHostAuthoring activeHost = CreateJoinedHost("Active", out _);
            LocalPlayerHostAuthoring otherHost = CreateJoinedHost("Other", out _);
            PauseActivityBindingScope scope = CreateScope("activity.active", 1);
            var context = new PauseActivityBindingRuntimeContext();
            var port = new FakePauseProductBindingPort();
            Assert.That(context.TryActivate(scope, RequiredIntent(), new[] { activeHost }, port, "test", "activate", out _), Is.True);

            context.TryActivate(scope, RequiredIntent(), new[] { otherHost }, port, "test", "host", out PauseActivityBindingOperationResult hostResult);
            context.TryActivate(CreateScope("activity.other", 1), RequiredIntent(), new[] { activeHost }, port, "test", "scope", out PauseActivityBindingOperationResult scopeResult);
            context.TryActivate(CreateScope("activity.active", 2), RequiredIntent(), new[] { activeHost }, port, "test", "newer-scope", out PauseActivityBindingOperationResult newerScopeResult);
            context.TryActivate(scope, RequiredIntent(), new[] { activeHost }, new FakePauseProductBindingPort(), "test", "port", out PauseActivityBindingOperationResult portResult);

            Assert.That(hostResult.Diagnostic, Does.Contain("active-scope-conflict"));
            Assert.That(scopeResult.Diagnostic, Does.Contain("active-scope-conflict"));
            Assert.That(newerScopeResult.Diagnostic, Does.Contain("active-scope-conflict"));
            Assert.That(portResult.Diagnostic, Does.Contain("active-scope-conflict"));
            Assert.That(port.RegisterCalls, Is.EqualTo(1));
        }

        [Test]
        public void RegistrationFailure_LeavesContextInactiveAndRetriable()
        {
            LocalPlayerHostAuthoring host = CreateJoinedHost("Retry", out _);
            PauseActivityBindingScope scope = CreateScope("activity.retry", 1);
            var context = new PauseActivityBindingRuntimeContext();
            var port = new FakePauseProductBindingPort { RegisterSucceeds = false };

            Assert.That(context.TryActivate(scope, RequiredIntent(), new[] { host }, port, "test", "fail", out PauseActivityBindingOperationResult failed), Is.False);
            Assert.That(failed.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Failed));
            Assert.That(context.Snapshot.HasActiveBinding, Is.False);
            Assert.That(context.State, Is.EqualTo(PauseActivityBindingRuntimeState.Inactive));

            port.RegisterSucceeds = true;
            Assert.That(context.TryActivate(scope, RequiredIntent(), new[] { host }, port, "test", "retry", out PauseActivityBindingOperationResult retried), Is.True);
            Assert.That(retried.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Activated));
        }

        [Test]
        public void Release_RejectsForeignAndStaleScopesThenReleasesCorrectScope()
        {
            LocalPlayerHostAuthoring host = CreateJoinedHost("Release", out PausePlayerInputBinding binding);
            PauseActivityBindingScope scope = CreateScope("activity.release", 3);
            var context = new PauseActivityBindingRuntimeContext();
            var port = new FakePauseProductBindingPort();
            Assert.That(context.TryActivate(scope, RequiredIntent(), new[] { host }, port, "test", "activate", out _), Is.True);

            context.TryRelease(CreateScope("activity.foreign", 3), "test", "foreign", out PauseActivityBindingOperationResult foreign);
            context.TryRelease(CreateScope("activity.release", 2), "test", "stale", out PauseActivityBindingOperationResult stale);
            Assert.That(foreign.Diagnostic, Does.Contain("foreign-scope-release"));
            Assert.That(stale.Diagnostic, Does.Contain("stale-scope-release"));

            Assert.That(context.TryRelease(scope, "test", "correct", out PauseActivityBindingOperationResult released), Is.True);
            Assert.That(released.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Released));
            Assert.That(released.Diagnostic, Does.Contain("binding-released"));
            Assert.That(binding.HasActiveBinding, Is.False);
            Assert.That(context.Snapshot.HasActiveBinding, Is.False);

            Assert.That(context.TryRelease(scope, "test", "repeat", out PauseActivityBindingOperationResult repeated), Is.True);
            Assert.That(repeated.Status, Is.EqualTo(PauseActivityBindingOperationStatus.AlreadyReleased));
        }

        [Test]
        public void ReleaseFailure_PreservesEvidenceForRetry()
        {
            LocalPlayerHostAuthoring host = CreateJoinedHost("Release Retry", out PausePlayerInputBinding binding);
            PauseActivityBindingScope scope = CreateScope("activity.release-retry", 1);
            var port = new FakePauseProductBindingPort { ReleaseSucceeds = false };
            var context = new PauseActivityBindingRuntimeContext();
            Assert.That(context.TryActivate(scope, RequiredIntent(), new[] { host }, port, "test", "activate", out _), Is.True);

            Assert.That(context.TryRelease(scope, "test", "fail", out PauseActivityBindingOperationResult failed), Is.False);
            Assert.That(failed.Diagnostic, Does.Contain("binding-release-failed"));
            Assert.That(context.State, Is.EqualTo(PauseActivityBindingRuntimeState.Failed));
            Assert.That(context.Snapshot.HasActiveBinding, Is.True);
            Assert.That(context.Snapshot.ActiveScope, Is.EqualTo(scope));
            Assert.That(binding.HasActiveBinding, Is.True);
            Assert.That(binding.BindingStatus, Is.EqualTo("Failed"));
            Assert.That(binding.BindingDiagnostic, Does.Contain("binding retained for retry"));

            port.ReleaseSucceeds = true;
            Assert.That(context.TryRelease(scope, "test", "retry", out PauseActivityBindingOperationResult retried), Is.True);
            Assert.That(retried.Status, Is.EqualTo(PauseActivityBindingOperationStatus.Released));
            Assert.That(port.ReleaseTokens, Has.Count.EqualTo(2));
            Assert.That(port.ReleaseTokens[1].Generation, Is.EqualTo(port.ReleaseTokens[0].Generation));
            Assert.That(port.ReleaseTokens[1].PlayerInstanceId, Is.EqualTo(port.ReleaseTokens[0].PlayerInstanceId));
        }

        [Test]
        public void ComponentRegistrationAndRelease_AreTransactionalAndOnDisableDoesNotDoubleRelease()
        {
            LocalPlayerHostAuthoring host = CreateJoinedHost("Component", out PausePlayerInputBinding binding);
            var rejectedPort = new FakePauseProductBindingPort { RegisterSucceeds = false };
            var activePort = new FakePauseProductBindingPort { ReleaseSucceeds = false };

            Assert.That(binding.TryInjectBindingPort(rejectedPort, out _), Is.False);
            Assert.That(binding.TryInjectBindingPort(activePort, out _), Is.True);
            Assert.That(activePort.RegisterCalls, Is.EqualTo(1));

            Assert.That(binding.TryReleaseBinding("fail", out _), Is.False);
            Assert.That(binding.HasActiveBinding, Is.True);
            activePort.ReleaseSucceeds = true;
            Assert.That(binding.TryReleaseBinding("retry", out _), Is.True);
            Assert.That(binding.HasActiveBinding, Is.False);
            Assert.That(activePort.ReleaseCalls, Is.EqualTo(2));

            typeof(PausePlayerInputBinding)
                .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(binding, null);
            Assert.That(activePort.ReleaseCalls, Is.EqualTo(2));
            Assert.That(host, Is.Not.Null);
        }

        private static IEnumerable<TestCaseData> InvalidHostEvidenceCases()
        {
            yield return new TestCaseData(null, "admitted-host-evidence-missing");
            yield return new TestCaseData(Array.Empty<LocalPlayerHostAuthoring>(), "admitted-host-evidence-missing");
        }

        [Test]
        public void RequiredIntent_RejectsNullDuplicateNonJoinedAndInvalidSlotEvidence()
        {
            var context = new PauseActivityBindingRuntimeContext();
            LocalPlayerHostAuthoring joined = CreateJoinedHost("Duplicate", out _);
            context.TryActivate(CreateScope("activity.null", 1), RequiredIntent(), new LocalPlayerHostAuthoring[] { null }, new FakePauseProductBindingPort(), "test", "null", out PauseActivityBindingOperationResult nullResult);
            context.TryActivate(CreateScope("activity.duplicate", 1), RequiredIntent(), new[] { joined, joined }, new FakePauseProductBindingPort(), "test", "duplicate", out PauseActivityBindingOperationResult duplicateResult);

            LocalPlayerHostAuthoring nonJoined = CreateHost("Non Joined");
            context.TryActivate(CreateScope("activity.non-joined", 1), RequiredIntent(), new[] { nonJoined }, new FakePauseProductBindingPort(), "test", "non-joined", out PauseActivityBindingOperationResult nonJoinedResult);

            LocalPlayerHostAuthoring invalidSlot = CreateHost("Invalid Slot");
            SetAdmissionState(invalidSlot, "Joined");
            context.TryActivate(CreateScope("activity.slot", 1), RequiredIntent(), new[] { invalidSlot }, new FakePauseProductBindingPort(), "test", "slot", out PauseActivityBindingOperationResult slotResult);

            Assert.That(nullResult.Diagnostic, Does.Contain("admitted-host-evidence-missing"));
            Assert.That(duplicateResult.Diagnostic, Does.Contain("admitted-host-evidence-duplicate"));
            Assert.That(nonJoinedResult.Diagnostic, Does.Contain("host-not-joined"));
            Assert.That(slotResult.Diagnostic, Does.Contain("host-slot-invalid"));
        }

        private LocalPlayerHostAuthoring CreateJoinedHost(string name, out PausePlayerInputBinding binding)
        {
            LocalPlayerHostAuthoring host = CreateHost(name);
            SetAdmissionState(host, "Joined");
            string slotName = name.ToLowerInvariant().Replace(' ', '-');
            SetPrivateField(host, "joinedPlayerSlotId", new PlayerSlotId($"test.{slotName}"));
            binding = host.gameObject.AddComponent<PausePlayerInputBinding>();
            SetPrivateField(binding, "playerInput", host.PlayerInput);
            return host;
        }

        private LocalPlayerHostAuthoring CreateHost(string name)
        {
            GameObject value = CreateObject(name);
            value.SetActive(false);
            PlayerInput input = value.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = value.AddComponent<LocalPlayerHostAuthoring>();
            SetPrivateField(host, "playerInput", input);
            return host;
        }

        private GameObject CreateObject(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }

        private static PauseActivityBindingIntentResolution RequiredIntent()
        {
            return PauseActivityBindingIntentResolution.Created(
                new PauseActivityBindingIntent(PauseActivityBindingRequiredness.Required, "test.intent"),
                "test.intent");
        }

        private static PauseActivityBindingScope CreateScope(string activityId, int sequence)
        {
            return new PauseActivityBindingScope(
                RuntimeContentOwner.Activity(activityId, "Test Activity"),
                sequence);
        }

        private static void SetAdmissionState(LocalPlayerHostAuthoring host, string state)
        {
            FieldInfo field = typeof(LocalPlayerHostAuthoring).GetField(
                "admissionState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(host, Enum.Parse(field.FieldType, state));
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class FakePauseProductBindingPort : IPauseProductBindingPort
        {
            private long nextGeneration;

            public bool RegisterSucceeds { get; set; } = true;
            public bool ReleaseSucceeds { get; set; } = true;
            public int RegisterCalls { get; private set; }
            public int ReleaseCalls { get; private set; }
            public List<PauseProductBindingToken> ReleaseTokens { get; } = new();

            public bool TryRegister(
                PausePlayerInputBinding binding,
                out PauseProductBindingToken token,
                out string diagnostic)
            {
                RegisterCalls++;
                if (!RegisterSucceeds)
                {
                    token = default;
                    diagnostic = "fake registration failure";
                    return false;
                }

                nextGeneration++;
                token = new PauseProductBindingToken(nextGeneration, binding.GetInstanceID());
                diagnostic = "fake registration succeeded";
                return true;
            }

            public bool ReleaseBinding(
                PauseProductBindingToken token,
                string reason,
                out string diagnostic)
            {
                ReleaseCalls++;
                ReleaseTokens.Add(token);
                diagnostic = ReleaseSucceeds ? "fake release succeeded" : "fake release failure";
                return ReleaseSucceeds && token.IsValid;
            }
        }
    }
}
