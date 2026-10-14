using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class FrameworkRuntimeHostAdmissionTests
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
        public void Operations_AreClosedUntilStartupCompletesSuccessfully()
        {
            var admission = new FrameworkRuntimeHostAdmission();

            Assert.That(admission.TryEnterOperation(cleanup: false, out _), Is.False);
            Assert.That(admission.TryBeginStart(), Is.True);
            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Starting));
            Assert.That(admission.TryEnterOperation(cleanup: false, out _), Is.False);

            admission.CompleteStart(succeeded: true);

            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Operational));
            Assert.That(admission.TryEnterOperation(cleanup: false, out System.IDisposable lease), Is.True);
            lease.Dispose();
        }

        [Test]
        public async Task Stop_ClosesNewMutationAndWaitsForAdmittedOperationToSettle()
        {
            var admission = new FrameworkRuntimeHostAdmission();
            Assert.That(admission.TryBeginStart(), Is.True);
            admission.CompleteStart(succeeded: true);
            Assert.That(admission.TryEnterOperation(cleanup: false, out System.IDisposable operation), Is.True);

            Task drained = admission.BeginStopAndWaitForOperationsAsync();

            Assert.That(drained.IsCompleted, Is.False);
            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopping));
            Assert.That(admission.TryEnterOperation(cleanup: false, out _), Is.False);
            Assert.That(admission.TryEnterOperation(cleanup: true, out System.IDisposable cleanup), Is.True);
            cleanup.Dispose();
            Assert.That(drained.IsCompleted, Is.False);

            operation.Dispose();
            await drained;
            Assert.That(admission.TryBeginStopFinalization(), Is.True);
            admission.CompleteStop(succeeded: true);

            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
            Assert.That(admission.TryEnterOperation(cleanup: true, out _), Is.False);
        }

        [Test]
        public async Task StopRequestedDuringBootstrap_WaitsForStartupThenContinuesFailClosed()
        {
            var admission = new FrameworkRuntimeHostAdmission();
            Assert.That(admission.TryBeginStart(), Is.True);

            Task startup = admission.BeginStopAndWaitForOperationsAsync();

            Assert.That(startup.IsCompleted, Is.False);
            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Starting));
            Assert.That(admission.TryEnterOperation(cleanup: true, out _), Is.False);

            admission.CompleteStart(succeeded: false);
            await startup;

            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopping));
            Assert.That(admission.TryBeginStopFinalization(), Is.True);
            admission.CompleteStop(succeeded: true);
            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
        }

        [Test]
        public void FailedStop_RemainsInertAndAllowsAnExactCleanupRetry()
        {
            var admission = new FrameworkRuntimeHostAdmission();
            Assert.That(admission.TryBeginStart(), Is.True);
            admission.CompleteStart(succeeded: true);
            admission.BeginStopAndWaitForOperationsAsync();
            Assert.That(admission.TryBeginStopFinalization(), Is.True);

            admission.CompleteStop(succeeded: false);

            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopping));
            Assert.That(admission.TryEnterOperation(cleanup: false, out _), Is.False);
            Assert.That(admission.TryEnterOperation(cleanup: true, out System.IDisposable cleanup), Is.True);
            cleanup.Dispose();

            Assert.That(admission.BeginStopAndWaitForOperationsAsync().IsCompleted, Is.True);
            Assert.That(admission.TryBeginStopFinalization(), Is.True);
            admission.CompleteStop(succeeded: true);
            Assert.That(admission.Phase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
        }

        [Test]
        public async Task Host_StopBeforeOperationalClosesAdmissionWithoutDestroyingTheOwner()
        {
            var hostObject = new GameObject("host-stop-before-operational");
            _objects.Add(hostObject);
            FrameworkRuntimeHost host = hostObject.AddComponent<FrameworkRuntimeHost>();

            FrameworkRuntimeHostStopResult result = await host.StopAsync("admission-test");

            Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            Assert.That(host.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Stopped));
            Assert.That(host != null, Is.True, "Controlled stop closes admission but does not destroy the Host owner.");
            Assert.That(host.TryEnterRuntimeOperation(cleanup: true, out _), Is.False);
        }

        [Test]
        public void Host_SelectionIngressIsRejectedBeforeOperational()
        {
            var hostObject = new GameObject("host-selection-before-operational");
            _objects.Add(hostObject);
            FrameworkRuntimeHost host = hostObject.AddComponent<FrameworkRuntimeHost>();
            var selectionPort = (IPlayerActorSelectionRuntimePort)host;

            PlayerActorSelectionResult result = selectionPort.TrySelectDefaultActor(
                default,
                expectedSelectionRevision: 0,
                source: "admission-test",
                reason: "before-operational");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ToDiagnosticString(), Does.Contain("Host phase is 'Created'"));
            Assert.That(host.LifecyclePhase, Is.EqualTo(FrameworkRuntimeHostPhase.Created));
        }
    }
}
