using System;
using Immersive.Framework.Camera;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Exact asset reference is definition authority. OutputId is a stable projection only.
    /// </summary>
    [CreateAssetMenu(fileName = "Camera Output", menuName = "Immersive Framework/Camera/Camera Output")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-027-A authored definition identity.")]
    public sealed class CameraOutputDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;

        public bool HasValidId => Guid.TryParseExact(stableId, "N", out var id) &&
                                  id != Guid.Empty && stableId == id.ToString("N");
        public CameraOutputId OutputId => HasValidId
            ? new CameraOutputId(stableId)
            : throw new InvalidOperationException("Camera Output definition requires an explicitly generated stable ID.");
        public string Description => description ?? string.Empty;
    }
}
