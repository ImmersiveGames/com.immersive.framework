using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// Typed ScriptableObject provider boundary for custom/third-party Progression
    /// Save backends.
    ///
    /// Implementations own their vendor-specific configuration and translate it into
    /// the Stable IProgressionSaveStore contract. The framework never discovers
    /// providers by reflection or silently falls back when a selected provider fails.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C typed custom Progression Save backend provider asset boundary.")]
    public abstract class ProgressionSaveStoreProviderAsset : ScriptableObject
    {
        /// <summary>
        /// Validates provider-authored configuration without creating runtime state.
        /// </summary>
        public virtual bool TryValidate(out string issue)
        {
            issue = string.Empty;
            return true;
        }

        /// <summary>
        /// Explicitly materializes one application-scoped backend instance.
        /// Return false with an actionable issue when the selected backend cannot be
        /// created. Do not substitute another backend.
        /// </summary>
        public abstract bool TryCreateStore(
            ProgressionSaveStoreCreationContext context,
            out IProgressionSaveStore store,
            out string issue);
    }
}
