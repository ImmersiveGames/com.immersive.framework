using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Runtime-only logical state for the normal assignment, normal occurrence and fallback coverage of one Output.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-038-C Output state boundary.")]
    public sealed class CameraOutputState
    {
        private readonly CameraOutputId _outputId;
        private SessionCameraAssignmentId _activeAssignmentId;
        private CameraOccurrenceMode _activeOccurrenceMode;
        private CameraOccurrenceIdentity _normalOccurrence;

        public CameraOutputState(CameraOutputId outputId)
        {
            if (!outputId.IsValid) throw new ArgumentException("Output state requires a valid Output identity.", nameof(outputId));
            _outputId = outputId;
        }

        public CameraOutputId OutputId => _outputId;
        public bool IsFallbackAvailable { get; private set; }
        public bool IsFallbackCovering { get; private set; }
        public bool HasActiveAssignment => _activeAssignmentId.IsValid;
        public SessionCameraAssignmentId ActiveAssignmentId => _activeAssignmentId;
        public bool HasPresentedNormalOccurrence => !IsFallbackCovering && _normalOccurrence.IsValid;
        public CameraOccurrenceIdentity PresentedNormalOccurrence => HasPresentedNormalOccurrence ? _normalOccurrence : default;
        public bool HasRetainedNormalOccurrence => _normalOccurrence.IsValid;
        public CameraOccurrenceIdentity RetainedNormalOccurrence => _normalOccurrence;

        public bool TryMakeFallbackAvailable(out string issue)
        {
            if (!IsFallbackAvailable)
            {
                IsFallbackAvailable = true;
                IsFallbackCovering = true;
            }
            issue = string.Empty;
            return true;
        }

        public bool TrySetActiveAssignment(SessionCameraAssignment assignment, out string issue)
        {
            issue = string.Empty;
            if (assignment == null || !assignment.TryValidate(out issue))
            {
                if (string.IsNullOrEmpty(issue)) issue = "An explicitly valid Session Camera Assignment is required.";
                return false;
            }

            bool outputMapped = false;
            for (int index = 0; index < assignment.Outputs.Count; index++)
            {
                if (assignment.Outputs[index].OutputId == _outputId) { outputMapped = true; break; }
            }
            if (!outputMapped)
            {
                issue = $"Assignment '{assignment.Id}' does not map Output '{_outputId}'.";
                return false;
            }

            bool assignmentConfigurationChanged = _activeAssignmentId != assignment.Id ||
                _activeOccurrenceMode != assignment.OccurrenceMode;
            if (_activeAssignmentId.IsValid && assignmentConfigurationChanged &&
                _normalOccurrence.IsValid && !IsFallbackCovering)
            {
                issue = "The current normal occurrence must be covered by Fallback before replacing its active Assignment.";
                return false;
            }

            if (assignmentConfigurationChanged)
            {
                _normalOccurrence = default;
                _activeAssignmentId = assignment.Id;
                _activeOccurrenceMode = assignment.OccurrenceMode;
            }

            issue = string.Empty;
            return true;
        }

        public bool TryPresentNormalOccurrence(CameraOccurrenceIdentity occurrence, out string issue)
        {
            if (!CanPresentNormalOccurrence(occurrence, out issue)) return false;
            _normalOccurrence = occurrence;
            IsFallbackCovering = false;
            issue = string.Empty;
            return true;
        }

        public bool CanPresentNormalOccurrence(CameraOccurrenceIdentity occurrence, out string issue)
        {
            if (!IsFallbackAvailable)
            {
                issue = "Fallback must be available before a normal occurrence can be presented.";
                return false;
            }
            if (!_activeAssignmentId.IsValid || !occurrence.IsValid || occurrence.AssignmentId != _activeAssignmentId || occurrence.OutputId != _outputId)
            {
                issue = "Normal occurrence must match the configured active Assignment and exact Output.";
                return false;
            }
            return TryValidateMode(occurrence, out issue);
        }

        public bool CanRestoreNormalOccurrence(out string issue)
        {
            if (!IsFallbackAvailable || !IsFallbackCovering || !_normalOccurrence.IsValid ||
                _normalOccurrence.AssignmentId != _activeAssignmentId ||
                _normalOccurrence.OutputId != _outputId)
            {
                issue = "No retained normal occurrence for the active Assignment can be restored.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

        public bool TryCoverWithFallback(out string issue)
        {
            if (!IsFallbackAvailable)
            {
                issue = "Fallback coverage is unavailable because no Fallback Camera is ready on this Output.";
                return false;
            }
            IsFallbackCovering = true;
            issue = string.Empty;
            return true;
        }

        public bool TryRestoreNormalOccurrence(out string issue)
        {
            if (!CanRestoreNormalOccurrence(out issue)) return false;
            IsFallbackCovering = false;
            issue = string.Empty;
            return true;
        }

        private bool TryValidateMode(CameraOccurrenceIdentity occurrence, out string issue)
        {
            bool requiresPlayerOccurrence = _activeOccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer;
            if (occurrence.IsIndividual != requiresPlayerOccurrence)
            {
                issue = $"Occurrence identity shape does not match active Assignment mode '{_activeOccurrenceMode}'.";
                return false;
            }
            issue = string.Empty;
            return true;
        }
    }
}
