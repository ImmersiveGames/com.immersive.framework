using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-E runtime Player Slot to Camera Presentation explicit selection binding.")]
    internal readonly struct PlayerCameraPresentationBinding
    {
        internal PlayerCameraPresentationBinding(
            PlayerSlotId playerSlotId,
            CameraPresentationDefinition presentationDefinition)
        {
            PlayerSlotId = playerSlotId;
            PresentationDefinition = presentationDefinition;
        }

        internal PlayerSlotId PlayerSlotId { get; }

        internal CameraPresentationDefinition PresentationDefinition { get; }

        internal bool IsValid =>
            PlayerSlotId.IsValid &&
            PresentationDefinition != null &&
            PresentationDefinition.HasValidId;
    }

    /// <summary>
    /// Immutable Session projection of Player Slot -> reusable Camera Presentation
    /// selection bindings. One Slot may target several definitions across lifecycle
    /// scopes; one Presentation definition may belong to at most one Player Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-E immutable Player Slot to Camera Presentation selection topology.")]
    internal sealed class PlayerCameraPresentationTopology
    {
        private readonly PlayerCameraPresentationBinding[] _bindings;
        private readonly Dictionary<
            CameraPresentationDefinition,
            PlayerCameraPresentationBinding> _byPresentation;

        private PlayerCameraPresentationTopology(
            PlayerCameraPresentationBinding[] bindings)
        {
            _bindings = bindings ??
                Array.Empty<PlayerCameraPresentationBinding>();
            _byPresentation =
                new Dictionary<
                    CameraPresentationDefinition,
                    PlayerCameraPresentationBinding>(
                        _bindings.Length);

            for (int index = 0;
                 index < _bindings.Length;
                 index++)
            {
                _byPresentation.Add(
                    _bindings[index].PresentationDefinition,
                    _bindings[index]);
            }
        }

        internal static PlayerCameraPresentationTopology Empty { get; } =
            new PlayerCameraPresentationTopology(
                Array.Empty<PlayerCameraPresentationBinding>());

        internal int BindingCount => _bindings.Length;

        internal IReadOnlyList<PlayerCameraPresentationBinding> Bindings =>
            _bindings;

        internal static bool TryCreate(
            IReadOnlyList<PlayerCameraPresentationBindingAuthoring>
                authoredBindings,
            out PlayerCameraPresentationTopology topology,
            out string diagnostic)
        {
            topology = null;
            authoredBindings ??=
                Array.Empty<PlayerCameraPresentationBindingAuthoring>();

            if (authoredBindings.Count == 0)
            {
                topology = Empty;
                diagnostic = string.Empty;
                return true;
            }

            var projected =
                new PlayerCameraPresentationBinding[
                    authoredBindings.Count];
            var seenPresentations =
                new HashSet<CameraPresentationDefinition>();
            var seenPresentationIds =
                new HashSet<CameraPresentationId>();

            for (int index = 0;
                 index < authoredBindings.Count;
                 index++)
            {
                PlayerCameraPresentationBindingAuthoring authored =
                    authoredBindings[index];
                if (authored == null)
                {
                    diagnostic =
                        $"Camera Session Player Presentation Bindings[{index}] is missing.";
                    return false;
                }

                var profile = authored.PlayerSlotProfile;
                if (profile == null)
                {
                    diagnostic =
                        $"Camera Session Player Presentation binding at index '{index}' requires a valid PlayerSlotProfile.";
                    return false;
                }

                if (!profile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string slotIssue))
                {
                    diagnostic =
                        $"Camera Session Player Presentation binding at index '{index}' requires a valid PlayerSlotProfile. {slotIssue}";
                    return false;
                }

                CameraPresentationDefinition presentation =
                    authored.PresentationDefinition;
                if (presentation == null)
                {
                    diagnostic =
                        $"Camera Session Player Presentation binding for Slot '{playerSlotId.StableText}' requires a CameraPresentationDefinition.";
                    return false;
                }

                if (!presentation.TryValidate(
                        out string presentationIssue))
                {
                    diagnostic =
                        $"Camera Session Player Presentation binding for Slot '{playerSlotId.StableText}' requires a valid CameraPresentationDefinition. {presentationIssue}";
                    return false;
                }

                if (presentation.SubjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection)
                {
                    diagnostic =
                        $"Camera Session Player Presentation binding for Slot '{playerSlotId.StableText}' requires Presentation '{presentation.name}' to use ExplicitSelection.";
                    return false;
                }

                if (!seenPresentations.Add(presentation))
                {
                    diagnostic =
                        $"Camera Presentation '{presentation.name}' may be bound to only one Player Slot.";
                    return false;
                }

                if (!seenPresentationIds.Add(
                        presentation.PresentationId))
                {
                    diagnostic =
                        $"Camera Session Player Presentation bindings contain duplicate CameraPresentationId '{presentation.PresentationId.Value}'.";
                    return false;
                }

                projected[index] =
                    new PlayerCameraPresentationBinding(
                        playerSlotId,
                        presentation);
            }

            Array.Sort(projected, CompareBindings);
            topology =
                new PlayerCameraPresentationTopology(projected);
            diagnostic = string.Empty;
            return true;
        }

        internal bool TryGetBinding(
            CameraPresentationDefinition presentation,
            out PlayerCameraPresentationBinding binding)
        {
            binding = default;
            return presentation != null &&
                _byPresentation.TryGetValue(
                    presentation,
                    out binding);
        }

        private static int CompareBindings(
            PlayerCameraPresentationBinding left,
            PlayerCameraPresentationBinding right)
        {
            int bySlot =
                string.Compare(
                    left.PlayerSlotId.StableText,
                    right.PlayerSlotId.StableText,
                    StringComparison.Ordinal);
            if (bySlot != 0)
            {
                return bySlot;
            }

            return string.Compare(
                left.PresentationDefinition.PresentationId.Value,
                right.PresentationDefinition.PresentationId.Value,
                StringComparison.Ordinal);
        }
    }
}
