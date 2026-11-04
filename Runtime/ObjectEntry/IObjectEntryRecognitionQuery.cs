using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Read-only, point-in-time recognition of a caller-supplied physical candidate. Recognition
    /// establishes Object Entry identity and occurrence availability only; it grants no gameplay authority.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Optional owner-bound physical Object Entry recognition query.")]
    public interface IObjectEntryRecognitionQuery
    {
        /// <summary>
        /// Recognizes the nearest Object Entry declaration in the candidate Collider's ancestor chain
        /// against committed Framework bindings. This method does not perform spatial discovery.
        /// </summary>
        ObjectEntryRecognitionResult Recognize(Collider candidate);
    }
}
