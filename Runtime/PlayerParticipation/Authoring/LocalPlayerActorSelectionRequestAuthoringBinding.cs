using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal static class LocalPlayerActorSelectionRequestAuthoringBinding
    {
        internal static LocalPlayerActorSelectionRequestAuthoringBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IPlayerActorSelectionRuntimePort selectionRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            if (selectionRuntime == null)
            {
                return LocalPlayerActorSelectionRequestAuthoringBindingResult.Rejected(
                    "RejectedMissingPlayerActorSelectionRuntime",
                    $"Local Player Actor Selection Request binding requires a Player Actor selection runtime port. roots='{rootCount}' authorings='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<LocalPlayerActorSelectionRequestAuthoring> authorings =
                CollectAuthorings(roots);
            if (authorings.Count == 0)
            {
                return LocalPlayerActorSelectionRequestAuthoringBindingResult.OptionalAbsent(
                    rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues = new List<string>();
            for (int index = 0; index < authorings.Count; index++)
            {
                LocalPlayerActorSelectionRequestAuthoring authoring = authorings[index];
                bool wasBound = authoring.HasPlayerActorSelectionRuntimeBinding;
                if (authoring.TryBindPlayerActorSelectionRuntime(
                        selectionRuntime,
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
                string scene = authoring.gameObject.scene.name
                    .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"authoring='{authoring.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejected > 0
                ? LocalPlayerActorSelectionRequestAuthoringBindingResult.Rejected(
                    "RejectedAuthoringBinding",
                    $"Local Player Actor Selection Request binding failed. roots='{rootCount}' authorings='{authorings.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    authorings.Count,
                    bound,
                    idempotent,
                    rejected)
                : LocalPlayerActorSelectionRequestAuthoringBindingResult.Completed(
                    rootCount,
                    authorings.Count,
                    bound,
                    idempotent);
        }

        private static List<LocalPlayerActorSelectionRequestAuthoring> CollectAuthorings(
            IReadOnlyList<GameObject> roots)
        {
            var result = new List<LocalPlayerActorSelectionRequestAuthoring>();
            var seenRoots = new HashSet<GameObject>();
            var seenAuthorings =
                new HashSet<LocalPlayerActorSelectionRequestAuthoring>();
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

                LocalPlayerActorSelectionRequestAuthoring[] candidates =
                    root.GetComponentsInChildren<
                        LocalPlayerActorSelectionRequestAuthoring>(true);
                for (int authoringIndex = 0;
                     authoringIndex < candidates.Length;
                     authoringIndex++)
                {
                    LocalPlayerActorSelectionRequestAuthoring candidate =
                        candidates[authoringIndex];
                    if (candidate != null && seenAuthorings.Add(candidate))
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
