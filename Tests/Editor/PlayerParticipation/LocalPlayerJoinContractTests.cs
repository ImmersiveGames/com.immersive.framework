using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class LocalPlayerJoinContractTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t ")]
        public void Request_RejectsMissingSource(string source)
        {
            var request = new LocalPlayerJoinRequest(source, "valid reason");

            Assert.That(request.TryValidate(out string issue), Is.False);
            Assert.That(issue, Is.EqualTo("Local Player join request requires a non-empty source."));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t ")]
        public void Request_RejectsMissingReason(string reason)
        {
            var request = new LocalPlayerJoinRequest("valid source", reason);

            Assert.That(request.TryValidate(out string issue), Is.False);
            Assert.That(issue, Is.EqualTo("Local Player join request requires a non-empty reason."));
        }

        [Test]
        public void Request_NormalizesRequiredTextAndOptionalControlScheme()
        {
            var request = new LocalPlayerJoinRequest("  menu  ", "  user selected  ", controlScheme: "  Gamepad  ");

            Assert.That(request.IsValid, Is.True);
            Assert.That(request.Source, Is.EqualTo("menu"));
            Assert.That(request.Reason, Is.EqualTo("user selected"));
            Assert.That(request.ControlScheme, Is.EqualTo("Gamepad"));
            Assert.That(request.HasDeviceHint, Is.False);
            Assert.That(request.HasControlSchemeHint, Is.True);
        }

        [Test]
        public void Request_PreservesOptionalHintsWithoutAcceptingThemAsAllocationInputs()
        {
            var request = new LocalPlayerJoinRequest("menu", "join requested");

            Assert.That(request.PairWithDevice, Is.Null);
            Assert.That(request.HasDeviceHint, Is.False);
            Assert.That(request.HasControlSchemeHint, Is.False);
            Assert.That(request.ToDiagnosticString(), Does.Contain("deviceHint='False'"));
        }

        [TestCase(LocalPlayerJoinStatus.RejectedInvalidRequest)]
        [TestCase(LocalPlayerJoinStatus.RejectedOperationInFlight)]
        [TestCase(LocalPlayerJoinStatus.RejectedRuntimeUnavailable)]
        [TestCase(LocalPlayerJoinStatus.RejectedManagerUnavailable)]
        [TestCase(LocalPlayerJoinStatus.RejectedManagerConfiguration)]
        [TestCase(LocalPlayerJoinStatus.RejectedJoiningClosed)]
        [TestCase(LocalPlayerJoinStatus.RejectedNoAvailableSlot)]
        [TestCase(LocalPlayerJoinStatus.RejectedProvisioningReturnedNull)]
        [TestCase(LocalPlayerJoinStatus.RejectedUnexpectedJoin)]
        [TestCase(LocalPlayerJoinStatus.RejectedCorrelationMismatch)]
        [TestCase(LocalPlayerJoinStatus.RejectedMissingPlayerInput)]
        [TestCase(LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost)]
        [TestCase(LocalPlayerJoinStatus.RejectedForeignOrStaleReservation)]
        [TestCase(LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost)]
        [TestCase(LocalPlayerJoinStatus.RejectedDeviceAlreadyOwned)]
        public void JoinStatus_RejectionsAreCompletedRejectedResults(LocalPlayerJoinStatus status)
        {
            LocalPlayerJoinResult result = CreateResult(status);

            Assert.That(result.Completed, Is.True);
            Assert.That(result.Rejected, Is.True);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failed, Is.False);
        }

        [TestCase(LocalPlayerJoinStatus.FailedAdmission)]
        [TestCase(LocalPlayerJoinStatus.FailedRollback)]
        public void JoinStatus_FailuresRemainDistinctFromRejections(LocalPlayerJoinStatus status)
        {
            LocalPlayerJoinResult result = CreateResult(status);

            Assert.That(result.Completed, Is.True);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Rejected, Is.False);
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void JoinStatus_NoneIsNotTerminal()
        {
            LocalPlayerJoinResult result = CreateResult(LocalPlayerJoinStatus.None);

            Assert.That(result.Completed, Is.False);
            Assert.That(result.Rejected, Is.False);
            Assert.That(result.Failed, Is.False);
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void PublicJoinInvocationSurfaceContainsOnlyTheScopedTrigger()
        {
            var runtimeAssembly = typeof(PlayerSessionJoinTrigger).Assembly;
            const string playerNamespace = "Immersive.Framework.PlayerParticipation.";

            Assert.That(runtimeAssembly.GetType(
                playerNamespace + "PlayerSessionJoinCommandTrigger"), Is.Null);
            Assert.That(runtimeAssembly.GetType(
                playerNamespace + "ILocalPlayerJoinAccess"), Is.Null);
            Assert.That(typeof(PlayerSessionScopedAccessConsumer)
                .GetMethod("TryGetJoinAccess"), Is.Null);
            Assert.That(typeof(LocalPlayerProvisioningAuthoring)
                .GetMethod("RequestJoin"), Is.Null);
            Assert.That(typeof(LocalPlayerProvisioningAuthoring)
                .GetProperty("LastJoinResult"), Is.Null);

            var internalResult = runtimeAssembly.GetType(
                playerNamespace + "LocalPlayerJoinResult");
            Assert.That(internalResult, Is.Not.Null);
            Assert.That(internalResult.IsPublic, Is.False);

            var request = typeof(PlayerSessionJoinTrigger).GetMethod(
                "RequestJoin", new[] { typeof(LocalPlayerJoinRequest) });
            Assert.That(request, Is.Not.Null);
            Assert.That(request.ReturnType, Is.EqualTo(typeof(LocalPlayerJoinOutcome)));
            var lastJoinStatus = typeof(PlayerActorPreparationRuntimeHostSnapshot)
                .GetProperty(nameof(PlayerActorPreparationRuntimeHostSnapshot.LastJoinStatus));
            Assert.That(lastJoinStatus, Is.Not.Null);
            Assert.That(lastJoinStatus.PropertyType, Is.EqualTo(typeof(LocalPlayerJoinStatus)));
        }

        private static LocalPlayerJoinResult CreateResult(LocalPlayerJoinStatus status)
        {
            return new LocalPlayerJoinResult(
                status,
                default,
                new LocalPlayerJoinRequest("test", "contract assertion"),
                null,
                null,
                null,
                default,
                null,
                null,
                -1,
                LocalPlayerJoinCallbackConfirmation.None,
                string.Empty);
        }
    }
}
