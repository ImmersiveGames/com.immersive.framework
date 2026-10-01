using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Reset.Unity;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>API status: Experimental. Semantic authoring target for a Reset request.</summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 RESET-035-E/F semantic request target.")]
    public struct ResetTarget
    {
        [SerializeField] private ResetTargetKind kind;
        [SerializeField] private Resettable resettable;
        [SerializeField] private ResetComposition composition;
        [SerializeField] private StableObjectReference stableReference;

        public ResetTargetKind Kind => kind;
        public Resettable Object => resettable;
        public ResetComposition Composition => composition;
        public StableObjectReference StableReference => stableReference;

        public bool IsValid => kind switch
        {
            ResetTargetKind.Object => resettable != null,
            ResetTargetKind.Composition => composition != null,
            ResetTargetKind.StableReference => stableReference.IsValid,
            ResetTargetKind.CurrentActivity => true,
            ResetTargetKind.CurrentRoute => true,
            _ => false
        };

        public static ResetTarget ForObject(Resettable value) => new ResetTarget { kind = ResetTargetKind.Object, resettable = value };
        public static ResetTarget ForComposition(ResetComposition value) => new ResetTarget { kind = ResetTargetKind.Composition, composition = value };
        public static ResetTarget ForStableReference(StableObjectReference value) => new ResetTarget { kind = ResetTargetKind.StableReference, stableReference = value };
        public static ResetTarget CurrentActivity() => new ResetTarget { kind = ResetTargetKind.CurrentActivity };
        public static ResetTarget CurrentRoute() => new ResetTarget { kind = ResetTargetKind.CurrentRoute };

        public override string ToString() => kind switch
        {
            ResetTargetKind.Object => $"kind='{kind}' object='{(resettable != null ? resettable.name : "<null>")}'",
            ResetTargetKind.Composition => $"kind='{kind}' composition='{(composition != null ? composition.name : "<null>")}'",
            ResetTargetKind.StableReference => $"kind='{kind}' {stableReference}",
            _ => $"kind='{kind}'"
        };
    }
}
