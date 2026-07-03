using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using Immersive.Framework.TransitionEffects;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Internal. Transition orchestrator that applies an explicit Unity transition surface.
    /// It owns no Route, Activity or scene lifecycle; it only maps transition phases to effect requests.
    /// Async execution completes only after the visual effect phase has settled.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24D3 ordered Unity Transition surface orchestrator with visual settle boundary.")]
    internal sealed class TransitionEffectOrchestrator : ITransitionOrchestrator
    {
        private readonly ITransitionEffectAdapter[] _adapters;
        private readonly string _surfaceLabel;

        internal TransitionEffectOrchestrator(
            IReadOnlyList<ITransitionEffectAdapter> adapters,
            string surfaceLabel)
        {
            _surfaceLabel = surfaceLabel.NormalizeTextOrFallback("Transition Surface");
            _adapters = CopyAdapters(adapters);
        }

        public TransitionResult Execute(TransitionRequest request)
        {
            return ExecuteInternalAsync(request, useAsyncAdapters: false).GetAwaiter().GetResult();
        }

        public Awaitable<TransitionResult> ExecuteAsync(TransitionRequest request)
        {
            return ExecuteInternalAsync(request, useAsyncAdapters: true);
        }

        private async Awaitable<TransitionResult> ExecuteInternalAsync(TransitionRequest request, bool useAsyncAdapters)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Transition surface orchestrator requires a valid request.", nameof(request));
            }

            var effectPhase = ResolveEffectPhase(request.Phase);
            bool visibleState = request.Phase == TransitionPhase.OperationOpened;
            var effectKind = TransitionEffectKind.Fade;
            string effectId = BuildEffectId(request, visibleState);
            var effectRequest = TransitionEffectRequest.Required(
                effectId,
                effectKind,
                request.OperationId,
                request.Kind,
                effectPhase,
                request.Source,
                request.Reason);
            var effectPlan = TransitionEffectPlan.Create(
                request.OperationId,
                request.Kind,
                request.Source,
                request.Reason,
                new[] { effectRequest });
            var evaluation = TransitionEffectAuthoringPolicy.Evaluate(effectPlan, _adapters);
            if (!evaluation.IsAllowed)
            {
                return BuildFailureResult(
                    request,
                    effectKind,
                    evaluation,
                    "FailedRequiredUnitySurfaceMissing",
                    visibleState ? "visible" : "hidden");
            }

            List<ITransitionEffectAdapter> matchingAdapters = CollectSupportingAdapters(effectKind);
            if (matchingAdapters.Count == 0)
            {
                return BuildFailureResult(
                    request,
                    effectKind,
                    evaluation,
                    "FailedRequiredUnitySurfaceMissing",
                    visibleState ? "visible" : "hidden");
            }

            var issues = new List<string>();
            var adapterEvidence = new List<TransitionEffectAdapterEvidence>(matchingAdapters.Count);
            int blockingIssueCount = 0;
            int warningIssueCount = 0;

            for (int i = 0; i < matchingAdapters.Count; i++)
            {
                var adapter = matchingAdapters[i];
                var result = useAsyncAdapters && adapter is IAsyncTransitionEffectAdapter asyncAdapter
                    ? await asyncAdapter.ExecuteAsync(effectRequest)
                    : adapter.Execute(effectRequest);
                adapterEvidence.Add(TransitionEffectAdapterEvidence.FromResult(adapter.AdapterName, result));

                if (result.BlocksTransition)
                {
                    blockingIssueCount++;
                }
                else if (result.CompletedWithWarnings)
                {
                    warningIssueCount++;
                }

                if (result.HasIssues)
                {
                    for (int issueIndex = 0; issueIndex < result.Issues.Count; issueIndex++)
                    {
                        string issueText = result.Issues[issueIndex];
                        if (!string.IsNullOrWhiteSpace(issueText))
                        {
                            issues.Add($"{adapter.AdapterName}: {issueText.Trim()}");
                        }
                    }
                }
            }

            if (blockingIssueCount > 0)
            {
                return TransitionResult.FailedResult(
                    request.OperationId,
                    request.Kind,
                    request.Source,
                    request.Reason,
                    BuildFailureMessage(visibleState, "Unity surface adapter execution failed."),
                    new[]
                    {
                        TransitionStep.Failed(
                            0,
                            request.Phase,
                            BuildStepLabel(request, visibleState),
                            "Transition surface adapter execution failed.")
                    },
                    issues,
                    effectKind,
                    TransitionEffectStatus.Failed,
                    matchingAdapters.Count,
                    "UnitySurface",
                    blockingIssueCount)
                    .WithEffectAdapterEvidence(adapterEvidence);
            }

            var effectStatus = warningIssueCount > 0
                ? TransitionEffectStatus.CompletedWithWarnings
                : TransitionEffectStatus.Succeeded;
            string transitionMessage = warningIssueCount > 0
                ? "CompletedWithUnitySurfaceWarnings"
                : "SucceededWithUnitySurface";
            var step = warningIssueCount > 0
                ? TransitionStep.Observed(
                    0,
                    request.Phase,
                    BuildStepLabel(request, visibleState),
                    BuildStepMessage(visibleState))
                : TransitionStep.Succeeded(
                    0,
                    request.Phase,
                    BuildStepLabel(request, visibleState),
                    BuildStepMessage(visibleState));

            if (warningIssueCount > 0)
            {
                return TransitionResult.CompletedWithWarningsResult(
                    request.OperationId,
                    request.Kind,
                    request.Source,
                    request.Reason,
                    transitionMessage,
                    new[] { step },
                    issues,
                    effectKind,
                    effectStatus,
                    matchingAdapters.Count,
                    "UnitySurface",
                    blockingIssueCount)
                    .WithEffectAdapterEvidence(adapterEvidence);
            }

            return TransitionResult.SucceededResult(
                request.OperationId,
                request.Kind,
                request.Source,
                request.Reason,
                transitionMessage,
                new[] { step },
                effectKind,
                effectStatus,
                matchingAdapters.Count,
                "UnitySurface",
                blockingIssueCount)
                .WithEffectAdapterEvidence(adapterEvidence);
        }

        private static TransitionPhase ResolveEffectPhase(TransitionPhase transitionPhase)
        {
            switch (transitionPhase)
            {
                case TransitionPhase.OperationOpened:
                    return TransitionPhase.GateBlockApplied;
                case TransitionPhase.OperationClosed:
                    return TransitionPhase.GateBlockReleased;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(transitionPhase),
                        transitionPhase,
                        "Transition surface orchestrator only supports operation opened and operation closed phases.");
            }
        }

        private TransitionResult BuildFailureResult(
            TransitionRequest request,
            TransitionEffectKind effectKind,
            TransitionEffectPolicyEvaluation evaluation,
            string message,
            string stateLabel)
        {
            var issues = new List<string>(evaluation.IssueCount + 1);
            for (int i = 0; i < evaluation.Issues.Count; i++)
            {
                issues.Add(evaluation.Issues[i].ToDiagnosticString());
            }

            issues.Add($"Surface '{_surfaceLabel}' could not apply the Transition surface while requested for {stateLabel}.");
            var adapterEvidence = new[]
            {
                TransitionEffectAdapterEvidence.MissingAdapter(
                    _surfaceLabel,
                    issues.Count,
                    Math.Max(1, evaluation.BlockingIssueCount),
                    message)
            };

            return TransitionResult.FailedResult(
                request.OperationId,
                request.Kind,
                request.Source,
                request.Reason,
                message,
                new[]
                {
                    TransitionStep.Failed(
                        0,
                        request.Phase,
                        BuildStepLabel(request, stateLabel == "visible"),
                        $"Transition surface '{_surfaceLabel}' could not be applied.")
                },
                issues,
                effectKind,
                TransitionEffectStatus.MissingAdapter,
                0,
                "RequiredSurfaceMissing",
                Math.Max(1, evaluation.BlockingIssueCount))
                .WithEffectAdapterEvidence(adapterEvidence);
        }

        private List<ITransitionEffectAdapter> CollectSupportingAdapters(TransitionEffectKind effectKind)
        {
            var supportingAdapters = new List<ITransitionEffectAdapter>();
            for (int i = 0; i < _adapters.Length; i++)
            {
                var adapter = _adapters[i];
                if (adapter != null && adapter.Supports(effectKind))
                {
                    supportingAdapters.Add(adapter);
                }
            }

            return supportingAdapters;
        }

        private static string BuildEffectId(TransitionRequest request, bool visibleState)
        {
            string phaseText = visibleState ? "before" : "after";
            string scopeText = request.Scope.ToString().ToLowerInvariant();
            string kindText = request.Kind.ToString().ToLowerInvariant();
            return $"framework.transition-surface.{scopeText}.{kindText}.{phaseText}.fade";
        }

        private static string BuildStepLabel(TransitionRequest request, bool visibleState)
        {
            string phaseText = visibleState ? "visible" : "hidden";
            return $"{request.Scope.ToString().ToLowerInvariant()}-{phaseText}";
        }

        private static string BuildStepMessage(bool visibleState)
        {
            return visibleState
                ? "Transition surface visual fade-in settled."
                : "Transition surface visual fade-out settled.";
        }

        private static string BuildFailureMessage(bool visibleState, string message)
        {
            string phaseText = visibleState ? "visible" : "hidden";
            return $"{message} Requested state='{phaseText}'.";
        }

        private static ITransitionEffectAdapter[] CopyAdapters(IReadOnlyList<ITransitionEffectAdapter> adapters)
        {
            if (adapters == null || adapters.Count == 0)
            {
                return Array.Empty<ITransitionEffectAdapter>();
            }

            var copy = new ITransitionEffectAdapter[adapters.Count];
            for (int i = 0; i < adapters.Count; i++)
            {
                copy[i] = adapters[i];
            }

            return copy;
        }
    }
}
