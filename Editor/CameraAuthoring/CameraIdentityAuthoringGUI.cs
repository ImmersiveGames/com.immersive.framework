using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal static class CameraIdentityAuthoringGUI
    {
        internal static void DrawIssue(string issue)
        {
            if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Error);
        }
    }

    internal sealed class CameraOutputReferenceGUI
    {
        internal void DrawReference(SerializedProperty property) =>
            DrawDefinitionReference(property, "Output Definition");

        internal static void DrawDefinitionReference(SerializedProperty property, string label)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            var definition = property.objectReferenceValue as ScriptableObject;
            CameraIdentityAuthoringGUI.DrawIssue(definition == null
                ? "Assign an exact " + label + " asset."
                : CameraDefinitionIdentityEditorUtility.ValidateLocalIdentity(definition));
        }
    }
}
