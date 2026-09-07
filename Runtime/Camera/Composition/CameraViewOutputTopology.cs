using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-H explicit View-to-Output binding.")]
    public readonly struct CameraViewOutputBinding
    {
        public CameraViewOutputBinding(
            CameraViewId viewId,
            CameraOutputId outputId,
            CameraViewport viewport)
        {
            ViewId = viewId;
            OutputId = outputId;
            Viewport = viewport;
        }

        public CameraViewId ViewId { get; }
        public CameraOutputId OutputId { get; }
        public CameraViewport Viewport { get; }
        public bool IsValid => ViewId.IsValid && OutputId.IsValid && Viewport.IsValid;
    }

    /// <summary>
    /// Immutable composition policy. A View may feed multiple Outputs, while one physical
    /// Output has exactly one View binding in a snapshot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-H explicit View-to-Output topology.")]
    public sealed class CameraViewOutputTopology
    {
        private readonly CameraViewOutputBinding[] _bindings;
        private readonly Dictionary<CameraOutputId, CameraViewOutputBinding> _byOutput;
        private readonly Dictionary<CameraViewId, CameraViewOutputBinding[]> _byView;

        private CameraViewOutputTopology(CameraViewOutputBinding[] bindings)
        {
            _bindings = bindings;
            _byOutput = new Dictionary<CameraOutputId, CameraViewOutputBinding>(bindings.Length);
            var grouped = new Dictionary<CameraViewId, List<CameraViewOutputBinding>>();
            for (int index = 0; index < bindings.Length; index++)
            {
                CameraViewOutputBinding binding = bindings[index];
                _byOutput.Add(binding.OutputId, binding);
                if (!grouped.TryGetValue(binding.ViewId, out List<CameraViewOutputBinding> viewBindings))
                {
                    viewBindings = new List<CameraViewOutputBinding>();
                    grouped.Add(binding.ViewId, viewBindings);
                }
                viewBindings.Add(binding);
            }

            _byView = new Dictionary<CameraViewId, CameraViewOutputBinding[]>(grouped.Count);
            foreach (KeyValuePair<CameraViewId, List<CameraViewOutputBinding>> pair in grouped)
                _byView.Add(pair.Key, pair.Value.ToArray());
        }

        public int BindingCount => _bindings.Length;

        public static bool TryCreate(
            IReadOnlyList<CameraViewOutputBinding> bindings,
            out CameraViewOutputTopology topology,
            out string diagnostic)
        {
            topology = null;
            if (bindings == null)
            {
                diagnostic = "Camera View-to-Output topology requires an explicit binding collection.";
                return false;
            }

            var ordered = new CameraViewOutputBinding[bindings.Count];
            var outputs = new HashSet<CameraOutputId>();
            for (int index = 0; index < bindings.Count; index++)
            {
                CameraViewOutputBinding binding = bindings[index];
                if (!binding.ViewId.IsValid)
                {
                    diagnostic = $"Camera View-to-Output binding at index '{index}' has an invalid View ID.";
                    return false;
                }
                if (!binding.OutputId.IsValid)
                {
                    diagnostic = $"Camera View-to-Output binding at index '{index}' has an invalid Output ID.";
                    return false;
                }
                if (!binding.Viewport.IsValid)
                {
                    diagnostic = $"Camera View-to-Output binding for View '{binding.ViewId}' and Output '{binding.OutputId}' has an invalid normalized viewport.";
                    return false;
                }
                if (!outputs.Add(binding.OutputId))
                {
                    diagnostic = $"Camera View-to-Output topology has conflicting bindings for Output '{binding.OutputId}'.";
                    return false;
                }
                ordered[index] = binding;
            }

            Array.Sort(ordered, CompareBindings);
            topology = new CameraViewOutputTopology(ordered);
            diagnostic = string.Empty;
            return true;
        }

        public bool TryGetBinding(CameraOutputId outputId, out CameraViewOutputBinding binding)
        {
            binding = default;
            return outputId.IsValid && _byOutput.TryGetValue(outputId, out binding);
        }

        public bool TryGetBinding(
            CameraViewId viewId,
            CameraOutputId outputId,
            out CameraViewOutputBinding binding)
        {
            return TryGetBinding(outputId, out binding) && binding.ViewId == viewId;
        }

        public IReadOnlyList<CameraViewOutputBinding> GetBindings(CameraViewId viewId) =>
            viewId.IsValid && _byView.TryGetValue(viewId, out CameraViewOutputBinding[] bindings)
                ? Array.AsReadOnly(bindings)
                : Array.Empty<CameraViewOutputBinding>();

        public CameraViewOutputTopologySnapshot CaptureSnapshot() =>
            new CameraViewOutputTopologySnapshot((CameraViewOutputBinding[])_bindings.Clone());

        private static int CompareBindings(CameraViewOutputBinding left, CameraViewOutputBinding right)
        {
            int outputComparison = string.Compare(left.OutputId.Value, right.OutputId.Value, StringComparison.Ordinal);
            return outputComparison != 0
                ? outputComparison
                : string.Compare(left.ViewId.Value, right.ViewId.Value, StringComparison.Ordinal);
        }
    }

    public sealed class CameraViewOutputTopologySnapshot
    {
        internal CameraViewOutputTopologySnapshot(CameraViewOutputBinding[] bindings)
        {
            Bindings = Array.AsReadOnly(bindings ?? Array.Empty<CameraViewOutputBinding>());
        }

        public IReadOnlyList<CameraViewOutputBinding> Bindings { get; }
        public int BindingCount => Bindings.Count;
    }
}
