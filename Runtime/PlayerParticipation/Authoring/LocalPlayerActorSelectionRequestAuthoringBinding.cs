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
            List<LocalPlayerActorSelectionRequestAuthoring> authorings =
                CollectAuthorings(roots);
            if (authorings.Count == 0)
            {
                return LocalPlayerActorSelectionRequestAuthoringBindingResult.OptionalAbsent(
                    rootCount);
            }

            if (selectionRuntime == null)
            {
                return LocalPlayerActorSelectionRequestAuthoringBindingResult.Rejected(
                    "RejectedMissingPlayerActorSelectionRuntime",
                    $"Local Player Actor Selection Request binding requires a Player Actor selection runtime port. roots='{rootCount}' authorings='{authorings.Count}' bound='0' idempotent='0' rejected='{authorings.Count}'.",
                    rootCount,
                    authorings.Count,
                    0,
                    0,
                    authorings.Count);
            }

            int pendingBound = 0;
            int idempotent = 0;
            int rejected = 0;
            var alreadyBound = new bool[authorings.Count];
            var issues = new List<string>();
            for (int index = 0; index < authorings.Count; index++)
            {
                LocalPlayerActorSelectionRequestAuthoring authoring = authorings[index];
                if (authoring.TryValidatePlayerActorSelectionRuntimeBinding(
                        selectionRuntime,
                        out alreadyBound[index],
                        out string issue))
                {
                    if (alreadyBound[index])
                    {
                        idempotent++;
                    }
                    else
                    {
                        pendingBound++;
                    }

                    continue;
                }

                rejected++;
                string scene = authoring.gameObject.scene.name
                    .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"gameObject='{authoring.gameObject.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (rejected > 0)
            {
                return LocalPlayerActorSelectionRequestAuthoringBindingResult.Rejected(
                    "RejectedAuthoringBinding",
                    $"Local Player Actor Selection Request binding failed before mutation. roots='{rootCount}' authorings='{authorings.Count}' bound='0' pendingBound='{pendingBound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    authorings.Count,
                    0,
                    idempotent,
                    rejected);
            }

            for (int index = 0; index < authorings.Count; index++)
            {
                if (!alreadyBound[index])
                {
                    authorings[index].ApplyPlayerActorSelectionRuntimeBinding(
                        selectionRuntime);
                }
            }

            return LocalPlayerActorSelectionRequestAuthoringBindingResult.Completed(
                rootCount,
                authorings.Count,
                pendingBound,
                idempotent);
        }

        internal static LocalPlayerActorSelectionRequestAuthoringReleaseResult TryRelease(
            IReadOnlyList<GameObject> roots,
            IPlayerActorSelectionRuntimePort selectionRuntime)
        {
            int rootCount = CountUniqueRoots(roots);
            List<LocalPlayerActorSelectionRequestAuthoring> authorings =
                CollectAuthorings(roots);
            if (authorings.Count == 0)
            {
                return LocalPlayerActorSelectionRequestAuthoringReleaseResult.OptionalAbsent(
                    rootCount);
            }

            if (selectionRuntime == null)
            {
                return LocalPlayerActorSelectionRequestAuthoringReleaseResult.Rejected(
                    "RejectedMissingPlayerActorSelectionRuntime",
                    $"Local Player Actor Selection Request release requires the expected runtime port. roots='{rootCount}' authorings='{authorings.Count}' released='0' idempotent='0' rejected='{authorings.Count}'.",
                    rootCount,
                    authorings.Count,
                    0,
                    0,
                    authorings.Count);
            }

            int pendingRelease = 0;
            int idempotent = 0;
            int rejected = 0;
            var alreadyReleased = new bool[authorings.Count];
            var issues = new List<string>();
            for (int index = 0; index < authorings.Count; index++)
            {
                LocalPlayerActorSelectionRequestAuthoring authoring = authorings[index];
                if (authoring.TryValidatePlayerActorSelectionRuntimeRelease(
                        selectionRuntime,
                        out alreadyReleased[index],
                        out string issue))
                {
                    if (alreadyReleased[index])
                    {
                        idempotent++;
                    }
                    else
                    {
                        pendingRelease++;
                    }

                    continue;
                }

                rejected++;
                string scene = authoring.gameObject.scene.name
                    .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"gameObject='{authoring.gameObject.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            if (rejected > 0)
            {
                return LocalPlayerActorSelectionRequestAuthoringReleaseResult.Rejected(
                    "RejectedAuthoringRelease",
                    $"Local Player Actor Selection Request release failed before mutation. roots='{rootCount}' authorings='{authorings.Count}' released='0' pendingRelease='{pendingRelease}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    authorings.Count,
                    0,
                    idempotent,
                    rejected);
            }

            for (int index = 0; index < authorings.Count; index++)
            {
                if (!alreadyReleased[index])
                {
                    authorings[index].ReleasePlayerActorSelectionRuntime(
                        selectionRuntime);
                }
            }

            return LocalPlayerActorSelectionRequestAuthoringReleaseResult.Completed(
                rootCount,
                authorings.Count,
                pendingRelease,
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
