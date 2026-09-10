using Unity.Cinemachine;

namespace Immersive.Framework.CameraAuthoring
{
    internal static class CameraSharedFollowProvenance
    {
        internal static bool Validate(CameraRigComposer composer, bool requireMaterialized, out string issue)
        {
            issue = string.Empty;
            var group = composer.FrameworkOwnedSharedFollowTargetGroup;
            var framing = composer.FrameworkOwnedSharedFollowGroupFraming;
            var camera = composer.CinemachineCamera;
            if (group != null && group.transform != composer.transform &&
                !group.transform.IsChildOf(composer.transform))
            {
                issue = "Recorded shared Follow Target Group is outside this Composer rig.";
                return false;
            }
            if (framing != null && (camera == null || framing.gameObject != camera.gameObject))
            {
                issue = "Recorded shared Follow Group Framing does not belong to this Composer's Cinemachine Camera.";
                return false;
            }
            if (camera != null)
            {
                foreach (var candidate in camera.GetComponents<CinemachineGroupFraming>())
                {
                    if (candidate == framing) continue;
                    issue = "Author-owned or unproven Cinemachine Group Framing conflicts with shared Follow materialization.";
                    return false;
                }
            }
            if (requireMaterialized && (camera == null || group == null || framing == null))
            {
                issue = "Shared Follow requires proven Target Group and Group Framing. Apply / Rebuild the rig in Edit Mode.";
                return false;
            }
            return true;
        }
    }
}
