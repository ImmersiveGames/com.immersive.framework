using System.Reflection;
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
            CameraRigBehaviorDefinition behavior = CreateBehavior(model);
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                var serialized = new SerializedObject(composer);
                serialized.FindProperty("behaviorDefinition").objectReferenceValue = behavior;
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
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void ForeignFramingBlocksBeforePipelineMutation()
        {
            var root = new GameObject("foreign-framing-test");
            var behavior = ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
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
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void MissingBehaviorBlocksAndFormerComposerFieldsDoNotExist()
        {
            var root = new GameObject("missing-behavior-test");
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                Assert.That(composer.TryValidateForApply(out string issue), Is.False);
                Assert.That(issue, Does.Contain("Camera Rig Behavior Definition"));
                var serialized = new SerializedObject(composer);
                Assert.That(serialized.FindProperty("presentationIntent"), Is.Null);
                Assert.That(serialized.FindProperty("followOffset"), Is.Null);
                Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void InvalidBehaviorReportsDefinitionModelFieldAndValue()
        {
            var root = new GameObject("invalid-behavior-test");
            var behavior = ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            try
            {
                behavior.name = "Invalid Follow";
                SetField(behavior, "sharedFollowMemberWeight", 0f);
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                Assert.That(composer.TryValidateForApply(out string issue), Is.False);
                Assert.That(issue, Does.Contain("Invalid Follow"));
                Assert.That(issue, Does.Contain("Follow"));
                Assert.That(issue, Does.Contain("sharedFollowMemberWeight"));
                Assert.That(issue, Does.Contain("0"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void FollowBehaviorProjectsAuthoredOffset()
        {
            var root = new GameObject("follow-values-test");
            var behavior = ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            try
            {
                var expected = new Vector3(3f, 4f, -9f);
                SetField(behavior, "followOffset", expected);
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                var result = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                Assert.That(result.Succeeded, Is.True, result.BlockingIssue);
                Assert.That(composer.CinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body), Is.TypeOf<CinemachineFollow>());
                Assert.That(((CinemachineFollow)composer.FrameworkOwnedPositionControl).FollowOffset, Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void MountedBehaviorProjectsAuthoredDamping()
        {
            var root = new GameObject("mounted-values-test");
            var behavior = ScriptableObject.CreateInstance<MountedCameraRigBehaviorDefinition>();
            try
            {
                SetField(behavior, "positionDamping", 1.25f);
                SetField(behavior, "rotationDamping", 2.5f);
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                var result = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                Assert.That(result.Succeeded, Is.True, result.BlockingIssue);
                Assert.That(((CinemachineHardLockToTarget)composer.FrameworkOwnedPositionControl).Damping, Is.EqualTo(1.25f));
                Assert.That(((CinemachineRotateWithFollowTarget)composer.FrameworkOwnedRotationControl).Damping, Is.EqualTo(2.5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void ThirdPersonBehaviorProjectsAllAuthoredValues()
        {
            var root = new GameObject("third-person-values-test");
            var behavior = ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>();
            try
            {
                var shoulder = new Vector3(0.7f, -0.2f, 0.1f);
                var damping = new Vector3(0.2f, 0.3f, 0.4f);
                SetField(behavior, "shoulderOffset", shoulder);
                SetField(behavior, "verticalArmLength", 0.8f);
                SetField(behavior, "cameraSide", 0.25f);
                SetField(behavior, "cameraDistance", 4.5f);
                SetField(behavior, "damping", damping);
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                var result = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                Assert.That(result.Succeeded, Is.True, result.BlockingIssue);
                var control = (CinemachineThirdPersonFollow)composer.FrameworkOwnedPositionControl;
                Assert.That(control.ShoulderOffset, Is.EqualTo(shoulder));
                Assert.That(control.VerticalArmLength, Is.EqualTo(0.8f));
                Assert.That(control.CameraSide, Is.EqualTo(0.25f));
                Assert.That(control.CameraDistance, Is.EqualTo(4.5f));
                Assert.That(control.Damping, Is.EqualTo(damping));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        [Test]
        public void ModelSwitchReplacesOnlyFrameworkOwnedControlsWithoutDuplicates()
        {
            var root = new GameObject("model-switch-test");
            var follow = ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            var thirdPerson = ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>();
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", follow);
                Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
                Component previous = composer.FrameworkOwnedPositionControl;

                SetField(composer, "behaviorDefinition", thirdPerson);
                var switched = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);

                Assert.That(switched.Succeeded, Is.True, switched.BlockingIssue);
                Assert.That(previous == null, Is.True);
                Assert.That(composer.FrameworkOwnedPositionControl, Is.TypeOf<CinemachineThirdPersonFollow>());
                Assert.That(composer.CinemachineCamera.GetComponents<CinemachineThirdPersonFollow>().Length, Is.EqualTo(1));
                Assert.That(composer.CinemachineCamera.GetComponents<CinemachineFollow>(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(follow);
                Object.DestroyImmediate(thirdPerson);
            }
        }

        [Test]
        public void ForeignIncompatiblePositionControlBlocksWithoutReplacement()
        {
            var root = new GameObject("foreign-position-test");
            var behavior = ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>();
            try
            {
                var composer = root.AddComponent<CameraRigComposer>();
                SetField(composer, "behaviorDefinition", behavior);
                var camera = root.AddComponent<CinemachineCamera>();
                var foreign = root.AddComponent<CinemachineFollow>();
                composer.EditorSetGeneratedReference(camera);

                var result = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);

                Assert.That(result.Succeeded, Is.False);
                Assert.That(camera.GetComponent<CinemachineFollow>(), Is.SameAs(foreign));
                Assert.That(camera.GetComponent<CinemachineThirdPersonFollow>(), Is.Null);
                Assert.That(composer.FrameworkOwnedPositionControl, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(behavior);
            }
        }

        private static CameraRigBehaviorDefinition CreateBehavior(CameraRigPresentationIntent model)
        {
            switch (model)
            {
                case CameraRigPresentationIntent.Fixed:
                    return ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>();
                case CameraRigPresentationIntent.Follow:
                    var follow = ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
                    SetField(follow, "lookAtRequirement", CameraTargetRequirement.Required);
                    return follow;
                case CameraRigPresentationIntent.Mounted:
                    return ScriptableObject.CreateInstance<MountedCameraRigBehaviorDefinition>();
                case CameraRigPresentationIntent.ThirdPerson:
                    return ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>();
                default:
                    return null;
            }
        }

        private static void SetField<T>(object target, string name, T value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
