using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. IF-ADR-035 executable Reset unit.
    /// A Resettable aggregates the local Reset capabilities found inside its own hierarchy boundary
    /// (<see cref="UnityResetParticipantBehaviour"/> and <see cref="IUnityResettable"/> components).
    /// It never authors ownership, scope or a textual Reset subject id and never registers itself:
    /// Route/Activity transactions register it with their target <see cref="RuntimeContentOwner"/>.
    /// A nested Resettable is a collection boundary; capabilities below it belong to the nested unit.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Resettable")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-035 RESET-035-B Resettable registered by owner-aware Route/Activity transactions.")]
    public sealed class Resettable : MonoBehaviour
    {
        [Tooltip("Optional diagnostics label. Never used as runtime identity.")]
        [SerializeField] private string displayName;

        [Tooltip("Reset target membership. FollowOwner derives Activity or Route membership from the content owner.")]
        [SerializeField] private ResetMembership membership = ResetMembership.FollowOwner;

        private ResetRegistrationHandle _subjectHandle;
        private ResetSubject _subject;
        private int _registeredCapabilityCount;

        /// <summary>True while an owner-aware transaction registration is active for this Resettable.</summary>
        public bool IsRegistered => _subjectHandle.IsSubject;

        /// <summary>Advanced/diagnostic evidence: runtime-generated subject id. Not authored identity.</summary>
        public ResetSubjectId RuntimeSubjectId => _subject.SubjectId;

        /// <summary>Advanced/diagnostic evidence: owner supplied by the registering transaction.</summary>
        public RuntimeContentOwner Owner => _subject.Owner;

        /// <summary>Semantic Reset membership policy, independent from the content lifetime owner.</summary>
        public ResetMembership Membership => membership;

        public int RegisteredCapabilityCount => _registeredCapabilityCount;

        public string DisplayName => displayName.NormalizeTextOrFallback(
            gameObject != null ? gameObject.name : nameof(Resettable));

        internal ResetRegistrationHandle SubjectHandle => _subjectHandle;

        internal ResetSubject Subject => _subject;

        internal void MarkRegistered(
            ResetRegistrationHandle subjectHandle,
            ResetSubject subject,
            int registeredCapabilityCount)
        {
            _subjectHandle = subjectHandle;
            _subject = subject;
            _registeredCapabilityCount = registeredCapabilityCount;
        }

        internal void ClearRegistrationEvidence()
        {
            _subjectHandle = default;
            _subject = default;
            _registeredCapabilityCount = 0;
        }
    }
}
