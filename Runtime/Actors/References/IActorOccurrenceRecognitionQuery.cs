using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Read-only physical recognition of a caller-supplied candidate against the Framework's current
    /// committed Actor occurrences. Recognition establishes occurrence identity only; it does not perform
    /// spatial discovery, visibility evaluation or gameplay authorization.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Owner-bound physical Actor occurrence recognition using the existing Actor occurrence authority.")]
    public interface IActorOccurrenceRecognitionQuery
    {
        /// <summary>
        /// Recognizes the nearest Actor declaration in the supplied Collider's ancestor chain and resolves
        /// its exact declared GameObject against current committed Player or NonPlayer occurrences.
        /// </summary>
        ActorOccurrenceRecognitionResult Recognize(Collider candidate);
    }
}
