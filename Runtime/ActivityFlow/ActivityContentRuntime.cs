using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for applying Activity visibility rules and dispatching Activity content contribution lifecycle in loaded scenes.
    /// It does not load scenes, spawn actors, or own Activity identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed partial class ActivityContentRuntime
    {
        private const int MaxObservedBindingsInMessage = 8;
        private readonly FrameworkLogger _logger = FrameworkLogger.Create<ActivityContentRuntime>();
        private ActivityContentApplyResult _lastApplyResult;
        private bool _hasLastApplyResult;
        private ActivityContentDiscoveryScope _discoveryScope;

        internal bool HasLastApplyResult => _hasLastApplyResult;
        internal void SetDiscoveryScope(ActivityContentDiscoveryScope scope) => _discoveryScope = scope;
        internal ActivityContentApplyResult LastApplyResult => _lastApplyResult;

        internal void ClearLastApplyResult()
        {
            _lastApplyResult = default;
            _hasLastApplyResult = false;
        }

        internal void HandleActivityEntered(ActivityEnteredEvent activityEnteredEvent)
        {
            if (activityEnteredEvent != null)
            {
                StoreLastApplyResult(ApplyTransitionUsingRule(activityEnteredEvent.PreviousActivity, activityEnteredEvent.Activity, activityEnteredEvent.Source, activityEnteredEvent.Reason));
            }
        }

        internal void HandleActivityExited(ActivityExitedEvent activityExitedEvent)
        {
            if (activityExitedEvent != null && activityExitedEvent.NextActivity == null)
            {
                StoreLastApplyResult(ApplyTransitionUsingRule(activityExitedEvent.Activity, null, activityExitedEvent.Source, activityExitedEvent.Reason));
            }
        }

        internal ActivityContentApplyResult ApplyActiveActivity(ActivityAsset activeActivity) =>
            ApplyTransitionUsingRule(null, activeActivity, "Unknown", "None");

        private ActivityContentApplyResult ApplyTransitionUsingRule(ActivityAsset previousActivity, ActivityAsset activeActivity, string source, string reason)
        {
            ActivityContentTransitionContext context = PrepareActivityContentTransition(previousActivity, activeActivity, source, reason);
            ExitPreviousActivityContent(context);
            EnterTargetActivityContent(context);
            return CompleteActivityContentTransition(context);
        }

        private IReadOnlyList<ActivityContentContribution> CollectActivityContentContributions(ActivityAsset previousActivity, ActivityAsset activeActivity) =>
            CollectComponents<ActivityContentContribution>(previousActivity, activeActivity);

        private IReadOnlyList<ActivityVisibilityRule> CollectActivityVisibilityRules(ActivityAsset previousActivity, ActivityAsset activeActivity) =>
            CollectComponents<ActivityVisibilityRule>(previousActivity, activeActivity);

        private IReadOnlyList<T> CollectComponents<T>(ActivityAsset previousActivity, ActivityAsset activeActivity) where T : Component
        {
            var components = new List<T>();
            var seen = new HashSet<T>();
            AddComponents(components, seen, SceneCompositionComponentQuery.GetComponents<T>(_discoveryScope, previousActivity));
            if (previousActivity == null || !ReferenceEquals(previousActivity, activeActivity))
            {
                AddComponents(components, seen, SceneCompositionComponentQuery.GetComponents<T>(_discoveryScope, activeActivity));
            }

            return components;
        }

        private static void AddComponents<T>(List<T> components, HashSet<T> seen, IReadOnlyList<T> discovered) where T : Component
        {
            if (components == null || seen == null || discovered == null)
            {
                return;
            }

            for (int i = 0; i < discovered.Count; i++)
            {
                if (discovered[i] != null && seen.Add(discovered[i]))
                {
                    components.Add(discovered[i]);
                }
            }
        }

        private static ActivityContentEntry CreateActivityContentEntry(ActivityContentContribution contribution, ActivityAsset activity, string source, string reason)
        {
            var handle = FrameworkContentHandle.ActivitySceneAuthoredBinding(
                ActivityContentSet.CreateActivityOwnerId(activity), activity != null ? activity.ActivityName : string.Empty,
                contribution != null ? contribution.ObjectName : string.Empty, contribution != null ? contribution.SceneName : string.Empty,
                contribution != null ? contribution.Requiredness : FrameworkContentRequiredness.Required,
                true, source, reason, "ActivityContentContribution.");
            return new ActivityContentEntry(handle);
        }

        private void StoreLastApplyResult(ActivityContentApplyResult applyResult)
        {
            _lastApplyResult = applyResult;
            _hasLastApplyResult = true;
        }

        private void DispatchActivityContentEntered(ActivityContentContribution contribution, ActivityAsset activity, ActivityAsset previousActivity, string source, string reason, out int receiverCount, out int failedReceiverCount)
        {
            var context = ActivityContentLifecycleContext.Entered(activity, previousActivity, contribution, source, reason);
            DispatchActivityContentLifecycle(contribution, "Entered", activity, true, receiver => receiver.OnActivityContentEntered(context), out receiverCount, out failedReceiverCount);
        }

        private void DispatchActivityContentExited(ActivityContentContribution contribution, ActivityAsset activity, ActivityAsset nextActivity, string source, string reason, out int receiverCount, out int failedReceiverCount)
        {
            var context = ActivityContentLifecycleContext.Exited(activity, nextActivity, contribution, source, reason);
            DispatchActivityContentLifecycle(contribution, "Exited", activity, false, receiver => receiver.OnActivityContentExited(context), out receiverCount, out failedReceiverCount);
        }

        private void DispatchActivityContentLifecycle(ActivityContentContribution contribution, string phase, ActivityAsset activity, bool parentFirst, Action<IActivityContentLifecycleReceiver> dispatch, out int receiverCount, out int failedReceiverCount)
        {
            receiverCount = 0;
            failedReceiverCount = 0;
            if (contribution == null || dispatch == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = contribution.GetComponentsInChildren<MonoBehaviour>(true);
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
                try { dispatch(receiver); }
                catch (Exception exception)
                {
                    failedReceiverCount++;
                    LogActivityContentReceiverException(contribution, phase, activity, receiver, exception);
                }
            }
        }

        private void LogActivityContentReceiverException(ActivityContentContribution contribution, string phase, ActivityAsset activity, IActivityContentLifecycleReceiver receiver, Exception exception)
        {
            string receiverType = receiver != null ? receiver.GetType().FullName : "<missing>";
            string activityName = activity.ToDiagnosticText(x => x.ActivityName);
            string exceptionType = exception != null ? exception.GetType().Name : "<unknown>";
            _logger.Error($"ActivityContentContribution lifecycle receiver failed. phase='{FormatValue(phase)}' activity='{FormatValue(activityName)}' object='{FormatValue(contribution.ObjectName)}' scene='{FormatValue(contribution.SceneName)}' receiver='{FormatValue(receiverType)}' exception='{FormatValue(exceptionType)}' message='{FormatValue(exception != null ? exception.Message : string.Empty)}'.");
        }

        private static string NormalizeSource(string source) => source.NormalizeTextOrFallback("Unknown");
        private static string NormalizeReason(string reason) => reason.NormalizeTextOrFallback("None");

        private static string ResolveAction(bool shouldBeActive, bool wasActive, bool changed)
        {
            if (changed) return shouldBeActive ? "Activate" : "Deactivate";
            return wasActive ? "KeepActive" : "KeepInactive";
        }

        private static void AddContributionObservation(List<string> observations, ref int omittedCount, ActivityContentContribution contribution, string action, string reason) =>
            AddObservation(observations, ref omittedCount, contribution != null ? contribution.ObjectName : string.Empty, contribution != null ? contribution.SceneName : string.Empty, contribution != null && contribution.Activity != null ? contribution.Activity.ActivityName : string.Empty, "contribution:" + action, reason);

        private static void AddVisibilityObservation(List<string> observations, ref int omittedCount, ActivityVisibilityRule rule, string action, string reason) =>
            AddObservation(observations, ref omittedCount, rule != null ? rule.ObjectName : string.Empty, rule != null ? rule.SceneName : string.Empty, FormatListedActivities(rule), "visibility:" + action, reason);

        private static void AddObservation(List<string> observations, ref int omittedCount, string objectName, string sceneName, string target, string action, string reason)
        {
            if (observations.Count >= MaxObservedBindingsInMessage) { omittedCount++; return; }
            observations.Add($"object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' target='{FormatValue(target)}' action='{FormatValue(action)}' reason='{FormatValue(reason)}'");
        }

        private static void AddContributionWarning(List<string> warnings, ActivityContentContribution contribution, string reason) =>
            warnings.Add($"kind='contribution' object='{FormatValue(contribution.ObjectName)}' scene='{FormatValue(contribution.SceneName)}' localContentId='{FormatValue(contribution.LocalContentIdText)}' requiredness='{contribution.Requiredness}' activity='{FormatValue(contribution.Activity != null ? contribution.Activity.ActivityName : string.Empty)}' reason='{FormatValue(reason)}'");

        private static void AddVisibilityWarning(List<string> warnings, ActivityVisibilityRule rule, string reason) =>
            warnings.Add($"kind='visibilityRule' object='{FormatValue(rule.ObjectName)}' scene='{FormatValue(rule.SceneName)}' listedActivities='{FormatValue(FormatListedActivities(rule))}' reason='{FormatValue(reason)}'");

        private static string FormatListedActivities(ActivityVisibilityRule rule)
        {
            if (rule == null || rule.Activities == null || rule.Activities.Count == 0) return "<none>";
            var names = new List<string>(rule.Activities.Count);
            for (int index = 0; index < rule.Activities.Count; index++) names.Add(rule.Activities[index] != null ? rule.Activities[index].ActivityName : "<null>");
            return string.Join(",", names);
        }

        private static string BuildDetailMessage(ActivityAsset activeActivity, List<string> observations, int omittedObservationCount)
        {
            if (observations == null || observations.Count == 0) return string.Empty;
            string details = $"Activity content diagnostics. activeActivity='{FormatValue(activeActivity.ToDiagnosticText(x => x.ActivityName))}' observations=[{string.Join("; ", observations)}]";
            return omittedObservationCount > 0 ? details + $" omitted='{omittedObservationCount}'" : details;
        }

        private static string BuildWarningMessage(List<string> warnings) => warnings == null || warnings.Count == 0 ? string.Empty : $"Activity content warning. warnings=[{string.Join("; ", warnings)}].";
        private static string FormatValue(string value) => string.IsNullOrWhiteSpace(value) ? "<empty>" : value.Replace("'", "\\'");
    }
}
