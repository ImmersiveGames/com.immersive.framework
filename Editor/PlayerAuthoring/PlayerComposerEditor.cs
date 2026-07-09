using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerAuthoring
{
    [CustomEditor(typeof(PlayerComposer))]
    public sealed class PlayerComposerEditor : UnityEditor.Editor
    {
        private SerializedProperty _recipe;
        private SerializedProperty _actorId;
        private SerializedProperty _playerSlotId;
        private SerializedProperty _playerInput;
        private SerializedProperty _gameplayActionMap;
        private SerializedProperty _cameraTarget;
        private SerializedProperty _lookAtTarget;
        private SerializedProperty _resetEnabled;
        private SerializedProperty _validationMode;
        private SerializedProperty _frameworkBindingsRoot;
        private SerializedProperty _createBindingsRootIfMissing;
        private SerializedProperty _createAnchorsIfMissing;
        private SerializedProperty _inputBindingRequired;
        private SerializedProperty _cameraBindingRequired;
        private SerializedProperty _resetScope;
        private SerializedProperty _resetParticipantPolicy;
        private SerializedProperty _materializeSlotOccupancy;
        private SerializedProperty _materializePassiveEntryViewControl;
        private SerializedProperty _logApplyRebuildDiagnostics;

        private bool _showAdvanced;
        private bool _showDebug;

        private void OnEnable()
        {
            _recipe = serializedObject.FindProperty("recipe");
            _actorId = serializedObject.FindProperty("actorId");
            _playerSlotId = serializedObject.FindProperty("playerSlotId");
            _playerInput = serializedObject.FindProperty("playerInput");
            _gameplayActionMap = serializedObject.FindProperty("gameplayActionMap");
            _cameraTarget = serializedObject.FindProperty("cameraTarget");
            _lookAtTarget = serializedObject.FindProperty("lookAtTarget");
            _resetEnabled = serializedObject.FindProperty("resetEnabled");
            _validationMode = serializedObject.FindProperty("validationMode");
            _frameworkBindingsRoot = serializedObject.FindProperty("frameworkBindingsRoot");
            _createBindingsRootIfMissing = serializedObject.FindProperty("createBindingsRootIfMissing");
            _createAnchorsIfMissing = serializedObject.FindProperty("createAnchorsIfMissing");
            _inputBindingRequired = serializedObject.FindProperty("inputBindingRequired");
            _cameraBindingRequired = serializedObject.FindProperty("cameraBindingRequired");
            _resetScope = serializedObject.FindProperty("resetScope");
            _resetParticipantPolicy = serializedObject.FindProperty("resetParticipantPolicy");
            _materializeSlotOccupancy = serializedObject.FindProperty("materializeSlotOccupancy");
            _materializePassiveEntryViewControl = serializedObject.FindProperty("materializePassiveEntryViewControl");
            _logApplyRebuildDiagnostics = serializedObject.FindProperty("logApplyRebuildDiagnostics");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Player Composer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Designer-first player authoring surface. Apply/Rebuild materializes technical framework bindings; this component does not execute gameplay and is not a PlayerManager.", MessageType.Info);

            DrawDesignerSection();
            DrawActions();
            DrawAdvancedSection();
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDesignerSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Designer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_recipe);
            using (new EditorGUI.DisabledScope(_recipe.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    ApplyRecipeDefaults();
                }
            }

            EditorGUILayout.PropertyField(_actorId);
            EditorGUILayout.PropertyField(_playerSlotId);
            EditorGUILayout.PropertyField(_playerInput);
            EditorGUILayout.PropertyField(_gameplayActionMap);
            EditorGUILayout.PropertyField(_cameraTarget);
            EditorGUILayout.PropertyField(_lookAtTarget);
            EditorGUILayout.PropertyField(_resetEnabled);
            EditorGUILayout.PropertyField(_validationMode);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply / Rebuild"))
                {
                    ApplyOrRebuild();
                }

                if (GUILayout.Button("Validate"))
                {
                    ValidateOnly();
                }
            }

            if (GUILayout.Button("Select Technical Bindings"))
            {
                SelectTechnicalBindings();
            }
        }

        private void DrawAdvancedSection()
        {
            EditorGUILayout.Space();
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced)
            {
                return;
            }

            EditorGUILayout.PropertyField(_frameworkBindingsRoot);
            EditorGUILayout.PropertyField(_createBindingsRootIfMissing);
            EditorGUILayout.PropertyField(_createAnchorsIfMissing);
            EditorGUILayout.PropertyField(_inputBindingRequired);
            EditorGUILayout.PropertyField(_cameraBindingRequired);
            EditorGUILayout.PropertyField(_resetScope);
            EditorGUILayout.PropertyField(_resetParticipantPolicy);
            EditorGUILayout.PropertyField(_materializeSlotOccupancy);
            EditorGUILayout.PropertyField(_materializePassiveEntryViewControl);
            EditorGUILayout.PropertyField(_logApplyRebuildDiagnostics);
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.Space();
            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);
            if (!_showDebug)
            {
                return;
            }

            PlayerComposer composer = (PlayerComposer)target;
            PlayerComposerDebugSnapshot snapshot = composer.CreateDebugSnapshot();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Resolved Actor Id", snapshot.ActorId);
                EditorGUILayout.TextField("Resolved Player Slot Id", snapshot.PlayerSlotId);
                EditorGUILayout.TextField("Resolved PlayerInput", snapshot.PlayerInputName);
                EditorGUILayout.TextField("Gameplay Action Map", snapshot.GameplayActionMap);
                EditorGUILayout.Toggle("Action Map Found", snapshot.ActionMapFound);
                EditorGUILayout.TextField("Bindings Root", snapshot.FrameworkBindingsRootName);
                EditorGUILayout.TextField("Camera Target", snapshot.CameraTargetName);
                EditorGUILayout.TextField("Look At Target", snapshot.LookAtTargetName);
                EditorGUILayout.Toggle("Reset Enabled", snapshot.ResetEnabled);
                EditorGUILayout.TextField("Last Status", snapshot.LastApplyRebuildStatus);
                EditorGUILayout.TextField("Last Blocking Issue", snapshot.LastBlockingIssue);
                EditorGUILayout.TextArea(snapshot.LastMaterializationSummary, GUILayout.MinHeight(72));
            }
        }

        private void ValidateOnly()
        {
            serializedObject.ApplyModifiedProperties();
            PlayerComposerApplyRebuildUtility.Validate((PlayerComposer)target, true);
            serializedObject.Update();
        }

        private void ApplyRecipeDefaults()
        {
            serializedObject.ApplyModifiedProperties();
            var composer = (PlayerComposer)target;
            Undo.RecordObject(composer, "Apply Player Recipe Defaults");
            if (!composer.EditorApplyRecipeDefaults(true, out string issue))
            {
                Debug.LogWarning($"[Immersive.Framework][PlayerComposer] Apply Recipe Defaults failed. player='{composer.name}' issue='{issue}'", composer);
                serializedObject.Update();
                return;
            }

            EditorUtility.SetDirty(composer);
            Debug.Log($"[Immersive.Framework][PlayerComposer] Recipe defaults applied. player='{composer.name}' recipe='{composer.Recipe.name}' actorId='{composer.ActorId}' playerSlotId='{composer.PlayerSlotId}' actionMap='{composer.GameplayActionMap}' resetEnabled='{composer.ResetEnabled}' resetParticipantPolicy='{composer.ResetParticipantPolicy}'.", composer);
            serializedObject.Update();
        }

        private void ApplyOrRebuild()
        {
            serializedObject.ApplyModifiedProperties();
            PlayerComposerApplyRebuildUtility.ApplyOrRebuild((PlayerComposer)target, true, true);
            serializedObject.Update();
        }

        private Transform EnsureBindingsRoot(PlayerComposer composer, PlayerComposerMaterializationReport report)
        {
            if (composer.FrameworkBindingsRoot != null)
            {
                report.AlreadyValid($"bindings-root:assigned:{composer.FrameworkBindingsRoot.name}");
                return composer.FrameworkBindingsRoot;
            }

            if (!composer.CreateBindingsRootIfMissing)
            {
                report.Blocked("bindings-root:missing-and-creation-disabled");
                return null;
            }

            Transform frameworkRoot = FindDirectChild(composer.transform, "_Framework");
            if (frameworkRoot == null)
            {
                frameworkRoot = CreateChild(composer.transform, "_Framework");
                report.Created("container:_Framework");
            }
            else
            {
                report.AlreadyValid("container:_Framework");
            }

            Transform bindingsRoot = FindDirectChild(frameworkRoot, "_Bindings");
            if (bindingsRoot == null)
            {
                bindingsRoot = CreateChild(frameworkRoot, "_Bindings");
                report.Created("bindings-root:_Framework/_Bindings");
            }
            else
            {
                report.AlreadyValid("bindings-root:_Framework/_Bindings");
            }

            return bindingsRoot;
        }

        private Transform EnsureAnchor(PlayerComposer composer, string anchorName, PlayerComposerMaterializationReport report)
        {
            if (!composer.CreateAnchorsIfMissing)
            {
                report.SkippedByPolicy($"anchor:{anchorName}:anchor-creation-disabled");
                return composer.transform;
            }

            Transform anchorsRoot = FindDirectChild(composer.transform, "Anchors");
            if (anchorsRoot == null)
            {
                anchorsRoot = CreateChild(composer.transform, "Anchors");
                report.Created("container:Anchors");
            }
            else
            {
                report.AlreadyValid("container:Anchors");
            }

            Transform anchor = FindDirectChild(anchorsRoot, anchorName);
            if (anchor == null)
            {
                anchor = CreateChild(anchorsRoot, anchorName);
                report.Created($"anchor:Anchors/{anchorName}");
            }
            else
            {
                report.AlreadyValid($"anchor:Anchors/{anchorName}");
            }

            return anchor;
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            var child = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Component EnsureComponent(GameObject owner, string fullName, string simpleName, bool required, PlayerComposerMaterializationReport report, Func<Component, bool> configure)
        {
            Type type = ResolveComponentType(fullName, simpleName);
            if (type == null)
            {
                if (required)
                {
                    report.Blocked($"missing-type:{simpleName}");
                }
                else
                {
                    report.SkippedByPolicy($"missing-optional-type:{simpleName}");
                }

                return null;
            }

            Component existing = owner.GetComponent(type);
            if (existing != null)
            {
                bool changed = configure?.Invoke(existing) == true;
                if (changed)
                {
                    report.Repaired(simpleName);
                }
                else
                {
                    report.AlreadyValid(simpleName);
                }

                return existing;
            }

            Component created = Undo.AddComponent(owner, type);
            configure?.Invoke(created);
            report.Created(simpleName);
            return created;
        }

        private static void EnsureOptionalRootComponent(PlayerComposer composer, string simpleName, PlayerComposerMaterializationReport report, Func<Component, bool> configure)
        {
            Type type = ResolveComponentType(null, simpleName);
            if (type == null)
            {
                report.SkippedByPolicy($"missing-optional-root-type:{simpleName}");
                return;
            }

            Component existing = composer.gameObject.GetComponent(type);
            if (existing != null)
            {
                bool changed = configure?.Invoke(existing) == true;
                if (changed)
                {
                    report.Repaired($"root:{simpleName}");
                }
                else
                {
                    report.AlreadyValid($"root:{simpleName}");
                }

                return;
            }

            Component created = Undo.AddComponent(composer.gameObject, type);
            configure?.Invoke(created);
            report.Created($"root:{simpleName}");
        }

        private static void EnsureBindingComponent(Transform bindingsRoot, string fullName, string simpleName, bool required, PlayerComposerMaterializationReport report, Func<Component, bool> configure)
        {
            if (bindingsRoot == null)
            {
                if (required)
                {
                    report.Blocked($"no-bindings-root:{simpleName}");
                }
                else
                {
                    report.SkippedByPolicy($"no-bindings-root:{simpleName}");
                }

                return;
            }

            Type type = ResolveComponentType(fullName, simpleName);
            if (type == null)
            {
                if (required)
                {
                    report.Blocked($"missing-type:{simpleName}");
                }
                else
                {
                    report.SkippedByPolicy($"missing-optional-type:{simpleName}");
                }

                return;
            }

            Component existing = bindingsRoot.GetComponent(type);
            if (existing != null)
            {
                bool changed = configure?.Invoke(existing) == true;
                if (changed)
                {
                    report.Repaired($"binding:{simpleName}");
                }
                else
                {
                    report.AlreadyValid($"binding:{simpleName}");
                }

                return;
            }

            Component created = Undo.AddComponent(bindingsRoot.gameObject, type);
            configure?.Invoke(created);
            report.Created($"binding:{simpleName}");
        }

        private static Type ResolveComponentType(string fullName, string simpleName)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (!string.IsNullOrEmpty(fullName) && type.FullName == fullName)
                {
                    return type;
                }

                if (!string.IsNullOrEmpty(simpleName) && type.Name == simpleName)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool SetSerialized(Component component, string propertyName, string value)
        {
            if (component == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                return false;
            }

            bool changed = false;
            if (property.propertyType == SerializedPropertyType.String)
            {
                string normalized = value ?? string.Empty;
                if (!string.Equals(property.stringValue, normalized, StringComparison.Ordinal))
                {
                    property.stringValue = normalized;
                    changed = true;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Enum)
            {
                changed = SetEnumByName(property, value);
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }

            return changed;
        }

        private static bool SetSerialized(Component component, string propertyName, UnityEngine.Object value)
        {
            if (component == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return false;
            }

            if (property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            return true;
        }

        private static bool SetEnumByName(SerializedProperty property, string value)
        {
            if (property == null || property.propertyType != SerializedPropertyType.Enum || string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < property.enumNames.Length; i++)
            {
                if (!string.Equals(property.enumNames[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.enumValueIndex == i)
                {
                    return false;
                }

                property.enumValueIndex = i;
                return true;
            }

            return false;
        }

        private void SelectTechnicalBindings()
        {
            serializedObject.ApplyModifiedProperties();
            PlayerComposer composer = (PlayerComposer)target;
            if (composer.FrameworkBindingsRoot != null)
            {
                Selection.activeTransform = composer.FrameworkBindingsRoot;
                EditorGUIUtility.PingObject(composer.FrameworkBindingsRoot.gameObject);
            }
            else
            {
                Debug.LogWarning($"[Immersive.Framework][PlayerComposer] No technical bindings root is assigned. player='{composer.name}'", composer);
            }
        }

        private sealed class PlayerComposerMaterializationReport
        {
            private readonly List<string> _entries = new();

            public int CreatedCount { get; private set; }
            public int RepairedCount { get; private set; }
            public int AlreadyValidCount { get; private set; }
            public int SkippedByPolicyCount { get; private set; }
            public int BlockedCount { get; private set; }
            public string FirstBlockingIssue { get; private set; } = string.Empty;

            public void Created(string entry)
            {
                CreatedCount++;
                Add("created", entry);
            }

            public void Repaired(string entry)
            {
                RepairedCount++;
                Add("repaired", entry);
            }

            public void AlreadyValid(string entry)
            {
                AlreadyValidCount++;
                Add("already-valid", entry);
            }

            public void SkippedByPolicy(string entry)
            {
                SkippedByPolicyCount++;
                Add("skipped-by-policy", entry);
            }

            public void Blocked(string entry)
            {
                BlockedCount++;
                if (string.IsNullOrEmpty(FirstBlockingIssue))
                {
                    FirstBlockingIssue = entry ?? string.Empty;
                }

                Add("blocked", entry);
            }

            public string CreateSummary()
            {
                string header = $"created={CreatedCount}; repaired={RepairedCount}; alreadyValid={AlreadyValidCount}; skippedByPolicy={SkippedByPolicyCount}; blocked={BlockedCount}";
                if (_entries.Count == 0)
                {
                    return header;
                }

                return header + "\n" + string.Join("\n", _entries);
            }

            private void Add(string category, string entry)
            {
                _entries.Add($"{category}:{entry ?? string.Empty}");
            }
        }
    }
}
