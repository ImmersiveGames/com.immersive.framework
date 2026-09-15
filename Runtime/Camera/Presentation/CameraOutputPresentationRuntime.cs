using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-028-C Session-bounded Camera.rect presentation authority.")]
    internal sealed class CameraOutputPresentationRuntime : IDisposable
    {
        private readonly CameraOutputSessionTopology _outputs;
        private readonly Dictionary<CameraOutputId, Acquisition> _owned =
            new Dictionary<CameraOutputId, Acquisition>();
        private int _revision;
        private bool _disposed;

        private CameraOutputPresentationRuntime(CameraOutputSessionTopology outputs)
        {
            _outputs = outputs;
        }

        internal CameraOutputPresentationSnapshot Snapshot => CaptureSnapshot();

        internal static bool TryCreate(
            CameraOutputSessionTopology outputs,
            out CameraOutputPresentationRuntime runtime,
            out string diagnostic)
        {
            runtime = null;
            if (outputs == null)
            {
                diagnostic = "Camera Output presentation runtime requires an active Output topology.";
                return false;
            }

            runtime = new CameraOutputPresentationRuntime(outputs);
            diagnostic = "Camera Output presentation runtime created with zero owned Outputs.";
            return true;
        }

        internal CameraOutputPresentationResult Apply(
            IReadOnlyList<CameraOutputPresentationBinding> bindings)
        {
            if (_disposed)
                return Rejected(
                    CameraOutputPresentationStatus.RejectedDisposed,
                    "Camera Output presentation runtime is already disposed.");
            if (bindings == null)
                return Rejected(
                    CameraOutputPresentationStatus.RejectedInvalidSnapshot,
                    "Camera Output presentation apply requires an explicit binding collection.");

            var candidates = new Candidate[bindings.Count];
            var requestedOutputs = new HashSet<CameraOutputId>();
            for (int index = 0; index < bindings.Count; index++)
            {
                CameraOutputPresentationBinding binding = bindings[index];
                if (!binding.OutputId.IsValid)
                {
                    return Rejected(
                        CameraOutputPresentationStatus.RejectedOutputUnavailable,
                        $"Camera Output presentation binding at index '{index}' has an invalid Output ID.");
                }
                if (!_outputs.TryGetOutput(
                        binding.OutputId,
                        out CameraOutputAuthoring output,
                        out string outputDiagnostic))
                {
                    return Rejected(
                        CameraOutputPresentationStatus.RejectedOutputUnavailable,
                        outputDiagnostic);
                }
                if (!requestedOutputs.Add(binding.OutputId))
                {
                    return Rejected(
                        CameraOutputPresentationStatus.RejectedDuplicateOutput,
                        $"Camera Output presentation snapshot contains duplicate Output '{binding.OutputId}'.");
                }
                if (!binding.Rect.IsValid)
                {
                    return Rejected(
                        CameraOutputPresentationStatus.RejectedInvalidRect,
                        $"Camera Output presentation binding for '{binding.OutputId}' has invalid normalized rect '{binding.Rect}'.");
                }
                if (output.UnityCamera == null)
                {
                    return Rejected(
                        CameraOutputPresentationStatus.RejectedMissingCamera,
                        $"Camera Output '{binding.OutputId}' has no available explicit Unity Camera.");
                }

                candidates[index] = new Candidate(
                    binding,
                    new CameraOutputPresentationTarget(output.UnityCamera));
            }

            Array.Sort(candidates, CompareCandidates);
            if (MatchesCurrentSnapshot(candidates))
            {
                return Result(
                    CameraOutputPresentationStatus.NoChange,
                    "Camera Output presentation snapshot already matches the current ownership state.");
            }

            bool previouslyOwned = _owned.Count > 0;
            ReleaseRemoved(requestedOutputs);
            for (int index = 0; index < candidates.Length; index++)
            {
                Candidate candidate = candidates[index];
                if (!_owned.TryGetValue(candidate.Binding.OutputId, out Acquisition acquisition))
                {
                    acquisition = new Acquisition(
                        candidate.Target,
                        candidate.Target.CaptureRect(),
                        candidate.Binding.Rect);
                    _owned.Add(candidate.Binding.OutputId, acquisition);
                }
                else
                {
                    acquisition.Rect = candidate.Binding.Rect;
                }

                candidate.Target.Apply(candidate.Binding.Rect);
            }

            _revision++;
            CameraOutputPresentationStatus status = candidates.Length == 0
                ? CameraOutputPresentationStatus.Cleared
                : previouslyOwned
                    ? CameraOutputPresentationStatus.Replaced
                    : CameraOutputPresentationStatus.Applied;
            return Result(
                status,
                $"Camera Output presentation snapshot applied with '{candidates.Length}' owned Output(s).");
        }

        internal CameraOutputPresentationResult Clear() =>
            Apply(Array.Empty<CameraOutputPresentationBinding>());

        public void Dispose()
        {
            if (_disposed) return;
            RestoreAll();
            _disposed = true;
        }

        private void ReleaseRemoved(HashSet<CameraOutputId> requestedOutputs)
        {
            var removed = new List<CameraOutputId>();
            foreach (KeyValuePair<CameraOutputId, Acquisition> pair in _owned)
            {
                if (!requestedOutputs.Contains(pair.Key)) removed.Add(pair.Key);
            }
            removed.Sort(CompareOutputIds);
            for (int index = 0; index < removed.Count; index++)
            {
                CameraOutputId outputId = removed[index];
                Acquisition acquisition = _owned[outputId];
                acquisition.Target.Restore(acquisition.Baseline);
                _owned.Remove(outputId);
            }
        }

        private void RestoreAll()
        {
            var outputIds = new List<CameraOutputId>(_owned.Keys);
            outputIds.Sort(CompareOutputIds);
            for (int index = 0; index < outputIds.Count; index++)
            {
                Acquisition acquisition = _owned[outputIds[index]];
                acquisition.Target.Restore(acquisition.Baseline);
            }
            _owned.Clear();
        }

        private bool MatchesCurrentSnapshot(Candidate[] candidates)
        {
            if (_owned.Count != candidates.Length) return false;
            for (int index = 0; index < candidates.Length; index++)
            {
                CameraOutputPresentationBinding binding = candidates[index].Binding;
                if (!_owned.TryGetValue(binding.OutputId, out Acquisition acquisition) ||
                    acquisition.Rect != binding.Rect)
                {
                    return false;
                }
            }
            return true;
        }

        private CameraOutputPresentationSnapshot CaptureSnapshot()
        {
            var outputIds = new List<CameraOutputId>(_owned.Keys);
            outputIds.Sort(CompareOutputIds);
            var entries = new CameraOutputPresentationEntrySnapshot[outputIds.Count];
            for (int index = 0; index < outputIds.Count; index++)
            {
                CameraOutputId outputId = outputIds[index];
                Acquisition acquisition = _owned[outputId];
                entries[index] = new CameraOutputPresentationEntrySnapshot(
                    outputId,
                    acquisition.Rect,
                    true);
            }
            return new CameraOutputPresentationSnapshot(_revision, _disposed, entries);
        }

        private CameraOutputPresentationResult Rejected(
            CameraOutputPresentationStatus status,
            string diagnostic) => new CameraOutputPresentationResult(
                status,
                CaptureSnapshot(),
                diagnostic);

        private CameraOutputPresentationResult Result(
            CameraOutputPresentationStatus status,
            string diagnostic) => new CameraOutputPresentationResult(
                status,
                CaptureSnapshot(),
                diagnostic);

        private static int CompareCandidates(Candidate left, Candidate right) =>
            CompareOutputIds(left.Binding.OutputId, right.Binding.OutputId);

        private static int CompareOutputIds(CameraOutputId left, CameraOutputId right) =>
            string.Compare(left.Value, right.Value, StringComparison.Ordinal);

        private readonly struct Candidate
        {
            internal Candidate(
                CameraOutputPresentationBinding binding,
                CameraOutputPresentationTarget target)
            {
                Binding = binding;
                Target = target;
            }

            internal CameraOutputPresentationBinding Binding { get; }
            internal CameraOutputPresentationTarget Target { get; }
        }

        private sealed class Acquisition
        {
            internal Acquisition(
                CameraOutputPresentationTarget target,
                Rect baseline,
                CameraOutputPresentationRect rect)
            {
                Target = target;
                Baseline = baseline;
                Rect = rect;
            }

            internal CameraOutputPresentationTarget Target { get; }
            internal Rect Baseline { get; }
            internal CameraOutputPresentationRect Rect { get; set; }
        }
    }
}
