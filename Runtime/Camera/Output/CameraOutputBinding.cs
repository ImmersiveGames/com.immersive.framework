using Unity.Cinemachine;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public readonly struct CameraOutputBinding
    {
        public CameraOutputBinding(
            CameraOutputId outputId,
            UnityEngine.Camera unityCamera,
            CinemachineBrain brain)
        {
            OutputId = outputId;
            UnityCamera = unityCamera;
            Brain = brain;
        }

        public CameraOutputId OutputId { get; }
        public UnityEngine.Camera UnityCamera { get; }
        public CinemachineBrain Brain { get; }

        public bool IsValid =>
            OutputId.IsValid &&
            UnityCamera != null &&
            Brain != null &&
            Brain.gameObject == UnityCamera.gameObject;
    }
}
