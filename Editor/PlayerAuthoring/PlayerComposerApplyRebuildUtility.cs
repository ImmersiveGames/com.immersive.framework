using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerAuthoring
{
    /// <summary>
    /// Canonical editor-only Apply/Rebuild entry point for PlayerComposer.
    /// P3A materializes only concrete runtime owners/adapters and removes obsolete passive binding components.
    /// </summary>
    public static class PlayerComposerApplyRebuildUtility
    {
        public static PlayerComposerApplyRebuildResult Validate(PlayerComposer composer, bool logDiagnostics = true)
        {
            if (composer == null)
            {
                return FailValidation("PlayerComposer validation requires a target composer.", logDiagnostics, null);
            }

            if (!composer.TryValidateForApply(out string issue))
            {
                composer.EditorSetApplyRebuildResult("ValidationFailed", issue, string.Empty);
                EditorUtility.SetDirty(composer);
                return FailValidation(issue, logDiagnostics, composer);
            }

            const string summary = "Validation completed. Minimal Player materialization is valid; no scene content was changed.";
            composer.EditorSetApplyRebuildResult("ValidationSucceeded", string.Empty, summary);
            EditorUtility.SetDirty(composer);

            if (logDiagnostics)
            {
                Debug.Log(
                    $"[Immersive.Framework][PlayerComposer] Validation succeeded. player='{composer.name}' " +
                    $"actorId='{composer.ActorId}' " +
                    $"authoredDefaultActionMap='{composer.GameplayActionMap}'.",
                    composer);
            }

            return PlayerComposerApplyRebuildResult.ValidationSucceeded(summary);
        }

        public static PlayerComposerApplyRebuildResult ApplyOrRebuild(
            PlayerComposer composer,
            bool logDiagnostics = true,
            bool useUndo = true)
        {
            if (composer == null)
            {
                const string nullComposerIssue = "PlayerComposer Apply/Rebuild requires a target composer.";
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][PlayerComposer] Apply/Rebuild failed. issue='{nullComposerIssue}'");
                }

                return PlayerComposerApplyRebuildResult.ApplyFailed(nullComposerIssue);
            }

            int undoGroup = BeginUndo(useUndo);

            if (!composer.TryValidateForApply(out string validationIssue))
            {
                composer.EditorSetApplyRebuildResult("ApplyFailed", validationIssue, string.Empty);
                EditorUtility.SetDirty(composer);
                EndUndo(useUndo, undoGroup);

                if (logDiagnostics)
                {
                    Debug.LogWarning(
                        $"[Immersive.Framework][PlayerComposer] Apply/Rebuild failed. player='{composer.name}' issue='{validationIssue}'",
                        composer);
                }

                return PlayerComposerApplyRebuildResult.ApplyFailed(validationIssue);
            }

            var report = new PlayerComposerMaterializationReport();

            if (composer.ControlEnabled && composer.PlayerInput != null)
            {
                if (!string.Equals(
                        composer.PlayerInput.defaultActionMap,
                        composer.GameplayActionMap,
                        StringComparison.Ordinal))
                {
                    if (useUndo)
                    {
                        Undo.RecordObject(composer.PlayerInput, "Apply Player Default Action Map");
                    }

                    composer.PlayerInput.defaultActionMap = composer.GameplayActionMap;
                    EditorUtility.SetDirty(composer.PlayerInput);
                    report.Repaired($"PlayerInput.defaultActionMap:{composer.GameplayActionMap}");
                }
                else
                {
                    report.AlreadyValid($"PlayerInput.defaultActionMap:{composer.GameplayActionMap}");
                }
            }

            Transform technicalRoot = EnsureTechnicalRoot(composer, report, useUndo);
            Transform cameraTarget = composer.CameraTarget;
            Transform lookAtTarget = composer.LookAtTarget;

            if (composer.CameraBindingRequired)
            {
                cameraTarget = EnsureAnchor(
                    composer,
                    "CameraTarget",
                    composer.CameraTarget,
                    true,
                    report,
                    useUndo);

                lookAtTarget = composer.LookAtPolicy == PlayerComposerLookAtPolicy.UseFollowTarget
                    ? cameraTarget
                    : EnsureAnchor(
                        composer,
                        "LookAtTarget",
                        composer.LookAtTarget,
                        true,
                        report,
                        useUndo);

                composer.EditorSetGeneratedReferences(technicalRoot, cameraTarget, lookAtTarget);

                if (composer.CameraTarget == null)
                {
                    report.Blocked("camera-anchor:CameraTarget:materialization-failed");
                }

                if (composer.LookAtTarget == null)
                {
                    report.Blocked("camera-anchor:LookAtTarget:materialization-failed");
                }
            }
            else
            {
                // Camera integration is disabled. Existing authored/generated anchors are preserved,
                // but Apply/Rebuild must not create new camera objects or rewrite references.
                composer.EditorSetGeneratedReferences(technicalRoot, null, null);
                report.SkippedByPolicy("camera-anchors:camera-binding-disabled");
            }

            PlayerActorDeclaration actorDeclaration = EnsureComponent<PlayerActorDeclaration>(
                composer.gameObject,
                report,
                useUndo,
                component =>
                {
                    bool changed = false;
                    changed |= SetSerialized(component, "actorId", composer.ActorId);
                    changed |= SetSerialized(component, "displayName", composer.name);
                    changed |= SetSerialized(component, "playerInput", composer.PlayerInput);
                    changed |= SetSerialized(component, "reason", "player-composer.apply");
                    return changed;
                });

            ConfigureGate(composer, report, useUndo);
            ConfigureReset(composer, actorDeclaration, technicalRoot, report, useUndo);
            RemoveEmptyBindingsContainer(composer, report, useUndo);

            string summary = report.CreateSummary();
            bool succeeded = report.BlockedCount == 0;
            string status = succeeded ? "ApplySucceeded" : "ApplyCompletedWithBlockingIssues";
            string issue = succeeded ? string.Empty : report.FirstBlockingIssue;

            composer.EditorSetApplyRebuildResult(status, issue, summary);
            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(composer.gameObject);
            EndUndo(useUndo, undoGroup);

            if (logDiagnostics && composer.LogApplyRebuildDiagnostics)
            {
                Debug.Log(
                    $"[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='{composer.name}' " +
                    $"actorId='{composer.ActorId}' " +
                    $"authoredDefaultActionMap='{composer.GameplayActionMap}' created='{report.CreatedCount}' " +
                    $"repaired='{report.RepairedCount}' removedLegacy='{report.RemovedCount}' " +
                    $"alreadyValid='{report.AlreadyValidCount}' skippedByPolicy='{report.SkippedByPolicyCount}' " +
                    $"blocked='{report.BlockedCount}'.",
                    composer);
            }

            return PlayerComposerApplyRebuildResult.ApplyCompleted(
                succeeded,
                status,
                issue,
                summary,
                report.CreatedCount,
                report.RepairedCount,
                report.AlreadyValidCount,
                report.SkippedByPolicyCount,
                report.BlockedCount);
        }

        private static void ConfigureGate(
            PlayerComposer composer,
            PlayerComposerMaterializationReport report,
            bool useUndo)
        {
            Type gateType = ResolveMonoBehaviourType("UnityPlayerInputGateAdapter");
            Component existing = gateType != null ? composer.GetComponent(gateType) : null;

            if (!composer.ControlEnabled || !composer.GateParticipation)
            {
                if (existing != null)
                {
                    Destroy(existing, useUndo);
                    report.Removed("root:UnityPlayerInputGateAdapter:policy-disabled");
                }
                else
                {
                    report.SkippedByPolicy("gate:disabled");
                }

                return;
            }

            if (!composer.HasCompleteControlConfiguration)
            {
                if (composer.InputBindingRequired)
                {
                    report.Blocked("gate:required-control-configuration-incomplete");
                }
                else
                {
                    report.SkippedByPolicy("gate:optional-control-configuration-incomplete");
                }

                return;
            }

            if (gateType == null)
            {
                report.Blocked("missing-type:UnityPlayerInputGateAdapter");
                return;
            }

            Component gate = existing ?? AddComponent(composer.gameObject, gateType, useUndo);
            bool changed = false;
            changed |= SetSerialized(gate, "playerInput", composer.PlayerInput);

            // Unity's PlayerInput remains the authority. The adapter receives its effective default map.
            changed |= SetSerialized(gate, "gameplayActionMapName", composer.GameplayActionMap);
            changed |= SetSerialized(gate, "actionMapName", composer.GameplayActionMap);
            changed |= SetSerialized(gate, "gameplayActionMap", composer.GameplayActionMap);

            if (existing == null)
            {
                report.Created("root:UnityPlayerInputGateAdapter");
            }
            else if (changed)
            {
                report.Repaired("root:UnityPlayerInputGateAdapter");
            }
            else
            {
                report.AlreadyValid("root:UnityPlayerInputGateAdapter");
            }
        }

        private static void ConfigureReset(
            PlayerComposer composer,
            PlayerActorDeclaration actorDeclaration,
            Transform technicalRoot,
            PlayerComposerMaterializationReport report,
            bool useUndo)
        {
            Type subjectType = ResolveMonoBehaviourType("UnityResetSubjectAdapter");
            Component subject = subjectType != null ? composer.GetComponent(subjectType) : null;

            if (!composer.ResetEnabled)
            {
                if (subject != null)
                {
                    Destroy(subject, useUndo);
                    report.Removed("root:UnityResetSubjectAdapter:reset-disabled");
                }
                else
                {
                    report.SkippedByPolicy("reset-subject:reset-disabled");
                }

                RemoveTransformResetParticipants(technicalRoot, report, useUndo);
                return;
            }

            if (subjectType == null)
            {
                report.Blocked("missing-type:UnityResetSubjectAdapter");
                return;
            }

            Component effectiveSubject = subject ?? AddComponent(composer.gameObject, subjectType, useUndo);
            bool subjectChanged = false;
            subjectChanged |= SetSerialized(effectiveSubject, "sourcePlayerActor", actorDeclaration);
            subjectChanged |= SetSerialized(effectiveSubject, "displayName", composer.name);
            subjectChanged |= SetSerialized(effectiveSubject, "diagnosticTag", "player-composer");
            subjectChanged |= SetSerialized(effectiveSubject, "scope", composer.ResetScope);

            if (subject == null)
            {
                report.Created("root:UnityResetSubjectAdapter");
            }
            else if (subjectChanged)
            {
                report.Repaired("root:UnityResetSubjectAdapter");
            }
            else
            {
                report.AlreadyValid("root:UnityResetSubjectAdapter");
            }

            if (composer.ResetParticipantPolicy != PlayerComposerResetParticipantPolicy.Transform)
            {
                RemoveTransformResetParticipants(technicalRoot, report, useUndo);
                report.SkippedByPolicy($"reset-participant:{composer.ResetParticipantPolicy}");
                return;
            }

            if (technicalRoot == null)
            {
                report.Blocked("reset-participant:no-technical-root");
                return;
            }

            Type participantType = ResolveMonoBehaviourType("UnityTransformResetParticipant");
            if (participantType == null)
            {
                report.Blocked("missing-type:UnityTransformResetParticipant");
                return;
            }

            Component participant = technicalRoot.GetComponent(participantType);
            Component effectiveParticipant = participant ?? AddComponent(technicalRoot.gameObject, participantType, useUndo);
            bool changed = SetSerialized(effectiveParticipant, "target", composer.transform);

            if (participant == null)
            {
                report.Created("technical:UnityTransformResetParticipant");
            }
            else if (changed)
            {
                report.Repaired("technical:UnityTransformResetParticipant");
            }
            else
            {
                report.AlreadyValid("technical:UnityTransformResetParticipant");
            }
        }

        private static Transform EnsureTechnicalRoot(
            PlayerComposer composer,
            PlayerComposerMaterializationReport report,
            bool useUndo)
        {
            Transform assigned = composer.FrameworkBindingsRoot;
            if (assigned != null && assigned.parent == composer.transform && assigned.name == "_Framework")
            {
                report.AlreadyValid("technical-root:_Framework");
                return assigned;
            }

            Transform root = FindDirectChild(composer.transform, "_Framework");
            if (root != null)
            {
                report.AlreadyValid("technical-root:_Framework");
                return root;
            }

            if (!composer.CreateBindingsRootIfMissing)
            {
                report.Blocked("technical-root:missing-and-creation-disabled");
                return null;
            }

            root = CreateChild(composer.transform, "_Framework", useUndo);
            report.Created("technical-root:_Framework");
            return root;
        }

        private static Transform EnsureAnchor(
            PlayerComposer composer,
            string anchorName,
            Transform assigned,
            bool required,
            PlayerComposerMaterializationReport report,
            bool useUndo)
        {
            if (assigned != null)
            {
                report.AlreadyValid($"anchor:assigned:{anchorName}");
                return assigned;
            }

            if (!composer.CreateAnchorsIfMissing)
            {
                if (required)
                {
                    report.Blocked($"anchor:{anchorName}:missing-and-creation-disabled");
                }
                else
                {
                    report.SkippedByPolicy($"anchor:{anchorName}:optional-and-creation-disabled");
                }

                return null;
            }

            Transform anchorsRoot = FindDirectChild(composer.transform, "Anchors");
            if (anchorsRoot == null)
            {
                anchorsRoot = CreateChild(composer.transform, "Anchors", useUndo);
                report.Created("container:Anchors");
            }
            else
            {
                report.AlreadyValid("container:Anchors");
            }

            Transform anchor = FindDirectChild(anchorsRoot, anchorName);
            if (anchor == null)
            {
                anchor = CreateChild(anchorsRoot, anchorName, useUndo);
                report.Created($"anchor:Anchors/{anchorName}");
            }
            else
            {
                report.AlreadyValid($"anchor:Anchors/{anchorName}");
            }

            return anchor;
        }

        private static void RemoveEmptyBindingsContainer(
            PlayerComposer composer,
            PlayerComposerMaterializationReport report,
            bool useUndo)
        {
            Transform framework = FindDirectChild(composer.transform, "_Framework");
            Transform bindings = framework != null ? FindDirectChild(framework, "_Bindings") : null;
            if (bindings == null)
            {
                return;
            }

            if (bindings.childCount == 0 && bindings.GetComponents<Component>().Length == 1)
            {
                Destroy(bindings.gameObject, useUndo);
                report.Removed("container:_Framework/_Bindings");
            }
        }

        private static void RemoveTransformResetParticipants(
            Transform technicalRoot,
            PlayerComposerMaterializationReport report,
            bool useUndo)
        {
            if (technicalRoot == null)
            {
                return;
            }

            Type type = ResolveMonoBehaviourType("UnityTransformResetParticipant");
            if (type == null)
            {
                return;
            }

            foreach (Component participant in technicalRoot.GetComponents(type))
            {
                Destroy(participant, useUndo);
                report.Removed("technical:UnityTransformResetParticipant");
            }
        }

        private static T EnsureComponent<T>(
            GameObject owner,
            PlayerComposerMaterializationReport report,
            bool useUndo,
            Func<T, bool> configure)
            where T : Component
        {
            T existing = owner.GetComponent<T>();
            if (existing == null)
            {
                T created = useUndo ? Undo.AddComponent<T>(owner) : owner.AddComponent<T>();
                configure?.Invoke(created);
                report.Created($"root:{typeof(T).Name}");
                return created;
            }

            bool changed = configure?.Invoke(existing) == true;
            if (changed)
            {
                report.Repaired($"root:{typeof(T).Name}");
            }
            else
            {
                report.AlreadyValid($"root:{typeof(T).Name}");
            }

            return existing;
        }

        private static Type ResolveMonoBehaviourType(string simpleName)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (type.Name == simpleName)
                {
                    return type;
                }
            }

            return null;
        }

        private static Component AddComponent(GameObject owner, Type type, bool useUndo)
        {
            return useUndo ? Undo.AddComponent(owner, type) : owner.AddComponent(type);
        }

        private static void Destroy(UnityEngine.Object value, bool useUndo)
        {
            if (value == null)
            {
                return;
            }

            if (useUndo)
            {
                Undo.DestroyObjectImmediate(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static Transform FindDirectChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform CreateChild(Transform parent, string name, bool useUndo)
        {
            var child = new GameObject(name);
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            }

            child.transform.SetParent(parent, false);
            return child.transform;
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

        private static PlayerComposerApplyRebuildResult FailValidation(
            string issue,
            bool logDiagnostics,
            PlayerComposer composer)
        {
            if (logDiagnostics)
            {
                Debug.LogWarning(
                    $"[Immersive.Framework][PlayerComposer] Validation failed. issue='{issue}'",
                    composer);
            }

            return PlayerComposerApplyRebuildResult.ValidationFailed(issue);
        }

        private static int BeginUndo(bool useUndo)
        {
            if (!useUndo)
            {
                return -1;
            }

            Undo.SetCurrentGroupName("Apply Player Composer");
            return Undo.GetCurrentGroup();
        }

        private static void EndUndo(bool useUndo, int group)
        {
            if (useUndo && group >= 0)
            {
                Undo.CollapseUndoOperations(group);
            }
        }

        private sealed class PlayerComposerMaterializationReport
        {
            private readonly List<string> entries = new();

            public int CreatedCount { get; private set; }
            public int RepairedCount { get; private set; }
            public int RemovedCount { get; private set; }
            public int AlreadyValidCount { get; private set; }
            public int SkippedByPolicyCount { get; private set; }
            public int BlockedCount { get; private set; }
            public string FirstBlockingIssue { get; private set; } = string.Empty;

            public void Created(string entry) { CreatedCount++; Add("created", entry); }
            public void Repaired(string entry) { RepairedCount++; Add("repaired", entry); }
            public void Removed(string entry) { RemovedCount++; Add("removed", entry); }
            public void AlreadyValid(string entry) { AlreadyValidCount++; Add("already-valid", entry); }
            public void SkippedByPolicy(string entry) { SkippedByPolicyCount++; Add("skipped-by-policy", entry); }

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
                string header =
                    $"created={CreatedCount}; repaired={RepairedCount}; removed={RemovedCount}; " +
                    $"alreadyValid={AlreadyValidCount}; skippedByPolicy={SkippedByPolicyCount}; blocked={BlockedCount}";

                return entries.Count == 0 ? header : header + "\n" + string.Join("\n", entries);
            }

            private void Add(string kind, string entry)
            {
                entries.Add($"{kind}:{entry ?? string.Empty}");
            }
        }
    }

    public readonly struct PlayerComposerApplyRebuildResult
    {
        private PlayerComposerApplyRebuildResult(
            bool succeeded,
            string status,
            string issue,
            string summary,
            int createdCount,
            int repairedCount,
            int alreadyValidCount,
            int skippedByPolicyCount,
            int blockedCount)
        {
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Issue = issue ?? string.Empty;
            Summary = summary ?? string.Empty;
            CreatedCount = createdCount;
            RepairedCount = repairedCount;
            AlreadyValidCount = alreadyValidCount;
            SkippedByPolicyCount = skippedByPolicyCount;
            BlockedCount = blockedCount;
        }

        public bool Succeeded { get; }
        public bool Failed => !Succeeded;
        public string Status { get; }
        public string Issue { get; }
        public string Summary { get; }
        public int CreatedCount { get; }
        public int RepairedCount { get; }
        public int AlreadyValidCount { get; }
        public int SkippedByPolicyCount { get; }
        public int BlockedCount { get; }

        public static PlayerComposerApplyRebuildResult ValidationSucceeded(string summary) =>
            new(true, "ValidationSucceeded", string.Empty, summary, 0, 0, 0, 0, 0);

        public static PlayerComposerApplyRebuildResult ValidationFailed(string issue) =>
            new(false, "ValidationFailed", issue, string.Empty, 0, 0, 0, 0, 1);

        public static PlayerComposerApplyRebuildResult ApplyFailed(string issue) =>
            new(false, "ApplyFailed", issue, string.Empty, 0, 0, 0, 0, 1);

        public static PlayerComposerApplyRebuildResult ApplyCompleted(
            bool succeeded,
            string status,
            string issue,
            string summary,
            int createdCount,
            int repairedCount,
            int alreadyValidCount,
            int skippedByPolicyCount,
            int blockedCount) =>
            new(
                succeeded,
                status,
                issue,
                summary,
                createdCount,
                repairedCount,
                alreadyValidCount,
                skippedByPolicyCount,
                blockedCount);
    }
}
