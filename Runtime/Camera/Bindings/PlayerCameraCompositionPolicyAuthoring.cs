using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [Serializable]
    public sealed class PlayerCameraCompositionBindingAuthoring
    {
        [SerializeField] private PlayerSlotProfile playerSlotProfile;
        [SerializeField] private CameraSharedComposition composition;

        public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;

        public CameraSharedComposition Composition => composition;

        public void Configure(
            PlayerSlotProfile playerSlot,
            CameraSharedComposition sharedComposition)
        {
            playerSlotProfile = playerSlot;
            composition = sharedComposition;
        }
    }

    /// <summary>
    /// Explicit policy that associates one Player Slot with the Camera Composition whose
    /// explicit Subject selection follows that Slot's current Camera Subject. It does not
    /// select an Output, Rig, Actor, Transform, or serialized Subject id.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Player Camera Composition Policy")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-B explicit Player Slot to Camera Composition integration policy.")]
    public sealed class PlayerCameraCompositionPolicyAuthoring : MonoBehaviour
    {
        [SerializeField]
        private List<PlayerCameraCompositionBindingAuthoring> bindings =
            new List<PlayerCameraCompositionBindingAuthoring>();

        public IReadOnlyList<PlayerCameraCompositionBindingAuthoring> Bindings => bindings;

        public void Configure(
            IReadOnlyList<PlayerCameraCompositionBindingAuthoring> configuredBindings)
        {
            if (configuredBindings == null)
            {
                throw new ArgumentNullException(nameof(configuredBindings));
            }

            bindings = new List<PlayerCameraCompositionBindingAuthoring>(configuredBindings);
        }
    }
}
