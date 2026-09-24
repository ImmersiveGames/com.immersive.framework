using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Stable GameApplication Camera Session authoring for one explicit
    /// Player Slot -> Camera Output binding.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-D GameApplication Camera Session Player Slot to Output binding.")]
    public sealed class PlayerCameraOutputBindingAuthoring
    {
        [SerializeField]
        private PlayerSlotProfile playerSlotProfile;

        [SerializeField]
        private CameraOutputDefinition outputDefinition;

        public PlayerSlotProfile PlayerSlotProfile =>
            playerSlotProfile;

        public CameraOutputDefinition OutputDefinition =>
            outputDefinition;

        public void Configure(
            PlayerSlotProfile playerSlot,
            CameraOutputDefinition output)
        {
            playerSlotProfile = playerSlot;
            outputDefinition = output;
        }
    }
}
