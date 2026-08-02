using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
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
        private SerializedProperty _logicalActorHostPrefab;

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
            _logicalActorHostPrefab = serializedObject.FindProperty("logicalActorHostPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawActor();
            DrawClassification();
            DrawLogicalComposition();
            bool normalAuthoringChanged = EditorGUI.EndChangeCheck();

            bool normalPropertiesApplied = serializedObject.ApplyModifiedProperties();
            if (normalAuthoringChanged || normalPropertiesApplied)
            {
                ClearValidationResult();
            }

            DrawProductActions();
            DrawValidationSummary();
            DrawAdvancedSection();
        }

        private void DrawActor()
        {
            DrawSection("Actor");

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

        private void DrawClassification()
        {
            DrawSection("Classification");

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

        private void DrawLogicalComposition()
        {
            DrawSection("Logical Composition");

            EditorGUILayout.PropertyField(
                _logicalActorHostPrefab,
                new GUIContent(
                    "Logical Actor Host Prefab",
                    "Canonical prefab used when a workflow materializes or verifies this Logical Actor. The Actor Profile does not instantiate it by itself."));
        }

        private void DrawProductActions()
        {
            DrawSection("Product Actions");

            if (GUILayout.Button("Validate"))
            {
                RunValidation();
            }
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
            DrawSection("Validation Summary");

            if (!_hasValidationResult || _validationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Not Validated",
                    EditorStyles.miniLabel);
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

                GameObject logicalHost = profile.LogicalActorHostPrefab;
                EditorGUILayout.TextField(
                    "Logical Host Asset Path",
                    logicalHost != null
                        ? AssetDatabase.GetAssetPath(logicalHost)
                        : string.Empty);
                EditorGUILayout.Toggle(
                    "Defined Actor Kind",
                    profile.HasDefinedActorKind);
                EditorGUILayout.Toggle(
                    "Defined Actor Role",
                    profile.HasDefinedActorRole);
                EditorGUILayout.Toggle(
                    "Logical Host Assigned",
                    profile.HasLogicalActorHostPrefab);
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
}
