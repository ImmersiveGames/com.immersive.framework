using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    internal static class UnityContentAnchorMaterializationBridgeBinding
    {
        internal static UnityContentAnchorMaterializationBridgeBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IContentAnchorMaterializationRuntimePort materializationRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (materializationRuntime == null)
            {
                return UnityContentAnchorMaterializationBridgeBindingResult.Rejected(
                    "RejectedMissingContentAnchorMaterializationRuntime",
                    $"Unity Content Anchor Materialization Bridge binding requires a Content Anchor materialization runtime port. roots='{rootCount}' bridges='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<UnityContentAnchorMaterializationBridge> bridges = CollectBridges(roots);
            if (bridges.Count == 0)
            {
                return UnityContentAnchorMaterializationBridgeBindingResult.OptionalAbsent(rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < bridges.Count; index++)
            {
                UnityContentAnchorMaterializationBridge bridge = bridges[index];
                bool wasBound = bridge.HasContentAnchorMaterializationRuntimeBinding;
                if (bridge.TryBindContentAnchorMaterializationRuntime(
                        materializationRuntime,
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
                string scene = bridge.gameObject.scene.name
                    .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"bridge='{bridge.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejected > 0
                ? UnityContentAnchorMaterializationBridgeBindingResult.Rejected(
                    "RejectedBridgeBinding",
                    $"Unity Content Anchor Materialization Bridge binding failed. roots='{rootCount}' bridges='{bridges.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    bridges.Count,
                    bound,
                    idempotent,
                    rejected)
                : UnityContentAnchorMaterializationBridgeBindingResult.Completed(
                    rootCount,
                    bridges.Count,
                    bound,
                    idempotent);
        }

        private static List<UnityContentAnchorMaterializationBridge> CollectBridges(
            IReadOnlyList<GameObject> roots)
        {
            var result = new List<UnityContentAnchorMaterializationBridge>();
            var seenRoots = new HashSet<GameObject>();
            var seenBridges = new HashSet<UnityContentAnchorMaterializationBridge>();
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

                UnityContentAnchorMaterializationBridge[] candidates =
                    root.GetComponentsInChildren<UnityContentAnchorMaterializationBridge>(true);
                for (int bridgeIndex = 0;
                     bridgeIndex < candidates.Length;
                     bridgeIndex++)
                {
                    UnityContentAnchorMaterializationBridge candidate = candidates[bridgeIndex];
                    if (candidate != null && seenBridges.Add(candidate))
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
