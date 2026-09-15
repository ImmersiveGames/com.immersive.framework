using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-028-C immutable per-Output presentation ownership evidence.")]
    internal readonly struct CameraOutputPresentationEntrySnapshot
    {
        internal CameraOutputPresentationEntrySnapshot(
            CameraOutputId outputId,
            CameraOutputPresentationRect rect,
            bool baselineCaptured)
        {
            OutputId = outputId;
            Rect = rect;
            BaselineCaptured = baselineCaptured;
        }

        internal CameraOutputId OutputId { get; }
        internal CameraOutputPresentationRect Rect { get; }
        internal bool BaselineCaptured { get; }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-028-C immutable Output presentation runtime snapshot.")]
    internal sealed class CameraOutputPresentationSnapshot
    {
        internal CameraOutputPresentationSnapshot(
            int revision,
            bool isDisposed,
            CameraOutputPresentationEntrySnapshot[] ownedOutputs)
        {
            Revision = revision;
            IsDisposed = isDisposed;
            OwnedOutputs = Array.AsReadOnly(
                ownedOutputs ?? Array.Empty<CameraOutputPresentationEntrySnapshot>());
        }

        internal int Revision { get; }
        internal bool IsDisposed { get; }
        internal IReadOnlyList<CameraOutputPresentationEntrySnapshot> OwnedOutputs { get; }
        internal int OwnedOutputCount => OwnedOutputs.Count;
    }
}
