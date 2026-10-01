using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Reset;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Authoring boundary for a semantic set of Resettable members.
    /// It does not register a subject, own content, or execute Reset.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Reset Composition")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 RESET-035-D ResetComposition authoring boundary.")]
    public sealed class ResetComposition : MonoBehaviour
    {
        [Tooltip("Descendants collects members in hierarchy order and stops at nested ResetComposition boundaries. Explicit Members uses only the typed references below.")]
        [SerializeField] private ResetCompositionMemberMode memberMode = ResetCompositionMemberMode.Descendants;

        [Tooltip("Used only by Explicit Members. Null entries are ignored with diagnostics; duplicate references are deduplicated.")]
        [SerializeField] private List<Resettable> explicitMembers = new List<Resettable>();

        [Tooltip("Default membership for members whose Resettable membership is FollowOwner. A local Activity or Route value takes precedence.")]
        [SerializeField] private ResetMembership membership = ResetMembership.FollowOwner;

        public ResetCompositionMemberMode MemberMode => memberMode;

        public ResetMembership Membership => membership;

        internal IReadOnlyList<Resettable> ExplicitMembers => explicitMembers;
    }
}
