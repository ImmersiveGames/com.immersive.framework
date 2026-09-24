using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental pending ADR018-A certification.
    /// Optional read-only catalog capability for Progression Save backends that can
    /// enumerate/project their known slots as a framework manifest.
    ///
    /// A backend does not need to implement this interface in order to satisfy the
    /// canonical IProgressionSaveStore persistence contract.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-A optional Progression Save catalog capability; separate from the core backend contract.")]
    public interface IProgressionSaveCatalog
    {
        ProgressionSaveManifestReadResult ReadManifest();
    }
}
