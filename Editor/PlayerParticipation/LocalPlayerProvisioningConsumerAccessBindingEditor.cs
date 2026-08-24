using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(LocalPlayerProvisioningConsumerAccessBinding))]
    internal sealed class LocalPlayerProvisioningConsumerAccessBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _scope;

        private void OnEnable()
        {
            _scope = serializedObject.FindProperty("scope");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Local Player Provisioning Consumer Access",
                "Receives the Framework Core provisioning port for one explicit Route or Activity lifecycle scope.");
            FrameworkAuthoringInspectorGui.IntentSummary(
                "This binding owns no Player state. Runtime bind, release and rejection evidence is written to the Console.");

            FrameworkAuthoringInspectorGui.Section("Lifecycle Scope");
            EditorGUILayout.PropertyField(
                _scope,
                new GUIContent(
                    "Scope",
                    "The Route or Activity lifecycle scope that owns this consumer access binding."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
