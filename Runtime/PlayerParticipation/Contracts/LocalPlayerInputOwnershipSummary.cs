using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-025 immutable local Player physical input ownership evidence.")]
    public readonly struct LocalPlayerInputOwnershipSummary
    {
        private static readonly IReadOnlyList<LocalPlayerInputDeviceSummary>
            NoDevices = new ReadOnlyCollection<LocalPlayerInputDeviceSummary>(
                Array.Empty<LocalPlayerInputDeviceSummary>());

        private readonly string _controlScheme;
        private readonly IReadOnlyList<LocalPlayerInputDeviceSummary> _devices;

        internal LocalPlayerInputOwnershipSummary(
            int unityPlayerIndex,
            string controlScheme,
            IReadOnlyList<LocalPlayerInputDeviceSummary> devices)
        {
            UnityPlayerIndex = unityPlayerIndex;
            _controlScheme = controlScheme;
            _devices = CopyDevices(devices);
        }

        /// <summary>
        /// Índice técnico do PlayerInput; não representa a identidade do Player Slot.
        /// </summary>
        public int UnityPlayerIndex { get; }

        /// <summary>
        /// Scheme efetivo observado, independente do hint solicitado no Join.
        /// </summary>
        public string ControlScheme => _controlScheme ?? string.Empty;

        public IReadOnlyList<LocalPlayerInputDeviceSummary> Devices =>
            _devices ?? NoDevices;

        private static IReadOnlyList<LocalPlayerInputDeviceSummary> CopyDevices(
            IReadOnlyList<LocalPlayerInputDeviceSummary> source)
        {
            if (source == null || source.Count == 0)
            {
                return NoDevices;
            }

            var copy = new LocalPlayerInputDeviceSummary[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return new ReadOnlyCollection<LocalPlayerInputDeviceSummary>(copy);
        }
    }
}
