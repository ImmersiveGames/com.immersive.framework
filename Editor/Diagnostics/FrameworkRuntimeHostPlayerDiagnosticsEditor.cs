using Immersive.Framework.ApplicationLifecycle;
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
    }
}
