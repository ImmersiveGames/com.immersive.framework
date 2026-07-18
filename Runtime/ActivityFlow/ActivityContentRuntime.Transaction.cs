using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityContentRuntime
    {
        /// <summary>
        /// Prepared scene-content transition. Discovery is frozen once so Exit and Enter
        /// operate on the same ordered binding collection.
        /// </summary>
        internal sealed class ActivityContentTransitionContext
        {
            internal ActivityContentTransitionContext(
                ActivityAsset previousActivity,
                ActivityAsset activeActivity,
                IReadOnlyList<ActivityLocalVisibilityAdapter> bindings,
                string source,
                string reason)
            {
                PreviousActivity = previousActivity;
                ActiveActivity = activeActivity;
                Bindings = bindings ?? Array.Empty<ActivityLocalVisibilityAdapter>();
                Source = source;
                Reason = reason;
                ObservedBindings = new List<string>(MaxObservedBindingsInMessage);
                WarningBindings = new List<string>();
                ActiveContentEntries = new List<ActivityContentEntry>();
            }

            internal ActivityAsset PreviousActivity { get; }

            internal ActivityAsset ActiveActivity { get; }

            internal IReadOnlyList<ActivityLocalVisibilityAdapter> Bindings { get; }

            internal string Source { get; }

            internal string Reason { get; }

            internal List<string> ObservedBindings { get; }

            internal List<string> WarningBindings { get; }

            internal List<ActivityContentEntry> ActiveContentEntries { get; }

            internal int BindingCount { get; set; }

            internal int ActivatedCount { get; set; }

            internal int DeactivatedCount { get; set; }

            internal int UnchangedCount { get; set; }

            internal int MissingActivityCount { get; set; }

            internal int EnterBindingCount { get; set; }

            internal int EnterReceiverCount { get; set; }

            internal int EnterFailedReceiverCount { get; set; }

            internal int ExitBindingCount { get; set; }

            internal int ExitReceiverCount { get; set; }

            internal int ExitFailedReceiverCount { get; set; }

            internal int OmittedObservationCount;

            internal bool ExitExecuted { get; set; }

            internal bool EnterExecuted { get; set; }
        }

        internal ActivityContentTransitionContext PrepareActivityContentTransition(
            ActivityAsset previousActivity,
            ActivityAsset activeActivity,
            string source,
            string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);
            IReadOnlyList<ActivityLocalVisibilityAdapter> bindings =
                CollectActivityLocalVisibilityAdapters();
            var context = new ActivityContentTransitionContext(
                previousActivity,
                activeActivity,
                bindings,
                resolvedSource,
                resolvedReason);

            for (int index = 0; index < context.Bindings.Count; index++)
            {
                ActivityLocalVisibilityAdapter binding = context.Bindings[index];
                if (binding == null || !binding.IsSceneBinding)
                {
                    continue;
                }

                context.BindingCount++;
                if (binding.Activity != null)
                {
                    continue;
                }

                context.MissingActivityCount++;
                AddWarning(
                    context.WarningBindings,
                    binding,
                    "MissingActivityReference");
                AddObservation(
                    context.ObservedBindings,
                    ref context.OmittedObservationCount,
                    binding,
                    "<missing>",
                    "Ignore",
                    "MissingActivityReference");
            }

            return context;
        }

        internal void ExitPreviousActivityContent(
            ActivityContentTransitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ExitExecuted)
            {
                throw new InvalidOperationException(
                    "Activity content transition Exit phase already executed.");
            }

            for (int index = 0; index < context.Bindings.Count; index++)
            {
                ActivityLocalVisibilityAdapter binding = context.Bindings[index];
                if (binding == null || !binding.IsSceneBinding ||
                    binding.Activity == null)
                {
                    continue;
                }

                bool exitsPrevious = context.PreviousActivity != null &&
                    !ReferenceEquals(
                        context.PreviousActivity,
                        context.ActiveActivity) &&
                    ReferenceEquals(
                        binding.Activity,
                        context.PreviousActivity);

                if (!exitsPrevious)
                {
                    continue;
                }

                context.ExitBindingCount++;
                DispatchActivityContentExited(
                    binding,
                    context.PreviousActivity,
                    context.ActiveActivity,
                    context.Source,
                    context.Reason,
                    out int receiverCount,
                    out int failedReceiverCount);
                context.ExitReceiverCount += receiverCount;
                context.ExitFailedReceiverCount += failedReceiverCount;

                bool wasActive = binding.gameObject.activeSelf;
                bool changed = binding.SetContentActive(false);
                RecordVisibilityChange(
                    context,
                    shouldBeActive: false,
                    wasActive,
                    changed);
                AddObservation(
                    context.ObservedBindings,
                    ref context.OmittedObservationCount,
                    binding,
                    binding.Activity.ActivityName,
                    ResolveAction(false, wasActive, changed),
                    "PreviousActivityExit");
            }

            context.ExitExecuted = true;
        }

        internal void EnterTargetActivityContent(
            ActivityContentTransitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.ExitExecuted)
            {
                throw new InvalidOperationException(
                    "Activity content transition Enter phase requires completed Exit phase.");
            }

            if (context.EnterExecuted)
            {
                throw new InvalidOperationException(
                    "Activity content transition Enter phase already executed.");
            }

            for (int index = 0; index < context.Bindings.Count; index++)
            {
                ActivityLocalVisibilityAdapter binding = context.Bindings[index];
                if (binding == null || !binding.IsSceneBinding ||
                    binding.Activity == null)
                {
                    continue;
                }

                bool wasPreviousActivity = context.PreviousActivity != null &&
                    !ReferenceEquals(
                        context.PreviousActivity,
                        context.ActiveActivity) &&
                    ReferenceEquals(
                        binding.Activity,
                        context.PreviousActivity);
                if (wasPreviousActivity)
                {
                    continue;
                }

                bool shouldBeActive = context.ActiveActivity != null &&
                    ReferenceEquals(
                        binding.Activity,
                        context.ActiveActivity);
                bool wasActive = binding.gameObject.activeSelf;
                bool changed = binding.SetContentActive(shouldBeActive);
                RecordVisibilityChange(
                    context,
                    shouldBeActive,
                    wasActive,
                    changed);
                string action = ResolveAction(
                    shouldBeActive,
                    wasActive,
                    changed);

                if (shouldBeActive)
                {
                    context.ActiveContentEntries.Add(
                        CreateActivityContentEntry(
                            binding,
                            context.ActiveActivity,
                            context.Source,
                            context.Reason,
                            action));
                    context.EnterBindingCount++;
                    DispatchActivityContentEntered(
                        binding,
                        context.ActiveActivity,
                        context.PreviousActivity,
                        context.Source,
                        context.Reason,
                        out int receiverCount,
                        out int failedReceiverCount);
                    context.EnterReceiverCount += receiverCount;
                    context.EnterFailedReceiverCount += failedReceiverCount;
                }

                AddObservation(
                    context.ObservedBindings,
                    ref context.OmittedObservationCount,
                    binding,
                    binding.Activity.ActivityName,
                    action,
                    shouldBeActive
                        ? "TargetActivityEnter"
                        : "DifferentActivity");
            }

            context.EnterExecuted = true;
        }

        internal ActivityContentApplyResult CompleteActivityContentTransition(
            ActivityContentTransitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.ExitExecuted || !context.EnterExecuted)
            {
                throw new InvalidOperationException(
                    "Activity content transition can complete only after Exit and Enter phases.");
            }

            if (context.BindingCount <= 0)
            {
                ActivityContentApplyResult empty =
                    ActivityContentApplyResult.Empty(context.ActiveActivity);
                StoreLastApplyResult(empty);
                return empty;
            }

            ActivityContentSet contentSet = ActivityContentSet.FromEntries(
                context.ActiveActivity,
                context.ActiveContentEntries);
            ActivityContentLifecycleResult lifecycleResult =
                ActivityContentLifecycleResult.ExecutedWith(
                    context.PreviousActivity,
                    context.ActiveActivity,
                    context.EnterBindingCount,
                    context.EnterReceiverCount,
                    context.EnterFailedReceiverCount,
                    context.ExitBindingCount,
                    context.ExitReceiverCount,
                    context.ExitFailedReceiverCount,
                    context.Source,
                    context.Reason);
            ActivityContentApplyResult result = ActivityContentApplyResult.Applied(
                context.ActiveActivity,
                context.BindingCount,
                context.ActivatedCount,
                context.DeactivatedCount,
                context.UnchangedCount,
                context.MissingActivityCount,
                contentSet,
                lifecycleResult,
                BuildDetailMessage(
                    context.ActiveActivity,
                    context.ObservedBindings,
                    context.OmittedObservationCount),
                BuildWarningMessage(context.WarningBindings));
            StoreLastApplyResult(result);
            return result;
        }

        private static void RecordVisibilityChange(
            ActivityContentTransitionContext context,
            bool shouldBeActive,
            bool wasActive,
            bool changed)
        {
            if (!changed)
            {
                context.UnchangedCount++;
                return;
            }

            if (shouldBeActive)
            {
                context.ActivatedCount++;
                return;
            }

            context.DeactivatedCount++;
        }
    }
}
