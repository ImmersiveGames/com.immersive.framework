using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>Reusable Session Camera rig configuration.</summary>
    [CreateAssetMenu(fileName = "Camera Definition", menuName = "Immersive Framework/Camera/Camera Definition", order = 10)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-038-E Session Camera Definition with member Actor target support.")]
    public sealed class CameraDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [SerializeField] private GameObject rigPrefab;

        public bool HasValidId => Guid.TryParseExact(stableId, "N", out Guid id) &&
            id != Guid.Empty && stableId == id.ToString("N");

        public CameraDefinitionId DefinitionId => HasValidId
            ? new CameraDefinitionId(stableId)
            : throw new InvalidOperationException("Camera Definition requires an explicitly generated stable ID.");

        public GameObject RigPrefab => rigPrefab;

        public bool TryValidateSessionCamera(
            CameraTargetPolicy targetPolicy,
            out string issue)
        {
            if (!HasValidId)
            {
                issue = "Camera Definition requires an explicitly generated stable ID.";
                return false;
            }

            if (rigPrefab == null)
            {
                issue = "Camera Definition requires an explicit Rig Prefab.";
                return false;
            }

            CameraRigComposer[] composers = rigPrefab.GetComponentsInChildren<CameraRigComposer>(true);
            if (composers.Length != 1 || composers[0] == null)
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' must contain exactly one CameraRigComposer. Found '{composers.Length}'.";
                return false;
            }

            if (rigPrefab.GetComponentInChildren<CameraOutputAuthoring>(true) != null)
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' must not contain CameraOutputAuthoring.";
                return false;
            }

            CameraRigComposer composer = composers[0];
            if (composer.BehaviorDefinition is GroupCameraRigBehaviorDefinition)
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' cannot use Group behavior in the Session membership cut; shared Group projection is deferred.";
                return false;
            }
            if (targetPolicy == CameraTargetPolicy.NoSubject &&
                (composer.EffectiveFollowRequirement != CameraTargetRequirement.NotUsed ||
                 composer.EffectiveLookAtRequirement != CameraTargetRequirement.NotUsed))
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' requires a Subject but its Assignment target policy is NoSubject.";
                return false;
            }
            if (targetPolicy == CameraTargetPolicy.MemberActorTargets &&
                composer.EffectiveFollowRequirement == CameraTargetRequirement.NotUsed &&
                composer.EffectiveLookAtRequirement == CameraTargetRequirement.NotUsed)
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' does not consume the Assignment's member Actor Subjects.";
                return false;
            }

            if (!composer.TryValidateForApply(out issue))
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' is invalid. {issue}";
                return false;
            }

            if (composer.CinemachineCamera == null)
            {
                issue = $"Camera Definition Rig Prefab '{rigPrefab.name}' must contain its materialized CinemachineCamera.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
