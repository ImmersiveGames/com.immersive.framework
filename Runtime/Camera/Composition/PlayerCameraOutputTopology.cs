using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-028-D runtime Player Slot to Camera Output identity binding.")]
    internal readonly struct PlayerCameraOutputBinding
    {
        internal PlayerCameraOutputBinding(
            PlayerSlotId playerSlotId,
            CameraOutputId outputId)
        {
            PlayerSlotId = playerSlotId;
            OutputId = outputId;
        }

        internal PlayerSlotId PlayerSlotId { get; }
        internal CameraOutputId OutputId { get; }
        internal bool IsValid => PlayerSlotId.IsValid && OutputId.IsValid;
    }

    /// <summary>
    /// Immutable Session projection of explicit Player Slot to Camera Output identity.
    /// It owns no Player, Output, Camera request or presentation-layout authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-028-D immutable Player Slot to Camera Output topology.")]
    internal sealed class PlayerCameraOutputTopology
    {
        private readonly PlayerCameraOutputBinding[] _bindings;
        private readonly Dictionary<PlayerSlotId, PlayerCameraOutputBinding> _bySlot;

        private PlayerCameraOutputTopology(PlayerCameraOutputBinding[] bindings)
        {
            _bindings = bindings;
            _bySlot = new Dictionary<PlayerSlotId, PlayerCameraOutputBinding>(
                bindings.Length);
            for (int index = 0; index < bindings.Length; index++)
            {
                _bySlot.Add(bindings[index].PlayerSlotId, bindings[index]);
            }
        }

        internal int BindingCount => _bindings.Length;

        internal IReadOnlyList<PlayerCameraOutputBinding> Bindings =>
            _bindings;

        internal static bool TryCreate(
            IReadOnlyList<PlayerCameraOutputBinding> bindings,
            CameraOutputSessionTopology outputs,
            out PlayerCameraOutputTopology topology,
            out string diagnostic)
        {
            topology = null;
            if (bindings == null || outputs == null)
            {
                diagnostic =
                    "Player Camera Output topology requires explicit bindings and the current Camera Output Session topology.";
                return false;
            }

            var ordered = new PlayerCameraOutputBinding[bindings.Count];
            var slots = new HashSet<PlayerSlotId>();
            var outputsSeen = new HashSet<CameraOutputId>();
            for (int index = 0; index < bindings.Count; index++)
            {
                PlayerCameraOutputBinding binding = bindings[index];
                if (!binding.PlayerSlotId.IsValid)
                {
                    diagnostic =
                        $"Player Camera Output binding at index '{index}' has an invalid Player Slot ID.";
                    return false;
                }

                if (!binding.OutputId.IsValid)
                {
                    diagnostic =
                        $"Player Camera Output binding at index '{index}' has an invalid Camera Output ID.";
                    return false;
                }

                if (!slots.Add(binding.PlayerSlotId))
                {
                    diagnostic =
                        $"Player Camera Output bindings contain duplicate or conflicting bindings for Player Slot '{binding.PlayerSlotId.StableText}'.";
                    return false;
                }

                if (!outputsSeen.Add(binding.OutputId))
                {
                    diagnostic =
                        $"Player Camera Output bindings cannot bind Camera Output '{binding.OutputId}' to more than one Player Slot. Player-bound physical Outputs require exclusive Slot ownership.";
                    return false;
                }

                if (!outputs.TryGetOutput(
                        binding.OutputId,
                        out _,
                        out string outputDiagnostic))
                {
                    diagnostic =
                        $"Player Camera Output binding for Slot '{binding.PlayerSlotId.StableText}' references an unavailable Output. {outputDiagnostic}";
                    return false;
                }

                ordered[index] = binding;
            }

            Array.Sort(ordered, CompareBindings);
            topology = new PlayerCameraOutputTopology(ordered);
            diagnostic = string.Empty;
            return true;
        }

        internal bool TryGetBinding(
            PlayerSlotId playerSlotId,
            out PlayerCameraOutputBinding binding)
        {
            binding = default;
            return playerSlotId.IsValid &&
                _bySlot.TryGetValue(playerSlotId, out binding);
        }

        private static int CompareBindings(
            PlayerCameraOutputBinding left,
            PlayerCameraOutputBinding right)
        {
            return string.Compare(
                left.PlayerSlotId.StableText,
                right.PlayerSlotId.StableText,
                StringComparison.Ordinal);
        }
    }
}
