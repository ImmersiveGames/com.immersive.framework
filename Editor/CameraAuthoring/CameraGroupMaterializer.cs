using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Camera.Cinemachine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal static class CameraGroupMaterializer
    {
        internal static void Materialize(
            CameraRigComposer composer,
            bool useUndo,
            CinemachineRigMaterializationReport report)
        {
            var group = composer.FrameworkOwnedGroupTargetGroup;
            var framing = composer.FrameworkOwnedGroupFraming;
            if (useUndo) Undo.RecordObject(composer, "Materialize Group Presentation");
            if (group == null)
            {
                var root = new GameObject("Group Target Group");
                if (useUndo) Undo.RegisterCreatedObjectUndo(root, "Create Group Target Group");
                root.transform.SetParent(composer.transform, false);
                group = useUndo
                    ? Undo.AddComponent<CinemachineTargetGroup>(root)
                    : root.AddComponent<CinemachineTargetGroup>();
                report.MarkCreated("group:target-group");
            }
            if (framing == null)
            {
                GameObject cameraObject = composer.CinemachineCamera.gameObject;
                framing = useUndo
                    ? Undo.AddComponent<CinemachineGroupFraming>(cameraObject)
                    : cameraObject.AddComponent<CinemachineGroupFraming>();
                report.MarkCreated("group:group-framing");
            }

            if (useUndo) Undo.RecordObject(framing, "Configure Group Framing");
            framing.enabled = true;
            framing.FramingSize = composer.GroupFramingSize;
            framing.Damping = composer.GroupDamping;
            framing.FovRange = composer.GroupFovRange;
            framing.DollyRange = composer.GroupDollyRange;
            framing.OrthoSizeRange = composer.GroupOrthoSizeRange;

            composer.EditorSetGroupMaterialization(group, framing);
            EditorUtility.SetDirty(framing);
            PrefabUtility.RecordPrefabInstancePropertyModifications(framing);
            EditorUtility.SetDirty(composer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(composer);
        }

        internal static void Dematerialize(
            CameraRigComposer composer,
            bool useUndo,
            CinemachineRigMaterializationReport report)
        {
            CinemachineGroupFraming framing = composer.FrameworkOwnedGroupFraming;
            CinemachineTargetGroup group = composer.FrameworkOwnedGroupTargetGroup;
            if (framing != null)
            {
                if (useUndo) Undo.DestroyObjectImmediate(framing);
                else Object.DestroyImmediate(framing);
                report.MarkRepaired("group:group-framing:framework-owned-removed");
            }
            if (group != null)
            {
                GameObject groupObject = group.gameObject;
                if (useUndo) Undo.DestroyObjectImmediate(groupObject);
                else Object.DestroyImmediate(groupObject);
                report.MarkRepaired("group:target-group:framework-owned-removed");
            }
            composer.EditorSetGroupMaterialization(null, null);
            EditorUtility.SetDirty(composer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(composer);
        }
    }
}
