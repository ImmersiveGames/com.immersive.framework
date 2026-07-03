using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public static UnityInputPlayerInputManagerEvidence ValidateLoadedPlayerInputManagerEvidence(string source, string reason)
        {
            PlayerInputManager[] managers = Object.FindObjectsByType<PlayerInputManager>(FindObjectsInactive.Include);
            return ValidatePlayerInputManagerEvidence(managers, source, reason);
        }

        public static UnityInputPlayerInputManagerEvidence ValidatePlayerInputManagerEvidence(
            IEnumerable<PlayerInputManager> managers,
            string source,
            string reason)
        {
            return UnityInputPlayerInputManagerEvidence.FromManagers(managers, source, reason);
        }

        public static UnityInputPlayerInputManagerEvidence ValidatePlayerInputManagerEvidenceCount(
            int managerCount,
            string source,
            string reason)
        {
            return UnityInputPlayerInputManagerEvidence.FromManagerCount(managerCount, source, reason);
        }

        public static UnityInputPlayerInputManagerEvidence ValidateRequiredSessionPlayerInputManagerEvidence(string source, string reason)
        {
            SessionPlayerInputManagerDeclaration[] declarations = Object.FindObjectsByType<SessionPlayerInputManagerDeclaration>(FindObjectsInactive.Include);
            return ValidateRequiredSessionPlayerInputManagerDeclarations(declarations, source, reason);
        }

        public static UnityInputPlayerInputManagerEvidence ValidateRequiredSessionPlayerInputManagerDeclarations(
            IEnumerable<SessionPlayerInputManagerDeclaration> declarations,
            string source,
            string reason)
        {
            int count = 0;
            if (declarations != null)
            {
                foreach (SessionPlayerInputManagerDeclaration declaration in declarations)
                {
                    if (declaration != null && declaration.HasPlayerInputManagerEvidence)
                    {
                        count++;
                    }
                }
            }

            return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(count, source, reason);
        }

        public static UnityInputPlayerInputManagerEvidence ValidateRequiredSessionPlayerInputManagerEvidenceCount(
            int managerCount,
            string source,
            string reason)
        {
            return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(managerCount, source, reason);
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
