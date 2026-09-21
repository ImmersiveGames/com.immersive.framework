using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Single writer of Player-backed explicit Camera Subject selection. Each bound Player
    /// Slot publishes only its current Camera Subject id into that Slot's Composition.
    /// It does not rediscover Actors or Presentations and does not choose Camera Outputs.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-031-B Player Slot to explicit Camera Composition selection runtime.")]
    internal sealed class PlayerCameraCompositionIntegrationRuntime : IDisposable
    {
        private sealed class PublishedSelection
        {
            internal PublishedSelection(
                PlayerSlotId playerSlotId,
                CameraSharedComposition composition,
                CameraCompositionSubjectSelectionContext context)
            {
                PlayerSlotId = playerSlotId;
                Composition = composition;
                Context = context;
            }

            internal PlayerSlotId PlayerSlotId { get; }

            internal CameraSharedComposition Composition { get; }

            internal CameraCompositionSubjectSelectionContext Context { get; }
        }

        private readonly PlayerActorCameraSubjectIntegrationRuntime _subjects;
        private readonly List<PublishedSelection> _published = new List<PublishedSelection>();
        private bool _disposed;

        private PlayerCameraCompositionIntegrationRuntime(
            PlayerActorCameraSubjectIntegrationRuntime subjects)
        {
            _subjects = subjects;
        }

        internal bool LastReconciliationSucceeded { get; private set; }

        internal string Diagnostic { get; private set; }

        internal static bool TryCreate(
            PlayerActorCameraSubjectIntegrationRuntime subjects,
            PlayerCameraCompositionTopology topology,
            out PlayerCameraCompositionIntegrationRuntime runtime,
            out string diagnostic)
        {
            runtime = null;
            if (subjects == null || topology == null)
            {
                diagnostic =
                    "Player Camera Composition integration requires the current Player Actor Camera Subject runtime and an explicit Composition topology.";
                return false;
            }

            if (topology.BindingCount == 0)
            {
                diagnostic =
                    "Player Camera Composition integration requires at least one explicit Slot to Composition binding.";
                return false;
            }

            var candidate = new PlayerCameraCompositionIntegrationRuntime(subjects);
            candidate._subjects.SubjectChanged += candidate.OnSubjectChanged;
            if (!candidate.TryBind(topology, out diagnostic))
            {
                candidate.Dispose();
                return false;
            }

            candidate.LastReconciliationSucceeded = true;
            candidate.Diagnostic =
                $"Player Camera Composition integration is ready with '{topology.BindingCount}' explicit binding(s).";
            runtime = candidate;
            diagnostic = string.Empty;
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subjects.SubjectChanged -= OnSubjectChanged;
            for (int index = 0; index < _published.Count; index++)
            {
                PublishedSelection published = _published[index];
                published.Context.Clear();
                if (published.Composition != null)
                {
                    published.Composition.DetachSubjectSelectionSource(
                        "Player Camera Composition integration released.");
                }
            }

            _published.Clear();
            LastReconciliationSucceeded = true;
            Diagnostic = "Player Camera Composition integration was released.";
        }

        private bool TryBind(
            PlayerCameraCompositionTopology topology,
            out string diagnostic)
        {
            IReadOnlyList<PlayerCameraCompositionBinding> bindings = topology.Bindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                PlayerCameraCompositionBinding binding = bindings[index];
                if (!binding.IsValid ||
                    binding.Composition.SubjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
                {
                    diagnostic =
                        $"Player Camera Composition binding at index '{index}' is not an ExplicitSelection Composition for a valid Player Slot.";
                    return false;
                }

                var context = new CameraCompositionSubjectSelectionContext(
                    new CameraCompositionSubjectSelectionContextId(
                        "camera-composition-subject-selection:" +
                        binding.Composition.MembershipContextIdText));
                var published = new PublishedSelection(
                    binding.PlayerSlotId,
                    binding.Composition,
                    context);
                _published.Add(published);
                Publish(published);
                try
                {
                    binding.Composition.AttachSubjectSelectionSource(context);
                }
                catch (Exception exception)
                {
                    diagnostic =
                        $"Player Camera Composition binding for Slot '{binding.PlayerSlotId.StableText}' could not attach explicit Subject selection. {exception.Message}";
                    return false;
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        private void OnSubjectChanged(PlayerSlotId playerSlotId)
        {
            if (_disposed || !playerSlotId.IsValid ||
                !TryGetPublished(playerSlotId, out PublishedSelection published))
            {
                return;
            }

            try
            {
                Publish(published);
                LastReconciliationSucceeded = true;
                Diagnostic =
                    $"Player Camera Composition selection is current for Slot '{playerSlotId.StableText}'.";
            }
            catch (Exception exception)
            {
                LastReconciliationSucceeded = false;
                Diagnostic =
                    $"Player Camera Composition selection failed for Slot '{playerSlotId.StableText}'. {exception.Message}";
            }
        }

        private void Publish(PublishedSelection published)
        {
            if (!_subjects.TryGetCurrentSubjectId(
                    published.PlayerSlotId,
                    out CameraSubjectId subjectId))
            {
                published.Context.Clear();
                return;
            }

            published.Context.Replace(new[] { subjectId });
        }

        private bool TryGetPublished(
            PlayerSlotId playerSlotId,
            out PublishedSelection published)
        {
            for (int index = 0; index < _published.Count; index++)
            {
                if (_published[index].PlayerSlotId == playerSlotId)
                {
                    published = _published[index];
                    return true;
                }
            }

            published = null;
            return false;
        }
    }
}
