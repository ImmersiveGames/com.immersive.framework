using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Read-only Camera-domain evidence of the exact Subject identities desired by one
    /// Composition. It does not make a Subject available and exposes no Player, Actor,
    /// Input, or scene-discovery surface.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-A explicit Camera Composition Subject selection source.")]
    public interface ICameraCompositionSubjectSelectionSource
    {
        CameraCompositionSubjectSelectionContextId ContextId { get; }

        int Revision { get; }

        CameraCompositionSubjectSelectionSnapshot CurrentSnapshot { get; }

        event Action<CameraCompositionSubjectSelectionSnapshot> SelectionChanged;
    }
}
