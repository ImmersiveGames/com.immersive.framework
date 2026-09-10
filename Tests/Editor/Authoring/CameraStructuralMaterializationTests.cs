using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraStructuralMaterializationTests
    {
        [TestCase(CameraRigPresentationIntent.Fixed)]
        [TestCase(CameraRigPresentationIntent.Follow)]
        [TestCase(CameraRigPresentationIntent.Mounted)]
        [TestCase(CameraRigPresentationIntent.ThirdPerson)]
        public void MaterializesWithoutSubjectsAndReusesStructure(CameraRigPresentationIntent model)
        {
            var root = new GameObject("structural-camera-test");
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                var serialized = new SerializedObject(composer);
                serialized.FindProperty("presentationIntent").intValue = (int)model;
                serialized.FindProperty("lookAtRequirement").intValue = (int)CameraTargetRequirement.Required;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var first = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                Assert.That(first.Succeeded, Is.True, first.BlockingIssue);
                var camera = composer.CinemachineCamera;
                Assert.That(camera.Follow, Is.Null);
                Assert.That(camera.LookAt, Is.Null);
                var body = camera.GetCinemachineComponent(CinemachineCore.Stage.Body);
                var aim = camera.GetCinemachineComponent(CinemachineCore.Stage.Aim);
                if (model == CameraRigPresentationIntent.Fixed)
                {
                    Assert.That(body, Is.Null);
                    Assert.That(aim, Is.Null);
                    Assert.That(composer.EffectiveLookAtRequirement, Is.EqualTo(CameraTargetRequirement.NotUsed));
                }
                else if (model == CameraRigPresentationIntent.Follow)
                {
                    Assert.That(body, Is.TypeOf<CinemachineFollow>());
                    Assert.That(aim, Is.TypeOf<CinemachineHardLookAt>());
                    Assert.That(composer.FrameworkOwnedSharedFollowTargetGroup, Is.Not.Null);
                    Assert.That(composer.FrameworkOwnedSharedFollowGroupFraming.enabled, Is.False);
                }
                else if (model == CameraRigPresentationIntent.Mounted)
                {
                    Assert.That(body, Is.TypeOf<CinemachineHardLockToTarget>());
                    Assert.That(aim, Is.TypeOf<CinemachineRotateWithFollowTarget>());
                }
                else Assert.That(body, Is.TypeOf<CinemachineThirdPersonFollow>());

                var group = composer.FrameworkOwnedSharedFollowTargetGroup;
                var framing = composer.FrameworkOwnedSharedFollowGroupFraming;
                var second = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                Assert.That(second.Succeeded, Is.True, second.BlockingIssue);
                Assert.That(second.CreatedCount, Is.Zero);
                Assert.That(composer.CinemachineCamera, Is.SameAs(camera));
                Assert.That(composer.FrameworkOwnedSharedFollowTargetGroup, Is.SameAs(group));
                Assert.That(composer.FrameworkOwnedSharedFollowGroupFraming, Is.SameAs(framing));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ForeignFramingBlocksBeforePipelineMutation()
        {
            var root = new GameObject("foreign-framing-test");
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                var camera = root.AddComponent<CinemachineCamera>();
                var authored = root.AddComponent<CinemachineGroupFraming>();
                composer.EditorSetGeneratedReference(camera);
                var result = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(camera.GetCinemachineComponent(CinemachineCore.Stage.Body), Is.Null);
                Assert.That(composer.FrameworkOwnedSharedFollowTargetGroup, Is.Null);
                Assert.That(composer.FrameworkOwnedSharedFollowGroupFraming, Is.Null);
                Assert.That(camera.GetComponent<CinemachineGroupFraming>(), Is.SameAs(authored));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
