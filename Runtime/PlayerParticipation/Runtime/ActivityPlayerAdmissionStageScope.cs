using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit staging-scope handle. Public adapters receive only Activity and owner
    /// evidence; the exact RuntimeScopeContext remains package-internal.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B frozen/preparatory Activity Player admission staging scope; IF-ADR-003.")]
    public sealed class ActivityPlayerAdmissionStageScope
    {
        public ActivityPlayerAdmissionStageScope(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string identity)
            : this(activity, default, owner, identity)
        {
        }

        internal ActivityPlayerAdmissionStageScope(
            ActivityAsset activity,
            RuntimeScopeContext runtimeContext,
            RuntimeContentOwner owner,
            string identity)
        {
            Activity = activity;
            RuntimeContext = runtimeContext;
            Owner = owner;
            Identity = identity ?? string.Empty;
        }

        public ActivityAsset Activity { get; }
        public RuntimeContentOwner Owner { get; }
        public string Identity { get; }
        public bool IsValid => Activity != null && Owner.IsValid;

        internal RuntimeScopeContext RuntimeContext { get; }
    }
}
