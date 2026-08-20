using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(
        typeof(
            LocalPlayerProvisioningHostRegistration))]
    internal sealed class
        LocalPlayerProvisioningHostRegistrationEditor :
            UnityEditor.Editor
    {
        private SerializedProperty _provisioningAuthoring;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _provisioningAuthoring =
                serializedObject.FindProperty(
                    "provisioningAuthoring");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawRegistrationSetup();

            serializedObject.ApplyModifiedProperties();

            DrawAdvanced();
        }

        private void DrawRegistrationSetup()
        {
            DrawSection("Provisioning Registration");

            EditorGUILayout.PropertyField(
                _provisioningAuthoring,
                new GUIContent(
                    "Provisioning Authoring",
                    "Explicit Local Player Provisioning Authoring exposed by this Game Application's UIGlobal composition."));

            if (_provisioningAuthoring == null ||
                _provisioningAuthoring.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign the Local Player Provisioning Authoring exposed by this Game Application's UIGlobal composition.",
                    MessageType.Error);
            }
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.Space(6f);

            _showAdvanced =
                EditorGUILayout.Foldout(
                    _showAdvanced,
                    "Advanced / Debug",
                    true);

            if (!_showAdvanced)
            {
                return;
            }

            DrawSection("Registration Evidence");

            LocalPlayerProvisioningAuthoring authoring =
                _provisioningAuthoring != null
                    ? _provisioningAuthoring
                        .objectReferenceValue
                        as LocalPlayerProvisioningAuthoring
                    : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Provisioning Authoring",
                    authoring,
                    typeof(
                        LocalPlayerProvisioningAuthoring),
                    true);

                EditorGUILayout.ObjectField(
                    "GameObject",
                    authoring != null
                        ? authoring.gameObject
                        : null,
                    typeof(GameObject),
                    true);

                EditorGUILayout.TextField(
                    "Authoring Context",
                    ResolveAuthoringContext(authoring));
            }
        }

        private static string ResolveAuthoringContext(
            LocalPlayerProvisioningAuthoring authoring)
        {
            if (authoring == null)
            {
                return "Missing";
            }

            GameObject gameObject = authoring.gameObject;

            if (PrefabUtility.IsPartOfPrefabAsset(
                    gameObject))
            {
                return "Prefab Asset";
            }

            if (gameObject.scene.IsValid())
            {
                return gameObject.scene.isLoaded
                    ? "Scene or Prefab Mode Object"
                    : "Scene Object";
            }

            return "Serialized Reference";
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
