using System.Reflection;
using Immersive.Framework.Camera;
using NUnit.Framework;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraOutputAuthorityBoundaryTests
    {
        [Test]
        public void AuthoringKeepsSessionDiagnosticsButDoesNotExposeApplicator()
        {
            PropertyInfo session = typeof(CameraOutputAuthoring).GetProperty(
                nameof(CameraOutputAuthoring.Session),
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(session, Is.Not.Null);
            Assert.That(session.GetMethod.IsPublic, Is.True);
            Assert.That(typeof(CameraOutputAuthoring).GetProperty(
                nameof(CameraOutputAuthoring.Applicator),
                BindingFlags.Instance | BindingFlags.Public), Is.Null);
        }

        [Test]
        public void SessionAndStateExposeOnlyReadOnlyObservationToConsumers()
        {
            AssertNoPublicConstructor(typeof(CameraOutputSession));
            AssertNoPublicMethod(typeof(CameraOutputSession), "TrySetActiveAssignment");
            AssertNoPublicMethod(typeof(CameraOutputSession), "PresentNormalOccurrence");
            AssertNoPublicMethod(typeof(CameraOutputSession), "CoverWithFallback");
            AssertNoPublicMethod(typeof(CameraOutputSession), "ReleaseFallbackCoverage");
            AssertNoPublicMethod(typeof(CameraOutputSession), "Synchronize");
            AssertNoPublicMethod(typeof(CameraOutputSession), "Teardown");
            AssertNoPublicProperty(typeof(CameraOutputSession), "Applicator");
            AssertNoPublicProperty(typeof(CameraOutputSession), "FallbackRig");

            PropertyInfo outputState = typeof(CameraOutputSession).GetProperty(
                nameof(CameraOutputSession.OutputState),
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(outputState, Is.Not.Null);
            Assert.That(outputState.GetMethod.IsPublic, Is.True);

            AssertNoPublicConstructor(typeof(CameraOutputState));
            AssertNoPublicMethod(typeof(CameraOutputState), "TryMakeFallbackAvailable");
            AssertNoPublicMethod(typeof(CameraOutputState), "TrySetActiveAssignment");
            AssertNoPublicMethod(typeof(CameraOutputState), "TryPresentNormalOccurrence");
            AssertNoPublicMethod(typeof(CameraOutputState), "TryCoverWithFallback");
            AssertNoPublicMethod(typeof(CameraOutputState), "TryRestoreNormalOccurrence");

            foreach (PropertyInfo property in typeof(CameraOutputState).GetProperties(
                         BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.That(property.SetMethod == null || !property.SetMethod.IsPublic,
                    Is.True,
                    $"Diagnostic property '{property.Name}' must not expose a public setter.");
            }
        }

        [Test]
        public void ApplicatorCannotBeConstructedOrUsedAsAConsumerMutationPath()
        {
            AssertNoPublicConstructor(typeof(CameraOutputRigApplicator));
            AssertNoPublicMethod(typeof(CameraOutputRigApplicator), "Clear");
            AssertNoPublicMethod(typeof(CameraOutputRigApplicator), "ApplyFallbackRig");
            AssertNoPublicMethod(typeof(CameraOutputRigApplicator), "ApplyNormalOccurrence");
        }

        private static void AssertNoPublicConstructor(System.Type type)
        {
            Assert.That(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public),
                Is.Empty,
                $"'{type.Name}' must not expose public construction.");
        }

        private static void AssertNoPublicMethod(System.Type type, string name)
        {
            Assert.That(type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public),
                Is.Null,
                $"'{type.Name}.{name}' must not be public.");
        }

        private static void AssertNoPublicProperty(System.Type type, string name)
        {
            Assert.That(type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public),
                Is.Null,
                $"'{type.Name}.{name}' must not be public.");
        }
    }
}
