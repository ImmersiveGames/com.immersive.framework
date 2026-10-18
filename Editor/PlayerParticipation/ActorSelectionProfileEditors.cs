using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(ActorProfile))]
    internal sealed class ActorProfileEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 72f;
        private const float PreviewHeight = 78f;
        private const float PreviewPadding = 6f;

        private SerializedProperty _actorProfileId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _icon;
        private SerializedProperty _actorKind;
        private SerializedProperty _actorRole;
        private SerializedProperty _baseContentParts;

        private bool _showAdvanced;
        private bool _hasValidationResult;
        private FrameworkAuthoringValidationReport _validationReport;

        private void OnEnable()
        {
            _actorProfileId = serializedObject.FindProperty("actorProfileId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _icon = serializedObject.FindProperty("icon");
            _actorKind = serializedObject.FindProperty("actorKind");
            _actorRole = serializedObject.FindProperty("actorRole");
            _baseContentParts = serializedObject.FindProperty("baseContentParts");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawIdentityAndMetadata();
            DrawPresentation();
            bool normalAuthoringChanged = EditorGUI.EndChangeCheck();

            bool normalPropertiesApplied = serializedObject.ApplyModifiedProperties();
            if (normalAuthoringChanged || normalPropertiesApplied)
            {
                ClearValidationResult();
            }

            DrawConfigurationStatus();
            DrawAdvancedSection();
        }

        private void DrawIdentityAndMetadata()
        {
            DrawSection("Identity / Metadata");

            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Designer-facing name used in Actor selection and diagnostics."));

            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional designer-facing description of this Actor option."));

            EditorGUILayout.PropertyField(
                _icon,
                new GUIContent(
                    "Icon",
                    "Optional Sprite used to recognize this Actor option."));

            DrawActorPreview();
            EditorGUILayout.PropertyField(
                _actorKind,
                new GUIContent(
                    "Actor Kind",
                    "Broad framework Actor category. This is not a project-specific class taxonomy."));

            EditorGUILayout.PropertyField(
                _actorRole,
                new GUIContent(
                    "Actor Role",
                    "Broad framework Actor role. This is not a loadout, team or character class."));
        }

        private void DrawActorPreview()
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, PreviewHeight);
            Rect frameRect = new Rect(
                rowRect.x,
                rowRect.y + 3f,
                PreviewSize,
                PreviewSize);

            GUI.Box(frameRect, GUIContent.none);

            Sprite sprite = _icon.objectReferenceValue as Sprite;
            Rect contentRect = new Rect(
                frameRect.x + PreviewPadding,
                frameRect.y + PreviewPadding,
                frameRect.width - (PreviewPadding * 2f),
                frameRect.height - (PreviewPadding * 2f));

            if (sprite != null)
            {
                Texture2D previewTexture = AssetPreview.GetAssetPreview(sprite);
                if (previewTexture == null)
                {
                    previewTexture = AssetPreview.GetMiniThumbnail(sprite);
                }

                if (previewTexture != null)
                {
                    GUI.DrawTexture(
                        contentRect,
                        previewTexture,
                        ScaleMode.ScaleToFit,
                        true);
                }
                else
                {
                    DrawCenteredPreviewLabel(contentRect, "Loading…");
                }

                if (AssetPreview.IsLoadingAssetPreview(sprite.GetEntityId()))
                {
                    Repaint();
                }
            }
            else
            {
                DrawCenteredPreviewLabel(contentRect, "No Icon");
            }

            float textX = frameRect.xMax + 10f;
            float textWidth = Mathf.Max(0f, rowRect.xMax - textX);
            string displayName = string.IsNullOrWhiteSpace(_displayName.stringValue)
                ? target.name
                : _displayName.stringValue.Trim();
            string actorId = string.IsNullOrWhiteSpace(_actorProfileId.stringValue)
                ? "No Actor Profile ID"
                : _actorProfileId.stringValue.Trim();
            string classification =
                _actorKind.hasMultipleDifferentValues ||
                _actorRole.hasMultipleDifferentValues
                    ? "Mixed classification"
                    : $"{(ActorKind)_actorKind.intValue} / {(ActorRole)_actorRole.intValue}";

            GUI.Label(
                new Rect(textX, rowRect.y + 9f, textWidth, EditorGUIUtility.singleLineHeight),
                displayName,
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(textX, rowRect.y + 31f, textWidth, EditorGUIUtility.singleLineHeight),
                classification,
                EditorStyles.miniLabel);
            GUI.Label(
                new Rect(textX, rowRect.y + 50f, textWidth, EditorGUIUtility.singleLineHeight),
                actorId,
                EditorStyles.miniLabel);
        }

        private static void DrawCenteredPreviewLabel(Rect rect, string text)
        {
            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true
            };
            GUI.Label(rect, text, style);
        }

        private void DrawPresentation()
        {
            DrawSection("Base Composition");

            EditorGUILayout.PropertyField(
                _baseContentParts,
                new GUIContent(
                    "Base Content Parts",
                    "Ordered references to keyed part assets for this Actor. The 'visual' key is mounted below Visual Content Mount."),
                true);

            EditorGUILayout.HelpBox(
                "Part assets can be shared intentionally. Editing a shared asset changes every Actor Profile that references it.",
                MessageType.Info);

            ActorProfile profile = (ActorProfile)target;
            if (profile.HasLegacyCompositionData)
            {
                EditorGUILayout.HelpBox(
                    "This profile still contains inline or legacy composition data. Runtime materialization uses only Base Content Part assets; run the explicit migration below.",
                    MessageType.Warning);
                if (GUILayout.Button("Migrate This Actor Profile"))
                {
                    serializedObject.ApplyModifiedProperties();
                    ActorProfileVisualContentMigrationResult result =
                        ActorProfileVisualContentMigration.MigrateAsset((ActorProfile)target);
                    serializedObject.Update();
                    ClearValidationResult();
                    if (result.Status is ActorProfileVisualContentMigrationStatus.Invalid or
                        ActorProfileVisualContentMigrationStatus.Conflict or
                        ActorProfileVisualContentMigrationStatus.Blocked)
                    {
                        Debug.LogWarning($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}", target);
                    }
                    else
                    {
                        Debug.Log($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}", target);
                    }
                }
            }

            if (GUILayout.Button("Create Base Content Part Asset"))
            {
                CreateBaseContentPartAsset(profile);
            }
        }

        private void CreateBaseContentPartAsset(ActorProfile profile)
        {
            string profilePath = AssetDatabase.GetAssetPath(profile);
            if (!profilePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !AssetDatabase.IsOpenForEdit(profilePath))
            {
                EditorUtility.DisplayDialog(
                    "Cannot Create Part Asset",
                    "Actor Profile part assets can only be created beneath an editable Assets folder. Copy this Profile into Assets before authoring new parts.",
                    "OK");
                return;
            }

            string profileDirectory = Path.GetDirectoryName(profilePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(profileDirectory))
            {
                EditorUtility.DisplayDialog("Cannot Create Part Asset", "The Actor Profile folder could not be resolved.", "OK");
                return;
            }

            string folderName = SanitizeFileName(Path.GetFileNameWithoutExtension(profilePath)) + "_BaseContentParts";
            string partFolder = $"{profileDirectory}/{folderName}";
            if (!AssetDatabase.IsValidFolder(partFolder))
            {
                string folderGuid = AssetDatabase.CreateFolder(profileDirectory, folderName);
                if (string.IsNullOrEmpty(folderGuid))
                {
                    EditorUtility.DisplayDialog("Cannot Create Part Asset", $"Could not create '{partFolder}'.", "OK");
                    return;
                }
            }

            string key = NextPartKey(profile.BaseContentParts);
            var originalReferences = new List<ActorCompositionPartAsset>(profile.BaseContentParts);
            ActorCompositionPartAsset part = ScriptableObject.CreateInstance<ActorCompositionPartAsset>();
            part.Initialize(key, null);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{partFolder}/{profile.BaseContentParts.Count:D2}_{SanitizeFileName(key)}.asset");
            bool assetCreated = false;
            bool profileReferencesChanged = false;
            try
            {
                AssetDatabase.CreateAsset(part, assetPath);
                assetCreated = true;
                Undo.RecordObject(profile, "Add Actor Profile Base Content Part");
                SerializedProperty references = serializedObject.FindProperty("baseContentParts");
                int newIndex = references.arraySize;
                references.InsertArrayElementAtIndex(newIndex);
                references.GetArrayElementAtIndex(newIndex).objectReferenceValue = part;
                profileReferencesChanged = true;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                EditorGUIUtility.PingObject(part);
                Selection.activeObject = part;
            }
            catch (Exception exception)
            {
                bool restored = !profileReferencesChanged || TryRestoreBaseReferences(profile, originalReferences);
                if (restored && assetCreated)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                if (restored && part != null && !AssetDatabase.Contains(part))
                {
                    UnityEngine.Object.DestroyImmediate(part);
                }

                string recovery = restored
                    ? "The Actor Profile reference list was restored."
                    : "The Profile reference rollback failed; the created asset was retained to avoid a broken reference. Inspect the Profile and asset before retrying.";
                Debug.LogError(
                    $"[IF-ADR-050] Could not create Base content part asset for '{profile.name}'. {exception.GetType().Name}: {exception.Message} {recovery}",
                    profile);
            }

            serializedObject.Update();
            ClearValidationResult();
        }

        private static bool TryRestoreBaseReferences(
            ActorProfile profile,
            IReadOnlyList<ActorCompositionPartAsset> originalReferences)
        {
            try
            {
                Undo.RecordObject(profile, "Restore Actor Profile Base Content Parts");
                var restore = new SerializedObject(profile);
                SerializedProperty references = restore.FindProperty("baseContentParts");
                if (references == null || !references.isArray)
                {
                    return false;
                }

                references.arraySize = originalReferences.Count;
                for (int index = 0; index < originalReferences.Count; index++)
                {
                    references.GetArrayElementAtIndex(index).objectReferenceValue = originalReferences[index];
                }

                restore.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NextPartKey(IReadOnlyList<ActorCompositionPartAsset> parts)
        {
            bool hasVisual = false;
            var used = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < parts.Count; index++)
            {
                ActorCompositionPartAsset part = parts[index];
                if (part == null)
                {
                    continue;
                }

                used.Add(part.Key);
                hasVisual |= string.Equals(part.Key, "visual", StringComparison.Ordinal);
            }

            if (!hasVisual)
            {
                return "visual";
            }

            for (int suffix = 1; ; suffix++)
            {
                string candidate = $"part-{suffix}";
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string SanitizeFileName(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "Part" : value.Trim();
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                normalized = normalized.Replace(invalidCharacter, '_');
            }

            return normalized;
        }

        private void DrawConfigurationStatus()
        {
            DrawSection("Configuration Status");

            if (GUILayout.Button("Validate"))
            {
                RunValidation();
            }

            DrawValidationSummary();
        }

        private void RunValidation()
        {
            _validationReport =
                PlayerActorSelectionAuthoringValidator.ValidateActorProfile(
                    (ActorProfile)target,
                    true);
            _hasValidationResult = true;
        }

        private void ClearValidationResult()
        {
            _validationReport = null;
            _hasValidationResult = false;
            Repaint();
        }

        private void DrawValidationSummary()
        {
            if (!_hasValidationResult || _validationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(_validationReport);
            FrameworkAuthoringValidationGui.DrawIssues(_validationReport, false);
        }

        private void DrawAdvancedSection()
        {
            EditorGUILayout.Space(6f);
            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                "Advanced / Debug",
                true);

            if (!_showAdvanced)
            {
                return;
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                _actorProfileId,
                new GUIContent(
                    "Actor Profile ID",
                    "Canonical stable ActorProfileId owned by this Profile. Changing it may invalidate external references."));

            string suggestedId = PlayerProfileIdSuggestionUtility.SuggestActorProfileId(
                _displayName.stringValue,
                target.name);
            DrawIdSuggestion(
                _actorProfileId,
                suggestedId);

            bool advancedAuthoringChanged = EditorGUI.EndChangeCheck();
            bool advancedPropertiesApplied = serializedObject.ApplyModifiedProperties();
            if (advancedAuthoringChanged || advancedPropertiesApplied)
            {
                ClearValidationResult();
            }

            DrawAdvancedEvidence((ActorProfile)target);
        }

        private static void DrawAdvancedEvidence(ActorProfile profile)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Asset Path",
                    AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.TextField(
                    "Normalized Identity",
                    profile.ActorProfileIdText);

                string typedIdentity = profile.TryGetActorProfileId(
                    out ActorProfileId actorProfileId,
                    out string issue)
                    ? actorProfileId.ToString()
                    : $"Invalid: {issue}";
                EditorGUILayout.TextField(
                    "Typed ActorProfileId",
                    typedIdentity);

                profile.TryGetVisualContentPrefab(out GameObject visualContent);
                EditorGUILayout.TextField(
                    "Base Visual Asset Path",
                    visualContent != null
                        ? AssetDatabase.GetAssetPath(visualContent)
                        : string.Empty);
                EditorGUILayout.Toggle(
                    "Defined Actor Kind",
                    profile.HasDefinedActorKind);
                EditorGUILayout.Toggle(
                    "Defined Actor Role",
                    profile.HasDefinedActorRole);
                EditorGUILayout.Toggle(
                    "Visual Content Assigned",
                    profile.HasVisualContentPrefab);
            }
        }

        private static void DrawIdSuggestion(
            SerializedProperty idProperty,
            string suggestedId)
        {
            EditorGUILayout.LabelField(
                "Suggested ID",
                string.IsNullOrWhiteSpace(suggestedId) ? "Unavailable" : suggestedId,
                EditorStyles.miniLabel);

            bool canApply =
                !idProperty.hasMultipleDifferentValues &&
                string.IsNullOrWhiteSpace(idProperty.stringValue) &&
                !string.IsNullOrWhiteSpace(suggestedId);

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button(new GUIContent(
                    "Use Suggested ID",
                    "Applies the displayed deterministic suggestion only while the ID is empty. Run Validate afterward to check project uniqueness.")))
                {
                    idProperty.stringValue = suggestedId;
                    GUI.FocusControl(null);
                }
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }

    [CustomEditor(typeof(ActorCompositionPartAsset))]
    internal sealed class ActorCompositionPartAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("key"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dependencies"), true);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox(
                "Assigning this same asset to multiple Actor Profiles is intentional sharing; edits apply to every reference.",
                MessageType.Info);
        }
    }
}
