using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    internal readonly struct CameraOutputStateSnapshot
    {
        internal CameraOutputStateSnapshot(
            SessionCameraAssignmentId assignmentId,
            CameraOccurrenceMode occurrenceMode,
            CameraOccurrenceIdentity occurrence,
            bool fallbackAvailable,
            bool fallbackCovering)
        {
            AssignmentId = assignmentId;
            OccurrenceMode = occurrenceMode;
            Occurrence = occurrence;
            FallbackAvailable = fallbackAvailable;
            FallbackCovering = fallbackCovering;
        }

        internal SessionCameraAssignmentId AssignmentId { get; }
        internal CameraOccurrenceMode OccurrenceMode { get; }
        internal CameraOccurrenceIdentity Occurrence { get; }
        internal bool FallbackAvailable { get; }
        internal bool FallbackCovering { get; }

        internal bool Matches(CameraOutputStateSnapshot other) =>
            AssignmentId == other.AssignmentId &&
            OccurrenceMode == other.OccurrenceMode &&
            Occurrence == other.Occurrence &&
            FallbackAvailable == other.FallbackAvailable &&
            FallbackCovering == other.FallbackCovering;
    }

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

        internal CameraOutputStateSnapshot CaptureSnapshot() => new CameraOutputStateSnapshot(
            _activeAssignmentId,
            _activeOccurrenceMode,
            _normalOccurrence,
            IsFallbackAvailable,
            IsFallbackCovering);

        internal bool CanCommitReplacement(
            CameraOutputStateSnapshot expected,
            SessionCameraAssignment assignment,
            CameraOccurrenceIdentity occurrence,
            bool fallbackCovering,
            out string issue)
        {
            issue = string.Empty;
            if (!CaptureSnapshot().Matches(expected))
            {
                issue = $"Camera Output '{_outputId}' changed while the Assignment candidate was being prepared.";
                return false;
            }

            if (fallbackCovering && !IsFallbackAvailable)
            {
                issue = $"Camera Output '{_outputId}' has no valid Fallback Camera for the candidate state.";
                return false;
            }

            if (assignment == null)
            {
                if (occurrence.IsValid || !fallbackCovering)
                {
                    issue = "Clearing an Output Assignment requires no occurrence and explicit Fallback coverage.";
                    return false;
                }
                return true;
            }

            if (!assignment.TryValidate(out issue))
            {
                return false;
            }
            bool outputMapped = false;
            for (int index = 0; index < assignment.Outputs.Count; index++)
            {
                if (assignment.Outputs[index].OutputId == _outputId)
                {
                    outputMapped = true;
                    break;
                }
            }
            if (!outputMapped)
            {
                issue = $"Assignment '{assignment.Id}' does not map Output '{_outputId}'.";
                return false;
            }

            if (occurrence.IsValid &&
                (occurrence.AssignmentId != assignment.Id ||
                 occurrence.OutputId != _outputId ||
                 occurrence.IsIndividual !=
                    (assignment.OccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer)))
            {
                issue = $"Candidate occurrence '{occurrence}' does not match Assignment '{assignment.Id}' and Output '{_outputId}'.";
                return false;
            }

            if (!occurrence.IsValid && !fallbackCovering)
            {
                issue = "An Assignment without a current candidate occurrence requires Fallback coverage.";
                return false;
            }
            return true;
        }

        internal bool TryCommitReplacement(
            CameraOutputStateSnapshot expected,
            SessionCameraAssignment assignment,
            CameraOccurrenceIdentity occurrence,
            bool fallbackCovering,
            out string issue)
        {
            if (!CanCommitReplacement(expected, assignment, occurrence, fallbackCovering, out issue))
            {
                return false;
            }

            _activeAssignmentId = assignment != null ? assignment.Id : default;
            _activeOccurrenceMode = assignment != null
                ? assignment.OccurrenceMode
                : CameraOccurrenceMode.Undefined;
            _normalOccurrence = occurrence;
            IsFallbackCovering = fallbackCovering;
            issue = string.Empty;
            return true;
        }

        internal void RestoreSnapshot(CameraOutputStateSnapshot snapshot)
        {
            _activeAssignmentId = snapshot.AssignmentId;
            _activeOccurrenceMode = snapshot.OccurrenceMode;
            _normalOccurrence = snapshot.Occurrence;
            IsFallbackAvailable = snapshot.FallbackAvailable;
            IsFallbackCovering = snapshot.FallbackCovering;
        }

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

        internal bool TryClearActiveAssignment(out string issue)
        {
            if (!_activeAssignmentId.IsValid || !IsFallbackAvailable || !IsFallbackCovering)
            {
                issue = "An active Assignment can be ended only while valid Fallback coverage is applied.";
                return false;
            }

            _activeAssignmentId = default;
            _activeOccurrenceMode = CameraOccurrenceMode.Undefined;
            _normalOccurrence = default;
            issue = string.Empty;
            return true;
        }

        internal bool TryRetainNormalOccurrence(CameraOccurrenceIdentity occurrence, out string issue)
        {
            if (!CanPresentNormalOccurrence(occurrence, out issue)) return false;
            _normalOccurrence = occurrence;
            IsFallbackCovering = true;
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

        internal bool TryRemoveIndividualOccurrence(
            CameraOccurrenceIdentity occurrence,
            out string issue)
        {
            if (_activeOccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer ||
                !_normalOccurrence.IsIndividual ||
                _normalOccurrence != occurrence ||
                occurrence.AssignmentId != _activeAssignmentId ||
                occurrence.OutputId != _outputId)
            {
                issue = "Only the exact currently retained Individual occurrence can be removed from this Output.";
                return false;
            }

            _normalOccurrence = default;
            IsFallbackCovering = true;
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
