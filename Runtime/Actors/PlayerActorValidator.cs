using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Validator for PlayerActor declarations.
    /// It produces diagnostics only; it does not own actors, input, spawning or movement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A PlayerActor declaration validator.")]
    public static class PlayerActorValidator
    {
        public static PlayerActorSet ValidateLoadedSceneDeclarations(string source, string reason)
        {
            PlayerActorDeclaration[] declarations = Object.FindObjectsByType<PlayerActorDeclaration>(FindObjectsInactive.Include);
            return ValidateDeclarations(declarations, source, reason);
        }

        public static PlayerActorSet ValidateDeclarations(
            IEnumerable<PlayerActorDeclaration> declarations,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerActorValidator));
            var descriptors = new List<PlayerActorDescriptor>();
            var issues = new List<PlayerActorSetIssue>();

            if (declarations != null)
            {
                foreach (PlayerActorDeclaration declaration in declarations)
                {
                    if (declaration == null)
                    {
                        issues.Add(PlayerActorSetIssue.BlockingIssue(
                            PlayerActorSetIssueKind.InvalidDeclaration,
                            string.Empty,
                            normalizedSource,
                            "PlayerActor declaration reference is null."));
                        continue;
                    }

                    if (declaration.TryCreateDescriptor(normalizedSource, out PlayerActorDescriptor descriptor, out PlayerActorSetIssue issue))
                    {
                        descriptors.Add(descriptor);
                        continue;
                    }

                    issues.Add(issue);
                }
            }

            return PlayerActorSet.FromDescriptors(descriptors, issues, normalizedSource, reason);
        }
    }
}
