using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Describes a selected framework camera rig without binding to a concrete camera package.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public readonly struct FrameworkCameraRigDescriptor
    {
        public FrameworkCameraRigDescriptor(
            GameObject rig,
            FrameworkCameraRigRole role,
            FrameworkCameraScope scope,
            FrameworkCameraAnchorDescriptor anchors,
            FrameworkCameraPriorityState priority,
            string source,
            string reason)
        {
            Rig = rig;
            Role = role;
            Scope = scope;
            Anchors = anchors;
            Priority = priority;
            Source = source.NormalizeTextOrFallback(nameof(FrameworkCameraRigDescriptor));
            Reason = reason.NormalizeText();
        }

        public GameObject Rig { get; }

        public FrameworkCameraRigRole Role { get; }

        public FrameworkCameraScope Scope { get; }

        public FrameworkCameraAnchorDescriptor Anchors { get; }

        public FrameworkCameraPriorityState Priority { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool HasRig => Rig != null;

        public bool IsValid => HasRig
            && Role != FrameworkCameraRigRole.Unknown
            && Scope != FrameworkCameraScope.Unknown
            && Priority.IsValid;

        public string RigName => Rig != null ? Rig.name : string.Empty;

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string rigText = RigName.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"rig='{rigText}' role='{Role}' scope='{Scope}' priority=({Priority.ToDiagnosticString()}) source='{Source}' reason='{reasonText}'";
        }
    }
}
