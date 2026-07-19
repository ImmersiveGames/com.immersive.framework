using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Gate;
using UnityEngine;

namespace Immersive.Framework.UnityInput
{
    internal static class UnityPlayerInputGateAdapterBinding
    {
        internal static UnityPlayerInputGateAdapterBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IInputGateRuntimePort inputGateRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (inputGateRuntime == null)
            {
                return UnityPlayerInputGateAdapterBindingResult.Rejected(
                    "RejectedMissingInputGateRuntime",
                    $"Unity PlayerInput Gate Adapter binding requires an Input Gate runtime port. roots='{rootCount}' adapters='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<UnityPlayerInputGateAdapter> adapters = CollectAdapters(roots);
            if (adapters.Count == 0)
            {
                return UnityPlayerInputGateAdapterBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < adapters.Count; index++)
            {
                UnityPlayerInputGateAdapter adapter = adapters[index];
                bool wasBound = adapter.HasInputGateRuntimeBinding;
                if (adapter.TryBindInputGateRuntime(inputGateRuntime, out string issue))
                {
                    if (wasBound)
                    {
                        idempotent++;
                    }
                    else
                    {
                        bound++;
                    }

                    continue;
                }

                rejected++;
                string scene = adapter.gameObject.scene.name.NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"adapter='{adapter.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejected > 0
                ? UnityPlayerInputGateAdapterBindingResult.Rejected(
                    "RejectedAdapterBinding",
                    $"Unity PlayerInput Gate Adapter binding failed. roots='{rootCount}' adapters='{adapters.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    adapters.Count,
                    bound,
                    idempotent,
                    rejected)
                : UnityPlayerInputGateAdapterBindingResult.Completed(
                    rootCount,
                    adapters.Count,
                    bound,
                    idempotent);
        }

        private static List<UnityPlayerInputGateAdapter> CollectAdapters(
            IReadOnlyList<GameObject> roots)
        {
            var result = new List<UnityPlayerInputGateAdapter>();
            var seenRoots = new HashSet<GameObject>();
            var seenAdapters = new HashSet<UnityPlayerInputGateAdapter>();
            if (roots == null)
            {
                return result;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root))
                {
                    continue;
                }

                UnityPlayerInputGateAdapter[] candidates =
                    root.GetComponentsInChildren<UnityPlayerInputGateAdapter>(true);
                for (int adapterIndex = 0; adapterIndex < candidates.Length; adapterIndex++)
                {
                    UnityPlayerInputGateAdapter candidate = candidates[adapterIndex];
                    if (candidate != null && seenAdapters.Add(candidate))
                    {
                        result.Add(candidate);
                    }
                }
            }

            return result;
        }

        private static int CountUniqueRoots(IReadOnlyList<GameObject> roots)
        {
            var result = new HashSet<GameObject>();
            if (roots == null)
            {
                return 0;
            }

            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index] != null)
                {
                    result.Add(roots[index]);
                }
            }

            return result.Count;
        }
    }
}
