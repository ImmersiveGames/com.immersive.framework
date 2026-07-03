using System;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class FrameworkResetRestartAuthoringValidator
    {
        internal static void ValidateOpenScenes(FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            ValidateOpenSceneObjectResetGroupTriggers(report);
            ValidateOpenSceneActivityRestartTriggers(report);
        }

        private static void ValidateOpenSceneObjectResetGroupTriggers(FrameworkAuthoringValidationReport report)
        {
            ObjectResetGroupTrigger[] triggers = Object.FindObjectsByType<ObjectResetGroupTrigger>(FindObjectsInactive.Include);
            int scannedCount = 0;

            if (triggers != null)
            {
                for (int i = 0; i < triggers.Length; i++)
                {
                    var trigger = triggers[i];
                    if (!IsLoadedSceneComponent(trigger))
                    {
                        continue;
                    }

                    scannedCount++;
                    ValidateObjectResetGroupTrigger(report, trigger);
                }
            }

            if (scannedCount == 0)
            {
                report.AddInfo("No scene-authored Object Reset Group Trigger components were found in loaded scenes.", null);
                return;
            }

            report.AddInfo($"Object Reset Group Trigger authoring validation scanned triggers='{scannedCount}'.", null);
        }

        private static void ValidateOpenSceneActivityRestartTriggers(FrameworkAuthoringValidationReport report)
        {
            ActivityRestartTrigger[] triggers = Object.FindObjectsByType<ActivityRestartTrigger>(FindObjectsInactive.Include);
            int scannedCount = 0;

            if (triggers != null)
            {
                for (int i = 0; i < triggers.Length; i++)
                {
                    var trigger = triggers[i];
                    if (!IsLoadedSceneComponent(trigger))
                    {
                        continue;
                    }

                    scannedCount++;
                    ValidateActivityRestartTrigger(report, trigger);
                }
            }

            if (scannedCount == 0)
            {
                report.AddInfo("No scene-authored Activity Restart Trigger components were found in loaded scenes.", null);
                return;
            }

            report.AddInfo($"Activity Restart Trigger authoring validation scanned triggers='{scannedCount}'.", null);
        }

        private static void ValidateObjectResetGroupTrigger(
            FrameworkAuthoringValidationReport report,
            ObjectResetGroupTrigger trigger)
        {
            if (trigger == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(trigger);
            var groupAssetProperty = serializedObject.FindProperty("groupAsset");
            var entriesProperty = serializedObject.FindProperty("entries");

            bool hasGroupAsset = groupAssetProperty != null && groupAssetProperty.objectReferenceValue != null;
            int inlineEntryCount = CountArray(entriesProperty);
            int groupAssetEntryCount = hasGroupAsset
                ? CountGroupAssetEntries(groupAssetProperty.objectReferenceValue as ObjectResetGroupAsset)
                : 0;

            if (!hasGroupAsset && inlineEntryCount == 0)
            {
                report.AddError(
                    "Object Reset Group Trigger has no Group Asset and no inline Reset Entries. Configure at least one target or assign an Object Reset Group Asset.",
                    trigger);
                return;
            }

            if (hasGroupAsset)
            {
                if (groupAssetEntryCount == 0)
                {
                    report.AddError(
                        "Object Reset Group Trigger references an Object Reset Group Asset with no targets.",
                        trigger);
                }

                if (inlineEntryCount > 0)
                {
                    report.AddWarning(
                        "Object Reset Group Trigger has both Group Asset and inline entries. The Group Asset provides the resolved entries; keep only one authoring source to avoid designer confusion.",
                        trigger);
                }
            }

            ValidateEntryArray(report, entriesProperty, trigger, "Object Reset Group Trigger inline entry");
        }

        private static void ValidateActivityRestartTrigger(
            FrameworkAuthoringValidationReport report,
            ActivityRestartTrigger trigger)
        {
            if (trigger == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(trigger);
            var targetActivityProperty = serializedObject.FindProperty("targetActivity");
            var useCurrentActivityWhenTargetMissingProperty = serializedObject.FindProperty("useCurrentActivityWhenTargetMissing");
            var requireTargetActivityIsCurrentProperty = serializedObject.FindProperty("requireTargetActivityIsCurrent");
            var resetSelectionModeProperty = serializedObject.FindProperty("resetSelectionMode");
            var resetGroupAssetProperty = serializedObject.FindProperty("resetGroupAsset");
            var resetEntriesProperty = serializedObject.FindProperty("resetEntries");

            bool hasTargetActivity = targetActivityProperty != null && targetActivityProperty.objectReferenceValue != null;
            bool useCurrentActivityWhenTargetMissing = useCurrentActivityWhenTargetMissingProperty == null || useCurrentActivityWhenTargetMissingProperty.boolValue;
            bool requireTargetActivityIsCurrent = requireTargetActivityIsCurrentProperty == null || requireTargetActivityIsCurrentProperty.boolValue;
            var resetSelectionMode = ResolveSelectionMode(resetSelectionModeProperty);
            bool hasResetGroupAsset = resetGroupAssetProperty != null && resetGroupAssetProperty.objectReferenceValue != null;
            int inlineEntryCount = CountArray(resetEntriesProperty);
            int resetGroupAssetEntryCount = hasResetGroupAsset
                ? CountGroupAssetEntries(resetGroupAssetProperty.objectReferenceValue as ObjectResetGroupAsset)
                : 0;

            if (!hasTargetActivity && !useCurrentActivityWhenTargetMissing)
            {
                report.AddError(
                    "Activity Restart Trigger has no Target Activity and Use Current Activity When Target Missing is disabled. The restart cannot resolve an Activity target.",
                    trigger);
            }

            if (hasTargetActivity && !requireTargetActivityIsCurrent)
            {
                report.AddWarning(
                    "Activity Restart Trigger targets an explicit Activity without requiring it to be current. This can act like an Activity switch; keep Require Target Activity Is Current enabled for restart semantics unless this is deliberate.",
                    trigger);
            }

            if (!Enum.IsDefined(typeof(ObjectResetSelectionMode), resetSelectionMode) || resetSelectionMode == ObjectResetSelectionMode.Unknown)
            {
                report.AddError(
                    "Activity Restart Trigger has invalid Reset Selection Mode. Choose ExplicitTargets, CurrentActivityEntries, CurrentRouteEntries, CurrentRouteAndActivityEntries or AllCurrentEntries.",
                    trigger);
                return;
            }

            if (resetSelectionMode == ObjectResetSelectionMode.ExplicitTargets)
            {
                if (!hasResetGroupAsset && inlineEntryCount == 0)
                {
                    report.AddError(
                        "Activity Restart Trigger uses ExplicitTargets but has no Reset Group Asset and no inline Reset Entries.",
                        trigger);
                }

                if (hasResetGroupAsset && resetGroupAssetEntryCount == 0)
                {
                    report.AddError(
                        "Activity Restart Trigger uses ExplicitTargets with an Object Reset Group Asset that has no targets.",
                        trigger);
                }

                if (hasResetGroupAsset && inlineEntryCount > 0)
                {
                    report.AddWarning(
                        "Activity Restart Trigger has both Reset Group Asset and inline Reset Entries. In ExplicitTargets mode the Reset Group Asset provides the resolved entries; remove inline entries or remove the asset to make authoring unambiguous.",
                        trigger);
                }

                ValidateEntryArray(report, resetEntriesProperty, trigger, "Activity Restart Trigger inline reset entry");
            }
            else
            {
                if (hasResetGroupAsset || inlineEntryCount > 0)
                {
                    report.AddWarning(
                        $"Activity Restart Trigger uses scoped Reset Selection Mode '{resetSelectionMode}', so explicit Reset Group Asset/inline entries are ignored. Remove explicit targets or switch to ExplicitTargets.",
                        trigger);
                }

                if (resetSelectionMode == ObjectResetSelectionMode.CurrentActivityEntries)
                {
                    report.AddInfo(
                        "Activity Restart Trigger uses CurrentActivityEntries. Route-scoped ObjectEntries such as a route-owned player are not included by this policy.",
                        trigger);
                }
            }

            ValidateTriggerStacking(report, trigger);
        }

        private static void ValidateTriggerStacking(
            FrameworkAuthoringValidationReport report,
            ActivityRestartTrigger trigger)
        {
            if (trigger == null)
            {
                return;
            }

            if (trigger.GetComponent<ObjectResetGroupTrigger>() != null)
            {
                report.AddWarning(
                    "Activity Restart Trigger is on the same GameObject as ObjectResetGroupTrigger. This is valid only if different UI buttons call different components; a Restart button should call only ActivityRestartTrigger.RequestActivityRestart().",
                    trigger);
            }

            if (trigger.GetComponent<ObjectResetTrigger>() != null)
            {
                report.AddWarning(
                    "Activity Restart Trigger is on the same GameObject as ObjectResetTrigger. This is valid only if different UI buttons call different components; a Restart button should not also call ObjectResetTrigger.RequestObjectReset().",
                    trigger);
            }
        }

        private static void ValidateEntryArray(
            FrameworkAuthoringValidationReport report,
            SerializedProperty entriesProperty,
            Object context,
            string label)
        {
            if (entriesProperty == null || !entriesProperty.isArray)
            {
                return;
            }

            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                var entryProperty = entriesProperty.GetArrayElementAtIndex(i);
                if (entryProperty == null)
                {
                    report.AddError($"{label} index '{i}' is null.", context);
                    continue;
                }

                var enabledProperty = entryProperty.FindPropertyRelative("enabled");
                if (enabledProperty != null && !enabledProperty.boolValue)
                {
                    continue;
                }

                var targetDeclarationProperty = entryProperty.FindPropertyRelative("targetDeclaration");
                var objectEntryIdProperty = entryProperty.FindPropertyRelative("objectEntryId");
                var targetDeclaration = targetDeclarationProperty != null
                    ? targetDeclarationProperty.objectReferenceValue as ObjectEntryDeclaration
                    : null;
                string objectEntryId = objectEntryIdProperty != null ? objectEntryIdProperty.stringValue : string.Empty;

                if (targetDeclaration == null && string.IsNullOrWhiteSpace(objectEntryId))
                {
                    report.AddError(
                        $"{label} index '{i}' has no Target Declaration and no Object Entry Id.",
                        context);
                    continue;
                }

                if (targetDeclaration != null)
                {
                    ValidateTargetDeclaration(report, targetDeclaration, context, $"{label} index '{i}'");
                }

                if (targetDeclaration != null && !string.IsNullOrWhiteSpace(objectEntryId))
                {
                    report.AddWarning(
                        $"{label} index '{i}' has both Target Declaration and Object Entry Id. Target Declaration wins; remove the extra id unless this is deliberate documentation.",
                        context);
                }
            }
        }

        private static void ValidateTargetDeclaration(
            FrameworkAuthoringValidationReport report,
            ObjectEntryDeclaration declaration,
            Object context,
            string label)
        {
            if (declaration == null)
            {
                return;
            }

            if (!declaration.HasObjectEntryId)
            {
                report.AddError($"{label} references an ObjectEntryDeclaration with no Object Entry Id.", context);
                return;
            }

            if (!declaration.HasRequiredAuthoredOwner)
            {
                report.AddError(
                    $"{label} references ObjectEntry '{declaration.ObjectEntryIdText}' without the required authored owner for scope '{declaration.Scope}'.",
                    context);
                return;
            }

            if (!declaration.TryCreateDescriptor(out _, out string issue))
            {
                report.AddError(
                    $"{label} references ObjectEntry '{declaration.ObjectEntryIdText}' that cannot create a descriptor. {issue}",
                    context);
            }
        }

        private static ObjectResetSelectionMode ResolveSelectionMode(SerializedProperty property)
        {
            if (property == null)
            {
                return ObjectResetSelectionMode.Unknown;
            }

            return (ObjectResetSelectionMode)property.intValue;
        }

        private static int CountGroupAssetEntries(ObjectResetGroupAsset groupAsset)
        {
            if (groupAsset == null)
            {
                return 0;
            }

            try
            {
                return groupAsset.EntryCount;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static int CountArray(SerializedProperty property)
        {
            return property != null && property.isArray ? property.arraySize : 0;
        }

        private static bool IsLoadedSceneComponent(Component component)
        {
            return component != null
                && component.gameObject != null
                && component.gameObject.scene.IsValid()
                && component.gameObject.scene.isLoaded;
        }
    }
}
