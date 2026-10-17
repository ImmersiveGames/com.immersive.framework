using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    internal interface ISceneLifecycleParticipant
    {
        SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots);

        SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason);
    }

    /// <summary>Callback opcional para participantes que precisam restaurar uma liberação de Scene compensada.</summary>
    internal interface ISceneLifecycleReleaseCompensationParticipant
    {
        void OnSceneReleaseCompensated(SceneCompositionScope scope);
    }
}
