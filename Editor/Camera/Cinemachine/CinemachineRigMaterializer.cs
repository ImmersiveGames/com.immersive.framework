using Immersive.Framework.Camera;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Editor-only utility that creates or repairs one supported Cinemachine
    /// presentation pipeline. Target resolution and Camera output arbitration are
    /// owned elsewhere; this materializer operates only on the supplied local rig.
    /// </summary>
    public static class CinemachineRigMaterializer
    {
        private enum PositionControlKind
        {
            None = 0,
            Follow = 10,
            HardLockToTarget = 20,
            ThirdPersonFollow = 30
        }

        private enum RotationControlKind
        {
            None = 0,
            HardLookAt = 10,
            RotateWithFollowTarget = 20
        }

        private sealed class StagePreflight
        {
            public CinemachineComponentBase FrameworkOwned { get; set; }

            public CinemachineComponentBase ExternalCompatible { get; set; }

            public bool Blocked { get; set; }
        }

        public static CinemachineRigMaterializationReport ApplyOrRebuild(
            CinemachineRigMaterializationRequest request)
        {
            var report = new CinemachineRigMaterializationReport();

            if (request == null)
            {
                report.MarkBlocked("request:null");
                return report;
            }

            report.Evidence.PresentationIntent =
                request.PresentationIntent;

            ValidateRequest(request, report);
            if (report.BlockedCount > 0)
            {
                return report;
            }

            UnityEngine.Camera unityCamera = null;
            CinemachineBrain brain = null;

            if (request.MaterializeUnityOutput)
            {
                unityCamera = ResolveUnityCamera(request, report);
                brain = EnsureBrain(unityCamera, request, report);

                if (unityCamera == null)
                {
                    report.MarkBlocked("unity-camera:missing");
                }

                if (brain == null)
                {
                    report.MarkBlocked("cinemachine-brain:missing");
                }
            }
            else
            {
                report.MarkSkipped("unity-output:not-requested");
                report.MarkSkipped("cinemachine-brain:not-requested");
            }

            CinemachineCamera cinemachineCamera =
                ResolveCinemachineCamera(request, report);

            if (cinemachineCamera == null)
            {
                report.MarkBlocked("cinemachine-camera:missing");
            }

            if (report.BlockedCount > 0)
            {
                return report;
            }

            ResolveDesiredPipeline(
                request,
                out PositionControlKind desiredPosition,
                out RotationControlKind desiredRotation);

            StagePreflight positionPreflight =
                PreflightStage(
                    cinemachineCamera,
                    CinemachineCore.Stage.Body,
                    request.FrameworkOwnedPositionControl,
                    component => IsDesiredPositionControl(
                        component,
                        desiredPosition),
                    "position",
                    desiredPosition.ToString(),
                    report);

            StagePreflight rotationPreflight =
                PreflightStage(
                    cinemachineCamera,
                    CinemachineCore.Stage.Aim,
                    request.FrameworkOwnedRotationControl,
                    component => IsDesiredRotationControl(
                        component,
                        desiredRotation),
                    "rotation",
                    desiredRotation.ToString(),
                    report);

            if (desiredPosition == PositionControlKind.Follow &&
                !request.CreateCinemachineFollowIfMissing &&
                positionPreflight.ExternalCompatible == null &&
                !IsDesiredPositionControl(
                    positionPreflight.FrameworkOwned,
                    desiredPosition))
            {
                positionPreflight.Blocked = true;
                report.MarkBlocked(
                    "cinemachine-follow:create-disabled");
            }

            if (positionPreflight.Blocked ||
                rotationPreflight.Blocked ||
                report.BlockedCount > 0)
            {
                CaptureBlockedStageEvidence(
                    positionPreflight,
                    rotationPreflight,
                    request,
                    report);
                return report;
            }

            CinemachineComponentBase positionControl =
                ReconcilePositionControl(
                    cinemachineCamera,
                    desiredPosition,
                    positionPreflight,
                    request,
                    report);

            CinemachineComponentBase rotationControl =
                ReconcileRotationControl(
                    cinemachineCamera,
                    desiredRotation,
                    rotationPreflight,
                    request,
                    report);

            ConfigurePositionControl(
                positionControl,
                request,
                report);
            ConfigureRotationControl(
                rotationControl,
                request,
                report);

            ApplyTargets(
                cinemachineCamera,
                request,
                report);

            MarkDirty(unityCamera);
            MarkDirty(brain);
            MarkDirty(positionControl);
            MarkDirty(rotationControl);
            MarkDirty(cinemachineCamera);
            EditorUtility.SetDirty(request.RigRoot.gameObject);

            report.Evidence.UnityCamera = unityCamera;
            report.Evidence.Brain = brain;
            report.Evidence.CinemachineCamera = cinemachineCamera;
            report.Evidence.PositionControl = positionControl;
            report.Evidence.PositionControlOwnership =
                ClassifyResolvedOwnership(
                    positionControl,
                    positionPreflight,
                    request.FrameworkOwnedPositionControl);
            report.Evidence.RotationControl = rotationControl;
            report.Evidence.RotationControlOwnership =
                ClassifyResolvedOwnership(
                    rotationControl,
                    rotationPreflight,
                    request.FrameworkOwnedRotationControl);
            report.Evidence.CinemachineFollow =
                positionControl as CinemachineFollow;
            report.Evidence.FollowTarget = cinemachineCamera.Follow;
            report.Evidence.LookAtTarget = cinemachineCamera.LookAt;
            report.Evidence.MaterializationRevision =
                NextRevision(request.PreviousMaterializationRevision);

            return report;
        }

        private static void ValidateRequest(
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (request.RigRoot == null)
            {
                report.MarkBlocked("rig-root:null");
                return;
            }

            if (request.RequireFollowTarget &&
                request.FollowTarget == null)
            {
                report.MarkBlocked("follow-target:required-missing");
            }

            if (request.RequireLookAtTarget &&
                request.LookAtTarget == null)
            {
                report.MarkBlocked("look-at-target:required-missing");
            }

            switch (request.PresentationIntent)
            {
                case CameraRigPresentationIntent.Fixed:
                    if (request.FollowTarget != null)
                    {
                        report.MarkBlocked(
                            "presentation:Fixed:follow-target-must-be-null");
                    }
                    break;

                case CameraRigPresentationIntent.Follow:
                    if (request.FollowTarget == null)
                    {
                        report.MarkBlocked(
                            "presentation:Follow:follow-target-required");
                    }

                    if (!IsFinite(request.FollowOffset))
                    {
                        report.MarkBlocked(
                            "presentation:Follow:follow-offset-invalid");
                    }
                    break;

                case CameraRigPresentationIntent.Mounted:
                    if (request.FollowTarget == null)
                    {
                        report.MarkBlocked(
                            "presentation:Mounted:follow-target-required");
                    }

                    if (request.LookAtTarget != null)
                    {
                        report.MarkBlocked(
                            "presentation:Mounted:look-at-not-supported");
                    }

                    if (!IsFiniteNonNegative(
                            request.MountedPositionDamping) ||
                        !IsFiniteNonNegative(
                            request.MountedRotationDamping))
                    {
                        report.MarkBlocked(
                            "presentation:Mounted:damping-invalid");
                    }
                    break;

                case CameraRigPresentationIntent.ThirdPerson:
                    if (request.FollowTarget == null)
                    {
                        report.MarkBlocked(
                            "presentation:ThirdPerson:follow-target-required");
                    }

                    if (request.LookAtTarget != null)
                    {
                        report.MarkBlocked(
                            "presentation:ThirdPerson:look-at-not-supported");
                    }

                    if (!IsFinite(request.ThirdPersonShoulderOffset) ||
                        !IsFinite(request.ThirdPersonVerticalArmLength) ||
                        !IsFinite(request.ThirdPersonCameraSide) ||
                        request.ThirdPersonCameraSide < 0f ||
                        request.ThirdPersonCameraSide > 1f ||
                        !IsFiniteNonNegative(
                            request.ThirdPersonCameraDistance) ||
                        !IsFiniteNonNegative(
                            request.ThirdPersonDamping))
                    {
                        report.MarkBlocked(
                            "presentation:ThirdPerson:settings-invalid");
                    }
                    break;

                case CameraRigPresentationIntent.Undefined:
                    report.MarkBlocked(
                        "presentation:Undefined:not-supported");
                    break;

                default:
                    report.MarkBlocked(
                        $"presentation:{request.PresentationIntent}:not-supported");
                    break;
            }
        }

        private static void ResolveDesiredPipeline(
            CinemachineRigMaterializationRequest request,
            out PositionControlKind position,
            out RotationControlKind rotation)
        {
            switch (request.PresentationIntent)
            {
                case CameraRigPresentationIntent.Fixed:
                    position = PositionControlKind.None;
                    rotation = request.LookAtTarget != null
                        ? RotationControlKind.HardLookAt
                        : RotationControlKind.None;
                    return;

                case CameraRigPresentationIntent.Follow:
                    position = PositionControlKind.Follow;
                    rotation = request.LookAtTarget != null
                        ? RotationControlKind.HardLookAt
                        : RotationControlKind.None;
                    return;

                case CameraRigPresentationIntent.Mounted:
                    position = PositionControlKind.HardLockToTarget;
                    rotation = RotationControlKind.RotateWithFollowTarget;
                    return;

                case CameraRigPresentationIntent.ThirdPerson:
                    position = PositionControlKind.ThirdPersonFollow;
                    rotation = RotationControlKind.None;
                    return;

                default:
                    position = PositionControlKind.None;
                    rotation = RotationControlKind.None;
                    return;
            }
        }

        private static StagePreflight PreflightStage(
            CinemachineCamera cinemachineCamera,
            CinemachineCore.Stage stage,
            Component frameworkOwnedReference,
            System.Func<CinemachineComponentBase, bool> isDesired,
            string stageLabel,
            string desiredLabel,
            CinemachineRigMaterializationReport report)
        {
            var result = new StagePreflight();
            CinemachineComponentBase[] components =
                cinemachineCamera.GetComponents<CinemachineComponentBase>();

            for (int i = 0; i < components.Length; i++)
            {
                CinemachineComponentBase component = components[i];
                if (component == null ||
                    component.Stage != stage)
                {
                    continue;
                }

                bool frameworkOwned =
                    frameworkOwnedReference != null &&
                    component == frameworkOwnedReference;

                if (frameworkOwned)
                {
                    if (result.FrameworkOwned != null &&
                        result.FrameworkOwned != component)
                    {
                        result.Blocked = true;
                        report.MarkBlocked(
                            $"cinemachine-{stageLabel}-control:ownership-evidence-ambiguous");
                        continue;
                    }

                    result.FrameworkOwned = component;
                    continue;
                }

                if (!isDesired(component))
                {
                    result.Blocked = true;
                    report.MarkBlocked(
                        $"cinemachine-{stageLabel}-control:external-or-unknown-conflict:{TypeName(component)}:desired={desiredLabel}");
                    continue;
                }

                if (result.ExternalCompatible != null &&
                    result.ExternalCompatible != component)
                {
                    result.Blocked = true;
                    report.MarkBlocked(
                        $"cinemachine-{stageLabel}-control:external-or-unknown-duplicate:{TypeName(component)}");
                    continue;
                }

                result.ExternalCompatible = component;
            }

            if (!result.Blocked &&
                result.FrameworkOwned != null &&
                result.ExternalCompatible != null &&
                isDesired(result.FrameworkOwned))
            {
                result.Blocked = true;
                report.MarkBlocked(
                    $"cinemachine-{stageLabel}-control:external-or-unknown-duplicate:{TypeName(result.ExternalCompatible)}");
            }

            return result;
        }

        private static void CaptureBlockedStageEvidence(
            StagePreflight position,
            StagePreflight rotation,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            CinemachineComponentBase positionEvidence =
                position.ExternalCompatible ?? position.FrameworkOwned;
            CinemachineComponentBase rotationEvidence =
                rotation.ExternalCompatible ?? rotation.FrameworkOwned;

            report.Evidence.PositionControl = positionEvidence;
            report.Evidence.PositionControlOwnership =
                ClassifyOwnership(
                    positionEvidence,
                    request.FrameworkOwnedPositionControl);
            report.Evidence.RotationControl = rotationEvidence;
            report.Evidence.RotationControlOwnership =
                ClassifyOwnership(
                    rotationEvidence,
                    request.FrameworkOwnedRotationControl);
            report.Evidence.CinemachineFollow =
                positionEvidence as CinemachineFollow;
        }

        private static CinemachineComponentBase ReconcilePositionControl(
            CinemachineCamera cinemachineCamera,
            PositionControlKind desired,
            StagePreflight preflight,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (preflight.ExternalCompatible != null)
            {
                if (preflight.FrameworkOwned != null)
                {
                    RemoveFrameworkOwnedControl(
                        preflight.FrameworkOwned,
                        "position",
                        request,
                        report);
                }

                report.MarkAlreadyValid(
                    $"cinemachine-position-control:external-compatible:{TypeName(preflight.ExternalCompatible)}");
                return preflight.ExternalCompatible;
            }

            if (desired == PositionControlKind.None)
            {
                if (preflight.FrameworkOwned != null)
                {
                    RemoveFrameworkOwnedControl(
                        preflight.FrameworkOwned,
                        "position",
                        request,
                        report);
                }
                else
                {
                    report.MarkAlreadyValid(
                        "cinemachine-position-control:none");
                }

                return null;
            }

            if (preflight.FrameworkOwned != null &&
                IsDesiredPositionControl(
                    preflight.FrameworkOwned,
                    desired))
            {
                report.MarkAlreadyValid(
                    $"cinemachine-position-control:{TypeName(preflight.FrameworkOwned)}");
                return preflight.FrameworkOwned;
            }

            if (preflight.FrameworkOwned != null)
            {
                RemoveFrameworkOwnedControl(
                    preflight.FrameworkOwned,
                    "position",
                    request,
                    report);
            }

            return CreatePositionControl(
                cinemachineCamera,
                desired,
                request,
                report);
        }

        private static CinemachineComponentBase ReconcileRotationControl(
            CinemachineCamera cinemachineCamera,
            RotationControlKind desired,
            StagePreflight preflight,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (preflight.ExternalCompatible != null)
            {
                if (preflight.FrameworkOwned != null)
                {
                    RemoveFrameworkOwnedControl(
                        preflight.FrameworkOwned,
                        "rotation",
                        request,
                        report);
                }

                report.MarkAlreadyValid(
                    $"cinemachine-rotation-control:external-compatible:{TypeName(preflight.ExternalCompatible)}");
                return preflight.ExternalCompatible;
            }

            if (desired == RotationControlKind.None)
            {
                if (preflight.FrameworkOwned != null)
                {
                    RemoveFrameworkOwnedControl(
                        preflight.FrameworkOwned,
                        "rotation",
                        request,
                        report);
                }
                else
                {
                    report.MarkAlreadyValid(
                        "cinemachine-rotation-control:none");
                }

                return null;
            }

            if (preflight.FrameworkOwned != null &&
                IsDesiredRotationControl(
                    preflight.FrameworkOwned,
                    desired))
            {
                report.MarkAlreadyValid(
                    $"cinemachine-rotation-control:{TypeName(preflight.FrameworkOwned)}");
                return preflight.FrameworkOwned;
            }

            if (preflight.FrameworkOwned != null)
            {
                RemoveFrameworkOwnedControl(
                    preflight.FrameworkOwned,
                    "rotation",
                    request,
                    report);
            }

            return CreateRotationControl(
                cinemachineCamera,
                desired,
                request,
                report);
        }

        private static CinemachineComponentBase CreatePositionControl(
            CinemachineCamera cinemachineCamera,
            PositionControlKind desired,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            switch (desired)
            {
                case PositionControlKind.Follow:
                    if (!request.CreateCinemachineFollowIfMissing)
                    {
                        report.MarkBlocked(
                            "cinemachine-follow:create-disabled");
                        return null;
                    }

                    CinemachineFollow follow =
                        AddComponent<CinemachineFollow>(
                            cinemachineCamera.gameObject,
                            request.UseUndo);
                    report.MarkCreated("cinemachine-position-control:CinemachineFollow");
                    return follow;

                case PositionControlKind.HardLockToTarget:
                    CinemachineHardLockToTarget hardLock =
                        AddComponent<CinemachineHardLockToTarget>(
                            cinemachineCamera.gameObject,
                            request.UseUndo);
                    report.MarkCreated(
                        "cinemachine-position-control:CinemachineHardLockToTarget");
                    return hardLock;

                case PositionControlKind.ThirdPersonFollow:
                    CinemachineThirdPersonFollow thirdPerson =
                        AddComponent<CinemachineThirdPersonFollow>(
                            cinemachineCamera.gameObject,
                            request.UseUndo);
                    report.MarkCreated(
                        "cinemachine-position-control:CinemachineThirdPersonFollow");
                    return thirdPerson;

                case PositionControlKind.None:
                default:
                    return null;
            }
        }

        private static CinemachineComponentBase CreateRotationControl(
            CinemachineCamera cinemachineCamera,
            RotationControlKind desired,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            switch (desired)
            {
                case RotationControlKind.HardLookAt:
                    CinemachineHardLookAt hardLookAt =
                        AddComponent<CinemachineHardLookAt>(
                            cinemachineCamera.gameObject,
                            request.UseUndo);
                    report.MarkCreated(
                        "cinemachine-rotation-control:CinemachineHardLookAt");
                    return hardLookAt;

                case RotationControlKind.RotateWithFollowTarget:
                    CinemachineRotateWithFollowTarget rotateWithTarget =
                        AddComponent<CinemachineRotateWithFollowTarget>(
                            cinemachineCamera.gameObject,
                            request.UseUndo);
                    report.MarkCreated(
                        "cinemachine-rotation-control:CinemachineRotateWithFollowTarget");
                    return rotateWithTarget;

                case RotationControlKind.None:
                default:
                    return null;
            }
        }

        private static void ConfigurePositionControl(
            CinemachineComponentBase control,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            switch (request.PresentationIntent)
            {
                case CameraRigPresentationIntent.Follow:
                    if (control is CinemachineFollow follow)
                    {
                        SetVector3(
                            follow.FollowOffset,
                            request.FollowOffset,
                            value => follow.FollowOffset = value,
                            "cinemachine-follow:follow-offset",
                            report);
                    }
                    break;

                case CameraRigPresentationIntent.Mounted:
                    if (control is CinemachineHardLockToTarget hardLock)
                    {
                        SetFloat(
                            hardLock.Damping,
                            request.MountedPositionDamping,
                            value => hardLock.Damping = value,
                            "cinemachine-hard-lock:damping",
                            report);
                    }
                    break;

                case CameraRigPresentationIntent.ThirdPerson:
                    if (control is CinemachineThirdPersonFollow thirdPerson)
                    {
                        SetVector3(
                            thirdPerson.ShoulderOffset,
                            request.ThirdPersonShoulderOffset,
                            value => thirdPerson.ShoulderOffset = value,
                            "cinemachine-third-person:shoulder-offset",
                            report);
                        SetFloat(
                            thirdPerson.VerticalArmLength,
                            request.ThirdPersonVerticalArmLength,
                            value => thirdPerson.VerticalArmLength = value,
                            "cinemachine-third-person:vertical-arm-length",
                            report);
                        SetFloat(
                            thirdPerson.CameraSide,
                            request.ThirdPersonCameraSide,
                            value => thirdPerson.CameraSide = value,
                            "cinemachine-third-person:camera-side",
                            report);
                        SetFloat(
                            thirdPerson.CameraDistance,
                            request.ThirdPersonCameraDistance,
                            value => thirdPerson.CameraDistance = value,
                            "cinemachine-third-person:camera-distance",
                            report);
                        SetVector3(
                            thirdPerson.Damping,
                            request.ThirdPersonDamping,
                            value => thirdPerson.Damping = value,
                            "cinemachine-third-person:damping",
                            report);
                    }
                    break;
            }
        }

        private static void ConfigureRotationControl(
            CinemachineComponentBase control,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (request.PresentationIntent ==
                    CameraRigPresentationIntent.Mounted &&
                control is CinemachineRotateWithFollowTarget rotateWithTarget)
            {
                SetFloat(
                    rotateWithTarget.Damping,
                    request.MountedRotationDamping,
                    value => rotateWithTarget.Damping = value,
                    "cinemachine-rotate-with-follow-target:damping",
                    report);
            }
        }

        private static bool IsDesiredPositionControl(
            CinemachineComponentBase component,
            PositionControlKind desired)
        {
            switch (desired)
            {
                case PositionControlKind.Follow:
                    return component is CinemachineFollow;
                case PositionControlKind.HardLockToTarget:
                    return component is CinemachineHardLockToTarget;
                case PositionControlKind.ThirdPersonFollow:
                    return component is CinemachineThirdPersonFollow;
                case PositionControlKind.None:
                default:
                    return false;
            }
        }

        private static bool IsDesiredRotationControl(
            CinemachineComponentBase component,
            RotationControlKind desired)
        {
            switch (desired)
            {
                case RotationControlKind.HardLookAt:
                    return component is CinemachineHardLookAt;
                case RotationControlKind.RotateWithFollowTarget:
                    return component is CinemachineRotateWithFollowTarget;
                case RotationControlKind.None:
                default:
                    return false;
            }
        }

        private static void RemoveFrameworkOwnedControl(
            CinemachineComponentBase component,
            string stageLabel,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (component == null)
            {
                return;
            }

            string typeName = TypeName(component);

            if (request.UseUndo)
            {
                Undo.DestroyObjectImmediate(component);
            }
            else
            {
                Object.DestroyImmediate(component);
            }

            report.MarkRepaired(
                $"cinemachine-{stageLabel}-control:framework-owned-removed:{typeName}");
        }

        private static UnityEngine.Camera ResolveUnityCamera(
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (request.UnityCamera != null)
            {
                if (!IsChildOrSelf(
                        request.UnityCamera.transform,
                        request.RigRoot))
                {
                    report.MarkBlocked(
                        "unity-camera:explicit-outside-rig-root");
                    return null;
                }

                report.MarkAlreadyValid("unity-camera:explicit");
                return request.UnityCamera;
            }

            UnityEngine.Camera[] localCameras =
                request.RigRoot.GetComponentsInChildren<UnityEngine.Camera>(true);

            if (localCameras.Length > 1)
            {
                report.MarkBlocked(
                    "unity-camera:multiple-local-candidates");
                return null;
            }

            if (localCameras.Length == 1)
            {
                report.MarkAlreadyValid(
                    "unity-camera:local-rig-child");
                return localCameras[0];
            }

            if (!request.CreateUnityCameraIfMissing)
            {
                report.MarkSkipped(
                    "unity-camera:create-disabled");
                return null;
            }

            string objectName = NormalizeObjectName(
                request.UnityCameraObjectName,
                "Unity Camera");

            var cameraObject = new GameObject(objectName);
            ParentCreatedObject(
                cameraObject.transform,
                request.RigRoot,
                request.UseUndo);
            RegisterCreatedObject(
                cameraObject,
                request.UseUndo,
                "Create Unity Camera");

            UnityEngine.Camera createdCamera =
                AddComponent<UnityEngine.Camera>(
                    cameraObject,
                    request.UseUndo);

            report.MarkCreated("unity-camera");
            return createdCamera;
        }

        private static CinemachineBrain EnsureBrain(
            UnityEngine.Camera unityCamera,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (unityCamera == null)
            {
                return null;
            }

            if (unityCamera.TryGetComponent(
                    out CinemachineBrain existingBrain))
            {
                report.MarkAlreadyValid("cinemachine-brain");
                return existingBrain;
            }

            CinemachineBrain createdBrain =
                AddComponent<CinemachineBrain>(
                    unityCamera.gameObject,
                    request.UseUndo);

            report.MarkCreated("cinemachine-brain");
            return createdBrain;
        }

        private static CinemachineCamera ResolveCinemachineCamera(
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            CinemachineCamera[] localCameras =
                request.RigRoot.GetComponentsInChildren<CinemachineCamera>(true);

            if (request.CinemachineCamera != null)
            {
                if (!IsChildOrSelf(
                        request.CinemachineCamera.transform,
                        request.RigRoot))
                {
                    report.MarkBlocked(
                        "cinemachine-camera:explicit-outside-rig-root");
                    return null;
                }

                for (int i = 0; i < localCameras.Length; i++)
                {
                    if (localCameras[i] != request.CinemachineCamera)
                    {
                        report.MarkBlocked(
                            "cinemachine-camera:multiple-local-candidates");
                        return null;
                    }
                }

                report.Evidence.CinemachineCamera =
                    request.CinemachineCamera;
                report.Evidence.CinemachineCameraOwnership =
                    ClassifyOwnership(
                        request.CinemachineCamera,
                        request.FrameworkOwnedCinemachineCamera);
                report.MarkAlreadyValid(
                    "cinemachine-camera:explicit");
                return request.CinemachineCamera;
            }

            if (localCameras.Length > 1)
            {
                report.MarkBlocked(
                    "cinemachine-camera:multiple-local-candidates");
                return null;
            }

            if (localCameras.Length == 1)
            {
                CinemachineCamera localCamera = localCameras[0];
                report.Evidence.CinemachineCamera = localCamera;
                report.Evidence.CinemachineCameraOwnership =
                    ClassifyOwnership(
                        localCamera,
                        request.FrameworkOwnedCinemachineCamera);
                report.MarkAlreadyValid(
                    "cinemachine-camera:local-rig-child");
                return localCamera;
            }

            if (!request.CreateCinemachineCameraIfMissing)
            {
                report.MarkSkipped(
                    "cinemachine-camera:create-disabled");
                return null;
            }

            string objectName = NormalizeObjectName(
                request.CinemachineCameraObjectName,
                "Cinemachine Camera");

            var cameraObject = new GameObject(objectName);
            ParentCreatedObject(
                cameraObject.transform,
                request.RigRoot,
                request.UseUndo);
            RegisterCreatedObject(
                cameraObject,
                request.UseUndo,
                "Create Cinemachine Camera");

            CinemachineCamera createdCamera =
                AddComponent<CinemachineCamera>(
                    cameraObject,
                    request.UseUndo);

            report.Evidence.CinemachineCamera = createdCamera;
            report.Evidence.CinemachineCameraOwnership =
                CinemachineRigMaterializationOwnership.FrameworkOwned;
            report.MarkCreated("cinemachine-camera");
            return createdCamera;
        }

        private static void ApplyTargets(
            CinemachineCamera cinemachineCamera,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (cinemachineCamera.Follow != request.FollowTarget)
            {
                cinemachineCamera.Follow = request.FollowTarget;
                report.MarkRepaired(
                    "cinemachine-camera:follow-target");
            }
            else
            {
                report.MarkAlreadyValid(
                    "cinemachine-camera:follow-target");
            }

            if (cinemachineCamera.LookAt != request.LookAtTarget)
            {
                cinemachineCamera.LookAt = request.LookAtTarget;
                report.MarkRepaired(
                    "cinemachine-camera:look-at-target");
            }
            else
            {
                report.MarkAlreadyValid(
                    "cinemachine-camera:look-at-target");
            }
        }

        private static CinemachineRigMaterializationOwnership
            ClassifyResolvedOwnership(
                CinemachineComponentBase component,
                StagePreflight preflight,
                Component previousFrameworkOwnedReference)
        {
            if (component == null)
            {
                return CinemachineRigMaterializationOwnership.None;
            }

            if (preflight.ExternalCompatible == component)
            {
                return CinemachineRigMaterializationOwnership.ExternalOrUnknown;
            }

            if (preflight.FrameworkOwned == component ||
                (previousFrameworkOwnedReference != null &&
                 component == previousFrameworkOwnedReference))
            {
                return CinemachineRigMaterializationOwnership.FrameworkOwned;
            }

            // A component that did not exist in preflight was created by this
            // materializer in the current operation.
            return CinemachineRigMaterializationOwnership.FrameworkOwned;
        }

        private static CinemachineRigMaterializationOwnership ClassifyOwnership(
            Component component,
            Component frameworkOwnedComponent)
        {
            if (component == null)
            {
                return CinemachineRigMaterializationOwnership.None;
            }

            return frameworkOwnedComponent != null &&
                   component == frameworkOwnedComponent
                ? CinemachineRigMaterializationOwnership.FrameworkOwned
                : CinemachineRigMaterializationOwnership.ExternalOrUnknown;
        }

        private static void SetFloat(
            float current,
            float desired,
            System.Action<float> assign,
            string diagnostic,
            CinemachineRigMaterializationReport report)
        {
            if (!Mathf.Approximately(current, desired))
            {
                assign(desired);
                report.MarkRepaired(diagnostic);
            }
            else
            {
                report.MarkAlreadyValid(diagnostic);
            }
        }

        private static void SetVector3(
            Vector3 current,
            Vector3 desired,
            System.Action<Vector3> assign,
            string diagnostic,
            CinemachineRigMaterializationReport report)
        {
            if (current != desired)
            {
                assign(desired);
                report.MarkRepaired(diagnostic);
            }
            else
            {
                report.MarkAlreadyValid(diagnostic);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) &&
                   value >= 0f;
        }

        private static bool IsFiniteNonNegative(Vector3 value)
        {
            return IsFinite(value) &&
                   value.x >= 0f &&
                   value.y >= 0f &&
                   value.z >= 0f;
        }

        private static bool IsChildOrSelf(
            Transform candidate,
            Transform root)
        {
            if (candidate == null ||
                root == null)
            {
                return false;
            }

            return candidate == root ||
                   candidate.IsChildOf(root);
        }

        private static string TypeName(Component component)
        {
            if (component == null)
            {
                return "<none>";
            }

            System.Type type = component.GetType();
            return type.FullName ?? type.Name;
        }

        private static int NextRevision(int previousRevision)
        {
            if (previousRevision < 0)
            {
                return 1;
            }

            return previousRevision < int.MaxValue
                ? previousRevision + 1
                : int.MaxValue;
        }

        private static void MarkDirty(Object value)
        {
            if (value != null)
            {
                EditorUtility.SetDirty(value);
            }
        }

        private static T AddComponent<T>(
            GameObject target,
            bool useUndo)
            where T : Component
        {
            return useUndo
                ? Undo.AddComponent<T>(target)
                : target.AddComponent<T>();
        }

        private static void ParentCreatedObject(
            Transform child,
            Transform parent,
            bool useUndo)
        {
            if (useUndo)
            {
                Undo.SetTransformParent(
                    child,
                    parent,
                    "Parent Cinemachine Rig Object");
            }
            else
            {
                child.SetParent(parent, false);
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        private static void RegisterCreatedObject(
            GameObject gameObject,
            bool useUndo,
            string undoName)
        {
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(
                    gameObject,
                    undoName);
            }
        }

        private static string NormalizeObjectName(
            string value,
            string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }
    }
}
