using System;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    /// <summary>
    /// Generates explicit stable identities for Camera authoring surfaces.
    /// Existing values are never replaced automatically.
    /// </summary>
    internal static class CameraAuthoringIdUtility
    {
        internal static string GenerateIdText()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
