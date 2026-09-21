using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-031-B runtime Player Slot to Camera Composition binding.")]
    internal readonly struct PlayerCameraCompositionBinding
    {
        internal PlayerCameraCompositionBinding(
            PlayerSlotId playerSlotId,
            CameraSharedComposition composition)
        {
            PlayerSlotId = playerSlotId;
            Composition = composition;
        }

        internal PlayerSlotId PlayerSlotId { get; }

        internal CameraSharedComposition Composition { get; }

        internal bool IsValid => PlayerSlotId.IsValid && Composition != null;
    }

    /// <summary>
    /// Immutable Session projection of explicit Player Slot to Camera Composition bindings.
    /// It does not own Output identity, Camera requests, or presentation layout.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-031-B immutable Player Slot to Camera Composition topology.")]
    internal sealed class PlayerCameraCompositionTopology
    {
        private readonly PlayerCameraCompositionBinding[] _bindings;
        private readonly Dictionary<PlayerSlotId, PlayerCameraCompositionBinding> _bySlot;

        private PlayerCameraCompositionTopology(PlayerCameraCompositionBinding[] bindings)
        {
            _bindings = bindings ?? Array.Empty<PlayerCameraCompositionBinding>();
            _bySlot = new Dictionary<PlayerSlotId, PlayerCameraCompositionBinding>(
                _bindings.Length);
            for (int index = 0; index < _bindings.Length; index++)
            {
                _bySlot.Add(_bindings[index].PlayerSlotId, _bindings[index]);
            }
        }

        internal int BindingCount => _bindings.Length;

        internal IReadOnlyList<PlayerCameraCompositionBinding> Bindings => _bindings;

        internal static PlayerCameraCompositionTopology Empty { get; } =
            new PlayerCameraCompositionTopology(Array.Empty<PlayerCameraCompositionBinding>());

        internal static bool TryCreate(
            IReadOnlyList<PlayerCameraCompositionBinding> bindings,
            out PlayerCameraCompositionTopology topology,
            out string diagnostic)
        {
            topology = null;
            if (bindings == null)
            {
                diagnostic =
                    "Player Camera Composition topology requires explicit Slot to Composition bindings.";
                return false;
            }

            var ordered = new PlayerCameraCompositionBinding[bindings.Count];
            var slots = new HashSet<PlayerSlotId>();
            var compositions = new HashSet<CameraSharedComposition>();
            for (int index = 0; index < bindings.Count; index++)
            {
                PlayerCameraCompositionBinding binding = bindings[index];
                if (!binding.PlayerSlotId.IsValid)
                {
                    diagnostic =
                        $"Player Camera Composition binding at index '{index}' has an invalid Player Slot ID.";
                    return false;
                }

                if (binding.Composition == null)
                {
                    diagnostic =
                        $"Player Camera Composition binding for Slot '{binding.PlayerSlotId.StableText}' requires a Camera Composition.";
                    return false;
                }

                if (binding.Composition.SubjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
                {
                    diagnostic =
                        $"Player Camera Composition binding for Slot '{binding.PlayerSlotId.StableText}' requires Composition '{binding.Composition.name}' to use ExplicitSelection. " +
                        $"Current policy is '{binding.Composition.SubjectPolicy}'.";
                    return false;
                }

                if (!slots.Add(binding.PlayerSlotId))
                {
                    diagnostic =
                        $"Player Camera Composition policy has duplicate bindings for Player Slot '{binding.PlayerSlotId.StableText}'.";
                    return false;
                }

                if (!compositions.Add(binding.Composition))
                {
                    diagnostic =
                        $"Player Camera Composition policy has duplicate bindings for Composition '{binding.Composition.name}'.";
                    return false;
                }

                ordered[index] = binding;
            }

            Array.Sort(ordered, CompareBindings);
            topology = new PlayerCameraCompositionTopology(ordered);
            diagnostic = string.Empty;
            return true;
        }

        internal bool TryGetBinding(
            PlayerSlotId playerSlotId,
            out PlayerCameraCompositionBinding binding)
        {
            binding = default;
            return playerSlotId.IsValid &&
                _bySlot.TryGetValue(playerSlotId, out binding);
        }

        private static int CompareBindings(
            PlayerCameraCompositionBinding left,
            PlayerCameraCompositionBinding right)
        {
            return string.Compare(
                left.PlayerSlotId.StableText,
                right.PlayerSlotId.StableText,
                StringComparison.Ordinal);
        }
    }
}
