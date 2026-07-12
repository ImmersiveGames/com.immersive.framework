using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Editor-only utility that creates or repairs Cinemachine technical materialization.
    /// It does not use Camera.main, does not search globally, and does not act as runtime authority.
    /// </summary>
    public static class CinemachineRigMaterializer
    {
        public static CinemachineRigMaterializationReport ApplyOrRebuild(
            CinemachineRigMaterializationRequest request)
        {
            var report = new CinemachineRigMaterializationReport();

            if (request == null)
            {
                report.MarkBlocked("request:null");
                return report;
            }

            if (request.RigRoot == null)
            {
                report.MarkBlocked("rig-root:null");
                return report;
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

            CinemachineFollow cinemachineFollow =
                EnsureFollowPipeline(
                    cinemachineCamera,
                    request,
                    report);

            if (request.FollowTarget != null &&
                cinemachineFollow == null)
            {
                report.MarkBlocked(
                    "cinemachine-follow:missing");
            }

            if (report.BlockedCount > 0)
            {
                return report;
            }

            ApplyFollowSettings(
                cinemachineFollow,
                request,
                report);

            ApplyTargets(cinemachineCamera, request, report);

            if (unityCamera != null)
            {
                EditorUtility.SetDirty(unityCamera);
            }

            if (brain != null)
            {
                EditorUtility.SetDirty(brain);
            }

            if (cinemachineFollow != null)
            {
                EditorUtility.SetDirty(cinemachineFollow);
            }

            EditorUtility.SetDirty(cinemachineCamera);
            EditorUtility.SetDirty(request.RigRoot.gameObject);

            report.Evidence.UnityCamera = unityCamera;
            report.Evidence.Brain = brain;
            report.Evidence.CinemachineCamera = cinemachineCamera;
            report.Evidence.CinemachineFollow = cinemachineFollow;
            report.Evidence.FollowTarget = cinemachineCamera.Follow;
            report.Evidence.LookAtTarget = cinemachineCamera.LookAt;

            return report;
        }

        private static CinemachineFollow EnsureFollowPipeline(
            CinemachineCamera cinemachineCamera,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (request.FollowTarget == null)
            {
                report.MarkSkipped("cinemachine-follow:not-requested");
                return null;
            }

            if (cinemachineCamera.TryGetComponent(
                    out CinemachineFollow existingFollow))
            {
                report.MarkAlreadyValid("cinemachine-follow");
                return existingFollow;
            }

            if (!request.CreateCinemachineFollowIfMissing)
            {
                report.MarkSkipped("cinemachine-follow:create-disabled");
                return null;
            }

            CinemachineFollow createdFollow =
                AddComponent<CinemachineFollow>(
                    cinemachineCamera.gameObject,
                    request.UseUndo);

            report.MarkCreated("cinemachine-follow");
            return createdFollow;
        }

        private static void ApplyFollowSettings(
            CinemachineFollow cinemachineFollow,
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (cinemachineFollow == null)
            {
                return;
            }

            if (cinemachineFollow.FollowOffset != request.FollowOffset)
            {
                cinemachineFollow.FollowOffset = request.FollowOffset;
                report.MarkRepaired("cinemachine-follow:follow-offset");
            }
            else
            {
                report.MarkAlreadyValid("cinemachine-follow:follow-offset");
            }
        }

        private static UnityEngine.Camera ResolveUnityCamera(
            CinemachineRigMaterializationRequest request,
            CinemachineRigMaterializationReport report)
        {
            if (request.UnityCamera != null)
            {
                report.MarkAlreadyValid("unity-camera:explicit");
                return request.UnityCamera;
            }

            UnityEngine.Camera localCamera =
                request.RigRoot.GetComponentInChildren<UnityEngine.Camera>(true);

            if (localCamera != null)
            {
                report.MarkAlreadyValid(
                    "unity-camera:local-rig-child");
                return localCamera;
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
            if (request.CinemachineCamera != null)
            {
                report.MarkAlreadyValid(
                    "cinemachine-camera:explicit");
                return request.CinemachineCamera;
            }

            CinemachineCamera localCamera =
                request.RigRoot.GetComponentInChildren<CinemachineCamera>(true);

            if (localCamera != null)
            {
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
