using System;
using System.Collections.Generic;
using Immersive.Framework.Reset;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>Internal typed resolution for ResetComposition authoring. It does not register or execute subjects.</summary>
    internal static class ResetCompositionResolver
    {
        internal readonly struct Resolution
        {
            internal Resolution(bool succeeded, IReadOnlyList<Resettable> members, IReadOnlyList<string> diagnostics)
            {
                Succeeded = succeeded;
                Members = members ?? Array.Empty<Resettable>();
                Diagnostics = diagnostics ?? Array.Empty<string>();
                Diagnostic = string.Join(" ", Diagnostics);
            }

            internal bool Succeeded { get; }

            internal IReadOnlyList<Resettable> Members { get; }

            internal IReadOnlyList<string> Diagnostics { get; }

            internal string Diagnostic { get; }
        }

        internal static Resolution Resolve(ResetComposition composition)
        {
            if (composition == null)
            {
                return Failure("reset-composition-invalid: composition reference is null.");
            }

            if (!Enum.IsDefined(typeof(ResetCompositionMemberMode), composition.MemberMode))
            {
                return Failure($"reset-composition-mode-invalid: mode='{composition.MemberMode}'.");
            }

            if (!Enum.IsDefined(typeof(ResetMembership), composition.Membership))
            {
                return Failure($"reset-composition-membership-invalid: membership='{composition.Membership}'.");
            }

            var diagnostics = new List<string>();
            List<Resettable> members = composition.MemberMode == ResetCompositionMemberMode.Descendants
                ? CollectDescendants(composition, diagnostics)
                : CollectExplicitMembers(composition, diagnostics);

            if (members.Count == 0)
            {
                diagnostics.Add($"reset-composition-empty: mode='{composition.MemberMode}' object='{composition.name}'.");
            }

            return new Resolution(true, members, diagnostics);
        }

        internal static bool TryResolveMemberships(
            IReadOnlyList<GameObject> roots,
            IReadOnlyList<Resettable> registeredResettables,
            out Dictionary<Resettable, ResetMembership> membershipByResettable,
            out string diagnostic)
        {
            membershipByResettable = new Dictionary<Resettable, ResetMembership>();
            diagnostic = string.Empty;

            var diagnostics = new List<string>();
            var registeredSet = new HashSet<Resettable>();
            if (registeredResettables != null)
            {
                for (int index = 0; index < registeredResettables.Count; index++)
                {
                    Resettable resettable = registeredResettables[index];
                    if (resettable != null && registeredSet.Add(resettable))
                    {
                        membershipByResettable.Add(resettable, resettable.Membership);
                    }
                }
            }

            var compositionMemberships = new Dictionary<Resettable, ResetMembership>();
            foreach (ResetComposition composition in CollectCompositions(roots))
            {
                Resolution resolution = Resolve(composition);
                diagnostics.AddRange(resolution.Diagnostics);
                if (!resolution.Succeeded)
                {
                    diagnostic = string.Join(" ", diagnostics);
                    return false;
                }

                for (int memberIndex = 0; memberIndex < resolution.Members.Count; memberIndex++)
                {
                    Resettable member = resolution.Members[memberIndex];
                    if (member == null)
                    {
                        continue;
                    }

                    if (!registeredSet.Contains(member))
                    {
                        diagnostics.Add(
                            $"reset-composition-member-outside-owner-roots: composition='{composition.name}' member='{member.name}'. Explicit members must be part of the materialized owner roots registered by the current transaction.");
                        diagnostic = string.Join(" ", diagnostics);
                        return false;
                    }

                    ResetMembership resolvedMembership = ResolveMembership(composition.Membership, member.Membership);
                    if (compositionMemberships.TryGetValue(member, out ResetMembership previousMembership)
                        && previousMembership != resolvedMembership)
                    {
                        diagnostics.Add(
                            $"reset-composition-membership-conflict: member='{member.name}' memberships='{previousMembership},{resolvedMembership}'. Local Resettable membership wins; composition defaults must agree for a shared member.");
                        diagnostic = string.Join(" ", diagnostics);
                        return false;
                    }

                    compositionMemberships[member] = resolvedMembership;
                    membershipByResettable[member] = resolvedMembership;
                }
            }

            diagnostic = string.Join(" ", diagnostics);
            return true;
        }

        internal static ResetMembership ResolveMembership(
            ResetMembership compositionMembership,
            ResetMembership resettableMembership)
        {
            return resettableMembership == ResetMembership.FollowOwner
                ? compositionMembership
                : resettableMembership;
        }

        private static List<Resettable> CollectDescendants(
            ResetComposition composition,
            List<string> diagnostics)
        {
            var members = new List<Resettable>();
            var seen = new HashSet<Resettable>();
            VisitDescendants(composition.transform, true, composition, members, seen, diagnostics);
            return members;
        }

        private static void VisitDescendants(
            Transform node,
            bool isBoundaryRoot,
            ResetComposition boundary,
            List<Resettable> members,
            HashSet<Resettable> seen,
            List<string> diagnostics)
        {
            if (node == null)
            {
                return;
            }

            if (!isBoundaryRoot)
            {
                ResetComposition nestedComposition = node.GetComponent<ResetComposition>();
                if (nestedComposition != null && nestedComposition != boundary)
                {
                    diagnostics.Add(
                        $"reset-composition-nested-boundary-skipped: outer='{boundary.name}' nested='{nestedComposition.name}'. The nested composition resolves its own members.");
                    return;
                }
            }

            Resettable resettable = node.GetComponent<Resettable>();
            if (resettable != null && seen.Add(resettable))
            {
                members.Add(resettable);
            }

            for (int childIndex = 0; childIndex < node.childCount; childIndex++)
            {
                VisitDescendants(node.GetChild(childIndex), false, boundary, members, seen, diagnostics);
            }
        }

        private static List<Resettable> CollectExplicitMembers(ResetComposition composition, List<string> diagnostics)
        {
            var members = new List<Resettable>();
            var seen = new HashSet<Resettable>();
            IReadOnlyList<Resettable> authoredMembers = composition.ExplicitMembers;
            if (authoredMembers == null)
            {
                diagnostics.Add($"reset-composition-explicit-list-null: composition='{composition.name}'.");
                return members;
            }

            for (int index = 0; index < authoredMembers.Count; index++)
            {
                Resettable member = authoredMembers[index];
                if (member == null)
                {
                    diagnostics.Add($"reset-composition-explicit-member-null: composition='{composition.name}' index='{index}'.");
                    continue;
                }

                if (!seen.Add(member))
                {
                    diagnostics.Add($"reset-composition-member-duplicate: composition='{composition.name}' index='{index}' member='{member.name}'.");
                    continue;
                }

                members.Add(member);
            }

            return members;
        }

        private static IReadOnlyList<ResetComposition> CollectCompositions(IReadOnlyList<GameObject> roots)
        {
            var compositions = new List<ResetComposition>();
            var seenRoots = new HashSet<GameObject>();
            var seenCompositions = new HashSet<ResetComposition>();
            if (roots == null)
            {
                return compositions;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null || !seenRoots.Add(root))
                {
                    continue;
                }

                VisitCompositions(root.transform, compositions, seenCompositions);
            }

            return compositions;
        }

        private static void VisitCompositions(
            Transform node,
            List<ResetComposition> compositions,
            HashSet<ResetComposition> seen)
        {
            if (node == null)
            {
                return;
            }

            ResetComposition composition = node.GetComponent<ResetComposition>();
            if (composition != null && seen.Add(composition))
            {
                compositions.Add(composition);
            }

            for (int childIndex = 0; childIndex < node.childCount; childIndex++)
            {
                VisitCompositions(node.GetChild(childIndex), compositions, seen);
            }
        }

        private static Resolution Failure(string diagnostic)
        {
            return new Resolution(false, Array.Empty<Resettable>(), new[] { diagnostic });
        }
    }
}
