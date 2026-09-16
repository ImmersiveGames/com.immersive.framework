using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [Serializable]
    public sealed class PlayerCameraOutputBindingAuthoring
    {
        [SerializeField] private PlayerSlotProfile playerSlotProfile;
        [SerializeField] private CameraOutputDefinition outputDefinition;

        public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
        public CameraOutputDefinition OutputDefinition => outputDefinition;

        public void Configure(
            PlayerSlotProfile playerSlot,
            CameraOutputDefinition output)
        {
            playerSlotProfile = playerSlot;
            outputDefinition = output;
        }
    }

    /// <summary>
    /// Explicit typed policy that associates configured Player Slots with Framework
    /// Camera Outputs. Serialized authoring keeps asset references; runtime projection
    /// contains only PlayerSlotId to CameraOutputId identity.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Player Camera Output Policy")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-028-D explicit Player Slot to Camera Output integration policy.")]
    public sealed class PlayerCameraOutputPolicyAuthoring : MonoBehaviour
    {
        [SerializeField]
        private List<PlayerCameraOutputBindingAuthoring> bindings =
            new List<PlayerCameraOutputBindingAuthoring>();

        public IReadOnlyList<PlayerCameraOutputBindingAuthoring> Bindings => bindings;

        public void Configure(
            IReadOnlyList<PlayerCameraOutputBindingAuthoring> configuredBindings)
        {
            if (configuredBindings == null)
            {
                throw new ArgumentNullException(nameof(configuredBindings));
            }

            bindings = new List<PlayerCameraOutputBindingAuthoring>(configuredBindings);
        }
    }
}
