using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Resolves and validates one explicit logical View-to-Output association topology
    /// against the exact physical Outputs available in a Session. This runtime owns no
    /// physical presentation: it does not read, write or restore <c>Camera.rect</c> or any
    /// other layout/viewport state. Output Presentation / Layout authority is a separate,
    /// not-yet-implemented concern (CAMERA-028-C).
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-028-B session-scoped logical View-to-Output association authority.")]
    internal sealed class CameraViewOutputRuntime : IDisposable
    {
        private readonly CameraOutputSessionTopology _outputs;
        private CameraViewOutputTopology _current;
        private bool _disposed;

        private CameraViewOutputRuntime(CameraOutputSessionTopology outputs)
        {
            _outputs = outputs;
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
            for (int index = 0; index < next.BindingCount; index++)
            {
                CameraViewOutputBinding binding = next.Bindings[index];
                if (!_outputs.TryGetOutput(binding.OutputId, out _, out diagnostic))
                    return false;
            }

            _current = topology;
            diagnostic = $"Resolved '{next.BindingCount}' explicit Camera View-to-Output logical association(s).";
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _current = null;
            _disposed = true;
        }
    }
}
