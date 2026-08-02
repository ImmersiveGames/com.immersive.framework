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
        private SerializedProperty provisioningAuthoring;
        private bool showAdvanced;

        private void OnEnable()
        {
            provisioningAuthoring =
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
                provisioningAuthoring,
                new GUIContent(
                    "Provisioning Authoring",
                    "Explicit Local Player Provisioning Authoring exposed by this Game Application's UIGlobal composition."));

            if (provisioningAuthoring == null ||
                provisioningAuthoring.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign the Local Player Provisioning Authoring exposed by this Game Application's UIGlobal composition.",
                    MessageType.Error);
            }
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.Space(6f);

            showAdvanced =
                EditorGUILayout.Foldout(
                    showAdvanced,
                    "Advanced / Debug",
                    true);

            if (!showAdvanced)
            {
                return;
            }

            DrawSection("Registration Evidence");

            LocalPlayerProvisioningAuthoring authoring =
                provisioningAuthoring != null
                    ? provisioningAuthoring
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
