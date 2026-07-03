using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Internal. One scene-authored Activity content handle known by the current Activity scope.
    /// It records local visibility adapter content; it is not a materialization handle or release contract.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Activity content entry introduced by F4B; not game-facing API.")]
    internal readonly struct ActivityContentEntry
    {
        public ActivityContentEntry(FrameworkContentHandle handle)
        {
            if (!handle.HasContentId)
            {
                throw new ArgumentException("Activity content entry requires a valid content handle.", nameof(handle));
            }

            if (handle.Scope != FrameworkContentScope.Activity)
            {
                throw new ArgumentException("Activity content entry requires a handle with Activity content scope.", nameof(handle));
            }

            if (handle.Kind != FrameworkContentKind.SceneAuthored)
            {
                throw new ArgumentException("Activity content entry requires a scene-authored content handle.", nameof(handle));
            }

            Handle = handle;
        }

        public FrameworkContentHandle Handle { get; }

        public string ToDiagnosticString()
        {
            return Handle.ToDiagnosticString();
        }
    }
}
