using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityLocalVisibilityAdapter))]
    [CanEditMultipleObjects]
    internal sealed class ActivityLocalVisibilityAdapterEditor : UnityEditor.Editor
    {
        private SerializedProperty _activity;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;

        private void OnEnable()
        {
            _activity = serializedObject.FindProperty("activity");
            _localContentId = serializedObject.FindProperty("localContentId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity Local Visibility Adapter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Marks this GameObject as scene-authored content for one Activity. Activity Flow activates it when the assigned Activity is active and deactivates it otherwise.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activity, new GUIContent("Activity"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Local Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _localContentId,
                new GUIContent(
                    "Local Content Id",
                    "Explicit local id required by F5 local identity. GameObject name and hierarchy path are diagnostics only and are not used as fallback."));
            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Authoring policy recorded by F5F. Required contributions can block future consumers; Optional contributions can be skipped with diagnostics. Absence validation is not active yet."));
            DrawLocalIdentityStatus();

            DrawActivityStatus();
            DrawHierarchyGuardrails();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This adapter only controls the active state of this GameObject. It does not spawn, pool, reset, save, bind actors, bind input, bind camera or load Activity content.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawAuthoringValidation()
        {
            var report = new FrameworkAuthoringValidationReport();
            for (int i = 0; i < targets.Length; i++)
            {
                report.AddRange(FrameworkAuthoringValidator.ValidateActivityLocalVisibilityAdapter(targets[i] as ActivityLocalVisibilityAdapter));
            }

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawLocalIdentityStatus()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                if (_localContentId.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox(
                        "Multiple selected adapters have different Local Content Id values.",
                        MessageType.Info);
                    return;
                }
            }

            if (_localContentId == null || string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Local Content Id is required for F5 local identity. GameObject names and hierarchy paths are diagnostics only and are not fallback identities.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                $"This Activity local visibility contribution uses explicit local id '{_localContentId.stringValue.Trim()}' and requiredness '{GetRequirednessLabel()}'.",
                MessageType.Info);
        }

        private string GetRequirednessLabel()
        {
            return _requiredness != null && !_requiredness.hasMultipleDifferentValues
                ? _requiredness.enumDisplayNames[_requiredness.enumValueIndex]
                : "Mixed";
        }

        private void DrawActivityStatus()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                if (_activity.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox(
                        "Multiple selected adapters have different Activity references.",
                        MessageType.Info);
                    return;
                }
            }

            if (_activity.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Activity is missing. This adapter will be skipped by Activity Content Runtime and will produce a runtime warning.",
                    MessageType.Error);
                return;
            }

            var activityAsset = _activity.objectReferenceValue as ActivityAsset;
            string activityName = activityAsset != null ? activityAsset.ActivityName : _activity.objectReferenceValue.name;

            EditorGUILayout.HelpBox(
                $"This GameObject is authored as content for Activity '{activityName}'. It will be active only while that Activity is active.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Activity", GUILayout.Width(140)))
                {
                    Selection.activeObject = _activity.objectReferenceValue;
                }
            }
        }

        private void DrawHierarchyGuardrails()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var binding = target as ActivityLocalVisibilityAdapter;
            if (binding == null)
            {
                return;
            }

            var parentBinding = FindParentBinding(binding);
            if (parentBinding != null)
            {
                EditorGUILayout.HelpBox(
                    $"A parent GameObject also has Activity Local Visibility Adapter: '{parentBinding.gameObject.name}'. Nested Activity local visibility adapter policy does not exist yet. Keep Activity local visibility adapter roots flat for now.",
                    MessageType.Warning);
            }

            int childBindingCount = CountChildBindings(binding);
            if (childBindingCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"This GameObject contains {childBindingCount} child Activity Local Visibility Adapter component(s). Nested Activity local visibility adapter policy does not exist yet. Keep Activity local visibility adapter roots flat for now.",
                    MessageType.Warning);
            }
        }

        private static ActivityLocalVisibilityAdapter FindParentBinding(ActivityLocalVisibilityAdapter binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<ActivityLocalVisibilityAdapter>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildBindings(ActivityLocalVisibilityAdapter binding)
        {
            ActivityLocalVisibilityAdapter[] all = binding.GetComponentsInChildren<ActivityLocalVisibilityAdapter>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != binding)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
