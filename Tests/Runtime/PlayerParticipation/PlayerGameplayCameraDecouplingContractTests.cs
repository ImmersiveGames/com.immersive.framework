using System;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Camera;
using NUnit.Framework;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerGameplayCameraDecouplingContractTests
    {
        private static readonly Assembly RuntimeAssembly =
            typeof(PlayerGameplayAdmissionSummary).Assembly;

        [Test]
        public void ObsoleteOrdinaryPlayerCameraTypes_AreRemoved()
        {
            string[] obsoleteTypes =
            {
                "PlayerGameplayCameraEligibilityRuntimeContext",
                "PlayerGameplayCameraEligibilitySummary",
                "PlayerGameplayCameraEligibilitySnapshot",
                "PlayerGameplayCameraEligibilityToken",
                "PlayerGameplayCameraEligibilityResult",
                "PlayerGameplayCameraEligibilityStatus",
                "PlayerGameplayCameraEligibilityState",
                "PlayerGameplayCameraAuthoring",
                "PlayerGameplayCameraRequiredness"
            };

            foreach (string typeName in obsoleteTypes)
            {
                Assert.That(
                    RuntimeAssembly.GetType(
                        $"Immersive.Framework.PlayerParticipation.{typeName}"),
                    Is.Null,
                    typeName);
            }

            Assert.That(
                RuntimeAssembly.GetType(
                    "Immersive.Framework.Camera.LocalPlayerCameraRequestPublisher"),
                Is.Null);
            Assert.That(
                Enum.GetNames(typeof(CameraRequestOwnerKind)),
                Does.Not.Contain("LocalPlayer"));
            Assert.That(
                Enum.GetNames(typeof(CameraRequestLifetimeKind)),
                Does.Not.Contain("LocalPlayerEligibility"));
        }

        [Test]
        public void GameplayAdmissionContract_ContainsOccupancyAndInputButNoCameraEvidence()
        {
            Assert.That(
                typeof(PlayerGameplayAdmissionSummary).GetProperty("OccupancyToken"),
                Is.Not.Null);
            Assert.That(
                typeof(PlayerGameplayAdmissionSummary).GetProperty("InputBindingToken"),
                Is.Not.Null);
            Assert.That(
                typeof(PlayerGameplayAdmissionSummary).GetProperties()
                    .Any(property => property.Name.Contains("Camera")),
                Is.False);
            Assert.That(
                typeof(PlayerGameplayAdmissionToken).GetProperties()
                    .Any(property => property.Name.Contains("Camera")),
                Is.False);
        }

        [Test]
        public void RuntimeCompositionAndRelease_HaveNoCameraCapabilityStage()
        {
            Type runtime = RequiredType("PlayerGameplayRuntimeHostModule");
            Type currentContext = RequiredType("PlayerGameplayCurrentContextRuntime");
            Type admissionContext = RequiredType("PlayerGameplayAdmissionRuntimeContext");
            Type endpointSource = RequiredType("IPlayerGameplayCurrentContextEndpointSource");
            Type releaseResult = RequiredType(
                "PlayerGameplayRuntimeHostModule+SessionPlayerLeaveGameplayReleaseResult");

            AssertNoCameraMembers(runtime);
            AssertNoCameraMembers(currentContext);
            AssertNoCameraMembers(releaseResult);
            AssertNoCameraParameters(admissionContext, "TryCreate");
            AssertNoCameraParameters(admissionContext, "TryAdmit");
            AssertNoCameraParameters(endpointSource, "TryResolveGameplayEndpoints");
        }

        [Test]
        public void PlayerGameplayRuntimeSnapshot_HasNoCameraDecisionEvidence()
        {
            Assert.That(
                typeof(PlayerGameplayRuntimeHostSnapshot).GetProperties()
                    .Any(property => property.Name.Contains("Camera")),
                Is.False);
        }

        private static Type RequiredType(string name)
        {
            Type type = RuntimeAssembly.GetType(
                $"Immersive.Framework.PlayerParticipation.{name}");
            Assert.That(type, Is.Not.Null, name);
            return type;
        }

        private static void AssertNoCameraMembers(Type type)
        {
            string[] names = type
                .GetMembers(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Select(member => member.Name)
                .ToArray();
            Assert.That(names.Any(name => name.Contains("Camera")), Is.False, type.FullName);
        }

        private static void AssertNoCameraParameters(Type type, string methodName)
        {
            MethodInfo method = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName);
            Assert.That(
                method.GetParameters().Any(parameter =>
                    parameter.ParameterType.Name.Contains("Camera")),
                Is.False,
                $"{type.FullName}.{methodName}");
        }
    }
}
