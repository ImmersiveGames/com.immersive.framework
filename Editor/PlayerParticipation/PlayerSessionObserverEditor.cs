using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionObserver))]
    internal sealed class PlayerSessionObserverEditor : UnityEditor.Editor
    {
        private SerializedProperty _scope;
        private SerializedProperty _onJoiningOpened;
        private SerializedProperty _onJoiningClosed;
        private SerializedProperty _onPlayerJoined;
        private SerializedProperty _onPlayerLeft;
        private SerializedProperty _onActorSelected;
        private SerializedProperty _onActorChanged;
        private SerializedProperty _onActorCleared;
        private bool _hasValidation;
        private bool _validationIsValid;
        private bool _validationOutdated;
        private string _validationIssue;
        private bool _showAdvanced;

        private void OnEnable()
        {
            if (!HasLiveTargets())
            {
                return;
            }

            _scope = serializedObject.FindProperty("scope");
            _onJoiningOpened = serializedObject.FindProperty("onJoiningOpened");
            _onJoiningClosed = serializedObject.FindProperty("onJoiningClosed");
            _onPlayerJoined = serializedObject.FindProperty("onPlayerJoined");
            _onPlayerLeft = serializedObject.FindProperty("onPlayerLeft");
            _onActorSelected = serializedObject.FindProperty("onActorSelected");
            _onActorChanged = serializedObject.FindProperty("onActorChanged");
            _onActorCleared = serializedObject.FindProperty("onActorCleared");
        }

        public override void OnInspectorGUI()
        {
            if (!HasLiveTargets())
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var observer = (PlayerSessionObserver)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "PLAYER SESSION OBSERVER",
                string.Empty);
            FrameworkAuthoringInspectorGui.Section("Scope");
            EditorGUILayout.PropertyField(
                _scope,
                new GUIContent(
                    "Scope",
                    "Explicit Route or Activity scope for this read-only observer. Framework Core supplies scoped access directly at runtime."));

            DrawEvents();

            DrawValidation(observer);

            if (Application.isPlaying && targets.Length == 1)
            {
                DrawRuntimeObservation(observer);
            }

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced(observer);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _validationOutdated = _hasValidation;
                _hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEvents()
        {
            FrameworkAuthoringInspectorGui.Section("Events");
            EditorGUILayout.LabelField("Joining", EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    _onJoiningOpened,
                    new GUIContent("On Joining Opened"));
                EditorGUILayout.PropertyField(
                    _onJoiningClosed,
                    new GUIContent("On Joining Closed"));
            }

            EditorGUILayout.LabelField("Player", EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    _onPlayerJoined,
                    new GUIContent("On Player Joined"));
                EditorGUILayout.PropertyField(
                    _onPlayerLeft,
                    new GUIContent("On Player Left"));
            }

            EditorGUILayout.LabelField("Actor", EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    _onActorSelected,
                    new GUIContent("On Actor Selected"));
                EditorGUILayout.PropertyField(
                    _onActorChanged,
                    new GUIContent("On Actor Changed"));
                EditorGUILayout.PropertyField(
                    _onActorCleared,
                    new GUIContent("On Actor Cleared"));
            }
        }

        private void DrawValidation(PlayerSessionObserver observer)
        {
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
                _hasValidation = true;
                _validationOutdated = false;
                _validationIsValid = observer.TryValidateConfiguration(
                    out _validationIssue);
            }

            string state = !_hasValidation
                ? _validationOutdated ? "Outdated" : "Not Validated"
                : _validationIsValid ? "Valid" : "Issue";
            EditorGUILayout.LabelField("Status", state);
            if (_hasValidation && !_validationIsValid)
            {
                EditorGUILayout.LabelField(
                    "Issue",
                    _validationIssue,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawRuntimeObservation(PlayerSessionObserver observer)
        {
            FrameworkAuthoringInspectorGui.Section("Session");
            EditorGUILayout.LabelField("Availability", observer.Availability.ToString());
            EditorGUILayout.LabelField("Activity", observer.ActivitySummary);

            FrameworkAuthoringInspectorGui.Section("Players");
            LocalPlayerProvisioningConsumerObservationSnapshot observation =
                observer.CurrentObservation;
            if (observation == null || observation.Slots.Count == 0)
            {
                EditorGUILayout.LabelField("No Player Slot evidence is published.");
                return;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                string title = slot.Slot.PlayerSlotId.IsValid
                    ? slot.Slot.PlayerSlotId.StableText
                    : "Unavailable Slot";
                EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Lifecycle", observer.DescribeSlotLifecycle(slot));
                    EditorGUILayout.LabelField("Selected Actor", observer.DescribeSelectedActor(slot));
                    EditorGUILayout.LabelField("Gameplay", observer.DescribeGameplay(slot));
                }
            }
        }

        private static void DrawAdvanced(PlayerSessionObserver observer)
        {
            EditorGUILayout.LabelField("Scoped Access State", observer.ScopedAccessState.ToString());
            EditorGUILayout.LabelField(
                "Diagnostic",
                observer.Diagnostic,
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(
                "Initialization Evidence",
                observer.InitializationSummary,
                EditorStyles.wordWrappedMiniLabel);

            LocalPlayerProvisioningConsumerObservationSnapshot observation =
                observer.CurrentObservation;
            if (observation == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Scope Owner", observation.ScopeOwner.StableText);
            EditorGUILayout.LabelField("Activity Occurrence", observation.ActivityOccurrence.ToString());
            EditorGUILayout.LabelField("Session Revision", observation.SessionRevision.ToString());
            EditorGUILayout.LabelField("Applied Session Revision", observation.AppliedSessionRevision.ToString());
        }

        private bool HasLiveTargets()
        {
            if (target == null || targets == null || targets.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
