using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(LocalPlayerProvisioningHostRegistration))]
    internal sealed class LocalPlayerProvisioningHostRegistrationEditor : UnityEditor.Editor
    {
        private SerializedProperty _provisioningAuthoring;
        private bool _advanced;
        private void OnEnable() => _provisioningAuthoring = serializedObject.FindProperty("provisioningAuthoring");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var registration = (LocalPlayerProvisioningHostRegistration)target;
            FrameworkAuthoringInspectorGui.ProductHeader("Local Player Provisioning Host Registration", "Registers the explicit Local Player provisioning surface owned by this UIGlobal composition.");
            FrameworkAuthoringInspectorGui.IntentSummary("Expose one explicit provisioning endpoint for this Game Application.");
            FrameworkAuthoringInspectorGui.Section("Provisioning Host");
            EditorGUILayout.PropertyField(_provisioningAuthoring, new GUIContent("Provisioning Authoring"));
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            EditorGUILayout.HelpBox(registration.ProvisioningAuthoring == null ? "Incomplete: assign a Local Player Provisioning Authoring component." : "Ready. This registration does not provision or admit a Player by itself.", registration.ProvisioningAuthoring == null ? MessageType.Error : MessageType.Info);
            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced) EditorGUILayout.LabelField("Resolved Authoring", registration.ProvisioningAuthoring != null ? registration.ProvisioningAuthoring.name : "<none>");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
