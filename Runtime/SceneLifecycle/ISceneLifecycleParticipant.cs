using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    internal interface ISceneLifecycleParticipant
    {
        bool OnSceneAvailable(Scene scene, IReadOnlyList<GameObject> roots, out string diagnostic);
        bool OnSceneReleasing(Scene scene, IReadOnlyList<GameObject> roots, string reason, out string diagnostic);
    }
}
