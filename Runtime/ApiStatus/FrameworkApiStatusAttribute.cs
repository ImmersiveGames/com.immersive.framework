using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.ApiStatus
{
    /// <summary>
    /// API status: Stable. Lightweight source-level marker for framework API maturity.
    /// This marker is documentation/validation metadata only; it does not alter runtime behavior.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly
        | AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Interface
        | AttributeTargets.Enum
        | AttributeTargets.Delegate,
        AllowMultiple = false,
        Inherited = false)]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Canonical API maturity metadata introduced by F1B.")]
    public sealed class FrameworkApiStatusAttribute : Attribute
    {
        public FrameworkApiStatusAttribute(FrameworkApiStatus status, string note)
        {
            Status = status;
            Note = note.NormalizeText();
        }

        public FrameworkApiStatus Status { get; }

        public string Note { get; }
    }
}
