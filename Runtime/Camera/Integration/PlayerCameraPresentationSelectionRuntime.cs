using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerSlots;
using Immersive.Logging.Records;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Player adapter that projects each configured Player Slot's current Camera
    /// Subject occurrence into exact live CameraPresentationRuntime occurrences.
    ///
    /// It never exposes Player identity to Camera core and never holds serialized
    /// scene CameraSharedComposition references.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-E Player current Subject to live Camera Presentation explicit selection adapter.")]
    internal sealed class PlayerCameraPresentationSelectionRuntime :
        IDisposable
    {
        private sealed class ActiveSelection
        {
            internal ActiveSelection(
                PlayerSlotId playerSlotId,
                CameraPresentationMaterializationHandle handle,
                CameraCompositionSubjectSelectionContext context)
            {
                PlayerSlotId = playerSlotId;
                Handle = handle;
                Context = context;
            }

            internal PlayerSlotId PlayerSlotId { get; }

            internal CameraPresentationMaterializationHandle Handle { get; }

            internal CameraCompositionSubjectSelectionContext Context { get; }
        }

        private readonly PlayerActorCameraSubjectIntegrationRuntime _subjects;
        private readonly PlayerCameraPresentationTopology _topology;
        private readonly Dictionary<
            CameraPresentationMaterializationHandle,
            ActiveSelection> _active =
                new Dictionary<
                    CameraPresentationMaterializationHandle,
                    ActiveSelection>();
        private readonly FrameworkLogger _logger =
            FrameworkLogger.Create<
                PlayerCameraPresentationSelectionRuntime>();
        private bool _disposed;

        private PlayerCameraPresentationSelectionRuntime(
            PlayerActorCameraSubjectIntegrationRuntime subjects,
            PlayerCameraPresentationTopology topology)
        {
            _subjects = subjects ??
                throw new ArgumentNullException(nameof(subjects));
            _topology = topology ??
                throw new ArgumentNullException(nameof(topology));
            _subjects.SubjectChanged += OnSubjectChanged;
        }

        internal int ConfiguredBindingCount =>
            _topology.BindingCount;

        internal int ActiveOccurrenceCount =>
            _active.Count;

        internal bool LastReconciliationSucceeded { get; private set; }

        internal string Diagnostic { get; private set; }

        internal static bool TryCreate(
            PlayerActorCameraSubjectIntegrationRuntime subjects,
            PlayerCameraPresentationTopology topology,
            out PlayerCameraPresentationSelectionRuntime runtime,
            out string diagnostic)
        {
            runtime = null;
            if (subjects == null || topology == null)
            {
                diagnostic =
                    "Player Camera Presentation selection requires current Player Camera Subject evidence and a valid Presentation binding topology.";
                return false;
            }

            if (topology.BindingCount == 0)
            {
                diagnostic =
                    "Player Camera Presentation selection requires at least one explicit Player Slot -> Presentation binding.";
                return false;
            }

            runtime =
                new PlayerCameraPresentationSelectionRuntime(
                    subjects,
                    topology);
            runtime.LastReconciliationSucceeded = true;
            runtime.Diagnostic =
                $"Player Camera Presentation selection is ready with '{topology.BindingCount}' configured binding(s).";
            diagnostic = string.Empty;
            return true;
        }

        internal bool TryAttach(
            CameraPresentationMaterializationHandle handle,
            out bool attached,
            out string diagnostic)
        {
            attached = false;
            diagnostic = string.Empty;

            if (_disposed)
            {
                diagnostic =
                    "Player Camera Presentation selection runtime is already disposed.";
                return false;
            }

            if (handle == null ||
                handle.IsReleased ||
                handle.Definition == null)
            {
                diagnostic =
                    "Player Camera Presentation selection requires a live materialized Presentation occurrence.";
                return false;
            }

            if (!_topology.TryGetBinding(
                    handle.Definition,
                    out PlayerCameraPresentationBinding binding))
            {
                return true;
            }

            if (_active.ContainsKey(handle))
            {
                attached = true;
                return true;
            }

            var context =
                new CameraCompositionSubjectSelectionContext(
                    new CameraCompositionSubjectSelectionContextId(
                        $"camera-presentation-player-selection:{binding.PlayerSlotId.StableText}:{handle.RuntimeContentIdentity.StableText}"));

            var active =
                new ActiveSelection(
                    binding.PlayerSlotId,
                    handle,
                    context);

            try
            {
                Publish(active);
                handle.PresentationRuntime
                    .AttachSubjectSelectionSource(context);
                _active.Add(handle, active);
                attached = true;
                LastReconciliationSucceeded = true;
                Diagnostic =
                    $"Player Camera Presentation selection attached Slot '{binding.PlayerSlotId.StableText}' to live Presentation '{handle.Definition.name}'.";

                _logger.Debug(
                    "Player Camera Presentation selection attached.",
                    LogFields.Field(
                        "playerSlot",
                        binding.PlayerSlotId.StableText),
                    LogFields.Field(
                        "presentation",
                        handle.Definition.name),
                    LogFields.Field(
                        "presentationId",
                        handle.PresentationId.Value),
                    LogFields.Field(
                        "output",
                        handle.Definition.OutputDefinition.OutputId.Value),
                    LogFields.Field(
                        "runtimeOwner",
                        handle.ScopeContext.Owner.StableText));
                return true;
            }
            catch (Exception exception)
            {
                context.Clear();
                LastReconciliationSucceeded = false;
                diagnostic =
                    $"Player Camera Presentation selection could not attach Slot '{binding.PlayerSlotId.StableText}' to Presentation '{handle.Definition.name}'. {exception.Message}";
                Diagnostic = diagnostic;
                return false;
            }
        }

        internal void ForgetReleased(
            CameraPresentationMaterializationHandle handle)
        {
            if (handle == null ||
                !_active.TryGetValue(
                    handle,
                    out ActiveSelection active))
            {
                return;
            }

            active.Context.Clear();
            _active.Remove(handle);

            _logger.Debug(
                "Player Camera Presentation selection released.",
                LogFields.Field(
                    "playerSlot",
                    active.PlayerSlotId.StableText),
                LogFields.Field(
                    "presentation",
                    handle.Definition != null
                        ? handle.Definition.name
                        : "<unknown>"),
                LogFields.Field(
                    "presentationId",
                    handle.Definition != null &&
                    handle.Definition.HasValidId
                        ? handle.PresentationId.Value
                        : "<invalid>"));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subjects.SubjectChanged -= OnSubjectChanged;

            var active =
                new List<ActiveSelection>(_active.Values);
            for (int index = 0;
                 index < active.Count;
                 index++)
            {
                ActiveSelection selection =
                    active[index];
                selection.Context.Clear();

                if (selection.Handle == null ||
                    selection.Handle.IsReleased)
                {
                    continue;
                }

                try
                {
                    selection.Handle.PresentationRuntime
                        .DetachSubjectSelectionSource(
                            "Player Camera Presentation selection runtime disposed.");
                }
                catch
                {
                    // Presentation teardown remains the occurrence owner's
                    // responsibility; disposal must not invent a second
                    // failure authority here.
                }
            }

            _active.Clear();
            LastReconciliationSucceeded = true;
            Diagnostic =
                "Player Camera Presentation selection runtime was released.";
        }

        private void OnSubjectChanged(
            PlayerSlotId playerSlotId)
        {
            if (_disposed || !playerSlotId.IsValid)
            {
                return;
            }

            bool succeeded = true;
            string issue = string.Empty;

            foreach (ActiveSelection active in _active.Values)
            {
                if (active.PlayerSlotId != playerSlotId)
                {
                    continue;
                }

                try
                {
                    Publish(active);
                }
                catch (Exception exception)
                {
                    succeeded = false;
                    issue = exception.Message;
                    break;
                }
            }

            LastReconciliationSucceeded = succeeded;
            Diagnostic = succeeded
                ? $"Player Camera Presentation selection is current for Slot '{playerSlotId.StableText}'."
                : $"Player Camera Presentation selection failed for Slot '{playerSlotId.StableText}'. {issue}";
        }

        private void Publish(
            ActiveSelection active)
        {
            if (!_subjects.TryGetCurrentSubjectId(
                    active.PlayerSlotId,
                    out CameraSubjectId subjectId))
            {
                active.Context.Clear();
                return;
            }

            active.Context.Replace(
                new[] { subjectId });
        }
    }
}
