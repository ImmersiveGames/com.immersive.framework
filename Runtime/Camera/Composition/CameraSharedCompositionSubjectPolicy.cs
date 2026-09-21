using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit, composition-selected policy for choosing which currently available Camera
    /// Subjects belong to one Camera Composition. This is never a global Framework assumption —
    /// each CameraSharedComposition instance selects its own policy explicitly. The policy
    /// operates only on Camera Subject availability and, for explicit selection, an ordered
    /// set of CameraSubjectId values. It never reads Player types, so future non-Player
    /// Subjects remain possible without changing this contract.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E explicit Camera Composition Subject selection policy.")]
    public enum CameraSharedCompositionSubjectPolicyKind
    {
        Undefined = 0,

        /// <summary>
        /// Canonical shared-Camera policy: every currently available Subject from the bound
        /// availability context belongs to this Composition. This policy is explicitly selected
        /// per composition instance, not assumed.
        /// </summary>
        AllAvailableSubjects = 10,

        /// <summary>
        /// Exact Camera-domain selection: membership is the intersection of the attached
        /// selection snapshot and current availability. A missing selected Subject is omitted.
        /// This policy never falls back to <see cref="AllAvailableSubjects"/> and never
        /// substitutes a different available Subject.
        /// </summary>
        ExplicitSelection = 20
    }

    /// <summary>
    /// Stateless deterministic evaluation of a CameraSharedCompositionSubjectPolicyKind
    /// against one availability snapshot. Returns a stable ordinal-sorted desired Subject id
    /// set; it never reads Player Slots, joined Players, Player indices or Actor Profiles.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E deterministic Camera Composition Subject selection policy evaluator.")]
    public static class CameraSharedCompositionSubjectPolicy
    {
        public static IReadOnlyList<CameraSubjectId> SelectDesiredSubjects(
            CameraSharedCompositionSubjectPolicyKind policyKind,
            CameraSubjectAvailabilitySnapshot availability) =>
            SelectDesiredSubjects(policyKind, availability, null);

        public static IReadOnlyList<CameraSubjectId> SelectDesiredSubjects(
            CameraSharedCompositionSubjectPolicyKind policyKind,
            CameraSubjectAvailabilitySnapshot availability,
            CameraCompositionSubjectSelectionSnapshot selection)
        {
            if (availability == null)
            {
                return Array.Empty<CameraSubjectId>();
            }

            switch (policyKind)
            {
                case CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects:
                    var ids = new CameraSubjectId[availability.Count];
                    for (int index = 0; index < availability.Count; index++)
                    {
                        ids[index] = availability.Entries[index].Subject.SubjectId;
                    }

                    return ids;

                case CameraSharedCompositionSubjectPolicyKind.ExplicitSelection:
                    return IntersectExplicitSelection(availability, selection);

                case CameraSharedCompositionSubjectPolicyKind.Undefined:
                default:
                    return Array.Empty<CameraSubjectId>();
            }
        }

        private static IReadOnlyList<CameraSubjectId> IntersectExplicitSelection(
            CameraSubjectAvailabilitySnapshot availability,
            CameraCompositionSubjectSelectionSnapshot selection)
        {
            if (selection == null || selection.Count == 0)
            {
                return Array.Empty<CameraSubjectId>();
            }

            var included = new List<CameraSubjectId>(selection.Count);
            var seen = new HashSet<CameraSubjectId>();
            for (int index = 0; index < selection.Count; index++)
            {
                CameraSubjectId subjectId = selection.SubjectIds[index];
                if (!subjectId.IsValid || !seen.Add(subjectId) ||
                    !availability.TryGet(subjectId, out _))
                {
                    continue;
                }

                included.Add(subjectId);
            }

            included.Sort((left, right) => string.CompareOrdinal(left.Value, right.Value));
            return included;
        }
    }
}
