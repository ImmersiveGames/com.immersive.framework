using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Immutable deterministic topology of Views and currently resolved Subjects.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B deterministic logical Camera View topology snapshot.")]
    public sealed class CameraViewAssignmentSnapshot
    {
        private readonly CameraViewSubjectSnapshot[] _views;
        private readonly IReadOnlyList<CameraViewSubjectSnapshot> _view;

        internal CameraViewAssignmentSnapshot(
            ViewAssignmentContextId contextId,
            SubjectAvailabilityContextId availabilityContextId,
            int availabilityRevision,
            int revision,
            CameraViewSubjectSnapshot[] views)
        {
            ContextId = contextId;
            AvailabilityContextId = availabilityContextId;
            AvailabilityRevision = availabilityRevision;
            Revision = revision;
            _views = views != null
                ? (CameraViewSubjectSnapshot[])views.Clone()
                : Array.Empty<CameraViewSubjectSnapshot>();
            _view = Array.AsReadOnly(_views);
        }

        public ViewAssignmentContextId ContextId { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
        public int AvailabilityRevision { get; }
        public int Revision { get; }
        public IReadOnlyList<CameraViewSubjectSnapshot> Views => _view;
        public int ViewCount => _views.Length;

        public bool TryGetView(
            CameraViewId viewId,
            out CameraViewSubjectSnapshot view)
        {
            for (int index = 0; index < _views.Length; index++)
            {
                if (_views[index].View.ViewId == viewId)
                {
                    view = _views[index];
                    return true;
                }
            }

            view = null;
            return false;
        }
    }
}
