using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Validates passive Pause Activity binding declarations from explicit evidence only.
    /// It never performs global discovery and includes inactive declarations when Activity roots
    /// are supplied, because Activity authoring is serialized intent rather than enabled behavior.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P2.1A explicit-root validator for passive Pause Activity binding authoring.")]
    public static class PauseActivityBindingAuthoringValidator
    {
        public static PauseActivityBindingIntentResolution ResolveDeclarations(
            IEnumerable<PauseActivityBindingAuthoring> declarations,
            string source)
        {
            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(PauseActivityBindingAuthoringValidator));
            var collected = new List<PauseActivityBindingAuthoring>();

            if (declarations != null)
            {
                foreach (PauseActivityBindingAuthoring declaration in declarations)
                {
                    if (declaration == null)
                    {
                        return PauseActivityBindingIntentResolution.Invalid(
                            collected.Count,
                            normalizedSource,
                            "Explicit Pause Activity Binding declaration evidence contains null.");
                    }

                    if (!collected.Contains(declaration))
                    {
                        collected.Add(declaration);
                    }
                }
            }

            return ResolveCollected(collected, normalizedSource);
        }

        public static PauseActivityBindingIntentResolution ResolveFromRoots(
            IReadOnlyList<GameObject> roots,
            string source)
        {
            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(PauseActivityBindingAuthoringValidator));
            if (roots == null)
            {
                return PauseActivityBindingIntentResolution.Invalid(
                    0,
                    normalizedSource,
                    "Activity root evidence is required. Use an empty root collection to declare intent-absent.");
            }

            var collected = new List<PauseActivityBindingAuthoring>();
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    return PauseActivityBindingIntentResolution.Invalid(
                        collected.Count,
                        normalizedSource,
                        "Activity root evidence contains null.");
                }

                PauseActivityBindingAuthoring[] declarations =
                    root.GetComponentsInChildren<PauseActivityBindingAuthoring>(true);
                for (int declarationIndex = 0;
                    declarationIndex < declarations.Length;
                    declarationIndex++)
                {
                    PauseActivityBindingAuthoring declaration = declarations[declarationIndex];
                    if (declaration != null && !collected.Contains(declaration))
                    {
                        collected.Add(declaration);
                    }
                }
            }

            return ResolveCollected(collected, normalizedSource);
        }

        private static PauseActivityBindingIntentResolution ResolveCollected(
            IReadOnlyList<PauseActivityBindingAuthoring> declarations,
            string source)
        {
            int count = declarations?.Count ?? 0;
            if (count == 0)
            {
                return PauseActivityBindingIntentResolution.Absent(source);
            }

            if (count > 1)
            {
                return PauseActivityBindingIntentResolution.UnsupportedMultiple(count, source);
            }

            PauseActivityBindingAuthoring declaration = declarations[0];
            if (declaration == null)
            {
                return PauseActivityBindingIntentResolution.Invalid(
                    count,
                    source,
                    "Pause Activity Binding declaration is null.");
            }

            if (!declaration.TryCreateIntent(
                    out PauseActivityBindingIntent intent,
                    out string diagnostic))
            {
                return PauseActivityBindingIntentResolution.Invalid(count, source, diagnostic);
            }

            return PauseActivityBindingIntentResolution.Created(intent, source);
        }
    }
}
