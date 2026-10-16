using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Editor-only conversion between designer framing controls and the canonical Follow offset.")]
    public static class CameraRigFramingEditorUtility
    {
        public static float GetDistance(Vector3 followOffset) => followOffset.magnitude;

        public static float GetPitch(Vector3 followOffset)
        {
            float horizontalDistance = Mathf.Sqrt(
                followOffset.x * followOffset.x + followOffset.z * followOffset.z);
            return Mathf.Atan2(followOffset.y, horizontalDistance) * Mathf.Rad2Deg;
        }

        public static float GetYaw(Vector3 followOffset)
        {
            if (Mathf.Approximately(followOffset.x, 0f) &&
                Mathf.Approximately(followOffset.z, 0f))
                return 0f;

            return Mathf.Atan2(followOffset.x, -followOffset.z) * Mathf.Rad2Deg;
        }

        public static Vector3 ToFollowOffset(float distance, float pitchDegrees, float yawDegrees)
        {
            float pitch = pitchDegrees * Mathf.Deg2Rad;
            float yaw = yawDegrees * Mathf.Deg2Rad;
            float horizontalDistance = distance * Mathf.Cos(pitch);
            return new Vector3(
                horizontalDistance * Mathf.Sin(yaw),
                distance * Mathf.Sin(pitch),
                -horizontalDistance * Mathf.Cos(yaw));
        }
    }
}
