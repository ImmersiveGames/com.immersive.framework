using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(SceneLocalPlayerAdmissionAuthoring))]
    public sealed class SceneLocalPlayerAdmissionAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty playerSlotProfile;
        private SerializedProperty localPlayerHost;
        private SerializedProperty actorProfile;
        private SerializedProperty sceneLogicalPlayerActor;
        private SerializedProperty admissionTiming;
        private bool showDebug;

        private void OnEnable()
        {
            playerSlotProfile = serializedObject.FindProperty("playerSlotProfile");
            localPlayerHost = serializedObject.FindProperty("localPlayerHost");
            actorProfile = serializedObject.FindProperty("actorProfile");
            sceneLogicalPlayerActor = serializedObject.FindProperty("sceneLogicalPlayerActor");
            admissionTiming = serializedObject.FindProperty("admissionTiming");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Scene Local Player Admission", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Admits one explicitly referenced local Player that already exists in an Activity scene. This surface does not call PlayerInputManager.JoinPlayer and never owns physical destruction of the Host or Logical Actor.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playerSlotProfile);
            EditorGUILayout.PropertyField(localPlayerHost);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Logical Actor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(actorProfile);
            EditorGUILayout.PropertyField(sceneLogicalPlayerActor);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Admission", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(admissionTiming);
            EditorGUILayout.HelpBox(
                "Apply / Rebuild validates explicit references and materializes typed prefab/profile evidence only. It does not reserve a Slot, generate a runtime ActorId or enable gameplay.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply / Rebuild"))
                {
                    SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild(
                        (SceneLocalPlayerAdmissionAuthoring)target,
                        true,
                        true);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Validate"))
                {
                    SceneLocalPlayerAdmissionAuthoringUtility.Validate(
                        (SceneLocalPlayerAdmissionAuthoring)target,
                        true);
                    serializedObject.Update();
                }
            }

            DrawRuntimeControls();
            DrawDebug();
        }

        private void DrawRuntimeControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Transaction", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "P3M4B1 exposes explicit manual admission/release only. On Activity Enter timing remains authoring intent until the lifecycle integration gate is validated.",
                MessageType.None);

            SceneLocalPlayerAdmissionAuthoring authoring =
                (SceneLocalPlayerAdmissionAuthoring)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying || !authoring.RuntimeReady))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Admit Now"))
                {
                    authoring.RequestAdmission(
                        nameof(SceneLocalPlayerAdmissionAuthoringEditor),
                        "inspector-manual-admission");
                    Repaint();
                }

                if (GUILayout.Button("Release Now"))
                {
                    authoring.RequestRelease(
                        nameof(SceneLocalPlayerAdmissionAuthoringEditor),
                        "inspector-manual-release");
                    Repaint();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime admission controls are available only in Play Mode.",
                    MessageType.Info);
            }
            else if (!authoring.RuntimeReady)
            {
                EditorGUILayout.HelpBox(authoring.RuntimeDiagnostic, MessageType.Warning);
            }
        }

        private void DrawDebug()
        {
            EditorGUILayout.Space();
            showDebug = EditorGUILayout.Foldout(showDebug, "Advanced / Debug", true);
            if (!showDebug)
            {
                return;
            }

            SceneLocalPlayerAdmissionAuthoring authoring =
                (SceneLocalPlayerAdmissionAuthoring)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Authoring Status", authoring.LastAuthoringStatus);
                EditorGUILayout.TextArea(
                    authoring.LastAuthoringDiagnostic,
                    GUILayout.MinHeight(54));
                EditorGUILayout.TextField(
                    "Player Slot Id",
                    authoring.TryGetPlayerSlotId(out var slot, out _)
                        ? slot.StableText
                        : string.Empty);
                EditorGUILayout.Toggle(
                    "Host Joined",
                    authoring.LocalPlayerHost != null && authoring.LocalPlayerHost.IsJoined);
                EditorGUILayout.Toggle(
                    "Typed Actor Evidence",
                    authoring.SceneLogicalPlayerActor != null &&
                    authoring.SceneLogicalPlayerActor.GetComponent<SceneLogicalPlayerActorEvidence>() != null);
                EditorGUILayout.Toggle("Runtime Ready", authoring.RuntimeReady);
                EditorGUILayout.Toggle("Active Admission", authoring.HasActiveAdmission);
                EditorGUILayout.TextArea(
                    authoring.RuntimeDiagnostic,
                    GUILayout.MinHeight(54));
                if (authoring.LastRuntimeResult != null)
                {
                    EditorGUILayout.EnumPopup(
                        "Runtime Status",
                        authoring.LastRuntimeResult.Status);
                    EditorGUILayout.TextField(
                        "Admission Token",
                        authoring.LastRuntimeResult.Token.StableText);
                }
            }
        }

        [MenuItem("GameObject/Immersive Framework/Player/Scene Local Player Admission", false, 20)]
        private static void CreateSurface(MenuCommand command)
        {
            var root = new GameObject("Scene Local Player Admission");
            Undo.RegisterCreatedObjectUndo(root, "Create Scene Local Player Admission");
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            root.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            Selection.activeGameObject = root;
        }
    }
}
