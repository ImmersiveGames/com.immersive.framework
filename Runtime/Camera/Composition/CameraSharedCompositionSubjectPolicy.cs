using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit, composition-selected policy for choosing which currently available Camera
    /// Subjects belong to one Camera View. This is never a global Framework assumption —
    /// each CameraSharedComposition instance selects its own policy explicitly. The policy
    /// operates only on CameraSubjectAvailabilitySnapshot.Entries, never on Player types,
    /// so future non-Player Subjects (vehicles, world objects, spectator targets) remain
    /// possible without changing this contract.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-E explicit Camera View Subject selection policy.")]
    public enum CameraSharedCompositionSubjectPolicyKind
    {
        Undefined = 0,

        /// <summary>
        /// Canonical shared-Camera policy: every currently available Subject from the bound
        /// availability context belongs to this View. This is the only policy CAMERA-026-E
        /// implements; it is explicitly selected per composition instance, not assumed.
        /// </summary>
        AllAvailableSubjects = 10
    }

    /// <summary>
    /// Stateless deterministic evaluation of a CameraSharedCompositionSubjectPolicyKind
    /// against one availability snapshot. Returns a stable ordinal-sorted desired Subject id
    /// set; it never reads Player Slots, joined Players, Player indices or Actor Profiles.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-E deterministic Camera View Subject selection policy evaluator.")]
    public static class CameraSharedCompositionSubjectPolicy
    {
        public static IReadOnlyList<CameraSubjectId> SelectDesiredSubjects(
            CameraSharedCompositionSubjectPolicyKind policyKind,
            CameraSubjectAvailabilitySnapshot availability)
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

                case CameraSharedCompositionSubjectPolicyKind.Undefined:
                default:
                    return Array.Empty<CameraSubjectId>();
            }
        }
    }
}
