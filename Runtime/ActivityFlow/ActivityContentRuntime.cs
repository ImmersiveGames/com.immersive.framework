using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for applying Activity content visibility in loaded scenes.
    /// It does not load scenes, spawn actors, or own Activity identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class ActivityContentRuntime
    {
        private const int MaxObservedBindingsInMessage = 8;

        private readonly FrameworkLogger _logger = FrameworkLogger.Create<ActivityContentRuntime>();

        private ActivityContentApplyResult _lastApplyResult;
        private bool _hasLastApplyResult;
        private RouteAsset _routeScope;
        private ActivityContentDiscoveryScope _discoveryScope;

        internal bool HasLastApplyResult => _hasLastApplyResult;

        internal void SetRouteScope(RouteAsset route)
        {
            _routeScope = route;
        }

        internal void SetDiscoveryScope(ActivityContentDiscoveryScope scope)
        {
            _discoveryScope = scope;
            if (scope.Route != null)
            {
                _routeScope = scope.Route;
            }
        }

        internal ActivityContentApplyResult LastApplyResult => _lastApplyResult;

        internal void ClearLastApplyResult()
        {
            _lastApplyResult = default;
            _hasLastApplyResult = false;
        }

        internal void HandleActivityEntered(ActivityEnteredEvent activityEnteredEvent)
        {
            if (activityEnteredEvent == null)
            {
                return;
            }

            StoreLastApplyResult(ApplyActivityTransition(activityEnteredEvent.PreviousActivity, activityEnteredEvent.Activity, activityEnteredEvent.Source, activityEnteredEvent.Reason));
        }

        internal void HandleActivityExited(ActivityExitedEvent activityExitedEvent)
        {
            if (activityExitedEvent == null || activityExitedEvent.NextActivity != null)
            {
                return;
            }

            StoreLastApplyResult(ApplyActivityTransition(activityExitedEvent.Activity, null, activityExitedEvent.Source, activityExitedEvent.Reason));
        }

        internal ActivityContentApplyResult ApplyActiveActivity(ActivityAsset activeActivity)
        {
            return ApplyActivityTransition(null, activeActivity, "Unknown", "None");
        }

        private ActivityContentApplyResult ApplyActivityTransition(ActivityAsset previousActivity, ActivityAsset activeActivity, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            IReadOnlyList<ActivityLocalVisibilityAdapter> bindings = CollectActivityLocalVisibilityAdapters();
            if (bindings == null || bindings.Count == 0)
            {
                return ActivityContentApplyResult.Empty(activeActivity);
            }

            int bindingCount = 0;
            int activatedCount = 0;
            int deactivatedCount = 0;
            int unchangedCount = 0;
            int missingActivityCount = 0;
            int lifecycleEnterBindingCount = 0;
            int lifecycleEnterReceiverCount = 0;
            int lifecycleEnterFailedReceiverCount = 0;
            int lifecycleExitBindingCount = 0;
            int lifecycleExitReceiverCount = 0;
            int lifecycleExitFailedReceiverCount = 0;
            var observedBindings = new List<string>(MaxObservedBindingsInMessage);
            var warningBindings = new List<string>();
            var activeContentEntries = new List<ActivityContentEntry>();
            int omittedObservationCount = 0;

            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.IsSceneBinding)
                {
                    continue;
                }

                bindingCount++;

                if (binding.Activity == null)
                {
                    missingActivityCount++;
                    AddWarning(warningBindings, binding, "MissingActivityReference");
                    AddObservation(
                        observedBindings,
                        ref omittedObservationCount,
                        binding,
                        "<missing>",
                        "Ignore",
                        "MissingActivityReference");
                    continue;
                }

                bool shouldBeActive = activeActivity != null && ReferenceEquals(binding.Activity, activeActivity);
                bool exitsPreviousActivity = previousActivity != null
                    && !ReferenceEquals(previousActivity, activeActivity)
                    && ReferenceEquals(binding.Activity, previousActivity);
                bool entersActiveActivity = shouldBeActive && !ReferenceEquals(previousActivity, activeActivity);

                if (exitsPreviousActivity)
                {
                    lifecycleExitBindingCount++;
                    DispatchActivityContentExited(
                        binding,
                        previousActivity,
                        activeActivity,
                        resolvedSource,
                        resolvedReason,
                        out int exitReceiverCount,
                        out int exitFailedReceiverCount);
                    lifecycleExitReceiverCount += exitReceiverCount;
                    lifecycleExitFailedReceiverCount += exitFailedReceiverCount;
                }

                bool wasActive = binding.gameObject.activeSelf;
                bool changed = binding.SetContentActive(shouldBeActive);
                string action = ResolveAction(shouldBeActive, wasActive, changed);
                string observationReason = shouldBeActive ? "MatchedActiveActivity" : "DifferentActivity";

                if (shouldBeActive)
                {
                    activeContentEntries.Add(CreateActivityContentEntry(binding, activeActivity, resolvedSource, resolvedReason, action));
                }

                if (entersActiveActivity)
                {
                    lifecycleEnterBindingCount++;
                    DispatchActivityContentEntered(
                        binding,
                        activeActivity,
                        previousActivity,
                        resolvedSource,
                        resolvedReason,
                        out int enterReceiverCount,
                        out int enterFailedReceiverCount);
                    lifecycleEnterReceiverCount += enterReceiverCount;
                    lifecycleEnterFailedReceiverCount += enterFailedReceiverCount;
                }

                if (changed)
                {
                    if (shouldBeActive)
                    {
                        activatedCount++;
                    }
                    else
                    {
                        deactivatedCount++;
                    }
                }
                else
                {
                    unchangedCount++;
                }

                AddObservation(
                    observedBindings,
                    ref omittedObservationCount,
                    binding,
                    binding.Activity.ActivityName,
                    action,
                    observationReason);
            }

            var activityContentSet = ActivityContentSet.FromEntries(activeActivity, activeContentEntries);
            var lifecycleResult = ActivityContentLifecycleResult.ExecutedWith(
                previousActivity,
                activeActivity,
                lifecycleEnterBindingCount,
                lifecycleEnterReceiverCount,
                lifecycleEnterFailedReceiverCount,
                lifecycleExitBindingCount,
                lifecycleExitReceiverCount,
                lifecycleExitFailedReceiverCount,
                resolvedSource,
                resolvedReason);

            return ActivityContentApplyResult.Applied(
                activeActivity,
                bindingCount,
                activatedCount,
                deactivatedCount,
                unchangedCount,
                missingActivityCount,
                activityContentSet,
                lifecycleResult,
                BuildDetailMessage(activeActivity, observedBindings, omittedObservationCount),
                BuildWarningMessage(warningBindings));
        }

        private IReadOnlyList<ActivityLocalVisibilityAdapter> CollectActivityLocalVisibilityAdapters()
        {
            var bindings = new List<ActivityLocalVisibilityAdapter>();
            var scannedSceneKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var route = _discoveryScope.Route != null ? _discoveryScope.Route : _routeScope;

            if (route != null)
            {
                AddSceneKey(scannedSceneKeys, route.PrimaryScenePath, route.PrimarySceneName);
                AddBindings(bindings, SceneScopedComponentQuery.GetComponentsInRoutePrimaryScene<ActivityLocalVisibilityAdapter>(route));
            }

            IReadOnlyList<ActivityContentDiscoveryScene> activityOwnedScenes = _discoveryScope.ActivityOwnedScenes;
            for (int i = 0; i < activityOwnedScenes.Count; i++)
            {
                var scene = activityOwnedScenes[i];
                if (!AddSceneKey(scannedSceneKeys, scene.ScenePath, scene.SceneName))
                {
                    continue;
                }

                AddBindings(
                    bindings,
                    SceneScopedComponentQuery.GetComponentsInLoadedScene<ActivityLocalVisibilityAdapter>(
                        scene.ScenePath,
                        scene.SceneName));
            }

            return bindings;
        }

        private static void AddBindings(
            List<ActivityLocalVisibilityAdapter> bindings,
            IReadOnlyList<ActivityLocalVisibilityAdapter> discovered)
        {
            if (bindings == null || discovered == null || discovered.Count == 0)
            {
                return;
            }

            for (int i = 0; i < discovered.Count; i++)
            {
                if (discovered[i] != null)
                {
                    bindings.Add(discovered[i]);
                }
            }
        }

        private static bool AddSceneKey(HashSet<string> sceneKeys, string scenePath, string sceneName)
        {
            if (sceneKeys == null)
            {
                return false;
            }

            string sceneKey = !string.IsNullOrWhiteSpace(scenePath)
                ? scenePath.Trim()
                : !string.IsNullOrWhiteSpace(sceneName) ? sceneName.Trim() : string.Empty;
            return !string.IsNullOrWhiteSpace(sceneKey) && sceneKeys.Add(sceneKey);
        }

        private static ActivityContentEntry CreateActivityContentEntry(
            ActivityLocalVisibilityAdapter binding,
            ActivityAsset activity,
            string source,
            string reason,
            string action)
        {
            var handle = FrameworkContentHandle.ActivitySceneAuthoredBinding(
                ActivityContentSet.CreateActivityOwnerId(activity),
                activity != null ? activity.ActivityName : string.Empty,
                binding != null ? binding.ObjectName : string.Empty,
                binding != null ? binding.SceneName : string.Empty,
                true,
                source,
                reason,
                $"Activity local visibility adapter action='{FormatValue(action)}'.");

            return new ActivityContentEntry(handle);
        }

        private void StoreLastApplyResult(ActivityContentApplyResult applyResult)
        {
            _lastApplyResult = applyResult;
            _hasLastApplyResult = true;
        }

        private void DispatchActivityContentEntered(
            ActivityLocalVisibilityAdapter binding,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            string source,
            string reason,
            out int receiverCount,
            out int failedReceiverCount)
        {
            var context = ActivityContentLifecycleContext.Entered(activity, previousActivity, binding, source, reason);
            DispatchActivityContentLifecycle(
                binding,
                "Entered",
                activity,
                true,
                receiver => receiver.OnActivityContentEntered(context),
                out receiverCount,
                out failedReceiverCount);
        }

        private void DispatchActivityContentExited(
            ActivityLocalVisibilityAdapter binding,
            ActivityAsset activity,
            ActivityAsset nextActivity,
            string source,
            string reason,
            out int receiverCount,
            out int failedReceiverCount)
        {
            var context = ActivityContentLifecycleContext.Exited(activity, nextActivity, binding, source, reason);
            DispatchActivityContentLifecycle(
                binding,
                "Exited",
                activity,
                false,
                receiver => receiver.OnActivityContentExited(context),
                out receiverCount,
                out failedReceiverCount);
        }

        private void DispatchActivityContentLifecycle(
            ActivityLocalVisibilityAdapter binding,
            string phase,
            ActivityAsset activity,
            bool parentFirst,
            Action<IActivityContentLifecycleReceiver> dispatch,
            out int receiverCount,
            out int failedReceiverCount)
        {
            receiverCount = 0;
            failedReceiverCount = 0;

            if (binding == null || dispatch == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return;
            }

            int start = parentFirst ? 0 : behaviours.Length - 1;
            int end = parentFirst ? behaviours.Length : -1;
            int step = parentFirst ? 1 : -1;

            for (int i = start; i != end; i += step)
            {
                if (behaviours[i] is not IActivityContentLifecycleReceiver receiver)
                {
                    continue;
                }

                receiverCount++;

                try
                {
                    dispatch(receiver);
                }
                catch (Exception exception)
                {
                    failedReceiverCount++;
                    LogActivityContentReceiverException(binding, phase, activity, receiver, exception);
                }
            }
        }

        private void LogActivityContentReceiverException(
            ActivityLocalVisibilityAdapter binding,
            string phase,
            ActivityAsset activity,
            IActivityContentLifecycleReceiver receiver,
            Exception exception)
        {
            string receiverType = receiver != null ? receiver.GetType().FullName : "<missing>";
            string activityName = activity.ToDiagnosticText(x => x.ActivityName);
            string exceptionType = exception != null ? exception.GetType().Name : "<unknown>";
            string exceptionMessage = exception != null ? exception.Message : string.Empty;

            _logger.Error(
                $"Activity Local Visibility Adapter lifecycle receiver failed. phase='{FormatValue(phase)}' activity='{FormatValue(activityName)}' object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' receiver='{FormatValue(receiverType)}' exception='{FormatValue(exceptionType)}' message='{FormatValue(exceptionMessage)}'.");
        }


        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("None");
        }
        private static string ResolveAction(bool shouldBeActive, bool wasActive, bool changed)
        {
            if (changed)
            {
                return shouldBeActive ? "Activate" : "Deactivate";
            }

            return wasActive ? "KeepActive" : "KeepInactive";
        }

        private static void AddObservation(
            List<string> observedBindings,
            ref int omittedObservationCount,
            ActivityLocalVisibilityAdapter binding,
            string assignedActivity,
            string action,
            string reason)
        {
            if (observedBindings.Count >= MaxObservedBindingsInMessage)
            {
                omittedObservationCount++;
                return;
            }

            observedBindings.Add(
                $"object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' assignedActivity='{FormatValue(assignedActivity)}' action='{FormatValue(action)}' reason='{FormatValue(reason)}'");
        }

        private static void AddWarning(List<string> warningBindings, ActivityLocalVisibilityAdapter binding, string reason)
        {
            warningBindings.Add(
                $"object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' reason='{FormatValue(reason)}'");
        }

        private static string BuildDetailMessage(ActivityAsset activeActivity, IReadOnlyList<string> observedBindings, int omittedObservationCount)
        {
            if (observedBindings == null || observedBindings.Count == 0)
            {
                return string.Empty;
            }

            string activeActivityName = activeActivity.ToDiagnosticText(x => x.ActivityName);
            string details = $"Activity Local Visibility Adapter diagnostics. activeActivity='{FormatValue(activeActivityName)}' observations=[{string.Join("; ", observedBindings)}]";
            if (omittedObservationCount > 0)
            {
                details += $" omitted='{omittedObservationCount}'";
            }

            return details + ".";
        }

        private static string BuildWarningMessage(IReadOnlyList<string> warningBindings)
        {
            if (warningBindings == null || warningBindings.Count == 0)
            {
                return string.Empty;
            }

            return $"Activity Local Visibility Adapter warning. warnings=[{string.Join("; ", warningBindings)}].";
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}
