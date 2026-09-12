using System;
using Immersive.Framework.Camera;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Exact asset reference is definition authority. ViewId is a stable projection only.
    /// </summary>
    [CreateAssetMenu(fileName = "Camera View", menuName = "Immersive Framework/Camera/Camera View")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-027-A authored definition identity.")]
    public sealed class CameraViewDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string stableId = string.Empty;
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;

        public bool HasValidId => Guid.TryParseExact(stableId, "N", out var id) &&
                                  id != Guid.Empty && stableId == id.ToString("N");
        public CameraViewId ViewId => HasValidId
            ? new CameraViewId(stableId)
            : throw new InvalidOperationException("Camera View definition requires an explicitly generated stable ID.");
        public string Description => description ?? string.Empty;
    }
}
