using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Explicit policy helper for required/optional effect adapter guardrails.
    /// The caller supplies adapters directly. This policy does not discover scene objects, own a registry, execute adapters or create fallbacks.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19E required/optional Transition Effect policy; no discovery registry or fallback.")]
    public static class TransitionEffectAuthoringPolicy
    {
        public const string PolicySource = "F19E.TransitionEffectAuthoringPolicy";
        public const string RequiredAdapterMissingIssue = "required-transition-effect-adapter-missing";
        public const string OptionalAdapterMissingIssue = "optional-transition-effect-adapter-missing";
        public const string DuplicateEffectIdIssue = "duplicate-transition-effect-id";

        public static TransitionEffectPolicyEvaluation Evaluate(
            TransitionEffectPlan plan,
            IReadOnlyList<ITransitionEffectAdapter> adapters)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition Effect authoring policy requires a valid effect plan.", nameof(plan));
            }

            int adapterCount = CountAdapters(adapters);
            int matchedRequests = 0;
            int requiredMissing = 0;
            int optionalMissing = 0;
            int duplicateIds = 0;
            var issues = new List<TransitionEffectPolicyIssue>();
            IReadOnlyList<TransitionEffectRequest> requests = plan.Requests;

            for (int i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                if (IsDuplicateEffectId(requests, i))
                {
                    duplicateIds++;
                    issues.Add(TransitionEffectPolicyIssue.DuplicateEffectId(request));
                }

                if (HasAdapterFor(request.EffectKind, adapters))
                {
                    matchedRequests++;
                    continue;
                }

                if (request.IsRequired)
                {
                    requiredMissing++;
                    issues.Add(TransitionEffectPolicyIssue.RequiredAdapterMissing(request));
                    continue;
                }

                optionalMissing++;
                issues.Add(TransitionEffectPolicyIssue.OptionalAdapterMissing(request));
            }

            return new TransitionEffectPolicyEvaluation(
                plan,
                PolicySource,
                adapterCount,
                matchedRequests,
                requiredMissing,
                optionalMissing,
                duplicateIds,
                issues);
        }

        public static bool HasAdapterFor(
            TransitionEffectKind effectKind,
            IReadOnlyList<ITransitionEffectAdapter> adapters)
        {
            if (effectKind == TransitionEffectKind.Unknown || adapters == null || adapters.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < adapters.Count; i++)
            {
                var adapter = adapters[i];
                if (adapter != null && adapter.Supports(effectKind))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountAdapters(IReadOnlyList<ITransitionEffectAdapter> adapters)
        {
            if (adapters == null || adapters.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < adapters.Count; i++)
            {
                if (adapters[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsDuplicateEffectId(IReadOnlyList<TransitionEffectRequest> requests, int index)
        {
            var current = requests[index];
            for (int i = 0; i < index; i++)
            {
                if (requests[i].EffectId == current.EffectId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
