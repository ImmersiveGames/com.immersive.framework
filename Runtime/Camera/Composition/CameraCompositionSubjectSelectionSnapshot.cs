using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Identity of one explicit Subject-selection authority. Blank values are invalid.
    /// The id is Camera-domain evidence and does not encode a Player Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-A explicit Camera Composition Subject selection context identity.")]
    public readonly struct CameraCompositionSubjectSelectionContextId :
        IEquatable<CameraCompositionSubjectSelectionContextId>
    {
        public CameraCompositionSubjectSelectionContextId(string value) =>
            Value = value.NormalizeText();

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraCompositionSubjectSelectionContextId other) =>
            string.Equals(Value ?? string.Empty, other.Value ?? string.Empty, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CameraCompositionSubjectSelectionContextId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(
            CameraCompositionSubjectSelectionContextId left,
            CameraCompositionSubjectSelectionContextId right) => left.Equals(right);

        public static bool operator !=(
            CameraCompositionSubjectSelectionContextId left,
            CameraCompositionSubjectSelectionContextId right) => !left.Equals(right);
    }

    /// <summary>
    /// Immutable, deterministically ordered set of exact Camera Subject identities desired
    /// by one Composition. The array is typed Camera-domain identity and is not a view of
    /// live writer state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-A immutable explicit Camera Composition Subject selection snapshot.")]
    public sealed class CameraCompositionSubjectSelectionSnapshot
    {
        private readonly CameraSubjectId[] _subjectIds;

        internal CameraCompositionSubjectSelectionSnapshot(
            CameraCompositionSubjectSelectionContextId contextId,
            int revision,
            CameraSubjectId[] subjectIds)
        {
            ContextId = contextId;
            Revision = revision;
            _subjectIds = subjectIds != null
                ? (CameraSubjectId[])subjectIds.Clone()
                : Array.Empty<CameraSubjectId>();
            SubjectIds = Array.AsReadOnly(_subjectIds);
        }

        public CameraCompositionSubjectSelectionContextId ContextId { get; }

        public int Revision { get; }

        public IReadOnlyList<CameraSubjectId> SubjectIds { get; }

        public int Count => _subjectIds.Length;
    }
}
