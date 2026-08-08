using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Diagnostics
{
    [CustomEditor(typeof(FrameworkRuntimeHost))]
    internal sealed class FrameworkRuntimeHostPlayerDiagnosticsEditor : UnityEditor.Editor
    {
        private bool _advanced;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            _advanced = EditorGUILayout.Foldout(_advanced, "Advanced / Debug", true);
            if (!_advanced)
            {
                return;
            }

            FrameworkRuntimeHost host = (FrameworkRuntimeHost)target;
            DrawPlayerSessionInitialization(host);
            var snapshot = host.SceneLocalPlayerAdmissionDiagnostics;
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Scene-Provided Admissions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Count", snapshot.ActiveAdmissionCount.ToString());
            EditorGUILayout.LabelField("Occupied Slot Count", snapshot.OccupiedSlotCount.ToString());
            EditorGUILayout.LabelField("Last Operation", snapshot.Operation);
            EditorGUILayout.LabelField("Last Status", snapshot.Status.ToString());
            EditorGUILayout.LabelField("Last Slot", snapshot.PlayerSlotId.IsValid ? snapshot.PlayerSlotId.StableText : "<none>");
            EditorGUILayout.LabelField("Last Actor", snapshot.ActorId.IsValid ? snapshot.ActorId.StableText : "<none>");
            EditorGUILayout.LabelField("Last Source", snapshot.Source);
            EditorGUILayout.LabelField("Last Reason", snapshot.Reason);
            EditorGUILayout.LabelField("Release Succeeded", snapshot.ReleaseSucceeded ? "Yes" : "No");
            EditorGUILayout.LabelField("Already Released", snapshot.AlreadyReleased ? "Yes" : "No");
            EditorGUILayout.LabelField("Host Evidence Present", snapshot.HostEvidencePresent ? "Yes" : "No");
            EditorGUILayout.HelpBox(snapshot.Message, snapshot.ReleaseRequested && !snapshot.ReleaseSucceeded && !snapshot.AlreadyReleased ? MessageType.Warning : MessageType.Info);
            EditorGUILayout.SelectableLabel(snapshot.ToDiagnosticString(), EditorStyles.textArea, GUILayout.MinHeight(56f));
        }

        private static void DrawPlayerSessionInitialization(
            FrameworkRuntimeHost host)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                "Player Session Initialization",
                EditorStyles.boldLabel);

            PlayerParticipationRuntimeHostModule module =
                host.GetComponent<PlayerParticipationRuntimeHostModule>();
            if (module == null)
            {
                EditorGUILayout.HelpBox(
                    "No Player Session runtime is attached to this host.",
                    MessageType.Info);
                return;
            }

            PlayerParticipationOperationResult initialization =
                module.InitializationResult;
            EditorGUILayout.LabelField(
                "Runtime Status",
                initialization != null ? initialization.Status.ToString() : "Unavailable");
            EditorGUILayout.LabelField(
                "Runtime Operation",
                initialization != null ? initialization.Operation : "Unavailable");
            EditorGUILayout.HelpBox(
                initialization != null
                    ? initialization.Message
                    : "No runtime initialization result is available.",
                initialization != null && initialization.Succeeded
                    ? MessageType.Info
                    : MessageType.Warning);

            EffectivePlayerSessionConfiguration configuration =
                module.EffectiveConfiguration;
            if (configuration == null)
            {
                EditorGUILayout.HelpBox(
                    "The host has no retained effective Player Session configuration.",
                    MessageType.Warning);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Initial Capacity",
                    configuration.InitialCapacity);
                EditorGUILayout.Toggle(
                    "Initial Joining Open",
                    configuration.InitialJoiningOpen);
                EditorGUILayout.EnumPopup(
                    "Actor Resolution Policy",
                    configuration.ActorResolutionPolicy);
            }

            EditorGUILayout.LabelField(
                "Frozen Effective Slot Order",
                EditorStyles.miniBoldLabel);
            for (int index = 0; index < configuration.Slots.Count; index++)
            {
                EffectivePlayerSlotProvisioning slot =
                    configuration.Slots[index];
                string slotName = slot.PlayerSlotProfile != null
                    ? slot.PlayerSlotProfile.name
                    : slot.PlayerSlotId.StableText;
                EditorGUILayout.LabelField(
                    $"{index + 1}. {slotName}",
                    slot.HostProvisioningMode.ToString());
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "    Default Actor (Captured)",
                        slot.DefaultActorProfile,
                        typeof(ActorProfile),
                        false);
                }
            }
        }
    }
}
