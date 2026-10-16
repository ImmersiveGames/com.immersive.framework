using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraRigPreviewTests
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> _created = new List<Object>();
        private readonly List<string> _createdAssetPaths = new List<string>();
        private CameraRigComposer _previewOwner;

        [TearDown]
        public void TearDown()
        {
            if (_previewOwner != null)
                CameraRigPreviewController.TryDisablePreview(_previewOwner, out _);
            _previewOwner = null;

            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
            foreach (string assetPath in _createdAssetPaths)
                AssetDatabase.DeleteAsset(assetPath);
            _createdAssetPaths.Clear();
            Undo.ClearAll();
        }

        [Test]
        public void LegacyFollowDefinition_DefaultsToFixedOffsetAndKeepsExistingOffset()
        {
            FollowCameraRigBehaviorDefinition behavior =
                ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            _created.Add(behavior);

            Assert.That(behavior.FollowMode, Is.EqualTo(FollowCameraRigMode.FixedOffset));
            Assert.That(behavior.FollowOffset, Is.EqualTo(new Vector3(0f, 5f, -8f)));
            Assert.That(behavior.LookAtRequirement, Is.EqualTo(Immersive.Framework.Camera.CameraTargetRequirement.Optional));
        }

        [TestCase(8f, 30f, 45f)]
        [TestCase(12.5f, -20f, -135f)]
        public void FramingControls_ConvertDistancePitchYawToFixedOffsetConsistently(
            float distance,
            float pitch,
            float yaw)
        {
            Vector3 offset = CameraRigFramingEditorUtility.ToFollowOffset(distance, pitch, yaw);

            Assert.That(CameraRigFramingEditorUtility.GetDistance(offset), Is.EqualTo(distance).Within(0.0001f));
            Assert.That(CameraRigFramingEditorUtility.GetPitch(offset), Is.EqualTo(pitch).Within(0.001f));
            Assert.That(Mathf.DeltaAngle(CameraRigFramingEditorUtility.GetYaw(offset), yaw), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ApplyOrRebuild_ScreenComposerModeMaterializesNativePositionComposerWithoutAimControl()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);

            CameraRigComposerApplyRebuildResult result =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);

            Assert.That(result.Succeeded, Is.True, result.BlockingIssue);
            CinemachinePositionComposer position =
                composer.CinemachineCamera.GetComponent<CinemachinePositionComposer>();
            Assert.That(position, Is.Not.Null);
            Assert.That(position.CameraDistance, Is.EqualTo(12.5f));
            Assert.That(position.Damping, Is.EqualTo(new Vector3(0.2f, 0.6f, 0.8f)));
            Assert.That(position.Composition.ScreenPosition, Is.EqualTo(new Vector2(0.12f, -0.08f)));
            Assert.That(composer.CinemachineCamera.GetComponent<CinemachineHardLookAt>(), Is.Null);
        }

        [TestCase(FollowCameraRigMode.FixedOffset, typeof(CinemachineFollow))]
        [TestCase(FollowCameraRigMode.ScreenComposer, typeof(CinemachinePositionComposer))]
        public void EnablePreview_UsesOriginalCameraAndCreatesOnlyTemporaryOutput(
            FollowCameraRigMode mode,
            System.Type expectedControllerType)
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            SetField(behavior, "followMode", mode);
            CameraRigComposer composer = CreateComposer(behavior);

            CameraRigComposerApplyRebuildResult apply =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
            Assert.That(apply.Succeeded, Is.True, apply.BlockingIssue);
            CinemachineCamera sourceCamera = composer.CinemachineCamera;
            Component sourceController = sourceCamera.GetComponent(expectedControllerType);
            Assert.That(sourceController, Is.Not.Null);

            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            CinemachineCamera previewCamera = CameraRigPreviewController.GetPreviewCamera(composer);
            Assert.That(previewCamera, Is.SameAs(sourceCamera));
            Assert.That(previewCamera.GetComponent(expectedControllerType), Is.SameAs(sourceController));

            int bodyControlCount = 0;
            foreach (CinemachineComponentBase component in previewCamera.GetComponents<CinemachineComponentBase>())
                if (component != null && component.Stage == CinemachineCore.Stage.Body)
                    bodyControlCount++;
            Assert.That(bodyControlCount, Is.EqualTo(1));

            GameObject outputRoot = CameraRigPreviewController.GetPreviewRoot(composer);
            Assert.That(outputRoot, Is.Not.Null);
            Assert.That(outputRoot.transform.IsChildOf(composer.transform), Is.True,
                "The temporary preview output should appear below its owning rig in the Hierarchy.");
            Assert.That(outputRoot.GetComponentInChildren<CinemachineCamera>(true), Is.Null);
            Assert.That(outputRoot.GetComponentInChildren<CameraRigComposer>(true), Is.Null);
            Assert.That(outputRoot.GetComponentInChildren<Camera>(true), Is.Not.Null);
            Assert.That(outputRoot.GetComponentInChildren<CinemachineBrain>(true), Is.Not.Null);

            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);
            Assert.That(composer.CinemachineCamera, Is.SameAs(sourceCamera));
            Assert.That(sourceCamera.GetComponent(expectedControllerType), Is.SameAs(sourceController));
        }

        [TestCase(FollowCameraRigMode.FixedOffset)]
        [TestCase(FollowCameraRigMode.ScreenComposer)]
        public void FramingControls_UpdateTheOriginalCameraComponentsDuringPreview(FollowCameraRigMode mode)
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            SetField(behavior, "followMode", mode);
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            if (mode == FollowCameraRigMode.ScreenComposer)
            {
                CinemachinePositionComposer position =
                    CameraRigPreviewController.GetPreviewCamera(composer).GetComponent<CinemachinePositionComposer>();
                ScreenComposerSettings composition = position.Composition;
                composition.HardLimits.Size = new Vector2(0.8f, 0.8f);
                position.Composition = composition;
            }

            Assert.That(CameraRigPreviewController.TryUpdatePreviewFollowControls(
                composer,
                9.5f,
                18f,
                42f,
                63f,
                1.4f,
                new Vector2(0.2f, 0.28f),
                new Vector2(0.5f, 0.7f),
                new Vector2(0.1f, -0.15f),
                out issue), Is.True, issue);

            CinemachineCamera camera = CameraRigPreviewController.GetPreviewCamera(composer);
            Assert.That(camera, Is.SameAs(composer.CinemachineCamera));
            Assert.That(camera.Lens.FieldOfView, Is.EqualTo(63f));
            if (mode == FollowCameraRigMode.FixedOffset)
            {
                Assert.That(camera.GetComponent<CinemachineFollow>().FollowOffset,
                    Is.EqualTo(CameraRigFramingEditorUtility.ToFollowOffset(9.5f, 18f, 42f)));
            }
            else
            {
                CinemachinePositionComposer position = camera.GetComponent<CinemachinePositionComposer>();
                Assert.That(position.CameraDistance, Is.EqualTo(9.5f));
                Assert.That(position.TargetOffset.y, Is.EqualTo(1.4f));
                Assert.That(position.Composition.DeadZone.Size, Is.EqualTo(new Vector2(0.2f, 0.28f)));
                Assert.That(position.Damping, Is.EqualTo(new Vector3(0.5f, 0.7f, behavior.Damping.z)));
                Assert.That(position.Composition.ScreenPosition, Is.EqualTo(new Vector2(0.1f, -0.15f)));
            }
            Assert.That(behavior.CameraDistance, Is.EqualTo(12.5f),
                "Preview controls must not persist Behavior Definition changes before Save Configuration.");
        }

        [Test]
        public void EnablePreview_BlocksExternalOrbitalFollowWithoutChangingSourceRig()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            SetField(behavior, "followMode", FollowCameraRigMode.FixedOffset);
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);

            CinemachineCamera sourceCamera = composer.CinemachineCamera;
            CinemachineOrbitalFollow orbitalFollow =
                sourceCamera.gameObject.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.Radius = 8.25f;
            string sourceCameraBefore = EditorJsonUtility.ToJson(sourceCamera);
            string orbitalFollowBefore = EditorJsonUtility.ToJson(orbitalFollow);

            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.False);
            Assert.That(issue, Does.Contain("external-or-unknown-conflict"));
            Assert.That(EditorJsonUtility.ToJson(sourceCamera), Is.EqualTo(sourceCameraBefore));
            Assert.That(EditorJsonUtility.ToJson(orbitalFollow), Is.EqualTo(orbitalFollowBefore));
            Assert.That(sourceCamera.GetComponent<CinemachineOrbitalFollow>(), Is.SameAs(orbitalFollow));
            Assert.That(orbitalFollow.Radius, Is.EqualTo(8.25f));
            Assert.That(CameraRigPreviewController.IsPreviewActive(composer), Is.False);
        }

        [Test]
        public void PreviewControls_KeepCompatibleExternalFollowReadOnly()
        {
            FollowCameraRigBehaviorDefinition behavior =
                ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            _created.Add(behavior);
            GameObject root = new GameObject("External Follow Rig");
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", behavior);
            _previewOwner = composer;
            GameObject cameraObject = new GameObject("External Cinemachine Camera");
            cameraObject.transform.SetParent(root.transform, false);
            CinemachineCamera camera = cameraObject.AddComponent<CinemachineCamera>();
            CinemachineFollow follow = cameraObject.AddComponent<CinemachineFollow>();
            SetField(composer, "cinemachineCamera", camera);

            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Vector3 originalOffset = follow.FollowOffset;
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.TryUpdatePreviewFollowControls(
                composer,
                14f,
                20f,
                30f,
                60f,
                originalOffset.y,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                out issue), Is.False);
            Assert.That(issue, Does.Contain("External / Unknown"));
            Assert.That(follow.FollowOffset, Is.EqualTo(originalOffset));
            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.False);
            Assert.That(issue, Does.Contain("External / Unknown"));
            Assert.That(follow.FollowOffset, Is.EqualTo(originalOffset));
        }

        [Test]
        public void Preview_CapturesAuthoringValuesRebuildsAndRestoresNormalBrainChannelsOnRemoval()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);

            GameObject targetObject = new GameObject("Preview Target");
            _created.Add(targetObject);
            GameObject outputObject = new GameObject("Existing Camera Output");
            _created.Add(outputObject);
            CinemachineBrain existingBrain = outputObject.AddComponent<CinemachineBrain>();
            OutputChannels originalMask = existingBrain.ChannelMask;
            CinemachineCamera sourceCamera = composer.CinemachineCamera;
            OutputChannels originalCameraChannel = sourceCamera.OutputChannel;

            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string composeIssue), Is.True, composeIssue);
            Assert.That(CameraRigPreviewController.SetPreviewTarget(composer, targetObject.transform, out string targetIssue), Is.True, targetIssue);

            CinemachineCamera previewCamera = CameraRigPreviewController.GetPreviewCamera(composer);
            Assert.That(previewCamera, Is.Not.Null);
            Assert.That(previewCamera.Follow, Is.SameAs(targetObject.transform));
            Assert.That(CameraRigPreviewController.HasUncapturedChanges(composer), Is.False);

            CinemachinePositionComposer previewPosition =
                previewCamera.GetComponent<CinemachinePositionComposer>();
            previewPosition.CameraDistance = 15.25f;
            previewPosition.Damping = new Vector3(0.4f, 0.9f, 1.2f);
            ScreenComposerSettings composition = previewPosition.Composition;
            composition.ScreenPosition = new Vector2(-0.2f, 0.18f);
            composition.DeadZone.Enabled = true;
            composition.DeadZone.Size = new Vector2(0.24f, 0.32f);
            composition.HardLimits.Enabled = true;
            composition.HardLimits.Size = new Vector2(0.84f, 0.92f);
            previewPosition.Composition = composition;

            Assert.That(CameraRigPreviewController.HasUncapturedChanges(composer), Is.True);
            Assert.That(CameraRigPreviewController.TryCapture(composer, out string captureIssue), Is.True, captureIssue);
            Assert.That(behavior.CameraDistance, Is.EqualTo(15.25f));
            Assert.That(behavior.Damping, Is.EqualTo(new Vector3(0.4f, 0.9f, 1.2f)));
            Assert.That(behavior.Composition.ScreenPosition, Is.EqualTo(new Vector2(-0.2f, 0.18f)));
            Assert.That(behavior.Composition.DeadZone.Enabled, Is.True);
            Assert.That(behavior.Composition.HardLimits.Enabled, Is.True);
            Assert.That(composer.CinemachineCamera.Follow, Is.SameAs(targetObject.transform),
                "Capture must leave the preview binding active until Disable restores it.");
            Assert.That(CameraRigPreviewController.HasUncapturedChanges(composer), Is.False);

            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out string removeIssue), Is.True, removeIssue);
            Assert.That(existingBrain.ChannelMask, Is.EqualTo(originalMask));
            Assert.That(sourceCamera.OutputChannel, Is.EqualTo(originalCameraChannel));
            Assert.That(sourceCamera.Follow, Is.Null,
                "Disable must restore the original target after the temporary scene target is removed.");

            CameraRigComposerApplyRebuildResult rebuild =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
            Assert.That(rebuild.Succeeded, Is.True, rebuild.BlockingIssue);
            CinemachinePositionComposer rebuilt =
                composer.CinemachineCamera.GetComponent<CinemachinePositionComposer>();
            Assert.That(rebuilt.CameraDistance, Is.EqualTo(15.25f));
            Assert.That(rebuilt.Damping, Is.EqualTo(new Vector3(0.4f, 0.9f, 1.2f)));
            Assert.That(rebuilt.Composition.ScreenPosition, Is.EqualTo(new Vector2(-0.2f, 0.18f)));
        }

        [Test]
        public void SaveConfiguration_PersistsScreenComposerEditsAndLeavesPreviewBindingsTransient()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);

            var targetObject = new GameObject("Unsaved Preview Target");
            _created.Add(targetObject);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.SetPreviewTarget(composer, targetObject.transform, out issue), Is.True, issue);

            CinemachineCamera camera = CameraRigPreviewController.GetPreviewCamera(composer);
            CinemachinePositionComposer position = camera.GetComponent<CinemachinePositionComposer>();
            position.CameraDistance = 17.5f;
            position.Damping = new Vector3(0.45f, 0.85f, 1.1f);
            ScreenComposerSettings composition = position.Composition;
            composition.DeadZone.Size = new Vector2(0.2f, 0.3f);
            position.Composition = composition;
            LensSettings lens = camera.Lens;
            lens.FieldOfView = 61f;
            camera.Lens = lens;

            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.True, issue);
            Assert.That(behavior.CameraDistance, Is.EqualTo(17.5f));
            Assert.That(behavior.Damping, Is.EqualTo(new Vector3(0.45f, 0.85f, 1.1f)));
            Assert.That(behavior.Composition.DeadZone.Size, Is.EqualTo(new Vector2(0.2f, 0.3f)));
            Assert.That(CameraRigPreviewController.HasUncapturedChanges(composer), Is.False);
            Assert.That(camera.Follow, Is.SameAs(targetObject.transform));

            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);
            Assert.That(camera.Follow, Is.Null, "The scene Target must not be captured into the runtime CinemachineCamera.");
            Assert.That(CameraRigComposerApplyRebuildUtility.Validate(composer).Succeeded, Is.True);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(composer.CinemachineCamera.GetComponent<CinemachinePositionComposer>().CameraDistance,
                Is.EqualTo(17.5f));
        }

        [Test]
        public void SaveConfiguration_UpdatesTheExistingSharedBehaviorReference()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer first = CreateComposer(behavior);
            CameraRigComposer second = CreateComposer(behavior);
            _previewOwner = first;
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(first, false, false).Succeeded, Is.True);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(second, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(first, out string issue), Is.True, issue);

            CinemachinePositionComposer position =
                CameraRigPreviewController.GetPreviewCamera(first).GetComponent<CinemachinePositionComposer>();
            position.CameraDistance = 21f;

            Assert.That(CameraRigPreviewController.TrySaveConfiguration(first, out issue), Is.True, issue);
            Assert.That(second.BehaviorDefinition, Is.SameAs(first.BehaviorDefinition));
            Assert.That(behavior.CameraDistance, Is.EqualTo(21f));
        }

        [TestCase(CameraRigPreviewPlayModeDecision.SaveAndPlay)]
        [TestCase(CameraRigPreviewPlayModeDecision.DiscardAndPlay)]
        [TestCase(CameraRigPreviewPlayModeDecision.CancelPlayMode)]
        public void PlayModeDecision_ResolvesSaveDiscardAndCancelSafely(
            CameraRigPreviewPlayModeDecision decision)
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            CinemachineCamera camera = CameraRigPreviewController.GetPreviewCamera(composer);
            CinemachinePositionComposer position = camera.GetComponent<CinemachinePositionComposer>();
            position.CameraDistance = 19f;

            bool proceed = CameraRigPreviewController.TryResolvePlayModeDecision(
                composer,
                decision,
                out issue);

            if (decision == CameraRigPreviewPlayModeDecision.CancelPlayMode)
            {
                Assert.That(proceed, Is.False);
                Assert.That(CameraRigPreviewController.IsPreviewActive(composer), Is.True);
                Assert.That(behavior.CameraDistance, Is.EqualTo(12.5f));
                Assert.That(position.CameraDistance, Is.EqualTo(19f));
                return;
            }

            Assert.That(proceed, Is.True, issue);
            Assert.That(CameraRigPreviewController.IsPreviewActive(composer), Is.False);
            Assert.That(behavior.CameraDistance,
                Is.EqualTo(decision == CameraRigPreviewPlayModeDecision.SaveAndPlay ? 19f : 12.5f));
            Assert.That(composer.CinemachineCamera.GetComponent<CinemachinePositionComposer>().CameraDistance,
                Is.EqualTo(decision == CameraRigPreviewPlayModeDecision.SaveAndPlay ? 19f : 12.5f));
        }

        [Test]
        public void SaveConfiguration_RegistersBehaviorChangesWithUndoAndRedo()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            CinemachinePositionComposer previewPosition =
                CameraRigPreviewController.GetPreviewCamera(composer).GetComponent<CinemachinePositionComposer>();
            previewPosition.CameraDistance = 19f;

            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.True, issue);
            Undo.FlushUndoRecordObjects();
            Assert.That(behavior.CameraDistance, Is.EqualTo(19f));

            Undo.PerformUndo();
            Assert.That(behavior.CameraDistance, Is.EqualTo(12.5f));
            Undo.PerformRedo();
            Assert.That(behavior.CameraDistance, Is.EqualTo(19f));
        }

        [Test]
        public void SaveConfiguration_AppliesOnlyAuthoredValuesAndNotThePreviewTargetToPrefab()
        {
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/__CameraRigPreviewPersistenceTest.prefab");
            _createdAssetPaths.Add(prefabPath);

            var sourceRoot = new GameObject("Prefab Camera Rig");
            sourceRoot.AddComponent<CameraRigComposer>();
            Assert.That(PrefabUtility.SaveAsPrefabAsset(sourceRoot, prefabPath), Is.Not.Null);
            Object.DestroyImmediate(sourceRoot);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            Assert.That(instance, Is.Not.Null);
            _created.Add(instance);
            CameraRigComposer composer = instance.GetComponent<CameraRigComposer>();
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            SetField(composer, "behaviorDefinition", behavior);
            _previewOwner = composer;
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);

            CinemachineCamera camera = composer.CinemachineCamera;
            OutputChannels sourceChannel = camera.OutputChannel;
            var targetObject = new GameObject("Prefab Preview Target");
            _created.Add(targetObject);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.SetPreviewTarget(composer, targetObject.transform, out issue), Is.True, issue);
            camera.GetComponent<CinemachinePositionComposer>().CameraDistance = 18.5f;

            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);
            Assert.That(camera.Follow, Is.Null);
            Assert.That(camera.OutputChannel, Is.EqualTo(sourceChannel));

            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            CinemachineCamera savedCamera = prefabAsset.GetComponentInChildren<CinemachineCamera>(true);
            Assert.That(savedCamera, Is.Not.Null);
            Assert.That(savedCamera.GetComponent<CinemachinePositionComposer>().CameraDistance,
                Is.EqualTo(18.5f));
            Assert.That(savedCamera.Follow, Is.Null,
                "Save Configuration must not serialize the scene-only Preview Target into the source prefab.");
            Assert.That(savedCamera.OutputChannel, Is.EqualTo(sourceChannel),
                "The Preview channel reservation must remain transient.");
        }

        [TestCase(0f)]
        [TestCase(55f)]
        public void SaveConfiguration_PersistsCameraPoseRelativeToAuthoredRigRoot(
            float authoredRootPitch)
        {
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/__CameraRigPosePersistenceTest.prefab");
            _createdAssetPaths.Add(prefabPath);

            var sourceRoot = new GameObject("Prefab Camera Rig With Authored Root Rotation");
            sourceRoot.transform.localRotation = Quaternion.Euler(authoredRootPitch, 0f, 0f);
            sourceRoot.AddComponent<CameraRigComposer>();
            Assert.That(PrefabUtility.SaveAsPrefabAsset(sourceRoot, prefabPath), Is.Not.Null);
            Object.DestroyImmediate(sourceRoot);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            Assert.That(instance, Is.Not.Null);
            _created.Add(instance);

            CameraRigComposer composer = instance.GetComponent<CameraRigComposer>();
            _previewOwner = composer;
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            SetField(composer, "behaviorDefinition", behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            CinemachinePositionComposer position =
                CameraRigPreviewController.GetPreviewCamera(composer).GetComponent<CinemachinePositionComposer>();
            ScreenComposerSettings composition = position.Composition;
            Assert.That(CameraRigPreviewController.TryUpdatePreviewFollowControls(
                composer,
                30f,
                30f,
                0f,
                50f,
                position.TargetOffset.y,
                composition.DeadZone.Size,
                new Vector2(position.Damping.x, position.Damping.y),
                composition.ScreenPosition,
                out issue), Is.True, issue);

            Quaternion previewCameraWorldRotation = composer.CinemachineCamera.transform.rotation;
            Quaternion expectedWorldRotation =
                Quaternion.Euler(authoredRootPitch, 0f, 0f) * Quaternion.Euler(30f, 0f, 0f);
            Assert.That(Quaternion.Angle(previewCameraWorldRotation, expectedWorldRotation),
                Is.LessThan(0.01f));
            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);

            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            CameraRigComposer savedComposer = prefabAsset.GetComponent<CameraRigComposer>();
            CinemachineCamera savedCamera = prefabAsset.GetComponentInChildren<CinemachineCamera>(true);
            Assert.That(Quaternion.Angle(
                    prefabAsset.transform.localRotation,
                    Quaternion.Euler(authoredRootPitch, 0f, 0f)),
                Is.LessThan(0.01f));
            Assert.That(savedComposer, Is.Not.Null);
            Assert.That(savedCamera, Is.Not.Null);
            Assert.That(savedCamera.GetComponent<CinemachinePositionComposer>().CameraDistance,
                Is.EqualTo(30f).Within(0.001f));
            Assert.That(savedCamera.Lens.FieldOfView, Is.EqualTo(50f).Within(0.001f));
            Vector3 savedCameraPose = (Vector3)typeof(CameraRigComposer)
                .GetField("capturedCameraLocalEulerAngles", PrivateInstance)
                .GetValue(savedComposer);
            Assert.That(Mathf.DeltaAngle(savedCameraPose.x, 30f), Is.EqualTo(0f).Within(0.01f));

            var runtimeClone = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            Assert.That(runtimeClone, Is.Not.Null);
            _created.Add(runtimeClone);
            CinemachineCamera runtimeCamera = runtimeClone.GetComponentInChildren<CinemachineCamera>(true);
            Assert.That(Quaternion.Angle(runtimeCamera.transform.rotation, previewCameraWorldRotation),
                Is.LessThan(0.01f),
                "The persisted rig frame and camera-local pose must reproduce the Preview orientation when instantiated.");
        }

        [Test]
        public void SaveConfiguration_DoesNotPromoteSceneRigRotationOverrideIntoPrefabPose()
        {
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/__CameraRigSceneRootOverrideTest.prefab");
            _createdAssetPaths.Add(prefabPath);

            var sourceRoot = new GameObject("Prefab Camera Rig With Identity Root");
            sourceRoot.AddComponent<CameraRigComposer>();
            Assert.That(PrefabUtility.SaveAsPrefabAsset(sourceRoot, prefabPath), Is.Not.Null);
            Object.DestroyImmediate(sourceRoot);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            Assert.That(instance, Is.Not.Null);
            _created.Add(instance);
            instance.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

            CameraRigComposer composer = instance.GetComponent<CameraRigComposer>();
            _previewOwner = composer;
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            SetField(composer, "behaviorDefinition", behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            CinemachinePositionComposer position =
                CameraRigPreviewController.GetPreviewCamera(composer).GetComponent<CinemachinePositionComposer>();
            ScreenComposerSettings composition = position.Composition;
            Assert.That(CameraRigPreviewController.TryUpdatePreviewFollowControls(
                composer,
                30f,
                30f,
                0f,
                50f,
                position.TargetOffset.y,
                composition.DeadZone.Size,
                new Vector2(position.Damping.x, position.Damping.y),
                composition.ScreenPosition,
                out issue), Is.True, issue);
            Assert.That(Quaternion.Angle(
                    composer.CinemachineCamera.transform.rotation,
                    Quaternion.Euler(85f, 0f, 0f)),
                Is.LessThan(0.01f),
                "Preview combines the scene rig rotation with the camera-local pitch.");

            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);

            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            CameraRigComposer savedComposer = prefabAsset.GetComponent<CameraRigComposer>();
            CinemachineCamera savedCamera = prefabAsset.GetComponentInChildren<CinemachineCamera>(true);
            Assert.That(savedComposer, Is.Not.Null);
            Assert.That(savedCamera, Is.Not.Null);
            Assert.That(Quaternion.Angle(prefabAsset.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f),
                "A scene instance rotation is not part of the source prefab's authored camera pose.");
            Vector3 savedCameraPose = (Vector3)typeof(CameraRigComposer)
                .GetField("capturedCameraLocalEulerAngles", PrivateInstance)
                .GetValue(savedComposer);
            Assert.That(Mathf.DeltaAngle(savedCameraPose.x, 30f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(savedCamera.GetComponent<CinemachinePositionComposer>().CameraDistance,
                Is.EqualTo(30f).Within(0.001f));
            Assert.That(savedCamera.Lens.FieldOfView, Is.EqualTo(50f).Within(0.001f));

            var runtimeClone = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            Assert.That(runtimeClone, Is.Not.Null);
            _created.Add(runtimeClone);
            CinemachineCamera runtimeCamera = runtimeClone.GetComponentInChildren<CinemachineCamera>(true);
            Assert.That(Quaternion.Angle(runtimeCamera.transform.rotation, Quaternion.Euler(30f, 0f, 0f)),
                Is.LessThan(0.01f),
                "The runtime prefab uses its authored root rotation and the newly captured camera-local pitch.");
        }

        [Test]
        public void PreviewObjects_AreMarkedDontSaveAndCanBeRemovedWithoutCapturing()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);

            CinemachinePositionComposer sourcePosition =
                composer.CinemachineCamera.GetComponent<CinemachinePositionComposer>();
            float originalDistance = sourcePosition.CameraDistance;
            Vector3 originalDamping = sourcePosition.Damping;
            Vector3 originalCameraPosition = composer.CinemachineCamera.transform.localPosition;
            LensSettings originalLens = composer.CinemachineCamera.Lens;

            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);
            GameObject previewRoot = CameraRigPreviewController.GetPreviewRoot(composer);
            Assert.That(previewRoot, Is.Not.Null);
            Assert.That((previewRoot.hideFlags & HideFlags.DontSave) == HideFlags.DontSave, Is.True);
            Assert.That(CameraRigPreviewController.GetPreviewCamera(composer), Is.SameAs(composer.CinemachineCamera));
            sourcePosition.CameraDistance = 18f;
            sourcePosition.Damping = new Vector3(2f, 3f, 4f);
            composer.CinemachineCamera.transform.localPosition += Vector3.one;
            LensSettings changedLens = composer.CinemachineCamera.Lens;
            changedLens.FieldOfView = 65f;
            composer.CinemachineCamera.Lens = changedLens;
            Assert.That(previewRoot.GetComponentInChildren<Camera>(), Is.Not.Null);
            Assert.That(previewRoot.GetComponentInChildren<CinemachineCamera>(), Is.Null);
            Assert.That(previewRoot.GetComponentInChildren<CinemachineBrain>(), Is.Not.Null);
            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.GetPreviewRoot(composer), Is.Null);
            Assert.That(sourcePosition.CameraDistance, Is.EqualTo(originalDistance));
            Assert.That(sourcePosition.Damping, Is.EqualTo(originalDamping));
            Assert.That(composer.CinemachineCamera.transform.localPosition, Is.EqualTo(originalCameraPosition));
            Assert.That(composer.CinemachineCamera.Lens.FieldOfView, Is.EqualTo(originalLens.FieldOfView));
            Assert.That(behavior.CameraDistance, Is.EqualTo(12.5f));
        }

        [Test]
        public void FixedOffsetSaveConfiguration_RebuildsOffsetWithoutPersistingTheSceneTarget()
        {
            FollowCameraRigBehaviorDefinition behavior =
                ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            _created.Add(behavior);
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            GameObject targetObject = new GameObject("Fixed Offset Preview Target");
            _created.Add(targetObject);

            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.SetPreviewTarget(composer, targetObject.transform, out issue), Is.True, issue);

            CinemachineFollow previewFollow =
                CameraRigPreviewController.GetPreviewCamera(composer).GetComponent<CinemachineFollow>();
            previewFollow.FollowOffset = new Vector3(2f, 6f, -11f);

            Assert.That(CameraRigPreviewController.TrySaveConfiguration(composer, out issue), Is.True, issue);
            Assert.That(behavior.FollowOffset, Is.EqualTo(new Vector3(2f, 6f, -11f)));
            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(composer.CinemachineCamera.GetComponent<CinemachineFollow>().FollowOffset,
                Is.EqualTo(new Vector3(2f, 6f, -11f)));
            Assert.That(composer.CinemachineCamera.Follow, Is.Null,
                "The selected preview Transform is not authored as the runtime target.");
        }

        [Test]
        public void Capture_RigRelativeCameraPoseAndOrthographicLensRebuildOnNestedCamera()
        {
            FixedCameraRigBehaviorDefinition behavior =
                ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>();
            _created.Add(behavior);

            GameObject root = new GameObject("Nested Camera Rig");
            root.transform.SetPositionAndRotation(new Vector3(4f, 3f, -2f), Quaternion.Euler(0f, 35f, 0f));
            _created.Add(root);
            Transform parent = new GameObject("Camera Parent").transform;
            parent.SetParent(root.transform, false);
            parent.localPosition = new Vector3(1f, 2f, 3f);
            parent.localRotation = Quaternion.Euler(0f, -15f, 0f);
            GameObject cameraObject = new GameObject("Cinemachine Camera");
            cameraObject.transform.SetParent(parent, false);
            CinemachineCamera sourceCamera = cameraObject.AddComponent<CinemachineCamera>();
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", behavior);
            SetField(composer, "cinemachineCamera", sourceCamera);
            _previewOwner = composer;
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);

            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);
            CinemachineCamera previewCamera = CameraRigPreviewController.GetPreviewCamera(composer);
            previewCamera.transform.localPosition = new Vector3(-2f, 4f, 7f);
            previewCamera.transform.localEulerAngles = new Vector3(12f, 22f, -7f);
            LensSettings lens = previewCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = 8.5f;
            lens.NearClipPlane = 0.25f;
            lens.FarClipPlane = 800f;
            previewCamera.Lens = lens;

            Vector3 expectedWorldPosition = previewCamera.transform.position;
            Quaternion expectedWorldRotation = previewCamera.transform.rotation;
            Assert.That(CameraRigPreviewController.TryCapture(composer, out issue), Is.True, issue);
            Assert.That(CameraRigPreviewController.TryDisablePreview(composer, out issue), Is.True, issue);

            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(Vector3.Distance(sourceCamera.transform.position, expectedWorldPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(sourceCamera.transform.rotation, expectedWorldRotation), Is.LessThan(0.01f));
            Assert.That(sourceCamera.Lens.ModeOverride, Is.EqualTo(LensSettings.OverrideModes.Orthographic));
            Assert.That(sourceCamera.Lens.OrthographicSize, Is.EqualTo(8.5f));
            Assert.That(sourceCamera.Lens.NearClipPlane, Is.EqualTo(0.25f));
            Assert.That(sourceCamera.Lens.FarClipPlane, Is.EqualTo(800f));
        }

        [Test]
        public void Capture_RegistersBehaviorChangesWithUndoAndRedo()
        {
            FollowCameraRigBehaviorDefinition behavior = CreateScreenComposerBehavior();
            CameraRigComposer composer = CreateComposer(behavior);
            Assert.That(CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false).Succeeded, Is.True);
            Assert.That(CameraRigPreviewController.TryEnablePreview(composer, out string issue), Is.True, issue);

            CinemachinePositionComposer previewPosition =
                CameraRigPreviewController.GetPreviewCamera(composer).GetComponent<CinemachinePositionComposer>();
            previewPosition.CameraDistance = 19f;

            Assert.That(CameraRigPreviewController.TryCapture(composer, out issue), Is.True, issue);
            Undo.FlushUndoRecordObjects();
            Assert.That(behavior.CameraDistance, Is.EqualTo(19f));

            Undo.PerformUndo();
            Assert.That(behavior.CameraDistance, Is.EqualTo(12.5f));
            Undo.PerformRedo();
            Assert.That(behavior.CameraDistance, Is.EqualTo(19f));
        }

        private FollowCameraRigBehaviorDefinition CreateScreenComposerBehavior()
        {
            FollowCameraRigBehaviorDefinition behavior =
                ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>();
            _created.Add(behavior);
            SetField(behavior, "followMode", FollowCameraRigMode.ScreenComposer);
            SetField(behavior, "cameraDistance", 12.5f);
            SetField(behavior, "damping", new Vector3(0.2f, 0.6f, 0.8f));
            ScreenComposerSettings composition = ScreenComposerSettings.Default;
            composition.ScreenPosition = new Vector2(0.12f, -0.08f);
            SetField(behavior, "composition", composition);
            return behavior;
        }

        private CameraRigComposer CreateComposer(FollowCameraRigBehaviorDefinition behavior)
        {
            GameObject root = new GameObject("Camera Rig");
            _created.Add(root);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", behavior);
            _previewOwner = composer;
            return composer;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        }
    }
}
