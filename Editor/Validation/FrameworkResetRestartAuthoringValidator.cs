using System;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Validation
{
    internal static class FrameworkResetRestartAuthoringValidator
    {
        internal static void ValidateOpenScenes(FrameworkAuthoringValidationReport report)
        {
            if (report == null) return;
            ValidateOpenSceneActivityRestartTriggers(report);
            ValidateOpenSceneResetRequestTriggers(report);
        }

        private static void ValidateOpenSceneActivityRestartTriggers(FrameworkAuthoringValidationReport report)
        {
            ActivityRestartTrigger[] triggers = Object.FindObjectsByType<ActivityRestartTrigger>(FindObjectsInactive.Include);
            int scannedCount = 0;
            if (triggers != null)
            {
                for (int index = 0; index < triggers.Length; index++)
                {
                    ActivityRestartTrigger trigger = triggers[index];
                    if (!IsLoadedSceneComponent(trigger)) continue;
                    scannedCount++;
                    ValidateActivityRestartTrigger(report, trigger);
                }
            }

            if (scannedCount == 0)
                report.AddInfo("No scene-authored Activity Restart Trigger components were found in loaded scenes.", null);
            else
                report.AddInfo($"Activity Restart Trigger authoring validation scanned triggers='{scannedCount}'.", null);
        }

        private static void ValidateActivityRestartTrigger(
            FrameworkAuthoringValidationReport report,
            ActivityRestartTrigger trigger)
        {
            if (trigger == null) return;
            var serializedObject = new SerializedObject(trigger);
            SerializedProperty targetActivity = serializedObject.FindProperty("targetActivity");
            SerializedProperty useCurrent = serializedObject.FindProperty("useCurrentActivityWhenTargetMissing");
            SerializedProperty requireCurrent = serializedObject.FindProperty("requireTargetActivityIsCurrent");
            bool hasTargetActivity = targetActivity != null && targetActivity.objectReferenceValue != null;
            bool useCurrentWhenMissing = useCurrent == null || useCurrent.boolValue;
            bool requireTargetIsCurrent = requireCurrent == null || requireCurrent.boolValue;

            if (!hasTargetActivity && !useCurrentWhenMissing)
                report.AddError("Activity Restart Trigger has no Target Activity and Use Current Activity When Target Missing is disabled.", trigger);
            if (hasTargetActivity && !requireTargetIsCurrent)
                report.AddWarning("Activity Restart Trigger targets an explicit Activity without requiring it to be current.", trigger);

            ValidateResetTarget(report, serializedObject.FindProperty("resetTarget"), trigger, "Activity Restart Trigger");
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
            SerializedProperty target,
            Object context,
            string label)
        {
            SerializedProperty kindProperty = target?.FindPropertyRelative("kind");
            if (kindProperty == null)
            {
                report.AddError($"{label} has no semantic Reset Target.", context);
                return;
            }

            ResetTargetKind kind = (ResetTargetKind)kindProperty.intValue;
            if (!Enum.IsDefined(typeof(ResetTargetKind), kind) || kind == ResetTargetKind.Unknown)
            {
                report.AddError($"{label} has an invalid or Unknown Reset Target kind.", context);
                return;
            }

            SerializedProperty objectTarget = target.FindPropertyRelative("objectTarget");
            SerializedProperty compositionTarget = target.FindPropertyRelative("compositionTarget");
            if (kind == ResetTargetKind.Object)
            {
                ValidateObjectTarget(report, objectTarget, context, label);
                if (HasCompositionPayload(compositionTarget))
                    report.AddError($"{label} has stale Composition payload while targeting Object.", context);
            }
            else if (kind == ResetTargetKind.Composition)
            {
                ValidateCompositionTarget(report, compositionTarget, context, label);
                if (HasObjectPayload(objectTarget))
                    report.AddError($"{label} has stale Object payload while targeting Composition.", context);
            }
            else if (HasObjectPayload(objectTarget) || HasCompositionPayload(compositionTarget))
            {
                report.AddError($"{label} has stale Object/Composition payload while targeting {kind}.", context);
            }
        }

        private static void ValidateObjectTarget(FrameworkAuthoringValidationReport report, SerializedProperty target, Object context, string label)
        {
            ValidateReferenceTarget(report, target, context, label, "directResettable", "Object/Direct requires a Resettable reference.");
        }

        private static void ValidateCompositionTarget(FrameworkAuthoringValidationReport report, SerializedProperty target, Object context, string label)
        {
            ValidateReferenceTarget(report, target, context, label, "directComposition", "Composition/Direct requires a ResetComposition reference.");
        }

        private static void ValidateReferenceTarget(
            FrameworkAuthoringValidationReport report,
            SerializedProperty target,
            Object context,
            string label,
            string directField,
            string directMissingMessage)
        {
            if (target == null)
            {
                report.AddError($"{label} has no typed target payload.", context);
                return;
            }

            SerializedProperty modeProperty = target.FindPropertyRelative("referenceMode");
            ResetReferenceMode mode = modeProperty != null ? (ResetReferenceMode)modeProperty.intValue : (ResetReferenceMode)(-1);
            if (!Enum.IsDefined(typeof(ResetReferenceMode), mode))
            {
                report.AddError($"{label} has an invalid Reference Mode.", context);
                return;
            }

            SerializedProperty direct = target.FindPropertyRelative(directField);
            SerializedProperty stable = target.FindPropertyRelative("stableReference");
            if (mode == ResetReferenceMode.Direct)
            {
                if (direct == null || direct.objectReferenceValue == null)
                    report.AddError($"{label}: {directMissingMessage}", context);
                if (HasStableReferencePayload(stable))
                    report.AddError($"{label} has a stale Stable reference while using Direct mode.", context);
                return;
            }

            if (direct != null && direct.objectReferenceValue != null)
                report.AddError($"{label} has a stale direct reference while using Stable mode.", context);
            ValidateStableReference(report, stable, context, label);
        }

        private static void ValidateStableReference(FrameworkAuthoringValidationReport report, SerializedProperty stable, Object context, string label)
        {
            if (stable == null)
            {
                report.AddError($"{label} has no StableObjectReference payload.", context);
                return;
            }

            string idText = stable.FindPropertyRelative("objectEntryIdText")?.stringValue ?? string.Empty;
            try
            {
                ObjectEntryId.From(idText.Trim());
            }
            catch (ArgumentException)
            {
                report.AddError($"{label} Stable reference requires a valid ObjectEntryId.", context);
                return;
            }

            SerializedProperty selector = stable.FindPropertyRelative("ownerSelectorKind");
            StableObjectOwnerSelectorKind kind = selector != null
                ? (StableObjectOwnerSelectorKind)selector.intValue
                : (StableObjectOwnerSelectorKind)(-1);
            SerializedProperty route = stable.FindPropertyRelative("routeOwner");
            SerializedProperty activity = stable.FindPropertyRelative("activityOwner");
            if (!Enum.IsDefined(typeof(StableObjectOwnerSelectorKind), kind))
            {
                report.AddError($"{label} Stable reference has an invalid owner selector.", context);
                return;
            }

            if (kind == StableObjectOwnerSelectorKind.Route)
            {
                RouteAsset routeAsset = route != null ? route.objectReferenceValue as RouteAsset : null;
                if (routeAsset == null || !routeAsset.HasValidRouteId)
                    report.AddError($"{label} Stable reference Route selector requires a RouteAsset with a valid RouteId.", context);
                if (activity != null && activity.objectReferenceValue != null)
                    report.AddError($"{label} Stable reference has an inactive Activity selector asset.", context);
            }
            else if (kind == StableObjectOwnerSelectorKind.Activity)
            {
                ActivityAsset activityAsset = activity != null ? activity.objectReferenceValue as ActivityAsset : null;
                if (activityAsset == null || !activityAsset.HasValidActivityId)
                    report.AddError($"{label} Stable reference Activity selector requires an ActivityAsset with a valid ActivityId.", context);
                if (route != null && route.objectReferenceValue != null)
                    report.AddError($"{label} Stable reference has an inactive Route selector asset.", context);
            }
            else if ((route != null && route.objectReferenceValue != null)
                || (activity != null && activity.objectReferenceValue != null))
            {
                report.AddError($"{label} Stable reference has an owner asset while the selector is Unspecified.", context);
            }
        }

        private static bool HasObjectPayload(SerializedProperty target) => target != null
            && (target.FindPropertyRelative("directResettable")?.objectReferenceValue != null
                || HasStableReferencePayload(target.FindPropertyRelative("stableReference")));

        private static bool HasCompositionPayload(SerializedProperty target) => target != null
            && (target.FindPropertyRelative("directComposition")?.objectReferenceValue != null
                || HasStableReferencePayload(target.FindPropertyRelative("stableReference")));

        private static bool HasStableReferencePayload(SerializedProperty stable) => stable != null
            && (!string.IsNullOrWhiteSpace(stable.FindPropertyRelative("objectEntryIdText")?.stringValue)
                || (stable.FindPropertyRelative("ownerSelectorKind")?.intValue ?? (int)StableObjectOwnerSelectorKind.Unspecified)
                    != (int)StableObjectOwnerSelectorKind.Unspecified
                || (stable.FindPropertyRelative("routeOwner")?.objectReferenceValue != null)
                || (stable.FindPropertyRelative("activityOwner")?.objectReferenceValue != null));

        private static bool IsLoadedSceneComponent(Component component) => component != null
            && component.gameObject != null
            && component.gameObject.scene.IsValid()
            && component.gameObject.scene.isLoaded;
    }
}
