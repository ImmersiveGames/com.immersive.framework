using System;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Validation
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
            ValidateOpenSceneResetRequestTriggers(report);
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
            var selectionProperty = serializedObject.FindProperty("selection");
            ValidateResetSelectionConfig(report, selectionProperty, trigger, "Object Reset Group Trigger");
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
            var resetTargetProperty = serializedObject.FindProperty("resetTarget");

            bool hasTargetActivity = targetActivityProperty != null && targetActivityProperty.objectReferenceValue != null;
            bool useCurrentActivityWhenTargetMissing = useCurrentActivityWhenTargetMissingProperty == null || useCurrentActivityWhenTargetMissingProperty.boolValue;
            bool requireTargetActivityIsCurrent = requireTargetActivityIsCurrentProperty == null || requireTargetActivityIsCurrentProperty.boolValue;

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

            ValidateResetTarget(report, resetTargetProperty, trigger, "Activity Restart Trigger");
            ValidateTriggerStacking(report, trigger);
        }

        private static void ValidateOpenSceneResetRequestTriggers(FrameworkAuthoringValidationReport report)
        {
            ResetRequestTrigger[] triggers = Object.FindObjectsByType<ResetRequestTrigger>(FindObjectsInactive.Include);
            int scannedCount = 0;
            if (triggers != null)
            {
                for (int index = 0; index < triggers.Length; index++)
                {
                    ResetRequestTrigger trigger = triggers[index];
                    if (!IsLoadedSceneComponent(trigger)) continue;
                    scannedCount++;
                    var serializedObject = new SerializedObject(trigger);
                    ValidateResetTarget(report, serializedObject.FindProperty("target"), trigger, "Reset Request Trigger");
                }
            }

            if (scannedCount > 0)
                report.AddInfo($"Reset Request Trigger authoring validation scanned triggers='{scannedCount}'.", null);
        }

        private static void ValidateResetTarget(
            FrameworkAuthoringValidationReport report,
            SerializedProperty targetProperty,
            Object context,
            string label)
        {
            SerializedProperty kindProperty = targetProperty?.FindPropertyRelative("kind");
            if (kindProperty == null)
            {
                report.AddError($"{label} has no semantic Reset Target.", context);
                return;
            }

            ResetTargetKind kind = (ResetTargetKind)kindProperty.intValue;
            if (!Enum.IsDefined(typeof(ResetTargetKind), kind) || kind == ResetTargetKind.Unknown)
            {
                report.AddError($"{label} has an invalid Reset Target kind.", context);
                return;
            }

            if (kind == ResetTargetKind.Object
                && targetProperty.FindPropertyRelative("resettable")?.objectReferenceValue == null)
                report.AddError($"{label} targets Object but has no Resettable reference.", context);

            if (kind == ResetTargetKind.Composition
                && targetProperty.FindPropertyRelative("composition")?.objectReferenceValue == null)
                report.AddError($"{label} targets Composition but has no ResetComposition reference.", context);
        }

        private static void ValidateResetSelectionConfig(
            FrameworkAuthoringValidationReport report,
            SerializedProperty resetSelectionProperty,
            Object context,
            string label)
        {
            if (resetSelectionProperty == null)
            {
                report.AddError($"{label} has no Reset Selection config.", context);
                return;
            }

            var modeProperty = resetSelectionProperty.FindPropertyRelative("mode");
            var explicitSubjectsProperty = resetSelectionProperty.FindPropertyRelative("explicitSubjects");
            var allowNoSubjectsProperty = resetSelectionProperty.FindPropertyRelative("allowNoSubjects");
            ResetSelectionMode mode = ResolveResetSelectionMode(modeProperty);
            bool allowNoSubjects = allowNoSubjectsProperty != null && allowNoSubjectsProperty.boolValue;
            int explicitSubjectCount = CountArray(explicitSubjectsProperty);

            if (!Enum.IsDefined(typeof(ResetSelectionMode), mode) || mode == ResetSelectionMode.Unknown)
            {
                report.AddError(
                    $"{label} has invalid Reset Selection Mode. Choose ExplicitSubjects, CurrentActivitySubjects, CurrentRouteSubjects, CurrentRouteAndActivitySubjects or AllCurrentSubjects.",
                    context);
                return;
            }

            if (mode == ResetSelectionMode.ExplicitSubjects)
            {
                if (explicitSubjectCount == 0 && !allowNoSubjects)
                {
                    report.AddError(
                        $"{label} uses ExplicitSubjects but has no explicit Reset Subjects and Allow No Subjects is disabled.",
                        context);
                }

                ValidateResetSubjectReferences(report, explicitSubjectsProperty, context, $"{label} explicit subject");
            }
            else if (explicitSubjectCount > 0)
            {
                report.AddWarning(
                    $"{label} uses scoped Reset Selection Mode '{mode}', so explicit Reset Subjects are ignored. Remove explicit subjects or switch to ExplicitSubjects.",
                    context);
            }

            if (mode == ResetSelectionMode.CurrentActivitySubjects)
            {
                report.AddInfo(
                    "CurrentActivity includes Activity-owned subjects and Route-owned subjects with effective Activity membership. Activity Restart further filters out Activity-owned content recreated by Clear/Reenter.",
                    context);
            }
        }

        private static void ValidateResetSubjectReferences(
            FrameworkAuthoringValidationReport report,
            SerializedProperty explicitSubjectsProperty,
            Object context,
            string label)
        {
            if (explicitSubjectsProperty == null || !explicitSubjectsProperty.isArray)
            {
                return;
            }

            for (int i = 0; i < explicitSubjectsProperty.arraySize; i++)
            {
                var referenceProperty = explicitSubjectsProperty.GetArrayElementAtIndex(i);
                if (referenceProperty == null)
                {
                    report.AddError($"{label} index '{i}' is null.", context);
                    continue;
                }

                var adapterProperty = referenceProperty.FindPropertyRelative("subjectAdapter");
                var subjectIdProperty = referenceProperty.FindPropertyRelative("subjectId");
                bool hasAdapter = adapterProperty != null && adapterProperty.objectReferenceValue != null;
                string subjectId = subjectIdProperty != null ? subjectIdProperty.stringValue : string.Empty;

                if (!hasAdapter && string.IsNullOrWhiteSpace(subjectId))
                {
                    report.AddError(
                        $"{label} index '{i}' has no UnityResetSubjectAdapter and no ResetSubjectId text.",
                        context);
                }

                if (hasAdapter && !string.IsNullOrWhiteSpace(subjectId))
                {
                    report.AddWarning(
                        $"{label} index '{i}' has both UnityResetSubjectAdapter and ResetSubjectId text. The adapter wins at runtime; remove the extra id unless this is deliberate documentation.",
                        context);
                }
            }
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

        private static ResetSelectionMode ResolveResetSelectionMode(SerializedProperty property)
        {
            if (property == null)
            {
                return ResetSelectionMode.Unknown;
            }

            return (ResetSelectionMode)property.intValue;
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
