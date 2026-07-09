using Immersive.Framework.Editor.Camera.Cinemachine;
using UnityEditor;
using UnityEngine;

public static class C4CinemachineRigMaterializerSmoke
{
    [MenuItem("Immersive Framework/QA/Camera/C4 Cinemachine Rig Materializer Smoke")]
    private static void Run()
    {
        GameObject rig = GameObject.Find("Camera Rig");
        GameObject followTarget = GameObject.Find("PlayerCameraTarget");
        GameObject lookAtTarget = GameObject.Find("PlayerLookAtTarget");

        if (rig == null || followTarget == null || lookAtTarget == null)
        {
            Debug.LogError(
                "[C4 Smoke] Missing scene objects. Required: 'Camera Rig', 'PlayerCameraTarget', 'PlayerLookAtTarget'.");
            return;
        }

        var request = new CinemachineRigMaterializationRequest
        {
            RigRoot = rig.transform,
            FollowTarget = followTarget.transform,
            LookAtTarget = lookAtTarget.transform,
            Priority = 10,
            RequireFollowTarget = true,
            RequireLookAtTarget = true,
            CreateUnityCameraIfMissing = true,
            CreateCinemachineCameraIfMissing = true,
            UseUndo = false
        };

        var first = CinemachineRigMaterializer.ApplyOrRebuild(request);
        Debug.Log($"[C4 Smoke] First Apply/Rebuild: {first.CreateSummary()}");

        var second = CinemachineRigMaterializer.ApplyOrRebuild(request);
        Debug.Log($"[C4 Smoke] Second Apply/Rebuild: {second.CreateSummary()}");

        if (!first.Succeeded || !second.Succeeded)
        {
            Debug.LogError(
                $"[C4 Smoke] FAILED. firstBlocked='{first.FirstBlockingIssue}' secondBlocked='{second.FirstBlockingIssue}'");
            return;
        }

        if (second.CreatedCount != 0)
        {
            Debug.LogError(
                $"[C4 Smoke] FAILED. Second run created objects/components. created='{second.CreatedCount}'");
            return;
        }

        Debug.Log("[C4 Smoke] PASS. Cinemachine rig materialization is idempotent.");
    }
}