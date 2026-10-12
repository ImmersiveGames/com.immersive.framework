using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>Base Unity behavior for a synchronous capability discovered by a Resettable boundary.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 Resettable capability authoring behavior.")]
    public abstract class UnityResetParticipantBehaviour : MonoBehaviour
    {
        [Header("Reset Capability")]
        [SerializeField] private ResetParticipantRequiredness requiredness = ResetParticipantRequiredness.Required;
        [SerializeField] private int order;
        [SerializeField] private string displayName;

        public ResetParticipantRequiredness Requiredness => requiredness;

        public int Order => order;

        public string DisplayName => ResolveDisplayName();

        public abstract ResetParticipantResult Reset(ResetContext context);

        protected ResetParticipantDescriptor CreateDescriptorForResult(ResetContext context)
        {
            return context.Participant;
        }

        protected string ResolveDisplayName()
        {
            return displayName.NormalizeTextOrFallback(GetType().Name);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureDefaults(
            ResetParticipantRequiredness defaultRequiredness,
            int defaultOrder,
            string defaultDisplayName)
        {
            requiredness = defaultRequiredness;
            order = defaultOrder;
            displayName = defaultDisplayName;
        }
#endif
    }
}
