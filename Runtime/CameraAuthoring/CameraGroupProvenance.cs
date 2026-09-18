using Unity.Cinemachine;

namespace Immersive.Framework.CameraAuthoring
{
    internal static class CameraGroupProvenance
    {
        internal static bool HasRecordedMaterialization(CameraRigComposer composer) =>
            composer.FrameworkOwnedGroupTargetGroup != null ||
            composer.FrameworkOwnedGroupFraming != null;

        internal static bool Validate(
            CameraRigComposer composer,
            bool requireMaterialized,
            out string issue)
        {
            issue = string.Empty;
            var group = composer.FrameworkOwnedGroupTargetGroup;
            var framing = composer.FrameworkOwnedGroupFraming;
            var camera = composer.CinemachineCamera;
            if (group != null &&
                (group.transform == composer.transform ||
                 !group.transform.IsChildOf(composer.transform)))
            {
                issue = "Recorded Group Target Group is not a child of this Composer rig.";
                return false;
            }
            if (framing != null && (camera == null || framing.gameObject != camera.gameObject))
            {
                issue = "Recorded Group Framing does not belong to this Composer's Cinemachine Camera.";
                return false;
            }
            if (camera != null)
            {
                foreach (var candidate in camera.GetComponents<CinemachineGroupFraming>())
                {
                    if (candidate == framing) continue;
                    issue = "Author-owned or unproven Cinemachine Group Framing conflicts with Group materialization.";
                    return false;
                }
            }
            if (requireMaterialized && (camera == null || group == null || framing == null))
            {
                issue = "Group requires proven Target Group and Group Framing. Apply / Rebuild the rig in Edit Mode.";
                return false;
            }
            return true;
        }

        internal static bool ValidateForRemoval(
            CameraRigComposer composer,
            out string issue)
        {
            if (!Validate(composer, false, out issue))
            {
                return false;
            }

            CinemachineTargetGroup group = composer.FrameworkOwnedGroupTargetGroup;
            if (group != null && !OwnsWholeTargetGroupObject(group))
            {
                issue = "Recorded Group Target Group object contains author-owned components or children and cannot be replaced safely.";
                return false;
            }

            return true;
        }

        private static bool OwnsWholeTargetGroupObject(CinemachineTargetGroup group)
        {
            if (group.transform.childCount != 0)
            {
                return false;
            }

            UnityEngine.Component[] components = group.GetComponents<UnityEngine.Component>();
            if (components.Length != 2)
            {
                return false;
            }

            return (components[0] == group.transform && components[1] == group) ||
                   (components[0] == group && components[1] == group.transform);
        }
    }
}
