using Immersive.Framework.CycleReset;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(RouteCycleResetTrigger))]
    [CanEditMultipleObjects]
    internal sealed class RouteCycleResetTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _reason;

        private void OnEnable()
        {
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Route Cycle Reset Trigger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Requests a Route Cycle Reset through the framework runtime. This is a cycle-level reset only: it does not reset a specific object, component, Player, Actor, pool, save snapshot or scene reload.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostics reason. Keep it route/activity-cycle oriented. Avoid object/player/component wording; those reset levels are later phases."));

            DrawReasonGuardrail(_reason);

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Expected F12 behaviour: with no reset participants discovered yet, a successful trigger request can report SucceededNoParticipants. That is valid until local/object reset participants exist.",
                MessageType.Info);

            DrawRuntimeResult();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeResult()
        {
            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            var trigger = target as RouteCycleResetTrigger;
            if (trigger == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Result", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Phase", trigger.LastEventPhase.ToString());
            EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());
            EditorGUILayout.LabelField("Last Result Status", trigger.LastResultStatus.ToString());

            if (!string.IsNullOrWhiteSpace(trigger.LastReason))
            {
                EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            }

            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.HelpBox(trigger.LastMessage, ResolveRuntimeMessageType(trigger));
            }

            if (trigger.HasLastResult)
            {
                EditorGUILayout.LabelField("Participants", trigger.LastParticipantCount.ToString());
                EditorGUILayout.LabelField("Succeeded / Skipped / Failed", $"{trigger.LastSucceededParticipantCount} / {trigger.LastSkippedParticipantCount} / {trigger.LastFailedParticipantCount}");
                EditorGUILayout.LabelField("Blocking / Non-blocking Issues", $"{trigger.LastBlockingIssueCount} / {trigger.LastNonBlockingIssueCount}");
                EditorGUILayout.HelpBox(trigger.LastResultSummary, MessageType.None);
            }
        }

        private static MessageType ResolveRuntimeMessageType(RouteCycleResetTrigger trigger)
        {
            if (trigger.LastRequestFailed)
            {
                return MessageType.Error;
            }

            if (trigger.LastRequestIgnored || trigger.LastResultCompletedWithWarnings)
            {
                return MessageType.Warning;
            }

            return MessageType.Info;
        }

        private static void DrawReasonGuardrail(SerializedProperty reason)
        {
            if (reason == null || reason.hasMultipleDifferentValues)
            {
                return;
            }

            var value = reason.stringValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                EditorGUILayout.HelpBox(
                    "No custom reason set. Runtime diagnostics will use the default Route Cycle Reset reason.",
                    MessageType.Info);
                return;
            }

            if (CycleResetTriggerAuthoringText.ContainsFutureResetVocabulary(value))
            {
                EditorGUILayout.HelpBox(
                    "The reason contains object/component/player/actor/pool/save/reload vocabulary. This trigger only requests Route Cycle Reset; use cycle-oriented wording to avoid confusing this with future local reset phases.",
                    MessageType.Warning);
            }
        }
    }
}
