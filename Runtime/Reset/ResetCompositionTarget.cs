using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>Addressing data for one semantic Composition Reset target.</summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 Composition target with direct or stable addressing.")]
    public struct ResetCompositionTarget
    {
        [SerializeField] private ResetReferenceMode referenceMode;
        [SerializeField] private ResetComposition directComposition;
        [SerializeField] private StableObjectReference stableReference;

        public ResetReferenceMode ReferenceMode => referenceMode;
        public ResetComposition DirectComposition => directComposition;
        public StableObjectReference StableReference => stableReference;

        public bool IsValid => referenceMode switch
        {
            ResetReferenceMode.Direct => directComposition != null,
            ResetReferenceMode.Stable => stableReference.IsValid,
            _ => false
        };

        public static ResetCompositionTarget Direct(ResetComposition composition) => new ResetCompositionTarget
        {
            referenceMode = ResetReferenceMode.Direct,
            directComposition = composition
        };

        public static ResetCompositionTarget Stable(StableObjectReference reference) => new ResetCompositionTarget
        {
            referenceMode = ResetReferenceMode.Stable,
            stableReference = reference
        };
    }
}
