#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    internal static class FrameworkQaCanvasBinding
    {
        internal static FrameworkQaCanvasBindingResult TryBind(
            IReadOnlyList<GameObject> roots,
            IFrameworkRuntimeDiagnosticsPort diagnosticsRuntime)
        {
            int rootCount =
                CountUniqueRoots(roots);
            if (diagnosticsRuntime == null)
            {
                return FrameworkQaCanvasBindingResult.Rejected(
                    "RejectedMissingFrameworkRuntimeDiagnostics",
                    $"Framework QA Canvas binding requires a framework runtime diagnostics port. roots='{rootCount}' canvases='0' bound='0' idempotent='0' rejected='0'.",
                    rootCount,
                    0,
                    0,
                    0,
                    0);
            }

            List<FrameworkQaCanvas> canvases =
                CollectCanvases(roots);
            if (canvases.Count == 0)
            {
                return FrameworkQaCanvasBindingResult.OptionalAbsent(
                    rootCount);
            }

            int bound = 0;
            int idempotent = 0;
            int rejected = 0;
            var issues =
                new List<string>();

            for (int index = 0;
                 index < canvases.Count;
                 index++)
            {
                FrameworkQaCanvas canvas =
                    canvases[index];
                bool wasBound =
                    canvas.HasFrameworkRuntimeDiagnosticsBinding;

                if (canvas.TryBindFrameworkRuntimeDiagnostics(
                        diagnosticsRuntime,
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
                string scene =
                    canvas.gameObject.scene.name
                        .NormalizeTextOrFallback("<unknown>");
                issues.Add(
                    $"canvas='{canvas.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'.");
            }

            return rejected > 0
                ? FrameworkQaCanvasBindingResult.Rejected(
                    "RejectedCanvasBinding",
                    $"Framework QA Canvas binding failed. roots='{rootCount}' canvases='{canvases.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ", issues)}",
                    rootCount,
                    canvases.Count,
                    bound,
                    idempotent,
                    rejected)
                : FrameworkQaCanvasBindingResult.Completed(
                    rootCount,
                    canvases.Count,
                    bound,
                    idempotent);
        }

        private static List<FrameworkQaCanvas> CollectCanvases(
            IReadOnlyList<GameObject> roots)
        {
            var result =
                new List<FrameworkQaCanvas>();
            var seenRoots =
                new HashSet<GameObject>();
            var seenCanvases =
                new HashSet<FrameworkQaCanvas>();

            if (roots == null)
            {
                return result;
            }

            for (int rootIndex = 0;
                 rootIndex < roots.Count;
                 rootIndex++)
            {
                GameObject root =
                    roots[rootIndex];
                if (root == null ||
                    !seenRoots.Add(root))
                {
                    continue;
                }

                FrameworkQaCanvas[] candidates =
                    root.GetComponentsInChildren<
                        FrameworkQaCanvas>(true);

                for (int canvasIndex = 0;
                     canvasIndex < candidates.Length;
                     canvasIndex++)
                {
                    FrameworkQaCanvas candidate =
                        candidates[canvasIndex];
                    if (candidate != null &&
                        seenCanvases.Add(candidate))
                    {
                        result.Add(candidate);
                    }
                }
            }

            return result;
        }

        private static int CountUniqueRoots(
            IReadOnlyList<GameObject> roots)
        {
            var result =
                new HashSet<GameObject>();

            if (roots == null)
            {
                return 0;
            }

            for (int index = 0;
                 index < roots.Count;
                 index++)
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
#endif
