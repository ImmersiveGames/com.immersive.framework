using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>Applies one explicit View-to-Output policy to exact physical output Cameras.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session-scoped CAMERA-026-H viewport authority.")]
    internal sealed class CameraViewOutputRuntime : IDisposable
    {
        private readonly CameraOutputSessionTopology _outputs;
        private readonly Dictionary<CameraOutputId, Rect> _authoredViewports = new Dictionary<CameraOutputId, Rect>();
        private CameraViewOutputTopology _current;
        private bool _disposed;

        private CameraViewOutputRuntime(CameraOutputSessionTopology outputs)
        {
            _outputs = outputs;
            CameraOutputTopologySnapshot snapshot = outputs.CaptureSnapshot();
            for (int index = 0; index < snapshot.Outputs.Count; index++)
            {
                CameraOutputId outputId = snapshot.Outputs[index].OutputId;
                if (outputs.TryGetOutput(outputId, out CameraOutputAuthoring output, out _))
                    _authoredViewports.Add(outputId, output.UnityCamera.rect);
            }
        }

        internal CameraViewOutputTopology Current => _current;

        internal static bool TryCreate(
            CameraOutputSessionTopology outputs,
            CameraViewOutputTopology topology,
            out CameraViewOutputRuntime runtime,
            out string diagnostic)
        {
            runtime = null;
            if (outputs == null || topology == null)
            {
                diagnostic = "Camera View-to-Output runtime requires explicit Output and binding topologies.";
                return false;
            }
            if (topology.BindingCount != outputs.OutputCount)
            {
                diagnostic = $"Camera View-to-Output policy must bind every active Output exactly once. outputs='{outputs.OutputCount}' bindings='{topology.BindingCount}'.";
                return false;
            }

            var candidate = new CameraViewOutputRuntime(outputs);
            if (!candidate.TryApply(topology, out diagnostic))
            {
                candidate.Dispose();
                return false;
            }
            runtime = candidate;
            return true;
        }

        internal bool TryApply(CameraViewOutputTopology topology, out string diagnostic)
        {
            if (_disposed || topology == null)
            {
                diagnostic = _disposed
                    ? "Camera View-to-Output runtime is already torn down."
                    : "Camera View-to-Output apply requires an explicit topology.";
                return false;
            }

            CameraViewOutputTopologySnapshot next = topology.CaptureSnapshot();
            var resolved = new CameraOutputAuthoring[next.BindingCount];
            for (int index = 0; index < next.BindingCount; index++)
            {
                CameraViewOutputBinding binding = next.Bindings[index];
                if (!_outputs.TryGetOutput(binding.OutputId, out resolved[index], out diagnostic))
                    return false;
                if (resolved[index].UnityCamera == null)
                {
                    diagnostic = $"Camera Output '{binding.OutputId}' has no physical Unity Camera for viewport application.";
                    return false;
                }
            }

            if (_current != null)
            {
                CameraViewOutputTopologySnapshot previous = _current.CaptureSnapshot();
                for (int index = 0; index < previous.BindingCount; index++)
                {
                    CameraOutputId outputId = previous.Bindings[index].OutputId;
                    if (!topology.TryGetBinding(outputId, out _) &&
                        _outputs.TryGetOutput(outputId, out CameraOutputAuthoring removed, out _) &&
                        _authoredViewports.TryGetValue(outputId, out Rect authored))
                        removed.UnityCamera.rect = authored;
                }
            }

            for (int index = 0; index < next.BindingCount; index++)
                resolved[index].UnityCamera.rect = next.Bindings[index].Viewport.ToRect();

            _current = topology;
            diagnostic = $"Applied '{next.BindingCount}' explicit Camera View-to-Output viewport binding(s).";
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (KeyValuePair<CameraOutputId, Rect> pair in _authoredViewports)
            {
                if (_outputs.TryGetOutput(pair.Key, out CameraOutputAuthoring output, out _) && output.UnityCamera != null)
                    output.UnityCamera.rect = pair.Value;
            }
            _current = null;
            _disposed = true;
        }
    }
}
