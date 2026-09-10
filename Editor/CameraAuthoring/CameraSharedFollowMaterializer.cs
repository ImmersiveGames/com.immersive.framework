using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Camera.Cinemachine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal static class CameraSharedFollowMaterializer
    {
        internal static void Materialize(CameraRigComposer composer, bool useUndo, CinemachineRigMaterializationReport report)
        {
            var group = composer.FrameworkOwnedSharedFollowTargetGroup;
            var framing = composer.FrameworkOwnedSharedFollowGroupFraming;
            if (useUndo) Undo.RecordObject(composer, "Materialize Shared Follow");
            if (group == null)
            {
                var root = new GameObject("Shared Follow Target Group");
                if (useUndo) Undo.RegisterCreatedObjectUndo(root, "Create Shared Follow Target Group");
                root.transform.SetParent(composer.transform, false);
                group = useUndo ? Undo.AddComponent<CinemachineTargetGroup>(root) : root.AddComponent<CinemachineTargetGroup>();
                report.MarkCreated("shared-follow:target-group");
            }
            if (framing == null)
            {
                var cameraObject = composer.CinemachineCamera.gameObject;
                framing = useUndo ? Undo.AddComponent<CinemachineGroupFraming>(cameraObject) : cameraObject.AddComponent<CinemachineGroupFraming>();
                framing.enabled = false;
                report.MarkCreated("shared-follow:group-framing");
            }
            composer.EditorSetSharedFollowMaterialization(group, framing);
            EditorUtility.SetDirty(composer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(composer);
        }
    }
}
