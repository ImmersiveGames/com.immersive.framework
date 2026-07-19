using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    internal static class UnityResetSubjectAdapterBinding
    {
        internal static UnityResetSubjectAdapterBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IResetRegistrationRuntimePort resetRegistrationRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (resetRegistrationRuntime == null)
            {
                return UnityResetSubjectAdapterBindingResult.Rejected(
                    "RejectedMissingResetRegistrationRuntime",
                    $"Unity Reset Subject Adapter binding requires a Reset registration runtime port. roots='{rootCount}' adapters='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<UnityResetSubjectAdapter> adapters = CollectAdapters(roots);
            if (adapters.Count == 0)
            {
                return UnityResetSubjectAdapterBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < adapters.Count; index++)
            {
                UnityResetSubjectAdapter adapter = adapters[index];
                bool wasBound = adapter.HasResetRegistrationRuntimeBinding;
                if (adapter.TryBindResetRegistrationRuntime(
                        resetRegistrationRuntime,
                        out string issue))
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
                string scene = adapter.gameObject.scene.name
                    .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"adapter='{adapter.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejected > 0
                ? UnityResetSubjectAdapterBindingResult.Rejected(
                    "RejectedAdapterBinding",
                    $"Unity Reset Subject Adapter binding failed. roots='{rootCount}' adapters='{adapters.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    adapters.Count,
                    bound,
                    idempotent,
                    rejected)
                : UnityResetSubjectAdapterBindingResult.Completed(
                    rootCount,
                    adapters.Count,
                    bound,
                    idempotent);
        }

        private static List<UnityResetSubjectAdapter> CollectAdapters(
            IReadOnlyList<GameObject> roots)
        {
            var result = new List<UnityResetSubjectAdapter>();
            var seenRoots = new HashSet<GameObject>();
            var seenAdapters = new HashSet<UnityResetSubjectAdapter>();
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

                UnityResetSubjectAdapter[] candidates =
                    root.GetComponentsInChildren<UnityResetSubjectAdapter>(true);
                for (int adapterIndex = 0;
                     adapterIndex < candidates.Length;
                     adapterIndex++)
                {
                    UnityResetSubjectAdapter candidate = candidates[adapterIndex];
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
