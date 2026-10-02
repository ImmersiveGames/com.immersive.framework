using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>API status: Experimental. Semantic authoring target for a Reset request.</summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 semantic Reset target and typed addressing payload.")]
    public struct ResetTarget
    {
        [SerializeField] private ResetTargetKind kind;
        [SerializeField] private ResetObjectTarget objectTarget;
        [SerializeField] private ResetCompositionTarget compositionTarget;

        public ResetTargetKind Kind => kind;
        public ResetObjectTarget ObjectTarget => objectTarget;
        public ResetCompositionTarget CompositionTarget => compositionTarget;

        public bool IsValid => kind switch
        {
            ResetTargetKind.Object => objectTarget.IsValid,
            ResetTargetKind.Composition => compositionTarget.IsValid,
            ResetTargetKind.CurrentActivity => true,
            ResetTargetKind.CurrentRoute => true,
            _ => false
        };

        public static ResetTarget ForObject(ResetObjectTarget value) => new ResetTarget { kind = ResetTargetKind.Object, objectTarget = value };
        public static ResetTarget ForComposition(ResetCompositionTarget value) => new ResetTarget { kind = ResetTargetKind.Composition, compositionTarget = value };
        public static ResetTarget CurrentActivity() => new ResetTarget { kind = ResetTargetKind.CurrentActivity };
        public static ResetTarget CurrentRoute() => new ResetTarget { kind = ResetTargetKind.CurrentRoute };

        public override string ToString() => kind switch
        {
            ResetTargetKind.Object => $"kind='{kind}' referenceMode='{objectTarget.ReferenceMode}'",
            ResetTargetKind.Composition => $"kind='{kind}' referenceMode='{compositionTarget.ReferenceMode}'",
            _ => $"kind='{kind}'"
        };
    }
}
