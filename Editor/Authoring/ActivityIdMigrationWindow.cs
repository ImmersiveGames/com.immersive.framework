using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    internal sealed class ActivityIdMigrationWindow : EditorWindow
    {
        private readonly List<ActivityAsset> _preview = new List<ActivityAsset>();
        private readonly Dictionary<ActivityAsset, string> _suggestions = new Dictionary<ActivityAsset, string>();
        private Vector2 _scroll;
        private string _summary = "Run Preview to inspect missing or non-canonical Activity IDs.";

        [MenuItem("Immersive Framework/Authoring/Migrate Activity IDs")]
        private static void Open()
        {
            GetWindow<ActivityIdMigrationWindow>("Activity ID Migration");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Manual Editor-only migration. Canonical Activity IDs use lowercase dot-separated segments. Preview never changes assets. Apply records Undo and skips collision candidates.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview"))
                {
                    BuildPreview();
                }

                using (new EditorGUI.DisabledScope(_preview.Count == 0))
                {
                    if (GUILayout.Button("Apply Suggested IDs"))
                    {
                        ApplyPreview();
                    }
                }
            }

            EditorGUILayout.HelpBox(_summary, MessageType.None);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int index = 0; index < _preview.Count; index++)
            {
                ActivityAsset activity = _preview[index];
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(activity, typeof(ActivityAsset), false);
                    EditorGUILayout.TextField("Suggested ID", _suggestions[activity]);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void BuildPreview()
        {
            _preview.Clear();
            _suggestions.Clear();
            var targetCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:ActivityAsset");
            for (int index = 0; index < guids.Length; index++)
            {
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(AssetDatabase.GUIDToAssetPath(guids[index]));
                if (activity == null)
                {
                    continue;
                }

                string current = activity.HasValidActivityId ? activity.ActivityId.StableText : activity.ActivityName;
                string suggestion = NormalizeCanonicalId(current);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    targetCounts[suggestion] = targetCounts.TryGetValue(suggestion, out int count) ? count + 1 : 1;
                }
            }

            int collisions = 0;
            int alreadyCanonical = 0;
            for (int index = 0; index < guids.Length; index++)
            {
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(AssetDatabase.GUIDToAssetPath(guids[index]));
                if (activity == null)
                {
                    continue;
                }

                string current = activity.HasValidActivityId ? activity.ActivityId.StableText : activity.ActivityName;
                string suggestion = NormalizeCanonicalId(current);
                if (string.IsNullOrEmpty(suggestion) || !targetCounts.TryGetValue(suggestion, out int count) || count != 1)
                {
                    collisions++;
                    continue;
                }

                if (activity.HasValidActivityId && string.Equals(activity.ActivityId.StableText, suggestion, StringComparison.Ordinal))
                {
                    alreadyCanonical++;
                    continue;
                }

                _preview.Add(activity);
                _suggestions.Add(activity, suggestion);
            }

            _summary = $"Preview: candidates='{_preview.Count}' alreadyCanonical='{alreadyCanonical}' collisionsOrIgnored='{collisions}'. Collisions are not overwritten.";
        }

        private void ApplyPreview()
        {
            int changed = 0;
            for (int index = 0; index < _preview.Count; index++)
            {
                ActivityAsset activity = _preview[index];
                if (activity == null || !_suggestions.TryGetValue(activity, out string suggestion))
                {
                    continue;
                }

                Undo.RecordObject(activity, "Migrate Activity ID");
                var serialized = new SerializedObject(activity);
                serialized.FindProperty("activityId").stringValue = suggestion.Trim();
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(activity);
                changed++;
            }

            AssetDatabase.SaveAssets();
            _summary = $"Apply: changed='{changed}'. Re-run Preview to verify idempotence and collisions.";
            _preview.Clear();
            _suggestions.Clear();
        }

        private static string NormalizeCanonicalId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(value.Length);
            bool pendingSeparator = false;
            foreach (char character in value.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0)
                    {
                        builder.Append('.');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                    pendingSeparator = false;
                }
                else
                {
                    pendingSeparator = builder.Length > 0;
                }
            }

            return builder.ToString();
        }
    }
}
