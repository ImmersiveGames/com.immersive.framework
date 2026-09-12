using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Session-scoped authority for the explicitly authored 1..N camera outputs.
    /// It owns collection, exact lookup, deterministic diagnostics and teardown;
    /// each CameraOutputAuthoring continues to own its own Session/Context/Applicator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session-scoped 1..N Camera Output topology.")]
    public sealed class CameraOutputSessionTopology : IDisposable
    {
        private readonly CameraOutputAuthoring[] _outputs;
        private readonly Dictionary<CameraOutputId, CameraOutputAuthoring> _outputsById;
        private bool _disposed;

        private CameraOutputSessionTopology(CameraOutputAuthoring[] outputs)
        {
            _outputs = outputs;
            _outputsById = new Dictionary<CameraOutputId, CameraOutputAuthoring>(outputs.Length);
            for (int index = 0; index < outputs.Length; index++)
            {
                var outputId = new CameraOutputId(outputs[index].OutputIdText);
                _outputsById.Add(outputId, outputs[index]);
            }
        }

        public int OutputCount => _outputs.Length;

        public static bool TryCreate(
            IReadOnlyList<CameraOutputAuthoring> authoredOutputs,
            out CameraOutputSessionTopology topology,
            out string diagnostic)
        {
            topology = null;
            if (authoredOutputs == null || authoredOutputs.Count == 0)
            {
                diagnostic = "Camera Output topology requires at least one explicitly authored output.";
                return false;
            }

            var outputs = new CameraOutputAuthoring[authoredOutputs.Count];
            var definitions = new List<CameraOutputDefinition>();
            foreach (var authored in authoredOutputs)
                definitions.Add(authored != null ? authored.OutputDefinition : null);
            try
            {
                CameraDefinitionValidation.ValidateOutputs(definitions);
            }
            catch (InvalidOperationException exception)
            {
                diagnostic = exception.Message;
                return false;
            }
            var ids = new HashSet<CameraOutputId>();
            var physicalBindings = new HashSet<UnityEngine.Object>();
            for (int index = 0; index < authoredOutputs.Count; index++)
            {
                CameraOutputAuthoring output = authoredOutputs[index];
                if (output == null)
                {
                    diagnostic = $"Camera Output topology contains a missing authoring reference at index '{index}'.";
                    return false;
                }

                var outputId = new CameraOutputId(output.OutputIdText);
                if (!outputId.IsValid)
                {
                    diagnostic = $"Camera Output topology contains an invalid Output ID at index '{index}'.";
                    return false;
                }
                if (!ids.Add(outputId))
                {
                    diagnostic = $"Camera Output topology contains duplicate Output ID '{outputId}'.";
                    return false;
                }
                if ((output.UnityCamera != null && !physicalBindings.Add(output.UnityCamera)) ||
                    (output.CinemachineBrain != null && !physicalBindings.Add(output.CinemachineBrain)) ||
                    (output.DefaultCameraRig != null && !physicalBindings.Add(output.DefaultCameraRig)))
                {
                    diagnostic =
                        $"Camera Output '{outputId}' shares a physical Camera, Cinemachine Brain or Default Camera Rig with another Output.";
                    return false;
                }
                outputs[index] = output;
            }

            Array.Sort(outputs, CompareOutputs);
            int initializedCount = 0;
            for (int index = 0; index < outputs.Length; index++)
            {
                if (!outputs[index].TryInitialize(out string initializationDiagnostic))
                {
                    for (int rollbackIndex = initializedCount - 1; rollbackIndex >= 0; rollbackIndex--)
                    {
                        outputs[rollbackIndex].TeardownSession("TopologyCreationRollback");
                    }
                    diagnostic =
                        $"Camera Output topology could not initialize output '{outputs[index].OutputIdText}'. {initializationDiagnostic}";
                    return false;
                }
                initializedCount++;
            }

            topology = new CameraOutputSessionTopology(outputs);
            diagnostic = $"Camera Output topology initialized with '{outputs.Length}' explicit output(s): {JoinOutputIds(outputs)}.";
            return true;
        }

        public bool TryGetOutput(
            CameraOutputId outputId,
            out CameraOutputAuthoring output,
            out string diagnostic)
        {
            output = null;
            if (_disposed)
            {
                diagnostic = "Camera Output topology is already torn down.";
                return false;
            }
            if (!outputId.IsValid)
            {
                diagnostic = "Camera Output lookup requires an explicit valid Output ID.";
                return false;
            }
            if (!_outputsById.TryGetValue(outputId, out output))
            {
                diagnostic = $"Camera Output '{outputId}' is not part of the active Session topology.";
                return false;
            }
            diagnostic = string.Empty;
            return true;
        }

        public CameraOutputTopologySnapshot CaptureSnapshot()
        {
            var entries = new CameraOutputTopologyEntrySnapshot[_outputs.Length];
            for (int index = 0; index < _outputs.Length; index++)
            {
                CameraOutputAuthoring output = _outputs[index];
                entries[index] = new CameraOutputTopologyEntrySnapshot(
                    new CameraOutputId(output.OutputIdText),
                    output.IsInitialized,
                    output.Context != null
                        ? output.Context.CaptureSnapshot()
                        : default,
                    output.LastStatus,
                    output.LastDiagnostic);
            }
            return new CameraOutputTopologySnapshot(_disposed, entries);
        }

        public void Dispose()
        {
            if (_disposed) return;
            for (int index = _outputs.Length - 1; index >= 0; index--)
            {
                _outputs[index].TeardownSession("SessionTopologyTeardown");
            }
            _disposed = true;
            _outputsById.Clear();
        }

        private static int CompareOutputs(CameraOutputAuthoring left, CameraOutputAuthoring right) =>
            string.Compare(left.OutputIdText, right.OutputIdText, StringComparison.Ordinal);

        private static string JoinOutputIds(IReadOnlyList<CameraOutputAuthoring> outputs)
        {
            var values = new string[outputs.Count];
            for (int index = 0; index < outputs.Count; index++) values[index] = outputs[index].OutputIdText;
            return string.Join(", ", values);
        }
    }

    public readonly struct CameraOutputTopologyEntrySnapshot
    {
        public CameraOutputTopologyEntrySnapshot(
            CameraOutputId outputId,
            bool initialized,
            CameraOutputContextSnapshot context,
            string status,
            string diagnostic)
        {
            OutputId = outputId;
            Initialized = initialized;
            Context = context;
            Status = status ?? string.Empty;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public CameraOutputId OutputId { get; }
        public bool Initialized { get; }
        public CameraOutputContextSnapshot Context { get; }
        public string Status { get; }
        public string Diagnostic { get; }
    }

    public sealed class CameraOutputTopologySnapshot
    {
        internal CameraOutputTopologySnapshot(
            bool tornDown,
            CameraOutputTopologyEntrySnapshot[] outputs)
        {
            IsTornDown = tornDown;
            Outputs = Array.AsReadOnly(outputs ?? Array.Empty<CameraOutputTopologyEntrySnapshot>());
        }

        public bool IsTornDown { get; }
        public IReadOnlyList<CameraOutputTopologyEntrySnapshot> Outputs { get; }
        public int OutputCount => Outputs.Count;
    }
}
