using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal static class CameraIdentityAuthoringGUI
    {
        internal static void DrawDefinition(SerializedProperty property, bool isView)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(isView ? "View Id" : "Output Id"));
                if (GUILayout.Button("Copy", GUILayout.Width(48)))
                    EditorGUIUtility.systemCopyBuffer = property.stringValue;
                if (GUILayout.Button("Generate", GUILayout.Width(72)))
                    property.stringValue = CameraAuthoringIdUtility.GenerateIdText();
            }
            DrawIssue(isView
                ? CameraIdentityAuthoringValidation.ViewIdIssue(property.stringValue)
                : CameraIdentityAuthoringValidation.OutputIdIssue(property.stringValue));
        }

        internal static void DrawIssue(string issue)
        {
            if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Error);
        }
    }

    internal sealed class CameraOutputReferenceGUI
    {
        private CameraOutputAuthoringTopology _topology;

        internal void DrawTopology(SerializedObject serialized)
        {
            if (GUILayout.Button("Refresh Active Outputs"))
            {
                serialized.ApplyModifiedProperties();
                _topology = CameraOutputAuthoringResolver.Resolve();
            }
            EditorGUILayout.HelpBox(_topology == null
                ? "Active Output topology has not been inspected. Refresh Active Outputs to list definitions; manual entry remains available."
                : _topology.Diagnostic + "\nRefresh after changing Settings or Output definitions.",
                _topology != null && !_topology.IsResolved ? MessageType.Warning : MessageType.Info);
            if (_topology != null)
                foreach (string issue in _topology.Issues) CameraIdentityAuthoringGUI.DrawIssue(issue);
        }

        internal void DrawReference(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, new GUIContent("Output", "Explicit Camera Output Id. Manual values are preserved."));
            if (_topology != null && _topology.IsResolved)
            {
                var labels = new string[_topology.Labels.Count + 1];
                labels[0] = "Select an Output…";
                for (int i = 0; i < _topology.Labels.Count; i++) labels[i + 1] = _topology.Labels[i];
                int selected = EditorGUILayout.Popup("Active Outputs", 0, labels);
                if (selected > 0)
                {
                    CameraOutputId id = _topology.Outputs[selected - 1];
                    string scenePath = _topology.ScenePath;
                    property.serializedObject.ApplyModifiedProperties();
                    _topology = CameraOutputAuthoringResolver.Resolve();
                    if (_topology.IsResolved && _topology.ScenePath == scenePath &&
                        id.IsValid && _topology.Count(id) == 1)
                        property.stringValue = id.Value;
                    else
                        EditorGUILayout.HelpBox("Output selection was not applied because the active topology changed or the definition is invalid/ambiguous. Review the refreshed list.", MessageType.Warning);
                }
            }
            CameraIdentityAuthoringGUI.DrawIssue(
                CameraIdentityAuthoringValidation.OutputReferenceIssue(property.stringValue, _topology));
        }
    }
}
