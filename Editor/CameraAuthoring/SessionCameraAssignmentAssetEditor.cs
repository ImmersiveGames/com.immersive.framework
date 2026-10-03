using System;
using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(SessionCameraAssignmentAsset))]
    internal sealed class SessionCameraAssignmentAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "This asset is the reusable Session Camera Assignment. Reference it from Game Applications and command triggers; its technical ID is generated and is not consumer-authored.",
                MessageType.Info);
            DrawPropertiesExcluding(serializedObject, "m_Script", "assignmentId");

            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(target);

            var asset = (SessionCameraAssignmentAsset)target;
            if (!asset.AssignmentId.IsValid)
            {
                if (GUILayout.Button("Generate Assignment Identity"))
                {
                    Undo.RecordObject(asset, "Generate Session Camera Assignment Identity");
                    SerializedObject current = new SerializedObject(asset);
                    current.FindProperty("assignmentId").stringValue = Guid.NewGuid().ToString("N");
                    current.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                }
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Assignment ID", asset.AssignmentId.Value);
            }

            if (GUILayout.Button("Validate Assignment"))
            {
                EditorGUILayout.Space();
                if (asset.TryBuild(out _, out string issue))
                    EditorUtility.DisplayDialog("Session Camera Assignment", "Assignment is valid.", "OK");
                else
                    EditorUtility.DisplayDialog("Session Camera Assignment", issue, "OK");
            }
        }
    }
}
