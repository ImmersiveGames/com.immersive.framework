using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerAuthoring
{
    /// <summary>
    /// Editor-only Apply/Rebuild entry point for PlayerComposer.
    /// This is the public technical surface used by the Inspector and by QA harnesses.
    /// It does not execute gameplay and does not create runtime authority.
    /// </summary>
    public static class PlayerComposerApplyRebuildUtility
    {
        public static PlayerComposerApplyRebuildResult Validate(PlayerComposer composer, bool logDiagnostics = true)
        {
            if (composer == null)
            {
                const string nullIssue = "PlayerComposer validation requires a target composer.";
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][PlayerComposer] Validation failed. issue='{nullIssue}'");
                }

                return PlayerComposerApplyRebuildResult.ValidationFailed(nullIssue);
            }

            if (!composer.TryValidateForApply(out string issue))
            {
                composer.EditorSetApplyRebuildResult("ValidationFailed", issue, string.Empty);
                EditorUtility.SetDirty(composer);
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][PlayerComposer] Validation failed. player='{composer.name}' issue='{issue}'", composer);
                }

                return PlayerComposerApplyRebuildResult.ValidationFailed(issue);
            }

            const string summary = "Validation completed. No materialization was changed.";
            composer.EditorSetApplyRebuildResult("ValidationSucceeded", string.Empty, summary);
            EditorUtility.SetDirty(composer);
            if (logDiagnostics)
            {
                Debug.Log($"[Immersive.Framework][PlayerComposer] Validation succeeded. player='{composer.name}' actorId='{composer.ActorId}' playerSlotId='{composer.PlayerSlotId}'", composer);
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
                const string nullIssue = "PlayerComposer Apply/Rebuild requires a target composer.";
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][PlayerComposer] Apply/Rebuild failed. issue='{nullIssue}'");
                }

                return PlayerComposerApplyRebuildResult.ApplyFailed(nullIssue);
            }

            int undoGroup = -1;
            if (useUndo)
            {
                Undo.SetCurrentGroupName("Apply Player Composer");
                undoGroup = Undo.GetCurrentGroup();
            }

            if (!composer.TryValidateForApply(out string issue))
            {
                composer.EditorSetApplyRebuildResult("ApplyFailed", issue, string.Empty);
                EditorUtility.SetDirty(composer);
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][PlayerComposer] Apply/Rebuild failed. player='{composer.name}' issue='{issue}'", composer);
                }

                CollapseUndoIfNeeded(useUndo, undoGroup);
                return PlayerComposerApplyRebuildResult.ApplyFailed(issue);
            }

            var serializedComposer = new SerializedObject(composer);
            serializedComposer.Update();
            SerializedProperty playerInputProperty = serializedComposer.FindProperty("playerInput");
            SerializedProperty cameraTargetProperty = serializedComposer.FindProperty("cameraTarget");
            SerializedProperty lookAtTargetProperty = serializedComposer.FindProperty("lookAtTarget");

            UnityEngine.Object playerInputReference = playerInputProperty != null ? playerInputProperty.objectReferenceValue : null;
            UnityEngine.Object cameraTargetReference = cameraTargetProperty != null ? cameraTargetProperty.objectReferenceValue : null;
            UnityEngine.Object lookAtTargetReference = lookAtTargetProperty != null ? lookAtTargetProperty.objectReferenceValue : null;

            var report = new PlayerComposerMaterializationReport();
            Transform bindingsRoot = EnsureBindingsRoot(composer, report, useUndo);
            Transform cameraTarget = EnsureAnchor(composer, "CameraTarget", report, useUndo);
            Transform lookAtTarget = EnsureAnchor(composer, "LookAtTarget", report, useUndo);

            composer.EditorSetGeneratedReferences(bindingsRoot, cameraTarget, lookAtTarget);
            serializedComposer.Update();

            Component actorDeclaration = EnsureComponent(composer.gameObject, "Immersive.Framework.Actors.PlayerActorDeclaration", "PlayerActorDeclaration", true, report, useUndo, component =>
            {
                bool changed = false;
                changed |= SetSerialized(component, "actorId", composer.ActorId);
                changed |= SetSerialized(component, "displayName", composer.name);
                changed |= SetSerialized(component, "playerInput", playerInputReference);
                changed |= SetSerialized(component, "reason", "player-composer.apply");
                return changed;
            });

            Component slotDeclaration = EnsureComponent(composer.gameObject, "Immersive.Framework.PlayerSlots.PlayerSlotDeclaration", "PlayerSlotDeclaration", true, report, useUndo, component =>
            {
                bool changed = false;
                changed |= SetSerialized(component, "slotId", composer.PlayerSlotId);
                changed |= SetSerialized(component, "displayName", composer.name);
                changed |= SetSerialized(component, "playerInput", playerInputReference);
                changed |= SetSerialized(component, "reason", "player-composer.apply");
                return changed;
            });

            EnsureOptionalRootComponent(composer, "UnityPlayerInputGateAdapter", report, useUndo, component =>
            {
                bool changed = false;
                changed |= SetSerialized(component, "playerInput", playerInputReference);
                changed |= SetSerialized(component, "sourceSlot", composer.PlayerSlotId);
                changed |= SetSerialized(component, "sourceSlotId", composer.PlayerSlotId);
                changed |= SetSerialized(component, "actionMapName", composer.GameplayActionMap);
                changed |= SetSerialized(component, "gameplayActionMap", composer.GameplayActionMap);
                return changed;
            });

            if (composer.ResetEnabled)
            {
                EnsureOptionalRootComponent(composer, "UnityResetSubjectAdapter", report, useUndo, component =>
                {
                    bool changed = false;
                    changed |= SetSerialized(component, "sourcePlayerActor", actorDeclaration);
                    changed |= SetSerialized(component, "displayName", composer.name);
                    changed |= SetSerialized(component, "diagnosticTag", "player-composer");
                    changed |= SetSerialized(component, "scope", composer.ResetScope);
                    return changed;
                });
            }
            else
            {
                report.SkippedByPolicy("reset-subject:reset-disabled");
            }

            EnsureBindingComponent(bindingsRoot, "Immersive.Framework.PlayerBinding.PlayerControlBindingTargetBehaviour", "PlayerControlBindingTargetBehaviour", true, report, useUndo, component =>
            {
                return SetSerialized(component, "bindingTargetName", "Player Control Binding Target");
            });

            EnsureBindingComponent(bindingsRoot, "Immersive.Framework.PlayerBinding.UnityPlayerInputBridgeTargetBehaviour", "UnityPlayerInputBridgeTargetBehaviour", true, report, useUndo, component =>
            {
                bool changed = false;
                changed |= SetSerialized(component, "bridgeTargetName", "Unity PlayerInput Bridge Target");
                changed |= SetSerialized(component, "expectedPlayerSlotId", composer.PlayerSlotId);
                changed |= SetSerialized(component, "playerInput", playerInputReference);
                return changed;
            });

            EnsureBindingComponent(bindingsRoot, "Immersive.Framework.PlayerBinding.UnityPlayerInputActivationTargetBehaviour", "UnityPlayerInputActivationTargetBehaviour", true, report, useUndo, component =>
            {
                bool changed = false;
                changed |= SetSerialized(component, "activationTargetName", "Unity PlayerInput Activation Target");
                changed |= SetSerialized(component, "expectedPlayerSlotId", composer.PlayerSlotId);
                changed |= SetSerialized(component, "playerInput", playerInputReference);
                changed |= SetSerialized(component, "actionMapName", composer.GameplayActionMap);
                return changed;
            });

            if (composer.MaterializeSlotOccupancy)
            {
                EnsureBindingComponent(bindingsRoot, "Immersive.Framework.PlayerSlots.PlayerSlotOccupancy", "PlayerSlotOccupancy", true, report, useUndo, component =>
                {
                    bool changed = false;
                    changed |= SetSerialized(component, "slotDeclaration", slotDeclaration);
                    changed |= SetSerialized(component, "slotId", composer.PlayerSlotId);
                    changed |= SetSerialized(component, "playerActorDeclaration", actorDeclaration);
                    changed |= SetSerialized(component, "occupiedActorId", composer.ActorId);
                    changed |= SetSerialized(component, "displayName", "Player Slot Occupancy");
                    changed |= SetSerialized(component, "reason", "player-composer.apply");
                    return changed;
                });
            }
            else
            {
                report.SkippedByPolicy("slot-occupancy:policy-disabled");
            }

            if (composer.CameraBindingRequired)
            {
                EnsureBindingComponent(bindingsRoot, null, "FrameworkCameraAnchorHost", false, report, useUndo, component =>
                {
                    bool changed = false;
                    changed |= SetSerialized(component, "trackingTarget", cameraTargetReference != null ? cameraTargetReference : cameraTarget);
                    changed |= SetSerialized(component, "lookAtTarget", lookAtTargetReference != null ? lookAtTargetReference : lookAtTarget);
                    return changed;
                });
            }
            else
            {
                report.SkippedByPolicy("camera-binding:policy-disabled");
            }

            if (composer.ResetEnabled && composer.ResetParticipantPolicy == PlayerComposerResetParticipantPolicy.Transform)
            {
                EnsureBindingComponent(bindingsRoot, "Immersive.Framework.Reset.Unity.UnityTransformResetParticipant", "UnityTransformResetParticipant", true, report, useUndo, component =>
                {
                    return SetSerialized(component, "target", composer.transform);
                });
            }
            else
            {
                report.SkippedByPolicy($"reset-participant:{composer.ResetParticipantPolicy}");
            }

            string summary = report.CreateSummary();
            string status = report.BlockedCount > 0 ? "ApplyCompletedWithBlockingIssues" : "ApplySucceeded";
            string blockingIssue = report.BlockedCount > 0 ? report.FirstBlockingIssue : string.Empty;
            composer.EditorSetApplyRebuildResult(status, blockingIssue, summary);
            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(composer.gameObject);
            serializedComposer.Update();
            CollapseUndoIfNeeded(useUndo, undoGroup);

            if (logDiagnostics && composer.LogApplyRebuildDiagnostics)
            {
                Debug.Log($"[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='{composer.name}' actorId='{composer.ActorId}' playerSlotId='{composer.PlayerSlotId}' created='{report.CreatedCount}' repaired='{report.RepairedCount}' alreadyValid='{report.AlreadyValidCount}' skippedByPolicy='{report.SkippedByPolicyCount}' blocked='{report.BlockedCount}' resetEnabled='{composer.ResetEnabled}' resetParticipantPolicy='{composer.ResetParticipantPolicy}'", composer);
            }

            return PlayerComposerApplyRebuildResult.ApplyCompleted(
                report.BlockedCount == 0,
                status,
                blockingIssue,
                summary,
                report.CreatedCount,
                report.RepairedCount,
                report.AlreadyValidCount,
                report.SkippedByPolicyCount,
                report.BlockedCount);
        }

        private static void CollapseUndoIfNeeded(bool useUndo, int undoGroup)
        {
            if (useUndo && undoGroup >= 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static Transform EnsureBindingsRoot(PlayerComposer composer, PlayerComposerMaterializationReport report, bool useUndo)
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
                frameworkRoot = CreateChild(composer.transform, "_Framework", useUndo);
                report.Created("container:_Framework");
            }
            else
            {
                report.AlreadyValid("container:_Framework");
            }

            Transform bindingsRoot = FindDirectChild(frameworkRoot, "_Bindings");
            if (bindingsRoot == null)
            {
                bindingsRoot = CreateChild(frameworkRoot, "_Bindings", useUndo);
                report.Created("bindings-root:_Framework/_Bindings");
            }
            else
            {
                report.AlreadyValid("bindings-root:_Framework/_Bindings");
            }

            return bindingsRoot;
        }

        private static Transform EnsureAnchor(PlayerComposer composer, string anchorName, PlayerComposerMaterializationReport report, bool useUndo)
        {
            if (!composer.CreateAnchorsIfMissing)
            {
                report.SkippedByPolicy($"anchor:{anchorName}:anchor-creation-disabled");
                return composer.transform;
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

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

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

        private static Transform CreateChild(Transform parent, string childName, bool useUndo)
        {
            var child = new GameObject(childName);
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            }

            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Component EnsureComponent(GameObject owner, string fullName, string simpleName, bool required, PlayerComposerMaterializationReport report, bool useUndo, Func<Component, bool> configure)
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

            Component created = useUndo ? Undo.AddComponent(owner, type) : owner.AddComponent(type);
            configure?.Invoke(created);
            report.Created(simpleName);
            return created;
        }

        private static void EnsureOptionalRootComponent(PlayerComposer composer, string simpleName, PlayerComposerMaterializationReport report, bool useUndo, Func<Component, bool> configure)
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

            Component created = useUndo ? Undo.AddComponent(composer.gameObject, type) : composer.gameObject.AddComponent(type);
            configure?.Invoke(created);
            report.Created($"root:{simpleName}");
        }

        private static void EnsureBindingComponent(Transform bindingsRoot, string fullName, string simpleName, bool required, PlayerComposerMaterializationReport report, bool useUndo, Func<Component, bool> configure)
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

            Component created = useUndo ? Undo.AddComponent(bindingsRoot.gameObject, type) : bindingsRoot.gameObject.AddComponent(type);
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

            private void Add(string kind, string entry)
            {
                _entries.Add($"{kind}:{entry ?? string.Empty}");
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

        public static PlayerComposerApplyRebuildResult ValidationSucceeded(string summary)
        {
            return new PlayerComposerApplyRebuildResult(true, "ValidationSucceeded", string.Empty, summary, 0, 0, 0, 0, 0);
        }

        public static PlayerComposerApplyRebuildResult ValidationFailed(string issue)
        {
            return new PlayerComposerApplyRebuildResult(false, "ValidationFailed", issue, string.Empty, 0, 0, 0, 0, 1);
        }

        public static PlayerComposerApplyRebuildResult ApplyFailed(string issue)
        {
            return new PlayerComposerApplyRebuildResult(false, "ApplyFailed", issue, string.Empty, 0, 0, 0, 0, 1);
        }

        public static PlayerComposerApplyRebuildResult ApplyCompleted(
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
            return new PlayerComposerApplyRebuildResult(
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
}
