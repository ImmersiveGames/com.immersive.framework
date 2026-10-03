using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraOutputDefinition))]
    internal sealed class CameraOutputDefinitionEditor : CameraOutputDefinitionEditorBase
    {
        protected override void DrawDefinitionFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        }
    }

    internal abstract class CameraOutputDefinitionEditorBase : UnityEditor.Editor
    {
        private bool _advanced;
        private bool _validationOutdated = true;
        private bool _identityCollision;
        private string _lastValidationIssue;

        public override void OnInspectorGUI()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            current.Update();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            EditorGUILayout.HelpBox(
                "Reference this exact Output asset from Session Camera Assignments. Its stable ID identifies the physical destination.",
                MessageType.Info);

            DrawDefinitionFields();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            if (current.ApplyModifiedProperties())
                _validationOutdated = true;

            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            var definition = inspectedTarget as ScriptableObject;
            if (definition == null)
                return;
            string localIdentityIssue =
                CameraOutputDefinitionIdentityEditorUtility.ValidateLocalIdentity(definition);
            if (localIdentityIssue != null)
            {
                EditorGUILayout.HelpBox(localIdentityIssue, MessageType.Error);
            }
            else if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Configuration changed or has not been validated. Validate explicitly to check identity collisions and rig configuration.",
                    MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(_lastValidationIssue))
            {
                EditorGUILayout.HelpBox(_lastValidationIssue, MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Configuration validated.", MessageType.Info);
            }

            var id = serializedObject.FindProperty("stableId");
            if (string.IsNullOrEmpty(id.stringValue))
            {
                if (GUILayout.Button("Generate Stable Identity"))
                {
                    CameraOutputDefinitionIdentityEditorUtility
                        .GenerateMissingId(definition);
                    _validationOutdated = true;
                    if (!IsCurrentTargetValid(inspectedTarget, current))
                        return;
                }
            }

            if (GUILayout.Button("Validate"))
            {
                if (!IsCurrentTargetValid(inspectedTarget, current))
                    return;
                _lastValidationIssue =
                    CameraOutputDefinitionIdentityEditorUtility.Validate(definition);
                if (!IsCurrentTargetValid(inspectedTarget, current))
                    return;
                _identityCollision = _lastValidationIssue != null &&
                    _lastValidationIssue.IndexOf("collision", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (_lastValidationIssue == null)
                {
                    _lastValidationIssue = ValidateDefinitionConfiguration();
                    if (!IsCurrentTargetValid(inspectedTarget, current))
                        return;
                }
                _validationOutdated = false;
            }

            if (!_validationOutdated && _identityCollision &&
                GUILayout.Button("Repair Collision — New Identity for This Definition"))
            {
                CameraOutputDefinitionIdentityEditorUtility.RepairCollision(definition);
                if (!IsCurrentTargetValid(inspectedTarget, current))
                    return;
                _identityCollision = false;
                _lastValidationIssue = null;
                _validationOutdated = true;
            }

            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            _advanced =
                EditorGUILayout.Foldout(
                    _advanced,
                    "Advanced / Debug",
                    true);
            if (_advanced)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        "Stable ID",
                        id.stringValue);
                }

                if (GUILayout.Button("Copy Stable ID"))
                {
                    EditorGUIUtility.systemCopyBuffer = id.stringValue;
                }
            }
        }

        protected abstract void DrawDefinitionFields();

        protected virtual string ValidateDefinitionConfiguration() => null;

        private bool TryGetValidSerializedObject(
            UnityEngine.Object inspectedTarget,
            out SerializedObject current)
        {
            current = null;
            if (this == null || inspectedTarget == null || target != inspectedTarget)
                return false;

            current = serializedObject;
            return current != null && current.targetObject == inspectedTarget;
        }

        private bool IsCurrentTargetValid(
            UnityEngine.Object inspectedTarget,
            SerializedObject current)
        {
            return current != null && inspectedTarget != null &&
                   target == inspectedTarget &&
                   current.targetObject == inspectedTarget;
        }
    }
}
