using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityContentRuntime
    {
        /// <summary>
        /// Prepared transition that freezes contribution and visibility discovery once.
        /// </summary>
        internal sealed class ActivityContentTransitionContext
        {
            internal ActivityContentTransitionContext(ActivityAsset previousActivity, ActivityAsset activeActivity, IReadOnlyList<ActivityContentContribution> contributions, IReadOnlyList<ActivityVisibilityRule> visibilityRules, string source, string reason)
            {
                PreviousActivity = previousActivity;
                ActiveActivity = activeActivity;
                Contributions = contributions ?? Array.Empty<ActivityContentContribution>();
                VisibilityRules = visibilityRules ?? Array.Empty<ActivityVisibilityRule>();
                Source = source;
                Reason = reason;
                Observations = new List<string>(MaxObservedBindingsInMessage);
                Warnings = new List<string>();
                RequiredInvalidContributionDiagnostics = new List<string>();
                ActiveContentEntries = new List<ActivityContentEntry>();
            }

            internal ActivityAsset PreviousActivity { get; }
            internal ActivityAsset ActiveActivity { get; }
            internal IReadOnlyList<ActivityContentContribution> Contributions { get; }
            internal IReadOnlyList<ActivityVisibilityRule> VisibilityRules { get; }
            internal string Source { get; }
            internal string Reason { get; }
            internal List<string> Observations { get; }
            internal List<string> Warnings { get; }
            internal List<string> RequiredInvalidContributionDiagnostics { get; }
            internal List<ActivityContentEntry> ActiveContentEntries { get; }
            internal int ContributionCount { get; set; }
            internal int VisibilityRuleCount { get; set; }
            internal int ActivatedCount { get; set; }
            internal int DeactivatedCount { get; set; }
            internal int UnchangedCount { get; set; }
            internal int MissingActivityCount { get; set; }
            internal int InvalidBindingCount { get; set; }
            internal int RequiredInvalidBindingCount { get; set; }
            internal int OptionalInvalidBindingCount { get; set; }
            internal int EnterBindingCount { get; set; }
            internal int EnterReceiverCount { get; set; }
            internal int EnterFailedReceiverCount { get; set; }
            internal int ExitBindingCount { get; set; }
            internal int ExitReceiverCount { get; set; }
            internal int ExitFailedReceiverCount { get; set; }
            internal int OmittedObservationCount;
            internal bool ExitExecuted { get; set; }
            internal bool EnterExecuted { get; set; }
            internal int ComponentCount => ContributionCount + VisibilityRuleCount;
        }

        internal ActivityContentTransitionContext PrepareActivityContentTransition(ActivityAsset previousActivity, ActivityAsset activeActivity, string source, string reason)
        {
            var context = new ActivityContentTransitionContext(previousActivity, activeActivity,
                CollectActivityContentContributions(previousActivity, activeActivity),
                CollectActivityVisibilityRules(previousActivity, activeActivity),
                NormalizeSource(source), NormalizeReason(reason));
            ValidateContributions(context);
            ValidateVisibilityRules(context);
            return context;
        }

        internal void ExitPreviousActivityContent(ActivityContentTransitionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.ExitExecuted) throw new InvalidOperationException("Activity content transition Exit phase already executed.");

            if (context.PreviousActivity != null && !ReferenceEquals(context.PreviousActivity, context.ActiveActivity))
            {
                for (int index = 0; index < context.Contributions.Count; index++)
                {
                    ActivityContentContribution contribution = context.Contributions[index];
                    if (contribution == null || !contribution.IsSceneBinding || !contribution.MatchesActivity(context.PreviousActivity) || !contribution.TryValidate(out _)) continue;
                    context.ExitBindingCount++;
                    DispatchActivityContentExited(contribution, context.PreviousActivity, context.ActiveActivity, context.Source, context.Reason, out int receiverCount, out int failedReceiverCount);
                    context.ExitReceiverCount += receiverCount;
                    context.ExitFailedReceiverCount += failedReceiverCount;
                    AddContributionObservation(context.Observations, ref context.OmittedObservationCount, contribution, "Exit", "MatchedActivityOwner");
                }
            }

            context.ExitExecuted = true;
        }

        internal void EnterTargetActivityContent(ActivityContentTransitionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.ExitExecuted) throw new InvalidOperationException("Activity content transition Enter phase requires completed Exit phase.");
            if (context.EnterExecuted) throw new InvalidOperationException("Activity content transition Enter phase already executed.");

            ApplyVisibilityRules(context);
            if (context.ActiveActivity != null && !ReferenceEquals(context.PreviousActivity, context.ActiveActivity))
            {
                for (int index = 0; index < context.Contributions.Count; index++)
                {
                    ActivityContentContribution contribution = context.Contributions[index];
                    if (contribution == null || !contribution.IsSceneBinding || !contribution.MatchesActivity(context.ActiveActivity) || !contribution.TryValidate(out _)) continue;
                    context.ActiveContentEntries.Add(CreateActivityContentEntry(contribution, context.ActiveActivity, context.Source, context.Reason));
                    context.EnterBindingCount++;
                    DispatchActivityContentEntered(contribution, context.ActiveActivity, context.PreviousActivity, context.Source, context.Reason, out int receiverCount, out int failedReceiverCount);
                    context.EnterReceiverCount += receiverCount;
                    context.EnterFailedReceiverCount += failedReceiverCount;
                    AddContributionObservation(context.Observations, ref context.OmittedObservationCount, contribution, "Enter", "MatchedActivityOwner");
                }
            }

            context.EnterExecuted = true;
        }

        internal ActivityContentApplyResult CompleteActivityContentTransition(ActivityContentTransitionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.ExitExecuted || !context.EnterExecuted) throw new InvalidOperationException("Activity content transition can complete only after Exit and Enter phases.");
            if (context.ComponentCount <= 0)
            {
                ActivityContentApplyResult empty = ActivityContentApplyResult.Empty(context.ActiveActivity);
                StoreLastApplyResult(empty);
                return empty;
            }

            ActivityContentSet contentSet = ActivityContentSet.FromEntries(context.ActiveActivity, context.ActiveContentEntries);
            ActivityContentLifecycleResult lifecycleResult = ActivityContentLifecycleResult.ExecutedWith(context.PreviousActivity, context.ActiveActivity, context.EnterBindingCount, context.EnterReceiverCount, context.EnterFailedReceiverCount, context.ExitBindingCount, context.ExitReceiverCount, context.ExitFailedReceiverCount, context.Source, context.Reason);
            ActivityContentApplyResult result = ActivityContentApplyResult.Applied(context.ActiveActivity, context.ComponentCount, context.ActivatedCount, context.DeactivatedCount, context.UnchangedCount, context.MissingActivityCount, context.InvalidBindingCount, context.RequiredInvalidBindingCount, context.OptionalInvalidBindingCount, contentSet, lifecycleResult, BuildDetailMessage(context.ActiveActivity, context.Observations, context.OmittedObservationCount), BuildWarningMessage(context.Warnings));
            StoreLastApplyResult(result);
            return result;
        }

        internal string BuildRequiredInvalidBindingDiagnostic(ActivityContentTransitionContext context)
        {
            return context == null || context.RequiredInvalidBindingCount <= 0
                ? string.Empty
                : $"Required ActivityContentContribution configuration is invalid. requiredInvalidContribution='{context.RequiredInvalidBindingCount}' diagnostics=[{string.Join("; ", context.RequiredInvalidContributionDiagnostics)}].";
        }

        private static void ValidateContributions(ActivityContentTransitionContext context)
        {
            for (int index = 0; index < context.Contributions.Count; index++)
            {
                ActivityContentContribution contribution = context.Contributions[index];
                if (contribution == null || !contribution.IsSceneBinding) continue;
                context.ContributionCount++;
                if (contribution.TryValidate(out string reason)) continue;
                context.InvalidBindingCount++;
                AddContributionWarning(context.Warnings, contribution, reason);
                if (contribution.Requiredness == FrameworkContentRequiredness.Optional) context.OptionalInvalidBindingCount++;
                else
                {
                    context.RequiredInvalidBindingCount++;
                    AddContributionWarning(context.RequiredInvalidContributionDiagnostics, contribution, reason);
                }
                AddContributionObservation(context.Observations, ref context.OmittedObservationCount, contribution, "Ignore", reason);
            }
        }

        private static void ValidateVisibilityRules(ActivityContentTransitionContext context)
        {
            for (int index = 0; index < context.VisibilityRules.Count; index++)
            {
                ActivityVisibilityRule rule = context.VisibilityRules[index];
                if (rule == null || !rule.IsSceneBinding) continue;
                context.VisibilityRuleCount++;
                ActivityVisibilityEvaluation evaluation = rule.EvaluateVisibility(context.ActiveActivity);
                if (evaluation.IsValid) continue;
                context.InvalidBindingCount++;
                AddVisibilityWarning(context.Warnings, rule, evaluation.DiagnosticReason);
                AddVisibilityObservation(context.Observations, ref context.OmittedObservationCount, rule, "Ignore", evaluation.DiagnosticReason);
            }
        }

        private static void ApplyVisibilityRules(ActivityContentTransitionContext context)
        {
            for (int index = 0; index < context.VisibilityRules.Count; index++)
            {
                ActivityVisibilityRule rule = context.VisibilityRules[index];
                if (rule == null || !rule.IsSceneBinding) continue;
                ActivityVisibilityEvaluation evaluation = rule.EvaluateVisibility(context.ActiveActivity);
                if (!evaluation.IsValid) continue;
                bool wasActive = rule.gameObject.activeSelf;
                bool changed = rule.SetContentActive(evaluation.DesiredVisibility);
                if (!changed) context.UnchangedCount++;
                else if (evaluation.DesiredVisibility) context.ActivatedCount++;
                else context.DeactivatedCount++;
                AddVisibilityObservation(context.Observations, ref context.OmittedObservationCount, rule, ResolveAction(evaluation.DesiredVisibility, wasActive, changed), evaluation.DiagnosticReason);
            }
        }
    }
}
