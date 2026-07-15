using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Validator for authored Unity Input target declarations.
    /// It creates diagnostic snapshots only; it does not read input or apply InputMode behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target declaration validator.")]
    public static class UnityInputTargetValidator
    {
        public static UnityInputTargetSet ValidateLoadedSceneDeclarations(string source, string reason)
        {
            UnityInputTargetDeclaration[] declarations = Object.FindObjectsByType<UnityInputTargetDeclaration>(FindObjectsInactive.Include);
            return ValidateDeclarations(declarations, source, reason);
        }

        public static UnityInputTargetSet ValidateDeclarations(
            IEnumerable<UnityInputTargetDeclaration> declarations,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputTargetValidator));
            var descriptors = new List<UnityInputTargetDescriptor>();
            var issues = new List<UnityInputTargetSetIssue>();

            if (declarations != null)
            {
                foreach (UnityInputTargetDeclaration declaration in declarations)
                {
                    if (declaration == null)
                    {
                        issues.Add(UnityInputTargetSetIssue.BlockingIssue(
                            UnityInputTargetSetIssueKind.InvalidDeclaration,
                            UnityInputTargetRole.Unknown,
                            string.Empty,
                            normalizedSource,
                            "Unity Input target declaration reference is null."));
                        continue;
                    }

                    if (declaration.TryCreateDescriptor(normalizedSource, out UnityInputTargetDescriptor descriptor, out UnityInputTargetSetIssue issue))
                    {
                        descriptors.Add(descriptor);
                        continue;
                    }

                    issues.Add(issue);
                }
            }

            return UnityInputTargetSet.FromDescriptors(descriptors, null, issues, normalizedSource, reason);
        }
    }
}
