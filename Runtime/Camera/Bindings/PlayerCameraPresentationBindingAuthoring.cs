using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Stable Player adapter authoring: one Player Slot supplies the current
    /// explicit Camera Subject selection for one Camera Presentation definition.
    ///
    /// The binding targets reusable Presentation intent, never a scene
    /// CameraSharedComposition or a materialized runtime occurrence.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-E Player Slot to Camera Presentation explicit selection binding.")]
    public sealed class PlayerCameraPresentationBindingAuthoring
    {
        [SerializeField]
        private PlayerSlotProfile playerSlotProfile;

        [SerializeField]
        private CameraPresentationDefinition presentationDefinition;

        public PlayerSlotProfile PlayerSlotProfile =>
            playerSlotProfile;

        public CameraPresentationDefinition PresentationDefinition =>
            presentationDefinition;

        public void Configure(
            PlayerSlotProfile playerSlot,
            CameraPresentationDefinition presentation)
        {
            playerSlotProfile = playerSlot;
            presentationDefinition = presentation;
        }
    }
}
