using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>Addressing data for one semantic Object Reset target.</summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 Object target with direct or stable addressing.")]
    public struct ResetObjectTarget
    {
        [SerializeField] private ResetReferenceMode referenceMode;
        [SerializeField] private Resettable directResettable;
        [SerializeField] private StableObjectReference stableReference;

        public ResetReferenceMode ReferenceMode => referenceMode;
        public Resettable DirectResettable => directResettable;
        public StableObjectReference StableReference => stableReference;

        public bool IsValid => referenceMode switch
        {
            ResetReferenceMode.Direct => directResettable != null,
            ResetReferenceMode.Stable => stableReference.IsValid,
            _ => false
        };

        public static ResetObjectTarget Direct(Resettable resettable) => new ResetObjectTarget
        {
            referenceMode = ResetReferenceMode.Direct,
            directResettable = resettable
        };

        public static ResetObjectTarget Stable(StableObjectReference reference) => new ResetObjectTarget
        {
            referenceMode = ResetReferenceMode.Stable,
            stableReference = reference
        };
    }
}
