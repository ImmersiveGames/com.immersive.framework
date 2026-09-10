using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class ActorCameraSubjectAuthoringTests
    {
        private GameObject _presentation;
        private GameObject _foreign;

        [TearDown]
        public void TearDown()
        {
            if (_presentation != null) Object.DestroyImmediate(_presentation);
            if (_foreign != null) Object.DestroyImmediate(_foreign);
        }

        [Test]
        public void ExplicitChildTransformResolvesExactly()
        {
            ActorCameraSubjectAuthoring authoring = CreateAuthoring();
            var mount = new GameObject("Observation Mount");
            mount.transform.SetParent(_presentation.transform, false);
            SetObservation(authoring, mount.transform);

            Assert.That(
                authoring.TryResolveObservation(
                    _presentation.transform,
                    out Transform observation,
                    out string issue),
                Is.True,
                issue);
            Assert.That(observation, Is.SameAs(mount.transform));
            Assert.That(observation, Is.Not.SameAs(_presentation.transform));
        }

        [Test]
        public void MissingRequiredTransformBlocksWithoutRootFallback()
        {
            ActorCameraSubjectAuthoring authoring = CreateAuthoring();

            Assert.That(
                authoring.TryResolveObservation(
                    _presentation.transform,
                    out Transform observation,
                    out string issue),
                Is.False);
            Assert.That(observation, Is.Null);
            StringAssert.Contains("requires an explicit Camera Subject Transform", issue);
            StringAssert.Contains("No Actor-root fallback", issue);
        }

        [Test]
        public void ForeignTransformIsRejected()
        {
            ActorCameraSubjectAuthoring authoring = CreateAuthoring();
            _foreign = new GameObject("Foreign Transform");
            SetObservation(authoring, _foreign.transform);

            Assert.That(
                authoring.TryResolveObservation(
                    _presentation.transform,
                    out Transform observation,
                    out string issue),
                Is.False);
            Assert.That(observation, Is.Null);
            StringAssert.Contains("must belong to the authored Actor Presentation", issue);
        }

        private ActorCameraSubjectAuthoring CreateAuthoring()
        {
            _presentation = new GameObject("Actor Presentation");
            return _presentation.AddComponent<ActorCameraSubjectAuthoring>();
        }

        private static void SetObservation(
            ActorCameraSubjectAuthoring authoring,
            Transform observation)
        {
            FieldInfo field = typeof(ActorCameraSubjectAuthoring).GetField(
                "observationTransform",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(authoring, observation);
        }
    }
}
